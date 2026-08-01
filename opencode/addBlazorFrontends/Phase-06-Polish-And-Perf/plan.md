# Phase 6 — Polish & Performance

> **Target:** Production-ready quality. Bundle optimization, lazy loading, WASM AOT/tree-shaking, accessiblity audit, full feature parity with React apps, documentation update.

## Status

- Phase 6: **🔲 Not started**
- Prerequisites: Phase 4 ✅ (testing done), Phase 5 ✅ (MAUI built)

## Task Checklist

### 6.1 WASM Bundle Size Optimization
- [ ] **Assembly trimming** — Re-enable `BlazorWebAssemblyEnableTrimming=true` (currently false from Phase 0 debugging)
  - Configure trimmer XML: `Linker.xml` to preserve dynamically-accessed types
  - Test all pages work with trimming enabled
- [ ] **MudBlazor tree-shaking** — Verify only used MudBlazor components are included
  - Configure MudBlazor's trimmer-friendly import path
  - Consider `<MudTrimmingConfiguration>` in csproj
- [ ] **Lazy loading** — Split admin app assemblies by feature area
  - `FSH.Admin.Identity.wasm`, `FSH.Admin.Tenants.wasm`, etc.
  - Lazy-load on route match: `LazyAssemblyLoader.LoadAsync(uri)`
  - Update Router to support lazy assemblies
- [ ] **AOT compilation** — Enable for release build
  - `RunAOTCompilation=true` in csproj
  - Benchmark: page load time, interaction response, bundle size
  - Measure: with/without AOT, with/without trimming
- [ ] **Pre-compression** — Add Brotli/Gzip compressed assets for deployment
  - `BlazorWebAssemblyJSHostCompression` configuration
  - Verify CDN serves compressed WASM files

### 6.2 Runtime Performance
- [ ] **Virtualized lists** — Audit all MudTable with large datasets
  - Ensure `ServerData` pattern everywhere (no client-side paging)
  - Use MudTable with `RowsPerPage="20"` and server-side total count
- [ ] **Infinite scroll** — Activity log, chat messages
  - Implement `OnScroll` JS interop for detect-bottom
  - Load more items via service call, append to list
- [ ] **Debounced search** — All search MudTextField
  - Use `Immediate="true"` with `DebounceInterval="300"`
  - Cancel previous request on new keystroke (CancellationToken)
- [ ] **MudBlazor render optimization** — Audit unnecessary re-renders
  - `@key` on MudTable rows
  - `StateHasChanged()` only when needed
  - Use `MudComponentBase`'s `ShouldRender()` override in heavy components
- [ ] **Image lazy loading** — MudImage/thumbnails with `loading="lazy"`

### 6.3 First-Load Performance
- [ ] **Splash screen** — Enhance current CSS spinner to branded splash
  - FSH logo, branded colors, progress bar
  - Fade out on app ready
- [ ] **Critical CSS** — Inline MudBlazor critical styles in index.html
  - Extract critical CSS for above-the-fold content
  - Defer non-critical MudBlazor CSS
- [ ] **Preload** — `link rel="preload"` for key assemblies
  - MudBlazor, Microsoft.AspNetCore.Components.WebAssembly
  - Preconnect to API endpoint
- [ ] **HTTP/2 Server Push** — For deployment behind reverse proxy
  - Push key assemblies on first request
- [ ] **PWA** — Progressive Web App support
  - Service worker for offline/caching
  - `manifest.json` for installable WASM app
  - Offline fallback page

### 6.4 Accessibility Audit
- [ ] **Keyboard navigation** — All pages navigable without mouse
  - MudTable row actions via keyboard
  - MudDialog close via Escape
  - MudMenu via Space/Enter
  - MudSelect via arrow keys
- [ ] **Screen reader support** — ARIA labels and roles
  - MudTable: `aria-label`, `aria-sort`
  - MudButton: `aria-label` for icon-only buttons
  - MudIcon: `aria-hidden="true"` with accessible text nearby
  - MudAlert: `role="alert"`
  - MudDialog: `role="dialog"`, `aria-labelledby`
- [ ] **Color contrast** — Verify WCAG 2.1 AA compliance
  - MudBlazor palette contrast ratios
  - Focus indicators visible
- [ ] **Focus management** — Focus moves correctly on navigation
  - MudToolbar focus trap in MudDialog
  - Skip-to-content link
- [ ] **Form accessibility** — MudForm with proper labels, error announcements
  - `<label for="...">` or MudInput with `aria-label`
  - Validation errors with `aria-describedby`
- [ ] **MudBlazor accessibility props** — Audit all components
  - `MudButton` `aria-label`
  - `MudIconButton` `aria-label` required
  - `MudTextField` with `For` / `Label`

### 6.5 Feature Parity Audit
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
- [ ] **Global error boundary** — `FshErrorBoundary` wraps all pages
  - Catches unhandled exceptions in render tree
  - Displays "Something went wrong" with reload button
  - Logs error to console / telemetry
- [ ] **HTTP error handling** — All service calls handle 4xx/5xx
  - 401 → AuthDelegatingHandler refresh flow
  - 403 → Permission denied page or component hide
  - 404 → Not found page
  - 429 → Retry-After handling
  - 500 → Error alert with correlation ID
- [ ] **Network connectivity** — Detect offline state, show banner
  - `Navigator.onLine` via JS interop
  - MudAlert: "You are offline. Some features may be unavailable."
  - Auto-dismiss on reconnect
- [ ] **Graceful degradation** — Components render even if API is down
  - Skeleton loading (MudSkeleton instead of spinner)
  - Cached data shown while offline (last-known-good)

### 6.7 Documentation
- [ ] **README.md** — Update with MAUI build instructions, Blazor ports
- [ ] **Blazor admin README** — Setup, architecture, conventions
- [ ] **Blazor dashboard README** — Setup, SSE notes, impersonation notes
- [ ] **MAUI README** — Platform setup, build commands, signing
- [ ] **Component library docs** — Document all shared components (FshTable, FshPageHeader, etc.)
- [ ] **Migration guide** — React → Blazor conversion guide for future pages
- [ ] **Wiki update** (if applicable) — Architecture decision records

### 6.8 Final Testing & Hardening
- [ ] **Load test** — MudTable with 10k+ items (server-side pagination verified)
- [ ] **Mobile viewport test** — All pages on 375px width
- [ ] **Slow network test** — 3G throttling, verify loading states
- [ ] **Memory leak check** — Verify MudDialog dispose, hub disconnect, event unsubscription
- [ ] **Edge cases:**
  - Empty list: MudAlert "No {items} found"
  - Very long text: MudTable text truncation with tooltip
  - Special characters: XSS prevention, HTML encoding
  - Concurrent logins: Token refresh race condition
  - Multi-tab: Storage events, auth state sync

## Next Up

**Task 6.1**: Re-enable assembly trimming.

1. Set `BlazorWebAssemblyEnableTrimming=true` in both WASM csproj files
2. Create `Linker.xml` to preserve types accessed via reflection (MudBlazor, JSON serialization)
3. Build and test all pages
4. Fix any trimmer-related runtime errors
5. Benchmark bundle size before/after

## Architecture Decisions

| Decision | Choice | Why |
|---|---|---|
| Bundle measurement | `dotnet wasm` size report | Built-in; measures each assembly compressed/uncompressed |
| Lazy loading | Per-feature assemblies | Every feature area (Identity, Tenants, Billing) is a separate lazy-loaded assembly |
| PWA | Service worker + manifest | WASM apps are served as static files; PWA adds install/offline |
| AOT | Enable for Release only | Dev build speed > AOT speed during development |
| Image lazy | Native `loading="lazy"` | Simple, works on MudImage, no JS needed |
| Error boundary | Custom component | Wraps Router > Found > content; catches render exceptions |

## Notes & Gotchas

- **Trimming + MudBlazor**: MudBlazor uses reflection for some component features. Ensure `Linker.xml` includes `MudBlazor.dll` preserve-all or test thoroughly.
- **AOT + WASM size**: AOT increases WASM size 2-3x but improves perf 2x. AOT is best for CPU-heavy pages (reports, charts). Consider per-page AOT via [`<RunAOTCompilation>` property with conditions](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly-performance?view=aspnetcore-10.0#ahead-of-time-aot-compilation).
- **Lazy assembly loading**: Each lazy-loaded assembly must be a separate project reference. Use `AdditionalAssemblies` on `Router` component. Assembly must exist at `/_framework/{Name}.wasm`.
- **PWA in development**: Requires serving via HTTPS. Use `dotnet run` with dev cert (already set up). Service worker cache busters via version hash.
- **Feature parity**: The React reference may have been updated. Re-check the commit hash or compare against the current state of `clients/admin/` and `clients/dashboard/`.
- **MudBlazor CSS size**: MudBlazor CSS file is ~300KB uncompressed. Critical CSS extraction can drop initial load to ~30KB. Consider `MudBlazor.Min.css` from CDN.

## Blocker Checklist

- [ ] Phase 4 + 5 complete
- [ ] All pages functional (no half-built features)
- [ ] Baseline bundle size measured (current state)
- [ ] Linker.xml exists for trimming
- [ ] Production deployment environment known (CDN, reverse proxy)
- [ ] PWA: app serves over HTTPS in staging
