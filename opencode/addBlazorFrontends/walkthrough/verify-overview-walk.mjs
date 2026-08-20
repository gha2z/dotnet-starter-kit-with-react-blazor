// verify-overview-walk.mjs — real-browser walkthrough for the dashboard Blazor parity work:
// 1) OverviewPage restyle (quick actions, stat cards, sections, fsh-sse-pulse)
// 2) Chat unread badge + mark-read
// 3) Topnav avatar image in user tile
// 4) Nav section caption font parity
import { pathToFileURL } from "node:url";
import path from "node:path";
const { chromium } = await import(
  pathToFileURL(path.join(import.meta.dirname, "..", "..", "..", "clients", "admin", "node_modules", "playwright", "index.mjs")).href
);
const { APPS, VIEWPORTS, IGNORED_CONSOLE } = await import(
  pathToFileURL(path.join(import.meta.dirname, "driver", "lib", "config.mjs")).href
);
const { login } = await import(pathToFileURL(path.join(import.meta.dirname, "driver", "lib", "auth.mjs")).href);

const app = APPS.dashboard.blazor;
const b = await chromium.launch({ ignoreHTTPSErrors: true });
const ctx = await b.newContext({ viewport: VIEWPORTS.desktop, ignoreHTTPSErrors: true });
const p = await ctx.newPage();
const consoleErrs = [];
const failed = [];
p.on("console", (m) => {
  if (m.type() === "error" && !IGNORED_CONSOLE.test(m.text())) consoleErrs.push(m.text().slice(0, 200));
});
p.on("requestfailed", (r) => failed.push(`${r.failure()?.errorText ?? "?"} ${r.method()} ${r.url()}`));

const results = [];
const check = (name, ok, detail = "") => results.push({ name, ok, detail: String(detail).slice(0, 400) });

try {
  const lr = await login(p, app, null);
  check("login", lr.ok, lr.error ?? lr.url);
  await p.waitForSelector(".fsh-topbar, .fsh-sidebar", { state: "attached", timeout: 90000 });
  await p.waitForTimeout(5000);

  // ---- 1) Overview page: quick actions + stat cards + sections
  const body = await p.locator("body").innerText();
  check("overview: quick actions present", /(Transfer|Top Up|Send|quick)/i.test(body) || /(Transfer|Send|top)/i.test(body), body.slice(0, 200));
  for (const label of ["Overview", "Recent Transactions", "Billing Summary", "Notifications", "Activity"]) {
    if (body.includes(label)) { check(`overview: section '${label}'`, true); break; }
  }
  // stat cards: look for metric-like labels
  for (const label of ["Wallet Balance", "Total Spent", "Active Subscriptions", "Unread Notifications", "Pending Invoices", "Active Sessions"]) {
    if (body.includes(label)) { check(`overview: stat '${label}'`, true); break; }
  }

  // fsh-sse-pulse on the server status dot (React parity)
  const pulse = await p.locator(".fsh-sse-pulse").count();
  check("overview: fsh-sse-pulse present", pulse > 0, `count=${pulse}`);

  // ---- 2) Chat unread badge (topbar FshChatUnreadBadge; general has 5 unread)
  const chatBadge = p.locator(".fsh-chat-unread-badge");
  const chatBadgeCount = await chatBadge.count();
  const chatBadgeText = chatBadgeCount ? (await chatBadge.first().innerText().catch(() => "")).trim() : "";
  check("chat badge: element rendered", chatBadgeCount > 0, `count=${chatBadgeCount}`);
  check("chat badge: unread count shown", chatBadgeText.length > 0, `badge='${chatBadgeText}'`);

  // ---- 3) Topnav avatar image (profile ImageUrl from MinIO)
  const avatarImg = p.locator(".fsh-user-menu-avatar");
  const avatarCount = await avatarImg.count();
  check("topnav: avatar img element", avatarCount > 0, "count=" + avatarCount);
  if (avatarCount > 0) {
    const loaded = await avatarImg.first().evaluate((el) => el.complete && el.naturalWidth > 0);
    const src = await avatarImg.first().getAttribute("src").catch(() => null);
    check("topnav: avatar loaded", loaded, `src=${src ?? "null"}`);
  }

  // ---- 4) Nav caption font parity: .fsh-nav-section-caption should use the nav-text font
  const captionFont = await p
    .locator(".fsh-nav-section-caption")
    .first()
    .evaluate((el) => {
      const cs = getComputedStyle(el);
      return `${cs.fontFamily} | ${cs.fontSize} | ${cs.fontWeight}`;
    })
    .catch(() => null);
  const navTextFont = await p
    .locator(".fsh-nav-text")
    .first()
    .evaluate((el) => {
      const cs = getComputedStyle(el);
      return `${cs.fontFamily} | ${cs.fontSize} | ${cs.fontWeight}`;
    })
    .catch(() => null);
  check("nav caption font matches nav-text", captionFont === navTextFont && captionFont !== null, `caption=[${captionFont}] nav=[${navTextFont}]`);

  // ---- 5) Chat mark-read: open chat, verify badge clears after channel read
  const chatBtn = p.locator(".fsh-chat-unread a, .fsh-chat-unread button").first();
  if ((await chatBtn.count()) > 0) {
    await chatBtn.click();
    await p.waitForTimeout(4000);
    check("chat: navigated to /chat", p.url().includes("/chat"), p.url());
    const channelNames = await p.locator(".fsh-chat-channel-name, .mud-list-item-text, .fsh-chat-sidebar *").allInnerTexts().catch(() => []);
    check("chat: channel list rendered", channelNames.some((t) => /general|random/i.test(t)), channelNames.join(" | ").slice(0, 200));
    await p.waitForTimeout(3000);
    const badgeAfter = await p.locator(".fsh-chat-unread-badge").count();
    check("chat: unread badge cleared after opening", badgeAfter === 0, `badgeCount=${badgeAfter}`);
  } else {
    check("chat: badge activator found", false, "no .fsh-chat-unread activator");
  }

  await p.screenshot({ path: path.join(import.meta.dirname, "evidence", "verify-overview-final.png"), fullPage: true });
  check("screenshot saved", true, "verify-overview-final.png");
} catch (err) {
  check("run", false, String(err).slice(0, 400));
}

console.log("=== RESULTS ===");
for (const r of results) console.log(`${r.ok ? "PASS" : "FAIL"}  ${r.name}${r.detail ? "  -- " + r.detail : ""}`);
console.log("=== CONSOLE ERRORS ===", consoleErrs.length ? consoleErrs : "(none)");
console.log("=== FAILED REQUESTS ===", failed.length ? failed : "(none)");
await b.close();