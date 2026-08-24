// probe-products-p2.mjs — P2 verification: searchable combobox filters, hover-reveal row
// actions, mobile card list, responsive brand/category columns (spec.md §2).
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const BASE = 'http://localhost:5176';
const OUT = path.join(import.meta.dirname, 'evidence', 'p2-products');
import fs from 'node:fs';
fs.mkdirSync(OUT, { recursive: true });

const results = [];
const ok = (name, pass, extra = '') => { results.push(pass); console.log(`  ${pass ? 'PASS' : 'FAIL'}: ${name}${extra ? ' — ' + extra : ''}`); };

async function login(page) {
  await page.goto(`${BASE}/login`, { waitUntil: 'load' });
  await page.waitForTimeout(1500);
  await page.getByLabel('Tenant', { exact: false }).first().waitFor({ state: 'visible', timeout: 20000 });
  await page.getByLabel('Tenant', { exact: false }).first().fill('acme');
  await page.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
  await page.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await page.getByLabel('Password', { exact: false }).first().blur().catch(() => {});
  await page.waitForTimeout(400);
  await page.getByRole('button', { name: /sign in/i }).first().click();
  await page.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 25000 });
  await page.waitForTimeout(1500);
}

const b = await chromium.launch({ ignoreHTTPSErrors: true });
try {
  // ---------- Desktop ----------
  const dCtx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const P = await dCtx.newPage();
  await login(P);
  await P.goto(`${BASE}/catalog/products`, { waitUntil: 'domcontentloaded' });
  await P.waitForSelector('.fsh-products-desktop .fsh-list-row', { timeout: 25000 });
  await P.waitForTimeout(800);

  // 1) Searchable combobox: type to narrow
  const brandBox = P.getByLabel('Brand', { exact: false }).first();
  await brandBox.click();
  await brandBox.fill('acme');
  await P.waitForTimeout(600);
  const options = await P.locator('.mud-popover .mud-list-item, .mud-autocomplete__list-item').allInnerTexts();
  ok('combobox narrows on typing', options.length > 0 && options.length <= 8, `options=[${options.slice(0, 3).join(' | ')}]`);
  await P.keyboard.press('Escape');

  // 2) Hover-reveal actions: hidden before hover, visible on hover
  const row = P.locator('.fsh-products-desktop .fsh-list-row').first();
  const actions = row.locator('.fsh-row-actions');
  const opBefore = await actions.evaluate((el) => getComputedStyle(el).opacity);
  await row.hover();
  await P.waitForTimeout(400);
  const opAfter = await actions.evaluate((el) => getComputedStyle(el).opacity);
  ok('row actions hidden until hover', parseFloat(opBefore) < 0.2, `opacity=${opBefore}`);
  ok('row actions visible on hover', parseFloat(opAfter) > 0.8, `opacity=${opAfter}`);
  await P.screenshot({ path: path.join(OUT, 'desktop-hover.png') });

  // 3) Mobile: cards visible, desktop table hidden, edit always visible
  const mCtx = await b.newContext({ viewport: { width: 390, height: 844 }, hasTouch: true, isMobile: true });
  const M = await mCtx.newPage();
  await login(M);
  await M.goto(`${BASE}/catalog/products`, { waitUntil: 'domcontentloaded' });
  await M.waitForSelector('.fsh-prod-card', { timeout: 25000 });
  await M.waitForTimeout(600);
  const cards = await M.locator('.fsh-prod-card').count();
  const desktopHidden = await M.locator('.fsh-products-desktop').isHidden();
  const mobileEditVisible = await M.locator('.fsh-prod-card [aria-label^="Edit"]').first().isVisible();
  ok('mobile: card list renders', cards > 0, `cards=${cards}`);
  ok('mobile: desktop table hidden', desktopHidden);
  ok('mobile: edit button always visible', mobileEditVisible);
  await M.screenshot({ path: path.join(OUT, 'mobile-cards.png') });

  // 4) Brands page mobile: slug/created columns hidden, slug under description
  await M.goto(`${BASE}/catalog/brands`, { waitUntil: 'domcontentloaded' });
  await M.waitForSelector('.fsh-brands-grid.fsh-list-row', { timeout: 25000 });
  await M.waitForTimeout(600);
  const slugHidden = await M.locator('.fsh-brands-grid.fsh-list-row .fsh-col-slug').first().isHidden();
  const createdHidden = await M.locator('.fsh-brands-grid.fsh-list-row .fsh-col-created').first().isHidden();
  const slugMobileVisible = await M.locator('.fsh-brands-grid.fsh-list-row .fsh-slug-mobile').first().isVisible();
  ok('brands mobile: slug column hidden', slugHidden);
  ok('brands mobile: created column hidden', createdHidden);
  ok('brands mobile: slug shown under description', slugMobileVisible);
  await M.screenshot({ path: path.join(OUT, 'mobile-brands.png') });

  // 5) Categories page mobile: same behavior
  await M.goto(`${BASE}/catalog/categories`, { waitUntil: 'domcontentloaded' });
  await M.waitForSelector('.fsh-categories-grid.fsh-list-row', { timeout: 25000 });
  await M.waitForTimeout(600);
  const catSlugHidden = await M.locator('.fsh-categories-grid.fsh-list-row .fsh-col-slug').first().isHidden();
  const catSlugMobile = await M.locator('.fsh-categories-grid.fsh-list-row .fsh-slug-mobile').first().isVisible();
  ok('categories mobile: slug column hidden', catSlugHidden);
  ok('categories mobile: slug under description', catSlugMobile);
  await M.screenshot({ path: path.join(OUT, 'mobile-categories.png') });
} catch (err) {
  ok('unhandled', false, String(err).slice(0, 200));
} finally {
  await b.close();
}
console.log(`TOTAL ${results.filter(Boolean).length} pass, ${results.filter((x) => !x).length} fail`);
process.exit(results.every(Boolean) ? 0 : 1);
