import path from "node:path";
import { pathToFileURL } from "node:url";

const PLAYWRIGHT_ENTRY = pathToFileURL(path.join("C:/repos/Project/dotnet-starter-kit-with-react-blazor-main/clients/admin/node_modules/playwright/index.mjs")).href;

const { chromium } = await import(PLAYWRIGHT_ENTRY);

const browser = await chromium.launch({ headless: false });
const page = await browser.newPage();
page.on("console", (msg) => console.log("CONSOLE:", msg.type(), msg.text()));
page.on("pageerror", (err) => console.log("PAGEERROR:", err.message));
page.on("request", (req) => console.log("REQUEST:", req.method(), req.url()));
page.on("requestfailed", (req) => console.log("REQUESTFAILED:", req.method(), req.url(), req.failure()?.errorText));
try {
  await page.goto("http://localhost:5176/login", { waitUntil: "load", timeout: 30000 });
  await page.waitForTimeout(10000);
} catch (e) {
  console.error("Error:", e.message);
}
await browser.close();