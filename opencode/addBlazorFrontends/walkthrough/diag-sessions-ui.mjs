// diag-sessions-ui.mjs — why does /system/sessions render 0 rows?
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const BASE = 'http://localhost:5176';

const b = await chromium.launch({ ignoreHTTPSErrors: true });
try {
  const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const P = await ctx.newPage();
  P.on('console', (m) => { if (m.type() === 'error') console.log('[console]', m.text().slice(0, 180)); });
  P.on('pageerror', (e) => console.log('[pageerror]', String(e).slice(0, 180)));
  await P.goto(`${BASE}/login`, { waitUntil: 'load' });
  await P.waitForTimeout(1500);
  await P.getByLabel('Tenant', { exact: false }).first().fill('acme');
  await P.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
  await P.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await P.getByLabel('Password', { exact: false }).first().blur().catch(() => {});
  await P.waitForTimeout(400);
  await P.getByRole('button', { name: /sign in/i }).first().click();
  await P.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 25000 });
  await P.goto(`${BASE}/system/sessions`, { waitUntil: 'load' });
  await P.waitForTimeout(5000);
  console.log('url:', P.url());
  console.log('tr count:', await P.locator('tr').count());
  const body = await P.evaluate(() => document.body.innerText.slice(0, 600));
  console.log('--- BODY ---');
  console.log(body);
} catch (err) {
  console.log('FAIL:', String(err).slice(0, 200));
} finally {
  await b.close();
}
