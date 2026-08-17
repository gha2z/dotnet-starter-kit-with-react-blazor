// diagnose.mjs — inspect a completed walkthrough run: per-screen pair summary
// plus the same parity verdict the report uses (so parity.md never needs re-reading).
// Usage: node diagnose.mjs <evidence-run-dir> <app> [screenId ...]
//   e.g. node diagnose.mjs 20260814065625 admin A13 A16
import fs from "node:fs";
import path from "node:path";
import { compareParity } from "./lib/vision.mjs";

const runDir = process.argv[2];
const app = process.argv[3] ?? "admin";
const want = new Set(process.argv.slice(4));
if (!runDir) {
  console.error("usage: node diagnose.mjs <run-dir> <app> [screenId ...]");
  process.exit(1);
}
const byId = new Map();
for (const t of ["react", "blazor"]) {
  const file = path.join(runDir, `${app}.${t}.raw.json`);
  if (!fs.existsSync(file)) {
    console.log(`(no ${file})`);
    continue;
  }
  const records = JSON.parse(fs.readFileSync(file, "utf8")).filter((r) => r.kind !== "terminal");
  for (const r of records) {
    if (!byId.has(r.id)) byId.set(r.id, {});
    byId.get(r.id)[t] = r;
  }
}
console.log(`run ${path.basename(runDir)} app=${app} screens=${byId.size}`);

const brief = (s) =>
  !s
    ? "(no snapshot)"
    : JSON.stringify({
        h1: s.h1,
        h2: s.h2?.slice(0, 3),
        regions: s.regions,
        counts: s.counts,
        textLen: s.textLen,
        empty: s.isEmptyState,
      });

for (const [id, pair] of byId) {
  if (want.size && !want.has(id)) continue;
  const r = pair.react;
  const b = pair.blazor;
  const v = r && b && !r.skipped && !b.skipped ? compareParity(r.snapshot, b.snapshot) : null;
  console.log(`\n=== ${id} === verdict=${v?.verdict ?? "N-A"}${v?.reasons?.length ? ` :: ${v.reasons.join(" | ")}` : ""}`);
  for (const t of ["react", "blazor"]) {
    const x = pair[t];
    if (!x) continue;
    console.log(`-- ${t}: url=${x.url} anchor=${x.anchorOk} skipped=${x.skipped} noRows=${x.noRows} errors=${x.errors.length}`);
    console.log(brief(x.snapshot));
    if (x.snapshot?.bodyText && x.snapshot.textLen < 60) {
      console.log(`  [snapshot body] ${x.snapshot.bodyText.slice(0, 200)}`);
    }
    if (x.errors.length) {
      for (const e of x.errors.slice(0, 4)) console.log(`  ERR: ${e.slice(0, 220)}`);
    }
    if (x.detailFacts) {
      console.log(`  [detail] textLen=${x.detailFacts.textLen} interactive=${x.detailFacts.counts?.interactive} h1=${x.detailFacts.h1?.slice(0, 3)}`);
      console.log(`  [detail body] ${(x.detailFacts.bodyText ?? "").slice(0, 400)}`);
    }
  }
}