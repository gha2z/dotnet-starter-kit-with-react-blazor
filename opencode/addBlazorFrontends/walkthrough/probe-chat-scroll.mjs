// probe-chat-scroll.mjs — P1.1 verification: render-aware auto-scroll + unseen pill (spec.md §1).
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const BASE = 'http://localhost:5176';
const API = 'https://localhost:7030';
const results = [];
const ok = (name, pass, extra = '') => { results.push(pass); console.log(`  ${pass ? 'PASS' : 'FAIL'}: ${name}${extra ? ' — ' + extra : ''}`); };

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

const dist = (p) => p.evaluate(() => {
  const el = document.querySelector('.fsh-chat-messages');
  return el ? el.scrollHeight - el.scrollTop - el.clientHeight : -1;
});

const b = await chromium.launch({ ignoreHTTPSErrors: true });
try {
  const aCtx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const bCtx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const A = await aCtx.newPage();
  const B = await bCtx.newPage();

  // Resolve a Channel-type id with messages via API
  const res = await fetch(`${API}/api/v1/identity/token/issue`, { method: 'POST', headers: { 'content-type': 'application/json', tenant: 'acme' }, body: JSON.stringify({ email: 'admin@acme.com', password: 'Password123!' }) });
  const tok = (await res.json()).accessToken;
  const chans = await (await fetch(`${API}/api/v1/chat/channels`, { headers: { authorization: `Bearer ${tok}` } })).json();
  const chan = chans.find((c) => c.type === 'Channel' && !/^qa/i.test(c.name || ''));

  await login(A);
  await A.goto(`${BASE}/chat/${chan.id}`, { waitUntil: 'domcontentloaded' });
  await A.waitForSelector('[data-message-id]', { timeout: 25000 });
  await A.waitForTimeout(1200);
  await login(B);
  await B.goto(`${BASE}/chat/${chan.id}`, { waitUntil: 'domcontentloaded' });
  await B.waitForSelector('[data-message-id]', { timeout: 25000 });
  await B.waitForTimeout(800);

  // 1) Own send → auto-scrolls to bottom after render
  const ts = Date.now().toString(36).slice(-6);
  const input = A.locator('textarea[aria-label="Message"], input[aria-label="Message"]').first();
  await input.fill(`SCROLL-${ts}`);
  await input.press('Enter');
  await A.waitForTimeout(1200);
  const d1 = await dist(A);
  ok('own send auto-scrolls to bottom', d1 >= 0 && d1 <= 5, `dist=${d1}px`);

  // 2) A scrolls up → B sends → NO yank, pill appears
  await A.locator('.fsh-chat-messages').evaluate((el) => { el.scrollTop = Math.max(0, el.scrollHeight - el.clientHeight - 500); });
  await A.waitForTimeout(300);
  const dBefore = await dist(A);
  const inputB = B.locator('textarea[aria-label="Message"], input[aria-label="Message"]').first();
  await inputB.fill(`REMOTE-${ts}`);
  await inputB.press('Enter');
  await A.waitForSelector(`[data-message-id]:has-text("REMOTE-${ts}")`, { timeout: 8000 });
  await A.waitForTimeout(900);
  const d2 = await dist(A);
  const pillVisible = await A.getByRole('button', { name: /new message/i }).isVisible().catch(() => false);
  ok('scrolled-up: incoming does NOT yank the view', d2 > 150, `dist=${d2}px (before=${dBefore}px)`);
  ok('scrolled-up: jump-to-bottom pill appears', pillVisible, `dist=${d2}px`);

  // 3) Click pill → back to pinned bottom
  if (pillVisible) {
    await A.getByRole('button', { name: /new message/i }).click();
    await A.waitForTimeout(900);
    const d3 = await dist(A);
    ok('pill click pins to bottom', d3 <= 5, `dist=${d3}px`);
  }

  // 4) While pinned at bottom, incoming still auto-scrolls
  await inputB.fill(`PINNED-${ts}`);
  await inputB.press('Enter');
  await A.waitForSelector(`[data-message-id]:has-text("PINNED-${ts}")`, { timeout: 8000 });
  await A.waitForTimeout(900);
  const d4 = await dist(A);
  ok('pinned: incoming auto-scrolls', d4 >= 0 && d4 <= 5, `dist=${d4}px`);
} catch (err) {
  ok('unhandled', false, String(err).slice(0, 200));
} finally {
  await b.close();
}
console.log(`TOTAL ${results.filter(Boolean).length} pass, ${results.filter((x) => !x).length} fail`);
process.exit(results.every(Boolean) ? 0 : 1);
