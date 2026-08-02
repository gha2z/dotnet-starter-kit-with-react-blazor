# Blazor WASM + MAUI — Implementation Roadmap

> Status: **Phase 0 ✅ | Phase 1 ✅ | Phase 2 🟨 (2.1 Users · 2.2 Roles · 2.3 Tenants · 2.4 Billing done — next: 2.5 Webhooks) | Phase 3 🔲 | Phase 4 🔲 | Phase 5 🔲 | Phase 6 🔲 | Phase 7 🟨 (parity completion — in progress)**
>
> **Parity sprint 1 (React ↔ Blazor, done):** both sidebars rebuilt 1:1 from `nav-items.ts` / `nav-data.ts`
> (accordion sections, permission-gated, collapse `"true"/"false"`) · dashboard theme = `fsh.theme` +
> Light/Dark/System (admin keeps binary `fsh.admin.theme`) · shared components (`FshNavSection`,
> `FshPager`, `FshFilterBar`, `FshLoadingRow`, `FshKpiTile`, `FshSectionRule`) · `FshNotificationBell`
> (SignalR `NotificationCreated` + `INotificationService`) · SSE status dot (`SseService.ConnectionChanged`)
> · dashboard bUnit suite 18/18 + admin 53/53.
>
> **Phase 7 hotfixes (done):** user/role detail crash (dropped `{Id:guid}` constraints — `RouteBindingRegressionTests`
> navigate through the real Router) · `<base href>` ordering in both `index.html` (deep-link CSS 404s —
> `IndexHtmlGuardTests` guards it).
>
> **Deferred (full-parity policy — menu entries render 404 until pages land):** command palette,
> accent/font/density settings, and the pending pages tracked in the gap tables of
> `Phase-07-Parity-Completion/plan.md`.

## Phase Overview

| # | Phase | Focus | Est. Effort | Depends On |
|---|---|---|---|---|
| **0** | Foundation & Tooling | Project scaffold, MudBlazor 9.x fixes, WASM boot, agent rules/skills | Done | — |
| **1** | Identity & Auth | Login/register/logout, token store, AuthStateProvider, DelegatingHandler, permission gating | 2-3 weeks | Phase 0 |
| **2** | Admin Feature Pages | Tenants, Users, Roles, Billing, Webhooks, Audits, Notifications, Health, Settings, Impersonation | 4 weeks | Phase 1 |
| **3** | Dashboard Feature Pages | Overview, SSE, Activity, Subscription, Wallet, Catalog, Tickets, Chat, Files, System | 4 weeks | Phase 1 |
| **4** | Testing | bUnit component tests, Playwright E2E, auth flow tests, perf benchmarks | 2 weeks | Phase 2 + 3 |
| **5** | MAUI Hybrid | MAUI project, SecureStorage auth, native features (push, biometric, camera, offline, deep links) | 3 weeks | Phase 2 + 3 |
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
|---|---|
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
