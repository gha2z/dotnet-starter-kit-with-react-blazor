// diag-chat-rail.mjs — measure the chat rail flex layout on dashboard-blazor.
// Usage: node diag-chat-rail.mjs
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const { APPS } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'config.mjs')).href);
const { login } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'auth.mjs')).href);

const OUT = path.join(import.meta.dirname, 'tmp');
const browser = await chromium.launch({ headless: true });

async function measure(width, label) {
  const ctx = await browser.newContext({ viewport: { width, height: 900 } });
  const page = await ctx.newPage();
  page.on('pageerror', (e) => console.log('pageerror:', e.message));
  try {
    const res = await login(page, APPS.dashboard.blazor, {});
    console.log(`\n[${label} w=${width}] login: ${res.ok ? 'OK' : 'FAIL'}`);
    await page.goto('http://localhost:5176/chat', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(4500);
    const report = await page.evaluate(() => {
      const info = (el, label) => {
        if (!el) return { label, missing: true };
        const s = getComputedStyle(el);
        const r = el.getBoundingClientRect();
        return {
          label,
          tag: el.tagName.toLowerCase(),
          top: Math.round(r.top), bottom: Math.round(r.bottom),
          height: Math.round(r.height),
          display: s.display, flex: s.flex, flexDirection: s.flexDirection,
          minHeight: s.minHeight, overflowY: s.overflowY,
          paddingTop: s.paddingTop, marginTop: s.marginTop, justifyContent: s.justifyContent,
          alignItems: s.alignItems,
        };
      };
      const q = (sel) => document.querySelector(sel);
      const root = q('.fsh-chat-root');
      const rail = q('.fsh-chat-rail');
      const paper = rail ? rail.querySelector('.mud-paper') : null;
      const searchWrap = paper ? paper.querySelector('div.pa-3') : null;
      const divider = paper ? paper.querySelector('.mud-divider') : null;
      const list = q('.fsh-chat-channel-list');
      const captions = [...document.querySelectorAll('.fsh-chat-section .mud-typography')].slice(0, 2);
      const firstItems = [...document.querySelectorAll('.fsh-chat-section .mud-list-item')];
      const sectionEls = [...document.querySelectorAll('.fsh-chat-section')];
      const searchInput = paper ? paper.querySelector('input') : null;
      return {
        viewport: { w: innerWidth, h: innerHeight },
        root: info(root, 'root'),
        rail: info(rail, 'rail'),
        paper: info(paper, 'paper'),
        searchWrap: info(searchWrap, 'searchWrap'),
        searchInput: info(searchInput, 'searchInput'),
        divider: info(divider, 'divider'),
        list: info(list, 'list'),
        sections: sectionEls.map((el) => info(el, 'section')),
        captions: captions.map((el) => info(el, 'caption')),
        firstItems: firstItems.map((el) => info(el, 'firstItem')).slice(0, 3),
        listChildren: list ? [...list.children].map((c) => ({ tag: c.tagName, cls: c.className, h: Math.round(c.getBoundingClientRect().height), top: Math.round(c.getBoundingClientRect().top) })) : [],
      };
    });
    console.log(JSON.stringify(report, null, 2));
    await page.screenshot({ path: path.join(OUT, `chat-rail-${label}-${width}.png`) });
  } catch (e) {
    console.log(`[${label} w=${width}] ERROR: ${e.message}`);
  } finally {
    await ctx.close();
  }
}

await measure(1440, 'desktop');
await measure(900, 'tablet');
await browser.close();
console.log('\nScreenshots saved to', OUT);