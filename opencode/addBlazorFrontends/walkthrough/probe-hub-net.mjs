// probe-hub-net.mjs — observe what the Blazor app actually does on /chat:
// does the SignalR negotiate/websocket fire? any console errors?
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const BASE = 'http://localhost:5176';
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const b = await chromium.launch({ ignoreHTTPSErrors: true });
const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
const p = await ctx.newPage();

p.on('console', (m) => {
  const t = m.text();
  if (/signalr|hub|realtime|error|warn|exception|unhandled/i.test(t)) console.log(`[console:${m.type()}] ${t.slice(0, 220)}`);
});
p.on('pageerror', (e) => console.log(`[pageerror] ${String(e).slice(0, 300)}`));
p.on('request', (r) => { if (/realtime\/hub|negotiate/i.test(r.url())) console.log(`[req] ${r.method()} ${r.url().slice(0, 140)}`); });
p.on('websocket', (ws) => {
  console.log(`[ws] OPEN ${ws.url().slice(0, 120)}`);
  ws.on('close', () => console.log(`[ws] CLOSE ${ws.url().slice(0, 120)}`));
});

await p.goto(`${BASE}/login`, { waitUntil: 'load' });
await p.waitForTimeout(1500);
await p.getByLabel('Tenant', { exact: false }).first().fill('acme');
await p.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
await p.getByLabel('Password', { exact: false }).first().fill('Password123!');
await p.getByLabel('Password', { exact: false }).first().blur().catch(() => {});
await p.waitForTimeout(600);
for (const label of ['Sign in', 'Sign In', 'Login']) {
  const btn = p.getByRole('button', { name: label, exact: false }).first();
  try { await btn.waitFor({ state: 'visible', timeout: 3000 }); await btn.click(); break; } catch { /* next */ }
}
await p.waitForURL((u) => !u.pathname.toLowerCase().endsWith('/login'), { timeout: 25000 });
console.log('[nav] logged in →', p.url());

console.log('--- navigating to /chat ---');
await p.goto(`${BASE}/chat`, { waitUntil: 'load' });
await p.waitForSelector('[data-message-id]', { timeout: 25000 });
console.log('[nav] chat loaded');
// send one message as THIS user (self path known-good) then wait 4s observing ws traffic
const box = p.locator('input[aria-label="Message"]');
await box.fill(`NETPROBE-${Date.now().toString(36)}`);
await box.press('Enter');
await p.waitForTimeout(5000);
console.log('--- done ---');
await b.close();
