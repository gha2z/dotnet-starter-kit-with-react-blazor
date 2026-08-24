// probe-impersonation.mjs — C4: full impersonation cycle with real clicks.
// admin (5175) → tenant detail → Impersonate user → pick user → reason →
// Start & open dashboard (new tab) → dashboard banner shows → End impersonation
// → /impersonation-ended terminal (D36).
// Usage: node probe-impersonation.mjs
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

let pass = 0, fail = 0;
const ok = (name, cond, extra = '') => {
  if (cond) { pass++; console.log(`  PASS: ${name}${extra ? ` — ${extra}` : ''}`); }
  else { fail++; console.log(`  FAIL: ${name}${extra ? ` — ${extra}` : ''}`); }
};

const browser = await chromium.launch({ ignoreHTTPSErrors: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();

async function login(page, base, tenant, email) {
  await page.goto(`${base}/login`, { waitUntil: 'load', timeout: 30000 });
  await page.waitForTimeout(1500);
  await page.getByLabel('Tenant', { exact: false }).first().waitFor({ state: 'visible', timeout: 20000 });
  await page.getByLabel('Tenant', { exact: false }).first().fill(tenant);
  await page.getByLabel('Email', { exact: false }).first().fill(email);
  await page.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await page.getByLabel('Password', { exact: false }).first().blur().catch(() => {});
  await page.waitForTimeout(500);
  for (const label of ['Sign in', 'Sign In', 'Login']) {
    const btn = page.getByRole('button', { name: label, exact: false }).first();
    try { await btn.waitFor({ state: 'visible', timeout: 3000 }); await btn.click(); break; } catch { /* next */ }
  }
  await page.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 25000 });
  await page.waitForTimeout(1200);
}

try {
  await login(page, 'http://localhost:5175', 'root', 'superadmin@root.com');

  // tenants → acme detail
  await page.goto('http://localhost:5175/tenants', { waitUntil: 'load' });
  await page.waitForTimeout(2500);
  const acmeRow = page.locator('[role="button"], tr, .fsh-list-row').filter({ hasText: /acme/i }).first();
  await acmeRow.click();
  await page.waitForTimeout(2000);
  ok('tenant detail opened', /\/tenants\//i.test(page.url()), page.url());

  // impersonate dialog
  await page.getByRole('button', { name: /impersonate user/i }).first().click();
  const dlg = page.locator('.mud-dialog').last();
  await dlg.waitFor({ state: 'visible', timeout: 8000 });
  await page.waitForTimeout(1200); // user search loads
  ok('impersonate dialog opens with user list', (await dlg.innerText()).length > 50);

  // pick a non-admin user (alice) via the search box → Next
  const search = dlg.getByPlaceholder(/search by name/i).first();
  await search.fill('alice');
  await search.blur(); // non-Immediate MudTextField binds on change(=blur)
  await page.waitForTimeout(1500); // debounce 250 + HTTP
  const diag = await page.evaluate(() => ({
    listItems: document.querySelectorAll('.mud-dialog .mud-list-item').length,
    dlgText: (document.querySelector('.mud-dialog')?.innerText ?? '').replace(/\n/g, ' | ').slice(0, 300),
    snackbar: document.querySelector('.mud-snackbar')?.innerText ?? '(none)',
  })).catch(() => null);
  console.log(`  DIAG: ${JSON.stringify(diag)}`);
  const userRow = dlg.locator('.mud-list-item').filter({ hasText: /alice/i }).first();
  if ((await userRow.count()) === 0) {
    // fall back to the first listed user
    await dlg.locator('.mud-list-item').first().click();
  } else {
    await userRow.click();
  }
  await page.waitForTimeout(400);
  await dlg.getByRole('button', { name: /next: configure/i }).click();
  await page.waitForTimeout(600);
  ok('step 2 (configure) reached', /configure impersonation/i.test(await dlg.innerText()));

  // reason → Start & open dashboard (opens a NEW TAB)
  const popupP = ctx.waitForEvent('page', { timeout: 25000 }).catch(() => null);
  const reasonField = dlg.getByLabel('Reason (required)', { exact: false }).first();
  await reasonField.fill('QA probe impersonation session');
  await reasonField.blur(); // non-Immediate bind — blur before Start
  await page.waitForTimeout(500);
  await dlg.getByRole('button', { name: /start & open dashboard|start and open/i }).click().catch(() => {});
  const dash = await popupP;
  ok('dashboard tab opened for impersonated user', !!dash, dash?.url() ?? 'no popup');

  if (dash) {
    await dash.waitForLoadState('load');
    await dash.waitForTimeout(4000); // WASM boot + handoff token exchange
    const banner = dash.locator('text=/impersonat/i').first();
    const bannerVisible = await banner.isVisible().catch(() => false);
    ok('impersonation banner visible in dashboard', bannerVisible);
    await dash.screenshot({ path: path.join(import.meta.dirname, 'evidence', 'impersonation-banner.png') });

    // End impersonation from the banner — React parity (impersonation-banner.tsx:47):
    // a root operator ends → /login; non-root → "/".
    const endBtn = dash.getByRole('button', { name: /end impersonation/i }).first();
    await endBtn.click();
    await dash.waitForURL((u) => /\/login$/i.test(u.pathname) || u.pathname === '/', { timeout: 20000 }).catch(() => {});
    ok('end impersonation → /login (root-operator parity)', /\/login$/i.test(dash.url()), dash.url());

    // D36 terminal page renders by direct navigation
    await dash.goto('http://localhost:5176/impersonation-ended', { waitUntil: 'load' });
    await dash.waitForTimeout(2500);
    const terminalTxt = await dash.evaluate(() => document.body.innerText).catch(() => '');
    ok('D36 /impersonation-ended renders', /impersonation ended/i.test(terminalTxt) && /back to sign in/i.test(terminalTxt), terminalTxt.replace(/\n/g, ' ').slice(0, 80));
  }

  console.log(`TOTAL ${pass} pass, ${fail} fail`);
} catch (err) {
  console.log(`  FAIL: unhandled — ${String(err).slice(0, 300)}`);
  await page.screenshot({ path: path.join(import.meta.dirname, 'evidence', 'impersonation-fail.png') }).catch(() => {});
} finally {
  await browser.close();
  process.exit(fail > 0 ? 1 : 0);
}
