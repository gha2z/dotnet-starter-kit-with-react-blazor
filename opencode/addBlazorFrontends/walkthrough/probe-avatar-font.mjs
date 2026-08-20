import { fileURLToPath } from "node:url";
import { APPS, PLAYWRIGHT_ENTRY } from "./driver/lib/config.mjs";
import { attachCapture, resetCapture } from "./driver/lib/capture.mjs";
import { login } from "./driver/lib/auth.mjs";

// GR-11 real-browser probe: profile avatar in topbar user tile (live refresh via
// ProfileEvents), FshImageInput picker label, and on-demand font loading.
// Runs against the Blazor dashboard (5176) and Blazor admin (5175), restores
// the original avatar URL + font afterward.

const { chromium } = await import(PLAYWRIGHT_ENTRY);
const browser = await chromium.launch({ headless: true, ignoreHTTPSErrors: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
const page = await ctx.newPage();
const capture = attachCapture(page);

const AVATAR_URL = "https://picsum.photos/seed/fsh-gr11/96/96";
const results = [];
const consoleSnap = () => ({
  errors: capture.consoleErrors.length,
  pageErrors: capture.pageErrors.length,
  failed: capture.failedRequests.length,
});

function record(name, ok, detail = "") {
  results.push({ name, ok, detail });
  console.log(`[avatar-font] ${ok ? "PASS" : "FAIL"} ${name}${detail ? ` — ${detail}` : ""}`);
}

async function getOriginalAvatar(page) {
  const src = await page.locator("img.fsh-avatar-lg").getAttribute("src").catch(() => null);
  return src ?? "none";
}

async function setTopbarVisible(page) {
  const tile = page.locator(".fsh-user-menu-tile");
  await tile.waitFor({ state: "visible", timeout: 30000 });
  return tile;
}

async function verifyTopbarAvatar(page, expectImg) {
  const img = page.locator(".fsh-user-menu-tile img.fsh-user-menu-avatar");
  const visible = await img.isVisible().catch(() => false);
  const initials = await page.locator(".fsh-user-menu-tile").innerText().catch(() => "");
  const ok = expectImg ? visible : !visible && initials.trim().length > 0;
  record(`topbar avatar ${expectImg ? "image" : "initials fallback"}`, ok,
    expectImg ? (visible ? "img rendered" : "no img") : `initials="${initials.trim()}"`);
}

// ---------- Dashboard ----------
const dCfg = APPS.dashboard.blazor;
let res = await login(page, dCfg, capture);
if (!res.ok) {
  record("dashboard login", false, res.error);
} else {
  record("dashboard login", true);
  await page.goto(`${dCfg.base}/settings/profile`, { waitUntil: "domcontentloaded", timeout: 60000 });
  await page.getByText("Photo", { exact: true }).first().waitFor({ state: "visible", timeout: 60000 });
  const original = await getOriginalAvatar(page);
  record("profile page loads", true, `original avatar: ${original === "none" ? "none" : "set"}`);

  const tile = await setTopbarVisible(page);
  const avatarBefore = await tile.locator("img.fsh-user-menu-avatar").isVisible().catch(() => false);
  record("topbar reflects profile state before", true, avatarBefore ? "img shown" : "initials shown");

  resetCapture(capture);
  await page.getByRole("button", { name: "Paste URL" }).click();
  const urlField = page.getByPlaceholder("https://…").first();
  await urlField.waitFor({ state: "visible", timeout: 10000 });
  await urlField.fill(AVATAR_URL);
  await urlField.press("Tab");
  await page.getByText("Profile image updated").first().waitFor({ state: "visible", timeout: 30000 }).catch(() => {});
  await page.waitForTimeout(1500);

  const pageImg = await page.locator("img.fsh-avatar-lg").getAttribute("src").catch(() => null);
  record("profile page shows uploaded avatar", pageImg === AVATAR_URL, `src=${pageImg}`);
  await verifyTopbarAvatar(page, true);
  record("profile save no console errors", consoleSnap().errors === 0 && consoleSnap().pageErrors === 0,
    JSON.stringify(consoleSnap()));

  // Picker label + hidden input presence (upload mode)
  await page.getByRole("button", { name: "Upload" }).click();
  const picker = page.locator("label.fsh-image-picker");
  await picker.waitFor({ state: "visible", timeout: 10000 });
  const pickerText = (await picker.innerText()).trim();
  const hiddenInput = await page.locator("input.fsh-input-file-hidden[type=file]").count();
  record("picker label rendered", pickerText.includes("Replace image") && hiddenInput === 1,
    `text="${pickerText}" input=${hiddenInput}`);
  resetCapture(capture);
  await picker.click().catch(() => {});
  await page.waitForTimeout(600);
  record("picker click no console errors", consoleSnap().errors === 0, JSON.stringify(consoleSnap()));

  // Font: body stack + on-demand stylesheet via appearance page
  const bodyFont = await page.evaluate(() => getComputedStyle(document.body).fontFamily);
  record("boot font is Figtree", /figtree/i.test(bodyFont), bodyFont.slice(0, 80));
  await page.goto(`${dCfg.base}/settings/appearance`, { waitUntil: "domcontentloaded", timeout: 60000 });
  await page.getByText("Manrope").first().waitFor({ state: "visible", timeout: 30000 });
  resetCapture(capture);
  await page.locator("button", { hasText: "Manrope" }).first().click();
  await page.waitForTimeout(1200);
  const dynamicLink = await page.evaluate(() => {
    const l = document.getElementById("fsh-dynamic-font");
    return l ? l.getAttribute("href") : null;
  });
  const manropeApplied = await page.evaluate(() => getComputedStyle(document.body).fontFamily);
  record("on-demand font loads Manrope", dynamicLink?.includes("Manrope") === true && /manrope/i.test(manropeApplied),
    `link=${dynamicLink?.slice(0, 90)} applied=${manropeApplied.slice(0, 60)}`);
  await page.locator("button", { hasText: "Figtree" }).first().click();
  await page.waitForTimeout(800);

  // Restore original avatar
  await page.goto(`${dCfg.base}/settings/profile`, { waitUntil: "domcontentloaded", timeout: 60000 });
  await page.getByText("Photo", { exact: true }).first().waitFor({ state: "visible", timeout: 60000 });
  if (original !== "none") {
    await page.getByRole("button", { name: "Paste URL" }).click();
    const f2 = page.getByPlaceholder("https://…").first();
    await f2.waitFor({ state: "visible", timeout: 10000 });
    await f2.fill(original);
    await f2.press("Tab");
    await page.getByText("Profile image updated").first().waitFor({ state: "visible", timeout: 30000 }).catch(() => {});
  } else {
    await page.locator("button", { hasText: "Remove" }).first().click().catch(() => {});
  }
  await page.waitForTimeout(1200);
  const restored = await getOriginalAvatar(page);
  record("avatar restored", restored === original, `restored=${restored}`);
  await verifyTopbarAvatar(page, original !== "none");
  await page.getByText("Logout").first().click().catch(() => {});
}

// ---------- Admin ----------
const aCfg = APPS.admin.blazor;
await page.goto(`${aCfg.base}/login`, { waitUntil: "load", timeout: 30000 });
res = await login(page, aCfg, capture);
if (!res.ok) {
  record("admin login", false, res.error);
} else {
  record("admin login", true);
  await page.goto(`${aCfg.base}/settings/profile`, { waitUntil: "domcontentloaded", timeout: 60000 });
  await page.getByText("Avatar", { exact: true }).first().waitFor({ state: "visible", timeout: 60000 });
  const original = await getOriginalAvatar(page);
  const tile = await setTopbarVisible(page);
  const avatarBefore = await tile.locator("img.fsh-user-menu-avatar").isVisible().catch(() => false);
  record("admin topbar state before", true, avatarBefore ? "img shown" : "initials shown");

  resetCapture(capture);
  await page.getByRole("button", { name: "Change avatar" }).click();
  const dlg = page.locator(".mud-dialog");
  await dlg.waitFor({ state: "visible", timeout: 10000 });
  const urlField = page.getByLabel("Image URL").first();
  await urlField.fill(AVATAR_URL);
  await page.getByRole("button", { name: "Save" }).click();
  await page.getByText("Profile image updated").first().waitFor({ state: "visible", timeout: 30000 }).catch(() => {});
  await page.waitForTimeout(1500);
  const pageImg = await page.locator("img.fsh-avatar-lg").getAttribute("src").catch(() => null);
  record("admin profile page shows avatar", pageImg === AVATAR_URL, `src=${pageImg}`);
  await verifyTopbarAvatar(page, true);
  record("admin save no console errors", consoleSnap().errors === 0, JSON.stringify(consoleSnap()));

  await page.getByRole("button", { name: "Clear" }).click();
  await page.getByText("Profile image cleared").first().waitFor({ state: "visible", timeout: 30000 }).catch(() => {});
  await page.waitForTimeout(1200);
  await verifyTopbarAvatar(page, false);
  const cleared = await getOriginalAvatar(page);
  record("admin avatar cleared", cleared === "none", `current=${cleared}`);
}

const failed = results.filter((r) => !r.ok);
console.log(`\n[avatar-font] ${results.length - failed.length}/${results.length} checks passed`);
if (capture.consoleErrors.length) {
  console.log(`[avatar-font] residual console errors (${capture.consoleErrors.length}):`);
  for (const e of capture.consoleErrors.slice(0, 8)) console.log(`  - ${e.slice(0, 200)}`);
}
await browser.close();
process.exit(failed.length ? 1 : 0);