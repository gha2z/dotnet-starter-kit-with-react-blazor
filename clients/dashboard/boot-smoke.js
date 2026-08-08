const { chromium } = require("playwright");

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const requests = [];
  const consoleMsgs = [];
  page.on("request", (r) => {
    if (r.url().includes("localhost")) requests.push(`${r.method()} ${r.url().replace("http://localhost:5176", "")}`);
  });
  page.on("response", (r) => {
    if (r.status() >= 400 && r.url().includes("localhost")) {
      consoleMsgs.push(`HTTP ${r.status()} ${r.url().replace("http://localhost:5176", "")}`);
    }
  });
  page.on("console", (m) => consoleMsgs.push(`[console.${m.type()}] ${m.text().slice(0, 300)}`));
  page.on("pageerror", (e) => consoleMsgs.push(`[pageerror] ${e.message.slice(0, 300)}`));

  await page.goto("http://localhost:5176/login", { timeout: 30000 });
  await page.waitForTimeout(25000);
  await page.screenshot({ path: ".opencode/temp/boot-smoke.png" });
  console.log("=== REQUESTS ===");
  console.log(requests.slice(0, 40).join("\n"));
  console.log("=== ERRORS / CONSOLE ===");
  console.log(consoleMsgs.slice(0, 30).join("\n"));
  const errorUi = await page.locator("#blazor-error-ui").isVisible().catch(() => false);
  const provider = await page.locator(".mud-theme-provider").count();
  console.log(`=== RESULT: blazor-error-ui=${errorUi} mud-theme-provider=${provider}`);
  await browser.close();
})();
