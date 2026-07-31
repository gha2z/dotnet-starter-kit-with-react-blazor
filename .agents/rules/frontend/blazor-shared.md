# Blazor — shared conventions

Applies to **both** `clients/admin-blazor` (FSH.Admin.Wasm) and `clients/dashboard-blazor` (FSH.Dashboard.Wasm). Read this before any Blazor work, then read the app-specific file.

Stack: .NET 10 Blazor WASM (standalone) · MudBlazor 8.x · `@microsoft/signalr` (via JS interop) · `System.IdentityModel.Tokens.Jwt` · bUnit · Playwright.

## Project structure

```
clients/
├── BlazorShared/                    # Razor Class Library — shared between both WASM apps + MAUI Hybrid
│   ├── Auth/                        # ITokenStore, AuthenticationStateProvider
│   ├── Components/                  # FshPageHeader, FshTable, FshConfirmDialog, FshPermissionGate…
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
- **Admin localStorage namespace:** `fsh.admin.*` — **Dashboard:** `fsh.dashboard.*`.

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

- **`FshMudTheme`** in `BlazorShared/Theming/` defines the FSH brand as a `MudTheme`.
- Both apps create their `MudThemeProvider` in `App.razor` with `ThemeProvider`.
- Admin theme: cool-cast neutrals (hue 240, small non-zero chroma), chartreuse signal accent.
- Dashboard theme: chroma-0 neutrals, swappable accent (rose/indigo/violet/sky/emerald/amber).

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
- `AppShell` wraps `MudLayout > MudDrawer + MudMainContent > @Body`.
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
- Handles reconnection with exponential backoff.

## Testing

### bUnit (unit tests)

- Test project per WASM app: `FSH.Admin.Wasm.Tests`, `FSH.Dashboard.Wasm.Tests`.
- Test context setup helper: `TestContext` with MudBlazor + BlazorShared services registered.
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
