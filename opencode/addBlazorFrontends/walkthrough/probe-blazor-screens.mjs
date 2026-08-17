// probe-blazor-screens.mjs — live DOM text + network probe for parity-DIFF screens.
// Usage: node probe-blazor-screens.mjs [--app dashboard|admin] [--url http://localhost:5176]
// Prints per-screen: final URL, h1/title text, main-region text (truncated), empty-state markers,
// and failed/aborted API requests observed during load.
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const { APPS } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'config.mjs')).href);
const { login } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'auth.mjs')).href);

const args = process.argv.slice(2);
const getArg = (name, dflt) => {
  const i = args.indexOf(name);
  return i >= 0 ? args[i + 1] : dflt;
};
const app = getArg('--app', 'dashboard');
const base = getArg('--url', app === 'admin' ? 'http://localhost:5175' : 'http://localhost:5176');

const SCREENS = {
  dashboard: [
    { id: 'D17', path: '/identity/users', expect: 'user' },
    { id: 'D24', path: '/catalog/categories', expect: 'categor' },
    { id: 'D25', path: '/catalog/products', expect: 'product' },
    { id: 'D23', path: '/catalog/brands', expect: 'brand' },
    { id: 'D11', path: '/system/trash', expect: 'trash' },
  ],
  admin: [
    { id: 'A17', path: '/identity/users', expect: 'user' },
  ],
}[app];

const CREDS = {
  dashboard: { tenant: 'acme', email: 'admin@acme.com', password: 'Password123!' },
  admin: { tenant: 'root', email: 'superadmin@root.com', password: 'Password123!' },
};

const browser = await chromium.launch({ headless: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();

const failed = new Map();
page.on('requestfailed', (req) => {
  const url = req.url();
  if (url.includes('/api/')) {
    failed.set(url, req.failure()?.errorText ?? 'failed');
  }
});
page.on('response', (res) => {
  if (res.status() >= 400 && res.url().includes('/api/')) {
    failed.set(res.url(), `HTTP ${res.status()}`);
  }
});

// --- login (driver's known-good helper)
const appCfg = APPS[app][app === 'admin' ? 'blazor' : 'blazor'];
const loginRes = await login(page, APPS[app].blazor, { });
console.log(`[probe] login ok=${loginRes.ok} url=${loginRes.url} ${loginRes.error ?? ''}`);

for (const s of SCREENS) {
  failed.clear();
  await page.goto(base + s.path, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(3000);
  const title = (await page.locator('h1, h2').first().textContent().catch(() => '')).trim();
  const mainText = (await page.locator('body').innerText()).slice(0, 900).replace(/\s+/g, ' ').trim();
  const emptyMarks = ['No users yet', 'No users found', 'No categories yet', 'No categories found',
    'No products yet', 'No products found', 'No brands yet', 'No trashed', 'error', 'Error', 'Unauthorized',
    'You do not have permission', '403'];
  const hits = emptyMarks.filter((m) => mainText.includes(m));
  console.log(`\n=== ${s.id} ${s.path}`);
  console.log(`  title: ${title}`);
  console.log(`  empty/error markers: ${hits.length ? hits.join(' | ') : '(none — populated content present)'}`);
  console.log(`  main text: ${mainText.slice(0, 400)}`);
  console.log(`  api failures (${failed.size}):`);
  for (const [u, err] of failed) console.log(`    ${err} ${u}`);
}

await browser.close();
console.log('\n[probe] done');