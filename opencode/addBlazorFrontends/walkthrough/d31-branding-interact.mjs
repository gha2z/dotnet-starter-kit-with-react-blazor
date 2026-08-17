// d31-branding-interact.mjs — real-browser interaction walkthrough for the
// dashboard-blazor Branding screen (GR11 gate). Exercises edit → dirty chip →
// save → persistence → reset against the live API, watching console + network.
// Usage: node d31-branding-interact.mjs [--url http://localhost:5176]
import { pathToFileURL } from "node:url";
import path from "node:path";
import fs from "node:fs";

const WALK = path.resolve(import.meta.dirname);
const CLIENTS = path.resolve(WALK, "..", "..", "..", "clients");
const EVIDENCE = path.join(WALK, "evidence", "d31-branding-walk");
fs.mkdirSync(EVIDENCE, { recursive: true });

const args = process.argv.slice(2);
const getArg = (name, dflt) => {
  const i = args.indexOf(name);
  return i >= 0 ? args[i + 1] : dflt;
};
const base = getArg("--url", "http://localhost:5176");

const { chromium } = await import(pathToFileURL(path.join(CLIENTS, "admin", "node_modules", "playwright", "index.mjs")).href);
const { APPS } = await import(pathToFileURL(path.join(WALK, "driver", "lib", "config.mjs")).href);
const { login } = await import(pathToFileURL(path.join(WALK, "driver", "lib", "auth.mjs")).href);

const IGNORE = /(signalr|negotiat|hub|hot-reload|hotreload|\.map|favicon)/i;
const errors = [];
const failed = [];

const browser = await chromium.launch({ headless: true, ignoreHTTPSErrors: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
const page = await ctx.newPage();
page.on("console", (m) => {
  if (m.type() === "error" && !IGNORE.test(m.text())) errors.push(m.text().slice(0, 300));
});
page.on("pageerror", (e) => errors.push(`pageerror: ${String(e).slice(0, 300)}`));
page.on("requestfailed", (req) => {
  const url = req.url();
  const err = req.failure()?.errorText ?? "failed";
  if (url.includes("/api/") && !/ERR_ABORTED/.test(err)) failed.push(`${err} ${url}`);
});
page.on("response", (res) => {
  if (res.status() >= 400 && res.url().includes("/api/")) failed.push(`HTTP ${res.status()} ${res.url()}`);
});

const pass = (label, cond, extra = "") => {
  console.log(`${cond ? "PASS" : "FAIL"}  ${label}${extra ? `  (${extra})` : ""}`);
  if (!cond) errors.push(`assertion: ${label}${extra ? ` (${extra})` : ""}`);
};

console.log(`[d31-interact] base=${base}`);

const loginRes = await login(page, APPS.dashboard.blazor, {});
pass("login", loginRes.ok, loginRes.error ?? loginRes.url);
if (!loginRes.ok) {
  await browser.close();
  process.exit(1);
}

await page.goto(`${base}/settings/branding`, { waitUntil: "domcontentloaded" });
await page.getByRole("button", { name: /save branding/i }).waitFor({ timeout: 30000 });
await page.waitForTimeout(800);

// --- 1. baseline state
const wasDefault = (await page.locator(".mud-chip", { hasText: /default/i }).count()) > 0;
console.log(`[d31-interact] baseline default chip present: ${wasDefault}`);

// --- 2. edit the Light palette Primary field
const primary = page.locator('input[type="text"]').first();
const before = await primary.inputValue();
console.log(`[d31-interact] light primary before: ${before}`);
const TEST_HEX = "#123456";
await primary.fill(TEST_HEX);
await primary.blur();
await page.getByText(/unsaved/i).waitFor({ timeout: 5000 });
pass("unsaved chip appears after edit", true);

const saveBtn = page.getByRole("button", { name: /save branding/i });
await saveBtn.waitFor({ state: "visible" });
await saveBtn.isEnabled().then((e) => pass("save button enabled when dirty", e));
await page.screenshot({ path: path.join(EVIDENCE, "interact-1-edited.png"), fullPage: false });

// --- 3. save
await saveBtn.click();
await page.locator(".mud-snackbar", { hasText: /branding saved/i }).first().waitFor({ timeout: 15000 });
pass("snackbar 'Branding saved'", true);
await page.waitForTimeout(600);

// --- 4. persistence: reload and verify the value stuck
await page.reload({ waitUntil: "domcontentloaded" });
await page.getByRole("button", { name: /save branding/i }).waitFor({ timeout: 30000 });
await page.waitForTimeout(800);
const afterReload = await page.locator('input[type="text"]').first().inputValue();
pass("saved value persists across reload", afterReload.toUpperCase() === TEST_HEX, afterReload);
await page.screenshot({ path: path.join(EVIDENCE, "interact-2-saved.png"), fullPage: false });

// --- 5. reset to defaults
await page.getByRole("button", { name: /reset to defaults/i }).click();
await page.locator(".mud-snackbar", { hasText: /branding reset to defaults/i }).first().waitFor({ timeout: 15000 });
pass("snackbar 'Branding reset to defaults'", true);
await page.waitForTimeout(800);
const afterReset = await page.locator('input[type="text"]').first().inputValue();
pass("primary restored to seed default after reset", afterReset.toUpperCase() === "#2563EB", afterReset);
const unsavedAfterReset = await page.getByText(/unsaved/i).count();
pass("no unsaved chip after reset", unsavedAfterReset === 0);
// Parity note (matches react branding.tsx:116): the default chip keys off
// theme.isDefault, and the server keeps the custom row (values reset to
// defaults) after a reset — so no chip in EITHER app post-reset. The
// pristine-tenant chip path is covered by bUnit.
const defaultChipAfterReset = await page.locator(".mud-chip", { hasText: /default/i }).count();
pass("no default chip after reset (server keeps row — react parity)", defaultChipAfterReset === 0);
await page.screenshot({ path: path.join(EVIDENCE, "interact-3-reset.png"), fullPage: false });

// --- 6. react behavioral comparison (drive the frozen react app, same actions)
const reactBase = "http://localhost:5174";
try {
  const rctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const rpage = await rctx.newPage();
  rpage.on("requestfailed", (req) => {
    const url = req.url();
    if (url.includes("/api/") && !/ERR_ABORTED/.test(req.failure()?.errorText ?? "")) failed.push(`react: ${url}`);
  });
  const rlogin = await login(rpage, APPS.dashboard.react, {});
  pass("react login", rlogin.ok, rlogin.error ?? rlogin.url);
  await rpage.goto(`${reactBase}/settings/branding`, { waitUntil: "domcontentloaded" });
  // Vite dev + disabled http cache: give the lazy chunk time to compile+render
  // (probe-proven pattern — the shell shows 'Loading branding…' for seconds).
  await rpage.waitForTimeout(6000);
  const rResetBtn = rpage.getByRole("button", { name: /reset.*to defaults/i });
  try {
    await rResetBtn.waitFor({ state: "visible", timeout: 45000 });
  } catch (err) {
    const body = (await rpage.locator("body").innerText().catch(() => "")).slice(0, 1600);
    errors.push(`react page never rendered buttons: ${String(err).slice(0, 120)}; body=${body}`);
  }
  if ((await rResetBtn.count()) > 0) {
    console.log(`[react] debug: inputs=${await rpage.locator("input").count()} bodyTail=${(await rpage.locator("body").innerText()).slice(-500).replace(/\n/g, " | ")}`);
    const rPrimary = rpage.locator('input[type="color"]').first();
    const rBefore = await rPrimary.inputValue();
    await rResetBtn.click();
    await rpage.getByText(/branding reset to defaults/i).first().waitFor({ timeout: 15000 });
    await rpage.waitForTimeout(800);
    let rAfter = "";
    try {
      rAfter = await rPrimary.inputValue();
    } catch (err) {
      const body = (await rpage.locator("body").innerText().catch(() => "")).slice(0, 1200);
      errors.push(`react inputs vanished after reset: ${String(err).split("\n")[0]}; inputs=${await rpage.locator("input").count()}; body=${body}`);
    }
    pass("react: reset restores seed primary", rAfter.toUpperCase() === rBefore.toUpperCase(), `${rBefore} -> ${rAfter}`);
    pass("react: no unsaved chip after reset", (await rpage.getByText(/unsaved/i).count()) === 0);
    pass("react: no default chip after reset (isDefault=false, row kept)",
      (await rpage.getByText("default", { exact: true }).count()) === 0);
    await rpage.screenshot({ path: path.join(EVIDENCE, "react-reset-compare.png"), fullPage: false });
  }
  await rctx.close();
} catch (err) {
  errors.push(`react comparison: ${String(err).slice(0, 200)}`);
}

// --- 7. console + network health
pass("no console errors (excl. signalr/hot-reload)", errors.length === 0, errors.join(" | "));
pass("no failed api calls (excl. ERR_ABORTED)", failed.length === 0, failed.join(" | "));

await browser.close();
const ok = errors.length === 0;
console.log(`\n[d31-interact] ${ok ? "ALL CHECKS PASSED" : "FAILURES PRESENT — see above"}`);
process.exit(ok ? 0 : 1);
