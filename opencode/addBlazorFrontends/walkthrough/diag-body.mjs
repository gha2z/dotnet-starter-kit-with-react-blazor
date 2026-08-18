// diag-body.mjs — dump rendered body text of / and /settings/profile on the running app.
// Usage: node diag-body.mjs
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const { APPS } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'config.mjs')).href);
const { login } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'auth.mjs')).href);

const BASE = 'http://localhost:5176';
const browser = await chromium.launch({ headless: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();

const dump = async (label, pathName) => {
  await page.goto(BASE + pathName, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(4000);
  const h1 = await page.locator('h1').count();
  const body = (await page.locator('body').innerText()).replace(/\s+/g, ' ').trim();
  const buttons = await page.locator('button').allInnerTexts();
  console.log(`\n===== ${label} (${pathName}) h1=${h1} =====`);
  console.log('BODY:', body.slice(0, 2500));
  console.log('BUTTONS:', JSON.stringify(buttons.map((b) => b.replace(/\s+/g, ' ').trim()).filter(Boolean).slice(0, 25)));
};

try {
  const loginRes = await login(page, APPS.dashboard.blazor, {});
  console.log('login:', loginRes.ok ? 'OK' : `FAIL ${loginRes.error ?? loginRes.url}`);
  await dump('MAIN', '/');
  await dump('PROFILE', '/settings/profile');
} finally {
  await browser.close();
}