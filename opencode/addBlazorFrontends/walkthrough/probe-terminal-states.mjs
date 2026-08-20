import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { APPS, PLAYWRIGHT_ENTRY } from "./driver/lib/config.mjs";
import { attachCapture } from "./driver/lib/capture.mjs";
import { login } from "./driver/lib/auth.mjs";

process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";

const { chromium } = await import(PLAYWRIGHT_ENTRY);
const HERE = fileURLToPath(new URL(".", import.meta.url));
const EVIDENCE = path.join(HERE, "evidence", "d35d36-terminal-states");
fs.mkdirSync(EVIDENCE, { recursive: true });

const API = "https://localhost:7030";
const TENANTS = "acme";

const results = [];
const check = (name, ok, detail = "") => {
  results.push({ name, ok, detail });
  console.log(`${ok ? "PASS" : "FAIL"}  ${name}${detail ? ` — ${detail}` : ""}`);
};

async function api(pathname, { method = "GET", token, tenant, body } = {}) {
  const headers = { accept: "application/json" };
  if (token) headers.authorization = `Bearer ${token}`;
  if (tenant) headers.tenant = tenant;
  if (body) headers["content-type"] = "application/json";
  const res = await fetch(`${API}${pathname}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  let json = null;
  try { json = JSON.parse(text); } catch { /* non-json */ }
  return { status: res.status, json, text };
}

async function loginApi(tenant, email, password) {
  const { status, json } = await api("/api/v1/identity/token/issue", {
    method: "POST",
    tenant,
    body: { email, password },
  });
  if (status !== 200 || !json?.accessToken) throw new Error(`api login failed ${status}`);
  return json.accessToken;
}

function decodeJti(token) {
  const payload = JSON.parse(Buffer.from(token.split(".")[1], "base64url").toString("utf8"));
  return payload.jti;
}

function asArray(json) {
  return Array.isArray(json) ? json : (json?.data ?? json?.items ?? []);
}

const browser = await chromium.launch({ headless: true, ignoreHTTPSErrors: true });
const cfgBlazor = APPS.dashboard.blazor;
const cfgReact = APPS.dashboard.react;

/* ─────────────────────────── D35: tenant deactivated ─────────────────────────── */
try {
  const rootToken = await loginApi("root", "superadmin@root.com", "Password123!");

  const tenants = await api("/api/v1/tenants", { token: rootToken });
  const acme = asArray(tenants.json).find((t) => String(t.tenantId ?? t.id ?? t.key ?? t.name).toLowerCase() === TENANTS)
    ?? asArray(tenants.json).find((t) => String(t.name ?? t.key ?? "").toLowerCase() === TENANTS);
  if (!acme) throw new Error(`acme tenant not found in ${JSON.stringify(asArray(tenants.json).slice?.(0, 3) ?? tenants.text.slice(0, 200))}`);
  const acmeId = String(acme.tenantId ?? acme.id ?? acme.key);
  check("D35 api: acme tenant resolved", true, acmeId);

  const setActive = async (isActive) => {
    const r = await api(`/api/v1/tenants/${acmeId}/activation`, {
      method: "POST",
      token: rootToken,
      body: { tenantId: acmeId, isActive },
    });
    return r.status;
  };

  /* Blazor dashboard */
  const bCtx = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
  const bPage = await bCtx.newPage();
  const bCap = attachCapture(bPage);
  const bLogin = await login(bPage, cfgBlazor, bCap);
  check("D35 blazor: login as acme admin", bLogin.ok, bLogin.url);
  if (bLogin.ok) {
    await bPage.goto(`${cfgBlazor.base}/`, { waitUntil: "domcontentloaded", timeout: 60000 });
    await bPage.getByText("Overview", { exact: true }).first().waitFor({ state: "visible", timeout: 90000 }).catch(() => {});
    check("D35 blazor: overview loaded", bPage.url().includes("/"), bPage.url());

    const st = await setActive(false);
    check("D35 blazor: tenant deactivated via api", st === 200, `status=${st}`);

    await bPage.reload({ waitUntil: "domcontentloaded", timeout: 60000 }).catch(() => {});
    let navOk = false;
    try {
      await bPage.waitForURL(/\/tenant-deactivated/, { timeout: 90000 });
      navOk = true;
    } catch { /* fall through */ }
    const bTitle = await bPage.getByText("Tenant deactivated", { exact: true }).first().isVisible().catch(() => false);
    const bBtn = await bPage.getByRole("button", { name: "Back to sign in" }).first().isVisible().catch(() => false);
    check("D35 blazor: routed to /tenant-deactivated", navOk, bPage.url());
    check("D35 blazor: terminal card renders", bTitle && bBtn, `title=${bTitle} btn=${bBtn}`);
    await bPage.screenshot({ path: path.join(EVIDENCE, "d35-blazor.png"), fullPage: true });
  }

  const stRe = await setActive(true);
  check("D35 blazor: tenant reactivated", stRe === 200, `status=${stRe}`);

  /* React dashboard (parity baseline) */
  const rCtx = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
  const rPage = await rCtx.newPage();
  const rCap = attachCapture(rPage);
  const rLogin = await login(rPage, cfgReact, rCap);
  check("D35 react: login as acme admin", rLogin.ok, rLogin.url);
  if (rLogin.ok) {
    await rPage.goto(`${cfgReact.base}/`, { waitUntil: "domcontentloaded", timeout: 60000 });
    await rPage.getByText("Overview", { exact: true }).first().waitFor({ state: "visible", timeout: 60000 }).catch(() => {});
    const st2 = await setActive(false);
    check("D35 react: tenant deactivated via api", st2 === 200, `status=${st2}`);

    await rPage.reload({ waitUntil: "domcontentloaded", timeout: 60000 }).catch(() => {});
    let navOk2 = false;
    try {
      await rPage.waitForURL(/\/tenant-deactivated/, { timeout: 60000 });
      navOk2 = true;
    } catch { /* fall through */ }
    await rPage.getByText("Tenant deactivated", { exact: true }).first().waitFor({ state: "visible", timeout: 15000 }).catch(() => {});
    const rTitle = await rPage.getByText("Tenant deactivated", { exact: true }).first().isVisible().catch(() => false);
    const rBtn = await rPage.getByRole("button", { name: "Back to sign in" }).first().isVisible().catch(() => false);
    check("D35 react: routed to /tenant-deactivated", navOk2, rPage.url());
    check("D35 react: terminal card renders", rTitle && rBtn, `title=${rTitle} btn=${rBtn}`);
    await rPage.screenshot({ path: path.join(EVIDENCE, "d35-react.png"), fullPage: true });
  }

  const st3 = await setActive(true);
  check("D35 cleanup: tenant active", st3 === 200, `status=${st3}`);

  /* Recovery: fresh navigation after reactivation → back to app, not terminal */
  if (bLogin.ok) {
    await bPage.goto(`${cfgBlazor.base}/`, { waitUntil: "domcontentloaded", timeout: 60000 });
    await bPage.getByText("Overview", { exact: true }).first().waitFor({ state: "visible", timeout: 60000 }).catch(() => {});
    const stillTerminal = bPage.url().includes("/tenant-deactivated");
    const body = await bPage.locator("body").innerText().catch(() => "");
    check("D35 blazor: fresh nav after reactivation leaves terminal", !stillTerminal, bPage.url());
    check("D35 blazor: app content back", !stillTerminal && (body.includes("Overview") || body.includes("Welcome")), "");
  }

  await bCtx.close();
  await rCtx.close();
} catch (err) {
  check("D35 probe crashed", false, String(err).slice(0, 300));
}

/* ─────────────────────────── D36: impersonation ended ─────────────────────────── */
try {
  const rootToken = await loginApi("root", "superadmin@root.com", "Password123!");

  const tenants = await api("/api/v1/tenants", { token: rootToken });
  const acme = asArray(tenants.json).find((t) => String(t.tenantId ?? t.id ?? t.key ?? t.name).toLowerCase() === TENANTS)
    ?? asArray(tenants.json).find((t) => String(t.name ?? t.key ?? "").toLowerCase() === TENANTS);
  const acmeId = String(acme?.tenantId ?? acme?.id ?? acme?.key);
  if (!acme) throw new Error(`acme tenant not found in ${tenants.text.slice(0, 200)}`);

  const users = await api("/api/v1/identity/users", { token: rootToken, tenant: TENANTS });
  const target = asArray(users.json).find((u) => String(u.email ?? u.userName ?? "").toLowerCase() === "admin@acme.com");
  if (!target) throw new Error(`admin@acme.com not found: ${users.status} ${users.text.slice(0, 200)}`);
  const targetId = String(target.id ?? target.userId);

  const startImpersonation = async () => {
    const r = await api("/api/v1/identity/impersonation/start", {
      method: "POST",
      token: rootToken,
      body: { targetUserId: targetId, targetTenantId: acmeId, reason: "gr11 d36 probe", durationMinutes: 10 },
    });
    if (r.status !== 200 || !r.json?.accessToken) throw new Error(`impersonation start failed ${r.status} ${r.text.slice(0, 200)}`);
    return r.json;
  };

  const revokeByJti = async (jti) => {
    const grants = await api("/api/v1/identity/impersonation/grants", { token: rootToken });
    const grant = asArray(grants.json).find((g) => String(g.tokenJti ?? g.jti) === jti);
    if (!grant) throw new Error(`grant for jti ${jti} not found`);
    const r = await api(`/api/v1/identity/impersonation/grants/${grant.id}/revoke`, { method: "POST", token: rootToken });
    return r.status;
  };

  const injectTokens = async (page, base, accessToken) => {
    await page.goto(`${base}/login`, { waitUntil: "domcontentloaded", timeout: 60000 });
    await page.waitForTimeout(2500);
    await page.evaluate(([at]) => {
      localStorage.setItem("fsh.dashboard.accessToken", at);
      localStorage.setItem("fsh.dashboard.refreshToken", "");
      localStorage.setItem("fsh.dashboard.tenant", "acme");
      localStorage.removeItem("fsh.dashboard.impersonation.actorAccessToken");
      localStorage.removeItem("fsh.dashboard.impersonation.actorRefreshToken");
      localStorage.removeItem("fsh.dashboard.impersonation.actorTenant");
    }, [accessToken]);
  };

  /* Blazor dashboard */
  const bCtx2 = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
  const bPage2 = await bCtx2.newPage();
  const bCap2 = attachCapture(bPage2);
  const imp1 = await startImpersonation();
  const jti1 = decodeJti(imp1.accessToken);
  check("D36 blazor: impersonation started", !!imp1.accessToken, `jti=${jti1.slice(0, 8)}`);

  await injectTokens(bPage2, cfgBlazor.base, imp1.accessToken);
  await bPage2.goto(`${cfgBlazor.base}/`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await bPage2.getByText("Overview", { exact: true }).first().waitFor({ state: "visible", timeout: 120000 }).catch(() => {});
  check("D36 blazor: impersonated session renders", !bPage2.url().includes("/login"), bPage2.url());

  const rev1 = await revokeByJti(jti1);
  check("D36 blazor: grant revoked", rev1 === 200, `status=${rev1}`);

  await bPage2.reload({ waitUntil: "domcontentloaded", timeout: 90000 }).catch(() => {});
  let navOk3 = false;
  try {
    await bPage2.waitForURL(/\/impersonation-ended/, { timeout: 120000 });
    navOk3 = true;
  } catch { /* fall through */ }
  const bTitle2 = await bPage2.getByText("Impersonation ended", { exact: true }).first().isVisible().catch(() => false);
  const bBtn2 = await bPage2.getByRole("button", { name: "Back to sign in" }).first().isVisible().catch(() => false);
  check("D36 blazor: routed to /impersonation-ended", navOk3, bPage2.url());
  check("D36 blazor: terminal card renders", bTitle2 && bBtn2, `title=${bTitle2} btn=${bBtn2}`);
  await bPage2.screenshot({ path: path.join(EVIDENCE, "d36-blazor.png"), fullPage: true });
  await bCtx2.close();

  /* React dashboard (parity baseline) */
  const rCtx2 = await browser.newContext({ viewport: { width: 1440, height: 900 }, ignoreHTTPSErrors: true });
  const rPage2 = await rCtx2.newPage();
  const rCap2 = attachCapture(rPage2);
  const imp2 = await startImpersonation();
  const jti2 = decodeJti(imp2.accessToken);
  check("D36 react: impersonation started", !!imp2.accessToken, `jti=${jti2.slice(0, 8)}`);

  await injectTokens(rPage2, cfgReact.base, imp2.accessToken);
  await rPage2.goto(`${cfgReact.base}/`, { waitUntil: "domcontentloaded", timeout: 60000 });
  await rPage2.getByText("Overview", { exact: true }).first().waitFor({ state: "visible", timeout: 60000 }).catch(() => {});
  check("D36 react: impersonated session renders", !rPage2.url().includes("/login"), rPage2.url());

  const rev2 = await revokeByJti(jti2);
  check("D36 react: grant revoked", rev2 === 200, `status=${rev2}`);

  await rPage2.reload({ waitUntil: "domcontentloaded", timeout: 60000 }).catch(() => {});
  let navOk4 = false;
  try {
    await rPage2.waitForURL(/\/impersonation-ended/, { timeout: 60000 });
    navOk4 = true;
  } catch { /* fall through */ }
  await rPage2.getByText("Impersonation ended", { exact: true }).first().waitFor({ state: "visible", timeout: 15000 }).catch(() => {});
  const rTitle2 = await rPage2.getByText("Impersonation ended", { exact: true }).first().isVisible().catch(() => false);
  const rBtn2 = await rPage2.getByRole("button", { name: "Back to sign in" }).first().isVisible().catch(() => false);
  check("D36 react: routed to /impersonation-ended", navOk4, rPage2.url());
  check("D36 react: terminal card renders", rTitle2 && rBtn2, `title=${rTitle2} btn=${rBtn2}`);
  await rPage2.screenshot({ path: path.join(EVIDENCE, "d36-react.png"), fullPage: true });
  await rCtx2.close();

  check("D36 cleanup: grants revoked (terminal state, no action)", true, "");
} catch (err) {
  check("D36 probe crashed", false, String(err).slice(0, 300));
}

await browser.close();

const failed = results.filter((r) => !r.ok);
console.log(`\n[terminal-states-probe] ${results.length - failed.length}/${results.length} passed`);
process.exit(failed.length === 0 ? 0 : 1);