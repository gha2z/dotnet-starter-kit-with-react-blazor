# Blazor — shared conventions

Applies to **both** `clients/admin-blazor` (FSH.Admin.Wasm) and `clients/dashboard-blazor` (FSH.Dashboard.Wasm). Read this before any Blazor work, then read the app-specific file.

Stack: .NET 10 Blazor WASM (standalone) · MudBlazor 9.7 · `@microsoft/signalr` (via JS interop) · `System.IdentityModel.Tokens.Jwt` · bUnit 2.0 · Playwright.

## Project structure

```
clients/
├── BlazorShared/                    # Razor Class Library — shared between both WASM apps + MAUI Hybrid
│   ├── Auth/                        # ITokenStore, AuthenticationStateProvider
│   ├── Components/                  # FshPageHeader, FshTable, FshConfirmDialog, FshPermissionGate,
│   │                                #   FshNavSection, FshNotificationBell, FshPager, FshFilterBar,
│   │                                #   FshLoadingRow, FshKpiTile, FshSectionRule…
│   ├── Infrastructure/              # AuthDelegatingHandler, HttpClient factory extensions
│   ├── Models/                      # PagedResult<T>, DTOs, request/response types
│   ├── Services/                    # Per-module typed API service interfaces + implementations
│   ├── Realtime/                    # SignalR hub connection management
│   ├── Sse/                         # SSE client (dashboard app only)
│   ├── Theming/                     # FshMudTheme (FSH brand → MudBlazor theme)
│   └── Permissions/                 # C# permission constants mirroring server *Permissions.cs
│
├── admin-blazor/
│   └── FSH.Admin.Wasm/              # Blazor WASM standalone app (operator console)
│       ├── Pages/
│       ├── Auth/                    # Admin-specific auth overrides
│       ├── Shared/                  # App-specific layout components
│       └── wwwroot/
│
└── dashboard-blazor/
    └── FSH.Dashboard.Wasm/          # Blazor WASM standalone app (tenant-facing)
        ├── Pages/
        ├── Auth/
        └── wwwroot/
```

## API client pattern

- **`HttpClient` with `AuthDelegatingHandler`** — registers via `AddHttpClient<IService, Service>()` in DI.
- Types are **hand-written C# records** mirroring the API JSON (no codegen).
- Per-module typed service classes in `BlazorShared/Services/`: `IUserService`, `ITenantService`, `ICatalogService`, etc.
- Paged queries: `Task<PagedResult<T>> SearchAsync(SearchRequest request)`.

### AuthDelegatingHandler behavior

1. Injects `Authorization: Bearer <token>` from `ITokenStore.GetAccessTokenAsync()`.
2. Injects `tenant` header (lowercase) from `ITokenStore.GetTenantAsync()`.
3. On 401 with a refresh token: triggers single-flight refresh via `SemaphoreSlim(1,1)` — identical dedup to React's `refreshPromise`.
4. Retries the original request once after refresh.
5. Parses error responses into `ApiRequestException` with ProblemDetails.
6. Returns `default` for 204/empty responses.

## Env / runtime config

- `IRuntimeConfigService` fetches `/config.json` at WASM startup via `HttpClient` (before Blazor router mounts).
- Same contract as React's `env.ts`: `{ apiBase, defaultTenant, dashboardUrl?, demoMode? }`.
- No `VITE_*` build-time vars — one built image promotes across environments.

## Auth (`BlazorShared/Auth/`)

- **`ITokenStore`** — interface wrapping localStorage JS interop. Mirror of React's `token-store.ts`.
  - `GetAccessTokenAsync()`, `GetRefreshTokenAsync()`, `SetTokensAsync()`, `ClearAsync()`
  - `GetTenantAsync()`, `SetTenantAsync()`
  - `GetPermissionsAsync()`, `SetPermissionsAsync()`
  - `Subscribe()` for cross-tab sync via `storage` JS event.
- **`AuthStateProvider`** — `AuthenticationStateProvider` implementation:
  1. Boot: read stored access token from JS interop
  2. Decode JWT payload (no library — manual base64 decode for header/claims)
  3. Validate expiry with 10-second skew
  4. If expired but refresh token exists: silent refresh
  5. Set `ClaimsPrincipal` with roles from token
  6. Fetch permissions from `GET /api/v1/identity/permissions` (admin) or from JWT (dashboard)
  7. Expose `PermissionsHydrated` task/event for RouteGuard
- Login: `POST /api/v1/identity/token/issue` with `X-FSH-App` header (`admin` / `dashboard`).
- Logout: clear tokens, clear permissions, navigate to `/login`.
- **localStorage prefixes:** admin `fsh.admin.*` — dashboard `fsh.dashboard.*`. **Exception — theme key:** both apps persist the theme under `fsh.theme` (dashboard, Light/Dark/System) and `fsh.admin.theme` (admin, binary) — the same keys the React apps use.

## Data fetching patterns

- Services inject `HttpClient` (via typed `IHttpClientFactory` pattern).
- **PagedResult<T>** mirrors React's `PagedResponse<T>`:
  ```csharp
  public record PagedResult<T>(
      List<T> Items,
      int PageNumber,
      int PageSize,
      int TotalCount,
      int TotalPages,
      bool HasNext,
      bool HasPrevious);
  ```
- Pages call `Service.SearchAsync(...)` in `OnInitializedAsync`, manage page/filter state as properties.
- Cache invalidation: services hold a `RefreshTrigger` event that components subscribe to — analogous to `queryClient.invalidateQueries()`.

## MudBlazor theming

- **`FshMudTheme`** in `BlazorShared/Theming/` defines the FSH brand as a `MudTheme` — React-19 parity:
  rose primary (`#E11D48` light / `#FB7185` dark), Outfit display font for headings, Inter for body
  (loaded via the app `index.html` font link, same as the React apps).
- **`FshThemeService`** (singleton, `BlazorShared/Theming/`) owns dynamic theming with a
  **`ThemeMode` enum (`Light | Dark | System`)**:
  - Constructor: `new FshThemeService(IJSRuntime, storageKey, defaultMode)`.
  - **Dashboard** registers `("fsh.theme", ThemeMode.System)` — React parity (key + System mode, OS-following).
  - **Admin** registers `("fsh.admin.theme")` — binary (default `Dark`); the topbar toggle uses the
    legacy `SetAsync(bool)` which is still supported and maps to Light/Dark.
  - Stored values are the raw strings `"light" | "dark" | "system"`; `System` resolves via `prefersDark()`
    in `fshTheme.js` at initialize and on every mode switch (`SetModeAsync`).
  - `Mode`, `IsDarkMode`, `SetModeAsync(ThemeMode)`, `SetAsync(bool)`, `Changed` event.
  - `Program.cs` must call `InitializeAsync()` before `RunAsync()` to avoid a light→dark flash.
- Shared design-system styles live in `BlazorShared/wwwroot/css/fsh.css` (referenced by both apps):
  brand mark/wordmark, `.fsh-sidebar` + `.fsh-topbar` shell (custom `<aside>`/`<header>`, NOT
  `MudLayout/MudDrawer`), accordion nav sections (`.fsh-nav-section`, animated `0fr→1fr` expand),
  nav links with active brand bar, mono labels, pills, skeletons, KPI tiles, pager/filter/loading-row,
  notification bell, SSE status dot, login orbs.
- Both apps' `MainLayout` use the shared shell: persisted sidebar collapse (`fsh.admin.sidebar.collapsed`
  / `fsh.sidebar.collapsed` — same keys as the React apps, stored as `"true"/"false"`), mobile drawer +
  backdrop below 900px, theme control, user menu (initials tile + name + tenant) with sign-out
  confirmation via `FshConfirmDialogContent`.
- Shared components in `BlazorShared/Components/`: `FshPageHeader`, `FshToneIconTile`, `FshStatusPill`,
  `FshStatTile`, `FshMonogram`, `FshEmptyState`, `FshErrorBand`, `FshConfirmDialogContent`,
  `FshPermissionGate`, `FshNavSection` (accordion section — header + animated body; `Collapsed` renders
  a flat icon stack), `FshNotificationBell` (MudMenu dropdown: unread badge, list of 20, mark-read on
  click + link nav, mark-all-read, SignalR `NotificationCreated` subscription, refresh on open),
  `FshPager` ("Showing N–M of T · folio PP/TT" + prev/next), `FshFilterBar`, `FshLoadingRow`,
  `FshKpiTile`, `FshSectionRule`. All expose a `Class` parameter when used with spacing utility classes.

## MudBlazor 9.x form gotchas

- MudForm has **no submit callback** (`OnValidSubmit`/`OnSubmit` do not exist and are silently dropped).
  Enter-to-submit = `OnEnterPressed="..."` on the `MudForm`; the submit button needs its own
  `OnClick="..."` (a `ButtonType="Submit"` button alone does nothing). Validation is explicit:
  `await _form.ValidateAsync(); if (!_form.IsValid) return;` (`Validate()` is obsolete,
  `ValidateAsync()` returns `Task`, not `Task<bool>`).
- Razor attribute tokenizer: string literals inside `@onclick="...(...)"` break parsing (both quote
  styles). Use method groups (`@onclick="GoHome"`) or parameterless lambdas — no `'/x'` or `"/x"` inside.

## Directory & naming conventions

- Pages: `Pages/{Area}/{Feature}/{Feature}Page.razor` + code-behind `{Feature}Page.razor.cs`.
- Services: `Services/I{Name}Service.cs` + `{Name}Service.cs`.
- Components: `Components/{Name}/{Name}.razor` + `{Name}.razor.cs`.
- Models/DTOs: `Models/{Module}/{Name}Dto.cs` or `{Name}Request.cs`.
- **Named partial classes** — no `@code` blocks in `.razor` files (use code-behind `.razor.cs`).
- File-scoped namespaces, 4-space indent, explicit types, nullable enabled.
- `ConfigureAwait(false)` on every await in service/DI code.

## Routing

- `Router` with `@page` directives for each routable component.
- `@attribute [Authorize]` on all protected pages.
- Both apps use the shared `fsh.css` shell (`fsh-sidebar`/`fsh-topbar` custom aside+header in `MainLayout`), not `MudLayout`.
- Lazy loading: Blazor WASM lazy-loads assemblies via `LazyAssemblyLoader` for large feature areas.

## Realtime (SignalR)

- `HubConnectionService` in `BlazorShared/Realtime/` mirrors `realtime-context.tsx`:
  - Connects to `/api/v1/realtime/hub`
  - Auth via `accessTokenFactory` reading from `ITokenStore`
  - Reconnects on token change
  - Exposes events as `event Action<Notification>` delegates
- Components subscribe via `@implements IDisposable` + register/unregister handlers.
- `@microsoft/signalr` via JS interop (Blazor JS isolation).

## SSE (dashboard only)

- `SseService` in `BlazorShared/Sse/` wraps `HttpClient.GetStreamAsync` + manual SSE line parsing.
- Exposes `IObservable<SseEvent>` for component subscription.
- **`ConnectionChanged` event** — raised on `StartAsync`, successful connect, every reconnect-loop
  iteration, and `StopAsync`. The dashboard topbar user menu shows a live status dot bound to it
  (`.fsh-sse-dot` / `connected` class in `fsh.css`).
- Handles reconnection with exponential backoff.

## Notifications

- **`INotificationService`** in `BlazorShared/Services/` (`NotificationService`): typed client for
  `GET /api/v1/notifications/unread-count`, `GET /api/v1/notifications?unreadOnly=&page=&pageSize=`,
  `POST /api/v1/notifications/{id}/read` (204), `POST /api/v1/notifications/read-all`.
- `NotificationDto` in `Models/Notifications/` mirrors the server record.
- **`FshNotificationBell`** subscribes to SignalR `NotificationCreated` (server pushes to
  `user:{userId}` on `AppHub`), refreshes the unread badge, and re-subscribes when hub state changes.
  Register the service in `Program.cs`; render the bell in the topbar; it needs
  `NotificationPermissions.Inbox.View` / `MarkRead` on the server side to work.

## Testing

### bUnit (unit tests)

- Test project per WASM app: `FSH.Admin.Wasm.Tests`, `FSH.Dashboard.Wasm.Tests`.
- **bUnit 2.0**: `BunitContext` (not `TestContext`) — e.g. `TestSetup : BunitContext, IAsyncLifetime`
  with `JSInterop.Mode = JSRuntimeMode.Loose` and `Services.AddMudServices()` in the ctor.
  `DefaultWaitTimeout = TimeSpan.FromSeconds(30)` for slow renders.
- **`Render<T>(parameters => ...)` — `RenderComponent<T>` is obsolete and warns (treat-warnings-as-errors).**
- **bUnit normalizes boolean attributes** (incl. `aria-expanded`): false ⇒ attribute removed, true ⇒
  bare/empty attribute. Assert presence (`Attributes.Any(a => a.Name == ...)`), not value.
- **JS module interop (`IJSRuntime.InvokeAsync("import", ...)`)** is not covered by loose-mode JSInterop
  `Setup` — write a minimal `FakeJSRuntime : IJSRuntime` returning a fake `IJSObjectReference`
  (`ValueTask<TValue>` signatures) that stores/returns values like localStorage.
- `MockHttpClient` via `AddHttpClient` with `MockHttpMessageHandler`.
- Page render tests: verify loading, empty, error, and data states.

### Playwright (e2e)

- Same pattern as React apps: `tests/{area}/{page}.spec.ts`.
- `seedAuthedSession(page, TEST_USER)` — injects JWT + permissions into localStorage via `AddInitScript`.
- `mockJsonResponse(page, urlGlob, body)` — intercepts API calls.
- Critical paths: login → navigate → list → detail → form submit.

## Add a page (workflow)

1. **Service**: Add `I{Name}Service` + `{Name}Service` in `BlazorShared/Services/`.
2. **Model**: Add DTOs in `BlazorShared/Models/{Module}/`.
3. **Page**: Create `Pages/{Area}/{Feature}/{Feature}Page.razor` + `.razor.cs`.
4. **Route**: Add `@page "/{module}/{resources}"` directive.
5. **(Admin) Permission gate**: Add `@attribute [Authorize(Policy = "Permissions.{Resource}.View")]`.
6. **bUnit test**: Render test for loading/empty/error/data states.
7. **Playwright test**: Auth seed + shell mocks + page mocks.
