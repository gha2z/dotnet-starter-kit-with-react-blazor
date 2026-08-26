// probe-p12.mjs — P12 behavioral verification (product images + file preview dialog)
import { pathToFileURL } from 'node:url';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const CLIENTS = path.resolve(here, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const shots = path.join(here, 'evidence', 'p12-behaviors');
fs.mkdirSync(shots, { recursive: true });

const BASE = 'http://localhost:5176';
const API = 'https://localhost:7030';
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

const results = [];
const ok = (id, name, pass, detail = '') =>
  results.push({ pass: !!pass, line: `${pass ? 'PASS' : 'FAIL'} [${id}] ${name}${detail ? ' — ' + detail : ''}` });

const consoleErrors = [];
const badResponses = [];
const ts = Date.now().toString(36);

const browser = await chromium.launch({ ignoreHTTPSErrors: true });
try {
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  page.on('response', (r) => { if (r.status() >= 400) badResponses.push(`${r.status()} ${r.url().slice(0, 120)}`); });
  page.on('console', (m) => { if (m.type() === 'error' && !/mono_download|favicon|instantiate_wasm_module/i.test(m.text())) consoleErrors.push(m.text()); });
  page.on('pageerror', (e) => consoleErrors.push(`[pageerror] ${e.message}`));

  // login
  await page.goto(`${BASE}/login`, { waitUntil: 'load' });
  await page.waitForTimeout(2500);
  await page.getByLabel(/tenant/i).first().fill('acme');
  await page.getByLabel(/tenant/i).first().blur();
  await page.getByLabel(/email/i).first().fill('admin@acme.com');
  await page.getByLabel(/email/i).first().blur();
  await page.getByLabel(/password/i).first().fill('Password123!');
  await page.getByLabel(/password/i).first().blur();
  await page.waitForTimeout(400);
  await page.getByRole('button', { name: /sign in/i }).first().click();
  await page.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 30000 });
  await page.waitForTimeout(2500);

  // pick a product from the list (first row) via API to get an id
  const tokRes = await page.request.post(`${API}/api/v1/identity/token/issue`, {
    headers: { tenant: 'acme', 'content-type': 'application/json' },
    data: { email: 'admin@acme.com', password: 'Password123!' },
  });
  const tok = (await tokRes.json()).accessToken;
  const listRes = await page.request.get(`${API}/api/v1/catalog/products?PageNumber=1&PageSize=5`, { headers: { authorization: `Bearer ${tok}`, tenant: 'acme' } });
  const products = (await listRes.json()).items ?? [];
  const product = products[0];
  ok('P12', 'product available', !!product, product?.sku);

  // ── product detail: image upload → preview → confirm-remove ──
  await page.goto(`${BASE}/catalog/products/${product.id}`, { waitUntil: 'load' });
  await page.waitForSelector('.fsh-detail-stat', { timeout: 30000 });
  await page.waitForTimeout(1500);

  const tilesBefore = await page.locator('.fsh-prod-image-tile').count();
  await page.setInputFiles('#detail-image-input', { name: `qa-img-${ts}.png`, mimeType: 'image/png', buffer: Buffer.from(
    'iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAAF0lEQVR4nGP8z8Dwn4EIwESMolGFlCsEAE1UAhGjOJKMAAAAAElFTkSuQmCC', 'base64') });
  await page.waitForTimeout(3500);
  const tilesAfter = await page.locator('.fsh-prod-image-tile').count();
  ok('P12', 'image uploaded → tile appears', tilesAfter === tilesBefore + 1, `${tilesBefore}→${tilesAfter}`);

  // click tile → preview dialog
  await page.locator('.fsh-prod-image-tile img').first().click();
  await page.waitForTimeout(900);
  const previewDlg = page.locator('.mud-dialog').last();
  const previewVisible = await previewDlg.isVisible().catch(() => false);
  const previewHasImg = previewVisible && await previewDlg.locator('img').count() > 0;
  ok('P12', 'image click → preview dialog with image', previewHasImg);
  if (previewVisible) {
    await page.screenshot({ path: path.join(shots, 'image-preview-dialog.png') });
    await previewDlg.getByRole('button', { name: /close/i }).last().click();
    await page.waitForTimeout(600);
  }

  // remove → CONFIRM dialog (not immediate)
  const tile = page.locator('.fsh-prod-image-tile').first();
  await tile.hover();
  await page.waitForTimeout(400);
  await tile.locator('[aria-label="Remove image"]').click();
  await page.waitForTimeout(800);
  const confirmDlg = page.locator('.mud-dialog').last();
  const confirmVisible = await confirmDlg.isVisible().catch(() => false);
  const confirmText = confirmVisible ? await confirmDlg.innerText().catch(() => '') : '';
  ok('P12', 'remove → confirmation dialog (detach)', confirmVisible && /detach/i.test(confirmText), confirmText.slice(0, 80).replace(/\n/g, ' | '));
  if (confirmVisible) await page.screenshot({ path: path.join(shots, 'image-remove-confirm.png') });
  const confirmBtn = confirmDlg.getByRole('button', { name: /remove|confirm|delete/i }).last();
  if (await confirmBtn.count() > 0) { await confirmBtn.click(); await page.waitForTimeout(1800); }
  const tilesFinal = await page.locator('.fsh-prod-image-tile').count();
  ok('P12', 'confirmed remove → tile gone', tilesFinal === tilesBefore, `${tilesAfter}→${tilesFinal}`);

  // ── files page: preview dialog rebuild ──
  await page.goto(`${BASE}/files`, { waitUntil: 'load' });
  await page.waitForTimeout(3000);
  await page.setInputFiles('#fileInput', { name: `qa-file-${ts}.txt`, mimeType: 'text/plain', buffer: Buffer.from('probe text file for preview') });
  await page.waitForTimeout(2500);
  const row = page.locator(`tr:has-text("qa-file-${ts}.txt")`).first();
  ok('P12', 'files: upload → row appears', await row.isVisible().catch(() => false));
  await row.click();
  await page.waitForTimeout(1200);
  const dlg = page.locator('.mud-dialog').last();
  const dlgText = await dlg.innerText().catch(() => '');
  const dlgCount = await page.locator('.mud-dialog').count();
  // single title bar: the empty-title bar renders only the X; the dialog header has the filename
  const titleBars = await page.locator('.mud-dialog-title').count();
  const titleText = titleBars > 0 ? (await page.locator('.mud-dialog-title').first().innerText().catch(() => '')).trim() : '';
  ok('P12', 'files preview: no duplicate title', !/Preview:/i.test(titleText), `titleBars=${titleBars} text="${titleText}"`);
  ok('P12', 'files preview: metadata card renders', /Visibility/i.test(dlgText) && /Owner type/i.test(dlgText) && /Uploaded by/i.test(dlgText));
  ok('P12', 'files preview: visibility switch present (uploader)', await dlg.locator('.mud-switch').count() > 0);
  ok('P12', 'files preview: Download + Close in footer', /Download/i.test(dlgText) && /Close/i.test(dlgText));
  ok('P12', 'files preview: Delete (two-step) present', /Delete/i.test(dlgText));
  await page.screenshot({ path: path.join(shots, 'file-preview-dialog.png') });

  // two-step delete: Delete → "Delete this file?" → Confirm delete
  await dlg.getByRole('button', { name: /^delete$/i }).first().click();
  await page.waitForTimeout(600);
  const step2 = await dlg.innerText().catch(() => '');
  ok('P12', 'files preview: inline confirm step appears', /Delete this file\?/i.test(step2) && /Confirm delete/i.test(step2));
  await page.screenshot({ path: path.join(shots, 'file-delete-step2.png') });
  await dlg.getByRole('button', { name: /confirm delete/i }).first().click();
  await page.waitForTimeout(1800);
  ok('P12', 'files: confirmed delete → row gone', !(await row.isVisible().catch(() => false)) && !(await page.locator('main').innerText().catch(() => '')).includes(`qa-file-${ts}`));

  ok('P12', 'GLOBAL no console/page errors', consoleErrors.length === 0, `${consoleErrors.slice(0, 2).join(' | ')} || bad: ${badResponses.slice(0, 3).join(' | ')}`);
} catch (err) {
  ok('P12', 'probe crashed', false, String(err).slice(0, 300));
} finally {
  await browser.close();
}

for (const r of results) console.log(' ', r.line);
const pass = results.filter((r) => r.pass).length;
console.log(`TOTAL ${pass} pass, ${results.length - pass} fail`);
