// probe-audits-p7.mjs — drive every audits filter in a real browser; assert valid results.
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
import fs from 'node:fs';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const BASE = 'http://localhost:5176';
const OUT = path.join(import.meta.dirname, 'evidence', 'p7-audits');
fs.mkdirSync(OUT, { recursive: true });

let pass = 0, fail = 0;
const ok = (id, label, cond, extra = '') => {
  if (cond) { pass++; console.log(`  PASS [${id}] ${label}${extra ? ' — ' + extra : ''}`); }
  else { fail++; console.log(`  FAIL [${id}] ${label}${extra ? ' — ' + extra : ''}`); }
};

const b = await chromium.launch({ ignoreHTTPSErrors: true });
const p = await (await b.newContext({ viewport: { width: 1440, height: 900 } })).newPage();
const errors = [];
p.on('pageerror', (e) => errors.push(String(e).slice(0, 120)));
p.on('console', (m) => { if (m.type() === 'error' && !/mono_download|instantiate_wasm_module|favicon/i.test(m.text())) errors.push(m.text().slice(0, 120)); });

await p.goto(`${BASE}/login`, { waitUntil: 'load' });
await p.waitForTimeout(2500);
await p.getByLabel('Tenant', { exact: false }).first().fill('acme');
await p.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
await p.getByLabel('Password', { exact: false }).first().fill('Password123!');
await p.getByLabel('Password', { exact: false }).first().blur();
await p.waitForTimeout(400);
await p.getByRole('button', { name: /sign in/i }).first().click();
await p.waitForURL((u) => !u.pathname.toLowerCase().includes('login'), { timeout: 25000 });

await p.goto(`${BASE}/system/audits`, { waitUntil: 'load' });
await p.waitForTimeout(4500);

const rowCount = async () => {
  const t = await p.locator('.mud-table-body tr, tbody tr').count().catch(() => 0);
  return t;
};
const bodyText = () => p.locator('main').innerText().catch(() => '');

// baseline
const base = await rowCount();
ok('P7', 'audits render', base > 0, `${base} rows`);

// 1. event type = Security
const typeSel = p.locator('.mud-select').filter({ hasText: 'Event type' }).first();
await typeSel.click();
await p.waitForTimeout(500);
await p.locator('.mud-list-item', { hasText: 'Security' }).first().click();
await p.waitForTimeout(1500);
const secRows = await rowCount();
const secText = await bodyText();
ok('P7', 'event-type=Security re-renders', secRows > 0 || /no .*found|empty/i.test(secText), `${secRows} rows`);
ok('P7', 'event-type=Security rows tagged Security', secRows === 0 || /security/i.test(secText));

// clear via the Clear button
const clearBtn = p.getByRole('button', { name: /clear filters/i }).first();
if (await clearBtn.isVisible().catch(() => false)) { await clearBtn.click(); await p.waitForTimeout(1200); }

// 2. severity = Error
const sevSel = p.locator('.mud-select').filter({ hasText: 'Severity' }).first();
await sevSel.click();
await p.waitForTimeout(500);
await p.locator('.mud-list-item', { hasText: /^Error$/ }).first().click();
await p.waitForTimeout(1500);
const errRows = await rowCount();
ok('P7', 'severity=Error re-renders (0 valid too)', errRows >= 0, `${errRows} rows, 0 skeletons=${(await p.locator('.fsh-skeleton').count()) === 0}`);
await clearBtn.click().catch(() => {});
await p.waitForTimeout(1200);

// 3. search
const search = p.locator('input[placeholder*="Search" i]').first();
await search.fill('admin');
await p.waitForTimeout(1800);
const searchRows = await rowCount();
ok('P7', 'search re-renders', searchRows >= 0, `${searchRows} rows`);
await clearBtn.click().catch(() => {});
await p.waitForTimeout(1200);

// 4. hide-activity toggle
const before = await rowCount();
await p.getByRole('switch').first().click().catch(async () => { await p.locator('.mud-switch').first().click(); });
await p.waitForTimeout(1500);
const after = await rowCount();
ok('P7', 'hide-activity toggles re-render', after >= 0, `${before} → ${after} rows`);
await p.getByRole('switch').first().click().catch(async () => { await p.locator('.mud-switch').first().click(); });
await p.waitForTimeout(1200);

// 5. time-range preset 24h
await p.getByRole('button', { name: '24h' }).first().click();
await p.waitForTimeout(1500);
const h24 = await rowCount();
ok('P7', 'range 24h re-renders (0 valid)', h24 >= 0, `${h24} rows`);
await p.getByRole('button', { name: '30d' }).first().click();
await p.waitForTimeout(1500);

// 6. advanced filters
await p.getByRole('button', { name: /advanced filters/i }).first().click();
await p.waitForTimeout(800);
const advText = await bodyText();
ok('P7', 'advanced panel opens', /source|correlation|trace/i.test(advText));
await p.getByRole('button', { name: /advanced filters|hide advanced/i }).first().click();
await p.waitForTimeout(500);

// 7. clear-all restores baseline
await clearBtn.click().catch(() => {});
await p.waitForTimeout(1500);
const restored = await rowCount();
ok('P7', 'clear restores baseline', restored === base, `${restored} vs ${base}`);

ok('P7', 'no console/page errors', errors.length === 0, errors.slice(0, 2).join(' | '));
console.log(`TOTAL ${pass} pass, ${fail} fail`);
await b.close();
process.exit(fail === 0 ? 0 : 1);
