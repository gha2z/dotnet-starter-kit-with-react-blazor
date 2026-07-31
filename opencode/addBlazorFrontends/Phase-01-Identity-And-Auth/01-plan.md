# Phase 1 — Identity & Authentication Flow

> **Target:** Both Blazor WASM apps authenticate against the API. Login page, token management, AuthStateProvider, DelegatingHandler, permission gating, and logout work end-to-end.

## Status

- Phase 1: **🔄 Partially complete** (core auth flow + login pages building, wired in DI, 0 build warnings)
- Prerequisites: Phase 0 ✅ (projects exist, MudBlazor boots, login stub present)

## Task Checklist

### 1.1 BlazorShared RCL — Auth Infrastructure
- [x] **ITokenStore** — Interface in BlazorShared/Auth/ (`ITokenStore.cs`)
  - AdminTokenStore (admin) + DashboardTokenStore (dashboard with impersonation)
  - Namespace: `fsh.admin.*` / `fsh.dashboard.*`
- [x] **AuthDelegatingHandler** — HttpClient handler in BlazorShared/Infrastructure/
  - Inject `Authorization: Bearer` + tenant header
  - 401 → refresh → retry with single-flight dedup (SemaphoreSlim)
  - Fixed: uses `IHttpClientFactory` with "FSH.Auth" named client for refresh calls
  - Fixed: matches API contract `{ token, refreshToken }` (was `{ accessToken, refreshToken }`)
- [x] **AuthStateProvider** — Custom `AuthenticationStateProvider` in BlazorShared/Auth/
  - Boot: read token → decode JWT → validate expiry → set identity
  - `NotifyLoginAsync()`, `NotifyLogoutAsync()` methods
  - `TokensChanged` event from token store
- [x] **IAuthService + AuthService** — Login/logout/refresh/permissions/forgot/reset/confirm API calls
  - `POST /api/v1/identity/token/issue` with `X-FSH-App: {admin|dashboard}` header
  - `POST /api/v1/identity/token/refresh` with `{ token, refreshToken }`
  - `GET /api/v1/identity/permissions` → `string[]`
  - `POST /api/v1/identity/forgot-password`
  - `POST /api/v1/identity/reset-password`
  - `GET /api/v1/identity/confirm-email?userId=&code=&tenant=`
- [x] **FshMudTheme** — FSH brand MudTheme (admin + dashboard variants, light/dark)
- [x] **Permission constants** — `IdentityPermissions` (Users + Roles), `MultitenancyPermissions` (Tenants)
- [x] **PermissionsProvider** — In-memory cache + token store fallback + API fetch
- [x] **FshPermissionGate** — Component-level gate (OR logic over Perms array)

### 1.2 BlazorShared RCL — Common Components
- [x] **FshPageHeader** — Title + icon + description + action button
- [x] **FshConfirmDialog** + **FshConfirmDialogContent** — MudDialog-based confirmation
- [x] **FshErrorBoundary** — Error display with reload button
- [x] **PagedResult<T>** + **SearchRequest** — Generic paged response models
- [x] **IRuntimeConfigService + RuntimeConfigService** — Load `/config.json` at startup

### 1.3 Admin App — Login & Auth Shell
- [x] **LoginPage.razor** — Full login form
  - MudForm with email, password, tenant fields + DataAnnotationsValidator
  - Calls `IAuthService.LoginAsync()` with `X-FSH-App: admin`
  - On success: `AuthStateProvider.NotifyLoginAsync()` → invalidate permissions → redirect
  - Error display (MudAlert), loading spinner (MudProgressCircular)
  - Forgot password link, signed-out reason display
- [x] **ForgotPasswordPage.razor** — Email + tenant form → success message
- [x] **ResetPasswordPage.razor** — Email + token + password + confirm + tenant form
  - Reads query params `email`, `token`, `tenant` on init
  - Confirm password validation
- [x] **ConfirmEmailPage.razor** — Auto-confirms on page load from `userId`, `code`, `tenant` query params
  - Shows loading/success/error states
- [x] **AppShell (MainLayout.razor)** — MudDrawer + MudAppBar + sidebar nav
  - User menu (settings, logout)
  - Theme toggle
- [x] **Route guard** — `AuthorizeRouteView` + `RedirectToLogin` in App.razor
- [ ] **Inactivity timer** — Service created but not yet wired in DI (needs JS interop refinement)
- [x] **Cross-tab logout** — `storage` JS event listener in App.razor.cs
- [x] **Program.cs DI wiring** — All services registered (IAuthService, IPermissionsProvider, IRuntimeConfigService, "FSH.Auth" named HttpClient)

### 1.4 Dashboard App — Login & Auth Shell
- [x] **LoginPage.razor** — Same as admin with `X-FSH-App: dashboard` header
- [x] **ForgotPasswordPage.razor** — Email + tenant form
- [x] **ResetPasswordPage.razor** — Same pattern as admin
- [x] **ConfirmEmailPage.razor** — Same pattern as admin
- [x] **AppShell (MainLayout.razor)** — MudDrawer + MudAppBar + command palette search field
- [x] **Route guard** — Simpler (no per-route permission gates)
- [ ] **Inactivity timer** — Service created but not yet wired in DI
- [x] **Program.cs DI wiring** — All services + SSE wiring
- [x] **Impersonation support** — DashboardTokenStore has StashTokensAsync/RestoreTokensAsync

## Next Up

**Phase 1 is partially complete.** Both apps build with 0 warnings/errors. Login page renders the full form. To fully close Phase 1:

1. Run both apps and test the login flow against the actual API (requires API running)
2. Wire `InactivityTimerService` in both Program.cs files (needs JS interop refinement — deferred)
3. Wire `PermissionsProvider` into `AuthStateProvider` so permissions are fetched on login

## Remaining Work

- [ ] **InactivityTimerService** — Register in DI (currently created but not wired). Needs JS interop activity detection refined.
- [ ] **AuthStateProvider permissions** — After `NotifyLoginAsync()`, automatically call `IPermissionsProvider.InvalidateCache()` so next access fetches fresh permissions from API.
- [ ] **Login E2E test** — Manually test against API: admin login → token stored → redirect → auth state persists on refresh
- [ ] **Error handling** — Test API failure scenarios: wrong password, network down, expired token

## Architecture Decisions

| Decision | Choice | Why |
|---|---|---|
| Token storage | localStorage via IJSRuntime | Matches React pattern exactly; ProtectedBrowserStorage adds abstraction |
| JWT decoding | System.IdentityModel.Tokens.Jwt | Same lib backend uses; avoid manual base64 decode |
| Refresh dedup | SemaphoreSlim(1,1) | Single-flight refresh identical to React's `refreshPromise` |
| Permission cache | In-memory, fetched once per app load | Matches React; re-fetch on token refresh |
| Session timeout | JS `setTimeout` via IJSRuntime | Browser-native; no server-side timer needed |
| Confirm dialog | MudDialog (not `ShowMessageBox`) | MudBlazor 9.x removed `ShowMessageBox` — custom dialog component required |

## Notes & Gotchas

- **Token namespace**: Admin uses `fsh.admin.*`, dashboard uses `fsh.dashboard.*` — keep separate to prevent collisions in same browser
- **X-FSH-App header**: Critical; API uses it to differentiate admin vs dashboard login flows. Root tenant rejected from dashboard login.
- **AuthStateProvider boot sequence**: Must complete before `Router` renders; use `AuthorizeRouteView` with `Resource` parameter
- **SignalR auth**: Pass token as `access_token_fragment` query param during negotiation — handled in Phase 2/3
- **MudBlazor 9.x**: `MudDialogInstance` renamed to `IMudDialogInstance` — use the interface name in DI
- **Cross-tab sync**: Use `IJSRuntime` to add `storage` event listener; when token cleared in one tab, all tabs redirect to login

## Blocker Checklist

- [x] Phase 0 projects boot without runtime errors
- [ ] API running and accessible at https://localhost:7030 (needed for login E2E test)
- [ ] Identity endpoints tested via Swagger/Scalar
- [x] MudBlazor 9.x installed in BlazorShared and both WASM projects
- [x] Auth infrastructure builds with 0 errors/warnings
- [x] Login page renders the full form
