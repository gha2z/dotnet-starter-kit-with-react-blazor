// probe-actions-admin.mjs — C2b: real-CRUD walkthrough of admin-blazor screens.
// Same discipline as probe-actions.mjs: real clicks, toast/DOM assertions,
// failure screenshots, console-error capture. QA-* names, best-effort cleanup.
// Usage: node probe-actions-admin.mjs [screenId ...]
import { pathToFileURL } from 'node:url';
import path from 'node:path';
import fs from 'node:fs';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const BASE = 'http://localhost:5175';
const OUT = path.join(import.meta.dirname, 'evidence', 'actions-admin');
fs.mkdirSync(OUT, { recursive: true });
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const ts = Date.now().toString(36);
let pass = 0, fail = 0;
const results = [];
const consoleErrors = [];

function ok(screen, name, cond, extra = '') {
  if (cond) { pass++; results.push(`  PASS [${screen}] ${name}${extra ? ` — ${extra}` : ''}`); }
  else { fail++; results.push(`  FAIL [${screen}] ${name}${extra ? ` — ${extra}` : ''}`); }
}

const dlg = (page) => page.locator('.mud-dialog').last();
async function toast(page) { return page.locator('.mud-snackbar').last().innerText().catch(() => ''); }
async function openCreate(page, label) {
  await page.getByRole('button', { name: label, exact: false }).first().click();
  await dlg(page).locator('input').first().waitFor({ state: 'visible', timeout: 8000 });
  await page.waitForTimeout(600);
}
async function saveDialog(page, saveLabel = 'Save') {
  for (const el of await dlg(page).locator('input, textarea').all()) {
    await el.blur().catch(() => {});
  }
  await page.waitForTimeout(400);
  await dlg(page).getByRole('button', { name: saveLabel, exact: false }).last().click().catch(async () => {
    // Overlay/interception fallback — dispatch a real click via JS
    await dlg(page).getByRole('button', { name: saveLabel, exact: false }).last().evaluate((el) => el.click()).catch(() => {});
  });
  await page.waitForTimeout(1800);
  const stillOpen = await dlg(page).locator('input').first().isVisible().catch(() => false);
  if (stillOpen) {
    await dlg(page).getByRole('button', { name: saveLabel, exact: false }).last().evaluate((el) => el.click()).catch(() => {});
    await page.waitForTimeout(1800);
  }
  await dlg(page).locator('input').first().waitFor({ state: 'detached', timeout: 8000 }).catch(() => {});
}
async function mainText(page) { return page.locator('main').innerText().catch(() => ''); }
async function rowVisible(page, name) { return (await mainText(page)).includes(name); }

const actions = {
  async A04(page) { // Tenants — create
    const name = `QA-Tenant-${ts}`;
    await page.goto(`${BASE}/tenants`, { waitUntil: 'load' });
    await page.waitForTimeout(2500);
    await openCreate(page, /new tenant/i);
    await dlg(page).locator('#ct-name').fill(name);
    // Billing plan select (required) — MudSelect id doesn't land on the input
    await dlg(page).locator('.mud-select-input').first().click();
    await page.locator('.mud-popover .mud-list-item').first().waitFor({ state: 'visible', timeout: 6000 }).catch(() => {});
    await page.locator('.mud-popover .mud-list-item').first().click().catch(() => {});
    await page.waitForTimeout(400);
    await dlg(page).locator('#ct-adminEmail').fill(`admin@${name.toLowerCase()}.com`);
    await dlg(page).locator('#ct-adminPassword').fill('Password123!');
    await saveDialog(page, /create tenant|create|save/i);
    ok('A04', 'tenant created', await rowVisible(page, name), await toast(page));
  },

  async A06(page) { // Users — register
    const uname = `qaadmin${ts}`;
    await page.goto(`${BASE}/users`, { waitUntil: 'load' });
    await page.waitForTimeout(2500);
    await openCreate(page, /new user|register/i);
    await dlg(page).getByLabel('First name', { exact: false }).fill('QA');
    await dlg(page).getByLabel('Last name', { exact: false }).fill('AdminProbe');
    await dlg(page).getByLabel('Username', { exact: false }).fill(uname);
    await dlg(page).getByLabel('Email', { exact: false }).first().fill(`${uname}@root.com`);
    await dlg(page).getByLabel('Password', { exact: false }).first().fill('Password123!');
    await dlg(page).getByLabel('Confirm password', { exact: false }).fill('Password123!');
    await saveDialog(page, /register|create|save/i);
    ok('A06', 'user registered', await rowVisible(page, uname), await toast(page));
  },

  async A08(page) { // Roles — create
    const name = `QA-ARole-${ts}`;
    await page.goto(`${BASE}/roles`, { waitUntil: 'load' });
    await page.waitForTimeout(2500);
    await openCreate(page, /new role|create role/i);
    await dlg(page).getByLabel('Name', { exact: false }).first().fill(name);
    await saveDialog(page, /create|save/i);
    ok('A08', 'role created (toast)', /created/i.test(await toast(page)), await toast(page));
    const search = page.locator('input[placeholder*="earch" i]').first();
    if (await search.isVisible().catch(() => false)) { await search.fill(name); await page.waitForTimeout(900); }
    ok('A08', 'role appears in list', await rowVisible(page, name));
  },

  async A11(page) { // Billing plans
    await page.goto(`${BASE}/billing/plans`, { waitUntil: 'load' });
    await page.waitForTimeout(2500);
    const txt = await mainText(page);
    ok('A11', 'plans listed', /plan/i.test(txt) && txt.trim().length > 40, txt.replace(/\n/g, ' ').slice(0, 60));
    const createBtn = page.getByRole('button', { name: /new plan|create plan/i }).first();
    if (await createBtn.isVisible().catch(() => false)) {
      ok('A11', 'plan create affordance', true);
    } else {
      ok('A11', 'plan create affordance', false, 'no create button (React parity?)');
    }
  },

  async A15(page) { // Impersonation grants list (start lives on user detail — C4)
    await page.goto(`${BASE}/impersonation`, { waitUntil: 'load' });
    await page.waitForTimeout(2500);
    const txt = await mainText(page);
    ok('A15', 'grants list renders', /impersonation|grant|no /i.test(txt) && txt.trim().length > 30, txt.replace(/\n/g, ' ').slice(0, 60));
    const manage = /re-open|revoke/i.test(txt);
    ok('A15', 'grant management affordances (when grants exist)', manage || /no /i.test(txt), manage ? 're-open/revoke present' : 'empty state');
  },

  async A17(page) { // Webhooks — create subscription
    const url = `https://hooks.example.com/qa-${ts}`;
    await page.goto(`${BASE}/webhooks`, { waitUntil: 'load' });
    await page.waitForTimeout(2500);
    await openCreate(page, /new subscription/i);
    await dlg(page).getByLabel('Endpoint URL', { exact: false }).fill(url);
    // Pick a suggested event chip (no text-bind dependency) — adds to _events
    const chip = dlg(page).locator('.mud-chip', { hasText: 'tenant.created' }).first();
    await chip.click().catch(async () => {
      await dlg(page).getByLabel('Custom event', { exact: false }).fill('tenant.created');
      await page.waitForTimeout(600);
      await dlg(page).getByRole('button', { name: 'Add', exact: true }).last().click().catch(() => {});
    });
    await page.waitForTimeout(600);
    await saveDialog(page, /create subscription|create|save/i);
    const t = await toast(page);
    const postState = await page.evaluate(() => ({
      dialogs: document.querySelectorAll('.mud-dialog').length,
      dlgText: (document.querySelector('.mud-dialog')?.innerText ?? '').replace(/\n/g, ' | ').slice(0, 700),
      mainHas: document.querySelector('main')?.innerText.includes('hooks.example.com'),
    })).catch(() => null);
    ok('A17', 'webhook subscription created', await rowVisible(page, 'hooks.example.com'), `${t} ${postState ? JSON.stringify(postState).slice(0, 300) : ''}`);
  },

  async A19(page) { // Notifications inbox
    await page.goto(`${BASE}/notifications/inbox`, { waitUntil: 'load' });
    await page.waitForTimeout(2500);
    const txt = await mainText(page);
    ok('A19', 'inbox renders', /notification|inbox|no /i.test(txt) && txt.trim().length > 30, txt.replace(/\n/g, ' ').slice(0, 60));
  },

  async A22(page) { // Settings profile — admin profile is READ-ONLY + image-URL dialog (parity)
    await page.goto(`${BASE}/settings/profile`, { waitUntil: 'load' });
    await page.waitForTimeout(2500);
    const txt = await mainText(page);
    ok('A22', 'read-only profile fields', /username/i.test(txt) && /email/i.test(txt));
    const editImage = page.getByRole('button', { name: /change avatar/i }).first();
    ok('A22', 'avatar edit affordance', await editImage.isVisible().catch(() => false));
  },

  async A24(page) { // Settings sessions
    await page.goto(`${BASE}/settings/sessions`, { waitUntil: 'load' });
    await page.waitForTimeout(2500);
    ok('A24', 'sessions render', (await mainText(page)).length > 40);
  },

  async A25(page) { // Settings appearance
    await page.goto(`${BASE}/settings/appearance`, { waitUntil: 'load' });
    await page.waitForTimeout(2500);
    const switches = await page.locator('.mud-switch input, input[type="checkbox"]').count();
    ok('A25', 'appearance controls present', switches > 0 || /theme|dark|light|accent/i.test(await mainText(page)), `${switches} switches`);
  },
};

const readonly = {
  A03: '/', A16: '/audits', A20: '/health', A10: '/billing', A12: '/billing/invoices', A14: '/billing/topups',
};

const browser = await chromium.launch({ ignoreHTTPSErrors: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();
page.on('pageerror', (e) => consoleErrors.push(`[pageerror] ${String(e).slice(0, 160)}`));
page.on('console', (m) => { if (m.type() === 'error' && /failed|exception/i.test(m.text()) && !/mono_download|favicon/i.test(m.text())) consoleErrors.push(`[console] ${m.text().slice(0, 160)}`); });

const requested = process.argv.slice(2);
try {
  await login(page);
  const screens = requested.length ? requested : Object.keys(actions);
  for (const id of screens) {
    console.log(`[${id}] running…`);
    try {
      await actions[id](page);
    } catch (err) {
      ok(id, 'action flow completed', false, String(err).slice(0, 160));
      await page.screenshot({ path: path.join(OUT, `${id}-fail.png`) }).catch(() => {});
      const diag = await page.evaluate(() => ({
        dialogs: document.querySelectorAll('.mud-dialog').length,
        buttons: [...document.querySelectorAll('.mud-dialog button')].map((b) => `${b.textContent?.trim()}${b.disabled ? '(dis)' : ''}`).slice(0, 6),
      })).catch(() => null);
      if (diag) console.log(`  DIAG [${id}]: ${JSON.stringify(diag)}`);
    }
  }
  if (requested.length === 0) {
    for (const [id, p] of Object.entries(readonly)) {
      try {
        await page.goto(`${BASE}${p}`, { waitUntil: 'load' });
        await page.waitForTimeout(2200);
        const mainTxt = await mainText(page);
        ok(id, 'screen renders with content', mainTxt.trim().length > 40, mainTxt.replace(/\n/g, ' ').slice(0, 60));
      } catch (err) {
        ok(id, 'screen renders', false, String(err).slice(0, 120));
      }
    }
  }
  ok('GLOBAL', 'no console/page errors', consoleErrors.length === 0, consoleErrors.slice(0, 3).join(' || '));
} catch (err) {
  console.log(`FATAL: ${String(err).slice(0, 300)}`);
} finally {
  console.log(results.join('\n'));
  console.log(`TOTAL ${pass} pass, ${fail} fail`);
  await browser.close();
  process.exit(fail > 0 ? 1 : 0);
}

async function login(page) {
  await page.goto(`${BASE}/login`, { waitUntil: 'load', timeout: 30000 });
  await page.waitForTimeout(1500);
  await page.getByLabel('Tenant', { exact: false }).first().waitFor({ state: 'visible', timeout: 20000 });
  await page.getByLabel('Tenant', { exact: false }).first().fill('root');
  await page.getByLabel('Email', { exact: false }).first().fill('superadmin@root.com');
  await page.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await page.getByLabel('Password', { exact: false }).first().blur().catch(() => {});
  await page.waitForTimeout(500);
  for (const label of ['Sign in', 'Sign In', 'Login']) {
    const btn = page.getByRole('button', { name: label, exact: false }).first();
    try { await btn.waitFor({ state: 'visible', timeout: 3000 }); await btn.click(); break; } catch { /* next */ }
  }
  await page.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 25000 });
  await page.waitForTimeout(1500);
}
