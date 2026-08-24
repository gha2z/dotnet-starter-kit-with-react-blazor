// probe-filters-p5.mjs — verify all fixed filter/pager paths re-render (P5).
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const BASE = 'http://localhost:5176';

let pass = 0, fail = 0;
const ok = (id, label, good, extra = '') => {
  console.log(`  ${good ? 'PASS' : 'FAIL'} [${id}] ${label}${extra ? ' — ' + extra : ''}`);
  good ? pass++ : fail++;
};

const b = await chromium.launch({ ignoreHTTPSErrors: true });
try {
  const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const P = await ctx.newPage();
  await P.goto(`${BASE}/login`, { waitUntil: 'load' });
  await P.waitForTimeout(1500);
  await P.getByLabel('Tenant', { exact: false }).first().fill('acme');
  await P.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
  await P.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await P.getByLabel('Password', { exact: false }).first().blur().catch(() => {});
  await P.waitForTimeout(400);
  await P.getByRole('button', { name: /sign in/i }).first().click();
  await P.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 25000 });

  const rows = () => P.locator('.fsh-tickets-grid.fsh-list-row').count();
  const skeletons = () => P.locator('.fsh-skeleton').count();

  // Tickets: every status + priority filter
  await P.goto(`${BASE}/tickets`, { waitUntil: 'load' });
  await P.waitForTimeout(4000);
  const baseRows = await rows();
  ok('P5', 'tickets initial render', baseRows > 0, `${baseRows} rows`);

  const statusPill = P.locator('.fsh-ticket-filter-pill').first();
  for (const [name, expect] of [['Open', 8], ['Closed', 0], ['Resolved', 2]]) {
    await statusPill.getByRole('button', { name, exact: true }).click();
    await P.waitForTimeout(2000);
    const n = await rows();
    const sk = await skeletons();
    ok('P5', `status=${name} re-renders`, sk === 0 && n === expect, `${n} rows, ${sk} skeletons`);
  }
  await statusPill.getByRole('button', { name: 'All', exact: true }).click();
  await P.waitForTimeout(2000);
  ok('P5', 'status=All restores', (await rows()) === baseRows, `${await rows()} rows`);

  const prioPill = P.locator('.fsh-ticket-filter-pill').nth(1);
  for (const [name, expect] of [['High', 2], ['Low', 2]]) {
    await prioPill.getByRole('button', { name, exact: true }).click();
    await P.waitForTimeout(2000);
    const n = await rows();
    ok('P5', `priority=${name} re-renders`, n === expect, `${n} rows`);
  }

  // Products: visibility pills
  await P.goto(`${BASE}/catalog/products`, { waitUntil: 'load' });
  await P.waitForTimeout(3500);
  const prodRows = () => P.locator('.fsh-products-desktop .fsh-list-row').count();
  const baseP = await prodRows();
  await P.locator('.fsh-prod-filter-pill').getByRole('button', { name: 'Hidden', exact: true }).click();
  await P.waitForTimeout(2000);
  ok('P5', 'products visibility=Hidden re-renders', (await P.locator('.fsh-skeleton').count()) === 0, `${await prodRows()} rows`);
  await P.locator('.fsh-prod-filter-pill').getByRole('button', { name: 'All', exact: true }).click();
  await P.waitForTimeout(2000);
  ok('P5', 'products visibility=All restores', (await P.locator('.fsh-skeleton').count()) === 0 && (await prodRows()) > 0, `${await prodRows()} rows`);

  // Brands: search + clear
  await P.goto(`${BASE}/catalog/brands`, { waitUntil: 'load' });
  await P.waitForTimeout(3000);
  await P.locator('.fsh-brands-grid.fsh-list-row').first().waitFor({ timeout: 15000 });
  await P.getByPlaceholder(/search/i).first().fill('Acme Goods');
  await P.waitForTimeout(1200);
  const filtered = await P.locator('.fsh-brands-grid.fsh-list-row').count();
  ok('P5', 'brands search filters', filtered >= 1 && filtered < 10, `${filtered} rows`);
  await P.getByRole('button', { name: /clear/i }).first().click().catch(async () => {
    await P.getByPlaceholder(/search/i).first().fill('');
  });
  await P.waitForTimeout(1500);
  ok('P5', 'brands clear re-renders', (await P.locator('.fsh-skeleton').count()) === 0, `${await P.locator('.fsh-brands-grid.fsh-list-row').count()} rows`);
} catch (err) {
  console.log('  FAIL [P5] unhandled —', String(err).slice(0, 200));
  fail++;
} finally {
  await b.close();
}
console.log(`TOTAL ${pass} pass, ${fail} fail`);
process.exit(fail === 0 ? 0 : 1);
