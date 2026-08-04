# Blazor WASM + MAUI — Implementation Roadmap
Last Update: 2026-Aug-04 19:45:00, by: opencode (auto/coding, model: opencode/mimo-v2-pro-max).

> Status: **Phase 0 ✅ | Phase 1 ✅ | Phase 2 ✅ (2.1–2.10 done — Tenants, Users, Roles, Billing, Webhooks, Audits, Health, Notifications inbox, Settings, Impersonation — admin blazor 147/147) | Phase 3 🟨 (3.1 Overview ✅ + 3.2 Activity ✅ + 3.3 Subscription ✅ + 3.4 Wallet ✅ + 3.5 Invoices ✅ + 3.6 Catalog ✅ + 3.7 Identity ✅ + 3.8 Tickets ✅ + 3.9 Chat ✅ + 3.10 Files ✅ — dashboard suite 119/119 — next: 3.11 System) | Phase 4 🔲 | Phase 5 🔲 | Phase 6 🔲 | Phase 7 🟨 (parity completion — admin pages complete + runtime DI hardened, styled 404 done)**
>
> **Blazor WASM apps: admin 147/147 tests passing, dashboard 119/119 tests passing.**
>
> **Parity sprint 1 (React ↔ Blazor, done):** both sidebars rebuilt 1:1 from `nav-items.ts` / `nav-data.ts`
> (accordion sections, permission-gated, collapse `"true"/"false"`) · dashboard theme = `fsh.theme` +
> Light/Dark/System (admin keeps binary `fsh.admin.theme`) · shared components (`FshNavSection`,
> `FshPager`, `FshFilterBar`, `FshLoadingRow`, `FshKpiTile`, `FshSectionRule`) · `FshNotificationBell`
> (SignalR `NotificationCreated` + `INotificationService`) · SSE status dot (`SseService.ConnectionChanged`)
> · dashboard bUnit suite 18/18 + admin 114/114.
>
> **Phase 7 hotfixes (done):** user/role detail crash (dropped `{Id:guid}` constraints — `RouteBindingRegressionTests`
> navigate through the real Router) · `<base href>` ordering in both `index.html` (deep-link CSS 404s —
> `IndexHtmlGuardTests` guards it).
>
> **Phase 2 feature pages (done in this phase-7 pass):** 2.4 Billing (plans/subscriptions/invoices/detail) ·
> 2.5 Webhooks (list/create/detail/test, delivery log) · 2.6 Audits (list + stat strip, detail side-sheet with
> identity/correlation/context/payload sections) · 2.7 Health (live + ready probes, 10s auto-refresh, expandable
> check rows) · 2.8 Notifications inbox (unread/all filter, mark-read, mark-all-read, live SignalR append) ·
> 2.9 Settings (profile/avatar, security + password/2FA, sessions, appearance theme cards) · 2.10 Impersonation
> (grant list, impersonate dialog with user search + duration + reason, revoke dialog, cross-app handoff) —
> each with bUnit coverage (**admin suite 147/147, 0 build warnings** — MudBlazor 9.7 API drift fixed).
>
> **Phase 3 — dashboard pages (in progress):** 3.1 Overview ✅ · 3.2 Activity ✅ · 3.3 Subscription ✅ · 3.4 Wallet ✅ · 3.5 Invoices ✅ · 3.6 Catalog ✅ · 3.7 Identity ✅ — brands/categories/products pages with full CRUD dialogs (editor, price, stock, delete confirm), detail page with hero/pricing/inventory/images/audit panels; identity users/roles/groups lists + details + register/role-editor/group-editor/add-members dialogs, grouped permission editor, sessions + impersonate. Next: 3.8 Tickets. (**dashboard 92/92**).
>
> **Fixed in this session:**
> - **Admin Blazor (root cause of the post-login "An unhandled error has occurred"):** `Program.cs` was
>   missing **9 API service registrations** — `IAuditService`, `IBillingService`, `IImpersonationService`,
>   `IRoleService`, `ISessionService`, `ITenantService`, `ITwoFactorService`, `IUserService`,
>   `IWebhookService` (all `AddScoped<IX, XService>()` using the scoped `"FSH.Api"` HttpClient), plus
>   `IHealthService` was registered as the concrete `HealthService` type only (HealthPage injects the
>   interface). `OverviewPage` (`/`) is the first render after login and crashed on `ITenantService`; the
>   NREs + "No element associated" errors in the console were cascading fallout. Cross-checked **every**
>   `[Inject]`/`@inject` across the admin app + BlazorShared against `Program.cs` — no gaps remain.
> - **Admin Blazor JS bug:** `js/fshWindow.js` used `export function openUrl()` but is loaded as a plain
>   `<script>` (not `type="module"`) → `Uncaught SyntaxError: Unexpected token 'export'` in the browser
>   console (broke the impersonation open-in-new-tab handoff). Fixed in both
>   `clients/BlazorShared/wwwroot/js/fshWindow.js` and the admin copy → `window.openUrl = function () {...}`.
> - **Dashboard Blazor**: 
>   * Added `[JsonConverter(typeof(FlexibleEnumJsonConverter<AuditTag>))]` to `AuditTag` enum in `AuditDtos.cs`
>   * Fixed `SseService` to stop retrying when session token is gone (no infinite 401 loops)
>   * Made `OverviewPage` observe `SseService.ConnectionChanged` only (app root owns SSE lifecycle)
>   * Removed unused `_loadingSse`/`_sseError` fields and loading spinner from `OverviewPage.razor`
> - **Verification:** full solution build **0 warnings / 0 errors** · admin bUnit **147/147** ·
>   dashboard bUnit **24/24** · dev server restarted with the fresh DLL (12:25:58) — the post-login crash is gone.
>   `fshWindow.js` export fix verified served on 5175. Health probes confirmed at `/health/live` + `/health/ready`
>   (not `/api/v1/health/...` — Blazor `HealthService` already hits the correct path).
>
> **Deferred (full-parity policy — menu entries render 404 until pages land):** command palette,
> accent/font/density settings, and the pending dashboard pages tracked in the gap tables of
> `Phase-07-Parity-Completion/plan.md`.

## Phase Overview

| # | Phase | Focus | Est. Effort | Depends On |
|---|-------|-------|-------------|------------|
| **0** | Foundation & Tooling | Project scaffold, MudBlazor 9.x fixes, WASM boot, agent rules/skills | Done | — |
| **1** | Identity & Auth | Login/register/logout, token store, AuthStateProvider, DelegatingHandler, permission gating | 2-3 weeks | Phase 0 |
| **2** | Admin Feature Pages | Tenants, Users, Roles, Billing, Webhooks, Audits, Health, Notifications, Settings, Impersonation | 4 weeks | Phase 1 |
| **3** | Dashboard Feature Pages | Overview, SSE, Activity, Subscription, Wallet, Catalog, Tickets, Chat, Files, System | 4 weeks | Phase 1 |
| **4** | Testing | bUnit component tests, Playwright E2E, auth flow tests, perf benchmarks | 2 weeks | Phase 2 + 3 |
| **5** | MAUI Hybrid | MAUI Blazor Hybrid + native features (push, biometric, camera, offline, deep links) | 3 weeks | Phase 2 + 3 |
| **6** | Polish & Perf | Bundle size, AOT, lazy loading, WASM trimming, accessiblity, feature parity audit with React | 2 weeks | Phase 4 + 5 |
| **7** | Parity Completion | React ↔ Blazor ↔ MAUI identical or better; gap tables to zero; docs always in sync | ongoing | 2 + 3 |

## Quick Start (from any phase)

```powershell
# API
dotnet run --project src/Host/FSH.Starter.Api

# Blazor WASM (run against API)
dotnet run --project clients/admin-blazor/FSH.Admin.Wasm  # :5175
dotnet run --project clients/dashboard-blazor/FSH.Dashboard.Wasm  # :5176

# Tests
dotnet test clients/admin-blazor/FSH.Admin.Wasm.Tests
dotnet test clients/dashboard-blazor/FSH.Dashboard.Wasm.Tests
```

## Key Architecture Decisions

| Decision | Choice |
|----------|--------|
| Component library | MudBlazor 9.x |
| Auth | AuthStateProvider + JSInterop (localStorage) |
| Rendering | Standalone WASM (not Server/Auto) |
| Shared code | `clients/BlazorShared` RCL |
| API calls | Typed services via `HttpClient` + DelegatingHandler |
| Testing | bUnit (unit) + Playwright (e2e) |
| Theming | MudTheme with FSH brand tokens |

## Phase Plan Files

- `Phase-01-Identity-And-Auth/plan.md` — Auth infrastructure + login/register flows
- `Phase-02-Admin-Feature-Pages/plan.md` — Admin CRUD pages
- `Phase-03-Dashboard-Feature-Pages/plan.md` — Dashboard pages + SSE
- `Phase-04-Testing/plan.md` — bUnit + Playwright test suites
- `Phase-05-MAUI-Hybrid/plan.md` — MAUI Blazor Hybrid + native features
- `Phase-06-Polish-And-Perf/plan.md` — Performance, bundle, parity audit
- `Phase-07-Parity-Completion/plan.md` — Parity gap tables, hotfixes, build order, verification gates

## Companion Files

| File | Purpose |
|------|---------|
| `00-Setup.md` | Environment verification checklist — paste and go |
| `99-Glossary.md` | Every acronym explained in plain English |
| `Phase 0/hands-on-phase-0.md` | Chapter 0: Foundation & Tooling — full educational deep-dive |
| (future) `Phase-0N/hands-on-phase-N.md` | Each phase has a companion hands-on chapter |

## Reference

- `Phase 0/` — Session notes from foundation work (MudBlazor upgrade, WASM boot fixes)
- `.agents/rules/frontend/blazor-shared.md` — Shared Blazor conventions
- `.agents/rules/frontend/blazor-admin.md` — Admin app conventions
- `.agents/rules/frontend/blazor-dashboard.md` — Dashboard app conventions
- `.agents/rules/frontend/maui-hybrid.md` — MAUI Hybrid conventions
- `.agents/skills/add-blazor-page/SKILL.md` — Skill: adding Blazor pages
- `clients/admin/src/` — React admin reference implementation
- `clients/dashboard/src/` — React dashboard reference implementation