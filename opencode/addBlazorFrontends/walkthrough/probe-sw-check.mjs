import { readFileSync } from "node:fs";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { APPS, PLAYWRIGHT_ENTRY } from "./driver/lib/config.mjs";

const HERE = fileURLToPath(new URL(".", import.meta.url));
const { chromium } = await import(PLAYWRIGHT_ENTRY);

const BASE = "http://localhost:5176";
const browser = await chromium.launch({ headless: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();

const failed = [];
page.on("requestfailed", (r) => failed.push(`FAILED ${r.url()} ${r.failure()?.errorText}`));
page.on("response", (r) => {
  if (r.status() >= 400) failed.push(`${r.status()} ${r.url()}`);
});

const consoleErrors = [];
page.on("console", (m) => {
  if (m.type() === "error") consoleErrors.push(m.text().slice(0, 300));
});

await page.goto(BASE + "/", { waitUntil: "domcontentloaded", timeout: 60000 });

let booted = false;
try {
  await page.getByText("Sign in", { exact: false }).first().waitFor({ state: "visible", timeout: 30000 });
  booted = true;
} catch {}
try {
  await page.getByText("Email", { exact: true }).first().waitFor({ state: "visible", timeout: 10000 });
  booted = true;
} catch {}

console.log("BOOTED:", booted, "URL:", page.url());
console.log("FAILED REQUESTS:", failed.length ? failed.join("\n") : "(none)");
console.log("CONSOLE ERRORS:", consoleErrors.length ? consoleErrors.join("\n") : "(none)");
await page.screenshot({ path: join(HERE, "evidence", "sw-probe.png"), fullPage: true });

const sw = await page.evaluate(async () => {
  const regs = await navigator.serviceWorker.getRegistrations();
  return regs.map((r) => ({ scope: r.scope, active: r.active?.scriptURL ?? null }));
}).catch(() => null);
console.log("SERVICE WORKER:", JSON.stringify(sw));

const cacheNames = await page.evaluate(async () => await caches.keys()).catch(() => null);
console.log("CACHE NAMES:", JSON.stringify(cacheNames));

let reloadRequests = [];
page.on("request", (r) => {
  if (r.resourceType() !== "serviceworker") reloadRequests.push(`${r.url().replace(BASE, "")} ${r.resourceType()}`);
});
await page.reload({ waitUntil: "domcontentloaded", timeout: 60000 });
await page.getByText("Email", { exact: true }).first().waitFor({ state: "visible", timeout: 30000 }).catch(() => {});
await page.waitForTimeout(5000);
console.log("RELOAD REQUESTS:", reloadRequests.length ? reloadRequests.join("\n") : "(none)");

const cacheContents = await page.evaluate(async () => {
  const out = {};
  for (const name of await caches.keys()) {
    const c = await caches.open(name);
    out[name] = (await c.keys()).map((r) => r.url.replace(location.origin, "")).sort();
  }
  return out;
}).catch(() => null);
console.log("CACHE CONTENTS:", JSON.stringify(cacheContents, null, 1));

const offlineTest = await ctx2OfflineCheck();
console.log("OFFLINE FALLBACK:", offlineTest);

async function ctx2OfflineCheck() {
  const ctx2 = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const p2 = await ctx2.newPage();
  await p2.goto(BASE + "/", { waitUntil: "domcontentloaded", timeout: 60000 }).catch(() => {});
  await p2.getByText("Email", { exact: true }).first().waitFor({ state: "visible", timeout: 30000 }).catch(() => {});
  await p2.waitForTimeout(5000);
  await ctx2.setOffline(true);
  await p2.reload({ waitUntil: "domcontentloaded", timeout: 30000 }).catch(() => {});
  await p2.waitForTimeout(3000);
  const body = await p2.locator("body").innerText().catch(() => "");
  const ok = body.toLowerCase().includes("offline") || p2.url().includes("offline");
  await ctx2.close();
  return `${ok} — body: ${body.slice(0, 120).replace(/\s+/g, " ")}`;
}

await browser.close();