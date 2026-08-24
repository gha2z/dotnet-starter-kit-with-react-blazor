// shot-product-detail.mjs — P3.1 visual evidence: hero card desktop + mobile.
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const BASE = 'http://localhost:5176';
const API = 'https://localhost:7030';
const OUT = path.join(import.meta.dirname, 'evidence', 'p3-detail');
import fs from 'node:fs';
fs.mkdirSync(OUT, { recursive: true });

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
  const res = await fetch(`${API}/api/v1/identity/token/issue`, { method: 'POST', headers: { 'content-type': 'application/json', tenant: 'acme' }, body: JSON.stringify({ email: 'admin@acme.com', password: 'Password123!' }) });
  const tok = (await res.json()).accessToken;
  const prods = await (await fetch(`${API}/api/v1/catalog/products?PageNumber=1&PageSize=5`, { headers: { authorization: `Bearer ${tok}` } })).json();
  const prod = prods.items.find((p) => !/^qa/i.test(p.name)) ?? prods.items[0];
  console.log('product:', prod.id, prod.name);

  const d = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const P = await d.newPage();
  await login(P);
  await P.goto(`${BASE}/catalog/products/${prod.id}`, { waitUntil: 'domcontentloaded' });
  await P.waitForSelector('.fsh-detail-card', { timeout: 25000 });
  await P.waitForTimeout(1200);
  await P.screenshot({ path: path.join(OUT, 'hero-desktop.png') });

  const m = await b.newContext({ viewport: { width: 390, height: 844 }, hasTouch: true, isMobile: true });
  const M = await m.newPage();
  await login(M);
  await M.goto(`${BASE}/catalog/products/${prod.id}`, { waitUntil: 'domcontentloaded' });
  await M.waitForSelector('.fsh-detail-card', { timeout: 25000 });
  await M.waitForTimeout(1200);
  await M.screenshot({ path: path.join(OUT, 'hero-mobile.png') });
  console.log('shots saved');
} catch (err) {
  console.log('FAIL:', String(err).slice(0, 200));
} finally {
  await b.close();
}
