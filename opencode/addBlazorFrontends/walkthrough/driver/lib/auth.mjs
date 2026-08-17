export async function login(page, appCfg, capture) {
  await page.goto(`${appCfg.base}/login`, { waitUntil: "load", timeout: 30000 });
  await page.waitForTimeout(1200);
  const s = appCfg.selectors;
  const field = (key) =>
    appCfg.framework === "blazor" ? page.getByLabel(s[key], { exact: false }).first() : page.locator(s[key]).first();
  try {
    const tenant = field("tenant");
    await tenant.waitFor({ state: "visible", timeout: 20000 });
    await tenant.fill(appCfg.login.tenant);
    const email = field("email");
    await email.waitFor({ state: "visible", timeout: 10000 });
    await email.fill(appCfg.login.email);
    const pwd = field("password");
    await pwd.waitFor({ state: "visible", timeout: 10000 });
    await pwd.fill(appCfg.login.password);
    await pwd.blur().catch(() => {});
  } catch (err) {
    return { ok: false, url: page.url(), error: `login fields: ${String(err).slice(0, 200)}` };
  }
  const labels = ["Sign in", "Sign In", "Login", "Log In", "Sign in to your account"];
  let clicked = false;
  for (const label of labels) {
    const btn = page.getByRole("button", { name: label, exact: false }).first();
    try {
      await btn.waitFor({ state: "visible", timeout: 4000 });
      const deadline = Date.now() + 5000;
      while (await btn.isDisabled()) {
        if (Date.now() > deadline) break;
        await page.waitForTimeout(200);
      }
      await btn.click();
      clicked = true;
      break;
    } catch {
      /* try next label */
    }
  }
  if (!clicked) return { ok: false, url: page.url(), error: "submit button not found" };
  try {
    await page.waitForURL((u) => !u.pathname.toLowerCase().endsWith("/login") && u.pathname !== "/login", {
      timeout: 25000,
    });
  } catch (err) {
    return { ok: false, url: page.url(), error: `post-login nav: ${String(err).slice(0, 200)}`, pageText: await page
        .locator("body")
        .innerText()
        .then((t) => t.slice(0, 300))
        .catch(() => "") };
  }
  await page.waitForTimeout(1500);
  return { ok: true, url: page.url() };
}