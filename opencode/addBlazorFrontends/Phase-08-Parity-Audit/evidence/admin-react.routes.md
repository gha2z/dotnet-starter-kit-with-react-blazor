# React Admin App Surface — Parity Evidence

Source tree: `clients/admin` (read-only parity reference — never edit).
Router: React Router 7 `createBrowserRouter` in `src/routes.tsx`, lazy-loaded page chunks via `lazyNamed()` helper.
All paths below are relative to `C:\repos\Project\dotnet-starter-kit-with-react-blazor-main\clients\admin\`.

---

## 1. Route table

Router definition: `src/routes.tsx:69-240`. Lazy page bindings: `src/routes.tsx:30-63`.
App provider shell: `src/App.tsx:12-38` (ThemeProvider → QueryClientProvider → AuthProvider → RealtimeProvider → Suspense → RouterProvider). Boot: `src/main.tsx:10` awaits `loadRuntimeConfig()` before render; `src/main.tsx:17-21` mounts `<App/>`.

Lazy helper (rewrites named export to default): `src/routes.tsx:21-28`.

### Public routes (no auth)

| Path | Component (file) | What the page does |
|---|---|---|
| `/login` | `LoginPage` (`src/pages/login.tsx:36`) | Tenant+email+password sign-in; dev-only demo-account quick sign-ins; redirects to `from` or `/` | `src/routes.tsx:70` |
| `/forgot-password` | `ForgotPasswordPage` (`src/pages/auth/forgot-password.tsx:30`) | Requests a one-time reset link by email+tenant | `src/routes.tsx:71` |
| `/reset-password` | `ResetPasswordPage` (`src/pages/auth/reset-password.tsx:44`) | Sets a new password from emailed token (reads `token`/`email`/`tenant` from URL); strength meter; navigates to `/login` on success | `src/routes.tsx:72` |
| `/confirm-email` | `ConfirmEmailPage` (`src/pages/auth/confirm-email.tsx:21`) | Confirms email from emailed link; shows loading/success/error states | `src/routes.tsx:73` |

### Protected routes (inside `ProtectedRoute` → `AppShell`, both with `RouteError` errorElement)

`src/routes.tsx:74-238`. App shell layout: `src/components/layout/app-shell.tsx:24-70`.

| Path | Component (file) | Guard perms | What the page does |
|---|---|---|---|
| `/` (index) | `DashboardPage` (`src/pages/dashboard.tsx:23`) | none | Operator landing: tenant-count KPI + pivot cards to Tenants/Identity/Billing |
| `/tenants` | `TenantsListPage` (`src/pages/tenants/list.tsx`) | `MultitenancyPermissions.Tenants.View` | Tenant registry list, search, status filter, create dialog, pagination | `src/routes.tsx:85-92` |
| `/tenants/new` | `<Navigate to="/tenants">` | — | Redirect stub (creation is now a dialog) | `src/routes.tsx:93-98` |
| `/tenants/:id` | `TenantDetailPage` (`src/pages/tenants/detail.tsx:50`) | `Tenants.View` | Tenant overview, provisioning status, activate/deactivate, renew/adjust validity, impersonate, active grants, branding editor | `src/routes.tsx:99-106` |
| `/users` | `UsersListPage` (`src/pages/users/list.tsx`) | `IdentityPermissions.Users.View` | Account directory: search, status/email-confirmed/role filters, table, create dialog, pagination | `src/routes.tsx:109-116` |
| `/users/new` | `<Navigate to="/users">` | — | Redirect stub | `src/routes.tsx:117-122` |
| `/users/:id` | `UserDetailPage` (`src/pages/users/detail.tsx:26`) | `Users.View` | User hero, identity card, roles editor, activate/deactivate, sessions card | `src/routes.tsx:123-130` |
| `/roles` | `RolesListPage` (`src/pages/roles/list.tsx:19`) | `IdentityPermissions.Roles.View` | Role registry list with client-side search; create dialog | `src/routes.tsx:133-140` |
| `/roles/new` | `<Navigate to="/roles">` | — | Redirect stub | `src/routes.tsx:141-146` |
| `/roles/:id` | `RoleDetailPage` (`src/pages/roles/detail.tsx:41`) | `Roles.View` | Role profile edit (RHForm+zod), permission-group editor, system-role lock, danger zone delete | `src/routes.tsx:147-154` |
| `/billing` | `BillingLayout` (`src/pages/billing/layout.tsx:18`) | `BillingPermissions.View` | Tabbed billing shell (Plans/Invoices/Top-ups); index redirects to invoices | `src/routes.tsx:157-171` |
| `/billing/plans` | `PlansListPage` (`src/pages/billing/plans-list.tsx:40`) | inherited `Billing.View` | Plan cards with KPI strip, plan form dialog (create/edit), usage rates | `src/routes.tsx:166` |
| `/billing/invoices` | `InvoicesListPage` (`src/pages/billing/invoices-list.tsx:82`) | inherited | Invoice table with KPI strip (page/billed/outstanding/paid), filters, nav to detail | `src/routes.tsx:167` |
| `/billing/invoices/:invoiceId` | `InvoiceDetailPage` (`src/pages/billing/invoice-detail.tsx:73`) | inherited | Invoice line items, download PDF, issue / mark paid / void actions | `src/routes.tsx:168` |
| `/billing/topups` | `TopupsListPage` (`src/pages/billing/topups-list.tsx:96`) | inherited | Wallet top-up request workflow (approve→invoice, reject), KPI strip, tenant/status filters | `src/routes.tsx:169` |
| `/impersonation` | `ImpersonationListPage` (`src/pages/impersonation/list.tsx:38`) | `IdentityPermissions.Impersonation.View` | All impersonation grants, KPI counts, status filter, revoke/reopen dialogs, 5s polling | `src/routes.tsx:174-181` |
| `/audits` | `AuditsListPage` (`src/pages/audits/list.tsx:40`) | `AuditingPermissions.AuditTrails.View` | Audit trail list, summary strip, filters, detail side-sheet | `src/routes.tsx:185-192` |
| `/audits/:id` | `<Navigate to="/audits">` | — | Redirect stub — detail opens as side sheet on list page | `src/routes.tsx:193-196` |
| `/webhooks` | `WebhooksListPage` (`src/pages/webhooks/list.tsx:39`) | `WebhooksPermissions.Subscriptions.View` | Webhook subscription list, test/delete actions, create dialog | `src/routes.tsx:200-207` |
| `/webhooks/:id` | `WebhookDetailPage` (`src/pages/webhooks/detail.tsx:42`) | `Subscriptions.View` | Subscription endpoint/events read-only + deliveries attempt list with pagination, test button | `src/routes.tsx:208-215` |
| `/notifications` | `NotificationsInboxPage` (`src/pages/notifications/inbox.tsx:32`) | none (all signed-in users) | Inbox with unread/all filter, live SignalR append, mark read / mark all | `src/routes.tsx:218` |
| `/health` | `HealthPage` (`src/pages/health/page.tsx:12`) | none | Liveness/readiness probes, KPI strip, 10s auto-refresh, expandable check details | `src/routes.tsx:221` |
| `/settings` | `SettingsLayout` (`src/pages/settings/layout.tsx:58`) | none (account-scoped) | Numbered settings rail (4 tabs); index redirects to profile | `src/routes.tsx:224-234` |
| `/settings/profile` | `ProfileSettings` (`src/pages/settings/profile.tsx:21`) | — | Avatar upload (presigned ImageInput), read-only identity, profile image mutation | `src/routes.tsx:229` |
| `/settings/security` | `SecuritySettings` (`src/pages/settings/security.tsx`) | — | Change password dialog, Enable/Disable 2FA (QR, TOTP verify, manual key), device/email/phone MFA toggles | `src/routes.tsx:230` |
| `/settings/sessions` | `SessionsSettings` (`src/pages/settings/sessions.tsx:24`) | — | Active sessions list, per-session revoke, revoke-all others | `src/routes.tsx:231` |
| `/settings/appearance` | `AppearanceSettings` (`src/pages/settings/appearance.tsx`) | — | Light/dark theme toggle; density "coming soon" placeholder | `src/routes.tsx:232` |
| `*` | `NotFoundPage` (`src/pages/not-found.tsx:12`) | — | Console-styled 404 with requested path and back-home/go-back | `src/routes.tsx:239` |

Redirect stubs (bookmark preservation, not real content): `/tenants/new`, `/users/new`, `/roles/new`, `/audits/:id` — `src/routes.tsx:93-98,117-122,141-146,193-196`.

**Route count: 26 renderable routes (4 public + 20 protected leaf/shell + 2 redirect indexes `/billing`,`/settings`) + 4 redirect stubs + 1 catch-all = 31 path entries in routes.tsx.**

Nav configuration (source of truth for the sidebar): `src/components/layout/nav-items.ts:41-47` (top singletons Overview/Settings), `:51-120` (3 accordion sections: Tenants, Identity, Operations), legacy flat `NAV_ITEMS` at `:164-216`.

---

## 2. Per-page feature inventory

### 2.1 `/` — Dashboard (`src/pages/dashboard.tsx:23`, 200 lines)
- **KPI**: tenant-count stat fed by `listTenants({ pageNumber:1, pageSize:1 })` — `src/pages/dashboard.tsx:26-39`; renders into a StatStrip — `src/pages/dashboard.tsx:71-74`.
- **Pivot cards** (feature cells linking to Tenants/Users/Roles/Billing): `src/pages/dashboard.tsx:126-152`; card markup `PivotCard` `:161-199` (ToneIconTile, arrow on hover).
- **Error/loading/empty**: `isLoading` slug `:71-74`; total `—` fallback `:71-74`; no table — dashboard is navigation-focused.
- **No real-time, no forms, no permission gating** beyond protected shell (route has no RouteGuard).

### 2.2 `/tenants` — Tenants list (`src/pages/tenants/list.tsx`, 283 lines; top l.1-120 + l.121-200 viewed)
- **List**: Desktop table w/ grid columns (`DESKTOP_COLS` `:60-63` area): Tenant name, Admin email (lg only), Status pill (`StatusPill` `:185-198`), chevron; mobile cards `:200+`. Row click → `/tenants/:id` `:141`.
- **Pagination**: server-side pageNumber state `:57`, prev/next buttons `:150-176`, page badge `:153`.
- **Search/filter**: search input `:110-121` + active status segmented filter.
- **Create**: `CreateTenantDialog` mounted `:178` (open state `:52`).
- **States**: `ErrorBand`, loading rows via shared components `:41`; header from `EntityPageHeader` `:86`.
- **Permission gating**: none in-page (route guard `Tenants.View`); create button not individually gated.

### 2.3 `/tenants/:id` — Tenant detail (`src/pages/tenants/detail.tsx`, 689 lines; l.1-339 viewed)
- **Sections**: Overview identity card `:142-240`; Impersonate dialog `:242-247` (perm `Users.Impersonate` `:60`); Renew/change plan dialog `:249-257` (perm `Tenants.UpgradeSubscription` `:62`); Adjust validity dialog `:259-266`; activate/deactivate ConfirmDialog `:268-292` (perm `Tenants.Update` `:66`); `ActiveGrantsCard` `:294`; `TenantBrandingCard` `:296`; Details InfoRows `:299-327`; Provisioning panel `:330-339+`.
- **Real-time / polling**: provisioning query polls every 2s while in-flight, stops on terminal/404 — `src/pages/tenants/detail.tsx:84-91`.
- **Permission gating**: `permissions.includes(...)` checks `:60-66`; action buttons conditionally rendered `:191-237`.
- **States**: ErrorBand `:133-135`, LoadingRow `:137`.

### 2.4 `/users` — Users list (`src/pages/users/list.tsx`, 485 lines; l.1-74 + l.75-349 viewed)
- **Table columns** (desktop grid): Name, Username, Email (lg), Status, chevron — `src/pages/users/list.tsx:228-237`; row comp `UserDesktopRow`. Mobile cards `UserMobileCard` `:290+` w/ Monogram `:317`, status pills `:336-349`.
- **Search + filters**: search input `:124-132`; Segmented Status (any/active/disabled) `:134-143`; Segmented Email confirmed `:144-153`; role `Select` `:155-162`; `clearFilters` `:92-97`.
- **Pagination**: `PAGE_SIZE` `:46` area, prev/next `:254-281`, page badge `:81-86`.
- **Create**: `CreateUserDialog` `:283`.
- **Permission gating**: route guard `Users.View` only.
- **States**: ErrorBand `:165-173`, loading `:175-182`, empty (no matches / no users) `:184-202`.

### 2.5 `/users/:id` — User detail (`src/pages/users/detail.tsx`, 406 lines; l.1-250 viewed)
- **Lists**: identity card DetailRows `:143-168`; `RolesEditor` checkbox list `:170-180` (implementation `:218-250+`, includes dirty count).
- **Actions**: Activate/Deactivate toggle button + mutation `:43-51` → button `:126-137`.
- **Sessions**: `UserSessionsCard userId` `:183`.
- **Form/validation**: Roles editor is draft-state checkboxes (no RHF/zod); no profile edit here.
- **States**: ErrorBand `:79`, LoadingRow `:81`.
- **Permission gating**: route guard only; no per-button perms documented in viewed lines.

### 2.6 `/roles` — Roles list (`src/pages/roles/list.tsx`, 310 lines; l.1-120 viewed)
- **Table/columns**: desktop grid `DESKTOP_COLS` `:16-17` (1fr Name, 2fr description lg, 120px ?, 24px chevron); mobile cards `:121+` presumably.
- **Search**: client-side search input `:77-85`, debounced 200ms `:25-28`, filter `:43-50`.
- **Sort**: system roles (Admin/Basic) first then alphabetical `:32-41`.
- **Create**: `CreateRoleDialog` `:67`/`:121`.
- **States**: ErrorBand `:87-95`, LoadingRow `:97`, no-results `:99-113`, EmptyState `:115-120`.
- **No pagination** (roles are few; full list from `listRoles` `:30`).

### 2.7 `/roles/:id` — Role detail (`src/pages/roles/detail.tsx`, 513 lines; l.1-350 viewed)
- **Form + validation**: profile section uses `react-hook-form` + `zodResolver` (`useForm` `:141-152`, schema `:35-38`) — same stack as every create dialog.
- **Permission editor**: `PermissionEditor` `:237+` — checkbox groups from `PERMISSION_CATALOG`, `selected` Set state `:240`, group toggle `:278-287`, dirty indicator + save/discard `:290-335`, per-group select-all state `:338-350+`.
- **System role lock**: `SYSTEM_ROLE_NAMES` `:33`, read-only banner `:93-116`, disabled profile for system roles `:120`, `DangerZone` delete only for non-system `:122-130`.
- **States**: ErrorBand `:81-89`, LoadingRow `:91`.
- **Permission gating**: route guard `Roles.View` only.

### 2.8 `/billing/plans` — Plans (`src/pages/billing/plans-list.tsx`)
- **KPI strip**: Plans / Active / Average base — `src/pages/billing/plans-list.tsx:81-91` (StatStrip). Plan cards list `title="All plans"` `:106`; edit button per plan `:177` opening `PlanFormDialog`.
- **Form dialog**: `src/components/billing/plan-form-dialog.tsx` — key/name/currency/interval/monthly+annual price/overage rates per quota resource (`ApiCalls`, `StorageBytes`, `Users`, `ActiveFeatureFlags`) `:48-56`; manual zod field validation `fieldError` `:78-79` + `:68`; not RHF (controlled inputs) `:265-355`.
- **No pagination/search** (plan list).

### 2.9 `/billing/invoices` — Invoices (`src/pages/billing/invoices-list.tsx:82`)
- **KPI strip** (4 stats): Page invoices / Billed / Outstanding / Paid — `:146-181`.
- **Table**: invoice rows; `keepPreviousData` query `:101`+; navigate to `/billing/invoices/:invoiceId` on row click `:167` (seen in topups ref); filters area (tenant/status) `:146+` area.
- **States**: shared ErrorBand/LoadingRow via `@/components/list` (import lines 30+).

### 2.10 `/billing/invoices/:invoiceId` — Invoice detail (`src/pages/billing/invoice-detail.tsx:73`)
- **Actions (mutations)**: download PDF `:100`, issue `:105`, mark paid `:115`, void `:124`.
- **Line items section** `:208`; issue form fields (due date) `:266`; void reason `:316`; notes `:348`; KPI-style header `:156`.
- **Forms**: issue/void use inline `Field` inputs (lib `Field`), not RHF.

### 2.11 `/billing/topups` — Top-ups (`src/pages/billing/topups-list.tsx:96`, 472 lines; l.1-80 viewed)
- **KPI strip**: Page requests / Pending / Requested — `:206-224`.
- **Filters**: tenant text input `:248-254`, status Select (Pending default) `:135-140`; status options `STATUSES` `:47`.
- **Workflow**: approve/reject mutations `:158-178`; decision ConfirmDialog `:422` area; onApprove navigates to generated invoice `:167`.
- **Realtime**: none (polls on demand).
- **Permission gating**: import `BillingPermissions` `:43` (in-page usage beyond visible lines).

### 2.12 `/impersonation` — Impersonation (`src/pages/impersonation/list.tsx:38`, 361 lines; l.1-160 viewed)
- **Real-time**: `refetchInterval: 5000ms` polling `:29,54`; refetchOnWindowFocus `:55`.
- **KPI strip** (4): Active/Ended/Revoked/Expired counts derived client-side `:60-68,95-100`.
- **Filter**: status Select `:102-110` (Active/Ended/Revoked/Expired `:31-36`).
- **List rows**: `GrantRow` `:139-158` with revoke (perm `Impersonation.Revoke` `:40` + Active only `:143`) and reopen (actor-only + `Users.Impersonate` `:150-155`).
- **Dialogs**: `ImpersonateDialog` `:23`; `RevokeGrantDialog` `:24`.
- **States**: ErrorBand `:112-120`, LoadingRow `:122`, EmptyState `:124-135`.
- **Permission gating**: in-page `canRevoke`/`canImpersonate` via `user.permissions.includes` `:40-41`.

### 2.13 `/audits` — Audits (`src/pages/audits/list.tsx:40`; grep evidence)
- **KPI**: `getAuditSummary` query `:88-90` → summary strip `:131` area.
- **Table**: rows w/ severity dot `:325`; filters (list has FilterBar — import lines); no matches EmptyState `:225`.
- **Detail**: `AuditDetailSheet` side-sheet (Radix Sheet) opened by selecting a row `:265`; sheet impl `src/pages/audits/detail.tsx:36-48` with `getAudit` fetch `:64-66`.

### 2.14 `/webhooks` — Webhooks (`src/pages/webhooks/list.tsx:39`, 260 lines; l.1-100 viewed)
- **List**: subscription rows w/ Active/Inactive badge `:206`, endpoint URL, event chips; PAGE_SIZE 25 `:37`; `keepPreviousData` `:49`.
- **Actions**: test subscription (toast success/warning on result) `:52-68`; delete `:70-79` (invalidate list).
- **Create**: `CreateWebhookDialog` `:33`.
- **Pagination**: shared `Pagination` comp `:31`.
- **States**: ErrorBand/LoadingRow `:28-31`, EmptyState `:124-125`.
- **Permission gating**: route guard `Subscriptions.View` only.

### 2.15 `/webhooks/:id` — Webhook detail (`src/pages/webhooks/detail.tsx:42`; grep evidence)
- **Sections**: Endpoint (URL/active/secret read-only) `:120-155`; Events chips `:159-170`; Deliveries attempt list `:173-225` w/ pagination (`Pagination` props `:210-219`).
- **Action**: test button → refetch deliveries `:69-71`.
- **Delivery rows**: success/failure icon, eventType chip, HTTP status, attemptCount, timestamp `:232-252`.
- **States**: ErrorBand `:192-193`, LoadingRow `:194-195`, empty `:196-199`.

### 2.16 `/notifications` — Notifications inbox (`src/pages/notifications/inbox.tsx:32`, 204 lines; l.1-150 viewed)
- **Real-time**: `useRealtimeEvent("NotificationCreated")` → invalidate inbox `:44-46` (SignalR).
- **Filter**: unread/all Select `:27-30,96-102`.
- **Actions**: mark-one-read `:48-52`, mark-all-read `:54-61`; row component `:141+`.
- **States**: ErrorBand `:105-113`, LoadingRow `:115`, EmptyState "inbox zero" `:117-128`.
- **No pagination** (pageSize 100 `:39`).

### 2.17 `/health` — Health (`src/pages/health/page.tsx:12`, 285 lines)
- **Real-time**: `refetchInterval: 10s` on liveness+readiness `:10,16,24`.
- **KPI strip** (4): Liveness, Readiness, Checks healthy, Checks failing `:66-95`.
- **Detail lists**: ProbeSection per endpoint `:110-124`; per-check rows w/ expandable details `:178-243`; status dots/badges/glyphs `:245-285`.
- **States**: ErrorBands per probe `:97-108`; "Probing…" `:156`; no-checks `:157-166`.
- **No permission gating** (route has none); refresh button `:60-63`.

### 2.18 `/settings/profile` — Profile (`src/pages/settings/profile.tsx:21`, 155 lines; l.1-100 viewed)
- **Avatar upload**: `ImageInput` + presigned flow, ownerType "User" `:68-74`; mutation `setProfileImage` `:25-38`.
- **Identity**: read-only fields (username/display name/email) `:77-100` — explicitly NOT editable (`:11-20` comment).
- **Forms**: none editable (server has no update-me endpoint).
- **States**: LoadingRow `:40`, ErrorBand `:41-51`.

### 2.19 `/settings/security` — Security (`src/pages/settings/security.tsx`, 586 lines; l.100-219 + l.391-510 viewed)
- **Change password dialog**: RHF + zodResolver (`useForm` `:161-169`, schema imported locally), RevealInput `:102-126`, mutation `:176-196`, dialog `:202+`.
- **2FA enable**: Begin mutation `:391-401`; QR display via `qrcode` lib inline SVG `:405-414`; manual shared-key copy `:421-446`; TOTP 6-digit verify `:448-483`.
- **2FA disable**: `TwoFactorDisable` `:492-510` — password verification then disable.
- **MFA toggles**: (beyond viewed lines) email/phone/device MFA sections present per description `:31` (`Security` tab: "Password and two-factor auth").

### 2.20 `/settings/sessions` — Sessions (`src/pages/settings/sessions.tsx:24`, 242 lines; l.1-80 viewed)
- **List**: `getMySessions`, sorted active-first `:27-32`; current-session badge; per-row revoke with busy Set tracking `:36-52`; revoke-all mutation `:54-63`.
- **States**: LoadingRow `:65`, ErrorBand `:66-75`.
- **No forms/validation**; no realtime (staleTime 15s `:29`).

### 2.21 `/settings/appearance` — Appearance (`src/pages/settings/appearance.tsx`, 109 lines; l.96-109 viewed)
- **Theme**: light/dark segmented control (light/dark only, no system) — `:96` area (first 95 lines contain the theme toggle UI).
- **Density**: disabled "Compact rows · coming soon" placeholder button `:97-106` (deliberate parity gap vs dashboard).

### 2.22 Auth pages
- **Login** (`src/pages/login.tsx:36`, 308+ lines): tenant field defaulting to `env.defaultTenant` (`:44`, env `src/env.ts:55`), email, password w/ show/hide `:48`; submit `performLogin` `:62-80` redirects to `from` or `/` `:67`; dev demo picker `:308` (data `src/pages/login.demo-accounts.ts:26-33`, dialog `src/components/auth/demo-accounts-dialog.tsx:28`); forgot-password link `:214`; no 2FA challenge step on login page (2FA is enforced at API/token level).
- **Forgot password** (`src/pages/auth/forgot-password.tsx:30`): email + tenant, success state with instructions `:116-128`, no captcha; plain form (no RHF/zod).
- **Reset password** (`src/pages/auth/reset-password.tsx:44`): reads query params `:50-52`; password + confirm w/ strength meter (`STRENGTH_META` `:38-41`, bar `:221-233`); success toast + navigate `/login` `:66-69`.
- **Confirm email** (`src/pages/auth/confirm-email.tsx:21`): fires `confirmEmail` on mount `:40`; loading / success / malformed-link states `:28-62`; link mentions re-request flow `:170-177`.

### 2.23 404 (`src/pages/not-found.tsx:12`) and RouteError (`src/components/route-error.tsx:11`)
- 404: centered card, requested path display `:46-54`, "Back home" link, go-back `:67-74`.
- RouteError: console-styled `// SYSTEM RESPONSE`, status code headline `:31-34`, collapsible stack `<details>` `:40-49`, reload/return buttons `:51-58`.

---

## 3. Layout & chrome

### Sidebar (`src/components/layout/sidebar.tsx`, 423 lines; l.1-150 viewed + NavBody grep)
- **Sections accordion**: `SidebarNavBody` `:170+`; single-select accordion `openSection` state `:55-64`; `AccordionSection` `:266+` w/ chevron `:304`; items `NavLinkRow` `:359+`.
- **Top/bottom singletons**: Overview (`/`) + Settings (`/settings`) — `src/components/layout/nav-items.ts:41-47`; rendered at `sidebar.tsx:195` (topNavTop) and `:252` (topNavBottom) per grep.
- **Collapsed mode**: persisted `localStorage` key `fsh.admin.sidebar.collapsed` `:16`; 52px icons `:74`; brand shrink `:77-123`; collapse/expand buttons `:106-122` and footer `:133-150`; collapsed flat icon stack w/ tooltip `:373-406`.
- **Permission visibility**: `granted` = `user.permissions` when hydrated; `filterNavSpec` drops unpermitted items and empty sections `:44-53`.
- **Brand**: "fullstackhero Admin" wordmark + `F` tile `:85-102`.

### Topbar (`src/components/layout/topbar.tsx`, 359 lines; l.147-359 viewed)
- **Notification bell**: `NotificationBell` `:184`.
- **User dropdown**: avatar (profile image or initials) `:201-206`, display name + tenant `:208-215`; menu = user info header `:229-249`, Theme section (Light/Dark via `ThemeMenuItem` `:257-270`), Account quick actions (Profile `:279-283`, Settings `:284-288`), Sign out w/ confirmation dialog `:294-303` + dialog `:307-356`.
- **Mobile**: `MobileNavTrigger` `:178`.
- **Theme toggle**: lives INSIDE the user dropdown (not a standalone topbar icon).
- **No command palette (Ctrl+K)** — grep across `src` for `cmdk`/`CommandDialog`/`palette` finds only Tailwind theme palette tokens (`tenant-branding-card.tsx`) and the ThemePreview palette — **none present**.

### Mobile nav (`src/components/layout/mobile-nav.tsx:63`)
- Sheet drawer (`MobileNavRoot`) using same `sections` nav spec + `filterNavSpec` `:19,71`; provider `:51`; trigger in topbar `:144+`.

### App shell (`src/components/layout/app-shell.tsx:24-70`)
- Skip-to-content link `:28-33`; sidebar + topbar + scrollable main `:35-62`; Suspense boundary for lazy chunks `:47-58`; `MobileNavRoot` `:65`; `InactivityGuard` `:68`.
- **No ImpersonationBanner in admin** — comment explicit: "admin is the operator surface so it doesn't impersonate itself" `:20-22`.

### Inactivity guard/dialog (`src/components/auth/inactivity-guard.tsx:15`, `inactivity-dialog.tsx:24`)
- Idle warning w/ SVG countdown ring, tone shifting `:40-94`; thresholds from runtime config `src/env.ts:10-17,39-40` (10 min idle, 60 s warning).
- **No command palette, no announcements/activity feed in admin.**

---

## 4. Theming

- **Theme provider**: `src/components/theme/theme-provider.tsx` — `STORAGE_KEY = "fsh.admin.theme"` `:11`; resolve order storage → system pref → dark `:14-20`; applies `.dark` class + `colorScheme` on `<html>` `:28-31`; exposes `theme`/`setTheme`/`toggle` `:5-9,34-41`; no accent/font/density settings in provider.
- **Theme persistence**: `localStorage` key `fsh.admin.theme` (`theme-provider.tsx:11`); sidebar collapsed key `fsh.admin.sidebar.collapsed` (`sidebar.tsx:16`); permissions cache key `fsh.admin.permissions` (`auth/token-store.ts:4`).
- **Accent / font / density settings**: admin exposes only **Light/Dark** (topbar dropdown `topbar.tsx:253-270`; settings Appearance page `settings/appearance.tsx`). Density is a disabled "coming soon" button `appearance.tsx:97-106`. **No user-facing accent/radius/font selector in admin** (tenant theming exists per-tenant in `TenantBrandingCard` `src/components/tenants/tenant-branding-card.tsx` — palettes `lightPalette` `:164`, logo urls `:314-330`).
- **Styling system**: Tailwind v4 + shadcn-style `@/components/ui` components; CSS vars `--color-*` in `oklch()` — pervasive (e.g. `topbar.tsx:173`, `users/list.tsx:340`). Toasts styled via `.fsh-toast` classes (`App.tsx:40-76`).

---

## 5. Auth flows

- **Login**: `LoginPage` → `AuthContext.login` (`src/auth/auth-context.tsx:34`) → `issueToken` (`src/auth/api`) → tokens to `tokenStore`; redirect to `from` (`login.tsx:62-80`). Demo accounts gated `import.meta.env.DEV` (`demo-accounts-dialog.tsx:18`).
- **Boot session restore**: `readStoredSession` treats expired access token as unusable; silent `refreshAccessToken()` at boot (`auth-context.tsx:58-61,73-103`); tokenStore subscription rebuilds user `:137-140`.
- **Permissions hydration**: `getMyPermissions()` after sign-in (`auth-context.tsx:107-135`); `permissionsHydrated` guard in `RouteGuard` (`route-guard.tsx:29-39`) prevents 403 flash.
- **Permission enforcement model**: JWT carries only role names; permissions fetch is separate (`auth-context.tsx:21-24`, `src/api/users.ts:60-65`).
- **ProtectedRoute** (`src/auth/protected-route.tsx:14`): auth-vs-anonymous; 403 style for explicit permission props `:41-43`.
- **RouteGuard** (`src/auth/route-guard.tsx:26-49`): per-route `perms`; renders `ForbiddenView` with missing perms list (`src/components/forbidden-view.tsx:15`).
- **Forgot/reset/confirm**: covered in §2.22. No password-strength API beyond client-side `scorePassword` (`reset-password.tsx:60`).
- **2FA**: enrollment UI in `settings/security.tsx` (QR `:405-414`, manual key `:421-446`, TOTP verify `:448-483`, disable w/ password `:492-510`). **No 2FA challenge step in the login page flow** — admin login form has no TOTP field (`login.tsx` fields = tenant/email/password only).
- **Impersonation**: admin **issues** impersonation tokens (never accepts them). Start = `ImpersonateDialog` (`impersonate-dialog.tsx:50`) two-step pick-user → reason+duration (`DURATION_OPTIONS` 10/15/30min `:34-38`); on success opens **dashboard** in a new tab with token in URL hash (`:45-49` comment, `env.dashboardUrl` `src/env.ts:38`, `:277` toast "Opened the dashboard as …"). Revoke = `RevokeGrantDialog` (`revoke-grant-dialog.tsx:36`, shows impersonating/started-by/reason/expires `:135-157`). **No impersonation banner/terminal pages in admin** (`app-shell.tsx:20-22` comment — intentionally absent).
- **Sign-out**: `logout()` + confirm dialog (`topbar.tsx:164-167,307-356`); inactivity auto-logout (`inactivity-guard.tsx`).

---

## Route summary table (one line per route)

| Route | Page file (src/) | Present | Feature notes |
|---|---|---|---|
| `/login` | `pages/login.tsx` | YES | tenant+email+pwd, demo picker, redirect, NO 2FA challenge |
| `/forgot-password` | `pages/auth/forgot-password.tsx` | YES | email+tenant → one-time link |
| `/reset-password` | `pages/auth/reset-password.tsx` | YES | token from URL, strength meter |
| `/confirm-email` | `pages/auth/confirm-email.tsx` | YES | auto-fire confirmEmail, 3 states |
| `/` | `pages/dashboard.tsx` | YES | tenant KPI + pivot cards |
| `/tenants` | `pages/tenants/list.tsx` | YES | table+cards, search, status filter, create dialog, pagination |
| `/tenants/new` | redirect → `/tenants` | stub | bookmark redirect |
| `/tenants/:id` | `pages/tenants/detail.tsx` | YES | hero, provisioning poll, activate, renew/validity dialogs, branding, active grants |
| `/users` | `pages/users/list.tsx` | YES | search+3 filters, table+cards, create dialog, pagination |
| `/users/new` | redirect → `/users` | stub | bookmark redirect |
| `/users/:id` | `pages/users/detail.tsx` | YES | identity card, roles editor, status toggle, sessions card |
| `/roles` | `pages/roles/list.tsx` | YES | client search, system-first sort, create dialog |
| `/roles/new` | redirect → `/roles` | stub | bookmark redirect |
| `/roles/:id` | `pages/roles/detail.tsx` | YES | RHF+zod profile, permission groups, system lock, delete |
| `/billing` | `pages/billing/layout.tsx` | shell | tabbed shell; index → invoices |
| `/billing/plans` | `pages/billing/plans-list.tsx` | YES | plan cards, KPI strip, PlanFormDialog |
| `/billing/invoices` | `pages/billing/invoices-list.tsx` | YES | invoice table, 4-stat KPI strip |
| `/billing/invoices/:invoiceId` | `pages/billing/invoice-detail.tsx` | YES | line items, download/issue/paid/void |
| `/billing/topups` | `pages/billing/topups-list.tsx` | YES | approve/reject → invoice workflow, KPI strip, filters |
| `/impersonation` | `pages/impersonation/list.tsx` | YES | grants table, 4-stat strip, 5s poll, revoke/reopen |
| `/audits` | `pages/audits/list.tsx` | YES | summary strip, filters, detail side-sheet |
| `/audits/:id` | redirect → `/audits` | stub | sidebar-sheet model |
| `/webhooks` | `pages/webhooks/list.tsx` | YES | subscriptions, test/delete, create dialog |
| `/webhooks/:id` | `pages/webhooks/detail.tsx` | YES | endpoint/events + deliveries pagination |
| `/notifications` | `pages/notifications/inbox.tsx` | YES | SignalR live, unread filter, mark read/all |
| `/health` | `pages/health/page.tsx` | YES | 10s poll, 4-stat strip, expandable checks |
| `/settings` | `pages/settings/layout.tsx` | shell | numbered rail; index → profile |
| `/settings/profile` | `pages/settings/profile.tsx` | YES | avatar presigned upload, read-only identity |
| `/settings/security` | `pages/settings/security.tsx` | YES | change pwd dialog, 2FA QR/TOTP enable+disable |
| `/settings/sessions` | `pages/settings/sessions.tsx` | YES | session list, revoke one/all |
| `/settings/appearance` | `pages/settings/appearance.tsx` | YES | light/dark only; density disabled stub |
| `*` | `pages/not-found.tsx` | YES | styled 404 + GoBack |

**Totals: 31 path entries (26 renderable + 4 redirect stubs + 1 catch-all), 20 feature pages, 2 lazy shell layouts (Billing, Settings), 4 public auth pages.**

---

### Key parity-relevant findings (unusual / gaps)
1. **No command palette** (Ctrl+K) anywhere in admin — grep for `cmdk`/command found nothing.
2. **No impersonation banner / terminal pages in admin** — admin only issues/revokes grants; the impersonated session lives in the dashboard app (documented in `app-shell.tsx:20-22`).
3. **Login has no 2FA challenge field** — 2FA enrollment exists under Settings; challenge presumably handled at the API/token layer or in the dashboard app.
4. **Creation routes are redirect stubs** — `/tenants/new`, `/users/new`, `/roles/new` all `<Navigate>` since creation moved into dialogs.
5. **Audit detail is a side sheet, not a route** (`/audits/:id` redirects back).
6. **Admin theming is minimal** — Light/Dark only; accent/font/density are not user-settable (density explicitly "coming soon").
7. **Real-time surface**: one SignalR event (`NotificationCreated`, `realtime-context.tsx:90`); other live data is polling (impersonation 5s, tenant provisioning 2s, health 10s, bell unread 60s).
8. **Form/validation stack**: `react-hook-form` + `zodResolver` used in role profile, change-password, all create dialogs; **PlanFormDialog deliberately uses controlled inputs + manual zod `fieldError`** instead of RHF.
9. **Toast system**: sonner `Toaster` themed from `ThemeProvider` (`App.tsx:48-77`), `.fsh-toast` classes.