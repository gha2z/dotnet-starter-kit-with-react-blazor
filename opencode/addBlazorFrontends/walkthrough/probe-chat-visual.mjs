// probe-chat-visual.mjs — capture screenshots of every chat state for visual
// inspection: desktop (1440x900) + mobile (390x844).
// Usage: node probe-chat-visual.mjs
import { pathToFileURL } from 'node:url';
import path from 'node:path';
import fs from 'node:fs';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const BASE = process.argv[2] ?? 'http://localhost:5176';
const OUT = path.join(import.meta.dirname, 'evidence', 'chat-visual');
fs.mkdirSync(OUT, { recursive: true });
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const ts = Date.now().toString(36);
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

async function apiToken(email) {
  const res = await fetch('https://localhost:7030/api/v1/identity/token/issue', {
    method: 'POST',
    headers: { 'content-type': 'application/json', tenant: 'acme', 'X-FSH-App': 'dashboard' },
    body: JSON.stringify({ email, password: 'Password123!' }),
  });
  return (await res.json()).accessToken;
}
async function firstChannelTypeId() {
  const tok = await apiToken('admin@acme.com');
  const res = await fetch('https://localhost:7030/api/v1/chat/channels', { headers: { authorization: `Bearer ${tok}` } });
  const list = await res.json();
  const chans = Array.isArray(list) ? list : list.items ?? [];
  const ch = chans.find((c) => c.type === 'Channel' && !/^QA-Room/i.test(c.name ?? '')) ?? chans[0];
  return ch.id;
}

async function login(context, email) {
  const page = await context.newPage();
  await page.goto(`${BASE}/login`, { waitUntil: 'load', timeout: 30000 });
  await page.waitForTimeout(1500);
  await page.getByLabel('Tenant', { exact: false }).first().waitFor({ state: 'visible', timeout: 20000 });
  await page.getByLabel('Tenant', { exact: false }).first().fill('acme');
  await page.getByLabel('Email', { exact: false }).first().fill(email);
  await page.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await page.getByLabel('Password', { exact: false }).first().blur().catch(() => {});
  await page.waitForTimeout(500);
  for (const label of ['Sign in', 'Sign In', 'Login']) {
    const btn = page.getByRole('button', { name: label, exact: false }).first();
    try { await btn.waitFor({ state: 'visible', timeout: 3000 }); await btn.click(); break; } catch { /* next */ }
  }
  await page.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 25000 });
  await page.waitForTimeout(1500);
  return page;
}

async function shot(page, name) {
  await page.waitForTimeout(600);
  await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: false });
  console.log(`[shot] ${name}`);
}

const browser = await chromium.launch({ ignoreHTTPSErrors: true });

// ══════════ DESKTOP 1440x900 ══════════
const ctxA = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const A = await login(ctxA, 'admin@acme.com');
const channelId = await firstChannelTypeId();
await A.goto(`${BASE}/chat/${channelId}`, { waitUntil: 'load' });
await A.waitForSelector('[data-message-id]', { timeout: 25000 });
await shot(A, 'd01-channel-initial');

// own message sent
const box = A.locator('input[aria-label="Message"]');
await box.fill(`Visual check ${ts} — desktop own msg`);
await box.press('Enter');
await A.waitForTimeout(1200);
await shot(A, 'd02-after-send');

// hover a message block (toolbar persistence)
const block = A.locator('.fsh-chat-message-other').last();
if (await block.count() > 0) {
  await block.hover();
  await A.waitForTimeout(400);
  await shot(A, 'd03-hover-other');
}
const ownBlock = A.locator('.fsh-chat-message-own').last();
if (await ownBlock.count() > 0) {
  await ownBlock.hover();
  await A.waitForTimeout(400);
  await shot(A, 'd04-hover-own');
}

// reply preview bar (toolbar is hover-gated CSS — use JS click after hover)
try {
  await block.hover();
  await A.waitForTimeout(250);
  await block.locator('[title="Reply"]').last().evaluate((el) => el.click());
  await A.waitForTimeout(600);
  await shot(A, 'd05-reply-bar');
  await A.locator('[aria-label="Cancel reply"]').click({ timeout: 3000 }).catch(() => {});
} catch (err) { console.log(`[warn] reply step: ${String(err).slice(0, 120)}`); }

// search dialog
await A.locator('[aria-label="Search messages"]').first().click();
await A.waitForSelector('.mud-dialog', { timeout: 8000 });
const sInput = A.locator('.mud-dialog input').first();
if (await sInput.count() > 0) { await sInput.fill('standup'); await A.waitForTimeout(700); }
await shot(A, 'd06-search-dialog');
await A.goto(`${BASE}/chat/${channelId}`, { waitUntil: 'load' });
await A.waitForSelector('[data-message-id]', { timeout: 20000 });

// settings dialog
try {
  await A.locator('[aria-label="Channel settings"]').click({ timeout: 5000 });
  await A.waitForSelector('.mud-dialog', { timeout: 8000 });
  await shot(A, 'd07-settings-dialog');
} catch (err) { console.log(`[warn] settings step: ${String(err).slice(0, 120)}`); }
await A.goto(`${BASE}/chat/${channelId}`, { waitUntil: 'load' });
await A.waitForSelector('[data-message-id]', { timeout: 20000 });

// create channel dialog
try {
  await A.locator('[aria-label="Create channel"]').first().click({ timeout: 5000 });
  await A.waitForSelector('.mud-dialog input', { timeout: 8000 });
  await A.locator('.mud-dialog input').first().fill(`Visual-${ts}`);
  await shot(A, 'd08-create-dialog');
} catch (err) { console.log(`[warn] create step: ${String(err).slice(0, 120)}`); }
await A.goto(`${BASE}/chat/${channelId}`, { waitUntil: 'load' });
await A.waitForSelector('[data-message-id]', { timeout: 20000 });

// new dm dialog
try {
  await A.locator('[aria-label="New direct message"]').first().click({ timeout: 5000 });
  await A.waitForSelector('.mud-dialog .fsh-chat-dm-user', { timeout: 10000 });
  await shot(A, 'd09-newdm-dialog');
} catch (err) { console.log(`[warn] newdm step: ${String(err).slice(0, 120)}`); }
await A.goto(`${BASE}/chat/${channelId}`, { waitUntil: 'load' });
await A.waitForSelector('[data-message-id]', { timeout: 20000 });

// DM conversation view
const dmItem = A.locator('.fsh-chat-rail .mud-list-item').filter({ hasText: /alice/i }).first();
let dmUrl = null;
if (await dmItem.count() > 0) {
  await dmItem.click();
  await A.waitForTimeout(1500);
  dmUrl = A.url();
  await shot(A, 'd10-dm-conversation');
}

// incoming toast while on channel
await A.goto(`${BASE}/chat/${channelId}`, { waitUntil: 'load' });
await A.waitForSelector('[data-message-id]', { timeout: 20000 });
{
  // second context B sends into the DM
  const ctxB = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const B = await login(ctxB, 'alice@acme.com');
  if (!dmUrl) {
    await B.goto(`${BASE}/chat`, { waitUntil: 'load' });
    await B.waitForTimeout(2000);
    const item = B.locator('.fsh-chat-rail .mud-list-item').filter({ hasText: /admin/i }).first();
    if (await item.count() > 0) { await item.click(); await B.waitForTimeout(1200); }
  } else {
    await B.goto(dmUrl, { waitUntil: 'load' });
    await B.waitForTimeout(1500);
  }
  const bBox = B.locator('input[aria-label="Message"]');
  if (await bBox.count() > 0) {
    await bBox.fill(`Toast visual ${ts}`);
    await bBox.press('Enter');
    await A.waitForTimeout(1500);
    await shot(A, 'd11-toast-visible');
  }
  await ctxB.close();
}

await ctxA.close();

// ══════════ MOBILE 390x844 ══════════
const ctxM = await browser.newContext({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true });
const M = await login(ctxM, 'admin@acme.com');
await M.goto(`${BASE}/chat/${channelId}`, { waitUntil: 'load' });
await M.waitForSelector('[data-message-id]', { timeout: 25000 });
await shot(M, 'm01-channel-initial');

const mbox = M.locator('input[aria-label="Message"]');
await mbox.scrollIntoViewIfNeeded().catch(() => {});
await shot(M, 'm02-composer-area');
await mbox.fill(`Mobile visual ${ts}`);
await mbox.press('Enter');
await M.waitForTimeout(1200);
await shot(M, 'm03-after-send');

// dialogs on mobile
try {
  await M.locator('[aria-label="Create channel"]').first().click({ timeout: 5000 });
  await M.waitForSelector('.mud-dialog input', { timeout: 8000 });
  await shot(M, 'm04-create-dialog');
} catch (err) { console.log(`[warn] m-create step: ${String(err).slice(0, 120)}`); }
await M.goto(`${BASE}/chat/${channelId}`, { waitUntil: 'load' });
await M.waitForSelector('[data-message-id]', { timeout: 20000 });

// rail scroll state (channels vs dms)
await M.evaluate(() => document.querySelector('.fsh-chat-root')?.scrollIntoView());
await shot(M, 'm05-full-page');
await ctxM.close();

console.log(`DONE → ${OUT}`);
await browser.close();
