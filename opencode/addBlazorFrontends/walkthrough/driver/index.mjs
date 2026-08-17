import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { APPS, ACCOUNTS, PLAYWRIGHT_ENTRY, TIMEOUTS } from "./lib/config.mjs";
import { attachCapture } from "./lib/capture.mjs";
import { login } from "./lib/auth.mjs";
import { walkScreen } from "./lib/walk.mjs";
import { emitReports, emitParity } from "./lib/report.mjs";
import { compareParity } from "./lib/vision.mjs";

const HERE = fileURLToPath(new URL(".", import.meta.url));
const EVIDENCE = path.join(HERE, "..", "evidence");

function parseArgs(argv) {
  const args = { app: "admin", target: "both", scope: "full", screen: null, runId: null, headed: false, skipLogin: false };
  const take = (i, flag) => {
    const inline = argv[i].split("=");
    if (inline.length === 2) return [inline[1], i];
    if (i + 1 >= argv.length) throw new Error(`--${flag} requires a value`);
    return [argv[i + 1], i + 1];
  };
  for (let i = 2; i < argv.length; i++) {
    const token = argv[i];
    let value = null;
    let consumed = i;
    if (token.startsWith("--app")) [value, consumed] = take(i, "app");
    else if (token.startsWith("--target")) [value, consumed] = take(i, "target");
    else if (token.startsWith("--scope")) [value, consumed] = take(i, "scope");
    else if (token.startsWith("--screen")) [value, consumed] = take(i, "screen");
    else if (token.startsWith("--run-id")) [value, consumed] = take(i, "run-id");
    else if (token === "--headed") args.headed = true;
    else if (token === "--skip-login") args.skipLogin = true;
    else throw new Error(`Unknown flag ${token}`);
    if (value !== null) {
      const flag = token.slice(2).split("=")[0];
      if (["app", "target", "scope", "screen", "run-id"].includes(flag)) args[flag] = value;
    }
    i = consumed;
  }
  if (!["admin", "dashboard"].includes(args.app)) throw new Error("--app must be admin|dashboard");
  if (!["react", "blazor", "both"].includes(args.target)) throw new Error("--target must be react|blazor|both");
  if (!["smoke", "full"].includes(args.scope)) throw new Error("--scope must be smoke|full");
  return args;
}

const { chromium } = await import(PLAYWRIGHT_ENTRY);

const args = parseArgs(process.argv);
const runId = args["run-id"] ?? new Date().toISOString().replace(/[-:T]/g, "").slice(0, 14);
const runDir = path.join(EVIDENCE, runId);
fs.mkdirSync(runDir, { recursive: true });

const registryPath = path.join(HERE, args.app === "admin" ? "registry.admin.json" : "registry.dashboard.json");
const registry = JSON.parse(fs.readFileSync(registryPath, "utf8"));

const targets = args.target === "both" ? ["react", "blazor"] : [args.target];
const appCfg = APPS[args.app];

const scopeFilter = (entry) => {
  if (args.screen) return entry.id === args.screen;
  if (args.scope === "smoke") return ["auth", "dashboard", "list", "detail"].includes(entry.kind) && !entry.deferNote;
  return true;
};
const screens = registry.filter(scopeFilter);

const browser = await chromium.launch({
  headless: !args.headed,
  ignoreHTTPSErrors: true,
  args: ["--disable-http-cache"],
});

console.log(`[walk] run=${runId} app=${args.app} targets=${targets.join(",")} screens=${screens.length}`);
console.log(`[walk] evidence: ${runDir}`);

const targetRecords = {};

for (const target of targets) {
  const cfg = appCfg[target];
  const shotDir = runDir;
  fs.mkdirSync(path.join(shotDir, cfg.id), { recursive: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  const capture = attachCapture(page);
  const records = [];

  // Auth-gated=false screens (login, forgot-password, reset-password) must be
  // visited BEFORE login: both apps redirect authenticated users away from
  // them (react forgot-password → "/"), so walking them post-login times out
  // on the anchor. Walk them first, then log in, then the authed screens.
  const preAuthScreens = screens.filter((s) => s.auth === false && s.id !== (args.app === "admin" ? "A01" : "D01"));
  for (const entry of preAuthScreens) {
    if (entry.kind === "terminal") continue;
    const rec = await walkScreen(page, cfg, entry, { capture, shotDir });
    records.push(rec);
    const errFlag = rec.anchorOk ? "" : rec.skipped ? " (SKIP: " + (rec.errors[0] ?? "").slice(0, 80) + ")" : ` (PROBLEM: ${(rec.errors[0] ?? "").slice(0, 80)})`;
    console.log(
      `[walk] ${cfg.id} ${rec.id} ${rec.name} -> ${rec.url} ${rec.anchorOk ? "OK" : rec.skipped ? "SKIP" : "FAIL"}${errFlag}`,
    );
  }

  if (!args.skipLogin) {
    const loginRec = {
      id: "LOGIN",
      name: `Login (${cfg.title})`,
      kind: "auth",
      targetUrl: `${cfg.base}/login`,
      url: null,
      anchorOk: null,
      anchorFound: null,
      errors: [],
      failed: [],
    };
    const res = await login(page, cfg, capture);
    loginRec.url = res.url;
    loginRec.anchorOk = res.ok;
    loginRec.anchorFound = res.ok ? "post-login navigation" : res.pageText;
    if (!res.ok) loginRec.errors.push(res.error);
    loginRec.errors.push(...capture.consoleErrors.map((e) => `console: ${e}`));
    loginRec.errors.push(...capture.pageErrors.map((e) => `pageerror: ${e}`));
    loginRec.failed = [...capture.failedRequests];
    records.push(loginRec);
    if (!res.ok) {
      console.log(`[walk] ${cfg.id}: LOGIN FAILED — ${res.error}`);
      emitReports(runDir, `${args.app}.${target}`, records);
      await page.close();
      await ctx.close();
      continue;
    }
    console.log(`[walk] ${cfg.id}: logged in → ${res.url}`);
  }

  for (const entry of screens) {
    if (entry.id === (args.app === "admin" ? "A01" : "D01")) continue; // login already handled
    if (entry.auth === false && !args.skipLogin) continue; // already walked pre-login
    if (entry.kind === "terminal") {
      records.push({ id: entry.id, name: entry.name, kind: "terminal", deferNote: entry.deferNote ?? "" });
      continue;
    }
    const rec = await walkScreen(page, cfg, entry, { capture, shotDir });
    records.push(rec);
    const errFlag = rec.anchorOk ? "" : rec.skipped ? " (SKIP: " + (rec.errors[0] ?? "").slice(0, 80) + ")" : ` (PROBLEM: ${(rec.errors[0] ?? "").slice(0, 80)})`;
    console.log(
      `[walk] ${cfg.id} ${rec.id} ${rec.name} -> ${rec.url} ${rec.anchorOk ? "OK" : rec.skipped ? "SKIP" : "FAIL"}${errFlag}`,
    );
  }

  emitReports(runDir, `${args.app}.${target}`, records);
  targetRecords[target] = records;
  console.log(`[walk] ${cfg.id}: done — ${records.length} records`);
  await page.close();
  await ctx.close();
}

if (targetRecords.react && targetRecords.blazor) {
  const { counts } = emitParity(runDir, args.app, targetRecords, screens, compareParity);
  console.log(`[parity] PASS ${counts.PASS} · DIFF ${counts.DIFF} · UNVERIFIABLE ${counts.UNVERIFIABLE} · N-A ${counts["N-A"]}`);
} else {
  console.log("[parity] skipped (single-target run)");
}

await browser.close();
console.log(`[walk] complete. reports in ${runDir}/`);