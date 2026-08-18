// probe-settings-theme.mjs — functional QA for dashboard-blazor settings pages,
// main page and light/dark theme switching (React parity focus).
// Usage: node probe-settings-theme.mjs
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const { APPS } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'config.mjs')).href);
const { login } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'auth.mjs')).href);

const BASE = 'http://localhost:5176';

const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok, detail });
  console.log(`${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` — ${detail}` : ''}`);
};

const browser = await chromium.launch({ headless: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();

const consoleErrors = [];
page.on('console', (msg) => {
  if (msg.type() === 'error') consoleErrors.push(`console: ${msg.text()}`);
});
page.on('pageerror', (err) => consoleErrors.push(`pageerror: ${err.message}`));

const apiFailures = new Set();
page.on('requestfailed', (req) => {
  const url = req.url();
  if (url.includes('/api/') && !url.includes('/sse/stream')) {
    apiFailures.add(`req: ${req.failure()?.errorText ?? 'failed'} ${url}`);
  }
});
page.on('response', (res) => {
  const url = res.url();
  if (res.status() >= 400 && url.includes('/api/') && !url.includes('/sse/stream')) {
    apiFailures.add(`http ${res.status()} ${url}`);
  }
});

const gotoWait = async (path) => {
  await page.goto(BASE + path, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('h1', { timeout: 30000 }).catch(() => {});
  await page.waitForTimeout(800);
};

const bodyText = async () => (await page.locator('body').innerText()).replace(/\s+/g, ' ');

const ls = async (key) => page.evaluate((k) => localStorage.getItem(k), key);
const isDark = async () => page.evaluate(() => document.body.classList.contains('mud-theme-dark'));
const bgColor = async () => page.evaluate(() => getComputedStyle(document.body).backgroundColor);

try {
  // ---- login (driver's known-good helper)
  const loginRes = await login(page, APPS.dashboard.blazor, {});
  check('login', loginRes.ok, loginRes.error ?? loginRes.url);

  // ================= MAIN PAGE (/) =================
  await gotoWait('/');
  const mainBody = await bodyText();
  check('main: welcome header', mainBody.includes('Welcome'));
  check('main: stat tiles', ['Plans', 'Invoices', 'Outstanding', 'Usage'].filter((t) => mainBody.includes(t)).length >= 2,
    'expected 2+ stat labels');
  check('main: quick actions', mainBody.includes('Invite users') && mainBody.includes('Browse catalog'));
  check('main: subscription card', mainBody.includes('Subscription'));
  check('main: live activity (SSE)', mainBody.includes('Live activity') || mainBody.includes('Recent activity'));

  // ================= SETTINGS TOUR =================
  const settingsPages = [
    { id: 'D27', path: '/settings', markers: ['Profile', 'Security', 'Appearance', 'Branding', 'Notifications', 'API keys'] },
    { id: 'D28', path: '/settings/profile', markers: ['First name', 'Last name', 'Email', 'Phone number', 'Photo', 'Change avatar', 'Save changes'] },
    { id: 'D29', path: '/settings/security', markers: ['Password', 'Change password', 'Active sessions'] },
    { id: 'D30', path: '/settings/appearance', markers: ['Theme', 'Accent colour', 'Typography', 'Density', 'Motion', 'Light', 'Dark'] },
    { id: 'D31', path: '/settings/branding', markers: ['Light palette', 'Dark palette', 'Brand assets', 'Logo URL', 'Favicon URL', 'Save branding'] },
    { id: 'D32', path: '/settings/notifications', markers: ['Notification preferences', "aren't tunable yet", 'Open notifications bell'] },
    { id: 'D33', path: '/settings/api-keys', markers: ['API keys', "aren't available yet", 'View roadmap'] },
  ];
  for (const s of settingsPages) {
    await gotoWait(s.path);
    const body = await bodyText();
    const missing = s.markers.filter((m) => !body.includes(m));
    check(`${s.id} ${s.path} markers`, missing.length === 0, missing.length ? `missing: ${missing.join(', ')}` : '');
  }

  // settings sidebar visible on settings pages
  await gotoWait('/settings/profile');
  const navPresent = await page.locator('nav, .mud-navmenu').count();
  check('settings: nav sidebar present', navPresent > 0);

  // ================= PROFILE SAVE ROUND-TRIP =================
  await gotoWait('/settings/profile');
  const firstNameInput = page.getByLabel('First name');
  const saveBtn = page.getByRole('button', { name: 'Save changes' });
  const originalFirst = await firstNameInput.inputValue().catch(() => '');
  check('profile: save disabled when clean', await saveBtn.isDisabled());

  const probeFirst = `Parity ${Date.now() % 100000}`;
  await firstNameInput.fill(probeFirst);
  await page.waitForTimeout(500);
  check('profile: save enabled while typing (Immediate)', !(await saveBtn.isDisabled()),
    `typed "${probeFirst}"`);
  await saveBtn.click();
  await page.waitForSelector('.mud-snackbar', { timeout: 10000 }).catch(() => {});
  const snackbarText = (await page.locator('.mud-snackbar').allInnerTexts()).join(' ').replace(/\s+/g, ' ');
  check('profile: save snackbar', snackbarText.includes('Profile saved'), snackbarText || 'no snackbar');

  await gotoWait('/settings/profile');
  const persisted = await page.getByLabel('First name').inputValue();
  check('profile: save persisted after reload', persisted === probeFirst, `field="${persisted}" expected="${probeFirst}"`);

  // restore original
  await page.getByLabel('First name').fill(originalFirst);
  await page.waitForTimeout(300);
  await page.getByRole('button', { name: 'Save changes' }).click();
  await page.waitForTimeout(1200);
  await gotoWait('/settings/profile');
  const restored = await page.getByLabel('First name').inputValue();
  check('profile: restore original value', restored === originalFirst, `field="${restored}" expected="${originalFirst}"`);

  // ================= SECURITY: CHANGE PASSWORD DIALOG =================
  await gotoWait('/settings/security');
  await page.getByRole('button', { name: 'Change password' }).click();
  await page.waitForSelector('.mud-dialog', { timeout: 10000 });
  const dialogVisible = await page.locator('.mud-dialog:visible').count();
  check('security: change-password dialog opens', dialogVisible > 0);
  const updateBtn = page.locator('.mud-dialog').getByRole('button', { name: /Update password|Save/i });
  const pw1 = page.locator('.mud-dialog input[type=password]').nth(0);
  const pw2 = page.locator('.mud-dialog input[type=password]').nth(1);
  const pw3 = page.locator('.mud-dialog input[type=password]').nth(2);
  check('security: dialog has 3 password fields', await pw1.count() === 1 && await pw2.count() === 1 && await pw3.count() === 1);
  const updateDisabledBefore = await updateBtn.isDisabled().catch(() => 'n/a');
  await pw1.fill('WrongPass1!');
  await pw2.fill('NewPass123!');
  await pw3.fill('NewPass123!');
  await page.waitForTimeout(400);
  const updateDisabledAfter = await updateBtn.isDisabled().catch(() => 'n/a');
  check('security: dialog submit enables while typing (Immediate)',
    updateDisabledBefore === true && updateDisabledAfter === false,
    `before=${updateDisabledBefore} after=${updateDisabledAfter}`);
  await page.locator('.mud-dialog').getByRole('button', { name: 'Cancel' }).click();
  await page.waitForTimeout(500);

  // sessions table renders rows
  const sessionRows = await page.locator('.mud-table-body .mud-table-row, tbody tr').count();
  check('security: sessions table has rows', sessionRows >= 1, `rows=${sessionRows}`);
  const revokeButtons = await page.locator('button').filter({ hasText: /Revoke/i }).count();
  check('security: revoke buttons present', revokeButtons >= 1, `revoke=${revokeButtons}`);

  // ================= APPEARANCE: THEME MODE SWITCHING =================
  await gotoWait('/settings/appearance');

  // read current state to restore later
  const storedThemeBefore = await ls('fsh.theme');
  const darkBefore = await isDark();

  // radios: order is Light, System, Dark per markup
  const radios = page.locator('input[type=radio]');
  const radioCount = await radios.count();
  check('appearance: 3 theme radios', radioCount === 3, `radios=${radioCount}`);

  await radios.nth(2).check();
  await page.waitForTimeout(600);
  check('appearance: Dark radio → body dark class', await isDark());
  check('appearance: Dark persisted to localStorage', (await ls('fsh.theme')) === 'dark', `fsh.theme=${await ls('fsh.theme')}`);

  await page.reload({ waitUntil: 'domcontentloaded' });
  await page.waitForSelector('h1', { timeout: 30000 }).catch(() => {});
  await page.waitForTimeout(1500);
  check('appearance: dark persists across reload', await isDark(), `dark=${await isDark()}`);

  await radios.nth(0).check();
  await page.waitForTimeout(600);
  check('appearance: Light radio → body light class', !(await isDark()));
  check('appearance: Light persisted to localStorage', (await ls('fsh.theme')) === 'light', `fsh.theme=${await ls('fsh.theme')}`);

  // ================= APPEARANCE: ACCENT / FONT / DENSITY / MOTION =================
  const accentBefore = await ls('fsh.accent');
  const accentCards = page.locator('button[aria-pressed]');
  const accentCount = await accentCards.count();
  check('appearance: accent cards render', accentCount >= 6, `cards=${accentCount}`);
  // click the second accent (first is current selection)
  const targetAccent = await accentCards.nth(1).getAttribute('aria-pressed');
  await accentCards.nth(1).click();
  await page.waitForTimeout(500);
  const accentAfter = await ls('fsh.accent');
  check('appearance: accent click persists', accentAfter !== null && accentAfter !== '', `fsh.accent=${accentAfter}`);

  const fontCards = page.locator('.fsh-font-sample').count();
  check('appearance: font cards render', (await fontCards) >= 10, `fonts=${await fontCards}`);

  const densitySwitch = page.getByLabel('Compact density');
  const densityBefore = await ls('fsh.density');
  if (await densitySwitch.count()) {
    await densitySwitch.click();
    await page.waitForTimeout(400);
    const densityAfter = await ls('fsh.density');
    check('appearance: density switch persists', densityAfter !== null, `fsh.density=${densityAfter}`);
  } else {
    check('appearance: density switch present', false, 'not found');
  }

  const motionSwitch = page.getByLabel('Reduce motion');
  const motionBefore = await ls('fsh.reduce-motion');
  if (await motionSwitch.count()) {
    await motionSwitch.click();
    await page.waitForTimeout(400);
    const motionAfter = await ls('fsh.reduce-motion');
    check('appearance: motion switch persists', motionAfter !== null, `fsh.reduce-motion=${motionAfter}`);
  } else {
    check('appearance: motion switch present', false, 'not found');
  }

  // restore accent to prior value
  if (accentBefore) {
    const cards = page.locator('button[aria-pressed]');
    const count = await cards.count();
    for (let i = 0; i < count; i++) {
      const label = (await cards.nth(i).innerText()).trim().split('\n')[0];
      if (label === accentBefore || (await cards.nth(i).innerText()).includes(accentBefore)) {
        await cards.nth(i).click();
        await page.waitForTimeout(400);
        break;
      }
    }
  }

  // ================= TOPBAR THEME MENU =================
  await gotoWait('/settings/appearance');
  await page.getByRole('button', { name: 'Theme' }).click();
  await page.waitForTimeout(600);
  const menuItemDark = page.locator('.mud-menu .mud-list-item').filter({ hasText: /^Dark/ });
  check('topbar: theme menu opens with Dark item', (await menuItemDark.count()) >= 1);
  await menuItemDark.first().click();
  await page.waitForTimeout(600);
  check('topbar: Dark menu item → body dark class', await isDark());
  check('topbar: Dark menu item persists', (await ls('fsh.theme')) === 'dark', `fsh.theme=${await ls('fsh.theme')}`);
  const darkBg = await bgColor();

  // restore: back to Light via menu
  await page.getByRole('button', { name: 'Theme' }).click();
  await page.waitForTimeout(600);
  await page.locator('.mud-menu .mud-list-item').filter({ hasText: /^Light/ }).first().click();
  await page.waitForTimeout(600);
  check('topbar: Light menu item → body light class', !(await isDark()));

  // ================= BRANDING (READ-ONLY) =================
  await gotoWait('/settings/branding');
  const colorInputs = await page.locator('input[type=color]').count();
  check('branding: 18 palette color inputs', colorInputs === 18, `count=${colorInputs}`);
  const saveBranding = page.getByRole('button', { name: 'Save branding' });
  check('branding: save disabled when clean', await saveBranding.isDisabled().catch(() => 'n/a') === true);
  // typing a hex value should dirty the page live (Immediate)
  const firstHex = page.locator('input.font-mono').first();
  const origHex = await firstHex.inputValue();
  const newHex = origHex === '#1A1A1A' ? '#2B2B2B' : '#1A1A1A';
  await firstHex.fill(newHex);
  await page.waitForTimeout(400);
  check('branding: save enables while typing (Immediate)', !(await saveBranding.isDisabled()));
  await firstHex.fill(origHex);
  await page.waitForTimeout(400);
  check('branding: save disables after revert', await saveBranding.isDisabled());

  // ================= CONSOLE / API =================
  check('no console errors', consoleErrors.length === 0, consoleErrors.slice(0, 3).join(' | '));
  check('no failed api calls', apiFailures.size === 0, [...apiFailures].slice(0, 3).join(' | '));
} finally {
  await browser.close();
}

const failed = results.filter((r) => !r.ok);
console.log(`\n==== ${results.length - failed.length}/${results.length} checks passed ====`);
if (failed.length) {
  console.log('FAILED:');
  for (const f of failed) console.log(`  - ${f.name}${f.detail ? ` (${f.detail})` : ''}`);
}
process.exit(failed.length ? 1 : 0);
