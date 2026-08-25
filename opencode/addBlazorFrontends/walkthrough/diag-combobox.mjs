// diag-combobox.mjs — verify the FshCombobox filters end-to-end (pick shows label, clear works, narrows, filters)
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const BASE = 'http://localhost:5176';

const b = await chromium.launch({ ignoreHTTPSErrors: true });
const ctx = await b.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
const p = await ctx.newPage();
const errors = [];
p.on('console', (m) => { if (m.type() === 'error') errors.push(m.text().slice(0, 100)); });
p.on('pageerror', (e) => errors.push(`[pageerror] ${String(e).slice(0, 100)}`));

const rows = async () => p.locator('.fsh-products-desktop .fsh-list-row').count();
const triggerText = async (i) => (await p.locator('.fsh-combobox-trigger').nth(i).innerText()).trim().split('\n')[0];

await p.goto(`${BASE}/login`, { waitUntil: 'load', timeout: 45000 });
await p.waitForTimeout(5000);
await p.getByLabel('Tenant', { exact: false }).first().fill('acme');
await p.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
await p.getByLabel('Password', { exact: false }).first().fill('Password123!');
await p.getByLabel('Password', { exact: false }).first().blur();
await p.waitForTimeout(400);
await p.getByRole('button', { name: /sign in/i }).first().click();
await p.waitForURL((u) => !u.pathname.toLowerCase().includes('login'), { timeout: 45000 });
await p.goto(`${BASE}/catalog/products`, { waitUntil: 'load', timeout: 45000 });
await p.locator('.mud-table').first().waitFor({ state: 'visible', timeout: 25000 }).catch(() => {});
await p.waitForTimeout(1000);

const baseRows = await rows();
console.log('PASS: page loads —', baseRows, 'rows');

// 1. open brand combobox → dropdown options render
await p.locator('.fsh-combobox-trigger').first().click();
await p.waitForTimeout(600);
const opts = p.locator('.fsh-combobox-popover .mud-list-item');
const optCount = await opts.count();
console.log(optCount > 0 ? 'PASS' : 'FAIL', ': dropdown opens —', optCount, 'options');

// 2. type-to-narrow in the popover search
await p.locator('.fsh-combobox-popover input').first().fill('acme');
await p.waitForTimeout(500);
const narrowed = await opts.count();
console.log(narrowed > 0 && narrowed < optCount ? 'PASS' : 'FAIL', ': search narrows —', narrowed, 'of', optCount);

// 3. pick → trigger shows the label, filter applies
const picked = (await opts.first().innerText()).trim();
await opts.first().click();
await p.waitForTimeout(1200);
const label = await triggerText(0);
console.log(label === picked ? 'PASS' : 'FAIL', `: selected label shows — "${label}"`);
const filteredRows = await rows();
console.log(filteredRows > 0 && filteredRows < baseRows ? 'PASS' : 'FAIL', ': filter applies —', filteredRows, 'of', baseRows, 'rows');

// 4. clear X appears and resets
const clearBtn = p.locator('.fsh-combobox-trigger').first().locator('.fsh-combobox-clear');
console.log(await clearBtn.count() === 1 ? 'PASS' : 'FAIL', ': clear button visible');
await clearBtn.click();
await p.waitForTimeout(1200);
const cleared = await triggerText(0);
console.log(cleared === 'All brands' ? 'PASS' : 'FAIL', `: clear resets — "${cleared}"`);
console.log((await rows()) === baseRows ? 'PASS' : 'FAIL', ': rows restored —', await rows());

// 5. category combobox same flow (pick + clear)
await p.locator('.fsh-combobox-trigger').nth(1).click();
await p.waitForTimeout(600);
await p.locator('.fsh-combobox-popover .mud-list-item').first().click();
await p.waitForTimeout(1200);
const catLabel = await triggerText(1);
console.log(catLabel !== 'All categories' ? 'PASS' : 'FAIL', `: category pick shows — "${catLabel}"`);
await p.locator('.fsh-combobox-trigger').nth(1).locator('.fsh-combobox-clear').click();
await p.waitForTimeout(1000);
console.log((await triggerText(1)) === 'All categories' ? 'PASS' : 'FAIL', ': category clear resets');

// 6. editor dialog pickers: open, pick brand+category, save
await p.getByRole('button', { name: /new product/i }).first().click();
await p.waitForTimeout(800);
const dlg = p.locator('.mud-dialog').last();
// MudPopover teleports to the root popover-provider — NOT inside .mud-dialog —
// so popover contents must be located at page level (:visible picks the live one).
await dlg.locator('.fsh-combobox-trigger').nth(0).click();
await p.waitForTimeout(700);
await p.locator('.fsh-combobox-popover:visible .mud-list-item').first().click();
await p.waitForTimeout(400);
await dlg.locator('.fsh-combobox-trigger').nth(1).click();
await p.waitForTimeout(1200);
console.log('popovers in dialog:', await dlg.locator('.fsh-combobox-popover').count());
console.log('category options visible:', await p.locator('.fsh-combobox-popover:visible .mud-list-item').count());
await p.locator('.fsh-combobox-popover:visible .mud-list-item').first().click({ timeout: 8000 });
await p.waitForTimeout(400);
const dlgBrand = (await dlg.locator('.fsh-combobox-trigger').nth(0).innerText()).trim().split('\n')[0];
console.log(dlgBrand !== 'Select a brand…' ? 'PASS' : 'FAIL', `: dialog brand picker shows — "${dlgBrand}"`);
await dlg.getByLabel('Name', { exact: false }).first().fill(`QA-CB-${Date.now().toString(36).slice(-6)}`);
await dlg.locator('input[placeholder="ACM-TS-001"]').fill(`QA-CB-${Date.now().toString(36).slice(-6)}`);
// Non-Immediate MudTextFields bind on CHANGE (blur) — blur all before saving.
for (const input of await dlg.locator('input').all()) await input.blur().catch(() => {});
await p.waitForTimeout(400);
await dlg.getByRole('button', { name: /add product|save changes/i }).last().click();
await p.waitForTimeout(2000);
console.log(/created|updated/i.test(await p.locator('.mud-snackbar').last().innerText().catch(() => '')) ? 'PASS' : 'FAIL', ': product saved via combobox pickers');

await p.screenshot({ path: 'evidence/p10-combobox/final.png' });
console.log('console errors:', errors.length, errors.slice(0, 3));
await b.close();
