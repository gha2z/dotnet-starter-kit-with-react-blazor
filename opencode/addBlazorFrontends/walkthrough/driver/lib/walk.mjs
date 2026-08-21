import { resetCapture, captureScreens, waitSettled } from "./capture.mjs";
import { snapshotInPage, dumpDomMap, EMPTY_PATTERN } from "./vision.mjs";

// Poll until the SPA has painted real user content, then snapshot. Blazor WASM
// boots to a "Loading..." shell before hydration; snapping during that window
// produces a blank structural index. List pages may also re-render a second
// time (sequential reference-data fetches) — a snapshot taken between the
// first paint and the row render under-reports interactives/text. Similarly,
// after a drill click the SPA may keep the OLD route's DOM on screen for a
// tick while the URL already changed — a snapshot captured then would index
// the wrong page. Never return on the first content sample: require the
// signature to be stable for two consecutive rounds (>= 3 samples) whether or
// not rows are present.
async function settleSnapshot(page) {
  let facts = null;
  let prev = null;
  let stableCount = 0;
  for (let i = 0; i < 30; i++) {
    try {
      facts = await snapshotInPage(page);
    } catch (err) {
      if (String(err).includes("Execution context was destroyed")) {
        await page.waitForTimeout(800);
        continue;
      }
      throw err;
    }
    // A boot splash ("Loading...", ~10 chars, 1 interactive element) must never
    // count as content — keep polling until real UI replaces it.
    const splash = (facts.bodyText ?? "").trim().length <= 20 && /loading/i.test(facts.bodyText ?? "");
    const hasContent = !facts?.snapshotError && !splash && (facts.counts.interactive > 1 || facts.textLen > 100);
    const loading = (facts.loadingEls ?? 0) > 0;
    const signature = `${facts.counts.interactive}|${facts.textLen}|${facts.counts.rowLike}|${facts.counts.tableRows}`;
    if (hasContent && !loading && signature === prev) {
      stableCount++;
      if (stableCount >= 2) return facts;
    } else {
      stableCount = 0;
    }
    prev = signature;
    await page.waitForTimeout(400);
  }
  return facts;
}

export async function walkScreen(page, appCfg, entry, ctx) {
  const path =
    entry.kind === "detail" && entry.drill
      ? entry.drill.listPath
      : appCfg.framework === "blazor" && entry.pathBlazor
        ? entry.pathBlazor
        : entry.path;
  const url = `${appCfg.base}${path}`;
  const rec = {
    id: entry.id,
    name: entry.name,
    kind: entry.kind,
    targetUrl: url,
    url: null,
    anchorOk: null,
    anchorFound: null,
    screenshotDir: null,
    skipped: false,
    noRows: false,
    errors: [],
    failed: [],
  };
  resetCapture(ctx.capture);
  try {
    await page.goto(url, { waitUntil: "load", timeout: 30000 });
  } catch (err) {
    rec.errors.push(`goto: ${String(err).slice(0, 150)}`);
    return rec;
  }
  rec.url = page.url();
  let drilled = null;
  if (entry.kind === "detail" && entry.drill) {
    // F1: snapshot the LIST view before drilling — the structural index
    // for a drill screen is its list, not the post-click detail view.
    const preDrill = await settleSnapshot(page);
    drilled = await drillDetail(page, appCfg, entry, ctx);
    if (preDrill.snapshotError) drilled.errors.push(`snapshot: ${preDrill.snapshotError}`);
    else drilled.preFacts = preDrill;
    rec.url = drilled.url;
    rec.anchorOk = drilled.anchorOk;
    rec.anchorFound = drilled.anchorFound;
    rec.errors.push(...drilled.errors);
    rec.skipped = drilled.skipped ?? false;
    rec.anchorList = entry.anchorBlazor ?? entry.anchor;
  } else {
    const anchors = appCfg.framework === "blazor" ? entry.anchorBlazor ?? entry.anchor : entry.anchorReact ?? entry.anchor;
    const settled = await waitSettled(page, anchors);
    rec.anchorOk = settled.ok;
    rec.anchorFound = settled.anchor;
    rec.errors.push(...(settled.ok ? [] : [settled.error]));
    rec.anchorList = Array.isArray(anchors) ? anchors : [anchors];
  }
  const facts = await settleSnapshot(page);
  if (facts.snapshotError) rec.errors.push(`snapshot: ${facts.snapshotError}`);
  rec.snapshot = drilled?.preFacts ?? facts;
  if (drilled?.preFacts) rec.detailFacts = facts;
  rec.noRows =
    facts.counts?.rowLike === 0 && facts.interactive !== 0 && EMPTY_PATTERN.test(facts.bodyText ?? "") && facts.counts?.interactive <= 20;
  rec.domFile = await dumpDomMap(page, `${ctx.shotDir}/${appCfg.id}`, rec.id);
  rec.errors.push(...ctx.capture.consoleErrors.map((e) => `console: ${e}`));
  rec.errors.push(...ctx.capture.pageErrors.map((e) => `pageerror: ${e}`));
  rec.failed = [...ctx.capture.failedRequests];
  const shots = await captureScreens(page, `${ctx.shotDir}/${appCfg.id}`, rec.id);
  rec.screens = shots;
  return rec;
}

export async function drillDetail(page, appCfg, entry, ctx) {
  const { listPath, re } = entry.drill;
  const errors = [];
  const listAnchors = entry.drill.listAnchor ?? entry.anchor;
  const settled = await waitSettled(page, listAnchors);
  if (!settled.ok) errors.push(`list anchor: ${settled.error}`);
  const explicit = entry.drill.rowSelectorBlazor ?? entry.drill.rowSelector;
  const candidates = explicit
    ? [explicit]
    : appCfg.framework === "react"
      ? [
          "main tbody tr",
          "tbody tr",
          "main div.cursor-pointer",
          "div.cursor-pointer",
          "main [role=row]",
          // React rows are often plain divs with an inner <Link> (EntityListRow
          // without onClick) — no cursor-pointer/role=row to latch onto.
          "main div.group:has(a[href])",
          "main div:has(a[href])",
          "main li:has(a,button)",
          "li:has(a,button)",
        ]
      : [
          "main tbody tr",
          "tbody tr",
          "main div.mud-table-row",
          "main tr",
          "main li:has(a,button)",
          "main div.fsh-list-row",
          "div.fsh-list-row",
        ];
  let rowSelector = explicit;
  if (!rowSelector) {
    // Rows may render after the list anchor does (skeleton → data). Poll the
    // candidate list for up to ~12s before giving up on the drill.
    for (let poll = 0; poll < 24; poll++) {
      for (const c of candidates) {
        const n = await page.locator(c).count().catch(() => 0);
        if (n > 0) {
          rowSelector = c;
          break;
        }
      }
      if (rowSelector) break;
      await page.waitForTimeout(500);
    }
  }
  if (!rowSelector) {
    return {
      url: page.url(),
      anchorOk: false,
      anchorFound: null,
      errors: ["no data rows to drill"],
      skipped: true,
    };
  }
  const row = page.locator(rowSelector).first();
  const wantDetail = () => page.waitForURL((u) => new RegExp(re).test(u.pathname), { timeout: 10000 });
  const tryClick = async (loc, waitMs = 15000) => {
    await loc.waitFor({ state: "visible", timeout: waitMs });
    await loc.click();
    await wantDetail();
  };
  try {
    // React link-rows (EntityListRow without onClick): the div itself is inert —
    // the inner <Link> is the drill affordance. Click it directly when present.
    const innerLink = page.locator(`${rowSelector} a[href]`).first();
    if ((await innerLink.count()) > 0) {
      await tryClick(innerLink);
    } else {
      await tryClick(row);
    }
  } catch (firstErr) {
    // F3: retry once — react list rows are sometimes replaced by a table re-render
    // (authorization/navigation timeout on the first click is expected, not fatal).
    try {
      if (await page.locator(`${rowSelector} a[href]`).count()) {
        await page.locator(`${rowSelector} a[href]`).first().click().catch(() => {});
      } else {
        await row.click().catch(() => {});
      }
      await wantDetail();
    } catch {
      // F2: fallback click target — blazor MudTable rows may need the inner
      // button/action cell rather than the tr itself.
      let fallbackOk = false;
      try {
        const fb = page.locator(`${rowSelector} [role="button"], ${rowSelector} a`).first();
        await fb.waitFor({ state: "visible", timeout: 5000 });
        await fb.click();
        await wantDetail();
        fallbackOk = true;
      } catch {}
      if (!fallbackOk) {
        const rowCount = await page.locator(rowSelector).count().catch(() => 0);
        if (rowCount === 0) {
          return {
            url: page.url(),
            anchorOk: false,
            anchorFound: null,
            errors: ["no data rows to drill"],
            skipped: true,
          };
        }
        const body = await page
          .locator("body")
          .innerText()
          .then((t) => t.slice(0, 600))
          .catch(() => "");
        return {
          url: page.url(),
          anchorOk: false,
          anchorFound: null,
          errors: [`drill: ${String(firstErr).slice(0, 120)}; url=${page.url()}; text=${body}`],
          skipped: false,
        };
      }
    }
  }
  const detailAnchors = appCfg.framework === "blazor" ? entry.anchorBlazor ?? entry.anchor : entry.anchorReact ?? entry.anchor;
  const det = await waitSettled(page, detailAnchors);
  errors.push(...(det.ok ? [] : [det.error]));
  return { url: page.url(), anchorOk: det.ok, anchorFound: det.anchor, errors };
}
