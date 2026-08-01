# Phase 2 — Admin Feature Pages

> **Target:** All admin operator pages built — Tenants, Users, Roles, Billing, Webhooks, Audits, Notifications, Health, Settings, Impersonation. Feature parity with `clients/admin` React app.

## Status

- Phase 2: **🟨 In progress** — 2.1 Users ✅ + 2.2 Roles ✅ + 2.3 Tenants ✅ + 2.11 Overview ✅ (pages + 53 bUnit tests green; suite 53/53) — next: 2.4 Billing
- Prerequisites: Phase 1 ✅ (auth+login working, permissions, AppShell, routing guard)
- **Phase 7 hardening applied to 2.1/2.2:** detail routes are plain `{Id}` (no `:guid` constraint — was crashing with `string` params); see `Phase-07-Parity-Completion/plan.md`.

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

### 2.4 Billing
- [ ] **IBillingService** — `GetPlansAsync`, `GetSubscriptionsAsync`, `GetInvoicesAsync`, `GetInvoiceDetailAsync`
- [ ] **BillingPlansPage.razor** — MudCard grid of plans, feature list (MudList), price highlights, CTA button
- [ ] **SubscriptionsListPage.razor** — MudTable with tenant, plan, status, dates
- [ ] **InvoicesListPage.razor** — MudTable with date, amount, status MudChip, download MudButton
- [ ] **InvoiceDetailPage.razor** — MudCard with line items, PDF preview

### 2.5 Webhooks
- [ ] **IWebhookService** — `SearchAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `TestAsync`
- [ ] **WebhooksListPage.razor** — MudTable with URL, events (MudChip list), status, last trigger timestamp
- [ ] **WebhookCreateDialog.razor** — MudForm: URL, MudSelect for events (multi), secret, retry config
- [ ] **WebhookDetailPage.razor** — Info card + delivery log (MudTable with status, timestamp, HTTP code)
- [ ] **WebhookTestButton.razor** — Trigger test → MudAlert for success/failure

### 2.6 Audits
- [ ] **IAuditService** — `SearchAsync`, `GetDetailAsync`
- [ ] **AuditTrailPage.razor** — MudTable with entity name, action, user, timestamp, MudChip for action type
- [ ] **AuditDetailDialog.razor** — MudDialog with JSON viewer (MudCode or MudTextField with monospace), old/new values diff

### 2.7 Notifications 🟨 (bell ✅, inbox page pending)
- [x] **INotificationService** — `GetUnreadCountAsync`, `ListAsync`, `MarkReadAsync`, `MarkAllReadAsync` (parity sprint; `clients/BlazorShared/Services/NotificationService.cs`)
- [ ] **NotificationsListPage.razor** — MudTable with message, type icon, timestamp, read/unread badge (MudBadge)
- [x] **FshNotificationBell.razor** — shared MudMenu bell (parity sprint): unread badge cap 99+, list of 20, mark-read on click + navigate, mark-all-read, refresh on open — in the topbar of both apps
- [x] **SignalR hub integration** — subscribe to `NotificationCreated` (AppHub, user group) in the bell; lifecycle tied to auth state

### 2.8 Health
- [ ] **IHealthService** — `GetHealthAsync` (server status, resource usage)
- [ ] **HealthDashboardPage.razor** — MudCard grid: API status (MudProgressCircular), DB connection, Redis, Hangfire, MinIO. MudProgressBar for resource usage.

### 2.9 Settings
- [ ] **ProfilePage.razor** — MudForm: edit first/last name, email, phone. MudFileInput for avatar
- [ ] **ThemeSettingsPage.razor** — Light/Dark/System (ThemeMode) + accent color picker — **accent/font/density deferred to Phase 7** (React parity item); theme mode itself already works via the topbar toggle/menu
- [ ] **SessionsListPage.razor** — MudTable with browser, device, IP, last active, revoke MudButton
- [ ] **SecurityPage.razor** — password change (React `/settings/security` parity)

### 2.10 Impersonation
- [ ] **IImpersonationService** — `BeginAsync`, `EndAsync`, `ListActiveAsync`
- [ ] **ImpersonationListPage.razor** — MudTable with impersonator, target user/tenant, start timestamp, end MudButton
- [ ] **ImpersonationBanner.razor** — MudAlert banner at top when impersonating: "You are impersonating {user}" + end button

### 2.11 Dashboard Landing Page ✅
- [x] **AdminDashboardPage.razor** — `Pages/Dashboard/OverviewPage` — stat tiles (tenants/users/roles), quick links; parity pass applied (stat tile components)

## Next Up

**Task 2.4**: Billing — plans grid + subscriptions list + invoices list/detail.

1. Read React reference: `clients/admin/src/pages/billing/`
2. `IBillingService` — extend with `GetSubscriptionsAsync`, `GetInvoicesAsync`, `GetInvoiceDetailAsync` (`clients/BlazorShared/Services/BillingService.cs`)
3. `BillingPlanDto` — add feature list, trial days (server `GetPlansEndpoint` shape; check `Modules.Billing.Contracts`)
4. `BillingPlansPage.razor` — MudCard grid of plans, price highlights, CTA
5. `SubscriptionsListPage.razor` — MudTable with tenant, plan, status, dates
6. `InvoicesListPage.razor` — MudTable with date, amount, status MudChip, download MudButton
7. `InvoiceDetailPage.razor` — MudCard with line items, PDF preview
8. Register routes + permission gates (`BillingPermissions.View` + per-action); bUnit tests

## Architecture Decisions

| Decision | Choice | Why |
|---|---|---|
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

- [ ] Phase 1 complete: can login, tokens stored, AuthStateProvider works, AppShell renders
- [ ] API identity endpoints work (test in Swagger): Users CRUD, Roles CRUD, Tenants CRUD
- [ ] Permission constants match server-side values
- [ ] MudTheme renders correctly in both light/dark mode
