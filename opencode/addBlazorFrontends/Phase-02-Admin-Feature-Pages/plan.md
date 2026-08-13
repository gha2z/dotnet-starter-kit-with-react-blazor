# Phase 2 — Admin Feature Pages
Last Update: 2026-Aug-03 18:45:55, by: opencode (auto/coding, model: mimo-v2.5-free).

> **Target:** All admin operator pages built — Tenants, Users, Roles, Billing, Webhooks, Audits, Notifications, Health, Settings, Impersonation. Feature parity with `clients/admin` React app.

## Status

- Phase 2: **✅ Complete** — All sub-features (2.1 through 2.10) are done, with bUnit tests passing and zero build warnings.
  - 2.1 Users ✅
  - 2.2 Roles ✅
  - 2.3 Tenants ✅
  - 2.4 Billing ✅
  - 2.5 Webhooks ✅
  - 2.6 Audits ✅
  - 2.7 Health ✅
  - 2.8 Notifications inbox ✅
  - 2.9 Settings ✅
  - 2.10 Impersonation ✅
  - 2.11 Dashboard Landing Page (Admin Overview) ✅
- Verification: admin bUnit suite **147/147**, build **0 warnings / 0 errors**; missing `INotificationService` registration fixed in `FSH.Admin.Wasm/Program.cs`.
- **Runtime DI audit (this session):** every `[Inject]`/`@inject` in the admin app + BlazorShared cross-checked against
  `Program.cs`. Added the **9 missing API service registrations** (`IAuditService`, `IBillingService`,
  `IImpersonationService`, `IRoleService`, `ISessionService`, `ITenantService`, `ITwoFactorService`, `IUserService`,
  `IWebhookService` — `AddScoped<IX, XService>()`) and re-registered `IHealthService` (was concrete `HealthService`
  only; `HealthPage` injects the interface). This was the post-login "An unhandled error has occurred" root cause
  (`OverviewPage` at `/` is the first render and crashed on `ITenantService`). Also fixed `js/fshWindow.js`
  `export function openUrl()` → `window.openUrl = function () {...}` (plain `<script>` load was throwing
  `SyntaxError: Unexpected token 'export'`).
- Prerequisites: Phase 1 ✅ (auth+login working, permissions, AppShell, routing guard)
- Phase 7 hardening applied to 2.1/2.2: detail routes are plain `{Id}` (no `:guid` constraint — was crashing with `string` params); see `Phase-07-Parity-Completion/plan.md`.
- Page numbering note: the roadmap's 2.5–2.8 map to plan sections below as 2.5 Webhooks, 2.6 Audits, 2.7 Health, 2.8 Notifications (the historical "2.7 Notifications / 2.8 Health" labels were renumbered when Health was built; 00-Index and this file now agree).

## Task Checklist

### 2.1 Identity — Users ✅
- [x] **IUserService** — `SearchAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetRolesAsync`, `AssignRolesAsync` (`clients/BlazorShared/Services/UserService.cs`)
- [x] **UserDto / RegisterUserRequest / UpdateUserRequest** models (`clients/BlazorShared/Models/Identity/UserDtos.cs`)
- [x] **UsersListPage.razor** — MudTable (server-side) with debounced search, MudTableSortLabel headers (firstname/username/email/isactive), MudSelect filters (status/email/role), MudChip status columns, row-click → detail, "New user" dialog, `_roleError`/`_error` alert separation
- [x] **UserCreateDialog.razor** — MudForm in MudDialog: first/last name, username, email, phone, password + confirm (mismatch validation)
- [x] **UserDetailPage.razor** — MudCard sections: profile info, role MudSwitches + pending-change save, status toggle, sessions table
- [x] **Permission gate**: `IdentityPermissions.Users.View`, `.Create`, `.Edit`, `.Delete` + FshPermissionGate
- [x] **bUnit tests** — 14/14 green (`FSH.Admin.Wasm.Tests/Pages/Identity/Users/`)

### 2.2 Identity — Roles ✅
- [x] **IRoleService** — `ListAsync`, `GetAsync`, `GetWithPermissionsAsync`, `UpsertAsync`, `DeleteAsync`, `UpdatePermissionsAsync`, `GetPermissionCatalogAsync` (`clients/BlazorShared/Services/RoleService.cs`)
- [x] **RoleDto / UpsertRoleRequest / UpdateRolePermissionsRequest / PermissionCatalogEntryDto** models (`clients/BlazorShared/Models/Identity/RoleDtos.cs`)
- [x] **RolesListPage.razor** — client-side MudTable (React parity): system-first sort (Admin/Basic), debounced search on name+description, permission count column, "System" chip, row-click → detail, New role dialog
- [x] **RoleCreateDialog.razor** — MudForm: name (2-64), description (max 256); navigates to detail on success
- [x] **RoleDetailPage.razor** — profile section (upsert; disabled for system roles), permission editor grouped by Resource from the **server catalog endpoint** (`/permissions/catalog`, tenant-filtered) with select-all/clear, root/basic chips, dirty counter, danger zone (type-name-to-confirm delete)
- [x] **Permission gate**: `IdentityPermissions.Roles.View`, `.Create`, `.Update`, `.Delete` + FshPermissionGate
- [x] **Fixed permission-constant bug**: BlazorShared + Program.cs policies used `*.Edit` claims — server issues `*.Update` (`Permissions.Users.Update`, `Permissions.Roles.Update`); gates never matched in production
- [x] **bUnit tests** — 16/16 green (`FSH.Admin.Wasm.Tests/Pages/Identity/Roles/`); TestSetup now exposes `Authorization` (`BunitAuthorizationContext`) — policy-gated buttons need `SetAuthorized` + `SetPolicies`

### 2.3 Tenants ✅
- [x] **ITenantService** — `SearchAsync`, `GetStatusAsync`, `GetProvisioningAsync`, `CreateAsync`, `RenewAsync`, `AdjustValidityAsync`, `ChangeActivationAsync`, `RetryProvisioningAsync` (`clients/BlazorShared/Services/TenantService.cs`); **IBillingService** — `GetPlansAsync` (`BillingService.cs`)
- [x] **TenantDto / TenantStatusDto / CreateTenantRequest (+Response) / RenewTenantRequest (+Response) / AdjustTenantValidityRequest (+Response) / ChangeTenantActivationRequest / TenantLifecycleResultDto / TenantProvisioningStatusDto (+StepDto)** models (`clients/BlazorShared/Models/Tenants/TenantDtos.cs`, `Models/Billing/BillingPlanDtos.cs` — `int Interval` projection, 0=Monthly 1=Yearly)
- [x] **TenantsListPage.razor** — server-side MudTable (`RowsPerPageString="Rows"`), monogram avatars, Active/Inactive chip, valid-until + admin email columns, row-click → detail, New tenant dialog gate
- [x] **TenantCreateDialog.razor** — auto-slug identifier with Edit/Auto adornment toggle, issuer auto-mirror (dirty-tracking), generate-password, billing plan MudSelect (from `/billing/plans`), advanced panel (issuer, connection string), navigates to detail on success
- [x] **TenantDetailPage.razor** — status header (Active/InGrace/Expired chips, plan, valid-until, issuer), Renew / Adjust validity / Deactivate gated by `Update`/`UpgradeSubscription`, provisioning run card with step timeline + retry, "no run history" state
- [x] **RenewTenantDialog.razor / AdjustTenantValidityDialog.razor** — plan-select renew (reuses `PlanLabel`) + MudDatePicker validity bump, typed `DialogParameters<T>`
- [x] **Permission gates**: `Tenants.View/.Create/.Update/.UpgradeSubscription/.ViewTheme/.UpdateTheme` — constants fixed to server truth (`MultitenancyPermissions.cs`; no `.Edit`/`.Delete` exist server-side)
- [x] **bUnit tests** — 19/19 green (`FSH.Admin.Wasm.Tests/Pages/Tenants/`); TestSetup `DefaultWaitTimeout` raised to 30s (dialog-render flakiness under parallel suite)
- [x] **MudBlazor 9.7 gotcha**: `OnAdornmentClick` only wires when `AdornmentIcon` is used — `AdornmentText` renders a non-clickable `<p>` (upstream `MudInputAdornment.razor` renders `MudIconButton` only in the icon branch)

### 2.4 Billing ✅
- [x] **IBillingService** — `GetPlansAsync`, `GetSubscriptionsAsync`, `GetInvoicesAsync`, `GetInvoiceDetailAsync`, `GetUsageTotalsAsync` (`clients/BlazorShared/Services/BillingService.cs`); `BillingPlanDto` + feature list (server `GetPlansEndpoint` shape)
- [x] **PlansListPage.razor** — MudCard grid of plans, price highlights, feature list, interval selector; `PlanFormDialog` for create/edit (`PlanLabel` reuse)
- [x] **SubscriptionsListPage.razor** — MudTable with tenant, plan, status, dates (`Pages/Billing/SubscriptionsListPage` under `BillingScaffold` tabs)
- [x] **InvoicesListPage.razor** — MudTable with invoice no, amount, status MudChip, usage summary, row-click → detail
- [x] **InvoiceDetailPage.razor** — status header, usage meter, line items, periods, credit note
- [x] **TopupsListPage.razor** — MudTable with transaction id, amount, status, `TopupDecisionDialog` (approve/reject)
- [x] **BillingScaffold.razor / BillingTabs.razor** — `/billing` index redirect → invoices (React parity), tabbed layout
- [x] **Permission gates** — `BillingPermissions.View` + per-action; bUnit tests

### 2.5 Webhooks ✅
- [x] **IWebhookService** — `GetSubscriptionsAsync`, `CreateSubscriptionAsync`, `DeleteSubscriptionAsync`, `TestSubscriptionAsync`, `GetDeliveriesAsync` (`clients/BlazorShared/Services/WebhookService.cs`); `WebhookSubscriptionDto` / `WebhookDeliveryDto` / `CreateWebhookSubscriptionRequest` models (`Models/Webhooks/`)
- [x] **WebhooksListPage.razor** — MudTable with URL, events (MudChip list), created date, actions; row-click → detail; New subscription dialog
- [x] **WebhookCreateDialog.razor** — MudForm: URL, MudSelect multi events, secret, retry config
- [x] **WebhookDetailPage.razor** — info card + **deliveries log** (MudTable with status chip, timestamp, HTTP code, request/response preview)
- [x] **WebhookTestButton** — `TestSubscriptionAsync` → success/failure snackbar
- [x] **Permission gates** — `WebhooksPermissions.*` + FshPermissionGate; bUnit tests (list + detail)

### 2.6 Audits ✅
- [x] **IAuditService** — `ListAsync` (ListAuditsRequest: search, event type, severity, tenant, correlation, sort), `GetAsync` (detail), `GetSummaryAsync` (events-by-type/severity histograms) (`clients/BlazorShared/Services/AuditService.cs`)
- [x] **AuditSummaryDto / AuditDetailDto / AuditSummaryAggregateDto** models (`Models/Audits/AuditDtos.cs`); `AuditOptionLists` (event-type + severity option lists)
- [x] **AuditsListPage.razor** — stat strip (total, security events, errors+critical), filter bar (search, event type, severity, cross-tenant tenant field, correlation), paper list with pill/dot status, pager, empty/error/loading states, row-click → detail side-sheet
- [x] **AuditDetailDialog.razor** — detail side-sheet: header w/ severity pill, identity section, correlation section (copy-to-clipboard chips), context section (who/where/when), payload JSON viewer (pretty-printed); subcomponents `AuditIdentitySection` / `AuditCorrelationSection` / `AuditContextSection` / `AuditPayloadSection` / `AuditCorrelationChip`
- [x] **Event-type/severity coercion** — server keys histograms by integer enum values; service translates to string unions so the UI can index by name
- [x] **Permission gate** — `AuditingPermissions.AuditTrails.View` + cross-tenant `.ViewCrossTenant` (React parity: tenant filter only when permitted); bUnit tests (list + sections + route binding)

### 2.7 Health ✅
- [x] **IHealthService** — `GetLivenessAsync`, `GetReadinessAsync` (`clients/BlazorShared/Services/HealthService.cs`); `HealthResult` / `HealthEntry` models (`Models/Health/HealthDtos.cs`)
- [x] **Anonymous probe client** — dedicated `FSH.Health` HttpClient (no auth handler, 8s timeout) so probes behave like React's raw `fetch` and don't drag the tenant header/token into a public endpoint; readiness 503-with-body is parsed
- [x] **HealthPage.razor** — stat strip (liveness, readiness, checks healthy, checks failing), liveness + readiness probe sections with `/health/live` `/health/ready` chips, "No dependency checks reported" empty state, 10s auto-refresh (`Timer`), manual Refresh button
- [x] **HealthCheckRow.razor** — status dot, name + description, duration, expandable details grid (mono key/value), degraded→failing aggregation (React parity)
- [x] No permission gate (probes are anonymous); bUnit tests

### 2.8 Notifications — inbox ✅
- [x] **INotificationService** — `GetUnreadCountAsync`, `ListAsync` (`?unreadOnly=&page=&pageSize=`), `MarkReadAsync`, `MarkAllReadAsync` (`clients/BlazorShared/Services/NotificationService.cs`); `NotificationDto` model
- [x] **NotificationsInboxPage.razor** — `/notifications/inbox`: header w/ count chip + Refresh + Mark all read, unread/all filter, live SignalR append (`NotificationCreated`), per-row Mark read, "inbox zero" empty state, error band
- [x] **FshNotificationBell.razor** — shared MudMenu bell (unread badge cap 99+, list of 20, mark-read on click + navigate, mark-all-read, refresh on open) — in the topbar of both apps (parity sprint)
- [x] **SignalR hub integration** — subscribe to `NotificationCreated` (AppHub, user group) in bell + inbox; lifecycle tied to auth state
- [x] bUnit tests (inbox list/empty/error/mark-all; bell suite already green)

### 2.9 Settings ✅
- [x] **IUserService** — `GetMyProfileAsync`, `SetProfileImageAsync`, `ChangePasswordAsync`; new **ISessionService** (`GetMySessionsAsync`, `RevokeMySessionAsync`, `RevokeAllMySessionsAsync`) and **ITwoFactorService** (`EnrollAsync` → sharedKey+authenticatorUri, `VerifyEnrollAsync`, `DisableAsync`) (`clients/BlazorShared/Services/*`); `ChangePasswordRequest` / `SetProfileImageRequest` / `TwoFactorEnrollmentResponse` models (`Models/Identity/SettingsDtos.cs`)
- [x] **ProfilePage.razor** — avatar (monogram + upload with immediate save), identity form (first/last name, username, email, phone, `IsActive`/`IsVerified` chips), image-set dialog hosted by `TestShell`
- [x] **SecurityPage.razor** — **PasswordSection** (change password, `ValidateAsync` + `_form.IsValid` pattern, `ContentCopy` icon) + **TwoFactorSection** (enable→shared key + copy, verify code, disable w/ password confirm via MudDialog)
- [x] **SessionsPage.razor** — own sessions MudTable (browser/device/IP/last active), revoke current + revoke-all (FshFormat date rendering in `FSH.BlazorShared.Formatting`)
- [x] **AppearancePage.razor** — Dark/Light theme cards (clickable `div role="button"`, not MudCard — MudBlazor 9.7 MudCard has no `OnClick`); `FshThemeService` persisted via JSInterop
- [x] **SettingsScaffold.razor** — shared SettingsShell tabs layout (profile/security/sessions/appearance)
- [x] **Permission** — `[Authorize]` on all four routes (FshPermissionGate not required — authenticated settings, React parity)
- [x] **bUnit tests** — 28/28 green (`Pages/Settings/*` + 4 settings route-binding tests); `TestShell.razor` adds MudPopover/Dialog/Snackbar providers for dialog tests

### 2.10 Impersonation ✅
- [x] **IImpersonationService** — `ListGrantsAsync`, `StartImpersonationAsync`, `RevokeGrantAsync`
- [x] **ImpersonationListPage.razor** — MudTable with impersonator, target user/tenant, start/expiry timestamp, status badge, revoke/reopen actions
- [x] **ImpersonateDialog.razor** — two-step (pick user in tenant → reason + duration) → issues token, hands off to dashboard in new tab via URL hash
- [x] **ImpersonationBanner.razor** — MudAlert banner at top when impersonating: "You are impersonating {user}" + end button
- [x] bUnit tests (list/detail/dialogs)

### 2.11 Dashboard Landing Page (Admin Overview) ✅
- [x] **AdminDashboardPage.razor** — `Pages/Dashboard/OverviewPage` — stat tiles (tenants/users/roles), quick links; parity pass applied (stat tile components)

## Next Up
- None — Phase 2 is complete. Move to Phase 3.

## Architecture Decisions

| Decision | Choice | Why |
|----------|--------|-----|
| Service pattern | Interface + implementation per module | Matches backend DI pattern; testable with mocks |
| List pages | Dedicated .razor page, not generic | Each list has unique columns/actions; generic table in Phase 1 for reuse |
| Create/Edit | MudDialog for simple forms, dedicated page for complex | Follows React pattern (dialog for quick-create, page for detail wizard) |
| Navigation | Custom `fsh-sidebar` accordion (parity sprint), gated by permissions | **Correction (Phase 7):** the earlier `MudNavMenu in Drawer` choice was superseded — both apps use the React-parity custom shell (`FshNavSection` accordion, collapsed icon stack); no `MudDrawer` |
| SignalR | Connect on login, disconnect on logout | Matches React: hub connection lifecycle tied to auth state |
| Sidebar state | Persist collapsed/expanded in localStorage | User preference survives refresh |

## Notes & Gotchas

- **SSE console hygiene (dashboard)**: real flow is `POST /api/v1/sse/token` → `GET /api/v1/sse/stream?token=<guid>` (never `/api/v1/realtime/stream`); connect only when a token exists (startup + `TokensChanged`), `StopAsync()` on logout, auto-reconnect with backoff. Full symptom→fix table in `hands-on-phase-2.md` §2.12.
- **Permission constants**: Must match server-side `Permissions` class exactly. Every permission used in a `[Authorize(Policy = "...")]` attribute must be registered in `Program.cs`.
- **Lazy loading**: Each major area (Identity, Tenants, etc.) should be a separate assembly for lazy loading — defer to Phase 6 unless bundle size becomes a problem
- **Form validation**: Use `DataAnnotationsValidator` + `MudForm.Validate()` — matches MudBlazor conventions. FluentValidation adapter available but not required.
- **Audit diff view**: Server returns old/new JSON — render as side-by-side in MudDialog or a monospace diff. Simple string diff is fine for Phase 2 (no need for jsdiff).
- **Impersonation**: When admin impersonates a user, they navigate to the dashboard app. The impersonation flow crosses app boundaries — coordinate with Phase 3.
- **MudBlazor MudTable**: Set `RowsPerPage` from config, not hardcoded. `ServerData` attribute for server-side pagination.
- **Debounce search**: Use `Immediate="true"` on `MudTextField` with debounce in the service layer via `CancellationToken` or timer.
- **Mobile**: MudDrawer with `Breakpoint="Breakpoint.Md"` for responsive sidebar. All MudTable should scroll horizontally on narrow viewports.

## Blocker Checklist

- [x] Phase 1 complete: can login, tokens stored, AuthStateProvider works, AppShell renders
- [x] API identity endpoints work (test in Swagger): Users CRUD, Roles CRUD, Tenants CRUD
- [x] Permission constants match server-side values
- [x] MudTheme renders correctly in both light/dark mode