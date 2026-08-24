// probe-chat-rail-shot.mjs — P1.2 visual evidence: DM rail avatars + presence dots.
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const BASE = 'http://localhost:5176';
const OUT = path.join(import.meta.dirname, 'evidence', 'chat-visual');

async function login(page, email) {
  await page.goto(`${BASE}/login`, { waitUntil: 'load' });
  await page.waitForTimeout(1500);
  await page.getByLabel('Tenant', { exact: false }).first().waitFor({ state: 'visible', timeout: 20000 });
  await page.getByLabel('Tenant', { exact: false }).first().fill('acme');
  await page.getByLabel('Email', { exact: false }).first().fill(email);
  await page.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await page.getByLabel('Password', { exact: false }).first().blur().catch(() => {});
  await page.waitForTimeout(400);
  await page.getByRole('button', { name: /sign in/i }).first().click();
  await page.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 25000 });
  await page.waitForTimeout(1500);
}

const b = await chromium.launch({ ignoreHTTPSErrors: true });
try {
  const aCtx = await b.newContext({ viewport: { width: 1440, height: 2600 } });
  const bCtx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const A = await aCtx.newPage();
  const B = await bCtx.newPage();

  await login(A, 'admin@acme.com');
  await login(B, 'alice@acme.com');
  // B sits on /chat so presence marks her online for A
  await B.goto(`${BASE}/chat`, { waitUntil: 'domcontentloaded' });
  await B.waitForTimeout(4000);

  await A.goto(`${BASE}/chat`, { waitUntil: 'domcontentloaded' });
  await A.waitForSelector('.fsh-chat-rail', { timeout: 25000 });
  await A.waitForTimeout(2500);
  const rail = A.locator('.fsh-chat-rail');
  // Scroll the DIRECT MESSAGES section into view so the shot shows the DM rows
  await A.getByText('DIRECT MESSAGES', { exact: false }).first().evaluate((el) => el.scrollIntoView({ block: 'start' }));
  await A.waitForTimeout(400);
  await rail.screenshot({ path: path.join(OUT, 'p1-rail-dm-avatars.png') });
  const avatars = await A.locator('.fsh-chat-rail .mud-avatar').count();
  const dots = await A.locator('.fsh-chat-rail [style*="mud-palette-success"]').count();
  console.log(`rail avatars=${avatars} presence-dots=${dots}`);
  console.log('shot saved: p1-rail-dm-avatars.png');
} catch (err) {
  console.log('FAIL:', String(err).slice(0, 200));
} finally {
  await b.close();
}
