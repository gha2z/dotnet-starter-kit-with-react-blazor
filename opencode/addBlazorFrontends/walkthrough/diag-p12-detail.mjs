import { pathToFileURL } from 'node:url';
import path from 'node:path';
import fs from 'node:fs';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const CLIENTS = path.resolve(here, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const shots = path.join(here, 'evidence', 'p12-behaviors');
fs.mkdirSync(shots, { recursive: true });
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

const browser = await chromium.launch({ ignoreHTTPSErrors: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
const errors = [];
const responses = [];
page.on('response', (r) => { if (r.url().includes('/api/')) responses.push(`${r.status()} ${r.url().replace('https://localhost:7030', '')}`); });
page.on('console', (m) => { if (m.type() === 'error' && !/mono_download|favicon|instantiate_wasm_module/i.test(m.text())) errors.push(m.text()); });
page.on('pageerror', (e) => errors.push(`[pageerror] ${e.message}`));

await page.goto('http://localhost:5176/login', { waitUntil: 'load' });
await page.waitForTimeout(2500);
await page.getByLabel(/email/i).first().fill('admin@acme.com');
await page.getByLabel(/email/i).first().blur();
await page.getByLabel(/password/i).first().fill('Password123!');
await page.getByLabel(/password/i).first().blur();
await page.waitForTimeout(400);
await page.getByRole('button', { name: /sign in/i }).first().click();
await page.waitForTimeout(6000);
console.log('url after login attempt:', page.url());
console.log('api responses:', responses.slice(0, 8).join(' | '));
console.log('snackbar/body:', (await page.locator('body').innerText().catch(() => '')).slice(0, 200).replace(/\n+/g, ' | '));

const res = await page.request.post('https://localhost:7030/api/v1/identity/token/issue', {
  headers: { tenant: 'acme', 'content-type': 'application/json' },
  data: { email: 'admin@acme.com', password: 'Password123!' },
});
console.log('token status:', res.status());
const tok = (await res.json()).accessToken;
const listRes = await page.request.get('https://localhost:7030/api/v1/catalog/products?PageNumber=1&PageSize=3', { headers: { authorization: `Bearer ${tok}`, tenant: 'acme' } });
console.log('list status:', listRes.status());
const items = (await listRes.json()).items ?? [];
console.log('first:', items[0]?.id, items[0]?.sku);

await page.goto(`http://localhost:5176/catalog/products/${items[0].id}`, { waitUntil: 'load' });
await page.waitForTimeout(8000);
const statCount = await page.locator('.fsh-detail-stat').count();
console.log('stat count:', statCount);
const bodyText = (await page.locator('main').innerText().catch(() => '')).slice(0, 400).replace(/\n+/g, ' | ');
console.log('body:', bodyText);
console.log('errors:', errors.slice(0, 4).join(' || ').slice(0, 400));
await page.screenshot({ path: path.join(shots, 'diag-detail.png') });
await browser.close();
