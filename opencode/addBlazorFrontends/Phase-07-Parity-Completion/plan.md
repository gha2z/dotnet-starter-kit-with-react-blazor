# Phase 7 — React Parity Completion & Hardening
Last Update: 2026-Aug-06, by: opencode (auto/coding, model: deepseek-v4-flash-free).

> **Target:** Both Blazor WASM apps (admin + dashboard) are pixel- and behavior-identical to the
> React 19 apps (`clients/admin`, `clients/dashboard`) — or better — and the MAUI Hybrid app matches
> the dashboard. Every parity gap below is tracked to zero; every rule/hands-on/phase md file stays
> in sync with the code.

## Status

- Phase 7: **🟨 In progress** — parity sprint 1 done (sidebar/theme/bell/SSE), hotfixes done **and
  verified (admin 147/147 + dashboard 178/178, both build **0 warnings**; admin PW roles 7/7, dashboard PW roles 5/5;
  17 role-permission integration tests green)**, MAUI Hybrid workload saga resolved (Hybrid builds
  4 TFMs, 0 warnings). Page build-out: **2.4 Billing ✅ · 2.5 Webhooks ✅ · 2.6 Audits ✅ · 2.7 Health ✅ ·
  2.8 Notifications inbox ✅ · 2.9 Settings ✅ · 2.10 Impersonation ✅ · styled 404 ✅ ·
   dashboard 3.1 Overview ✅ · 3.2 Activity ✅ · 3.3 Subscription ✅ · 3.4 Wallet ✅ · 3.5 Invoices ✅ · 3.6 Catalog ✅ · 3.7 Identity ✅ · 3.8 Tickets ✅ · 3.9 Chat ✅ · 3.10 Files ✅ · 3.11 System ✅** (admin suite 147/147, dashboard 178/178) — next up dashboard 3.12 Settings.
- **Runtime hardening (this session):** admin app crashed on every authenticated render at `/` with
  "Cannot provide a value for property 'TenantService'" → the **9 missing `AddScoped` API service
  registrations** in `FSH.Admin.Wasm/Program.cs` were added (Audit/Billing/Impersonation/Role/Session/
  Tenant/TwoFactor/User/Webhook) and `IHealthService` was registered as the interface (was concrete only).
  Also fixed `js/fshWindow.js` `export` syntax error on a plain `<script>` load (now `window.openUrl`).
- Prerequisites: Phases 0–1 ✅ · Phase 2 ✅ (2.1–2.10 + admin overview) · Phase 5 unblocked — MAUI workload
  installed (elevated) and `clients/FSH.Hybrid` migrated to .NET 10 conventions (see §7.6 in
  `hands-on-phase-7.md` for the full saga + project-file recipe).

## What was already delivered (parity sprint 1) ✅

- **Sidebars rebuilt 1:1 from the React nav sources** — admin `nav-items.ts`, dashboard `nav-data.ts`:
  accordion sections (`FshNavSection`), permission-gated (perm AND anyPerm), single-select +
  route re-sync, collapsed 52px icon-stack mode, persisted `fsh.admin.sidebar.collapsed` /
  `fsh.sidebar.collapsed` with `"true"/"false"` (matching React).
- **Theme parity** — `FshThemeService` with `ThemeMode { Light | Dark | System }`; dashboard key
  `fsh.theme` (default System, OS-following), admin key `fsh.admin.theme` (binary, default Dark);
  stored as raw `"light"|"dark"|"system"` via `fshTheme.js`.
- **Notifications bell** — shared `FshNotificationBell` (unread badge cap 99+, list of 20, mark-read
  + navigate, mark-all-read, refresh on open) wired to `INotificationService`
  (`GET /api/v1/notifications/unread-count`, `...?unreadOnly=&page=&pageSize=`,
  `POST .../{id}/read`, `POST .../read-all`) + SignalR `NotificationCreated` (`AppHub`, user group).
- **SSE status indicator** (dashboard) — `SseService.ConnectionChanged` event → `.fsh-sse-dot`
  in the topbar user menu.
- **Shared components** — `FshNavSection`, `FshPager`, `FshFilterBar`, `FshLoadingRow`,
  `FshKpiTile`, `FshSectionRule` + `fsh.css` additions (accordion, KPI, pager, bell, SSE dot).
- **Dashboard test project bootstrapped** — `TestSetup` (bUnit 2.0 `BunitContext`,
  `JSRuntimeMode.Loose`, MudServices, fake `IJSObjectReference` for the theme module) + suites:
  admin 114/114, dashboard 18/18 (incl. `RouteBindingRegressionTests` navigating the real `Router`).
- **Docs synced** — `.agents/rules/frontend/blazor-{shared,admin,dashboard}.md` rewritten for the
  parity state; `00-Index.md` updated.

## Hotfixes delivered in this phase ✅

| Bug | Root cause | Fix |
|---|---|---|
| Clicking a user/role crashed (`Arg_InvalidCastException` on `Id`) | `@page "/users/{Id:guid}"` / `"/roles/{Id:guid}"` produce a `Guid` route value but pages bind `[Parameter] string Id` — Guid cannot be cast to string | Dropped the `:guid` constraint (`{Id}`); 3 regression tests navigate through the real `Router` — green |
| Full-page reload at a sub-route 404s all CSS/JS (`/roles/_content/... ERR_ABORTED 404`) | `<base href="/" />` appeared AFTER the `<link>` tags in `index.html`; the browser resolves relative links against the document URL until it parses `<base>` | Moved `<base href="/" />` to the top of `<head>` in both apps; guard test asserts ordering — green |
| Role detail page: "Failed to load role: …404 Not Found" on `GET /identity/roles/{id}/permissions` | Backend role-permissions endpoints were registered as `/{id:guid}/permissions` on the identity group (missing the `/roles` segment), and React admin + dashboard clients mirrored the asymmetric path; only `BlazorShared/RoleService` used the canonical `/roles/…` path — a Phase-2 fix on the Blazor side had regressed on the server | Backend GET→`/roles/{id:guid}/permissions`, PUT→`/roles/{id}/permissions`; updated `clients/admin/src/api/roles.ts`, `clients/dashboard/src/api/identity.ts`, both Playwright roles specs, 5 integration test files, and `identity-roles.http` (PUT + POST lines). Verified: backend build 0 warnings, admin/dashboard Blazor suites green, admin PW roles 7/7 + dashboard PW roles 5/5, 17 role-permission integration tests green against real Postgres (Testcontainers) |
| `Architecture.Tests` `BuildingBlocks_Core_Domain_Namespaces_Should_Match_Folder` failed | Test split file content on `Environment.NewLine` (CRLF) but `src/BuildingBlocks/Core/Domain/IDomainEvent.cs` is committed with LF-only endings → whole file became one "line" and the `namespace ` scan missed it (pre-existing, surfaced in the full-suite run) | Test now splits on `\n` and trims `\r` (line-ending agnostic). Architecture suite 51/51 — no BuildingBlocks change |

## Parity gap tables

### Admin app — `clients/admin/src/pages` ↔ `clients/admin-blazor`

| React route | Blazor page | Status |
|---|---|---|
| `/` (dashboard.tsx) | `Pages/Dashboard/OverviewPage` | ✅ |
| `/users` + `/users/{id}` + create | `Users/UsersListPage`, `UserDetailPage`, `UserCreateDialog` | ✅ |
| `/roles` + `/roles/{id}` + create | `Roles/RolesListPage`, `RoleDetailPage`, `RoleCreateDialog` | ✅ |
| `/tenants` + `/tenants/{id}` + create | `Tenants/TenantsListPage`, `TenantDetailPage`, `TenantCreateDialog` | ✅ |
| `/audits` + `/audits/{id}` | `Pages/Audits/AuditsListPage` (+ stat strip) + `AuditDetailDialog` (identity/correlation/context/payload sections) | ✅ |
| `/webhooks` + `/webhooks/{id}` + create | `Pages/Webhooks/WebhooksListPage`, `WebhookDetailPage` (delivery log), `WebhookCreateDialog` (events multi-select, secret, retry), test endpoint | ✅ |
| `/billing/*` (plans, invoices, topups, invoice detail) | `Pages/Billing/PlansListPage` (+ `PlanFormDialog`), `InvoicesListPage`, `InvoiceDetailPage`, `TopupsListPage` (+ `TopupDecisionDialog`) | ✅ |
| `/health` | `Pages/Health/HealthPage` (live + ready probes, 10s auto-refresh, expandable check rows) | ✅ |
| `/impersonation` | `Pages/Impersonation/ImpersonationListPage` (+ `ImpersonateDialog`, `RevokeGrantDialog`) | ✅ |
| `/notifications/inbox` | `Pages/Notifications/NotificationsInboxPage` (unread/all filter, mark-read/all-read, live SignalR append) | ✅ |
| `/settings/*` (profile, security, sessions, appearance) | `Pages/Settings/{ProfilePage, SecurityPage, SessionsPage, AppearancePage}.razor` (+ `SettingsScaffold`), 2FA enroll/verify/disable, session revoke, theme cards | ✅ |
| `/auth/*` (login, forgot, reset, confirm) | `Pages/Auth/*` | ✅ |
| `not-found.tsx` | `FshNotFound.razor` (shared) | ✅ |
| — | **deferred parity items** | 🔲 command palette → ✅ (Phase C, e4b3dcb7, 155/155) · accent/font/density settings → ✅ (sess-maui @ 2026-08-06 — React admin `settings/appearance.tsx` parity: Density placeholder section + Active badge on theme cards; admin 158/158, 0 warnings) |

### Dashboard app — `clients/dashboard/src/pages` ↔ `clients/dashboard-blazor`

| React route | Blazor page | Status |
|---|---|---|
| `/` (overview.tsx) | `Pages/Overview/OverviewPage` (+ `.razor.cs` code-behind, `IDashboardService`, SSE LIVE chip) | ✅ 3.1 |
| `/auth/*` | `Pages/Auth/*` | ✅ |
| `/activity` (activity.tsx) | `Pages/Activity/ActivityPage` (+ `.razor.cs`, live SSE event log, tone pills, 200-cap) | ✅ 3.2 |
| `/subscription` | `Pages/Subscription/SubscriptionPage` (+ `.razor.cs`, plan card, validity, usage bars, recent invoices) | ✅ 3.3 |
| `/wallet` | `Pages/Wallet/WalletPage` (+ `.razor.cs`, balance card, top-up form, paginated request list) | ✅ 3.4 |
| `/invoices` + `/invoices/{id}` | `Pages/Invoices/InvoicesPage` (search+pager list) + `InvoiceDetailPage` (line items, PDF download) | ✅ 3.5 |
| `/catalog/*` (products + detail, brands, categories) | `Pages/Catalog/BrandsPage` + `CategoriesPage` + `ProductsPage` + `ProductDetailPage` + `BrandEditorDialog` + `CategoryEditorDialog` + `ProductEditorDialog` + `PriceDialog` + `StockDialog` | ✅ 3.6 |
| `/identity/users`, `/roles`, `/groups` (+ details) | `Pages/Identity/UsersListPage` + `UserCreateDialog` + `UserDetailPage` (roles/sessions/impersonate) + `RolesListPage` + `RoleEditorDialog` + `RoleDetailPage` (grouped permission editor) + `GroupsListPage` + `GroupEditorDialog` + `GroupDetailPage` + `AddGroupMembersDialog` | ✅ 3.7 |
| `/tickets` + `/tickets/{id}` | `Pages/Tickets/TicketsListPage` + `TicketDetailPage` + `CreateTicketDialog` + `ResolveDialog` + `AssignDialog` | ✅ 3.8 |
| `/chat/*` (channel rail, chat page, settings, pinned, search, composer, messages…) | `Pages/Chat/ChatPage` (+ `CreateChannelDialog`, SignalR integration) | ✅ 3.9 |
| `/files` | `Pages/Files/FileManagerPage` (+ `FilePreviewDialog`, presigned upload via `fshFile.js`) | ✅ 3.10 |
| `/system/health`, `/system/audits`, `/system/trash`, `/system/sessions` | `Pages/System/HealthPage`, `AuditsPage` (+ `AuditDetailDialog`), `SessionsPage`, `TrashPage` (tabbed) | ✅ 3.11 |
| `/settings/*` (profile, security, appearance, api-keys, notifications…) | — | 🔲 3.12 |
| impersonation banner + `/tenant-deactivated`, `/impersonation-ended` (terminal pages) | — **docs claimed they existed; they do not** | 🔲 3.13 |
| command palette (`Ctrl+K`) | — | 🔲 3.14 (deferred) |
| `not-found.tsx` | bare `<NotFound>` template | 🔲 styled 404 |

### MAUI Hybrid — `clients/FSH.Hybrid`

| Surface | Status |
|---|---|
| Shell (sidebar/topbar parity, theme) | 🔲 5.x — app has **zero `.razor` pages**; workload now installed (elevated) and project migrated to .NET 10 conventions — builds 4 TFMs green (§7.6 of `hands-on-phase-7.md`) |
| All dashboard screens | 🔲 Phase 5 |
| Native features (SecureStorage, push, biometric, camera, offline queue, deep links) | 🔲 Phase 5 |

## Build order

1. **Admin** 2.4 Billing ✅ → 2.5 Webhooks ✅ → 2.6 Audits ✅ → 2.7 Health ✅ → 2.8 Notifications inbox ✅ →
   2.9 Settings ✅ → 2.10 Impersonation ✅ → styled 404 (parallel: permission constants per feature).
2. **Dashboard** 3.1 Overview ✅ → 3.2 Activity ✅ → 3.3 Subscription ✅ → 3.4 Wallet ✅ → 3.5 Invoices ✅ → 3.6 Catalog ✅ → 3.7 Identity ✅ → 3.8…3.13 per the table (each page: service → page → route → SSE/live data where
   the React page has it → bUnit test).
3. **MAUI** 5.x — workload install, shell parity, then screens.
4. **Phase 6 exit** — parity audit (6.5) items re-checked against these tables; deferred items
   (command palette, accent/font/density settings) only after all pages land.

## Verification gate (every page, no exceptions)

- bUnit render test (loading/empty/error/data states) in the app's test project.
- Manual smoke against a running API (dev or Aspire stack).
- Nav + permission wiring checked against the parity tables.
- Docs updated in the **same** change: `.agents/rules/frontend/*` if a convention changed,
  this file's status column, and `00-Index.md`.

## Gotchas carried into this phase

- Route parameters: pages bind `[Parameter] string Id` — **never** use `{Id:guid}` constraints with a
  string parameter (Guid route value → `InvalidCastException`). Add a `Router`-level regression test
  for every new detail route.
- `index.html` `<base href="/" />` must stay the first element in `<head>`; the guard test
  (`IndexHtmlGuardTests`) enforces it.
- MAUI workload state is fragile on this machine (failed install deleted manifest packs; elevated
  repair required). See `Phase-05-MAUI-Hybrid/plan.md` for the fix steps — and §7.6 of
  `hands-on-phase-7.md` for the finished recipe (legacy `Microsoft.NET.Sdk.Maui` SDK is gone in
  .NET 10; use `Microsoft.NET.Sdk.Razor` + `Platforms/` folder + pinned package versions).
