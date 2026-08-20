// probe-session-review.mjs — targeted real-browser verification for this session's
// dashboard-blazor changes: overview parity (stat cards / quick actions / sections /
// LIVE pulse), nav-section-caption font parity, chat unread badge position + mark-read,
// topbar avatar tile. Requires the dashboard Blazor app on 5176 (latest build).
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const { APPS } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'config.mjs')).href);
const { login } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'auth.mjs')).href);

const BASE = process.argv[2] ?? 'http://localhost:5176';
const fails = [];
const consoleErrors = [];
const pageErrors = [];
const apiFailures = [];

const browser = await chromium.launch({ headless: true, ignoreHTTPSErrors: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
const page = await ctx.newPage();
page.on('console', (m) => { if (m.type() === 'error') consoleErrors.push(m.text()); });
page.on('pageerror', (e) => pageErrors.push(String(e)));
page.on('requestfailed', (r) => { if (r.url().includes('/api/')) apiFailures.push(`${r.failure()?.errorText ?? 'failed'} ${r.url()}`); });
page.on('response', (r) => { if (r.status() >= 400 && r.url().includes('/api/')) apiFailures.push(`HTTP ${r.status()} ${r.url()}`); });

const ok = (name, cond, extra = '') => {
  console.log(`  ${cond ? 'PASS' : 'FAIL'} ${name}${cond ? '' : ' — ' + extra}`);
  if (!cond) fails.push(name + (extra ? ' — ' + extra : ''));
};

// --- login
const loginRes = await login(page, APPS.dashboard.blazor, {});
console.log(`[probe] login ok=${loginRes.ok} url=${loginRes.url} ${loginRes.error ?? ''}`);
if (!loginRes.ok) { console.error('LOGIN FAILED — aborting'); process.exit(1); }

// --- overview
await page.goto(`${BASE}/`, { waitUntil: 'domcontentloaded' });
await page.locator('.fsh-stat-card').first().waitFor({ timeout: 15000 });
await page.waitForTimeout(1500);

const statCards = await page.locator('.fsh-stat-card').count();
ok('4 stat cards', statCards === 4, `count=${statCards}`);
const statLabels = await page.locator('.fsh-stat-label').allTextContents();
ok('stat labels (Plan/Valid for/Resources/Live events)', JSON.stringify(statLabels.map((s) => s.trim())) === JSON.stringify(['Plan', 'Valid for', 'Resources', 'Live events']), statLabels.join('|'));

const statValues = await page.locator('.fsh-stat-value').allTextContents();
ok('stat values rendered (no skeletons)', statValues.length === 4 && statValues.every((v) => v.trim().length > 0), statValues.join('|'));

const qaTitles = await page.locator('.fsh-quick-action-title').allTextContents();
ok('4 quick actions', qaTitles.length === 4, `count=${qaTitles.length}`);
ok('quick action titles', ['Invite users', 'Browse catalog', 'Subscription', 'Live activity'].every((t) => qaTitles.map((x) => x.trim()).includes(t)), qaTitles.join('|'));

const cardTitles = await page.locator('.fsh-detail-card-title').allTextContents();
const expectedTitles = ['Subscription', 'System status', 'Recent audits', 'Usage by resource', 'Quick actions', 'Live feed'];
ok('all 6 widget sections', expectedTitles.every((t) => cardTitles.map((x) => x.trim()).includes(t)), cardTitles.join('|'));

const bodyText = await page.locator('body').innerText();
ok('greeting header rendered', /Good (morning|afternoon|evening)/i.test(bodyText), 'no greeting');
const ssePulse = await page.locator('.fsh-sse-pulse').count();
ok('SSE live pulse present', ssePulse > 0, 'no .fsh-sse-pulse');

// --- nav section caption font parity
const captionFont = await page.locator('.fsh-nav-section-caption').first().evaluate((el) => getComputedStyle(el).fontFamily);
const navTextFont = await page.locator('.fsh-nav-text').first().evaluate((el) => getComputedStyle(el).fontFamily);
ok('nav section caption font == nav text font', captionFont === navTextFont, `caption=${captionFont} nav=${navTextFont}`);
const captionProps = await page.locator('.fsh-nav-section-caption').first().evaluate((el) => {
  const s = getComputedStyle(el);
  return { size: s.fontSize, weight: s.fontWeight, transform: s.textTransform, spacing: s.letterSpacing };
});
ok('caption not uppercase (React parity)', captionProps.transform === 'none', JSON.stringify(captionProps));

// --- topbar avatar tile (user menu shows profile image)
const tileHasImg = await page.locator('.fsh-user-menu-tile img.fsh-user-menu-avatar').count();
ok('topbar user tile shows avatar <img>', tileHasImg > 0, 'no avatar img in user tile');

// --- chat page + unread badge
await page.goto(`${BASE}/chat`, { waitUntil: 'domcontentloaded' });
await page.waitForTimeout(2500);
const chatOk = await page.locator('.fsh-chat-messages, .fsh-chat-channel, .mud-navmenu').first().isVisible().catch(() => false);
ok('chat page loads', chatOk);

const badge = page.locator('.fsh-chat-unread-badge');
const badgeCount = await badge.count();
if (badgeCount > 0) {
  const style = await badge.first().evaluate((el) => {
    const s = getComputedStyle(el);
    const r = el.getBoundingClientRect();
    const btn = el.parentElement.querySelector('button').getBoundingClientRect();
    return { top: s.top, right: s.right, badgeTop: r.top, badgeBottom: r.bottom, btnTop: btn.top, btnBottom: btn.bottom, btnLeft: btn.left, btnRight: btn.right, text: el.textContent };
  });
  ok('badge positioned against button (not floating off)', style.badgeTop >= style.btnTop - 1 && style.badgeBottom <= style.btnBottom + 1, JSON.stringify(style));
  console.log(`  [info] badge style=${style.top} ${style.right} text=${style.text}`);

  // mark-read: if a channel shows unread, open it and expect the badge to clear/drop
  const unreadChannel = page.locator('.fsh-chat-channel:has([class*="unread"])').first();
  const unreadChannels = await unreadChannel.count();
  if (unreadChannels > 0) {
    const before = badge.first().textContent().catch(() => '');
    await unreadChannel.click();
    await page.waitForTimeout(1500);
    const afterCount = await badge.count();
    const after = afterCount > 0 ? await badge.first().textContent() : '0';
    ok('mark-read updates badge', afterCount === 0 || Number(after) < Number(before), `before=${before} after=${after}`);
  } else {
    console.log('  [info] no unread channels — mark-read badge-drop check skipped (n/a)');
  }
} else {
  console.log('  [info] no unread badge rendered (0 unread) — position check skipped');
}

// --- global error hygiene
ok('no console errors', consoleErrors.length === 0, consoleErrors.join(' | ').slice(0, 300));
ok('no page errors', pageErrors.length === 0, pageErrors.join(' | ').slice(0, 300));
ok('no failed API requests', apiFailures.length === 0, apiFailures.join(' | ').slice(0, 300));

await browser.close();
console.log(`\n[probe] ${fails.length === 0 ? 'ALL CHECKS PASSED' : fails.length + ' FAILURES'}`);
process.exit(fails.length === 0 ? 0 : 1);