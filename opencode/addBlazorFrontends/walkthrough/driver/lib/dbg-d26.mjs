import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const dir = path.resolve(here, "../../evidence/20260817104114");
const r = JSON.parse(fs.readFileSync(`${dir}/dashboard.react.raw.json`, "utf8"));
const b = JSON.parse(fs.readFileSync(`${dir}/dashboard.blazor.raw.json`, "utf8"));

const show = (snap, label) => {
  console.log(`--- ${label}`);
  console.log(JSON.stringify(snap.counts));
  const tags = snap.tagCount ?? {};
  console.log("tags:", JSON.stringify(tags));
  console.log("rowLike:", snap.counts.rowLike, "bodyText head:", (snap.bodyText ?? "").slice(0, 300).replace(/\n/g, " | "));
};

const bD25 = b.find((x) => x.id === "D25");
const bD26 = b.find((x) => x.id === "D26");
show(bD25.snapshot, "BLAZOR D25 pre-drill LIST");
show(bD26.snapshot, "BLAZOR D26 pre-drill LIST");
const rD26 = r.find((x) => x.id === "D26");
show(rD26.detailFacts, "REACT D26 detailFacts (claims detail url)");