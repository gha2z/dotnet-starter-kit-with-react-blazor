// diag-d13.mjs — isolated files CRUD flow with step logging
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const BASE = 'http://localhost:5176';
const ts = Date.now().toString(36).slice(-6);

const b = await chromium.launch({ ignoreHTTPSErrors: true });
const ctx = await b.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
const p = await ctx.newPage();
p.on('console', (m) => { if (m.type() === 'error') console.log('[console]', m.text().slice(0, 120)); });

console.log('step: goto login');
await p.goto(`${BASE}/login`, { waitUntil: 'load', timeout: 45000 });
await p.waitForTimeout(5000);
await p.getByLabel('Tenant', { exact: false }).first().fill('acme');
await p.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
await p.getByLabel('Password', { exact: false }).first().fill('Password123!');
await p.getByLabel('Password', { exact: false }).first().blur();
await p.waitForTimeout(400);
console.log('step: click sign in');
await p.getByRole('button', { name: /sign in/i }).first().click();
await p.waitForURL((u) => !u.pathname.toLowerCase().includes('login'), { timeout: 45000 });
console.log('step: goto /files');
await p.goto(`${BASE}/files`, { waitUntil: 'load', timeout: 45000 });
await p.waitForTimeout(4000);

const zone = p.locator('.fsh-file-dropzone-inner').first();
console.log('step: wait for list to settle (loading spinner gone)');
await p.locator('.mud-table, .mud-paper.pa-8').first().waitFor({ state: 'visible', timeout: 25000 }).catch(() => {});
await p.waitForTimeout(800);
console.log('zone visible:', await zone.isVisible().catch((e) => `ERR ${String(e).slice(0, 60)}`));
console.log('step: setInputFiles directly (chooser interception is flaky in headless + async-handler chain)');
await p.setInputFiles('#fileInput', { name: `qa-${ts}.txt`, mimeType: 'text/plain', buffer: Buffer.from('probe upload') });
  console.log('step: setFiles done, wait for row');
  await p.waitForTimeout(3000);
  const seen = await p.locator('main').innerText().then((t) => t.includes(`qa-${ts}.txt`));
  console.log('uploaded row visible:', seen);
  const row = p.locator(`tr:has-text("qa-${ts}.txt")`).first();
  console.log('step: click row (preview)');
  await row.click({ timeout: 10000 }).catch((e) => console.log('row click err:', String(e).slice(0, 600)));
  await p.waitForTimeout(1200);
  const dlgCount = await p.locator('.mud-dialog').count();
  console.log('dialogs open:', dlgCount);
  const dlgText = dlgCount > 0 ? (await p.locator('.mud-dialog').last().innerText()).slice(0, 400).replace(/\n/g, ' | ') : '(none)';
  console.log('dialog text:', dlgText);
  await b.close();



