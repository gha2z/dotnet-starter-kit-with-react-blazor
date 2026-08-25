// shot-product-detail.mjs — side-by-side hero diff: React 5174 vs Blazor 5176 (desktop + mobile)
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

// First product from the API (shared DB — same id on both apps)
async function firstProductId() {
  const res = await fetch('https://localhost:7030/api/v1/identity/token/issue', {
    method: 'POST',
    headers: { 'content-type': 'application/json', tenant: 'acme' },
    body: JSON.stringify({ email: 'admin@acme.com', password: 'Password123!' }),
  });
  const tok = (await res.json()).accessToken;
  const list = await (await fetch('https://localhost:7030/api/v1/catalog/products?PageNumber=1&PageSize=1', {
    headers: { authorization: `Bearer ${tok}`, tenant: 'acme' },
  })).json();
  return list.items[0].id;
}

const pid = await firstProductId();
console.log('product:', pid);
const b = await chromium.launch({ ignoreHTTPSErrors: true });

async function shoot(base, tag) {
  const ctx = await b.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
  const p = await ctx.newPage();
  await p.goto(`${base}/login`, { waitUntil: 'load', timeout: 45000 });
  await p.waitForTimeout(4500);
  await p.getByLabel('Tenant', { exact: false }).first().fill('acme');
  await p.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
  await p.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await p.getByLabel('Password', { exact: false }).first().blur();
  await p.waitForTimeout(400);
  await p.getByRole('button', { name: /sign in/i }).first().click();
  await p.waitForURL((u) => !u.pathname.toLowerCase().includes('login'), { timeout: 45000 });
  await p.goto(`${base}/catalog/products/${pid}`, { waitUntil: 'load', timeout: 45000 });
  await p.waitForTimeout(5000);
  await p.screenshot({ path: `evidence/p11-detail/${tag}-hero.png` });
  // images section (scroll to it)
  await p.mouse.wheel(0, 500);
  await p.waitForTimeout(600);
  await p.screenshot({ path: `evidence/p11-detail/${tag}-images.png` });
  // mobile
  const m = await ctx.newPage();
  await m.setViewportSize({ width: 390, height: 844 });
  await m.goto(`${base}/catalog/products/${pid}`, { waitUntil: 'load', timeout: 45000 });
  await m.waitForTimeout(4000);
  await m.screenshot({ path: `evidence/p11-detail/${tag}-mobile.png`, fullPage: false });
  await ctx.close();
  console.log(`shots: ${tag} hero/images/mobile`);
}

await shoot('http://localhost:5174', 'react');
await shoot('http://localhost:5176', 'blazor');
await b.close();
