import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const dir = path.resolve(here, "../../evidence/20260817111320");
const r = JSON.parse(fs.readFileSync(`${dir}/dashboard.react.raw.json`, "utf8"));
const b = JSON.parse(fs.readFileSync(`${dir}/dashboard.blazor.raw.json`, "utf8"));

const show = (snap, label) => {
  console.log(`--- ${label}`);
  console.log(JSON.stringify(snap.counts));
  console.log("bodyText head:", (snap.bodyText ?? "").slice(0, 400).replace(/\n/g, " | "));
};

show(r.find((x) => x.id === "D31").snapshot, "REACT D31 Branding");
show(b.find((x) => x.id === "D31").snapshot, "BLAZOR D31 Branding");