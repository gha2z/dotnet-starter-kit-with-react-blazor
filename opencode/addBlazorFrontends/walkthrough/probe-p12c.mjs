// probe-p12c.mjs — P12.2 non-uploader role check: FilePreviewDialog hides manage actions
// (visibility switch, delete) when current user != CreatedByUserId.
import { pathToFileURL } from 'node:url';
import path from 'node:path';
import fs from 'node:fs';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const CLIENTS = path.resolve(here, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const shots = path.join(here, 'evidence', 'p12-behaviors');
fs.mkdirSync(shots, { recursive: true });

const BASE = 'http://localhost:5176';
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

const results = [];
const ok = (id, name, pass, detail = '') =>
  results.push({ pass: !!pass, line: `${pass ? 'PASS' : 'FAIL'} [${id}] ${name}${detail ? ' — ' + detail : ''}` });

const consoleErrors = [];
const browser = await chromium.launch({ ignoreHTTPSErrors: true });
try {
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  page.on('console', (m) => { if (m.type() === 'error' && !/mono_download|favicon|instantiate_wasm_module/i.test(m.text())) consoleErrors.push(m.text()); });
  page.on('pageerror', (e) => consoleErrors.push(`[pageerror] ${e.message}`));

  // login as alice (non-uploader)
  await page.goto(`${BASE}/login`, { waitUntil: 'load' });
  await page.waitForTimeout(2500);
  await page.getByLabel(/tenant/i).first().fill('acme');
  await page.getByLabel(/tenant/i).first().blur();
  await page.getByLabel(/email/i).first().fill('alice@acme.com');
  await page.getByLabel(/email/i).first().blur();
  await page.getByLabel(/password/i).first().fill('Password123!');
  await page.getByLabel(/password/i).first().blur();
  await page.waitForTimeout(400);
  await page.getByRole('button', { name: /sign in/i }).first().click();
  await page.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 30000 });
  ok('P12C', 'alice login', true);

  await page.goto(`${BASE}/files`, { waitUntil: 'load' });
  await page.waitForTimeout(3500);

  // switch to "Shared in tenant" — rows there are owned by OTHER users, so the
  // uploader gate (CreatedByUserId == current user) must HIDE manage actions.
  await page.getByRole('button', { name: /shared in tenant/i }).first().click();
  await page.waitForTimeout(1500);

  const rows = page.locator('table tbody tr');
  const rowCount = await rows.count();
  ok('P12C', 'shared files visible to alice', rowCount > 0, `${rowCount} rows`);
  if (rowCount > 0) {
    // find a row owned by ANOTHER user (first shared row may be alice's own public file)
    const tokRes = await page.request.post(`${BASE.replace('5176', '7030').replace('http://', 'https://')}/api/v1/identity/token/issue`, {
      headers: { tenant: 'acme', 'content-type': 'application/json' },
      data: { email: 'alice@acme.com', password: 'Password123!' },
    });
    const tok = (await tokRes.json()).accessToken;
    const uid = JSON.parse(Buffer.from(tok.split('.')[1], 'base64').toString('utf8')).sub;

    let targetText = '';
    let foundOther = false;
    for (let i = 0; i < rowCount && !foundOther; i++) {
      await rows.nth(i).click();
      await page.waitForTimeout(1200);
      const dlg = page.locator('.mud-dialog').last();
      if (!(await dlg.isVisible().catch(() => false))) continue;
      const txt = await dlg.innerText().catch(() => '');
      const m = txt.match(/Uploaded by\s*([0-9a-f-]{36})/i);
      if (m && m[1].toLowerCase() !== String(uid).toLowerCase()) {
        foundOther = true;
        targetText = txt;
        await page.screenshot({ path: path.join(shots, 'file-preview-non-uploader.png') });
        break;
      }
      const closeBtn = dlg.getByRole('button', { name: /close/i }).last();
      if (await closeBtn.count() > 0) { await closeBtn.click(); await page.waitForTimeout(500); }
    }
    if (!foundOther) {
      ok('P12C', 'found a shared row owned by another user', false);
    } else {
      const dlg = page.locator('.mud-dialog').last();
      ok('P12C', 'found a shared row owned by another user', true);
      ok('P12C', 'preview dialog opens for non-uploader', /Visibility/i.test(targetText));
      ok('P12C', 'non-uploader: no visibility switch', (await dlg.locator('.mud-switch').count()) === 0);
      ok('P12C', 'non-uploader: no Delete action', !/\bDelete\b/i.test(targetText));
      ok('P12C', 'non-uploader: Download + Close still present', /Download/i.test(targetText) && /Close/i.test(targetText));
    }
  }

  ok('P12C', 'GLOBAL no console/page errors', consoleErrors.length === 0, consoleErrors.slice(0, 2).join(' | '));
} catch (err) {
  ok('P12C', 'probe crashed', false, String(err).slice(0, 300));
} finally {
  await browser.close();
}

for (const r of results) console.log(' ', r.line);
const pass = results.filter((r) => r.pass).length;
console.log(`TOTAL ${pass} pass, ${results.length - pass} fail`);
