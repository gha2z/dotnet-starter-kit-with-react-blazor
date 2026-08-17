import { fileURLToPath } from "node:url";
import path from "node:path";
import { APPS, PLAYWRIGHT_ENTRY, TIMEOUTS } from "./lib/config.mjs";
import { login } from "./lib/auth.mjs";
import { waitSettled } from "./lib/capture.mjs";

const HERE = fileURLToPath(new URL(".", import.meta.url));
const { chromium } = await import(PLAYWRIGHT_ENTRY);

const app = APPS.admin.react;
const browser = await chromium.launch({ headless: true, ignoreHTTPSErrors: true, args: ["--disable-http-cache"] });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
const page = await ctx.newPage();

const loginRes = await login(page, app, { capture: { consoleErrors: [], pageErrors: [], failedRequests: [] } });
console.log(`login: ${loginRes.ok} -> ${loginRes.url}`);

await page.goto(`${app.base}/billing/invoices`, { waitUntil: "load", timeout: 30000 });
const settled = await waitSettled(page, ["Billing", "Invoices"]);
console.log(`settled: ${settled.ok} anchor=${settled.anchor}`);

const cands = [
  "div.cursor-pointer",
  "li",
  "tr",
  "tbody tr",
  "[role=row]",
  "a[href*='/billing/invoices/']",
  "button",
  "a",
];
for (const sel of cands) {
  const n = await page.locator(sel).count().catch(() => -1);
  console.log(`count ${sel} = ${n}`);
}

const links = await page
  .locator("a[href]")
  .evaluateAll((els) => els.map((e) => e.getAttribute("href")).filter((h) => h && h.includes("/billing/")))
  .catch(() => []);
console.log("billing hrefs:", JSON.stringify(links));

const outline = await page
  .locator("main")
  .evaluateAll((els) => {
    const walk = (el, depth) => {
      if (depth > 3) return "";
      let out = "";
      for (const c of el.children) {
        const cls = (c.getAttribute("class") || "").slice(0, 40);
        out += `${"  ".repeat(depth)}<${c.tagName.toLowerCase()}>${cls ? ` .${cls}` : ""}\n`;
        out += walk(c, depth + 1);
      }
      return out;
    };
    return walk(els[0], 0);
  })
  .catch(() => "no main");
console.log(outline);

await browser.close();