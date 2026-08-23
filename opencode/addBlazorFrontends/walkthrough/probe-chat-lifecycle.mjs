// probe-chat-lifecycle.mjs — C1 channel lifecycle: create validation, modify,
// archive + graceful unreachable state, role-gated settings (admin vs member),
// member add/remove, leave. Baseline run EXPECTS failures on role-gating items
// (marked post-fix); after the ChannelSettingsDialog fix everything must pass.
// Usage: node probe-chat-lifecycle.mjs
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const BASE = process.argv[2] ?? 'http://localhost:5176';
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const ts = Date.now().toString(36);
let pass = 0, fail = 0;
const ok = (name, cond, extra = '') => {
  if (cond) { pass++; console.log(`  PASS: ${name}${extra ? ` — ${extra}` : ''}`); }
  else { fail++; console.log(`  FAIL: ${name}${extra ? ` — ${extra}` : ''}`); }
};

process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
async function apiToken(email) {
  const res = await fetch('https://localhost:7030/api/v1/identity/token/issue', {
    method: 'POST',
    headers: { 'content-type': 'application/json', tenant: 'acme', 'X-FSH-App': 'dashboard' },
    body: JSON.stringify({ email, password: 'Password123!' }),
  });
  return (await res.json()).accessToken;
}
async function channelsFor(email) {
  const tok = await apiToken(email);
  const res = await fetch('https://localhost:7030/api/v1/chat/channels', { headers: { authorization: `Bearer ${tok}` } });
  const list = await res.json();
  return Array.isArray(list) ? list : list.items ?? [];
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
  await page.waitForTimeout(1200);
  return page;
}

// MudDialog close is animated — a locator can match the CLOSING dialog. Always
// wait for detachment before reopening, and give async name-resolution a beat.
async function closeDialog(page) {
  const cancel = page.locator('.mud-dialog').last().getByRole('button', { name: /cancel/i });
  if (await cancel.count() > 0) { await cancel.click().catch(() => {}); }
  else { await page.keyboard.press('Escape'); }
  await page.locator('.mud-dialog').first().waitFor({ state: 'detached', timeout: 6000 }).catch(() => {});
  await page.waitForTimeout(400);
}
async function openSettings(page) {
  await page.locator('[aria-label="Channel settings"]').click();
  await page.locator('.mud-dialog').first().waitFor({ state: 'visible', timeout: 8000 });
  await page.waitForTimeout(900);
}

const browser = await chromium.launch({ ignoreHTTPSErrors: true });
const ctxA = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const ctxB = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const consoleErrors = [];
try {
  const A = await login(ctxA, 'admin@acme.com');
  A.on('pageerror', (e) => { consoleErrors.push(String(e).slice(0, 200)); console.log(`  [pageerror] ${String(e).slice(0, 160)}`); });
  A.on('console', (m) => { if (m.type() === 'error' && /unhandled|exception|error:/i.test(m.text())) console.log(`  [console] ${m.text().slice(0, 160)}`); });

  // ── 1. Create validation ─────────────────────────────────────────────
  await A.goto(`${BASE}/chat`, { waitUntil: 'load' });
  await A.waitForSelector('[aria-label="Create channel"]', { timeout: 20000 });
  await A.locator('[aria-label="Create channel"]').first().click();
  const dlg = A.locator('.mud-dialog');
  await dlg.locator('input').first().waitFor({ state: 'visible', timeout: 8000 });
  const createBtn = dlg.getByRole('button', { name: 'Create', exact: true });
  ok('create disabled on empty name', await createBtn.isDisabled().catch(() => true));
  const nameInput = dlg.locator('input').first();
  await nameInput.fill('X'.repeat(85));
  const len = (await nameInput.inputValue()).length;
  ok('name capped at 80 chars', len <= 80, `len=${len}`);
  await dlg.getByRole('button', { name: 'Cancel', exact: true }).click();
  await A.waitForTimeout(500);
  ok('cancel closes create dialog', (await A.locator('.mud-dialog').count()) === 0);

  // ── 2. Create real channel ───────────────────────────────────────────
  const room = `QA-LC-${ts}`;
  await A.locator('[aria-label="Create channel"]').first().click();
  await dlg.locator('input').first().waitFor({ state: 'visible', timeout: 8000 });
  await dlg.locator('input').first().fill(room);
  await dlg.getByRole('button', { name: 'Create', exact: true }).click();
  await A.waitForTimeout(1800);
  const railTxt = await A.evaluate(() => document.querySelector('.fsh-chat-root')?.innerText ?? '');
  ok('created channel in rail', railTxt.includes(room), room);
  const chans = await channelsFor('admin@acme.com');
  const mine = chans.find((c) => c.name === room);
  ok('channel via API', !!mine, mine?.id ?? 'missing');
  const roomId = mine.id;

  // ── 3. Modify (rename + description persist) ─────────────────────────
  await A.goto(`${BASE}/chat/${roomId}`, { waitUntil: 'load' });
  await A.waitForSelector('[aria-label="Channel settings"]', { timeout: 20000 });
  await openSettings(A);
  await dlg.locator('input').first().fill(`${room}-v2`);
  await dlg.locator('textarea').first().fill('Lifecycle probe description');
  await dlg.getByRole('button', { name: 'Save', exact: true }).click();
  await A.locator('.mud-dialog').first().waitFor({ state: 'detached', timeout: 6000 }).catch(() => {});
  await A.waitForTimeout(800);
  const headerTxt = await A.evaluate(() => document.querySelector('.fsh-chat-conversation .mud-paper')?.innerText ?? '');
  ok('rename reflected in header (scoped)', headerTxt.includes(`${room}-v2`), headerTxt.split('\n')[0]);
  await openSettings(A);
  const descVal = await dlg.locator('textarea').first().inputValue();
  ok('description persisted', descVal === 'Lifecycle probe description', `"${descVal}"`);
  const dlgTxt = await dlg.innerText();
  const memberTxt = dlgTxt.split('Members')[1] ?? '';
  // Names render with the GUID as an intentional caption line — assert that at
  // least one real display name (two capitalized words) is present.
  ok('members listed with names', /[A-Z][a-z]+\s+[A-Z][a-z]+/.test(memberTxt), memberTxt.split('\n').filter(Boolean).slice(0, 4).join(' | '));
  await closeDialog(A);

  // ── 4. Member add (admin) — post-fix UI ──────────────────────────────
  await openSettings(A);
  const addInput = dlg.locator('input[placeholder*="Search users"]'); // name input + private checkbox precede it
  let addedOk = false;
  if (await addInput.count() > 0) {
    await addInput.fill('alice');
    await A.waitForTimeout(1500);
    const pick = dlg.locator('.mud-list-item').filter({ hasText: /alice/i }).first();
    if (await pick.count() > 0) {
      await pick.click();
      await A.waitForTimeout(1800);
      const listTxt = (await dlg.innerText()).split('Members')[1] ?? '';
      addedOk = /alice/i.test(listTxt);
    } else {
      console.log('  DIAG picker results:', (await dlg.innerText()).slice(0, 200).replace(/\n/g, ' | '));
    }
  }
  ok('admin can add member via picker (post-fix)', addedOk);
  await closeDialog(A);

  // ── 5. Non-admin gating: alice on the QA channel she was just added to ─
  const B = await login(ctxB, 'alice@acme.com');
  await B.waitForTimeout(2500); // ChatChannelAdded → LoadChannels
  const bchans = await channelsFor('alice@acme.com');
  const qa = bchans.find((c) => c.type === 'Channel' && new RegExp(`^${room}-v2$`).test(c.name ?? ''));
  ok('alice sees the channel she was added to', !!qa, qa?.id ?? 'missing');
  if (qa) {
    await B.goto(`${BASE}/chat/${qa.id}`, { waitUntil: 'load' });
    await B.waitForSelector('[aria-label="Channel settings"]', { timeout: 20000 });
    await openSettings(B);
    const bDlg = B.locator('.mud-dialog').first();
    const bDlgTxt = await bDlg.innerText();
    const hasArchive = /archive/i.test(bDlgTxt);
    const hasLeave = /leave channel/i.test(bDlgTxt);
    ok('non-admin: NO archive button (post-fix)', !hasArchive, hasArchive ? 'archive visible' : 'hidden');
    ok('non-admin: Leave channel offered (post-fix)', hasLeave);
    // general section gating: name field hidden for non-admin (post-fix)
    const nameFieldVisible = await bDlg.locator('input').first().isVisible().catch(() => false);
    ok('non-admin: general section hidden (post-fix)', !nameFieldVisible);
    if (hasLeave) {
      await bDlg.getByRole('button', { name: /leave channel/i }).click();
      await B.waitForTimeout(900);
      const confirmBtn = B.locator('.mud-dialog').last().getByRole('button', { name: /^leave$/i }).last();
      if (await confirmBtn.count() > 0) { await confirmBtn.click().catch(() => {}); }
      await B.locator('.mud-dialog').first().waitFor({ state: 'detached', timeout: 6000 }).catch(() => {});
      await B.waitForTimeout(1800);
      const bRail = await B.evaluate(() => document.querySelector('.fsh-chat-root')?.innerText ?? '');
      ok('leave removes channel from rail (post-fix)', !bRail.includes(`${room}-v2`), bRail.includes(`${room}-v2`) ? 'still in rail' : '');
    }
  }

  // ── 6. Archive (admin) + graceful unreachable ────────────────────────
  await A.goto(`${BASE}/chat/${roomId}`, { waitUntil: 'load' });
  await A.waitForSelector('[aria-label="Channel settings"]', { timeout: 20000 });
  await openSettings(A);
  await dlg.getByRole('button', { name: 'Archive', exact: true }).click();
  await A.waitForTimeout(900);
  // confirm dialog (FshConfirmDialogContent)
  const confirmBtn = A.locator('.mud-dialog').last().getByRole('button', { name: /^archive$/i }).last();
  await confirmBtn.click().catch(() => {});
  await A.locator('.mud-dialog').first().waitFor({ state: 'detached', timeout: 6000 }).catch(() => {});
  await A.waitForTimeout(2000);
  const aRail = await A.evaluate(() => document.querySelector('.fsh-chat-root')?.innerText ?? '');
  ok('archived channel leaves rail', !aRail.includes(`${room}-v2`), aRail.includes(`${room}-v2`) ? 'still in rail' : '');
  const toastTxt = await A.locator('.mud-snackbar').last().innerText().catch(() => '');
  ok('archive toast shown', /archived/i.test(toastTxt), toastTxt.slice(0, 60));

  // graceful unreachable state
  await A.goto(`${BASE}/chat/${roomId}`, { waitUntil: 'load' });
  await A.waitForSelector('.fsh-chat-root', { timeout: 30000 });
  await A.waitForTimeout(2500);
  const rootAlive = await A.evaluate(() => !!document.querySelector('.fsh-chat-root, .fsh-empty, main'));
  const convTxt = await A.evaluate(() => document.querySelector('.fsh-chat-conversation')?.innerText ?? document.body.innerText);
  const graceful = /isn'?t reachable|archived|no longer a member/i.test(convTxt);
  ok('archived channel URL shows graceful state (post-fix)', rootAlive && graceful, convTxt.replace(/\n/g, ' ').slice(0, 100));

  ok('no unhandled page errors', consoleErrors.length === 0, `${consoleErrors.length} errors`);

  console.log(`TOTAL ${pass} pass, ${fail} fail`);
} catch (err) {
  fail++;
  console.log(`  FAIL: unhandled — ${String(err).slice(0, 300)}`);
  try {
    const pg = A ?? B;
    if (pg) {
      await pg.screenshot({ path: path.join(import.meta.dirname, 'evidence', 'chat-lifecycle-crash.png') }).catch(() => {});
      const overlays = await pg.evaluate(() => ({
        dialogs: document.querySelectorAll('.mud-dialog-container').length,
        overlays: document.querySelectorAll('.mud-overlay').length,
        scrim: document.querySelectorAll('.mud-overlay-scrim').length,
        bodyLast: document.body.innerText.slice(0, 200),
      }));
      console.log(`  DIAG overlays: ${JSON.stringify(overlays)}`);
    }
  } catch { /* diagnostics best-effort */ }
} finally {
  await browser.close();
  process.exit(fail > 0 ? 1 : 0);
}
