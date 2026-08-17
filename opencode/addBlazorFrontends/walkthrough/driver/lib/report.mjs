import fs from "node:fs";
import path from "node:path";

export function emitReports(runDir, appName, records) {
  const jsonPath = path.join(runDir, `${appName}.raw.json`);
  fs.writeFileSync(jsonPath, JSON.stringify(records, null, 2), "utf8");

  const rows = records
    .filter((r) => r.kind !== "terminal")
    .map((r) => {
      const status = r.skipped ? "SKIP" : r.anchorOk ? "OK" : "PROBLEM";
      const errs = r.errors.length ? ` \u26a0 ${r.errors.length}` : "";
      return `| ${r.id} | ${r.name} | \`${r.url ?? r.targetUrl}\` | ${status}${errs} |`;
    });
  const md = [
    `# ${appName} — walkthrough run ${path.basename(runDir)}`,
    "",
    "| id | screen | url | status |",
    "|---|---|---|---|",
    ...rows,
    "",
    "## Terminal / deferred screens",
    "",
    ...records.filter((r) => r.kind === "terminal").map((r) => `- **${r.id} ${r.name}** — deferred: ${r.deferNote ?? ""}`),
    "",
    "## Console / network errors per screen",
    "",
    ...records.flatMap((r) => {
      const errs = r.errors ?? [];
      const failed = r.failed ?? [];
      return errs.length
        ? [`### ${r.id} ${r.name}`, "", ...errs.map((e) => `- \`${e}\``), ...failed.map((f) => `- FAILED ${f.url} => ${f.err}`), ""]
        : [];
    }),
  ].join("\n");
  fs.writeFileSync(path.join(runDir, `${appName}.md`), md, "utf8");
  return { jsonPath, mdPath: path.join(runDir, `${appName}.md`) };
}

// Phase-10: structural parity between react and blazor runs of the same app.
// targetRecords: { react: [rec], blazor: [rec] } — pairs screens by id.
export function emitParity(runDir, appName, targetRecords, screens, compareFn) {
  const pairs = [];
  for (const entry of screens) {
    const r = targetRecords.react?.find((x) => x.id === entry.id);
    const b = targetRecords.blazor?.find((x) => x.id === entry.id);
    if (!r || !b) continue;
    if (r.kind === "terminal" || b.kind === "terminal") continue;
    if (r.skipped || b.skipped) {
      pairs.push({ id: entry.id, name: entry.name, verdict: "N-A", reasons: ["skipped (no rows / drill N-A) on one side"] });
      continue;
    }
    const p = compareFn(r.snapshot, b.snapshot);
    pairs.push({
      id: entry.id,
      name: entry.name,
      verdict: p.verdict,
      reasons: p.reasons,
      cosmetic: p.cosmetic ?? [],
      react: { rows: r.noRows, anchors: r.anchorOk, errors: r.errors.length },
      blazor: { rows: b.noRows, anchors: b.anchorOk, errors: b.errors.length },
      domFiles: { react: r.domFile, blazor: b.domFile },
    });
  }
  const counts = { PASS: 0, DIFF: 0, UNVERIFIABLE: 0, "N-A": 0 };
  for (const p of pairs) counts[p.verdict]++;
  const rows = pairs.map((p) => `| ${p.id} | ${p.name} | ${p.verdict} | ${p.reasons.join("; ") || "—"} |`);
  const md = [
    `# ${appName} — structural parity (react vs blazor)`,
    "",
    `Run: ${path.basename(runDir)} · ${pairs.length} comparable screens · PASS ${counts.PASS} · DIFF ${counts.DIFF} · UNVERIFIABLE ${counts.UNVERIFIABLE} · N-A ${counts["N-A"]}`,
    "",
    "| id | screen | verdict | reasons |",
    "|---|---|---|---|",
    ...rows,
    "",
    "## DIFF / UNVERIFIABLE details",
    "",
    ...pairs
      .filter((p) => p.verdict !== "PASS")
      .map((p) => [
        `### ${p.id} ${p.name} — ${p.verdict}`,
        "",
        `- react: rows=${p.react?.rows} anchors=${p.react?.anchors} errors=${p.react?.errors}`,
        `- blazor: rows=${p.blazor?.rows} anchors=${p.blazor?.anchors} errors=${p.blazor?.errors}`,
        ...(p.domFiles?.react ? [`- react dom map: \`${p.domFiles.react}\``] : []),
        ...(p.domFiles?.blazor ? [`- blazor dom map: \`${p.domFiles.blazor}\``] : []),
        "",
      ])
      .flat(),
    "",
    "> DIFF = structural difference (headings/regions/interactive/input counts, content volume, empty-state). PASS = structural parity. UNVERIFIABLE = snapshot missing. Pixel-level comparison is out of scope for this harness (vision pass).",
  ].join("\n");
  fs.writeFileSync(path.join(runDir, `${appName}.parity.md`), md, "utf8");
  fs.writeFileSync(path.join(runDir, `${appName}.parity.json`), JSON.stringify(pairs, null, 2), "utf8");
  return { counts, pairs };
}