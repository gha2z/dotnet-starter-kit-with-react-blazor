# Phase 2 — Admin Feature Pages

> **Target:** All admin operator pages built — Tenants, Users, Roles, Billing, Webhooks, Audits, Notifications, Health, Settings, Impersonation. Feature parity with `clients/admin` React app.

## Status

- Phase 2: **🔲 Not started**
- Prerequisites: Phase 1 ✅ (auth+login working, permissions, AppShell, routing guard)

## Task Checklist

### 2.1 Identity — Users
- [ ] **IUserService** — `SearchAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetRolesAsync`, `AssignRolesAsync`
- [ ] **UserDto / CreateUserRequest / UpdateUserRequest** models
- [ ] **UsersListPage.razor** — MudTable with search, MudTableSortLabel, pagination, MudChip for roles, delete confirm dialog
- [ ] **UserCreatePage.razor** — MudForm in MudDialog: first name, last name, email, phone, role select
- [ ] **UserDetailPage.razor** — MudCard sections: profile info, assigned roles (MudChip), active sessions, impersonate button
- [ ] **Permission gate**: `Permissions.Identity.Users.View`, `.Create`, `.Edit`, `.Delete`

### 2.2 Identity — Roles
- [ ] **IRoleService** — `SearchAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetPermissionsAsync`, `UpdatePermissionsAsync`
- [ ] **RoleDto / CreateRoleRequest / UpdateRoleRequest** models
- [ ] **RolesListPage.razor** — MudTable with users count, edit/delete actions
- [ ] **RoleDetailPage.razor / RoleEditPage.razor** — MudTreeView with checkboxes for permission editor, MudSwitch for toggles
- [ ] **Permission gate**: `Permissions.Identity.Roles.View`, `.Create`, `.Edit`, `.Delete`

### 2.3 Tenants
- [ ] **ITenantService** — `SearchAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `UpgradeSubscriptionAsync`
- [ ] **TenantDto / CreateTenantRequest** models
- [ ] **TenantsListPage.razor** — MudTable with subscription tier, expiry, status MudChip, search
- [ ] **TenantCreatePage.razor** — MudStepper wizard: admin account, domain, subscription plan
- [ ] **TenantDetailPage.razor** — MudCard: info, subscription (tier + dates), usage stats
- [ ] **TenantUpgradeDialog.razor** — MudDialog: MudSelect for plan tier, MudNumericField for period
- [ ] **Permission gate**: `Permissions.Tenants.View`, `.Create`, `.Edit`, `.Upgrade`

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

### 2.7 Notifications
- [ ] **INotificationService** — `SearchAsync`, `MarkReadAsync`, `MarkAllReadAsync`
- [ ] **NotificationsListPage.razor** — MudTable with message, type icon, timestamp, read/unread badge (MudBadge)
- [ ] **NotificationBell.razor** — MudMenu trigger in AppBar: unread count badge, dropdown list, mark-read action
- [ ] **SignalR hub integration** — Receive real-time notifications via SignalR hub connection

### 2.8 Health
- [ ] **IHealthService** — `GetHealthAsync` (server status, resource usage)
- [ ] **HealthDashboardPage.razor** — MudCard grid: API status (MudProgressCircular), DB connection, Redis, Hangfire, MinIO. MudProgressBar for resource usage.

### 2.9 Settings
- [ ] **ProfilePage.razor** — MudForm: edit first/last name, email, phone. MudFileInput for avatar
- [ ] **ThemeSettingsPage.razor** — Dark/light MudSwitch, accent color picker (MudSelect)
- [ ] **SessionsListPage.razor** — MudTable with browser, device, IP, last active, revoke MudButton

### 2.10 Impersonation
- [ ] **IImpersonationService** — `BeginAsync`, `EndAsync`, `ListActiveAsync`
- [ ] **ImpersonationListPage.razor** — MudTable with impersonator, target user/tenant, start timestamp, end MudButton
- [ ] **ImpersonationBanner.razor** — MudAlert banner at top when impersonating: "You are impersonating {user}" + end button

### 2.11 Dashboard Landing Page
- [ ] **AdminDashboardPage.razor** — MudGrid of MudCards: total tenants, active users, revenue (if billing), recent activity feed
- [ ] Quick stats API call

## Next Up

**Task 2.1**: Users list + create/detail pages.

1. Read React reference: `clients/admin/src/pages/identity/users.tsx`
2. Create `IUserService` + `UserService` in BlazorShared/Services/
3. Create DTOs in BlazorShared/Models/
4. Create `UsersListPage.razor` (MudTable with search, sort, pagination)
5. Create `UserCreateDialog.razor` (MudForm in MudDialog)
6. Create `UserDetailPage.razor` (MudCard sections)
7. Register routes + permission gates
8. Write bUnit tests

## Architecture Decisions

| Decision | Choice | Why |
|---|---|---|
| Service pattern | Interface + implementation per module | Matches backend DI pattern; testable with mocks |
| List pages | Dedicated .razor page, not generic | Each list has unique columns/actions; generic table in Phase 1 for reuse |
| Create/Edit | MudDialog for simple forms, dedicated page for complex | Follows React pattern (dialog for quick-create, page for detail wizard) |
| Navigation | MudNavMenu in Drawer, gated by permissions | `INavService` provides filtered nav items based on `PermissionsProvider` |
| SignalR | Connect on login, disconnect on logout | Matches React: hub connection lifecycle tied to auth state |
| Sidebar state | Persist collapsed/expanded in localStorage | User preference survives refresh |

## Notes & Gotchas

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
