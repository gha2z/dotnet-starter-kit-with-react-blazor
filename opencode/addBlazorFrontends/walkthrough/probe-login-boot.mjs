import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { APPS, PLAYWRIGHT_ENTRY } from "./driver/lib/config.mjs";

const { chromium } = await import(PLAYWRIGHT_ENTRY);
const cfg = APPS.dashboard.blazor;

const browser = await chromium.launch({ headless: true, ignoreHTTPSErrors: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
const page = await ctx.newPage();

const consoleErrors = [];
page.on("console", (m) => { if (m.type() === "error") consoleErrors.push(m.text()); });
page.on("pageerror", (e) => consoleErrors.push(`pageerror: ${e.message}`));
page.on("requestfailed", (r) => consoleErrors.push(`requestfailed: ${r.url()} ${r.failure()?.errorText}`));

const t0 = Date.now();
await page.goto(`${cfg.base}/login`, { waitUntil: "domcontentloaded", timeout: 60000 });
console.log(`[probe] goto ok in ${Date.now() - t0}ms; waiting for Tenant field...`);

try {
  await page.getByLabel("Tenant").first().waitFor({ state: "visible", timeout: 90000 });
  console.log(`[probe] Tenant field visible after ${Date.now() - t0}ms`);
} catch {
  console.log(`[probe] FAILED: Tenant field never became visible (${Date.now() - t0}ms)`);
}

const html = await page.content();
console.log(`[probe] title=${await page.title()}`);
console.log(`[probe] body length=${html.length}`);
const bodyText = (await page.locator("body").innerText().catch(() => "")).slice(0, 600);
console.log(`[probe] body text: ${JSON.stringify(bodyText)}`);
console.log(`[probe] console/page errors (${consoleErrors.length}):`);
for (const e of consoleErrors.slice(0, 12)) console.log(`  - ${e.slice(0, 300)}`);

await browser.close();