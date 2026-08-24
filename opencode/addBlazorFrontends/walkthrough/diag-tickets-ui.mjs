// diag-tickets-ui.mjs — click the Open filter in the real UI and observe.
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
  const reqs = [];
  P.on('request', (r) => { if (r.url().includes('/api/v1/tickets')) reqs.push(r.url()); });
  P.on('response', async (r) => { if (r.url().includes('/api/v1/tickets')) console.log('[resp]', r.status(), r.url().slice(0, 140)); });
  P.on('console', (m) => { if (m.type() === 'error') console.log('[console]', m.text().slice(0, 160)); });

  await P.goto(`${BASE}/login`, { waitUntil: 'load' });
  await P.waitForTimeout(1500);
  await P.getByLabel('Tenant', { exact: false }).first().fill('acme');
  await P.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
  await P.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await P.getByLabel('Password', { exact: false }).first().blur().catch(() => {});
  await P.waitForTimeout(400);
  await P.getByRole('button', { name: /sign in/i }).first().click();
  await P.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 25000 });

  await P.goto(`${BASE}/tickets`, { waitUntil: 'load' });
  await P.waitForTimeout(4000);
  console.log('--- initial rows:', await P.locator('.fsh-tickets-grid.fsh-list-row').count());

  // Click the "Open" status filter button
  const openBtn = P.locator('.fsh-ticket-filter-pill').first().getByRole('button', { name: 'Open', exact: true });
  console.log('open btn count:', await openBtn.count());
  await openBtn.click();
  await P.waitForTimeout(3500);
  console.log('--- after Open filter rows:', await P.locator('.fsh-tickets-grid.fsh-list-row').count());
  console.log('--- skeletons:', await P.locator('.fsh-skeleton').count());
  const body = await P.evaluate(() => document.body.innerText.slice(0, 900));
  console.log('--- BODY ---');
  console.log(body);
  console.log('--- ticket API requests:', reqs.length);
} catch (err) {
  console.log('FAIL:', String(err).slice(0, 250));
} finally {
  await b.close();
}
