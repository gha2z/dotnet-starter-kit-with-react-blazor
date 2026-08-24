// probe-inactivity.mjs — C4: config-driven inactivity auto-logout, live.
// Route-intercepts /config.json to inject inactivityTimeoutMinutes: 1 (no product
// file changes), then: login → idle → warning dialog appears → "Stay signed in"
// dismisses → idle again → auto sign-out lands on /login.
// Usage: node probe-inactivity.mjs [dashboard|admin]
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const APP = process.argv[2] ?? 'dashboard';
const BASE = APP === 'admin' ? 'http://localhost:5175' : 'http://localhost:5176';
const TENANT = APP === 'admin' ? 'root' : 'acme';
const EMAIL = APP === 'admin' ? 'superadmin@root.com' : 'admin@acme.com';
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

let pass = 0, fail = 0;
const ok = (name, cond, extra = '') => {
  if (cond) { pass++; console.log(`  PASS: ${name}${extra ? ` — ${extra}` : ''}`); }
  else { fail++; console.log(`  FAIL: ${name}${extra ? ` — ${extra}` : ''}`); }
};

const browser = await chromium.launch({ ignoreHTTPSErrors: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();

// Inject a 1-minute idle timeout via config interception (React parity: durations
// come from runtime config so operators can tune per deployment).
await ctx.route('**/config.json', async (route) => {
  const resp = await route.fetch();
  const body = await resp.json();
  body.inactivityTimeoutMinutes = 2;
  await route.fulfill({ json: body });
});

try {
  // login
  await page.goto(`${BASE}/login`, { waitUntil: 'load', timeout: 30000 });
  await page.waitForTimeout(1500);
  await page.getByLabel('Tenant', { exact: false }).first().waitFor({ state: 'visible', timeout: 20000 });
  await page.getByLabel('Tenant', { exact: false }).first().fill(TENANT);
  await page.getByLabel('Email', { exact: false }).first().fill(EMAIL);
  await page.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await page.getByLabel('Password', { exact: false }).first().blur().catch(() => {});
  await page.waitForTimeout(500);
  for (const label of ['Sign in', 'Sign In', 'Login']) {
    const btn = page.getByRole('button', { name: label, exact: false }).first();
    try { await btn.waitFor({ state: 'visible', timeout: 3000 }); await btn.click(); break; } catch { /* next */ }
  }
  await page.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 25000 });
  ok('login', true, page.url());

  // idle=120s, warning window=60s → warning opens at ~60s idle, expiry at ~120s.
  // Check at 70s: inside the warning window.
  console.log('[inactivity] idling 70s for the warning…');
  await page.waitForTimeout(70_000);
  const dialogVisible = await page.getByText('Are you still there?').isVisible().catch(() => false);
  ok('warning dialog appears after idle', dialogVisible);
  if (dialogVisible) {
    await page.screenshot({ path: path.join(import.meta.dirname, 'evidence', `inactivity-warning-${APP}.png`) });
    // Stay signed in → dialog dismisses, countdown resets to the full idle
    await page.getByRole('button', { name: /stay signed in/i }).click();
    await page.waitForTimeout(1500);
    const dismissed = !(await page.getByText('Are you still there?').isVisible().catch(() => false));
    ok('stay-signed-in dismisses warning', dismissed);
  }

  // idle again — no activity → auto sign-out to /login (130s > 120s idle)
  console.log('[inactivity] idling 130s for auto sign-out…');
  await page.waitForTimeout(130_000);
  const backAtLogin = page.url().toLowerCase().includes('/login');
  ok('auto sign-out lands on /login', backAtLogin, page.url());

  console.log(`TOTAL ${pass} pass, ${fail} fail`);
} catch (err) {
  console.log(`  FAIL: unhandled — ${String(err).slice(0, 300)}`);
  await page.screenshot({ path: path.join(import.meta.dirname, 'evidence', `inactivity-fail-${APP}.png`) }).catch(() => {});
} finally {
  await browser.close();
  process.exit(fail > 0 ? 1 : 0);
}
