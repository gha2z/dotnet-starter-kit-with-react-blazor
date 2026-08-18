// diag-layout.mjs — screenshots + computed-style checks of sidebar/section-nav on both Blazor apps.
// Usage: node diag-layout.mjs
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const { APPS } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'config.mjs')).href);
const { login } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'auth.mjs')).href);

const OUT = path.join(import.meta.dirname, 'tmp');
const browser = await chromium.launch({ headless: true });

async function inspect(appKey, port, label) {
  const base = `http://localhost:${port}`;
  for (const width of [1440, 1100, 900]) {
    const ctx = await browser.newContext({ viewport: { width, height: 900 } });
    const page = await ctx.newPage();
    try {
      const res = await login(page, APPS[appKey].blazor, {});
      console.log(`\n[${label} w=${width}] login: ${res.ok ? 'OK' : 'FAIL'}`);
      for (const p of ['/settings/profile', '/']) {
        await page.goto(base + p, { waitUntil: 'domcontentloaded' });
        await page.waitForTimeout(3500);
        const report = await page.evaluate(() => {
          const vis = (el) => {
            if (!el) return 'MISSING';
            const s = getComputedStyle(el);
            const r = el.getBoundingClientRect();
            return `display=${s.display} vis=${s.visibility} w=${Math.round(r.width)} x=${Math.round(r.x)}`;
          };
          const aside = document.querySelector('aside.fsh-sidebar');
          const secNav = document.querySelector('nav[aria-label="Settings sections"]');
          const secParent = secNav ? secNav.closest('div') : null;
          const backdrop = document.querySelector('.fsh-sidebar-backdrop');
          const h1 = document.querySelector('h1');
          return {
            aside: vis(aside),
            sectionNav: vis(secNav),
            sectionParent: vis(secParent),
            backdrop: backdrop ? 'PRESENT' : 'absent',
            h1: h1 ? h1.textContent : 'none',
          };
        });
        console.log(`  ${p}:`, JSON.stringify(report));
        await page.screenshot({ path: path.join(OUT, `${label}-${width}-${p.replace(/\//g, '_')}.png`), fullPage: false });
      }
    } catch (e) {
      console.log(`[${label} w=${width}] ERROR: ${e.message}`);
    } finally {
      await ctx.close();
    }
  }
}

await inspect('admin', 5175, 'admin');
await inspect('dashboard', 5176, 'dash');
await browser.close();
console.log('\nScreenshots saved to', OUT);