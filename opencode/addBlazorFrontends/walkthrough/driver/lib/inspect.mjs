// inspect.mjs — print a compact triage table from a run's raw JSON files.
// Usage: node lib/inspect.mjs <runDir> [app]
import fs from "node:fs";
import path from "node:path";

const runDir = process.argv[2] ?? ".";
const app = process.argv[3] ?? "admin";

function show(file, tag) {
  const rs = JSON.parse(fs.readFileSync(file, "utf8"));
  console.log(`== ${tag}`);
  for (const r of rs.filter((x) => x.kind !== "terminal")) {
    const s = r.snapshot ?? {};
    const c = s.counts ?? {};
    console.log(
      [
        r.id.padEnd(5),
        r.name.slice(0, 24).padEnd(25),
        `ok=${r.anchorOk}`,
        `skip=${r.skipped}`,
        `noRows=${r.noRows}`,
        `errs=${r.errors.length}`,
        `h1=${(s.h1 ?? []).length}`,
        `h2=${(s.h2 ?? []).length}`,
        `main=${s.regions ? !!s.regions.main : "?"}`,
        `int=${c.interactive}`,
        `inp=${c.inputs}`,
        `rows=${c.tableRows}`,
        `empty=${s.isEmptyState}`,
        `doms=${r.domFile ? 1 : 0}`,
      ].join(" "),
    );
  }
}

show(path.join(runDir, `${app}.react.raw.json`), `${app} REACT`);
show(path.join(runDir, `${app}.blazor.raw.json`), `${app} BLAZOR`);