// probe-react-branding.mjs — one-off dump of the react dashboard branding page state.
import { pathToFileURL } from "node:url";
import path from "node:path";
const CLIENTS = path.resolve(import.meta.dirname, "..", "..", "..", "clients");
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, "admin", "node_modules", "playwright", "index.mjs")).href);
const { APPS } = await import(pathToFileURL(path.join(import.meta.dirname, "driver", "lib", "config.mjs")).href);
const { login } = await import(pathToFileURL(path.join(import.meta.dirname, "driver", "lib", "auth.mjs")).href);

const browser = await chromium.launch({ headless: true, ignoreHTTPSErrors: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
const page = await ctx.newPage();
const api = [];
page.on("response", (res) => {
  if (res.url().includes("/api/")) api.push(`${res.status()} ${res.url().replace("https://localhost:7030", "")}`);
});
const res = await login(page, APPS.dashboard.react, {});
console.log(`login ok=${res.ok} ${res.error ?? ""}`);
await page.goto("http://localhost:5174/settings/branding", { waitUntil: "domcontentloaded" });
await page.waitForTimeout(5000);
console.log("url:", page.url());
console.log("--- body text ---");
console.log((await page.locator("body").innerText()).slice(0, 3000).replace(/\n+/g, " | "));
console.log("--- footer buttons ---");
const resetBtn = page.getByRole("button", { name: /reset to defaults/i });
try {
  await resetBtn.waitFor({ state: "visible", timeout: 45000 });
  console.log("reset button: VISIBLE");
} catch (err) {
  console.log("reset button: NOT FOUND —", String(err).split("\n")[0]);
}
console.log("reset count:", await resetBtn.count());
console.log("save count:", await page.getByRole("button", { name: /save branding/i }).count());
console.log("inputs:", await page.locator("input").count());
console.log("--- api calls ---");
console.log(api.join("\n") || "(none)");
await browser.close();