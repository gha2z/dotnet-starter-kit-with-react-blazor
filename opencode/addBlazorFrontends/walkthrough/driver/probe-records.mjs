import fs from "node:fs";

const [jsonPath, ...ids] = process.argv.slice(2);
if (!jsonPath) {
  console.error("usage: node probe-records.mjs <raw.json> [id...]");
  process.exit(1);
}

const records = JSON.parse(fs.readFileSync(jsonPath, "utf8"));
const want = new Set(ids);
for (const r of records) {
  if (want.size && !want.has(r.id)) continue;
  const s = r.snapshot ?? {};
  const d = r.detailFacts ?? {};
  console.log(
    `${r.id} ${r.name} | url=${r.url ?? ""} | ok=${r.anchorOk} skip=${r.skipped ?? false} rows=${r.noRows}`,
  );
  for (const [label, facts] of [["snap", s], ["det", d]]) {
    if (!facts || !facts.tagCount) continue;
    console.log(
      `  ${label}: regions=${JSON.stringify(facts.regions)} textLen=${facts.textLen} inter=${facts.counts?.interactive} h1=${JSON.stringify(facts.h1 ?? [])}`,
    );
    console.log(`  ${label}: tagCount=${JSON.stringify(facts.tagCount)}`);
  }
  if (r.errors?.length) console.log(`  errors: ${JSON.stringify(r.errors)}`);
}