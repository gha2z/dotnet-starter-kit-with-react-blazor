// probe-diagnose-overview.mjs — observe overview API calls in detail after login.
// Usage: node probe-diagnose-overview.mjs [base]
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
const { APPS } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'config.mjs')).href);
const { login } = await import(pathToFileURL(path.join(import.meta.dirname, 'driver', 'lib', 'auth.mjs')).href);

const base = process.argv[2] ?? 'http://localhost:5176';
const b = await chromium.launch({ ignoreHTTPSErrors: true });
const ctx = await b.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
const p = await ctx.newPage();
const responses = [];
const failures = [];
const aborted = new Map();
p.on('response', (r) => { if (r.url().includes('/api/')) responses.push(`${r.status()} ${r.request().method()} ${r.url()}`); });
p.on('requestfailed', (r) => {
  if (r.url().includes('/api/')) {
    const err = r.failure()?.errorText ?? 'failed';
    failures.push(`${err} ${r.method()} ${r.url()}`);
    aborted.set(r.url(), err);
  }
});
const consoleErrors = [];
p.on('console', (m) => { if (m.type() === 'error') consoleErrors.push(m.text().slice(0, 200)); });

const loginRes = await login(p, APPS.dashboard.blazor, {});
console.log('login ok=', loginRes.ok, loginRes.url ?? '', loginRes.error ?? '');

await p.goto(`${base}/`, { waitUntil: 'domcontentloaded' });
await p.waitForTimeout(8000);

console.log('--- API RESPONSES (during load) ---');
console.log(responses.join('\n'));
console.log('--- API FAILED/ABORTED ---');
console.log(failures.join('\n') || '(none)');
console.log('--- BODY TEXT (first 900) ---');
console.log((await p.evaluate(() => document.body.innerText)).replace(/\s+/g, ' ').slice(0, 900));
console.log('--- STAT VALUES ---');
console.log(await p.locator('.fsh-stat-value').allTextContents());
console.log('--- GREETING ---');
console.log(await p.locator('.fsh-greeting').textContent().catch(() => '(not found)'));
console.log('--- CONSOLE ERRORS ---');
console.log(consoleErrors.join('\n') || '(none)');
await b.close();