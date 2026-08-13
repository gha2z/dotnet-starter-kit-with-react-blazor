# Chapter 7 — React Parity Completion & Hardening (hands-on)

This chapter is the educational companion to `Phase-07-Parity-Completion/plan.md`. It explains the
**why** behind the parity conventions, the two hotfixes, and the patterns you will use for every
remaining page. Read it before starting any dashboard 3.x page or the remaining admin pages
(2.10 Impersonation, styled 404; 2.9 Settings is delivered — see 7.8).

## 7.1 Why parity is structural, not visual

The React apps (`clients/admin`, `clients/dashboard`) are the source of truth. A Blazor page is
"done" only when three things match the React page:

1. **Route + URL shape** — same path, same query parameters (e.g. `?search=`, `?page=`).
2. **Behavior** — same server calls, same optimistic updates, same loading/empty/error states.
3. **Look & feel** — same layout structure, using the shared `fsh.css` primitives
   (`FshKpiTile`, `FshPager`, `FshFilterBar`, `FshLoadingRow`, `FshSectionRule`, …) so the two
   render identically without re-implementing CSS.

The sidebar is *the* parity backbone: `MainLayout` in both apps mirrors the React nav sources
1:1 — every entry exists, gated by the same permission strings, and unimplemented pages keep their
menu entries and land on the 404 page (full-parity policy). When you add a page, you **remove a row
from the gap table** in `plan.md`; you never remove a nav entry.

## 7.2 The two hotfixes (and the lessons)

### Route binding: `{Id:guid}` vs `string Id`

```razor
@page "/users/{Id:guid}"   ← BROKEN with [Parameter] string Id
[Parameter] public string Id { get; set; }
```

A route constraint like `:guid` doesn't just validate the URL — the Router **converts the value**:
the route parameter you receive is a `Guid` object. Binding a `Guid` to a `string` property throws
`Arg_InvalidCastException` ("Unable to set property 'Id'…"), surfacing as the Blazor error UI.
`TenantDetailPage` worked because it used a plain `{Id}` (no constraint → string).

**Rule:** detail routes bind `[Parameter] string Id` and use `@page "/{resource}/{Id}"` — no
constraints. This also matches React, which treats ids as strings end-to-end.

**Regression testing:** unit tests that pass `Id` via `Render<T>(p => p.Add(...))` do **not** exercise
route binding — the crash only reproduces through the real `Router`. The
`RouteBindingRegressionTests` class renders `<Router>` + `<RouteView>` and drives
`NavigationManager.NavigateTo($"/users/{id}")`:

```csharp
var router = Render<Router>(p => p
    .Add(x => x.AppAssembly, typeof(FSH.Admin.Wasm.App).Assembly)
    .Add(x => x.Found, FoundFragment())        // RouteView + tiny RouteHost layout
    .Add(x => x.NotFound, NotFoundFragment()));
Services.GetRequiredService<NavigationManager>().NavigateTo($"/users/{id}");
router.WaitForAssertion(() => router.Markup.ShouldContain("jane@example.com"));
```

Add one of these tests for **every** new detail route (tickets, invoices, products, …).

**Two harness gotchas that made the first Router tests fail (both now fixed):**

1. **`.NET 10 `LayoutComponentBase` no longer auto-renders `Body`.** In older .NET the base class
   rendered `@Body` itself, so a pure-C# test layout worked. In .NET 10 a layout must render its
   body explicitly — a C# test layout that doesn't override `BuildRenderTree` renders *nothing*,
   and the routed page silently never appears (`markup=[]`, no exception, page component never
   instantiated). Fix for the test layout:

   ```csharp
   public sealed class RouteHost : LayoutComponentBase
   {
       protected override void BuildRenderTree(RenderTreeBuilder builder)
       {
           builder.AddContent(0, Body);
       }
   }
   ```

2. **Navigate before rendering.** The Router's first render matches the current URI — at
   `http://localhost/` that's the app's overview page, which has its own service dependencies
   (`ITenantService`/`IBillingService`) and would fail DI in the test. `NavigateTo(...)` before
   `Render<Router>(...)` makes the initial match the page under test and keeps the test
   dependency-light.

**Status: all Router regression tests pass (114/114 admin suite).**

### `<base href="/" />` ordering

`index.html` loads assets with relative URLs (`_content/MudBlazor/MudBlazor.min.css`). The browser
resolves relative URLs against the `<base>` element — **but only once it has parsed it**. If the
`<base>` tag appears after the `<link>` tags, a full page load at `/roles/…` resolves the links
against the *document URL*: `GET /roles/_content/… → 404` (the "messy" reload page). The login page
at `/` never shows the problem because `/` happens to be the base.

**Rule:** `<base href="/" />` is the first element in `<head>` (before every `<link>`). The
`IndexHtmlGuardTests` class scans both `index.html` files on every test run and fails if a `<link>`
precedes the base tag — cheap insurance against re-breaking deep links.

## 7.3 The shared toolkit you will reuse

| Component | Use for | React counterpart |
|---|---|---|
| `FshNavSection` | accordion sidebar sections | `AccordionSection` |
| `FshNotificationBell` | topbar notifications dropdown | `NotificationsBell` |
| `FshPager` | "Showing N–M of T · folio PP/TT" + prev/next | `Pagination` |
| `FshFilterBar` | search/filter row above lists | `FiltersBar` |
| `FshLoadingRow` | skeleton row while loading | loading skeletons |
| `FshKpiTile` | stat tile with icon + positive/negative delta | `KpiCard` |
| `FshSectionRule` | section divider under headers | `SectionTitle` |

Services live in `BlazorShared/Services` (typed `HttpClient` via `AuthDelegatingHandler`), DTOs in
`BlazorShared/Models/{Module}/`, permission constants in `BlazorShared/Permissions/` (mirror of the
server's `*Permissions.cs` — e.g. `WebhooksPermissions.Subscriptions.View` is the constant whose
*value* is `"Permissions.Webhooks.View"`; check the server constant when the class name and value
diverge).

## 7.4 Theming model (post-parity-sprint)

- `FshThemeService` ctor: `(IJSRuntime, storageKey, defaultMode)`.
- Dashboard: `("fsh.theme", ThemeMode.System)` — Light/Dark/System menu, OS-following.
- Admin: `("fsh.admin.theme")` — binary toggle, default Dark (legacy `SetAsync(bool)` still works).
- Stored raw values: `"light" | "dark" | "system"` (same keys the React apps write — a shared
  browser profile keeps the theme in sync across apps).
- `System` resolves via `prefersDark()` inside `fshTheme.js` at initialize and on each switch.
- New theme UI on dashboard uses inline check-mark markup — MudBlazor 9.7 has no
  `MudMenuItem.EndIcon` (it's silently ignored).

## 7.5 Testing stack (current state)

- bUnit **2.0.66**: `BunitContext` (not `TestContext`); `Render<T>` (not `RenderComponent<T>`);
  boolean attributes like `aria-expanded` are normalized (false → attribute removed, true → bare),
  so assert **presence** via `Attributes.Any(a => a.Name == ...)`.
- JS module interop (`InvokeAsync("import", …)`) is not covered by loose-mode `JSInterop.Setup` —
  use the fake `IJSRuntime`/`IJSObjectReference` pattern from `FshThemeServiceTests`.
- Router-level route tests: see 7.2. Base-href guard: see 7.2.
- Admin suite: 146 tests. Dashboard suite: 18 tests. Both green (0 warnings — the build
  treats warnings as errors).

## 7.6 MAUI workload saga (this machine) — RESOLVED

`dotnet workload install maui` **must run elevated** (UAC), and the workload store on this machine
is fragile: a failed non-elevated attempt deleted the manifest packs under
`C:\Program Files\dotnet\sdk-manifests\10.0.300\`, breaking every `dotnet` build with
`MSB4242 … missing manifests`. Recovery recipe (documented in the log trail in `%TEMP%\maui-*`):

1. Elevated: remove `sdk-manifests\10.0.300\workloadsets\10.0.302` (stale set).
2. Elevated: **recreate the empty folder** `workloadsets\10.0.302` (the installer requires it).
3. Elevated: `dotnet workload install maui`.
4. Elevated: `dotnet workload update` (kept the set on maui 10.0.20 / manifest 10.0.100 band).

### Final outcome — Hybrid builds green (4 TFMs, 0 warnings)

The original failure was **not** the machine: `.NET 10 removed the legacy `Microsoft.NET.Sdk.Maui`
SDK`. The FSH.Hybrid project file was migrated to the current .NET 10 MAUI conventions:

- **SDK**: `Microsoft.NET.Sdk.Razor` (Blazor WebView's targets require the Razor SDK's
  `StaticWebAssetsPrepareForRun`; plain `Microsoft.NET.Sdk` fails with `MSB4057`).
- **Platforms folder required**: .NET 10 no longer auto-generates the app entry point. Added
  `Platforms\Windows\App.xaml(.cs)` (`MauiWinUIApplication`), `Platforms\iOS\Program.cs` +
  `AppDelegate.cs`, `Platforms\MacCatalyst\Program.cs` + `AppDelegate.cs`,
  `Platforms\Android\MainActivity.cs` + `MainApplication.cs` (copy the `dotnet new maui-blazor`
  template, swap namespaces) — otherwise `CS5001` (no `Main`).
- **Versions pinned** (the legacy SDK injected them before): `Microsoft.Maui.Controls` +
  `Microsoft.Maui.Controls.Compatibility` + `Microsoft.AspNetCore.Components.WebView.Maui` at
  `$(MauiVersion)` (= 10.0.20 from the workload), `CommunityToolkit.Maui` **13.0.0** (14.x/15.x
  require Maui Controls ≥ 10.0.60, which this workload set does not ship),
  `CommunityToolkit.Mvvm` 8.4.2, `Microsoft.Extensions.Logging.Debug` 10.0.0, `MudBlazor` 9.7.0.
- `WindowsPackageType=None` (no MSIX build), maccatalyst `SupportedOSPlatformVersion` 15.0
  (CommunityToolkit.Maui requirement; 14.0 → CA1416).
- `App.xaml.cs` overrides `CreateWindow` instead of the obsolete `MainPage` setter.
- `Resources\` was empty — created `AppIcon\appicon.svg`/`appiconfg.svg`, `Splash\splash.svg`.

Build check: `dotnet build clients\FSH.Hybrid\FSH.Hybrid\FSH.Hybrid.csproj` → Build succeeded,
0 warnings, 0 errors (net10.0-android / -ios / -maccatalyst / -windows10.0.19041.0).

> Don't use `-p:TargetFrameworks=…` restore overrides against this project — it pollutes
> `clients\BlazorShared\obj\project.assets.json` (NETSDK1005 on the RCL's `net10.0` target);
> clean both `obj`/`bin` folders and restore normally if that happens.

The Hybrid app itself still has **zero `.razor` pages** - Phase 5 builds the shell + screens
from scratch. Both Blazor WASM suites remain green after all of this: admin **114/114**, dashboard
**18/18** (also bumped transitive `AngleSharp` to 1.7.0 in the test projects - 1.3.0 triggers
`NU1902` GHSA-pgww-w46g-26qg).

## 7.7 Hotfix 3 — role-permissions route asymmetry (404 on role detail)

Symptom: clicking a role in the admin roles page rendered the page fine but showed
`Failed to load role: net_http_message_not_success_statuscode_reason, 404, Not Found` — the
browser console showed `GET /api/v1/identity/roles/{id}/permissions → 404`.

**Root cause:** the backend registered the two role-permissions endpoints on the identity group
without the `/roles` segment — `GET /{id:guid}/permissions` and `PUT /{id}/permissions` —
i.e. `/api/v1/identity/{id}/permissions`. The React admin + dashboard API clients mirrored that
asymmetric path (a `roles.ts` comment even documented the asymmetry), while `BlazorShared/RoleService`
used the canonical `/roles/{id}/permissions`. A Phase-2 fix on the Blazor side had regressed on the
server. The 404 was real — the route simply did not exist.

**Lesson:** a URL is a contract. When two frontends + an `.http` scratch file disagree with the
server, the server's own convention wins: every sibling endpoint lives under `/roles/`, so the
permissions endpoints belong there too. `Phase-02-Admin-Feature-Pages/hands-on-phase-2.md` (§10
table) records the same bug on the Blazor side — grep the repo for `/{id}/permissions` before
touching these routes again.

**Fix (server-first, then every consumer in the same change):**

1. `GetRolePermissionsEndpoint` → `MapGet("/roles/{id:guid}/permissions")`
2. `UpdateRolePermissionsEndpoint` → `MapPut("/roles/{id}/permissions")`
3. `clients/admin/src/api/roles.ts` + `clients/dashboard/src/api/identity.ts` → add `/roles/`,
   delete the asymmetry comment.
4. Playwright specs `clients/admin/tests/roles/roles.spec.ts` +
   `clients/dashboard/tests/identity/roles.spec.ts` → update mock URLs / PUT assertion.
5. Integration tests: `RolePermissionTests`, `RolePrivilegeEscalationTests`,
   `SystemRoleProtectionTests`, `GroupRolePermissionTests`, `PermissionCacheInvalidationTests`
   → `{IdentityBasePath}/roles/{id}/permissions`.
6. `identity-roles.http` → PUT and POST lines gained the missing `/roles` (POST `/api/v1/identity`
   was broken too).

**Verification:** backend build 0 warnings; admin Blazor 114/114 + dashboard Blazor 18/18; admin PW
roles 7/7 + dashboard PW roles 5/5 (after `npx playwright install chromium` — browsers were not
installed on this machine); 17 role-permission integration tests green against real Postgres
(Testcontainers).

## 7.8 Delivering 2.9 Settings — the lessons

Settings landed as four routes behind a shared `SettingsScaffold` in
`clients/admin-blazor/FSH.Admin.Wasm/Pages/Settings/` (`ProfilePage`, `SecurityPage` with
`PasswordSection` + `TwoFactorSection`, `SessionsPage`, `AppearancePage`), backed by new/changed
services in `BlazorShared` (`ISessionService`, `ITwoFactorService`, `IUserService` additions).
The admin suite grew from 114 → **146** tests (28 new: 4 page suites + 4 settings routes driven
through the real `Router`). Gotchas worth remembering:

**1. `MudCard` has no `OnClick` in MudBlazor 9.7.** An `@onclick` on `<MudCard>` compiles but is
silently swallowed into `UserAttributes` — a dead click handler. `AppearancePage`'s theme cards are
clickable `<div role="button" tabindex="0" @onclick @onkeydown>` instead (and that's what the tests
click).

**2. bUnit asserts the HTML-escaped markup.** A button rendering `Verify & enable` appears in
`cut.Markup` as `Verify &amp; enable`. Match the escaped form: `ShouldContain("Verify &amp; enable")`.

**3. Inline-MudDialog needs the providers.** The app renders `MudDialogProvider`/`MudPopoverProvider`/
`MudSnackbarProvider` in `App.razor`, but a bare `Render<SecurityPage>()` has none — opening the 2FA
disable dialog fails. The test project's `TestShell.razor` mounts the providers around
`@ChildContent`; dialog tests do `Render<TestShell>(p => p.AddChildContent<SecurityPage>())`, and the
dialog itself is rendered by the provider **outside** the page subtree — reach its inputs via
`cut.FindAll("div.mud-dialog input")`, not `section.FindAll("input")`.

**4. `@bind-Valid` on `MudForm` is stale read-after-write.** The bound flag updates only after
`Validate()` completes, so `if (!_formValid)` immediately after `Validate()` is wrong and form
submission tests fail. Use the pattern from `RoleCreateDialog`: `await _form.ValidateAsync();
if (!_form.IsValid) return;`.

**5. Register every service before the container renders.** `RenderRouter` calls
`GetRequiredService<NavigationManager>()`, freezing the container, so inject `ITwoFactorService`/
sessions/theme substitutes *before* rendering — and build the theme from bUnit's `JSInterop.JSRuntime`
(setup's `JSInterop.JSRuntime`), never from `Services.GetRequiredService<IJSRuntime>()` after render.

**6. Settings routes are auth-gated, not permission-gated.** The four `@page "/settings/*"` routes
carry `[Authorize]` and no `PermissionsSettings.*` claim — matching React, where any signed-in user
sees profile/security/sessions/appearance. Check the React route guards before adding a
`FshPermissionGate` on a settings page.
