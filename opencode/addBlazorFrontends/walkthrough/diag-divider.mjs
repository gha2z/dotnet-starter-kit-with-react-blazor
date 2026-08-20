// diag-divider.mjs — dump the rail divider's rendered markup + classes on dashboard-blazor /chat.
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const { APPS } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'config.mjs')).href);
const { login } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'auth.mjs')).href);

const browser = await chromium.launch({ headless: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();
page.on('pageerror', (e) => console.log('pageerror:', e.message));
const res = await login(page, APPS.dashboard.blazor, {});
console.log('login:', res.ok ? 'OK' : 'FAIL');
await page.goto('http://localhost:5176/chat', { waitUntil: 'domcontentloaded' });
await page.waitForTimeout(4500);
const report = await page.evaluate(() => {
  const hr = document.querySelector('.fsh-chat-rail hr');
  if (!hr) return { missing: true };
  const s = getComputedStyle(hr);
  const r = hr.getBoundingClientRect();
  return {
    outerHTML: hr.outerHTML.slice(0, 400),
    className: hr.className,
    inlineStyle: hr.getAttribute('style'),
    computedFlex: s.flex,
    height: Math.round(r.height),
    top: Math.round(r.top),
  };
});
console.log(JSON.stringify(report, null, 2));
await ctx.close();
await browser.close();