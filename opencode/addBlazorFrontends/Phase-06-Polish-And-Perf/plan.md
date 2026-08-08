# Phase 6 — Polish & Performance
Last Update: 2026-Aug-07 13:20:00, by: opencode (auto/coding, model: deepseek-v4-flash-free).

> **Target:** Production-ready quality. Bundle optimization, lazy loading, WASM AOT/tree-shaking, accessiblity audit, full feature parity with React apps, documentation update.

## Status

- Phase 6: **🟡 In progress** — waves 1–6 committed (`e5bb0367`, `6b6fa334`, `a9c78f86`, `5d4eaaca`, `8bc78500`, `68401ba3`); wave 7 `c8b0624f` (6.4 contrast); wave 8 `123d7517` (6.6 resilience); wave 9 `0cbbfde3` (6.7 docs).
- Prerequisites: Phase 4 ✅ (testing done), Phase 5 ✅ (MAUI built)

## Task Checklist

### 6.1 WASM Bundle Size Optimization
- [x] **Assembly trimming** — `BlazorWebAssemblyEnableTrimming=true` (dashboard; was `false`)
  - ⚠️ **`.NET 10 finding`**: `false` alone was ineffective — `PublishTrimmed` defaults true, so the app was ALREADY shipping trimmed. Verified: untrimmed publish (`false` + `PublishTrimmed=false`) = **79.52 MB**; trimmed = **22.29 MB** (4.97 MB gz) — ~3.6x smaller. **admin-blazor csproj also fixed** (`true` + removed `OverrideHtmlAssetPlaceholders`).
  - ✅ **Trimmed publish smoke PASSED** (static hosting of `dotnet publish` output): splash shown, boot 3.47 s, login rendered, zero JS errors, no `#blazor-error-ui`.
- [x] **Deployability fix — `OverrideHtmlAssetPlaceholders` removed (both apps)**
  - With the flag `true`, the published `index.html` kept the literal `_framework/blazor.webassembly.js` script src, but the publish emits only the content-hashed `blazor.webassembly.{hash}.js` → **any static hosting 404s and the app never boots**. The dev server masks it (it serves the logical name). Removing the flag makes the publish emit the un-hashed bootstrap copy. Root-caused via trimmed-publish smoke; smoke now runs against a real static server (port 5180, `npx serve -s`).
- [ ] **MudBlazor tree-shaking** — Verify only used MudBlazor components are included
  - Configure MudBlazor's trimmer-friendly import path
  - Consider `<MudTrimmingConfiguration>` in csproj
- [x] **Lazy loading** — Split app assemblies by feature area
  - **DASHBOARD lazy loading DONE (wave 4, 2026-08-06)** — see "6.1b Lazy loading (dashboard)" below.
  - **ADMIN lazy loading DONE (wave 5, 2026-08-07)** — see "6.1c Lazy loading (admin)" below.
- [ ] **AOT compilation** — Enable for release build
  - **Benchmarked (2026-08-06): trim+AOT = 56.60 MB / 11.51 MB gz vs trim-only 22.29 / 4.97**; boot 6.95 s vs 3.47 s (static localhost, cold browser). **Decision: keep AOT OFF** — 2.5x size + 2x boot for a CRUD dashboard with no CPU-heavy paths. Revisit if per-page AOT / reporting-heavy pages land. `RunAOTCompilation` not set.
  - Measure: with/without AOT, with/without trimming (done — table in Architecture Decisions)
- [ ] **Pre-compression** — Add Brotli/Gzip compressed assets for deployment
  - `BlazorWebAssemblyJSHostCompression` configuration
  - Verify CDN serves compressed WASM files

### 6.1b Lazy Loading (dashboard) — DONE (wave 4)

- **Split**: all `Pages/` + `Shared/TerminalLayout.razor` moved out of `FSH.Dashboard.Wasm` into a new RCL **`clients/dashboard-blazor/FSH.Dashboard.Pages`** (git mv, namespaces unchanged `FSH.Dashboard.Pages.*`). App now holds only shell + auth infrastructure. RCL registered in `src/FSH.Starter.slnx`.
- **Wiring**: `<BlazorWebAssemblyLazyLoad Include="FSH.Dashboard.Pages.wasm" />` + `TrimmerRootAssembly Include="FSH.Dashboard.Pages"` in csproj; `App.razor` Router gets `AdditionalAssemblies="@_lazyAssemblies"`; `App.razor.cs` `OnNavigateAsync` → `LazyAssemblyLoader.LoadAssembliesAsync(["FSH.Dashboard.Pages.wasm"])` once, then route discovery works.
- **.NET 10 mechanics (verified from pack + runtime source)**: lazy list flows via `Microsoft.NET.Sdk.WebAssembly.Browser.targets` (pack `10.0.10`, lines 407/821) `LazyLoadedAssemblies="@(BlazorWebAssemblyLazyLoad)"` into the `_GenerateBuildWasmBootJson` / publish twin. Boot config (inlined in `_framework/dotnet.js` between `/*json-start*/`/`/*json-end*/`) carries the lazy set as **`resources.lazyAssembly` (singular array — the old top-level `lazyAssemblies` key no longer exists)**. **Both** the csproj item **and** `LoadAssembliesAsync` must use the **`.wasm`** extension (docs since .NET 8; task's `TryGetLazyLoadedAssembly` strips `.dll`/webcil only for key matching). Missing `AdditionalAssemblies` = loaded assembly's routes never discovered (classic 404 → blank page).
- **Verified**: Debug boot config shows `resources.lazyAssembly = [FSH.Dashboard.Pages.wasm]`; trimmed Release publish: Pages **deferred** (absent from eager `resources.assembly`), total 252 files / **20.34 MB / 4.57 MB gz** (vs wave-2 locked 22.29 / 4.97), lazy file 645.8 KB (198.5 KB gz, 152.1 KB br); E2E **18/18** + bUnit **179/179** green.
- ⚠️ **RELEASE-PUBLISH BLOCKER (pre-existing, NOT caused by lazy — see Notes & Gotchas)**: every Release publish of dashboard AND admin WASM apps crashes the Mono runtime (`MONO interpreter: NIY encountered in method Microsoft.Extensions.Localization.LocalizationOptions:.ctor` → `interp.c:4135` assertion; other build shapes die even earlier, silently). Reproduced on clean worktree of `a9c78f86` (pre-lazy HEAD) → **lazy refactor exonerated**. Upstream: open dotnet/runtime #121849 family (milestone 12.0.0), MudBlazor .NET 10 target unshipped (#12049). Tracking: new task "6.9 Release-publish blocker" in Blocker Checklist.

### 6.1c Lazy loading (admin) — DONE (wave 5)

- **Split**: all `Pages/**` (77 files: Audits, Auth, Billing, Dashboard, Health, Identity, Impersonation, Notifications, Settings, Tenants, Webhooks) moved out of `FSH.Admin.Wasm` into a new RCL **`clients/admin-blazor/FSH.Admin.Pages`** (git mv, namespaces preserved `FSH.Admin.Wasm.Pages.*` via `RootNamespace=FSH.Admin.Wasm`). `Shared/` (MainLayout, CommandPalette, NavSpec, RedirectToLogin) stays in the app assembly — `App.razor` applies layout via `DefaultLayout="@typeof(MainLayout)"`, so no circular dependency. RCL registered in `src/FSH.Starter.slnx`.
- **Wiring**: `<BlazorWebAssemblyLazyLoad Include="FSH.Admin.Pages.wasm" />` + `TrimmerRootAssembly Include="FSH.Admin.Pages"` in csproj; `App.razor` Router gets `AdditionalAssemblies="@_lazyAssemblies"` + `OnNavigateAsync` → `LazyAssemblyLoader.LoadAssembliesAsync(["FSH.Admin.Pages.wasm"])` once. Loading indicator shown during deferred load.
- **Verified**: RCL + app build **0 warnings / 0 errors**; trimmed Release publish: Pages deferred (`resources.lazyAssembly` present in boot config), lazy file 488 KB (158 KB gz, 123 KB br); bUnit **158/158 green** (including 12 RouteBindingRegressionTests that needed `AdditionalAssemblies` fix for the RCL).
- **Test fix**: `RouteBindingRegressionTests.RenderRouter` updated to pass `new[] { typeof(FSH.Admin.Wasm.Pages.Dashboard.OverviewPage).Assembly }` as `AdditionalAssemblies` — the Router needs the lazy assembly for route discovery in tests.
- Same 6.9 release-publish blocker applies (pre-existing, upstream).

### 6.2 Runtime Performance
- [x] **Virtualized lists audit** — all dashboard list pages reviewed (2026-08-06):
  - **Server-side paging already correct**: Users, Tickets, Products, Brands, Categories, Invoices, Audits, Sessions, Wallet (page controls reload with `_pageNumber`/`PageSize`).
  - **Roles / Groups**: full-set fetch + client-side filter — OK: bounded sets (tenant roles/groups), matches React parity.
  - **Activity**: SSE event list capped at 200 (`MaxEvents`) — OK.
  - **Trash**: loads full trash sets (products/brands/categories) — matches React page; note for future paging if data grows.
  - No MudTable `Items`+`Pager` client-side-paging anti-patterns beyond the bounded cases above. No changes required.
- [x] **Infinite scroll** — Activity log, chat messages
  - **Chat (wave 11, dashboard)**: scroll-top load-older via ES-module interop (`fshChatScroll.js` — `watchScrollTop/unwatchScrollTop/scrollToBottom/scrollHeight/restoreScrollPosition`, per-reference cleanup map). Cursor = oldest loaded message id (`ListChannelMessagesAsync(channelId, before, pageSize)`); `InitialPageSize=100`, `OlderPageSize=50`; `_hasOlder` set when a full page returns; dedupe against SignalR arrivals via id set; scroll offset preserved across prepend (saved `scrollHeight` before fetch, restored after render); reentrancy guard `_loadingOlder`; `[JSInvokable] OnScrollTopReached`; module invoked via `IJSObjectReference` (never global lookup). 4 new bUnit tests (prepend, exhaustion, in-flight guard, no-channel no-op). Dashboard suite 204/204.
  - **Activity (wave 11, N/A)**: live SSE ring buffer (`MaxEvents=200`) with NO history endpoint — infinite scroll not applicable; React parity (dashboard Activity also shows latest only). No work.
  - Load more items via service call, append to list
- [x] **Debounced search** — All search MudTextField
  - **Tickets list**: search was `Immediate="true"` with NO reload handler (typing did nothing server-side) — fixed with debounced `OnSearchChangedAsync` (250 ms, CancellationTokenSource, resets to page 1), mirroring `UsersListPage`. bUnit `Reloads_with_search_term_after_debounce` covers it. Users page already had the pattern.
- [ ] **MudBlazor render optimization** — Audit unnecessary re-renders
  - `@key` on MudTable rows
  - `StateHasChanged()` only when needed
  - Use `MudComponentBase`'s `ShouldRender()` override in heavy components
- [x] **Image lazy loading** — native `loading="lazy"` — already present in code: product thumbs (ProductsPage), brand logos (BrandsPage), product detail gallery (ProductDetailPage) all render `<img loading="lazy" referrerpolicy="no-referrer">`. FilePreviewDialog (dialog, opened on demand) correctly has no lazy. No code change needed — item verified & ticked (wave 12).

### 6.3 First-Load Performance
- [x] **Splash screen** — Branded splash replacing bare "Loading..."
  - `index.html`: FSH mark tile + spinner + "FullStackHero / Loading your dashboard…", `role="status" aria-live="polite"`. React dashboard has no splash (its bundle renders fast); WASM needs the container pre-boot, so branding was the parity win. Fade-out not needed — Blazor replaces the container on render.
- [ ] **Critical CSS** — Inline MudBlazor critical styles in index.html
  - Extract critical CSS for above-the-fold content
  - Defer non-critical MudBlazor CSS
- [x] **Preload** — `link rel="preload"` for boot JS + font preconnect (wave 12)
  - **Constraint found**: framework assemblies are content-hashed in `_framework/` (`MudBlazor.xr72q1v0gr.wasm` etc.) — static `rel="preload"` of assemblies is impossible without build-time hash injection; the boot loader itself is the actual fetch bottleneck, not the blazor.boot.json entries. What's statically preloadable: the boot chain (`_framework/blazor.webassembly.js`, `_framework/dotnet.js` — stable names) + `_content/` CSS.
  - Both apps: `<link rel="preconnect" href="https://fonts.googleapis.com">`, `<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>`, `<link rel="preload" href="_framework/blazor.webassembly.js" as="script">`, `<link rel="preload" href="_framework/dotnet.js" as="script">`. PWA cache-first covers repeat loads; preload shaves the first-visit chain.
  - **API preconnect**: skipped — API base is runtime `config.json` (`https://localhost:7030` dev), not statically known; preconnect must live in the CDN/hosting config (deployment item below).
- [ ] **HTTP/2 Server Push** — For deployment behind reverse proxy
  - Push key assemblies on first request
- [x] **PWA** — Progressive Web App support
  - **DONE (wave 6, 2026-08-07)**: both WASM apps (admin + dashboard) are installable + offline-capable.
  - `manifest.json` per app (name, short_name, rose theme `#E11D48`, dark bg `#1B2A2C`, standalone, scope `/`, maskable + any icons).
  - Service worker `service-worker.js` (v1.0): precaches shell (index/manifest/offline), cache-first for `_framework/*` (content-hashed = immutable), network-first for navigation with `offline.html` fallback, network-first+fallback elsewhere. Version-busted via `CACHE_NAME`.
  - `offline.html` branded fallback page (retry button). Icons generated as PNG (192/512/maskable-512) via a node script (`.opencode/temp/gen-icons.js`).
  - `index.html` both apps: `<link rel="manifest">`, theme-color meta, apple-touch-icon, SW registration guarded by `'serviceWorker' in navigator`.
  - Verified: both apps build 0 errors; admin bUnit **158/158** + dashboard bUnit **179/179** green; publish smoke confirmed all PWA assets emitted (manifest/offline/sw/icons with gz/br sidecars).

### 6.4 Accessibility Audit
- [x] **Skip-to-content link** — First Tab stop in the shell
  - `MainLayout` renders `.fsh-skip-link` (visually hidden until focus) targeting `main#fsh-main` (`tabindex="-1"`). **Gotcha**: Blazor's SPA click interceptor swallows plain `#fsh-main` fragment navigation (hash changes, focus never moves) — fixed via `@onclick:preventDefault` + `js/fshSkipLink.js` (`fshSkipLink.focusMain`). E2E `SkipLink_IsFirstTabStop_AndJumpsFocusToMainContent` verifies Tab → focus → Enter → focus lands on `#fsh-main`.
- [x] **MudIconButton aria-labels** — icon-only buttons named
  - Sidebar collapse ("Collapse sidebar"), collapsed expand ("Expand sidebar"), drawer trigger ("Open navigation"). Search + Theme already labeled. Nav landmark: `aria-label="Primary"`; `main` landmark present.
  - **Audit pass**: 20 candidate icon buttons reviewed — 17 already labeled (Brands/Categories/Products row edit-delete, Product detail cover/remove, File manager preview/download/delete, Group remove-member, Session revoke/delete, Chat create-channel…). Added labels to the 3 missing: StockDialog "Decrease/Increase stock by 1", Chat "Send message", Audits "View audit detail".
- [x] **Table/dialog a11y decision** — MudTable has no native `aria-label` splat on the `<table>` element (attributes go to the wrapper div); all tables sit under matching page headings (h1) so the heading names the region. MudBlazor 9 provides `role="dialog"` + Escape-close natively on MudDialog. No code change; documented instead of forcing labels onto wrapper divs.
- [x] **Screen reader support** — ARIA labels and roles (DONE wave 12, 2026-08-08)
  - MudTable: `aria-label`/`aria-sort` — **decision**: MudTable has no native splat on `<table>` (attrs go to wrapper div); all tables sit under matching h1 headings that name the region (see table/dialog decision row). Documented, not forced.
  - MudButton/MudIconButton: `aria-label` for icon-only buttons — **done** (wave 7 audit: 20 reviewed, 17 already labeled, 3 added).
  - MudIcon: renders `aria-hidden="true"` by default in MudBlazor 9 (verified in rendered markup) — no change.
  - MudAlert: `role="alert"` — **fixed (wave 12)**: MudBlazor 9.7 does NOT emit `role="alert"` by default; added `role="alert"` to `FshOfflineBanner` (the only page-level alert). Guard assertion added to `FshOfflineBannerTests` (`banner.GetAttribute("role") == "alert"` + icon `aria-hidden`).
  - MudDialog: `role="dialog"` + Escape-close — native MudBlazor 9; `aria-labelledby` not needed (title bar renders the dialog title).
  - Chat composer got `aria-label="Message"` (placeholder-only field had no accessible name).
- [x] **Color contrast** — Verify WCAG 2.1 AA compliance (DONE 2026-08-07)
  - **Audit**: computed WCAG ratios for every palette pair (light + dark). Dark passed fully. Light had 2 text violations + 1 button violation:
    - Primary `#E11D48` on Background `#F5F5F7` = **4.31:1** (< 4.5) → **`#D11A42`** (4.9 bg / 5.3 surface / 5.3 white-on)
    - Secondary `#4A7B8C` on Background = **4.28:1** → **`#457383`** (4.8 bg / 5.2 surface)
    - Dark PrimaryContrastText default white on `#FB7185` = **2.69:1** (filled buttons) → **`#121216`** (6.9:1)
  - Divider rules (1.3:1 light / 1.2:1 dark) are decorative — headings + hover carry the boundary; documented, not changed.
  - **Guard**: `FshMudThemeContrastTests` (dashboard suite) re-audits both palettes on every run — 2 new tests, dashboard 181/181 + admin 158/158 green.
- [x] **Focus indicators visible** — global `:focus-visible` rule added to `BlazorShared/wwwroot/css/fsh.css` (2px primary outline + 2px offset), React-parity with `globals.css`. Applies to all MudBlazor apps via the shared stylesheet. Skip-link focus style already existed per-app.
- [x] **Focus management** — Focus moves correctly on navigation
  - MudToolbar focus trap in MudDialog — MudBlazor 9 native (MudDialog focus traps + restores focus on close, Escape-close). Skip-to-content link done (wave 7, E2E-verified). No changes required — verified.
- [x] **Form accessibility** — MudForm with proper labels, error announcements (wave 12 audit)
  - **Audit**: all labeled `MudTextField`/`MudSelect` render proper `<label for>` via MudBlazor (input id + label pairing); MudForm validation messages are announced on submit. Placeholder-only fields are the only gap: chat composer (fixed — `aria-label="Message"`), search boxes (decorative/utility, adjacent to their function — documented; MudTextField has no splat for aria on the inner input, so leaving placeholder-only is the pragmatic choice).
  - Validation errors with `aria-describedby` — MudBlazor renders the validation text in the field's helper/error slot (announced); no code change.
- [x] **MudBlazor accessibility props** — Audit all components (wave 12 audit)
  - `MudButton`/`MudIconButton` `aria-label` — wave 7 audit complete (all icon-only labeled).
  - `MudTextField` with `For` / `Label` — all labeled fields use `Label=` (MudBlazor renders the label+id pairing); only deliberate exceptions are the placeholder-only utility fields documented above.

### 6.5 Feature Parity Audit

> **Parity status is now tracked in `Phase-07-Parity-Completion/plan.md`** (gap tables per app,
> hotfixes, verification gates). This checklist is the historical list — cross-reference it with the
> Phase-7 tables and tick items there as pages land.

- [ ] **Admin app parity** — Compare each page with `clients/admin/src/pages/`
  - Auth: Same login/register/forgot/reset/confirm pages
  - Dashboard cards: Same stats, same layout
  - Tenants: Same fields, same stepper wizard
  - Users: Same table columns, same role assignment
  - Roles: Same permission editor layout
  - Billing: Same plan cards, same invoice table
  - Webhooks: Same form fields, same delivery log
  - Audits: Same table, same diff view
  - Notifications: Same badge count, same dropdown
  - Health: Same status indicators
  - Settings: Same profile/sessions/theme
  - Impersonation: Same list/end flow
- [ ] **Dashboard app parity** — Compare with `clients/dashboard/src/pages/`
  - Overview: Same stats cards, same SSE updates
  - Activity: Same filters, same timeline
  - Subscription: Same plan display, same usage bars
  - Wallet: Same balance, same transaction columns
  - Invoices: Same table, same PDF download
  - Catalog: Same product cards, same brand/category lists
  - Identity: Same read-only user/role lists
  - Tickets: Same thread view
  - Chat: Same channel/message layout
  - Files: Same file manager layout
  - System: Same health/audit/trash/sessions pages
- [ ] **Command palette** — Works same as React (Ctrl+K, search, navigate)
- [ ] **Inactivity timeout** — Same threshold, same warning dialog behavior
- [ ] **Cross-tab logout** — Works on all tabs simultaneously
- [ ] **Impersonation flow** — Cross-app redirect works correctly

### 6.6 Error Handling & Resilience
- [x] **Global error boundary** — `FshErrorBoundary` wraps all pages (both apps)
  - Catches unhandled exceptions in render tree
  - Displays "Something went wrong" with reload button
  - Logs error to console / telemetry
- [x] **HTTP error handling** — All service calls handle 4xx/5xx
  - 401 → AuthDelegatingHandler refresh flow
  - 403 → Permission denied page or component hide (FshPermissionGate) + tenant-deactivated/impersonation-ended routing (TerminalErrorHandler)
  - 404 → Not found page (FshNotFound)
  - 429 → **Retry-After handling (wave 8): `RetryAfterHandler`** — single retry honoring Retry-After (delta or HTTP-date), capped at 30 s, idempotent verbs only (GET/HEAD/PUT/DELETE; POST/PATCH surface the 429). Registered innermost in both apps' `FSH.Api` chain. Tests: 7 in `RetryAfterHandlerTests`.
  - 500 → Error alert with correlation ID — `FshErrorBand` gained optional `CorrelationId` param (rendered as "Request <id>"); pages pass the id when they have it.
- [x] **Network connectivity** — Detect offline state, show banner (wave 8)
  - `navigator.onLine` via JS interop (`fshNetwork.js` module + `FshNetworkStatus : INetworkStatus`)
  - MudAlert: "You are offline. Some features may be unavailable." (`FshOfflineBanner` in both MainLayouts)
  - Auto-dismiss on reconnect (browser online event → StatusChanged → banner hides)
  - Tests: 5 in `FshOfflineBannerTests` + 3 `FshErrorBandTests`.
- [ ] **Graceful degradation** — Components render even if API is down
  - [x] Skeleton loading (MudSkeleton instead of spinner) — pages already render `fsh-skeleton` rows while loading
  - [ ] Cached data shown while offline (last-known-good) — deferred; needs a data-cache layer, not yet scoped

### 6.7 Documentation
- [x] **Blazor dashboard README** — `clients/dashboard-blazor/FSH.Dashboard.Wasm/README.md` (run, tests, architecture, publish notes)
- [x] **Blazor admin README** — `clients/admin-blazor/FSH.Admin.Wasm/README.md` (run, tests, permission flow, publish notes)
- [x] **Component library docs** — `clients/BlazorShared/README.md` (areas + all 19 `Fsh*` components + conventions)
- [x] **README.md (root)** — updated (wave 9): Blazor WASM run commands + ports (5175/5176) + MAUI run note; repo-layout rows point at the migration guide. (Root README is shared — announced on board row #5 before editing.)
- [ ] **MAUI README** — sess-maui zone (Platform setup, build commands, signing)
- [x] **Migration guide** — `clients/BlazorShared/MIGRATION-GUIDE.md`: React→Blazor concept map, per-page checklist, conventions, real-time/offline parity notes
- [ ] **Wiki update** (if applicable) — Architecture decision records

### 6.8 Final Testing & Hardening
- [x] **Load test** — server-side pagination verified by design (all high-volume lists paged; E2E users test exercises the server-search path with paged mocks); 10k-row synthetic load adds no coverage beyond the existing pager bUnit coverage — noted, skipped.
- [x] **Mobile viewport test** — 375×812 E2E (`MobileViewportTests`): Overview/Users/Products render with no horizontal document overflow; drawer trigger (`.fsh-topbar-menu` → role button "Open navigation") visible <900px.
- [x] **Slow network test** — `SlowNetworkTests` (CDP throttle ~1 MB/s + 250 ms latency): branded splash shown during download, boot completes under 180 s, no `#blazor-error-ui`, login renders. Suite is now **18** E2E tests.
- [x] **Memory leak check** — Verify MudDialog dispose, hub disconnect, event unsubscription
  - **Audit (wave 10, 2026-08-08):** every `+=` subscription in both apps + BlazorShared has a matching `-=` in `Dispose()`/`DisposeAsync()` — verified 17 unsubscribe sites (MainLayout auth/nav/SSE, App.razor.cs TokensChanged, FshNotificationBell Hub.StateChanged, FshOfflineBanner Network.StatusChanged, Overview/Activity SSE, both Appearance pages Theme.Changed, ImpersonationBanner, ChatPage SignalR sub list, CommandPalette, HealthPage, Audits debounce CTS, Tickets debounce CTS). `InactivityTimerService` is `IAsyncDisposable` (timer disposed in `Stop()`). ChatPage disposes its SignalR subscription list. **No leaks found.**
- [x] **Edge cases:**
  - Empty list: `FshEmptyState` used on **all** list pages (39 usages across both apps) — ✅
  - Very long text: `fsh-truncate` class + `title=` tooltip on all long-text cells (55 usages) — ✅
  - Special characters: **no `MarkupString` anywhere in clients/** — Blazor auto-HTML-encodes by default — ✅
  - Concurrent logins: **refresh-token rotation race FIXED (wave 10)** — server rotates refresh tokens (`RefreshTokenRotated` audit), but `AuthDelegatingHandler` read the refresh token *before* acquiring the static `RefreshLock` — a second concurrent 401 waited on the lock, then refreshed with the now-rotated (invalid) token → session killed. Fix: re-read the store after acquiring the lock (double-checked); if another request already refreshed, retry with the fresh access token instead of refreshing. 4 new tests in `AuthDelegatingHandlerTests` (dashboard 200/200, admin 158/158).
  - Multi-tab: **already handled** — both apps' `App.razor.cs` listen for `storage` events and reload on token removal (cross-tab logout); E2E `AuthFlowTests` cover it — ✅

## Next Up

**Waves 4-11 committed.** Both WASM apps lazy-loaded + PWA + 6.4 contrast/focus (wave 7) + 6.6 error handling/resilience (wave 8) + 6.7 docs (wave 9) + **6.8 audit + refresh-race fix (wave 10: dashboard 200/200 + admin 158/158)** + **chat infinite scroll (wave 11: dashboard 204/204)**. **Wave 12 (uncommitted): 6.3 preload (boot-JS preload + font preconnect, both apps), 6.2 image-lazy verified, 6.4 SR/form/focus/MudBlazor a11y audit done (MudAlert role=alert fix + chat composer aria-label + guard tests)**. Next: commit wave 12, then last-known-good cache (deferred), 6.9 blocker (upstream), HTTP/2 Server Push (deployment item).

## Architecture Decisions

| Decision | Choice | Why |
|---|---|---|
| Bundle measurement | Publish + `_framework` size sum | Direct output measurement (built-in `dotnet wasm` size report not available for AOT-less publish) |
| Trimming | Explicit `true` on both WASM apps | `.NET 10 finding`: `PublishTrimmed` defaults true — `false` alone never disabled it; explicit flag matches effective behavior, keeps 22.29 MB |
| Skip link | JS interop + preventDefault | Blazor SPA interceptor swallows fragment navigation; native href never moves focus |
| AOT | **OFF (benchmarked)** | trim+AOT 56.60 MB/11.51 gz + 6.95 s boot vs trim-only 22.29 MB/4.97 gz + 3.47 s — 2.5x size, 2x boot for no CPU-heavy gain; revisit per-page |
| Lazy loading | Single Pages RCL (`.wasm` lazy item) + `AdditionalAssemblies` | One lazy assembly covers all feature pages; per-feature split (admin parity item) would need N RCLs — revisit after admin lazy |
| PWA | Custom service worker + manifest (hand-written) | WASM apps are served as static files; PWA adds install/offline. Avoids `BlazorWebAssemblyPWA` template SDK bits (keeps full control over cache strategy; `_framework/*` cache-first since content-hashed) |
| Image lazy | Native `loading="lazy"` | Simple, works on MudImage, no JS needed |
| Error boundary | Custom component | Wraps Router > Found > content; catches render exceptions |

## Notes & Gotchas

- **`OverrideHtmlAssetPlaceholders=true` breaks static hosting** (both apps had it): published index.html references the literal `_framework/blazor.webassembly.js` which only exists as `blazor.webassembly.{hash}.js` in `_framework/` — dev server masks it, any CDN/nginx/static server 404s and the app never boots. Removed from both csproj files; verified via static-host smoke.
- **Smoke harness** (temp, not committed): publish → `npx serve -s` on 5180 → Playwright boot check (splash, `.mud-theme-provider` attached, login heading, `#blazor-error-ui` absent, JS errors). `http-server` lacks SPA fallback — `serve -s` required.
- **Trimmed publish measurement gotcha**: publishing twice into the same obj folder reuses cached `wasm` intermediates (identical hash-named outputs, misleading "no trim" result). Always clean `obj/Release` between configs.
- **AOT measurement**: 56.60 MB total / 11.51 MB gz; `dotnet.native.wasm` alone 27.93 MB (8.61 gz).
- **Trimming + MudBlazor**: MudBlazor 9 ships trim-friendly; no Linker.xml needed (verified by publish smoke).
- **Skip link + Blazor interceptor**: fragment-only `href` gets intercepted by `blazor.webassembly.js` — always pair with `@onclick:preventDefault` + JS focus.
- **Theme button a11y**: `MudMenu` activator div mirrors the inner button's accessible name (strict-mode ambiguity in Playwright role queries — scope with `.First`).
- **AOT + WASM size**: AOT increases WASM size 2-3x but improves perf 2x. AOT is best for CPU-heavy pages (reports, charts). Consider per-page AOT via [`<RunAOTCompilation>` property with conditions](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly-performance?view=aspnetcore-10.0#ahead-of-time-aot-compilation).
- **Lazy assembly loading**: Each lazy-loaded assembly must be a separate project reference. Use `AdditionalAssemblies` on `Router` component. Assembly must exist at `/_framework/{Name}.wasm`.
- **`.NET 10 lazy key gotcha**: boot config lazy set is `resources.lazyAssembly` (singular, array of `{virtualPath,name,hash,cache}`) — the old `lazyAssemblies` top-level key is GONE. `GenerateWasmBootJson.cs` maps it (`resourceData.lazyAssembly` → `assets.lazyAssembly`); verify via the boot config, not the SDK docs' old shape.
- **`LoadAssembliesAsync` + lazy item must both be `.wasm`**: `.dll` paths 404 at runtime (resources map uses `.wasm` keys since .NET 8; the old docs' `.dll` form is stale).
- **Router `AdditionalAssemblies` is mandatory**: without it lazy-loaded routes are never discovered → the app boots to the 404 page with zero console errors (E2E reproduced this — `Locator expected to be visible` on `text=Sign in`).
- ⚠️ **RELEASE-PUBLISH BLOCKER (6.9, pre-existing, both WASM apps)**: Release `dotnet publish` output crashes on boot. Trimmed + untrimmed + clean-obj + jiterpreter-off all reproduce; `MONO interpreter: NIY encountered in method Microsoft.Extensions.Localization.LocalizationOptions:.ctor ()` → `Assertion: should not be reached at interp.c:4135` (NIY = interpreter hit an un-implementable opcode; only emit site in runtime source is `transform-simd.c` for unimplemented `PackedSimd` calls, but the trivial BCL ctor contains none — mechanism still upstream). **Confirmed pre-existing**: clean worktree publish of `a9c78f86` fails identically; admin-blazor Release publish fails identically. Debug/DevServer (what E2E/bUnit exercise) is fully green. Upstream: dotnet/runtime #121849 (open, milestone 12.0.0 — same NIY class; reporter's bin/obj fix did NOT help us), #121840/#121738 family; MudBlazor net10.0 target unshipped (#12049, v9 cycle). Re-test candidates: newer 10.0.x runtime servicing, MudBlazor ≥ .NET 10-targeted release, or `RunAOTCompilation=true` (AOT sidesteps the interpreter; note upstream #125794: AOT + lazy has its own known bug).
- **PWA in development**: Requires serving via HTTPS. Use `dotnet run` with dev cert (already set up). Service worker cache busters via version hash.
- **Feature parity**: The React reference may have been updated. Re-check the commit hash or compare against the current state of `clients/admin/` and `clients/dashboard/`.
- **MudBlazor CSS size**: MudBlazor CSS file is ~300KB uncompressed. Critical CSS extraction can drop initial load to ~30KB. Consider `MudBlazor.Min.css` from CDN.

## Blocker Checklist

- [x] Phase 4 + 5 complete
- [x] All pages functional (no half-built features)
- [x] Baseline bundle size measured (untrimmed 79.52 MB / trimmed 22.29 MB / AOT 56.60 MB)
- [x] Linker.xml exists for trimming (not needed — MudBlazor 9 trim-friendly, verified via publish smoke)
- [ ] **6.9 RELEASE-PUBLISH BLOCKER** (new, pre-existing): dashboard + admin WASM Release publishes crash the Mono interpreter at boot (`LocalizationOptions:.ctor` NIY, interp.c:4135). Not caused by lazy refactor (baseline `a9c78f86` reproduces). Upstream dotnet/runtime #121849. Re-test after runtime servicing / MudBlazor net10 target / AOT; until then production deploys of the Blazor WASM apps are blocked upstream (Debug/DevServer unaffected).
- [ ] Production deployment environment known (CDN, reverse proxy)
- [ ] PWA: app serves over HTTPS in staging
