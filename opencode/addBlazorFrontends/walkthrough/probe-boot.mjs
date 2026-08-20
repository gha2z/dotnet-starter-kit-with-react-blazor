// probe-boot.mjs — wait for a Blazor WASM app to finish bootstrapping; report console + network.
// Usage: node probe-boot.mjs <base-url> [waitMs]
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const base = process.argv[2] ?? 'http://localhost:5176';
const waitMs = Number(process.argv[3] ?? 25000);
const b = await chromium.launch({ ignoreHTTPSErrors: true });
const p = await b.newPage();
const logs = [];
const fails = [];
const http = [];
p.on('console', (m) => logs.push(`[${m.type()}] ${m.text()}`));
p.on('pageerror', (e) => logs.push(`[pageerror] ${e}`));
p.on('requestfailed', (r) => fails.push(`${r.failure()?.errorText} ${r.url()}`));
p.on('response', (r) => { if (r.status() >= 400) http.push(`${r.status()} ${r.url()}`); });
await p.goto(`${base}/login`, { waitUntil: 'domcontentloaded' });
await p.waitForTimeout(waitMs);
console.log('URL:', p.url());
console.log('INPUTS:', await p.locator('input').count());
console.log('--- BODY ---');
console.log((await p.evaluate(() => document.body.innerText)).slice(0, 800));
console.log('--- CONSOLE/PAGEERROR ---');
console.log(logs.slice(0, 40).join('\n'));
console.log('--- FAILED REQUESTS ---');
console.log(fails.slice(0, 20).join('\n'));
console.log('--- HTTP >=400 ---');
console.log(http.slice(0, 20).join('\n'));
await b.close();