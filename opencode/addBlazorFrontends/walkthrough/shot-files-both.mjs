// shot-files-both.mjs — capture React (5174) + Blazor (5176) files pages, desktop + mobile.
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
import { pathToFileURL } from 'node:url';
import path from 'node:path';
import fs from 'node:fs';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

const OUT = path.join(import.meta.dirname, 'evidence', 'p4-files');
fs.mkdirSync(OUT, { recursive: true });

async function loginAndShoot(base, tag) {
  const b = await chromium.launch({ ignoreHTTPSErrors: true });
  const ctx = await b.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
  const p = await ctx.newPage();
  await p.goto(`${base}/login`, { waitUntil: 'load' });
  await p.waitForTimeout(5000);
  // React login: email+password only (tenant from config); Blazor: tenant+email+password
  const tenant = p.getByLabel('Tenant', { exact: false });
  if (await tenant.count()) { await tenant.first().fill('acme'); }
  await p.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
  await p.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await p.getByLabel('Password', { exact: false }).first().blur();
  await p.waitForTimeout(400);
  await p.getByRole('button', { name: /sign in/i }).first().click();
  await p.waitForURL((u) => !u.pathname.toLowerCase().includes('login'), { timeout: 45000 });
  await p.goto(`${base}/files`, { waitUntil: 'load' });
  await p.waitForTimeout(5000);
  await p.screenshot({ path: path.join(OUT, `${tag}-desktop.png`) });
  // mobile
  const mctx = await b.newContext({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true, ignoreHTTPSErrors: true });
  const mp = await mctx.newPage();
  await mp.goto(`${base}/login`, { waitUntil: 'load' });
  await mp.waitForTimeout(5000);
  const mtenant = mp.getByLabel('Tenant', { exact: false });
  if (await mtenant.count()) { await mtenant.first().fill('acme'); }
  await mp.getByLabel('Email', { exact: false }).first().fill('admin@acme.com');
  await mp.getByLabel('Password', { exact: false }).first().fill('Password123!');
  await mp.getByLabel('Password', { exact: false }).first().blur();
  await mp.waitForTimeout(400);
  await mp.getByRole('button', { name: /sign in/i }).first().click();
  await mp.waitForURL((u) => !u.pathname.toLowerCase().includes('login'), { timeout: 45000 });
  await mp.goto(`${base}/files`, { waitUntil: 'load' });
  await mp.waitForTimeout(5000);
  await mp.screenshot({ path: path.join(OUT, `${tag}-mobile.png`) });
  await b.close();
  console.log(`shots: ${tag}-desktop.png ${tag}-mobile.png`);
}

await loginAndShoot('http://localhost:5174', 'react');
await loginAndShoot('http://localhost:5176', 'blazor');

