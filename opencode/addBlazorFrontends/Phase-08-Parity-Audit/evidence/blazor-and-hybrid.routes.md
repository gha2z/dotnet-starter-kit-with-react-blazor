# Blazor + MAUI Hybrid App Surface Inventory (Parity Audit Evidence)

Generated for Phase-08 Parity Audit. All paths absolute under
`C:\repos\Project\dotnet-starter-kit-with-react-blazor-main\clients`.
Read-only research: no source files were modified. `clients/FSH.Hybrid/**` and
`clients/BlazorShared/**` treated as read-only (uncommitted wire work exists in
`clients/FSH.Hybrid` — see §1.6).

Route counts: **Admin Blazor = 27 @page**, **Dashboard Blazor = 37 @page**,
**Hybrid = 3 @page (+ 2 native MAUI Shell tabs)**, **BlazorShared = 0 @page** (RCL).

---

## 1. Admin Blazor route table (`clients/admin-blazor`, FSH.Admin.Wasm)

`@page` source: FSH.Admin.Pages (`..\admin-blazor\FSH.Admin.Pages\`) renders into
FSH.Admin.Wasm (Router in `App.razor`). Permission gates are policy-based: 25
`AddPolicy("Permissions.*")` registrations in
`clients/admin-blazor/FSH.Admin.Wasm/Program.cs:67-91`, applied per-page via
`@attribute [Authorize(Policy=...)]` and enforced by `AuthorizeRouteView`.

Realtime: no SSE in admin. SignalR notifications via `FshNotificationBell`
(`clients/admin-blazor/FSH.Admin.Wasm/Shared/MainLayout.razor:97`) + shared
`IHubConnectionService`. Command palette: `clients/admin-blazor/FSH.Admin.Wasm/Shared/MainLayout.razor:152`.
Theming: `FshThemeService` injected in `App.razor:3`.

| # | Route | `.razor` page (relative to `admin-blazor/FSH.Admin.Pages/Pages/`) | 1-line description | Kind | Tables | Search/filter/pager | Forms | Permission gate | Realtime/theme |
|---|-------|-------------------------------------------------------------------|--------------------|------|--------|---------------------|-------|-----------------|----------------|
| 1 | `/` | `Dashboard/OverviewPage.razor:1` | Operator dashboard landing (KPI/summary); bunit-tested `FSH.Admin.Wasm.Tests/Pages/Dashboard/OverviewPageTests.cs:31` | dashboard | — | — | — | `[Authorize]` (shell) | FshThemeService |
| 2 | `/login` | `Auth/LoginPage.razor:1` | Sign-in (tenant + email/password) | auth form | — | — | MudForm | public | theme |
| 3 | `/forgot-password` | `Auth/ForgotPasswordPage.razor:1` | Request password-reset email | auth form | — | — | MudForm | public | theme |
| 4 | `/reset-password` | `Auth/ResetPasswordPage.razor:1` | Set new password from token | auth form | — | — | MudForm | public | theme |
| 5 | `/confirm-email` | `Auth/ConfirmEmailPage.razor:1` | Confirm email from link | auth form | — | — | form | public | theme |
| 6 | `/users` | `Identity/Users/UsersListPage.razor:1` | User list + create invite dialog | list | MudTable | FshFilterBar/FshPager shared | `UserCreateDialog` | `Permissions.Users.*` | bell/theme |
| 7 | `/users/{Id}` | `Identity/Users/UserDetailPage.razor:1` | User detail + edit/roles/2FA actions | detail | — | — | edit form | `Permissions.Users.Update` | theme |
| 8 | `/roles` | `Identity/Roles/RolesListPage.razor:1` | Role list + create dialog | list | MudTable | shared pager | `RoleCreateDialog` | `Permissions.Roles.*` | theme |
| 9 | `/roles/{Id}` | `Identity/Roles/RoleDetailPage.razor:1` | Role detail (permission matrix) | detail | permission table | — | — | `Permissions.Roles.Update` | theme |
| 10 | `/tenants` | `Tenants/TenantsListPage.razor:1` | Tenant list + create dialog | list | MudTable | shared | `TenantCreateDialog` | `Permissions.Tenants.*` | theme |
| 11 | `/tenants/{Id}` | `Tenants/TenantDetailPage.razor:1` | Tenant detail + upgrade subscription + theme | detail | — | — | dialogs (subscription/theme) | `Permissions.Tenants.*` | theme |
| 12 | `/webhooks` | `Webhooks/WebhooksListPage.razor:1` | Webhook subscriptions list | list | MudTable | shared | create/edit dialog | `Permissions.Webhooks.*` | theme |
| 13 | `/webhooks/{Id}` | `Webhooks/WebhookDetailPage.razor:1` | Webhook detail + test payload | detail | — | — | test form | `Permissions.Webhooks.Test` | theme |
| 14 | `/audits` | `Audits/AuditsListPage.razor:1` | Audit trail list + detail dialog | list | MudTable | shared filter/pager | `AuditDetailDialog` | `Permissions.AuditTrails.*` | theme |
| 15 | `/billing` | `Billing/BillingIndexPage.razor:1` | Billing hub card page | hub | — | — | — | `Permissions.Billing.View` | theme |
| 16 | `/billing/invoices` | `Billing/InvoicesListPage.razor:1` | Invoice list | list | MudTable | shared | — | `Permissions.Billing.View` | theme |
| 17 | `/billing/invoices/{Id}` | `Billing/InvoiceDetailPage.razor:1` | Invoice detail (items, totals) | detail | items table | — | — | `Permissions.Billing.View` | theme |
| 18 | `/billing/plans` | `Billing/PlansListPage.razor:1` | Billing plans list | list | MudTable | shared | — | `Permissions.Billing.View` | theme |
| 19 | `/billing/topups` | `Billing/TopupsListPage.razor:1` | Wallet top-up history | list | MudTable | shared | — | `Permissions.Billing.Manage` | theme |
| 20 | `/health` | `Health/HealthPage.razor:1` | API/Redis health checks | detail | checks table | — | — | `[Authorize]` | theme |
| 21 | `/impersonation` | `Impersonation/ImpersonationListPage.razor:1` | Active impersonations + revoke | list | MudTable | — | revoke action | `Permissions.Impersonation.*` | theme |
| 22 | `/notifications/inbox` | `Notifications/NotificationsInboxPage.razor:1` | Notification inbox | list | cards | mark-all-read | — | `[Authorize]` | FshNotificationBell+Hub |
| 23 | `/settings` | `Settings/SettingsIndexPage.razor:1` | Settings hub | hub | — | — | — | `[Authorize]` | theme |
| 24 | `/settings/profile` | `Settings/ProfilePage.razor:1` | Profile + change-password | form | — | — | MudForm | `[Authorize]` | theme |
| 25 | `/settings/sessions` | `Settings/SessionsPage.razor:1` | My active sessions + revoke | list | MudTable | — | revoke | `[Authorize]` | theme |
| 26 | `/settings/security` | `Settings/SecurityPage.razor:1` | Two-factor auth + API key mgmt | form/detail | — | — | forms | `[Authorize]` | theme |
| 27 | `/settings/appearance` | `Settings/AppearancePage.razor:1` | Theme light/dark/system + accent | form | — | — | MudForm | `[Authorize]` | FshThemeService |

Bunit coverage exists for nearly every surface: `FSH.Admin.Wasm.Tests/Pages/*` (Overview, Audits
List+DetailDialog, Billing {Plans,Invoices,InvoiceDetail,Topups} + dialogs, Tenants
List/Detail/CreateDialog, Webhooks List/Detail, Identity Users/Roles incl. `RouteBindingRegressionTests`,
Settings Appearance/Security/Sessions/Profile, Dashboard Overview, Health, NotificationsInbox),
`Components/CommandPaletteTests`, `Html/IndexHtmlGuardTests`. Counts in §5.

---

## 2. Dashboard Blazor route table (`clients/dashboard-blazor`, FSH.Dashboard.Wasm)

`@page` source: FSH.Dashboard.Pages. Shell
`FSH.Dashboard.Wasm/Shared/MainLayout.razor` wires: `FshThemeService` (:6), `ISseService` (:9),
`FshOfflineBanner` (:97), `FshNotificationBell` (:103), SSE status dot "Realtime connected/offline"
(:171-172), `CommandPalette` (:221) with Ctrl+K. `App.razor` subscribes `FshThemeService` (:3).
SSE is consumed only by `Overview/OverviewPage.razor.cs:15` and `Activity/ActivityPage.razor.cs:12`
(both `[Inject] ISseService`). SignalR bell = shared `FshNotificationBell` (same RCL as admin).

| # | Route | `.razor` page (relative to `dashboard-blazor/FSH.Dashboard.Pages/Pages/`) | 1-line description | Kind | Realtime | Theme/UX notes |
|---|-------|----------------------------------------------------------------------------|--------------------|------|----------|----------------|
| 1 | `/` | `Overview/OverviewPage.razor:1` | Tenant dashboard (KPIs, recent activity, subscription status) — SSE live updates | dashboard | SSE (`OverviewPage.razor.cs:15`) | FshThemeService |
| 2 | `/activity` | `Activity/ActivityPage.razor:1` | Live activity feed/trail — SSE | list/feed | SSE (`ActivityPage.razor.cs:12`) | theme |
| 3 | `/wallet` | `Wallet/WalletPage.razor:1` | WhatsApp wallet balance + top-up history | list | — | theme |
| 4 | `/login` | `Auth/LoginPage.razor:1` | Tenant sign-in | auth form | — | theme |
| 5 | `/forgot-password` | `Auth/ForgotPasswordPage.razor:1` | Request password reset | auth form | — | theme |
| 6 | `/reset-password` | `Auth/ResetPasswordPage.razor:1` | Set new password | auth form | — | theme |
| 7 | `/confirm-email` | `Auth/ConfirmEmailPage.razor:1` | Confirm email | auth form | — | theme |
| 8 | `/catalog/products` | `Catalog/ProductsPage.razor:1` | Products list + create/edit dialog | list | — | shared FshFilterBar/FshPager |
| 9 | `/catalog/products/{Id}` | `Catalog/ProductDetailPage.razor:1` | Product detail | detail | — | theme |
| 10 | `/catalog/brands` | `Catalog/BrandsPage.razor:1` | Brands list/CRUD | list | — | shared |
| 11 | `/catalog/categories` | `Catalog/CategoriesPage.razor:1` | Categories list/CRUD | list | — | shared |
| 12 | `/tickets` | `Tickets/TicketsListPage.razor:1` | Ticket list + filters | list | — | shared |
| 13 | `/tickets/{Id}` | `Tickets/TicketDetailPage.razor:1` | Ticket detail + comments/replies | detail | — | theme |
| 14 | `/subscription` | `Subscription/SubscriptionPage.razor:1` | Subscription + upgrade | detail/form | — | theme |
| 15 | `/impersonation-ended` | `Terminal/ImpersonationEndedPage.razor:1` | Terminal screen after impersonation ends | terminal | — | TerminalLayout (`Terminal/TerminalLayout.razor:1`) |
| 16 | `/tenant-deactivated` | `Terminal/TenantDeactivatedPage.razor:1` | Terminal screen when tenant deactivated | terminal | — | TerminalLayout |
| 17 | `/system/trash` | `System/TrashPage.razor:1` | Restore-pending items (products/brands/categories/tickets/files) | list | — | theme |
| 18 | `/invoices` | `Invoices/InvoicesPage.razor:1` | Invoice list | list | — | theme |
| 19 | `/system/sessions` | `System/SessionsPage.razor:1` | Active sessions + revoke all | list | — | theme |
| 20 | `/settings/security` | `Settings/SettingsSecurityPage.razor:1` | 2FA + security | form | — | theme |
| 21 | `/invoices/{Id}` | `Invoices/InvoiceDetailPage.razor:1` | Invoice detail | detail | — | theme |
| 22 | `/system/health` | `System/HealthPage.razor:1` | Health checks | detail | — | theme |
| 23 | `/files` | `Files/FileManagerPage.razor:1` | File manager (upload, presigned URLs) | list | — | theme |
| 24 | `/settings/appearance` | `Settings/SettingsAppearancePage.razor:1` | Theme appearance | form | — | FshThemeService |
| 25 | `/chat` | `Chat/ChatPage.razor:1` | Channel chat + SignalR messages | app | SignalR (shared HubConnectionService) | theme |
| 26 | `/settings/api-keys` | `Settings/SettingsApiKeysPage.razor:1` | API key management | form | — | theme |
| 27 | `/identity/groups/{Id}` | `Identity/GroupDetailPage.razor:1` | Group detail + members | detail | — | theme |
| 28 | `/identity/groups` | `Identity/GroupsListPage.razor:1` | Group list/CRUD | list | — | shared |
| 29 | `/identity/roles/{Id}` | `Identity/RoleDetailPage.razor:1` | Role detail/permissions | detail | — | theme |
| 30 | `/settings` | `Settings/SettingsIndexPage.razor:1` | Settings hub | hub | — | theme |
| 31 | `/settings/branding` | `Settings/SettingsBrandingPage.razor:1` | Tenant branding (logo/theme accent) | form | — | FshThemeService |
| 32 | `/identity/roles` | `Identity/RolesListPage.razor:1` | Role list + create | list | — | shared |
| 33 | `/settings/notifications` | `Settings/SettingsNotificationsPage.razor:1` | Notification preferences | form | — | bell |
| 34 | `/identity/users/{Id}` | `Identity/UserDetailPage.razor:1` | User detail + roles | detail | — | theme |
| 35 | `/identity/users` | `Identity/UsersListPage.razor:1` | User list + create | list | — | shared |
| 36 | `/system/audits` | `System/AuditsPage.razor:1` | Audit trail (tenant-scoped) | list | — | shared filter/pager |
| 37 | `/settings/profile` | `Settings/SettingsProfilePage.razor:1` | Profile + change password | form | — | theme |

Terminal/404: 404 handled by shared `FshNotFound` (`BlazorShared/Components/FshNotFound.razor:1`,
"Go to Dashboard" button). Terminal pages render inside `Pages/Terminal/TerminalLayout.razor`
(a bare `LayoutComponentBase`, 3 lines). Impersonation state surfaced via
`Auth/ImpersonationBanner` + `ImpersonationHandoff` (see test files §5).

---

## 3. Hybrid surface (`clients/FSH.Hybrid/FSH.Hybrid`) — read-only, working-tree truth

Uncommitted session work verified via `git status`:
`M Shared/MainLayout.razor`, `M Shared/MainLayout.razor.cs`, `M wwwroot/css/app.css`,
`M wwwroot/index.html`, `?? Shared/NavSpec.cs`, `?? wwwroot/js/` (§9 git status).
`NavSpec.cs` exists in working tree and **is** the sidebar source of truth (§96).

### 3.1 Blazor `@page` routes (only 3)

| Route | File | Notes |
|-------|------|-------|
| `/` | `Pages/OverviewPage.razor:1` | `@attribute [Authorize]` (:2); `IDashboardService` KPI tiles `FshKpiTile` (Tenant/Plan/Subscription, :17-24); text :30-33 confirms parity gap: "Feature pages (chat, files, tickets, catalog, billing, settings) land with the BlazorShared page parity work" |
| `/files` | `Pages/FilesPage.razor:1` | `@attribute [Authorize]` (:2); native `IMediaPickerService` (Pick/CapturePhoto) + `IHybridFileUploadService` presigned PUT (:4-5); `MudTable` file rows (:30-40); upload status alerts |
| `/login` | `Pages/Auth/LoginPage.razor:1` | `MauiAuthService` + `ITokenStore` + `MauiAuthStateProvider` + `IBiometricService` injected (:2-6); MudForm tenant/email/password; biometric unlock on authed entry |

Root component `Main.razor`: `MudThemeProvider` with `FshMudTheme.CreateDashboard()` (`FshThemeService.IsDarkMode`), `Router` over `typeof(Main).Assembly` with `AuthorizeRouteView` + `RedirectToLogin` for NotAuthorized and `FshNotFound` for NotFound (:14-29). Deep-link bridge consumed in `OnInitializedAsync` via `HybridNavigationBridge.BlazorPathReceived`/`PendingPath` (:44-50, :56-65).

### 3.2 Layout + nav (working tree)

- `Shared/MainLayout.razor` (248 lines, uncommitted): custom sidebar (no MudDrawer) —
  brand mark, `<NavLink>` loops over `VisibleTopItems`/`VisibleSections` from `NavSpec`
  (:37-56), collapsible 220px↔52px (:17-18), skip-link (:9), drawer on mobile (:12-15),
  theme menu Light/Dark/System (:78-113), user menu with initials + sign-out (:114-150),
  `AuthorizeView` wrapper — shell chrome only when authorized, unauthenticated sees bare `@Body` (:159-163).
- `Shared/NavSpec.cs` (66 lines, **untracked**): single source of nav. `TopItems` = Overview `/`,
  Chat `/chat` (ChatPermissions.Channels.View), My Files `/files` (FilesPermissions.Upload)
  (:17-22). `TrashPermissions` any-of list (:24-31). `Sections` = operations
  (activity/subscription/wallet/invoices), catalog (products/brands/categories), helpdesk (tickets),
  identity (users/roles/groups), system (health/audits/sessions/trash) (:33-65). Doc comment:
  "Mirrors the dashboard NavSpec … but omits BottomItems — Settings is handled natively via
  MAUI Shell flyout."
- **Unusual finding (impact)**: NavSpec targets the full **dashboard** route set (`/chat`,
  `/activity`, `/subscription`, `/wallet`, `/invoices`, `/catalog/*`, `/tickets`, `/identity/*`,
  `/system/*`) but only 3 of those routes exist as `@page` in the hybrid project. Any
  permission-visible nav item on an unimplemented path renders `FshNotFound`
  (`Main.razor:27-29`). Nav render is permission-gated (`MainLayout.razor:174-180`), so e.g.
  `/activity` (no permission requirement) is always visible and always 404s today.
  `OverviewPage.razor:30-33` self-documents the gap.

### 3.3 Native shell (MAUI)

- `AppShell.xaml` (`Shell.FlyoutBehavior="Flyout"`): single FlyoutItem with 3 tabs —
  Dashboard→`hybrid:MainPage` (route `home`), Settings→`pages:SettingsPage` (route `settings`),
  About→`pages:AboutPage` (route `about`) (:12-22).
- `AppShell.xaml.cs`: registers `SettingsPage` and `AboutPage` Shell routes (:11-12).
- `App.xaml.cs`: cold-start deep link via `HybridNavigationBridge.TakeInitialAppLink()` (:20-23);
  on window created starts `IOfflineQueueProcessor.ProcessQueueAsync()` (:28-35); `OnAppLinkRequestReceived`
  → `HandleAppLink` → `DeepLinkService.Parse` + Shell `GoToAsync` (:40-67); `OnSleep` locks
  `MauiAuthStateProvider` (:69-77); `OnResume` unlocks + flushes queue (:79-92); Windows Mica backdrop (:94-102).
- Native pages are **not Blazor**: `Pages/SettingsPage` and `Pages/AboutPage` are MAUI (XAML). About
  is likely the git submodule convention; verify against `Pages/` contents (list was blocked — see §6 note).

### 3.4 DI + native services (`MauiProgram.cs`, 106 lines)

| Service | Registration | Implementation / behavior |
|---------|--------------|----------------------------|
| `ITokenStore` | `MauiProgram.cs:28` | `MauiTokenStore.cs` — SecureStorage access/refresh/tenant/permissions; no impersonation stash (:66-73) |
| `IBiometricService` | :29 | `BiometricService.cs` — Win `UserConsentVerifier`, Android `BiometricPrompt` (BiometricStrong), iOS/macOS `LAContext` (:15-24 dispatch) |
| `IRuntimeConfigService` | :30 | `HybridRuntimeConfigService.cs` — `Preferences`-backed ApiBase (default `https://localhost:7030`), `DefaultTenant=root` (:7-18) |
| `IConnectivityService` | :31 | `ConnectivityService.cs` — MAUI Essentials Connectivity wrapper |
| `IOfflineQueueService` | :32 | `OfflineQueueService.cs` — SQLite FIFO (`offline-queue.db3`), `MaxRetries = 3` (:29-40) |
| `IOfflineQueueProcessor` | :33 | flushes queue on connectivity restore / resume (App.xaml.cs:33, :88) |
| `IDeepLinkService` | :34 | `DeepLinkService.cs` — `fsh://` → shell route + optional Blazor path (:16-39) |
| `IMediaPickerService` | :35 | `MediaPickerService.cs` — native FilePicker / MediaPicker camera (:22-38) |
| `IPushNotificationService` | :36 | `PushNotificationService.cs` — **Phase 5.4, compile-gated `#if ANDROID && FSH_FIREBASE`** (:21-27); no platform push without Firebase setup (package refs check: `MauiProgram.cs` none of Plugin.Firebase; `FSH.Hybrid.csproj:36-46` only CommunityToolkit.Maui, sqlite-net, AndroidX.Biometric) |
| Auth | :39-45 | `AuthStateProvider` + `AuthenticationStateProvider` mapper, `PermissionsProvider`, `MauiAuthStateProvider`, `IAuthService→AuthService`, `MauiAuthService` |
| HTTP | :48-62 | `AuthDelegatingHandler` (transient) + `OfflineDelegatingHandler` outermost on `"FSH.Api"` (:58-59). `OfflineDelegatingHandler.cs` queues POST/PUT/PATCH/DELETE when offline, throws `OfflineException` (:27-46) |
| Data services (RCL) | :65-79 | Billing, Notification, Dashboard, Catalog, User, Role, Group, Session, Impersonation, Ticket, Chat, File, Health, Audit, `HybridFileUploadService` |
| Authorization | :81 | `FshPolicies.Register` — 45 policies in `Auth/FshPolicies.cs:9-57` (Users/Roles/Groups/Sessions/Tenants/Billing/Catalog/Webhooks/AuditTrails/Impersonation/Chat/Files) |
| MudBlazor + theme | :84-90 | `AddMudServices` + `AddSingleton<FshThemeService>`→`SecureThemeService(IJSRuntime)` |
| Realtime | :93-94 | `IHubConnectionService→HubConnectionService` (SignalR), `ISseService→SseService` (SSE) — same RCL types as WASM apps |
| WebView | :97 | `AddMauiBlazorWebView()`; Debug tools when `DEBUG` (:99-102) |

### 3.5 Hybrid pages vs dashboard page set (parity delta)

Present in Hybrid today: `/`, `/files`, `/login` only.
Referenced by Hybrid NavSpec but **implemented only in dashboard-blazor FSH.Dashboard.Pages**:
`/chat`, `/activity`, `/subscription`, `/wallet`, `/invoices`, `/invoices/{Id}`,
`/catalog/products`, `/catalog/products/{Id}`, `/catalog/brands`, `/catalog/categories`,
`/tickets`, `/tickets/{Id}`, `/identity/users`, `/identity/users/{Id}`, `/identity/roles`,
`/identity/roles/{Id}`, `/identity/groups`, `/identity/groups/{Id}`, `/system/health`,
`/system/audits`, `/system/sessions`, `/system/trash`.
Hybrid-only native surfaces: `/settings` + `/about` (MAUI Shell tabs), `fsh://` deep links.

---

## 4. Shared RCL inventory (`clients/BlazorShared`, FSH.BlazorShared)

### 4.1 Components (`Components/*.razor`)

| Component | Responsibility |
|-----------|----------------|
| `FshNavSection.razor` | Collapsible sidebar nav group (used by Hybrid `MainLayout.razor:47`; bunit in FshNavSectionTests) |
| `FshPager.razor` | Pagination control for list pages (FshPagerTests) |
| `FshFilterBar.razor` | Search/filter bar for list pages |
| `FshLoadingRow.razor` | Table loading skeleton row |
| `FshKpiTile.razor` | KPI metric tile (Hybrid `OverviewPage.razor:17-23`) |
| `FshSectionRule.razor` | Section divider/label |
| `FshNotificationBell.razor` | SignalR notification bell; `Hub.On<NotificationDto>("NotificationCreated", …)` (:121), unread badge, mark-all-read, navigates `item.Link` (:178-181) |
| `FshNotFound.razor` | 404 page ("Go to Dashboard" button, :17-20) |
| `FshOfflineBanner.razor` | Offline indicator (dashboard `MainLayout.razor:97`) |
| `FshErrorBand.razor` / `FshErrorBoundary.razor` | Error display / error boundary (Hybrid `Main.razor:13`) |
| `FshEmptyState.razor` | Empty-list state (used by bell :38) |
| `FshPageHeader.razor` | Page title/description (Hybrid pages) |
| `FshPermissionGate.razor` | Permission-gated UI block |
| `FshToneIconTile.razor`, `FshMonogram.razor`, `FshStatTile.razor`, `FshStatusPill.razor` | Small presentational atoms (login tile `LoginPage.razor:15`) |
| `FshConfirmDialog.razor` + `FshConfirmDialogContent.razor` | Confirm dialog extension for MudDialog |

Per-app `CommandPalette` lives in the app projects, not the RCL
(admin `MainLayout.razor:152`, dashboard `MainLayout.razor:221`; each has its own
`CommandPaletteTests`). There is no BlazorShared `CommandPalette`.

### 4.2 Auth infrastructure (`Auth/`)

- `AuthStateProvider.cs` — `AuthenticationStateProvider` (injected in admin
  `Program.cs`, dashboard, + Hybrid `MauiProgram.cs:39-41`).
- `ITokenStore.cs` — interface implemented by WASM TokenStore and Hybrid `MauiTokenStore` (§3.4).
- `PermissionsProvider.cs` — publishes the user's permission claims.

`Infrastructure/AuthDelegatingHandler.cs` — attached to `"FSH.Api"` in all three hosts
(Hybrid `MauiProgram.cs:59`); `Infrastructure/RetryAfterHandler.cs`, `FshNetworkStatus.cs +
INetworkStatus.cs`, `InactivityTimerService.cs`, `IRuntimeConfigService.cs` (WASM impl
`RuntimeConfigService.cs` reads `/config.json`; Hybrid impl overrides via Preferences),
`NavigationExtensions.cs`.

### 4.3 Services (`Services/*.cs`) — interface + implementation per domain

`IUserService/UserService`, `IRoleService/RoleService`, `IGroupService/GroupService`,
`ISessionService/SessionService`, `IImpersonationService/ImpersonationService`,
`ITenantService/TenantService` (not ref'd by Hybrid), `IWebhookService/WebhookService` (admin-only),
`IBillingService/BillingService`, `IDashboardService/DashboardService`,
`ICatalogService/CatalogService`, `ITicketService/TicketService`, `IChatService/ChatService`,
`IFileService/FileService`, `IHealthService/HealthService`, `IAuditService/AuditService`,
`IAuthService/AuthService` (+ `TwoFactorService`, `NotificationService/INotificationService`).
Hybrid `MauiProgram.cs:65-79` registers the subset it references and adds
`IHybridFileUploadService/HybridFileUploadService` (presigned PUT with native stream,
`MediaPickerService.cs:16-18`).

### 4.4 Realtime / SSE / permissions / models / JS

- `Realtime/HubConnectionService.cs` + `Realtime/IHubConnectionService.cs` — SignalR wrapper
  (bell subscribes `Hub.On<T>`; `MainLayout`-hosted).
- `Sse/SseService.cs` + `Sse/SseTokenResponse.cs` — SSE with `X-Accel-Buffering`-style auth token
  handshake; wired in dashboard (`MainLayout.razor:9`, pages inject `ISseService`, `_Imports.razor:19`).
- `Permissions/*.cs` — 11 permission constant classes: Chat, Catalog, Files, Groups, Identity,
  Multitenancy, Sessions, Tickets, Webhooks, Auditing, Billing (used by Hybrid `NavSpec.cs` §3.2
  and `FshPolicies.cs` claims).
- `Models/*` — shared DTOs for all domains (PagedResult, Notifications, Billing{Wallet,Topup,Invoice,
  BillingPlan}, Catalog, Chat, Files, Health, Dashboard, Identity{User,Role,Group,Impersonation,
  Settings}, Audits{+OptionLists}, Tenants, Tickets, Webhooks, Json/FlexibleEnumJsonConverter).
- `Formatting/FshFormat.cs` — shared format helpers.
- JS modules (`wwwroot/js/`): `fshWindow.js`, `fshTheme.js`, `fshNetwork.js`, `fshFile.js`,
  `fshError.js`, `fshChatScroll.js` (WASM apps). Hybrid adds its own untracked
  `wwwroot/js/fshSkipLink.js` (skip-link focus, `MainLayout.razor:183`).

---

## 5. Test surface

Method counts = declared `[Fact]`/`[Theory]` attributes found in `*.cs` (excluding bin/obj);
tests were NOT executed.

| App | Test project | Framework | Declared tests |
|-----|--------------|-----------|----------------|
| Admin (WASM) bunit | `clients/admin-blazor/FSH.Admin.Wasm.Tests/FSH.Admin.Wasm.Tests.csproj` (bunit 2.0.66, xunit 2.9.3, Shouldly, NSubstitute, AngleSharp — csproj:14-21) | bunit | **150 `[Fact]` + 1 `[Theory]`** (`Html/IndexHtmlGuardTests.cs:33`) = **151** |
| Admin E2E | `clients/admin-blazor/FSH.Admin.Wasm.E2E.Tests/FSH.Admin.Wasm.E2E.Tests.csproj` | Playwright (route-mocked) | **9 `[Fact]`** (AuthFlow 4, PermissionGate 2, Tenants 1, Users 2) |
| Dashboard (WASM) bunit | `clients/dashboard-blazor/FSH.Dashboard.Wasm.Tests/FSH.Dashboard.Wasm.Tests.csproj` (bunit 2.0.66 — csproj:14-21) | bunit | **209 `[Fact]`** (Auth 20, Components 26, Infrastructure 11, Theming 10, Pages 142) |
| Dashboard E2E | `clients/dashboard-blazor/FSH.Dashboard.Wasm.E2E.Tests/FSH.Dashboard.Wasm.E2E.Tests.csproj` | Playwright | **15 `[Fact]`** (AuthFlow 4, CatalogProducts 2, MobileViewport 3, Users 3, SlowNetwork 1, PermissionGate 2) |
| Hybrid unit | `clients/FSH.Hybrid/FSH.Hybrid.Tests/FSH.Hybrid.Tests.csproj` (net10.0-windows, xunit — csproj:4-15) | xunit | **12 `[Fact]`** — `OfflineQueueServiceTests` 5, `OfflineQueueProcessorTests` 3, `OfflineDelegatingHandlerTests` 4 (no bunit — no .razor tests) |

Dashboard bunit breakdown (from §92): Auth `ImpersonationBannerTests` 7, `ImpersonationHandoffTests` 5,
`TerminalErrorHandlerTests` 8; Components `CommandPaletteTests` 8, `FshErrorBandTests` 3,
`FshNavSectionTests` 4, `FshOfflineBannerTests` 5, `FshPagerTests` 6; Infrastructure
`AuthDelegatingHandlerTests` 4, `RetryAfterHandlerTests` 7; Theming `FshMudThemeContrastTests` 2,
`FshThemeServiceTests` 8; Pages: Activity 7, Brands 5, Categories 4, ProductDetail 9, Products 7,
Chat 12, FileManager 6, GroupDetail 3, GroupsList 2, RoleDetail 8, RolesList 3, UserCreateDialog 2,
UserDetail 4, UsersList 2, InvoiceDetail 3, Invoices 4, Overview 6, ChangePasswordDialog 4,
SettingsAppearance 3, SettingsProfile 3, SettingsSecurity 3, Subscription 5, Audits 3, Health 4,
Sessions 4, Trash 3, TerminalPages 4, TicketDetail 8, TicketsList 6, Wallet 5.

Admin bunit breakdown (from §90): Overview 4, AuditsList 5, AuditDetailDialog 4, Billing Invoices 4,
InvoiceDetail 8, Plans 5, Topups 6, Health 6, RolesList 5, RoleDetail 7, RoleCreateDialog 3,
RouteBindingRegression 10, UsersList 5, UserDetail 4, UserCreateDialog 4, NotificationsInbox 4,
Appearance 6, Profile 6, Security 10, Sessions 7, TenantsList 6, TenantDetail 7, TenantCreateDialog 6,
WebhookList 6, WebhookDetail 4, CommandPalette 8.

---

## 6. Summary tables

### Admin (27 routes — FSH.Admin.Pages)

| Group | Routes |
|-------|--------|
| Auth (4) | `/login`, `/forgot-password`, `/reset-password`, `/confirm-email` |
| Identity (4) | `/users`, `/users/{Id}`, `/roles`, `/roles/{Id}` |
| Tenancy (2) | `/tenants`, `/tenants/{Id}` |
| Billing (5) | `/billing`, `/billing/invoices`, `/billing/invoices/{Id}`, `/billing/plans`, `/billing/topups` |
| Integrations/ops (5) | `/webhooks`, `/webhooks/{Id}`, `/audits`, `/health`, `/impersonation` |
| Dashboard/notify (2) | `/`, `/notifications/inbox` |
| Settings (5) | `/settings`, `/settings/profile`, `/settings/sessions`, `/settings/security`, `/settings/appearance` |

### Dashboard (37 routes — FSH.Dashboard.Pages)

| Group | Routes |
|-------|--------|
| Auth (4) | `/login`, `/forgot-password`, `/reset-password`, `/confirm-email` |
| Overview/ops (4) | `/`, `/activity`, `/wallet`, `/subscription` |
| Billing (3) | `/invoices`, `/invoices/{Id}`, `/settings/api-keys` (settings) |
| Catalog (4) | `/catalog/products`, `/catalog/products/{Id}`, `/catalog/brands`, `/catalog/categories` |
| Helpdesk (2) | `/tickets`, `/tickets/{Id}` |
| Files (1) | `/files` |
| Chat (1) | `/chat` |
| Identity (6) | `/identity/users`, `/identity/users/{Id}`, `/identity/roles`, `/identity/roles/{Id}`, `/identity/groups`, `/identity/groups/{Id}` |
| System (5) | `/system/health`, `/system/audits`, `/system/sessions`, `/system/trash`, `/settings/notifications` |
| Settings (4) | `/settings`, `/settings/profile`, `/settings/appearance`, `/settings/branding`, `/settings/security` (5 incl. security) |
| Terminal (3) | `/impersonation-ended`, `/tenant-deactivated`, 404→`FshNotFound` |

### Hybrid (3 Blazor @page + 2 native Shell tabs)

| Surface | Type |
|---------|------|
| `/` OverviewPage | Blazor (authorized, KPI tiles) |
| `/files` FilesPage | Blazor (native picker/camera → presigned upload) |
| `/login` LoginPage | Blazor (MauiAuthService; biometric unlock) |
| Settings / About | native MAUI Shell tabs (`AppShell.xaml:16-20`) |
| 404 / terminal | `FshNotFound` via `Main.razor:27-29` |

---

## Unusual findings

1. **Hybrid nav points at unimplemented routes.** `Shared/NavSpec.cs` (untracked) mirrors the full
   dashboard route set, but only `/`, `/files`, `/login` exist as `@page` in FSH.Hybrid. Visible
   nav items such as `/activity` (no permission gate) render `FshNotFound`. Self-documented at
   `Pages/OverviewPage.razor:30-33`.
2. **`@page` in RCL: none.** All routable pages live in the per-app Pages projects; BlazorShared is
   components/services only (0 `@page`).
3. **SSE is dashboard-only** (`ISseService` injected in dashboard Overview + Activity;
   `FSH.Dashboard.Wasm/_Imports.razor:19`). Admin uses SignalR bell only; Hybrid wires `SseService`
   in DI (`MauiProgram.cs:94`) but no hybrid page consumes it yet.
4. **Push notifications are compile-gated**: `#if ANDROID && FSH_FIREBASE`
   (`PushNotificationService.cs:21`); no Firebase/Plugin referenced in `FSH.Hybrid.csproj:36-46`,
   so the shared path always falls back to `null` token + info log until `push-setup.md` is followed.
5. **Hybrid auth/offline native-only additions** not present in either WASM app: `BiometricService`,
   `OfflineQueueService`/`OfflineDelegatingHandler`/`OfflineException`, `DeepLinkService` +
   `HybridNavigationBridge`, `MediaPickerService`/`HybridFileUploadService`, `SecureThemeService`
   (SecureStorage-backed `FshThemeService`), `MauiAuthStateProvider` app-lock on `OnSleep`.
6. **One blocked read** (working-tree `Pages/` directory listing of FSH.Hybrid was rejected by the
   shell guard): page inventory was instead derived from `@page` grep (§7) = all `.razor` files
   with routes; the native `SettingsPage`/`AboutPage` XAML pages confirmed via
   `AppShell.xaml:16-20`.