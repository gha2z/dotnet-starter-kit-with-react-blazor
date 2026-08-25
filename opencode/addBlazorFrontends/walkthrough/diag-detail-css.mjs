// diag-detail-css.mjs — is fsh-detail-stat styled on the live page?
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const res = await fetch('http://localhost:5176/_content/FSH.BlazorShared/css/fsh.css');
const css = await res.text();
console.log('served css status:', res.status, 'len:', css.length, 'has fsh-detail-stat:', css.includes('fsh-detail-stat'));

const pid = '01a03958-4a3f-7a1b-8312-e4ba70588226';
const b = await chromium.launch({ ignoreHTTPSErrors: true });
const ctx = await b.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
const p = await ctx.newPage();
await p.goto(`http://localhost:5176/login`, { waitUntil: 'load', timeout: 45000 });
await p.waitForTimeout(4500);
await p.getByLabel('Tenant', { exact: false }).first().fill('acme');
await p.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
await p.getByLabel('Password', { exact: false }).first().fill('Password123!');
await p.getByLabel('Password', { exact: false }).first().blur();
await p.waitForTimeout(400);
await p.getByRole('button', { name: /sign in/i }).first().click();
await p.waitForURL((u) => !u.pathname.toLowerCase().includes('login'), { timeout: 45000 });
await p.goto(`http://localhost:5176/catalog/products/${pid}`, { waitUntil: 'load', timeout: 45000 });
await p.waitForTimeout(9000);
await p.locator('.fsh-detail-stat').first().waitFor({ state: 'visible', timeout: 20000 }).catch(() => {});

const stat = p.locator('.fsh-detail-stat').first();
console.log('stat count:', await p.locator('.fsh-detail-stat').count());
if (await stat.count() > 0) {
  const cs = await stat.evaluate((el) => {
    const s = getComputedStyle(el);
    return { display: s.display, height: s.height, alignItems: s.alignItems, borderRadius: s.borderRadius };
  });
  console.log('computed .fsh-detail-stat:', JSON.stringify(cs));
}

// Images card — dump structure to find the mystery BROWSE FILES button
const cards = p.locator('.fsh-section-card');
const n = await cards.count();
for (let i = 0; i < n; i++) {
  const txt = await cards.nth(i).innerText();
  if (/images/i.test(txt)) {
    const html = await cards.nth(i).evaluate((el) => el.innerHTML);
    const buttons = await cards.nth(i).locator('button').all();
    console.log('images card buttons:');
    for (const btn of buttons) {
      console.log('  -', JSON.stringify((await btn.innerText().catch(() => '')).trim()), (await btn.getAttribute('class') ?? '').slice(0, 60));
    }
    const hasBrowse = html.toUpperCase().includes('BROWSE');
    console.log('html contains BROWSE:', hasBrowse);
    const browseBtn = cards.nth(i).locator('button', { hasText: /browse/i }).first();
    if (await browseBtn.count() > 0) {
      console.log('browse btn html:', (await browseBtn.evaluate((el) => el.outerHTML)).slice(0, 400));
      console.log('browse parent html:', (await browseBtn.evaluate((el) => el.parentElement?.outerHTML ?? '')).slice(0, 500));
    }
    break;
  }
}
await b.close();
