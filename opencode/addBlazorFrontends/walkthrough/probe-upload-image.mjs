import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { APPS, PLAYWRIGHT_ENTRY } from "./driver/lib/config.mjs";
import { attachCapture } from "./driver/lib/capture.mjs";
import { login } from "./driver/lib/auth.mjs";

const { chromium } = await import(PLAYWRIGHT_ENTRY);
const cfg = APPS.dashboard.blazor;

const productId = "01a00b75-d43c-71c3-a93e-09d3b32f1f65";
const pngPath = path.join(fileURLToPath(new URL(".", import.meta.url)), "tmp", "probe-upload.png");
fs.writeFileSync(pngPath, Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==", "base64"));

const browser = await chromium.launch({ headless: true, ignoreHTTPSErrors: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
const page = await ctx.newPage();
const capture = attachCapture(page);

const res = await login(page, cfg, capture);
if (!res.ok) {
  console.log(`[upload-probe] LOGIN FAILED: ${res.error}`);
  process.exit(1);
}

await page.goto(`${cfg.base}/catalog/products/${productId}`, { waitUntil: "domcontentloaded", timeout: 60000 });

const uploadBtn = page.getByRole("button", { name: "Upload images" });
await uploadBtn.waitFor({ state: "visible", timeout: 90000 });
console.log("[upload-probe] Upload images button visible");

const hint = await page.locator("text=JPG / PNG / WebP / GIF").first().isVisible().catch(() => false);
console.log(`[upload-probe] format hint visible: ${hint}`);

const countBefore = (await page.locator("text=/^\\d+ image/").first().innerText().catch(() => "0 images")).trim();
console.log(`[upload-probe] count before: ${countBefore}`);

await uploadBtn.click();
await page.waitForTimeout(800);
const fileInput = page.locator("input[type=file]").first();
await fileInput.setInputFiles(pngPath);
console.log("[upload-probe] file set, waiting for completion...");

let uploaded = false;
try {
  await page.getByText("Image uploaded").first().waitFor({ state: "visible", timeout: 90000 });
  uploaded = true;
  console.log("[upload-probe] SUCCESS: 'Image uploaded' snackbar shown");
} catch {
  const snack = await page.locator(".mud-snackbar").allInnerTexts().catch(() => []);
  console.log(`[upload-probe] FAIL: no success snackbar. snackbars=${JSON.stringify(snack)}`);
}

let countAfter = (await page.locator("text=/^\\d+ image/").first().innerText().catch(() => "0 images")).trim();
try {
  await page.getByText(/^[1-9]\d* images/).first().waitFor({ state: "visible", timeout: 30000 });
  countAfter = (await page.locator("text=/^\\d+ image/").first().innerText()).trim();
} catch {
  await page.waitForTimeout(3000);
  countAfter = (await page.locator("text=/^\\d+ image/").first().innerText().catch(() => "0 images")).trim();
}
console.log(`[upload-probe] count after: ${countAfter}`);

console.log(`[upload-probe] console errors (${capture.consoleErrors.length}):`);
for (const e of capture.consoleErrors.slice(0, 8)) console.log(`  - ${e.slice(0, 250)}`);

fs.unlinkSync(pngPath);
await browser.close();
process.exit(uploaded ? 0 : 1);