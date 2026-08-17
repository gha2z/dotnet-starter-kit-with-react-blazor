import fs from "node:fs";
import path from "node:path";

export const EMPTY_PATTERN = /no (data|rows?|records?|audits|notifications)|nothing (here|yet)|empty|awaiting data|no results/i;

const MAX_ELEMENTS = 1600;
const MAX_DEPTH = 60;
const TEXT_CAP = 1500;

export async function snapshotInPage(page) {
  return page.evaluate((textCap) => {
    const STRUCTURAL_TAGS = new Set([
      "header", "nav", "main", "aside", "footer", "section", "article",
      "h1", "h2", "h3", "h4", "h5", "h6", "p", "a", "button", "input",
      "select", "textarea", "form", "table", "thead", "tbody", "tr", "td",
      "th", "ul", "ol", "li", "img", "dialog", "label", "span", "div",
    ]);
    const REGION_TAGS = new Set(["header", "nav", "main", "aside", "footer"]);
    const INTERACTIVE_TAGS = new Set(["a", "button", "input", "select", "textarea", "summary"]);
    const walk = (el, depth, sb) => {
      if (sb.count > 9000 || depth > 60) return;
      const tag = el.tagName?.toLowerCase() ?? "";
      sb.count++;
      if (tag === "script" || tag === "style" || tag === "noscript" || tag === "template") return;
      if (STRUCTURAL_TAGS.has(tag)) sb.tags[tag] = (sb.tags[tag] ?? 0) + 1;
      if (REGION_TAGS.has(tag)) sb.regions[tag] = true;
      if (INTERACTIVE_TAGS.has(tag)) {
        if (tag === "a") {
          if (el.href && !/^javascript:/.test(el.href)) {
            sb.interactive++;
            sb.links++;
          }
        } else {
          sb.interactive++;
          if (tag === "button") sb.buttons++;
          else if (tag === "input") sb.inputs++;
          else if (tag === "select") sb.selects++;
          else if (tag === "textarea") sb.textareas++;
        }
      }
      if (tag === "h1") sb.h1.length < 12 && sb.h1.push((el.textContent ?? "").trim().slice(0, 120));
      else if (tag === "h2") sb.h2.length < 12 && sb.h2.push((el.textContent ?? "").trim().slice(0, 120));
      if (tag === "img") {
        sb.images++;
        if (el.getAttribute("alt")) sb.imgAlt++;
      }
      if (tag === "table") {
        sb.tables++;
        sb.tableRows += el.querySelectorAll("tbody tr, tr").length;
      }
      sb.rowLike += /row/i.test(el.className ?? "") ? 1 : 0;
      if (tag === "form") sb.forms++;
      if (tag === "dialog") sb.dialogs++;
      const role = el.getAttribute?.("role");
      if (el.tagName?.toLowerCase() !== "button" && role === "button") sb.interactive++;
      for (const c of el.children) walk(c, depth + 1, sb);
    };
    const sb = {
      count: 0, tags: {}, regions: { header: false, nav: false, main: false, aside: false, footer: false },
      interactive: 0, buttons: 0, links: 0, inputs: 0, selects: 0, textareas: 0,
      images: 0, imgAlt: 0, forms: 0, tables: 0, tableRows: 0, rowLike: 0, dialogs: 0,
      h1: [], h2: [],
    };
    try {
      walk(document.documentElement, 0, sb);
      const loadingEls = document.querySelectorAll(
        '[class*="skeleton"], [class*="Skeleton"], [class*="loading"], [class*="Loading"], [aria-busy="true"], .mud-progress-circular, .fsh-loading-row',
      ).length;
      const fullText = (document.body?.innerText ?? "").trim();
      const bodyText = fullText.slice(0, textCap);
      return {
        ok: true,
        title: document.title,
        url: location.href,
        h1: sb.h1,
        h2: sb.h2,
        regions: sb.regions,
        loadingEls,
        counts: {
          interactive: sb.interactive, buttons: sb.buttons, links: sb.links,
          inputs: sb.inputs, selects: sb.selects, textareas: sb.textareas,
          images: sb.images, imgAlt: sb.imgAlt, forms: sb.forms, tables: sb.tables,
          tableRows: sb.tableRows, rowLike: sb.rowLike, dialogs: sb.dialogs,
        },
        tagCount: sb.tags,
        textLen: fullText.length,
        bodyText,
        isEmptyState: /no (data|rows?|records?|audits|notifications)|nothing (here|yet)|empty|awaiting data|no results/i.test(bodyText) && sb.rowLike === 0 && sb.interactive <= 20,
      };
    } catch (err) {
      return { ok: false, snapshotError: String(err).slice(0, 200) };
    }
  }, TEXT_CAP);
}

export async function dumpDomMap(page, dir, name) {
  try {
    const map = await page.evaluate((maxE, maxD) => {
      const out = { url: location.href, title: document.title, elements: [] };
      const walk = (el, depth) => {
        if (out.elements.length >= maxE || depth > maxD) return;
        const tag = el.tagName?.toLowerCase() ?? "";
        if (tag === "script" || tag === "style") return;
        out.elements.push({ tag, id: el.id || null, cls: (el.className ?? "").slice(0, 80) || null });
        for (const c of el.children) walk(c, depth + 1);
      };
      try {
        walk(document.documentElement, 0);
      } catch {}
      return out;
    }, MAX_ELEMENTS, MAX_DEPTH);
    fs.mkdirSync(dir, { recursive: true });
    fs.writeFileSync(path.join(dir, `${name}--dom.json`), JSON.stringify(map, null, 1));
    return `${name}--dom.json`;
  } catch (err) {
    return null;
  }
}

// Contract (report.mjs emitParity): compareFn(reactSnapshot, blazorSnapshot) ->
// { verdict: "PASS" | "DIFF" | "UNVERIFIABLE", reasons: [string] }.
// Parity = blazor must be at least as rich as react; being richer is an
// enrichment, never a gap. Only blazor-missing things flag.
export function compareParity(r, b) {
  if (!r) return { verdict: "UNVERIFIABLE", reasons: ["react snapshot missing"] };
  if (!b) return { verdict: "UNVERIFIABLE", reasons: ["blazor snapshot missing"] };
  if (r.snapshotError || b.snapshotError)
    return { verdict: "UNVERIFIABLE", reasons: [`react=${r.snapshotError} blazor=${b.snapshotError}`].filter(Boolean) };
  const reasons = [];
  const lacks = (what, cond, detail) => {
    if (cond) reasons.push(`${what} mismatch (react ${detail})`);
  };
  lacks("main region", r.regions.main && !b.regions.main, `true vs blazor ${b.regions.main}`);
  if (r.counts.interactive >= 5 && b.counts.interactive < r.counts.interactive * 0.5)
    reasons.push(`interactive count mismatch (react ${r.counts.interactive} vs blazor ${b.counts.interactive})`);
  lacks("input count", r.counts.inputs > 1 && b.counts.inputs < r.counts.inputs * 0.5, `${r.counts.inputs} vs blazor ${b.counts.inputs}`);
  if (r.textLen >= 300 && b.textLen < r.textLen * 0.3)
    reasons.push(`content volume mismatch (react ${r.textLen} vs blazor ${b.textLen} chars)`);
  lacks("empty-state", r.isEmptyState === false && b.isEmptyState === true, "empty=false vs blazor empty=true");
  if (r.counts.tableRows > 0 && b.counts.tableRows === 0)
    reasons.push(`table rows mismatch (react ${r.counts.tableRows} vs blazor 0)`);
  return { verdict: reasons.length ? "DIFF" : "PASS", reasons };
}