// diag-brands-mobile.mjs — why does .fsh-brands-grid not render on mobile?
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const BASE = 'http://localhost:5176';

const b = await chromium.launch({ ignoreHTTPSErrors: true });
try {
  const ctx = await b.newContext({ viewport: { width: 390, height: 844 }, hasTouch: true, isMobile: true });
  const M = await ctx.newPage();
  M.on('console', (m) => { if (m.type() === 'error') console.log('[console]', m.text().slice(0, 160)); });
  await M.goto(`${BASE}/login`, { waitUntil: 'load' });
  await M.waitForTimeout(1500);
  await M.getByLabel('Tenant', { exact: false }).first().fill('acme');
  await M.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
  await M.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await M.getByLabel('Password', { exact: false }).first().blur().catch(() => {});
  await M.waitForTimeout(400);
  await M.getByRole('button', { name: /sign in/i }).first().click();
  await M.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 25000 });
  await M.goto(`${BASE}/catalog/brands`, { waitUntil: 'domcontentloaded' });
  await M.waitForTimeout(6000);
  const body = await M.evaluate(() => document.body.innerText.slice(0, 500));
  console.log('--- BODY ---');
  console.log(body);
  console.log('rows:', await M.locator('.fsh-brands-grid .fsh-list-row').count());
  console.log('head:', await M.locator('.fsh-brands-grid.fsh-list-head').count());
  console.log('skeletons:', await M.locator('.fsh-skeleton').count());
} catch (err) {
  console.log('FAIL:', String(err).slice(0, 200));
} finally {
  await b.close();
}
