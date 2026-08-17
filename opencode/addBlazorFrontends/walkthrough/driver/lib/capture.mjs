import { VIEWPORTS, IGNORED_FAILURES, IGNORED_CONSOLE, TIMEOUTS } from "./config.mjs";

export function attachCapture(page) {
  const state = { consoleErrors: [], pageErrors: [], failedRequests: [] };
  page.on("console", (msg) => {
    if (msg.type() !== "error") return;
    if (IGNORED_CONSOLE.test(msg.text())) return; // Lesson 2026-08-13: hub-negotiation noise
    state.consoleErrors.push(msg.text().slice(0, 300));
  });
  page.on("pageerror", (err) => state.pageErrors.push(String(err).slice(0, 300)));
  page.on("requestfailed", (req) => {
    const url = req.url();
    if (IGNORED_FAILURES.test(url)) return;
    state.failedRequests.push({ url: url.slice(0, 160), err: String(req.failure()?.errorText ?? "").slice(0, 120) });
  });
  return state;
}

export function resetCapture(state) {
  state.consoleErrors.length = 0;
  state.pageErrors.length = 0;
  state.failedRequests.length = 0;
}

// Phase-10 F1: pages render chrome + header instantly while data fetch is in flight
// ("0 tenants Loading the registry…", "LOADING PROFILE", MudTable "Loading…"). The
// snapshot must wait for the fetch to settle, otherwise we record mid-load states.
const LOADING_PATTERN = /\b(loading|fetching)\b/i;

export async function waitDataLoaded(page, timeoutMs = 15000) {
  const started = Date.now();
  for (;;) {
    const bodyText = await page
      .locator("body")
      .innerText()
      .catch(() => "");
    if (!LOADING_PATTERN.test(bodyText)) return true;
    if (Date.now() - started > timeoutMs) return false;
    await page.waitForTimeout(400);
  }
}

export async function waitSettled(page, anchorList, timeoutMs = TIMEOUTS.nav) {
  const settleMs = TIMEOUTS.settle ?? 1500;
  const tryAnchors = async (scope) => {
    let lastError = null;
    for (const a of anchorList) {
      try {
        const loc = scope.getByText(a, { exact: false }).first();
        await loc.waitFor({ state: "visible", timeout: timeoutMs });
        return { ok: true, anchor: a };
      } catch (err) {
        lastError = err;
      }
    }
    return { ok: false, anchor: null, error: String(lastError ?? "no anchors").slice(0, 200) };
  };
  // Lesson 2026-08-13: global getByText matches sidebar/nav chrome BEFORE the page hydrates,
  // so list screens were captured with 0 content. Anchor-scope to <main> first, then fall
  // back to a settle delay + full-page scan for content that lives outside <main>.
  const main = page.locator("main").first();
  try {
    await main.waitFor({ state: "attached", timeout: 5000 });
  } catch {
    const fb = await tryAnchors(page);
    if (fb.ok) return fb;
    await page.waitForTimeout(settleMs);
    return tryAnchors(page);
  }
  const scoped = await tryAnchors(main);
  if (scoped.ok) return scoped;
  await page.waitForTimeout(settleMs);
  return tryAnchors(page);
}

export async function captureScreens(page, dir, name) {
  const shots = [];
  for (const [label, vp] of Object.entries(VIEWPORTS)) {
    await page.setViewportSize(vp);
    await page.waitForTimeout(400);
    const file = `${dir}/${name}--${label}.png`;
    await page.screenshot({ path: file, fullPage: true });
    shots.push(file);
  }
  await page.setViewportSize(VIEWPORTS.desktop);
  return shots;
}