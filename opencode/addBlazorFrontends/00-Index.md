# Blazor WASM + MAUI — Implementation Roadmap

> Status: **Phase 0 ✅ | Phase 1 🔲 | Phase 2 🔲 | Phase 3 🔲 | Phase 4 🔲 | Phase 5 🔲 | Phase 6 🔲**

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

- `Phase-01-Identity-And-Auth/01-plan.md` — Auth infrastructure + login/register flows
- `Phase-02-Admin-Feature-Pages/01-plan.md` — Admin CRUD pages
- `Phase-03-Dashboard-Feature-Pages/01-plan.md` — Dashboard pages + SSE
- `Phase-04-Testing/01-plan.md` — bUnit + Playwright test suites
- `Phase-05-MAUI-Hybrid/01-plan.md` — MAUI Blazor Hybrid + native features
- `Phase-06-Polish-And-Perf/01-plan.md` — Performance, bundle, parity audit

## Reference

- `Phase 0/` — Session notes from foundation work (MudBlazor upgrade, WASM boot fixes)
- `.agents/rules/frontend/blazor-shared.md` — Shared Blazor conventions
- `.agents/rules/frontend/blazor-admin.md` — Admin app conventions
- `.agents/rules/frontend/blazor-dashboard.md` — Dashboard app conventions
- `.agents/rules/frontend/maui-hybrid.md` — MAUI Hybrid conventions
- `.agents/skills/add-blazor-page/SKILL.md` — Skill: adding Blazor pages
- `clients/admin/src/` — React admin reference implementation
- `clients/dashboard/src/` — React dashboard reference implementation
