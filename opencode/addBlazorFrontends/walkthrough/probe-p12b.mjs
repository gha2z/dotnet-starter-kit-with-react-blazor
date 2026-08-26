// probe-p12b.mjs — P12.3 behavioral matrix: price/stock dialogs, refresh spin, meta links,
// ID chips, inventory tone, not-found panel.
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
const API = 'https://localhost:7030';
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

const results = [];
const ok = (id, name, pass, detail = '') =>
  results.push({ pass: !!pass, line: `${pass ? 'PASS' : 'FAIL'} [${id}] ${name}${detail ? ' — ' + detail : ''}` });

const consoleErrors = [];
const browser = await chromium.launch({ ignoreHTTPSErrors: true });
try {
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  page.on('console', (m) => { if (m.type() === 'error' && !/mono_download|favicon|instantiate_wasm_module/i.test(m.text()) && !/404 \(/i.test(m.text())) consoleErrors.push(m.text()); });
  page.on('pageerror', (e) => consoleErrors.push(`[pageerror] ${e.message}`));

  // login (tenant field required!)
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

  const tokRes = await page.request.post(`${API}/api/v1/identity/token/issue`, {
    headers: { tenant: 'acme', 'content-type': 'application/json' },
    data: { email: 'admin@acme.com', password: 'Password123!' },
  });
  const tok = (await tokRes.json()).accessToken;
  const H = { authorization: `Bearer ${tok}`, tenant: 'acme' };
  const listRes = await page.request.get(`${API}/api/v1/catalog/products?PageNumber=1&PageSize=10`, { headers: H });
  const products = (await listRes.json()).items ?? [];
  const product = products[0];
  const originalPrice = product.price.amount;
  ok('P12B', 'product available', !!product, product?.sku);

  await page.goto(`${BASE}/catalog/products/${product.id}`, { waitUntil: 'load' });
  await page.waitForSelector('.fsh-detail-stat', { timeout: 30000 });
  await page.waitForTimeout(1200);

  // ── identifiers: ID chips ──
  const idsText = (await page.locator('main').innerText().catch(() => ''));
  ok('P12B', 'identifiers show Product/Brand/Category IDs', /Product ID/i.test(idsText) && /Brand ID/i.test(idsText) && /Category ID/i.test(idsText));

  // ── meta links ──
  const brandLink = page.locator('.fsh-detail-meta-link').first();
  const brandHref = await brandLink.getAttribute('href').catch(() => null);
  ok('P12B', 'meta brand is a filtered-list link', !!brandHref && /catalog\/products\?brand=/.test(brandHref), brandHref ?? 'none');

  // ── pricing caption ──
  ok('P12B', 'pricing caption "Listed price"', /Listed price/i.test(idsText));

  // ── price dialog: was→becomes live preview ──
  await page.locator('.fsh-detail-stat.primary').click();
  await page.waitForTimeout(900);
  let dlg = page.locator('.mud-dialog').last();
  const wasVisible = /was/i.test(await dlg.innerText().catch(() => '')) && /becomes/i.test(await dlg.innerText().catch(() => ''));
  ok('P12B', 'price dialog: was→becomes preview', wasVisible);
  const amount = dlg.locator('input').first();
  await amount.fill(String(originalPrice + 5));
  await page.waitForTimeout(600);
  const dlgText2 = await dlg.innerText().catch(() => '');
  const becomesUp = await dlg.locator('.fsh-was-becomes-value.is-up').count() > 0;
  const showsNew = dlgText2.includes(String(originalPrice + 5));
  ok('P12B', 'price dialog: live becomes + green delta', becomesUp && showsNew, `becomesUp=${becomesUp}`);
  await page.screenshot({ path: path.join(shots, 'price-dialog-preview.png') });

  // free-text currency: type a 3-letter code not in any preset list
  const currencyInput = dlg.locator('input').nth(1);
  await currencyInput.fill('AED');
  await page.waitForTimeout(500);
  const curVal = await currencyInput.inputValue();
  ok('P12B', 'price dialog: free-text currency (AED accepted)', curVal === 'AED', curVal);
  // lowercase auto-uppercases
  await currencyInput.fill('eur');
  await page.waitForTimeout(400);
  const curVal2 = await currencyInput.inputValue();
  ok('P12B', 'price dialog: currency auto-uppercased', curVal2 === 'EUR', curVal2);
  // 4th char clamps to 3
  await currencyInput.fill('USDX');
  await page.waitForTimeout(400);
  ok('P12B', 'price dialog: currency clamps to 3 chars', (await currencyInput.inputValue()) === 'USD');
  // submit the change (restore-safe: set back to original)
  await amount.fill(String(originalPrice));
  await page.waitForTimeout(400);
  await dlg.getByRole('button', { name: /change price/i }).last().click();
  await page.waitForTimeout(2000);

  // ── stock dialog: guard + tones ──
  await page.locator('.fsh-detail-stat').nth(1).click();
  await page.waitForTimeout(900);
  dlg = page.locator('.mud-dialog').last();
  const stockDlgText = await dlg.innerText().catch(() => '');
  ok('P12B', 'stock dialog: domain-event copy', /ProductStockAdjusted/i.test(stockDlgText));
  // delta starts at 0 → submit disabled
  const submitDisabled = await dlg.getByRole('button', { name: /adjust stock/i }).last().isDisabled().catch(() => true);
  ok('P12B', 'stock dialog: delta-0 init → submit disabled', submitDisabled);
  // negative delta → guard banner
  const deltaInput = dlg.locator('.fsh-delta-input input');
  await deltaInput.fill('-999');
  await page.waitForTimeout(600);
  const guardVisible = await dlg.locator('.fsh-negative-guard').isVisible().catch(() => false);
  const guardText = guardVisible ? await dlg.locator('.fsh-negative-guard').innerText() : '';
  ok('P12B', 'stock dialog: negative guard banner', guardVisible && /cannot go negative/i.test(guardText), guardText.slice(0, 60).replace(/\n/g, ' '));
  const submitStillDisabled = await dlg.getByRole('button', { name: /adjust stock/i }).last().isDisabled().catch(() => true);
  ok('P12B', 'stock dialog: submit blocked on negative', submitStillDisabled);
  const becomesRed = await dlg.locator('.fsh-was-becomes-value.is-down').count() > 0;
  ok('P12B', 'stock dialog: becomes red on negative', becomesRed);
  await page.screenshot({ path: path.join(shots, 'stock-dialog-guard.png') });
  // valid positive delta → submit enabled
  await deltaInput.fill('2');
  await page.waitForTimeout(500);
  const submitEnabled = !(await dlg.getByRole('button', { name: /adjust stock/i }).last().isDisabled().catch(() => true));
  const becomesGreen = await dlg.locator('.fsh-was-becomes-value.is-up').count() > 0;
  ok('P12B', 'stock dialog: valid delta → enabled + green', submitEnabled && becomesGreen);
  await dlg.getByRole('button', { name: /adjust stock/i }).last().click();
  await page.waitForTimeout(2000);
  // revert the +2 (PATCH, per CatalogService.AdjustProductStockAsync)
  const revert = await page.request.patch(`${API}/api/v1/catalog/products/${product.id}/stock`, { headers: H, data: { delta: -2 } });
  ok('P12B', 'stock adjusted +2 then reverted', revert.status() === 200, `revert=${revert.status()}`);

  // ── not-found panel ──
  await page.goto(`${BASE}/catalog/products/00000000-0000-0000-0000-000000000000`, { waitUntil: 'load' });
  await page.waitForTimeout(3500);
  const nfText = (await page.locator('main').innerText().catch(() => ''));
  ok('P12B', 'not-found panel (React copy + back)', /Product not found/i.test(nfText) && /may have been deleted/i.test(nfText) && /Back to products/i.test(nfText));
  await page.screenshot({ path: path.join(shots, 'not-found-panel.png') });

  ok('P12B', 'GLOBAL no console/page errors', consoleErrors.length === 0, consoleErrors.slice(0, 2).join(' | '));
} catch (err) {
  ok('P12B', 'probe crashed', false, String(err).slice(0, 300));
} finally {
  await browser.close();
}

for (const r of results) console.log(' ', r.line);
const pass = results.filter((r) => r.pass).length;
console.log(`TOTAL ${pass} pass, ${results.length - pass} fail`);
