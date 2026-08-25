// probe-dialogs-p6.mjs — visual verification of the standardized CRUD dialog chrome.
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const BASE = 'http://localhost:5176';
const OUT = path.join(import.meta.dirname, 'evidence', 'p6-dialogs');
import fs from 'node:fs';
fs.mkdirSync(OUT, { recursive: true });

const b = await chromium.launch({ ignoreHTTPSErrors: true });
const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
const p = await ctx.newPage();
const errors = [];
p.on('pageerror', (e) => errors.push(String(e).slice(0, 120)));
p.on('console', (m) => { if (m.type() === 'error' && /failed|exception/i.test(m.text()) && !/mono_download|instantiate_wasm_module|favicon/i.test(m.text())) errors.push(m.text().slice(0, 120)); });

// login
await p.goto(`${BASE}/login`, { waitUntil: 'load' });
await p.waitForTimeout(2500);
await p.getByLabel('Tenant', { exact: false }).first().fill('acme');
await p.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
await p.getByLabel('Password', { exact: false }).first().fill('Password123!');
await p.getByLabel('Password', { exact: false }).first().blur();
await p.waitForTimeout(400);
await p.getByRole('button', { name: /sign in/i }).first().click();
await p.waitForURL((u) => !u.pathname.toLowerCase().includes('login'), { timeout: 25000 });
await p.waitForTimeout(1500);

const shot = (name) => p.screenshot({ path: path.join(OUT, name) });

// 1. product editor (create)
await p.goto(`${BASE}/catalog/products`, { waitUntil: 'load' });
await p.waitForTimeout(3500);
await p.getByRole('button', { name: /new product/i }).first().click();
await p.locator('.mud-dialog input').first().waitFor({ state: 'visible', timeout: 10000 });
await p.waitForTimeout(400);
await shot('editor-product.png');
await p.keyboard.press('Escape');
await p.waitForTimeout(500);

// 2. ticket dialog
await p.goto(`${BASE}/tickets`, { waitUntil: 'load' });
await p.waitForTimeout(3500);
await p.getByRole('button', { name: /new ticket/i }).first().click();
await p.locator('.mud-dialog input').first().waitFor({ state: 'visible', timeout: 10000 });
await p.waitForTimeout(400);
await shot('dialog-ticket.png');
const cancel = p.locator('.mud-dialog').last().getByRole('button', { name: /cancel/i }).first();
const cancelOutlined = await cancel.evaluate((el) => getComputedStyle(el).borderTopWidth).catch(() => '0px');
console.log(`ticket Cancel border-width: ${cancelOutlined} (1px = outlined ✓)`);
await p.keyboard.press('Escape');
await p.waitForTimeout(500);

// 3. delete confirm (hover a product row → trash)
await p.goto(`${BASE}/catalog/products`, { waitUntil: 'load' });
await p.waitForTimeout(3500);
const del = p.locator('[aria-label^="Delete "]').first();
if (await del.count() > 0) {
  await del.click();
  await p.locator('.mud-dialog').last().waitFor({ state: 'visible', timeout: 8000 });
  await p.waitForTimeout(400);
  await shot('confirm-delete.png');
  await p.locator('.mud-dialog').last().getByRole('button', { name: /cancel/i }).first().click().catch(() => {});
}

console.log(`console errors: ${errors.length}`);
errors.slice(0, 4).forEach((e) => console.log(`  [err] ${e}`));
await b.close();
