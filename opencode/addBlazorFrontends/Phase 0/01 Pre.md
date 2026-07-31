# Adding Blazor WASM front-ends

```Prompt
I'm planning to use this repository as the ground basis for developing my next web and cross-platform SaaS apps. I want build the existing react 19 front-ends alternatives by building the .NET 10 BLAZOR WASM front-ends (mobile friendly for tablets and phones through browser) and the required cross-platform apps later using .NET 10/11 MAUI. 

Give me the complete requirements (PRD or other relevant docs), detail roadmap, milestones, and AI-friendly multi tasks/steps (any production-grade best practices md files to feed the ai agents) to achieve the goals along with the complete guidelines for any developer to drive any AI agents to complete this project. The repository itself is considered to be ai-friendly, and there is also another repository for the agent-skills; https://github.com/dotnet/skills for your consideration.
```

---

I now have a thorough understanding of the codebase, the existing agent framework, and the dotnet/skills ecosystem. Here is the complete plan.

---

# FullStackHero Blazor WASM + MAUI — Complete Implementation Plan

## 1. Architecture Decisions (Confirmed)

| Decision | Choice | Rationale |
|---|---|---|
| Component library | **MudBlazor** | Material Design 3, mobile-responsive, rich DataGrid/forms/dialogs/navigation. MAUI-friendly via BlazorWebView. |
| Auth pattern | **AuthenticationStateProvider + JSInterop** | Mirrors React's `tokenStore` pattern. Tokens in localStorage via JS interop; `DelegatingHandler` for automatic header injection; server permissions via `IAuthorizationService`. |
| Project location | `clients/admin-blazor` + `clients/dashboard-blazor` | Alongside existing React apps at same level. Clear naming. No path collisions. |
| MAUI strategy | **MAUI Blazor Hybrid App** | Single MAUI project hosting Blazor WebView. Shared project (`Clients/BlazorShared`) between WASM and Hybrid for maximum code reuse. |
| State management | **MudBlazor + DI services** | MudBlazor handles local component state; scoped services with event callbacks for cross-component; no Fluxor needed initially (re-evaluate at Phase 4 for MAUI). |
| Testing | **bUnit** (unit) + **Playwright** (e2e) | bUnit for component/handler tests alongside existing xUnit. Playwright for e2e — can share test infrastructure with React tests. |
| Render mode | **Standalone WASM** | Not Server/Auto. Fits existing architecture where backend is pure API. WASM app is deployed as static files (like the React apps). |

---

## 2. PRD — Product Requirements Document

### 2.1 Admin Blazor App (`clients/admin-blazor`)

**Purpose:** Operator/SuperAdmin console — pixel-functional parity with `clients/admin` (React).

**Must-have pages & features (Phase 2):**

| Area | Pages | MudBlazor Components | React Source for reference |
|---|---|---|---|
| Auth | Login, ForgotPassword, ResetPassword, ConfirmEmail | MudTextField, MudButton, MudAlert, MudCard | `src/pages/auth/*` |
| Dashboard | Overview cards, stats | MudGrid, MudCard, MudChart | `src/pages/dashboard/` |
| Tenants | List, Create, Detail, Edit, Upgrade | MudTable, MudDialog, MudForm | `src/pages/tenants/` |
| Users | List, Create, Detail, Edit, Roles editor | MudTable, MudDialog, MudChip | `src/pages/identity/` |
| Roles | List, Create, Edit, Permission editor | MudTable, MudTreeView, MudSwitch | `src/pages/identity/` |
| Billing | Plans, Subscriptions, Invoices | MudTable, MudCard | `src/pages/billing/` |
| Webhooks | List, Create, Detail | MudTable, MudDialog | `src/pages/webhooks/` |
| Audits | List, Detail | MudTable | `src/pages/audits/` |
| Notifications | List, Mark read | MudTable, MudBadge | `src/pages/notifications/` |
| Health | System health dashboard | MudCard, MudProgress | `src/pages/health/` |
| Settings | Profile, Theme, Sessions | MudForm | `src/pages/settings/` |
| Impersonation | Begin session, List active | MudTable | `src/pages/impersonation/` |

**Key behaviors to replicate:**
- `X-FSH-App: admin` header on login
- localStorage namespace `fsh.admin.*`
- Permissions fetched from `GET /api/v1/identity/permissions`, cached, gated at route level
- `RouteGuard` equivalent via `AuthorizeView` + custom `PermissionAuthorizer`
- Sidebar nav gated by permissions
- 10-min inactivity timeout with 60s warning dialog
- SignalR for real-time notifications
- Cross-tab logout via `storage` event

### 2.2 Dashboard Blazor App (`clients/dashboard-blazor`)

**Purpose:** Tenant-facing app — pixel-functional parity with `clients/dashboard` (React).

**Must-have pages & features (Phase 3):**

| Area | Pages | Notes |
|---|---|---|
| Auth | Login, ForgotPassword, etc. | `X-FSH-App: dashboard` header; rejected for root tenant |
| Overview | Tenant dashboard, activity feed | SSE real-time feed |
| Activity | Activity timeline | Virtualized list |
| Subscription | Plan details, usage, upgrade | MudCard, MudProgress |
| Wallet | Top-up, balance, transactions | MudTable |
| Invoices | List, Detail | MudTable |
| Catalog | Brands, Categories, Products | MudTable, image preview |
| Identity | Profile, Users, Roles | |
| Tickets | List, Detail, Create, Chat | SignalR real-time |
| Chat | Channel list, Messages | SignalR, virtualized |
| Files | File manager, Upload, Presigned URLs | MudUpload, S3 integration |
| System | Health, Audit, Trash, Sessions | |
| Settings | Profile, Theme accent switcher | MudForm |

**Key divergences from admin:**
- No per-route `RouteGuard` — flatter permission model
- SSE support (Server-Sent Events)
- Impersonation flow (token stashing, `act_sub`/`act_tenant` claims)
- Terminal pages: `/tenant-deactivated`, `/impersonation-ended`
- Accent color swappable (rose/indigo/violet/sky/emerald/amber)
- 20-min inactivity timeout
- Command palette (cmdk equivalent) — can be MudAutocomplete or custom
- Chat page with SignalR integration

### 2.3 MAUI Blazor Hybrid App

**Purpose:** Cross-platform native apps (iOS, Android, Windows, macOS) sharing code with the WASM apps.

**Key features:**
- BlazorWebView hosting the Blazor WASM components via a `Clients/BlazorShared` project
- Native navigation shell (MudBlazor Navigation + MAUI Shell)
- Push notifications (native Firebase/APNs)
- Biometric auth (Face ID / Fingerprint)
- Camera access for file uploads
- Offline queue with background sync
- Deep linking (universal links / app links)
- In-app purchases for subscriptions (StoreKit/Google Play)
- Native file picker (replaces browser upload)
- Status bar/safe area handling

### 2.4 Shared Infrastructure (`Clients/BlazorShared`)

A Razor Class Library (RCL) shared between WASM and MAUI Hybrid:

- **HttpClient pipeline** — `AuthDelegatingHandler` (JWT header + tenant header + 401 refresh), `RetryDelegatingHandler`
- **Authentication services** — `ITokenStore`, `IAuthService`, `AuthenticationStateProvider`
- **Models/DTOs** — Shared `PagedResult<T>`, request/response types
- **MudBlazor theme** — FSH brand tokens as MudTheme
- **Common components** — `FshPageHeader`, `FshTable`, `FshForm`, `FshFilterBar`, `FshPagination`, `FshConfirmDialog`, `FshErrorBoundary`, `FshPermissionGate`
- **API service classes** — Per-module typed services (mirroring `src/api/*.ts`)
- **SignalR client** — Shared `HubConnection` management
- **SSE client** — Shared `SseClient` (for dashboard)
- **Runtime config** — Same pattern: load `/config.json` at startup
- **Permission constants** — C# equivalents of `permissions.ts` frozen objects

---

## 3. Project Structure & File Manifest

```
clients/
├── admin-blazor/
│   ├── FSH.Admin.Wasm/                       # Blazor WASM app
│   │   ├── Pages/
│   │   │   ├── Auth/                         # Login, ForgotPassword, etc.
│   │   │   ├── Dashboard/
│   │   │   ├── Tenants/
│   │   │   ├── Identity/                     # Users, Roles
│   │   │   ├── Billing/
│   │   │   ├── Webhooks/
│   │   │   ├── Audits/
│   │   │   ├── Notifications/
│   │   │   ├── Health/
│   │   │   ├── Settings/
│   │   │   └── Impersonation/
│   │   ├── Shared/                           # App-specific shared components
│   │   ├── Auth/                             # Admin AuthStateProvider, token store
│   │   ├── wwwroot/
│   │   │   ├── config.json
│   │   │   └── appsettings.json
│   │   ├── Program.cs
│   │   └── FSH.Admin.Wasm.csproj
│   └── FSH.Admin.Wasm.Tests/                 # bUnit + Playwright tests
│       └── Pages/
│
├── dashboard-blazor/
│   ├── FSH.Dashboard.Wasm/
│   │   ├── Pages/
│   │   │   ├── Auth/
│   │   │   ├── Overview/
│   │   │   ├── Activity/
│   │   │   ├── Subscription/
│   │   │   ├── Wallet/
│   │   │   ├── Invoices/
│   │   │   ├── Catalog/
│   │   │   ├── Identity/
│   │   │   ├── Tickets/
│   │   │   ├── Chat/
│   │   │   ├── Files/
│   │   │   ├── System/
│   │   │   └── Settings/
│   │   ├── Auth/
│   │   ├── Sse/
│   │   ├── wwwroot/
│   │   └── Program.cs
│   └── FSH.Dashboard.Wasm.Tests/
│
├── BlazorShared/                             # Razor Class Library
│   ├── Auth/                                 # TokenStore, AuthStateProvider
│   ├── Components/                           # FshTable, FshPageHeader, etc.
│   ├── Services/                             # Per-module API service classes
│   ├── Models/                               # DTOs, PagedResult<T>
│   ├── Infrastructure/                       # DelegatingHandlers, HttpClient factory
│   ├── Realtime/                             # SignalR hub connection
│   ├── Sse/                                  # SSE client (dashboard)
│   ├── Theming/                              # MudTheme with FSH tokens
│   ├── Permissions/                          # Permission constants
│   ├── wwwroot/                              # Static assets
│   └── FSH.BlazorShared.csproj
│
└── FSH.Hybrid/                               # MAUI Blazor Hybrid
    ├── FSH.Hybrid/
    │   ├── Platforms/
    │   │   ├── Android/
    │   │   ├── iOS/
    │   │   ├── MacCatalyst/
    │   │   └── Windows/
    │   ├── Services/                         # Native: PushNotification, Biometric, Camera
    │   ├── Pages/                            # MAUI Shell pages
    │   ├── App.xaml / App.xaml.cs
    │   ├── MauiProgram.cs
    │   └── FSH.Hybrid.csproj
    └── FSH.Hybrid.Tests/
```

---

## 4. Detailed Roadmap & Milestones

### Phase 0: Foundation & Tooling (Week 1)

| Task | Deliverable | Key Files |
|---|---|---|
| Scaffold `FSH.BlazorShared` RCL | Project creates, targets `net10.0`, adds MudBlazor NuGet | `clients/BlazorShared/FSH.BlazorShared.csproj` |
| Scaffold `FSH.Admin.Wasm` | Standalone WASM app, references BlazorShared | `clients/admin-blazor/FSH.Admin.Wasm/` |
| Scaffold `FSH.Dashboard.Wasm` | Standalone WASM app, references BlazorShared | `clients/dashboard-blazor/FSH.Dashboard.Wasm/` |
| Add solution entries | Update `FSH.Starter.slnx` with new Blazor projects | `src/FSH.Starter.slnx` |
| AppHost integration | Add Blazor WASM apps to Aspire orchestration | `src/Host/FSH.Starter.AppHost/AppHost.cs` |
| Create `.agents/rules/frontend/blazor-shared.md` | Agent guide for shared Blazor conventions | `.agents/rules/frontend/blazor-shared.md` |
| Create `.agents/rules/frontend/blazor-admin.md` | Agent guide for admin Blazor app | `.agents/rules/frontend/blazor-admin.md` |
| Create `.agents/rules/frontend/blazor-dashboard.md` | Agent guide for dashboard Blazor app | `.agents/rules/frontend/blazor-dashboard.md` |
| Create `.agents/skills/add-blazor-page/SKILL.md` | Agent skill for adding Blazor pages | `.agents/skills/add-blazor-page/SKILL.md` |

**Key NuGet packages to add:**
- `MudBlazor` (or `MudBlazor.Wasm` for standalone) — all Blazor projects
- `Microsoft.AspNetCore.Components.Authorization` — WASM projects
- `Microsoft.AspNetCore.SignalR.Client` — BlazorShared
- `Microsoft.Extensions.Http` — BlazorShared
- `bUnit` + `bUnit.web` — test projects
- `Microsoft.Playwright.NUnit` — e2e test projects

### Phase 1: Shared Infrastructure (Week 2-3)

| Task | Details | MudBlazor Equivalents |
|---|---|---|
| `ITokenStore` + JS interop impl | Mirror `token-store.ts` — localStorage via `IJSRuntime`. `ProtectedBrowserStorage` alternative considered but JS interop matches React pattern exactly. | N/A |
| `AuthDelegatingHandler` | HttpClient handler: injects `Authorization: Bearer`, `tenant` header. Handles 401 → refresh → retry. Single-flight refresh dedup identical to React's `refreshPromise` pattern. | N/A |
| `AuthStateProvider` | `AuthenticationStateProvider` implementation. Boot sequence: read token → decode JWT (System.IdentityModel.Tokens.Jwt) → validate expiry → set auth state → fetch permissions. Exposes `PermissionsHydrated` event. | N/A |
| `FshMudTheme` | FSH brand tokens: `MudTheme.Palette` with cool-cast neutrals (admin) / chroma-0 neutrals (dashboard) / chartreuse accent (admin) / swappable accent (dashboard) | `MudTheme` |
| `PagedResult<T>` | Generic type mirroring `PagedResponse<T>` from React | N/A |
| API service classes | Per-module typed services (IUserService, ITenantService, etc.) — mirroring `src/api/*.ts` functions | N/A |
| `FshPageHeader` | Title + icon + description + action button — mirror of `EntityPageHeader` | `MudIcon`, `MudText`, `MudButton` |
| `FshTable` / `FshListPage` | Generic table with sort/filter/pagination — mirror of CSS-grid table pattern | `MudTable`, `MudTableSortLabel`, `MudSelect` for filter |
| `FshFilterBar` | Search + segmented toggle + selects | `MudTextField`, `MudToggleGroup`, `MudSelect` |
| `FshPagination` | Page controls — mirror of React's Pagination component | `MudTable` has built-in; custom `MudButton` |
| `FshConfirmDialog` | Confirm/cancel dialog | `MudDialog` |
| `FshErrorBand` | Error state display | `MudAlert` |
| `FshForm` / `FshFormSection` | Form layout with section cards — mirror of `FormShell`, `FormSection`, `FormActions` | `MudCard`, `MudForm` |
| `FshPermissionGate` | Blazor `AuthorizeView`-style component checking specific permissions | `MudChip` for role badges |
| Permission constants | C# static classes mirroring `permissions.ts` frozen objects | N/A |
| Runtime config service | Load `/config.json` at startup — mirror of `env.ts` | N/A |
| SignalR hub service | Shared `HubConnection` lifecycle mirroring `realtime-context.tsx` | N/A |
| SSE client service | `SseClient` for dashboard — mirror of dashboard's SSE | N/A |
| Inactivity timer service | Timer + warning dialog — mirror of `useInactivityTimeout` | `MudDialog` for warning |
| bUnit test helpers | Test context setup, mock `AuthenticationStateProvider`, mock `HttpClient` | N/A |
| Playwright test infrastructure | Shared helpers: `seedAuthedSession`, `mockJsonResponse`, `installShellMocks` | N/A |

### Phase 2: Admin Blazor App (Weeks 4-7)

**Week 4 — Auth & Shell:**
- Login page (MudCard, MudTextField, MudButton) with `X-FSH-App: admin` header
- ForgotPassword, ResetPassword, ConfirmEmail pages
- AppShell layout: MudDrawer (sidebar), MudAppBar (top bar), breadcrumbs
- Auth routing guard using `AuthenticationStateProvider` + `AuthorizeRouteView`
- ProtectedRoute + PermissionGate equivalent
- Sidebar nav with MudNavMenu, gated by permissions
- Inactivity timeout with warning dialog
- Cross-tab logout via `storage` JS event listener

**Week 5 — Identity Pages:**
- Users list: MudTable with search, filtering, pagination
- User create: MudDialog with MudForm
- User detail: MudCard sections for profile, roles, sessions
- Roles list + MudTable
- Role create/edit with permission editor (MudTreeView with checkboxes)
- Permission constants in C# mirroring `Permissions.Identity.*`, etc.
- Permission gate component integration

**Week 6 — Tenants, Audit, Webhooks:**
- Tenants list, create, detail, upgrade
- Tenant MudStepper for provisioning wizard
- Audit trail list with MudTable, filter by entity/resource
- Audit detail MudDialog with JSON viewer
- Webhooks list, create (MudSelect for events), detail with delivery log
- Webhook test button with MudAlert for success/failure

**Week 7 — Billing, Notifications, Health, Settings:**
- Billing plans list (MudCard grid), subscription detail
- Invoices list with MudTable, MudChip for status badges
- Notifications list with MudBadge, mark-read button
- Notification real-time via SignalR (toast via MudAlert or snackbar)
- Health dashboard: MudCard grid with server status, MudProgress bars
- Settings pages: profile edit, theme toggle, active sessions list
- Impersonation list: active impersonation sessions with end button

### Phase 3: Dashboard Blazor App (Weeks 8-11)

**Week 8 — Auth & Shell:**
- Login page with `X-FSH-App: dashboard` header
- AppShell: MudDrawer, MudAppBar, command palette (MudAutocomplete as quick-nav)
- Impersonation boot: detect `act_sub`/`act_tenant` claims from JWT
- Terminal pages: TenantDeactivated, ImpersonationEnded (outside AppShell)
- SSE client integration for Overview real-time feed
- Same inactivity timer (20-min threshold, 60s warning)

**Week 9 — Overview, Activity, Subscription, Wallet:**
- Overview dashboard: MudGrid of MudCard with live SSE-updated stats
- Activity timeline: virtualized list (MudTable with infinite scroll)
- Subscription detail: plan name, features (MudList), usage bars (MudProgressLinear)
- Wallet: balance display, top-up MudDialog, transaction list (MudTable)

**Week 10 — Catalog, Invoices, Identity:**
- Product list: MudTable with thumbnail, brand/category filters, price formatting
- Product detail: MudCard, image gallery (MudCarousel), specs table
- Brands/Categories: MudTable with MudChip
- Identity: user list, role list (with permission viewer, not editor)
- Invoice list: MudTable with filter by status/date, download button
- Invoice detail: MudCard, PDF preview (iframe or MudFileDisplay)

**Week 11 — Tickets, Chat, Files, System:**
- Ticket list: MudTable with search, filter by status/priority
- Ticket detail: MudCard thread with MudTextField reply
- Chat: channel list (MudNavMenu), message list (virtualized MudList), MudTextField send — all via SignalR
- File manager: MudTable file list, MudUploadDragAndDrop, presigned URL generation
- System pages: health, audit, trash, sessions — similar to admin but tenant-scoped

### Phase 4: MAUI Blazor Hybrid App (Weeks 12-14)

**Week 12 — MAUI Project Scaffold:**
- Create `FSH.Hybrid` MAUI Blazor Hybrid project
- Reference `BlazorShared` RCL
- Set up shell navigation (FlyoutItem + TabBar)
- Configure `MauiProgram.cs` with DI registration matching WASM bootstrap
- Build `MauiAuthenticationStateProvider` using MAUI's `SecureStorage` for tokens
- Platform-specific auth: biometric via `Platforms/Android/BiometricService.cs`, etc.

**Week 13 — Native Features:**
- Push notifications: Firebase (Android) + APNs (iOS) integration
- Offline queue: `SQLite` + `Microsoft.Maui.Storage` for background data
- Camera/file access: `MediaPicker` for photo/video uploads, `FilePicker` for documents
- Deep linking: register `fsh://` scheme + universal links
- Status bar/safe area: MAUI `SafeArea` layout adjustments
- In-app purchases: `Microsoft.Maui.StoreKit` (iOS) / `Google.BillingClient` (Android)

**Week 14 — Hybrid Shell Polish:**
- MAUI Shell with custom `FlyoutBehavior` (responsive: flyout on tablet, tab on phone)
- Platform-specific styling (Android Material 3 / iOS Human Interface)
- App lifecycle handling (foreground/background reconnect SignalR)
- Splash screen + app icon
- Certificate signing configuration (Android keystore, iOS provisioning)

### Phase 5: Testing, Polish & Perf (Weeks 15-16)

**Week 15 — bUnit Tests:**
- Every page: render test, permission gate test, loading/empty/error states
- Every form: validation test, submission test
- Every service: mocked HttpClient tests
- Auth flow: token refresh, permission hydration, expiry
- SignalR: hub connection events, notification received
- SSE: stream parsing, reconnection

**Week 16 — Playwright E2E + Perf:**
- Shared Playwright infrastructure with existing React test patterns
- Critical paths: login → navigate → list → detail → form submit
- Permission-based navigation gating
- Lazy loading verification (assembly splitting)
- WASM bundle size optimization (tree-shake MudBlazor, trim unused)
- AOT compilation on/off benchmarking
- MAUI: UI test infrastructure (AppCenter/Xamarin.UITest or MAUI UITest)

---

## 5. AI Agent Skills & Guidelines (Following dotnet/skills format)

### 5.1 Rules Files to Create

**`.agents/rules/frontend/blazor-shared.md`**

```
# Blazor — shared conventions

Applies to BOTH `clients/admin-blazor` and `clients/dashboard-blazor`.
Read this first, then the app-specific file.

Stack: .NET 10 · Blazor WASM (standalone) · MudBlazor 8.x · 
       `@microsoft/signalr` (via JS interop) · bUnit · Playwright.

## API client pattern

- `HttpClient` with `AuthDelegatingHandler` (registers via `AddHttpClient<IService, Service>()`).
- Types are hand-written C# records/classes mirroring the API JSON.
- Per-module typed service classes: `IUserService`, `ITenantService`, etc.
- `AuthDelegatingHandler`: injects Authorization + tenant headers, handles 401 → refresh → retry.
- Single-flight refresh: `refreshPromise` in a `SemaphoreSlim(1,1)` — identical dedup to React.
- Paged queries: `Task<PagedResult<T>> SearchAsync(SearchRequest request)`.

## Env / runtime config

- `IRuntimeConfigService` loads `/config.json` at startup via `HttpClient`.
- Called in `Program.cs` before `builder.Build()` — throws if config missing.

## Auth (CascadingAuthenticationState)

- Custom `AuthenticationStateProvider` wrapping `ITokenStore` + JWT decode.
- Boot sequence mirrors React: read token → decode → validate → set identity → fetch permissions.
- `PermissionsProvider` service: fetches `GET /identity/permissions`, caches in memory, exposes `GetPermissionsAsync()`.
- Components gate with `<AuthorizeView>` + `@attribute [Authorize(Policy = "Permissions.Users.View")]`.

## MudBlazor theming

- MudTheme defined in `BlazorShared/Theming/FshMudTheme.cs`.
- Dim/light modes via MudThemeProvider.
- Admin: `FshMudTheme.Admin` (cool-cast neutrals, chartreuse accent, Geist fonts).
- Dashboard: `FshMudTheme.Dashboard` (chroma-0 neutrals, swappable accent).

## Directory conventions

- Pages: `Pages/{Area}/{Feature}/{Feature}Page.razor` — named, not default.
- Services: `Services/I{Name}Service.cs` + `{Name}Service.cs`.
- Components: `Components/{Name}/{Name}.razor` + `{Name}.razor.cs`.
```

**`.agents/rules/frontend/blazor-admin.md`**

```
# Blazor — admin app (`clients/admin-blazor`)

Operator/SuperAdmin console. Read `blazor-shared.md` first.

## Forms — MudForm + DataAnnotations

- Use `MudForm` with `EditForm` + data annotations (or FluentValidation adapter).
- MudBlazor components: `MudTextField`, `MudSelect`, `MudDatePicker`, `MudNumericField`.
- Validation: annotate models with `[Required]`, `[StringLength]`, `[EmailAddress]`, etc.
- Submit pattern: `await form.Validate()` → service call → UI update.

## Permission gating (three layers)

1. **Auth guard at route level:** `<AuthorizeView>`
2. **Component-level gate:** `<FshPermissionGate Perms="Permissions.Users.View">`
3. **Nav item filter:** `INavService` provides permission-gated nav items.

## Routing

- `Microsoft.AspNetCore.Components.Routing.Router` with `@page` directives.
- `@attribute [Authorize]` on all protected pages.
- `AppShell` wraps `MudLayout` > `MudDrawer` + `MudMainContent` > `@Body`.

## Pages are feature-foldered, named

- `Pages/Identity/Users/UsersListPage.razor`
- `Pages/Identity/Users/UserDetailPage.razor`
```

**`.agents/rules/frontend/blazor-dashboard.md`**

```
# Blazor — dashboard app (`clients/dashboard-blazor`)

Tenant-facing app. Read `blazor-shared.md` first.

## No route-level permission gating

- Dashboard has flatter permission model. Use `FshPermissionGate` for component-level gating only.
- Server 403 is the backstop — no `permissionsHydrated` flash prevention needed.

## SSE integration

- `ISseClient` service in BlazorShared, wired in dashboard's `Program.cs` only.
- Consume via `@implements IDisposable` + `SseClient.OnMessage` subscription.

## Impersonation

- Detect impersonation from JWT claims: `act_sub`, `act_tenant`, `act_name`.
- BeginImpersonation/StopImpersonation with token stash/restore via `ITokenStore`.
- EndImpersonation endpoint mints fresh operator tokens.

## Terminal pages

- `Pages/Terminal/TenantDeactivated.razor` — redirect on 403 with tenant-deactivated problem.
- `Pages/Terminal/ImpersonationEnded.razor` — redirect on impersonation-revoked problem.
```

**`.agents/rules/frontend/maui-hybrid.md`**

```
# MAUI Blazor Hybrid — conventions

Read `blazor-shared.md` first — this file covers MAUI-only divergences.

## Stack

.NET 10 MAUI · BlazorWebView · BlazorShared RCL · MudBlazor · SQLite (offline).

## Project structure

- `FSH.Hybrid/FSH.Hybrid.csproj` — MAUI app.
- Platform folders: `Platforms/Android/`, `Platforms/iOS/`, `Platforms/MacCatalyst/`, `Platforms/Windows/`.
- References `FSH.BlazorShared` RCL (same as WASM).

## Auth difference — SecureStorage

- `ITokenStore` implementation uses `SecureStorage` (MAUI) instead of localStorage.
- Biometric unlock: `MauiAuthService` wraps `Platforms/BiometricService`.

## Native service registration

- `MauiProgram.cs`: register `ITokenStore` → `MauiTokenStore`, `IAuthService` → `MauiAuthService`.
- Conditionally register platform services: `#if ANDROID`, `#if IOS`.

## Offline

- `OfflineQueueService`: intercepts failed HTTP calls, stores in SQLite, replays on connectivity restore.
- `ConnectivityService`: monitors `Connectivity.Current.NetworkAccess`, fires events.

## Build & deploy

- `dotnet build -t:Run -f net10.0-android` for Android emulator.
- iOS requires Mac build host + Apple Developer account.
- Signing: `AndroidKeyStore` in csproj, iOS provisioning in `Entitlements.plist`.
```

### 5.2 Skills to Create (following dotnet/skills format)

Each skill follows the `SKILL.md` format from dotnet/skills: YAML frontmatter + Markdown body with When to use, Workflow, Validation, Don'ts.

**Skill index:**

| Skill | Purpose | Location |
|---|---|---|
| `add-blazor-page` | Add a list+detail+create page to a Blazor WASM app — the Blazor equivalent of `add-react-page` | `.agents/skills/add-blazor-page/SKILL.md` |
| `setup-blazor-auth` | Wire up AuthenticationStateProvider, token store, DelegatingHandler | `.agents/skills/setup-blazor-auth/SKILL.md` |
| `implement-blazor-form` | Add a MudForm with validation mirroring React's RHF+zod pattern | `.agents/skills/implement-blazor-form/SKILL.md` |
| `implement-blazor-list` | Add a paged, filterable, sortable MudTable — mirroring React's CSS-grid list | `.agents/skills/implement-blazor-list/SKILL.md` |
| `setup-blazor-realtime` | Wire SignalR hub to Blazor — notifications, chat | `.agents/skills/setup-blazor-realtime/SKILL.md` |
| `setup-blazor-sse` | Wire SSE streaming (dashboard app) | `.agents/skills/setup-blazor-sse/SKILL.md` |
| `add-maui-hybrid-feature` | Add a native feature to the MAUI Hybrid app (push, biometric, camera, etc.) | `.agents/skills/add-maui-hybrid-feature/SKILL.md` |
| `create-migration-blazor` | Scaffold a new migration after the EF core pattern (already exists for backend) | Reuse existing `create-migration` |
| `add-permission-csharp` | Mirror a permission from backend to C# constants + `PermissionPolicyProvider` | `.agents/skills/add-permission-csharp/SKILL.md` |

**Example: `add-blazor-page/SKILL.md` (outline):**

```yaml
---
name: add-blazor-page
description: Add a list+detail+create page to a Blazor WASM app (admin-blazor or dashboard-blazor). Covers service, page, route, MudBlazor components, and test. Use for any new frontend screen.
argument-hint: "[admin|dashboard] [Area] [Resource]"
---
```

Then the body would mirror the structure of `add-react-page` with:
- **Step 1:** Service class (`Services/I{Name}Service.cs` + `{Name}Service.cs`) — mirroring API module
- **Step 2:** DTOs / models — C# records mirroring TypeScript types
- **Step 3:** List page — MudTable with search/filter/pagination
- **Step 4:** Detail page — MudCard sections
- **Step 5:** Create/Edit — MudForm in MudDialog
- **Step 6:** Route registration — `@page` directive
- **Step 7:** (Admin) permission gate — `@attribute [Authorize(Policy = "...")]`
- **Step 8:** bUnit test
- **Step 9:** Playwright test
- **Checklist**

---

## 6. AI Agent Guidelines for Developers

### How to Drive AI Agents on This Project

**Systematic approach following the existing FSH agent pattern:**

1. **Set the context first:** Always instruct the agent to read the relevant `.agents/rules/` files before starting work:
   - Any Blazor WASM work → `blazor-shared.md` + `blazor-admin.md` or `blazor-dashboard.md`
   - MAUI work → `maui-hybrid.md`
   - Backend work (still needed!) → existing `architecture.md`, `api-conventions.md`, etc.

2. **Use existing skills as templates:** The `add-react-page` skill at `.agents/skills/add-react-page/SKILL.md` is the reference pattern. The Blazor equivalents follow the same structure but with MudBlazor + Blazor patterns.

3. **Skill loading order:** Load the app-specific skill first (`add-blazor-page`), then the cross-cutting skill (`implement-blazor-list` or `implement-blazor-form`).

4. **Cross-reference React source:** Every Blazor page has a direct React counterpart. Tell the agent: "Read `clients/admin/src/pages/identity/users.tsx` first, then build the Blazor equivalent at `clients/admin-blazor/Pages/Identity/Users/UsersListPage.razor`."

5. **Test-driven:** After creating a page, instruct the agent to:
   - Run `dotnet test clients/admin-blazor/FSH.Admin.Wasm.Tests/` (bUnit)
   - Then run Playwright e2e tests following existing pattern

6. **MudBlazor reference:** When asking an agent to build UI, specify the MudBlazor components to use:
   - "Use MudTable with MudTableSortLabel for the list"
   - "Use MudForm with MudTextField for the form"
   - "Use MudDialog for the create/edit modal"
   - "Use MudCard with MudCardContent for detail sections"

7. **The golden mutate() rule translates to:** Pass per-call data through method parameters, not through closed-over component state. In Blazor, this means service methods accept all data as parameters, and callers use `EventCallback` for results.

### Prompt Template for AI Agents

```
You are building a Blazor WASM page for the {admin|dashboard} app.
1. Read .agents/rules/frontend/blazor-shared.md and .agents/rules/frontend/blazor-{app}.md
2. Read the React counterpart at clients/{react-app}/src/pages/{area}/{page}.tsx for reference
3. Create (or modify):
   a. The service interface & implementation in BlazorShared/Services/
   b. The DTO model in BlazorShared/Models/
   c. The page at clients/{app}-blazor/Pages/{Area}/{Feature}/
   d. The route registration (@page directive)
   e. The permission gate (admin only)
   f. The bUnit test
   g. The Playwright e2e test
4. Use MudBlazor components: {MudTable, MudForm, MudDialog, etc.}
5. Run tests afterward: dotnet test {test_project}
```

---

## 7. AppHost Integration

Update `src/Host/FSH.Starter.AppHost/AppHost.cs` to add the Blazor WASM apps:

```csharp
// Blazor Admin WASM — served via `dotnet run` in dev, or static files in production
builder.AddProject<Projects.FSH_Admin_Wasm>($"{appPrefix}-admin-blazor")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 5175, targetPort: 5175, isProxied: false)
    .WithExternalHttpEndpoints()
    .WithEnvironment("ApiBaseUrl", api.GetEndpoint("https"));

// Blazor Dashboard WASM
builder.AddProject<Projects.FSH_Dashboard_Wasm>($"{appPrefix}-dashboard-blazor")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 5176, targetPort: 5176, isProxied: false)
    .WithExternalHttpEndpoints()
    .WithEnvironment("ApiBaseUrl", api.GetEndpoint("https"));

// MAUI Hybrid (desktop development only in AppHost — mobile requires physical device/emulator)
// builder.AddProject<Projects.FSH_Hybrid>($"{appPrefix}-hybrid")
//     .WithReference(api)
//     .WaitFor(api);
```

Update `MinIO CORS origins` to include Blazor dev ports:
```csharp
const string BlazorAdminOrigin = "http://localhost:5175";
const string BlazorDashboardOrigin = "http://localhost:5176";
// Add these to MINIO_API_CORS_ALLOW_ORIGIN
```

Update `appsettings.Development.json` with new CORS origins for the API.

---

## 8. Key Package Versions (to add to `Directory.Packages.props`)

```xml
<PackageVersion Include="MudBlazor" Version="8.5.0" />
<PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.0" />
<PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="10.0.0" />
<PackageVersion Include="Microsoft.AspNetCore.Components.Authorization" Version="10.0.0" />
<PackageVersion Include="Microsoft.AspNetCore.SignalR.Client" Version="10.0.0" />
<PackageVersion Include="Microsoft.Extensions.Http" Version="10.0.0" />
<PackageVersion Include="System.IdentityModel.Tokens.Jwt" Version="8.5.0" />
<PackageVersion Include="bunit" Version="2.0.0" />
<PackageVersion Include="Microsoft.Playwright.NUnit" Version="1.52.0" />
<PackageVersion Include="Microsoft.Maui.Essentials" Version="10.0.0" />
<PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageVersion Include="CommunityToolkit.Maui" Version="10.0.0" />
```

---

## 9. Summary of Work Items

```
├── docs/ (PRD, roadmap)
├── .agents/
│   ├── rules/
│   │   └── frontend/
│   │       ├── blazor-shared.md (NEW)
│   │       ├── blazor-admin.md (NEW)
│   │       ├── blazor-dashboard.md (NEW)
│   │       └── maui-hybrid.md (NEW)
│   └── skills/
│       ├── add-blazor-page/SKILL.md (NEW)
│       ├── setup-blazor-auth/SKILL.md (NEW)
│       ├── implement-blazor-form/SKILL.md (NEW)
│       ├── implement-blazor-list/SKILL.md (NEW)
│       ├── setup-blazor-realtime/SKILL.md (NEW)
│       ├── setup-blazor-sse/SKILL.md (NEW)
│       ├── add-maui-hybrid-feature/SKILL.md (NEW)
│       └── add-permission-csharp/SKILL.md (NEW)
├── clients/
│   ├── BlazorShared/                     (NEW — Razor Class Library)
│   │   ├── Auth/                         (TokenStore, AuthStateProvider)
│   │   ├── Components/                   (FshTable, FshPageHeader, etc.)
│   │   ├── Services/                     (IUserService, etc.)
│   │   ├── Models/                       (PagedResult, DTOs)
│   │   ├── Infrastructure/               (DelegatingHandlers)
│   │   ├── Realtime/                     (SignalR hub service)
│   │   ├── Sse/                          (dashboard-only SSE)
│   │   ├── Theming/                      (FshMudTheme)
│   │   └── Permissions/                  (C# permission constants)
│   ├── admin-blazor/                     (NEW — Blazor WASM admin)
│   │   ├── FSH.Admin.Wasm/
│   │   │   ├── Pages/
│   │   │   ├── Shared/
│   │   │   ├── Auth/
│   │   │   ├── wwwroot/
│   │   │   ├── Program.cs
│   │   │   └── FSH.Admin.Wasm.csproj
│   │   └── FSH.Admin.Wasm.Tests/
│   ├── dashboard-blazor/                 (NEW — Blazor WASM dashboard)
│   │   ├── FSH.Dashboard.Wasm/
│   │   └── FSH.Dashboard.Wasm.Tests/
│   └── FSH.Hybrid/                       (NEW — MAUI Blazor Hybrid)
│       └── FSH.Hybrid/
└── src/
    └── Host/
        └── FSH.Starter.AppHost/
            └── AppHost.cs                 (MODIFIED — add Blazor + update CORS)
    └── BuildingBlocks/
        └── Directory.Packages.props       (MODIFIED — add Blazor/MAUI packages)
    └── FSH.Starter.slnx                   (MODIFIED — add Blazor+MAUI projects)
```

---

## Next Step

I recommend starting with **Phase 0 (Foundation)** — creating the BlazorShared RCL, both WASM project scaffolds, and the `.agents/rules/` files. The BlazorShared project is the critical path: every subsequent phase depends on it.

Once you approve this plan, I can begin generating the actual files, starting with:
1. The `.agents/rules/frontend/blazor-*.md` rule files
2. The `.agents/skills/add-blazor-page/SKILL.md` skill file
3. The `BlazorShared` RCL project scaffold
4. The `FSH.Admin.Wasm` and `FSH.Dashboard.Wasm` project scaffolds

Would you like me to proceed with implementation?