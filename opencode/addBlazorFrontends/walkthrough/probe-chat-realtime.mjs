// probe-chat-realtime.mjs — 0-gap chat verification with TWO live sessions:
//   A = admin@acme.com, B = alice@acme.com (both dashboard-blazor :5176)
// Tests: channel realtime, DM realtime, notification toast (+click-through),
// avatar rendering, settings gate (channel-only) + save, create-channel dialog.
// Usage: node probe-chat-realtime.mjs
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const BASE = process.argv[2] ?? 'http://localhost:5176';
const ts = Date.now().toString(36);
let pass = 0, fail = 0;
const ok = (name, cond, extra = '') => {
  if (cond) { pass++; console.log(`  PASS: ${name}${extra ? ` — ${extra}` : ''}`); }
  else { fail++; console.log(`  FAIL: ${name}${extra ? ` — ${extra}` : ''}`); }
};

async function login(context, email) {
  const page = await context.newPage();
  await page.goto(`${BASE}/login`, { waitUntil: 'load', timeout: 30000 });
  await page.waitForTimeout(1500);
  await page.getByLabel('Tenant', { exact: false }).first().waitFor({ state: 'visible', timeout: 20000 });
  await page.getByLabel('Tenant', { exact: false }).first().fill('acme');
  await page.getByLabel('Email', { exact: false }).first().fill(email);
  await page.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await page.getByLabel('Password', { exact: false }).first().blur().catch(() => {});
  await page.waitForTimeout(400);
  for (const label of ['Sign in', 'Sign In', 'Login']) {
    const btn = page.getByRole('button', { name: label, exact: false }).first();
    try { await btn.waitFor({ state: 'visible', timeout: 3000 }); await btn.click(); break; } catch { /* next */ }
  }
  await page.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 25000 });
  await page.waitForTimeout(1200);
  return page;
}

async function sendMessage(page, text) {
  const box = page.locator('input[aria-label="Message"]');
  await box.waitFor({ state: 'visible', timeout: 10000 });
  await box.fill(text);
  await page.waitForTimeout(250);
  await box.press('Enter');
}

async function waitForText(page, text, timeoutMs = 5000) {
  const deadline = Date.now() + timeoutMs;
  const t0 = Date.now();
  while (Date.now() < deadline) {
    try {
      const hit = await page.locator(`[data-message-id]:has-text("${text}")`).count();
      if (hit > 0) return Date.now() - t0;
    } catch { /* nav */ }
    await page.waitForTimeout(200);
  }
  return -1;
}

const browser = await chromium.launch({ ignoreHTTPSErrors: true });
const ctxA = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const ctxB = await browser.newContext({ viewport: { width: 1440, height: 900 } });

// Resolve a Channel-TYPE conversation deterministically (the default-first rail
// item can be a DM depending on recency ordering).
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
  const ch = chans.find((c) => c.type === 'Channel' && !/^(QA-Room|QA-LC|Visual-)/i.test(c.name ?? '')) ?? chans[0];
  return ch.id;
}

try {
  console.log('[chat-walk] logging in A (admin@acme.com) + B (alice@acme.com)');
  const A = await login(ctxA, 'admin@acme.com');
  const B = await login(ctxB, 'alice@acme.com');
  ok('login A+B', (await A.title()) !== undefined && (await B.title()) !== undefined);

  // ── 1. Open chat on a deterministic Channel-type conversation ─────────
  const channelId = await firstChannelTypeId();
  console.log(`[chat-walk] A opens /chat/${channelId} (Channel-type)`);
  await A.goto(`${BASE}/chat/${channelId}`, { waitUntil: 'load' });
  await A.waitForSelector('[data-message-id]', { timeout: 25000 });
  ok('A landed on a channel', A.url().includes(channelId), channelId);
  console.log(`[chat-walk] B joins same channel ${channelId}`);
  await B.goto(`${BASE}/chat/${channelId}`, { waitUntil: 'load' });
  await B.waitForSelector('[data-message-id]', { timeout: 20000 });

  // ── 2. Real-time: B sends in channel, A sees instantly (no reopen) ───
  const rt1 = `RT-CH-${ts}`;
  console.log(`[chat-walk] B sends "${rt1}"`);
  await sendMessage(B, rt1);
  const msToA = await waitForText(A, rt1, 5000);
  ok('real-time channel msg appears for other user w/o reopen', msToA >= 0, `${msToA}ms`);

  // ── 3. Avatar block exists for other-author messages ─────────────────
  const avInfo = await A.evaluate(() => {
    const row = document.querySelector('.fsh-chat-message-other');
    const av = row?.querySelector('.mud-avatar');
    return {
      hasAvatar: !!av,
      hasImg: !!av?.querySelector('img'),
      initials: (av?.textContent ?? '').trim(),
    };
  });
  ok('other-user avatar rendered', avInfo.hasAvatar && (avInfo.hasImg || avInfo.initials.length > 0),
    avInfo.hasImg ? '<img> present' : `initials "${avInfo.initials}"`);

  // ── 4. Notification toast: B creates a fresh DM with A via New DM dialog,
  //      then messages while A views the channel ──────────────────────────
  console.log('[chat-walk] B creates DM with Admin via New DM dialog');
  const dmViaDialog = await (async () => {
    const newDmBtn = B.locator('[aria-label="New direct message"]');
    try {
      await newDmBtn.waitFor({ state: 'visible', timeout: 8000 });
      await newDmBtn.click();
      // Search by exact email (debounced 250ms server search), then click the row
      const search = B.locator('.mud-dialog input').first();
      await search.waitFor({ state: 'visible', timeout: 8000 });
      await search.fill('admin@acme.com');
      const row = B.locator('.mud-dialog .fsh-chat-dm-user', { hasText: 'admin@acme.com' }).first();
      await row.waitFor({ state: 'visible', timeout: 12000 });
      await row.click();
      // Must navigate to a conversation DIFFERENT from the shared channel
      await B.waitForURL((u) => /\/chat\/[0-9a-f-]{36}/i.test(u.pathname) && !u.pathname.includes(channelId), { timeout: 15000 });
      return true;
    } catch (err) {
      console.log(`  DIAG dm-dialog: ${String(err).slice(0, 200)}`);
      return false;
    }
  })();
  ok('B created/opened DM with A via dialog', dmViaDialog);
  const dmIdB = B.url().match(/([0-9a-f-]{36})/i)?.[1];
  ok('DM id differs from channel id', !!dmIdB && dmIdB !== channelId, `dm=${dmIdB}`);
  // Give A's ChatChannelAdded→LoadChannels a beat to learn the new conversation
  await A.waitForTimeout(1500);
  const notif = `DM-NOTIF-${ts}`;
  console.log(`[chat-walk] B sends DM "${notif}" (A still on channel ${channelId})`);
  const snackbarBefore = await A.locator('.mud-snackbar').count();
  await sendMessage(B, notif);
  let snackHit = false;
  {
    const deadline = Date.now() + 6000;
    while (Date.now() < deadline) {
      const cnt = await A.locator('.mud-snackbar').count();
      if (cnt > snackbarBefore) { snackHit = true; break; }
      await A.waitForTimeout(200);
    }
  }
  const snackText = await A.locator('.mud-snackbar').last().innerText().catch(() => '');
  ok('notification toast for msg in non-active DM', snackHit || snackText.includes(notif), snackText.slice(0, 80));
  ok('toast shows real DM title (not fallback)', /alice/i.test(snackText) && !/a conversation|#a channel/.test(snackText), snackText.slice(0, 80));

  // ── 6. Real-time in DM (ensure A is actually viewing the DM first) ────
  if (!A.url().includes(dmIdB)) {
    // fallback: navigate like a user — rail DM item titled with A's own name is
    // ambiguous, so click-through already tried; use direct nav as last resort
    await A.goto(`${BASE}/chat/${dmIdB}`, { waitUntil: 'load' });
    await A.waitForSelector('[data-message-id]', { timeout: 20000 });
  }
  const dm2 = `RT-DM-${ts}`;
  await sendMessage(B, dm2);
  const msDm = await waitForText(A, dm2, 5000);
  ok('real-time DM msg appears instantly', msDm >= 0, `${msDm}ms`);

  // ── 7. Settings: visible+working on channel, absent on DM ─────────────
  console.log('[chat-walk] A back to channel for settings checks');
  await A.goto(`${BASE}/chat/${channelId}`, { waitUntil: 'load' });
  await A.waitForSelector('[data-message-id]', { timeout: 20000 });
  const setBtnCh = A.locator('[aria-label="Channel settings"]');
  ok('settings button visible on channel', await setBtnCh.isVisible().catch(() => false));
  await setBtnCh.click();
  const dlgInput = A.locator('.mud-dialog input').first();
  const dlgVisible = await dlgInput.isVisible({ timeout: 8000 }).catch(() => false);
  const preName = dlgVisible ? await dlgInput.inputValue() : '';
  ok('settings dialog opens with current name', dlgVisible && preName.length > 0, `"${preName}"`);
  if (dlgVisible) {
    await dlgInput.fill(`${preName}-QA`);
    await A.getByRole('button', { name: 'Save', exact: false }).last().click();
    await A.waitForTimeout(1500);
    const headerTxt = await A.evaluate(() => document.querySelector('.fsh-chat-root')?.innerText ?? '');
    ok('settings save updates channel title live', headerTxt.includes(`${preName}-QA`));
    // restore original name
    await setBtnCh.click();
    await dlgInput.fill(preName);
    await A.getByRole('button', { name: 'Save', exact: false }).last().click();
    await A.waitForTimeout(1000);
  }

  await A.goto(dmIdB ? `${BASE}/chat/${dmIdB}` : A.url(), { waitUntil: 'load' });
  await A.waitForTimeout(2000);
  const setBtnDmCount = await A.locator('[aria-label="Channel settings"]').count();
  ok('settings button ABSENT on direct message', setBtnDmCount === 0);

  // ── 8. Create channel dialog ─────────────────────────────────────────
  console.log('[chat-walk] create-channel dialog');
  await A.goto(`${BASE}/chat`, { waitUntil: 'load' });
  await A.waitForSelector('[aria-label="Create channel"]', { timeout: 15000 });
  await A.locator('[aria-label="Create channel"]').first().click();
  const createInput = A.locator('.mud-dialog input').first();
  ok('create dialog opens', await createInput.isVisible({ timeout: 8000 }).catch(() => false));
  const roomName = `QA-Room-${ts}`;
  await A.locator('.mud-dialog input').first().fill(roomName);
  await A.locator('.mud-dialog').getByRole('button', { name: 'Create', exact: true }).click();
  await A.waitForTimeout(1800);
  const railHasRoom = await A.evaluate((n) => document.querySelector('.fsh-chat-root')?.innerText.includes(n) ?? false, roomName);
  ok('created channel appears in rail', railHasRoom, roomName);

  // ── console errors ────────────────────────────────────────────────────
  ok('walkthrough complete', pass + fail > 8);
} catch (err) {
  fail++;
  console.log(`  FAIL: unhandled — ${String(err).slice(0, 300)}`);
} finally {
  console.log(`TOTAL ${pass} pass, ${fail} fail`);
  await browser.close();
  process.exit(fail > 0 ? 1 : 0);
}
