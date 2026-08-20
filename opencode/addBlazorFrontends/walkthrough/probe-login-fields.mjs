// probe-login-fields.mjs — dump the live login page structure for the given app.
// Usage: node probe-login-fields.mjs <base-url>
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const base = process.argv[2] ?? 'http://localhost:5176';
const b = await chromium.launch({ ignoreHTTPSErrors: true });
const p = await b.newPage();
await p.goto(`${base}/login`, { waitUntil: 'domcontentloaded' });
await p.waitForTimeout(5000);
console.log('URL:', p.url());
console.log('--- BODY TEXT ---');
console.log((await p.evaluate(() => document.body.innerText)).slice(0, 1500));
console.log('--- INPUTS ---');
console.log(await p.locator('input').count());
console.log('--- LABELS ---');
console.log(await p.evaluate(() => [...document.querySelectorAll('label, .mud-input-label')].map((x) => x.textContent.trim()).join(' | ')));
console.log('--- ARIA ---');
console.log(await p.evaluate(() => [...document.querySelectorAll('[aria-label], [placeholder]')].map((x) => x.getAttribute('aria-label') || x.getAttribute('placeholder')).join(' | ')));
await b.close();