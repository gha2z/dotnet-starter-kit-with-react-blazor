# Phase 4 — Testing
Last Update: 2026-Aug-06 05:20:00, by: opencode (auto/coding, model: deepseek-v4-flash-free).

> **Target:** Comprehensive test coverage with bUnit (component unit tests) + Playwright (E2E). Auth flows, page states (loading/empty/error/edge), permission gating, form validation, SSE, SignalR.

## Status

- Phase 4: **🟡 In progress** — bUnit suites landed; E2E landing per app.
  - Dashboard bUnit: **178/178 ✅** (`clients/dashboard-blazor/FSH.Dashboard.Wasm.Tests`)
  - Admin bUnit: **155/155 ✅** (`clients/admin-blazor/FSH.Admin.Wasm.Tests`)
  - Admin Playwright E2E: **9/9 ✅** (`clients/admin-blazor/FSH.Admin.Wasm.E2E.Tests`, port 5175)
  - Dashboard Playwright E2E: **11/11 ✅** (`clients/dashboard-blazor/FSH.Dashboard.Wasm.E2E.Tests`, port 5176)
- Prerequisites: Phase 2 ✅ + Phase 3 ✅ (all pages built)

## Task Checklist

### 4.1 bUnit Test Infrastructure
- [ ] **TestContext setup helpers** — `BunitContext` with:
  - Mock `AuthenticationStateProvider` (authenticated / unauthenticated / specific permissions)
  - Mock `IJSRuntime` (token store, SSE)
  - Mock `HttpClient` (configured message handler)
  - MudBlazor services registered
- [ ] **Mock service generators** — AutoFixture + NSubstitute for all `I*Service` interfaces
- [ ] **NavigationManager mock** — Verify redirects (login, 403, not-found)
- [ ] **Test data builders** — Create test DTOs with AutoFixture

### 4.2 Auth Flow Tests
- [ ] Login page: renders form, validates empty fields, calls service, stores token, navigates
- [ ] Login page: shows error MudAlert on API failure
- [ ] Login page: loading state (MudProgressCircular while submitting)
- [ ] ForgotPassword: email validation, success message
- [ ] ResetPassword: token+password validation, success
- [ ] AuthStateProvider: boot sequence — no token → unauthenticated
- [ ] AuthStateProvider: valid JWT → authenticated with claims
- [ ] AuthStateProvider: expired JWT → unauthenticated
- [ ] AuthStateProvider: refresh succeeds → re-authenticated
- [ ] AuthStateProvider: refresh fails → redirect login
- [ ] AuthDelegatingHandler: adds Authorization header
- [ ] AuthDelegatingHandler: adds tenant header
- [ ] AuthDelegatingHandler: 401 → refresh → retry
- [ ] AuthDelegatingHandler: refresh fails → 401 propagated
- [ ] Cross-tab logout: storage event received → redirect login
- [ ] Inactivity timer: countdown → warning dialog → auto-logout
- [ ] Inactivity timer: activity resets timer

### 4.3 Page Component Tests (Admin)
- [ ] **UsersListPage**: renders MudTable with data, empty state, loading spinner, error state
- [ ] **UsersListPage**: search filters, pagination works
- [ ] **UserCreateDialog**: form validation (required fields, email format), submit calls service
- [ ] **UserDetailPage**: renders MudCard sections, impersonate button visible
- [ ] **RolesListPage**: tree renders, permissions show
- [ ] **TenantsListPage**: table renders, upgrade dialog works
- [ ] **TenantCreatePage**: MudStepper progression
- [ ] **BillingPlansPage**: MudCard grid renders
- [ ] **NotificationsListPage**: MudBadge counts, mark-read flows
- [ ] **HealthDashboardPage**: MudCards show status indicators
- [ ] **ProfilePage**: form load/save, validation
- [ ] **ImpersonationListPage**: table renders, end button works

### 4.4 Page Component Tests (Dashboard)
- [ ] **OverviewPage**: MudCards render, SSE event updates card value
- [ ] **SubscriptionPage**: plan details display, usage bars
- [ ] **WalletPage**: balance display, top-up dialog validation
- [ ] **ProductsListPage**: MudTable with thumbnails, brand/category filters
- [ ] **TicketsListPage**: status MudChip colors, search/filter
- [ ] **TicketDetailPage**: thread renders messages, reply sends
- [ ] **ChatPage**: channel list, message list loads older on scroll
- [ ] **FileManagerPage**: MudTable with actions, upload zone renders
- [ ] **CommandPalette**: MudAutocomplete filters pages, selects navigates

### 4.5 Permission Gate Tests
- [ ] **FshPermissionGate**: renders child content when user has permission
- [ ] **FshPermissionGate**: hides child content when user lacks permission
- [ ] **FshPermissionGate**: multiple permissions match (OR logic)
- [ ] **Route guard**: protected page with auth → renders
- [ ] **Route guard**: protected page without auth → redirects login
- [ ] **Route guard**: admin route gated by specific permission → 403/page not found

### 4.6 Form Validation Tests
- [ ] MudForm required fields: submit with empty → validation errors shown
- [ ] Email format: invalid → error message
- [ ] StringLength: too long → error message
- [ ] Password confirmation: mismatch → error message
- [ ] Form submit: valid → service called, success → close dialog / navigate
- [ ] Form submit: valid → API error → error displayed, stay on form

### 4.7 SSE Tests
- [ ] SSE client: connects, receives messages, fires events
- [ ] SSE client: reconnects on disconnect (exponential backoff)
- [ ] SSE client: max retries exhausted → error event
- [ ] Overview page: SSE message → MudCard value updates

### 4.8 SignalR Tests
- [ ] Hub connection: connects with auth token
- [ ] Hub connection: receives notification → toast/badge update
- [ ] Hub connection: receives chat message → prepend to message list
- [ ] Hub connection: disconnect → reconnection with backoff
- [ ] Chat page: send message → appears in chat

### 4.9 Playwright E2E Tests

Dashboard suite (`clients/dashboard-blazor/FSH.Dashboard.Wasm.E2E.Tests`), route-mocked on port 5176, mirrors the admin harness:
- [x] **Test infrastructure**: authed session seeding (localStorage init scripts), route-level API mocks + CORS/OPTIONS, SSE/SignalR shell mocks, `WaitForBlazorReady`
- [x] **Critical path: login flow**: navigate → fill form (tenant/email/password) → submit → tokens stored → redirect to overview
- [x] **Critical path: login failure**: 401 → "Login failed (401)" alert → stays on /login
- [x] **Critical path: logout**: user menu → confirm → tokens cleared → redirect to login
- [x] **Critical path: users list**: navigate to users → rows render → search queries backend (`/identity/users/search?Search=`) and filters rows
- [x] **Critical path: user create**: users → Register user dialog → fill form → submit → POST `/identity/register` body verified → dialog closes + success snackbar (dashboard parity: no detail navigation)
- [x] **Permission gate E2E**: user without permission → page still renders but nav link + actions hidden (dashboard parity: no 403 surface — that is admin-app behavior)
- [ ] **Critical path: tenant create**: admin-app concern (admin E2E suite)
- [ ] **Inactivity timeout**: admin-app concern (admin E2E suite)
- [x] **Cross-tab logout**: second tab → logout in first → storage event → second tab redirects to login

Admin suite: 9/9 ✅ (auth flow incl. cross-tab, permission gate w/ 403 surface, users page, inactivity timeout).

## Next Up

**Remaining Phase 4**: none for the dashboard stream — both E2E suites landed. Next stream work is Phase 6 (dashboard polish) per STATUS.md.

## Architecture Decisions

| Decision | Choice | Why |
|---|---|---|
| Test framework | xUnit + bUnit | Matches existing backend tests; bUnit is MudBlazor's recommended test lib |
| Mocking | NSubstitute | Matches existing backend convention (not Moq) |
| Test data | AutoFixture | Matches existing convention; reduces boilerplate |
| Assertions | Shouldly | Matches existing convention (`result.ShouldBe(expected)`) |
| E2E framework | Playwright (not Selenium) | Matches existing React E2E infrastructure; faster, cross-browser |
| E2E mocking | Route-level API mocks | Avoids needing real API; matches `route-mocked` React pattern |

## Notes & Gotchas

- **bUnit + MudBlazor**: Must register MudBlazor services and theme in test context. Use `AddMudBlazorTestServices()` extension or manually register `MudServices`.
- **IJSRuntime in tests**: Most JsInteropTokenStore methods need `IJSRuntime.InvokeAsync`. Mock with `IJSRuntimeMock` or use `bunit`'s `BunitJSInterop`.
- **Component under test**: Wrap in `<MudThemeProvider>` and `<CascadingAuthenticationState>` if the component uses auth. Can use `RenderTree.Add<MudThemeProvider>()`.
- **Playwright route mock**: Override `page.route('**/api/v1/**')` to return fixture JSON. Seed token via `page.evaluate()` to set localStorage before navigation.
- **AuthStateProvider async boot**: In tests, ensure boot completes before asserting auth state. Use `authStateProvider.GetAuthenticationStateAsync()`.
- **HttpClient mock**: Use `MockHttpMessageHandler` or configure `HttpClient` factory to return a test handler. Register as `AddHttpClient<IService, Service>().AddHttpMessageHandler<MockHandler>()`.
- **Playwright install**: `pwsh bin/Debug/net10.0/playwright.ps1 install` — ensure Playwright browsers are installed on CI.

## Blocker Checklist

- [ ] Phase 2 + 3 complete (pages to test exist)
- [ ] bUnit NuGet packages in Directory.Packages.props (already 2.0.66)
- [ ] Playwright NuGet packages added
- [ ] Playwright browsers installed in CI/dev environment
- [ ] Test projects reference BlazorShared, WASM projects, bUnit, Playwright
