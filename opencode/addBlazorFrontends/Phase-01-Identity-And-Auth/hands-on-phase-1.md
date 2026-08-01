# Chapter 1: Identity & Authentication — "Who Are You?"

> **Audience**: From absolute beginner to senior. Each section has tiered callouts.
> **Prerequisites**: Chapter 0 (Foundation & Tooling) complete. Both WASM apps boot.
> **Time to complete**: 3-5 hours reading + 2-3 hours hands-on.
> **What you'll know after this chapter**: How JWT authentication works end-to-end, how the login form works, how tokens are stored safely, how 401→refresh works without races, how permissions are cached and gated, and how every auth page (forgot/reset/confirm email) fits together.

---

## 1.0 The Big Picture — What "Authentication" Actually Means

Before any code, understand the flow you're about to build:

```
┌──────────┐   1. POST email+password    ┌──────────┐
│          │ ──────────────────────────▶ │          │
│ Blazor   │                            │  API     │
│ WASM app │   2. 200 OK: JWT + refresh  │ (ASP.NET │
│ (browser)│ ◀────────────────────────── │ Identity)│
│          │                            │          │
│          │   3. GET /tenants           │          │
│          │ ─── Bearer JWT ───────────▶ │          │
│          │                            │          │
└──────────┘   4. 200 OK: tenant data   └──────────┘
```

**Three actors:**
1. **The user** — types email/password into a MudForm
2. **The app (client)** — sends credentials, receives tokens, stores them, sends them on every API call
3. **The API (server)** — verifies credentials, issues tokens, verifies tokens on every request

> **🐣 Beginner**: Think of a JWT like a museum wristband. You show your ID at the ticket counter (login) → you get a wristband (JWT) → you show the wristband at every exhibit (API call) → you don't need to show your ID again until the wristband expires.

> **👨‍🔬 Senior Note**: This is a "stateless" auth model. The server doesn't remember who you are between requests — it just verifies the *signature* of your token. That's why the server can scale horizontally: any server can verify any token.

### 1.0.1 What's Inside a JWT?

A JWT is three base64-encoded parts, separated by dots:

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkFsaWNlIiwiZXhwIjoxNzU2NzU1MjAwfQ.4o2F5h3kQp9dZ8Bf...
```

| Part | Contents | Why it matters |
|------|----------|---------------|
| **Header** | Algorithm (`HS256`), token type (`JWT`) | Tells the server how the token was signed |
| **Payload** | Claims: `sub` (user id), `name`, `email`, `permissions`, `exp` (expiry), `tenant` | The actual identity data |
| **Signature** | Cryptographic hash of header+payload with a server secret | Proves the token wasn't forged |

**The critical claim: `exp` (expiry).** A JWT is *not* revocable — once issued, it's valid until it expires. That's why:
- Access tokens are short-lived (e.g., 15-30 minutes)
- Refresh tokens are longer-lived (e.g., 7 days) but only usable to get a *new* access token

> **🐣 Beginner**: The JWT doesn't *hide* your data — it's just base64, anyone can decode it. The *signature* is what prevents forgery. Never put secrets in a JWT.

> **👨‍🔬 Senior Note**: This project uses **System.IdentityModel.Tokens.Jwt** (`JwtSecurityTokenHandler.ReadJwtToken()`) to decode the token client-side. The client reads the `exp` claim to decide "am I logged in?" — this avoids a server round-trip on every page load. The server never trusts the client's read — it re-verifies the signature on every request.

---

## 1.1 The Token Store — Where Tokens Live

### The Problem

The user logs in → we have tokens → where do we put them so the app remembers the user after a page refresh?

| Storage option | Survives refresh? | Survives browser restart? | Security |
|---------------|-------------------|--------------------------|----------|
| C# variable (in-memory) | ❌ No | ❌ No | Best |
| `localStorage` | ✅ Yes | ✅ Yes | ⚠️ XSS-readable |
| `sessionStorage` | ✅ Yes | ❌ No | ⚠️ XSS-readable |
| `cookie` (HttpOnly) | ✅ Yes | ✅ Yes | ✅ XSS-proof |

**Why localStorage?** The React apps use it (parity!), WASM has no HttpOnly-cookie equivalent that's simple, and the tokens are short-lived. We mitigate XSS risk with a strict CSP (Content Security Policy).

### The Code

`clients/BlazorShared/Auth/ITokenStore.cs`:

```csharp
public interface ITokenStore
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task<string?> GetTenantAsync();
    Task SetTokensAsync(string? accessToken, string? refreshToken);
    Task SetTenantAsync(string? tenant);
    Task SetPermissionsAsync(string[] permissions);
    Task<string[]?> GetPermissionsAsync();
    Task ClearAsync();
}
```

> **🐣 Beginner**: An **interface** (`ITokenStore`) is a contract — "any class that wants to be a token store MUST have these methods." The rest of the app codes against the interface, so it doesn't care *where* tokens are stored (localStorage, memory, a file...).

The concrete implementation uses `IJSRuntime` to call browser `localStorage`:

```csharp
// Snippet from AdminTokenStore (simplified)
public sealed class AdminTokenStore(IJSRuntime js) : ITokenStore
{
    private const string AccessTokenKey = "fsh.admin.accessToken";
    private const string RefreshTokenKey = "fsh.admin.refreshToken";
    // ...
}
```

**Full files:**
- `clients/BlazorShared/Auth/ITokenStore.cs` — the contract
- `clients/BlazorShared/Auth/AdminTokenStore.cs` — admin implementation
- `clients/BlazorShared/Auth/DashboardTokenStore.cs` — dashboard implementation (+ impersonation stash/restore)

### Why Two Token Stores?

```csharp
// Admin
private const string AccessTokenKey = "fsh.admin.accessToken";
// Dashboard
private const string AccessTokenKey = "fsh.dashboard.accessToken";
```

Both apps run in the *same browser* (localhost:5175 and localhost:5176). Without separate key namespaces, the dashboard would silently read the admin's token and vice versa.

> **👨‍🔬 Senior Note**: The `X-FSH-App` header is the *server-side* counterpart: the API validates that an admin token isn't used to call dashboard-only endpoints. The `fsh.admin.*` / `fsh.dashboard.*` localStorage prefixes are the *client-side* counterpart. Belt and suspenders — a classic cross-cutting concern.

> **🧑‍💻 Junior Tip**: This "separate namespaces per app in the same browser" problem is a classic gotcha. When you see `localStorage`, always ask: "what if two apps share this origin?" — your key naming is your isolation strategy.

---

## 1.2 The Auth Service — Talking to the Identity API

### The Contract

`clients/BlazorShared/Services/IAuthService.cs`:

```csharp
public interface IAuthService
{
    Task<TokenResponse> LoginAsync(string email, string password, string tenant, string appHeader, CancellationToken ct = default);
    Task<RefreshResponse> RefreshAsync(string accessToken, string refreshToken, CancellationToken ct = default);
    Task<string[]> GetPermissionsAsync(CancellationToken ct = default);
    Task ForgotPasswordAsync(string email, string tenant, CancellationToken ct = default);
    Task ResetPasswordAsync(string email, string password, string token, string tenant, CancellationToken ct = default);
    Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken ct = default);
}

public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTime? AccessTokenExpiresAt, DateTime? RefreshTokenExpiresAt);
public sealed record RefreshResponse(string Token, string RefreshToken);
```

### The Login Implementation

`clients/BlazorShared/Services/AuthService.cs`:

```csharp
public async Task<TokenResponse> LoginAsync(string email, string password, string tenant, string appHeader, CancellationToken ct = default)
{
    using var client = httpClientFactory.CreateClient("FSH.Auth");

    var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/token/issue")
    {
        Content = JsonContent.Create(new { email, password }),
    };
    request.Headers.TryAddWithoutValidation("tenant", tenant);
    request.Headers.TryAddWithoutValidation("X-FSH-App", appHeader);

    var response = await client.SendAsync(request, ct);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
    return result ?? throw new InvalidOperationException("Null login response");
}
```

**Why `IHttpClientFactory` + a named client "FSH.Auth"?**

The named client (`FSH.Auth`) is *deliberately* configured **without** the `AuthDelegatingHandler`. Login is anonymous — we don't have a token yet. If the handler were attached, it would try to attach a (nonexistent) token and possibly even attempt a refresh mid-login. Two clients, two purposes:

| Client | Handler | Purpose |
|--------|---------|---------|
| `FSH.Auth` | ❌ None | Anonymous calls: login, refresh, forgot/reset password, confirm email |
| default authed client | ✅ `AuthDelegatingHandler` | All authenticated API calls |

> **🐣 Beginner**: **DelegatingHandler** = an HTTP "filter" that every request passes through before leaving the app. It's like airport security: every passenger (request) goes through the scanner (handler) where a token gets stamped on.

> **👨‍🔬 Senior Note**: Why not just call `HttpClient` directly? Because **HttpClient leaks sockets** if you create/dispose it per call (it's not really IDisposable). `IHttpClientFactory` pools and reuses handlers, rotates DNS, and enables per-client configuration — the Microsoft-recommended way since .NET Core 2.1.

### The "Why Not?" Section

| Approach | Rejected because... |
|----------|---------------------|
| `HttpClient` singleton | Socket exhaustion, stale DNS, no per-request config |
| Raw `HttpClient` per call | Socket leaks — factory solves this properly |
| Use the authed client for login | Handler would attach stale tokens / attempt refresh before login |
| `TypedClient<AuthService>` | Named client is fine; typed client adds DI complexity for a thin service |

---

## 1.3 The Delegating Handler — Automatic Auth on Every Call

This is the heart of the auth infrastructure. `clients/BlazorShared/Infrastructure/AuthDelegatingHandler.cs`:

```csharp
public sealed class AuthDelegatingHandler(ITokenStore tokenStore, IHttpClientFactory httpClientFactory) : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenStore.GetAccessTokenAsync();
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var tenant = await tokenStore.GetTenantAsync();
        if (tenant is not null)
        {
            request.Headers.TryAddWithoutValidation("tenant", tenant);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var refresh = await tokenStore.GetRefreshTokenAsync();
            if (refresh is not null)
            {
                await RefreshLock.WaitAsync(cancellationToken);
                try
                {
                    var newToken = await RefreshAccessTokenAsync(token!, refresh, cancellationToken);
                    await tokenStore.SetTokensAsync(newToken.Token, newToken.RefreshToken);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken.Token);

                    // Dispose old response before retry
                    response.Dispose();
                    response = await base.SendAsync(request, cancellationToken);
                }
                catch (Exception ex)
                {
                    await tokenStore.ClearAsync();
                    throw new ApiRequestException(401, $"Session expired: {ex.Message}");
                }
                finally
                {
                    RefreshLock.Release();
                }
            }
        }

        return response;
    }
}
```

### What Happens Step by Step

1. **Attach**: every request gets `Authorization: Bearer <token>` + `tenant` header
2. **Send**: the request goes out
3. **401?** — the access token expired. Ask the token store for a refresh token
4. **Refresh**: POST `/api/v1/identity/token/refresh` with `{ token, refreshToken }` → get new pair
5. **Store + retry**: save the new tokens, re-attach the new access token, send the *original request* again
6. **Failure?** — clear tokens, throw `ApiRequestException(401, "Session expired")` → the UI can catch this and redirect to login

### The Single-Flight Pattern (SemaphoreSlim)

The biggest bug in naive token refresh: **thundering herd**. Imagine the dashboard fires 10 API calls in parallel. All 10 get 401. Without a lock, all 10 would call the refresh endpoint **simultaneously**. The server's refresh token rotation invalidates the old refresh token after first use → 9 of 10 refreshes fail → user gets logged out even though the refresh token was fine.

```csharp
private static readonly SemaphoreSlim RefreshLock = new(1, 1);

await RefreshLock.WaitAsync(cancellationToken);
try { /* refresh */ }
finally { RefreshLock.Release(); }
```

**Effect**: the first 401 acquirer refreshes; the other 9 wait on the lock, then (with the *new* token now in the store) simply retry their original request with a fresh token.

> **🐣 Beginner**: `SemaphoreSlim` = a door with a key. `WaitAsync` = knock and wait for the key. `Release` = hand the key back. With `new(1, 1)` only ONE thread can hold the key at a time.

> **👨‍🔬 Senior Note**: This mirrors the React app's `refreshPromise` single-flight pattern exactly (see `clients/admin/src/api/apiClient.ts`). The lock is `static` so it's shared across all handler instances. Note the subtlety: the request body can only be re-sent if the content is re-created or buffered — for JSON content via `JsonContent`, a fresh `HttpRequestMessage` is safest. Also note the original 401 response is **disposed** before the retry to avoid socket leaks.

> **🧑‍💻 Junior Gotcha**: The tenant header! A refresh call without the `tenant` header would fail with 401/403 in a multi-tenant system. Notice the handler re-reads the tenant from the store on every request — because a user can switch tenants mid-session.

---

## 1.4 The AuthStateProvider — "Am I Logged In?"

Blazor's `AuthenticationStateProvider` answers that question for the whole app. `clients/BlazorShared/Auth/AuthStateProvider.cs`:

```csharp
public sealed class AuthStateProvider(ITokenStore tokenStore) : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> AnonymousState =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
        {
            return await AnonymousState;
        }

        var principal = CreatePrincipal(token);
        return new AuthenticationState(principal);
    }

    public async Task NotifyLoginAsync(string accessToken, string? refreshToken, string tenant)
    {
        await tokenStore.SetTokensAsync(accessToken, refreshToken);
        await tokenStore.SetTenantAsync(tenant);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task NotifyLogoutAsync()
    {
        await tokenStore.ClearAsync();
        NotifyAuthenticationStateChanged(AnonymousState);
    }

    private static ClaimsPrincipal CreatePrincipal(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, "jwt");
        return new ClaimsPrincipal(identity);
    }

    private static bool IsTokenExpired(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.ValidTo <= DateTime.UtcNow.AddSeconds(10);
    }
}
```

### Key Concepts

| Concept | Explanation |
|---------|-------------|
| `ClaimsIdentity` | The "proof" of who you are — a list of claims (name, email, permissions...) |
| `ClaimsPrincipal` | A user that can have multiple identities (e.g., admin + impersonated user) |
| `AuthenticationState` | Wraps the principal; this is what Blazor's `AuthorizeView` and `[Authorize]` read |
| `NotifyAuthenticationStateChanged` | Tells every `AuthorizeView` in the app to re-evaluate **immediately** |

**The 10-second buffer**: `jwt.ValidTo <= DateTime.UtcNow.AddSeconds(10)` — the client treats a token as expired 10 seconds *early*. This avoids edge cases where a token that just expired at the network layer still passes a client-side check (clock skew).

> **🐣 Beginner**: When you log in, `NotifyLoginAsync` stores the tokens and *pings* every component that checks auth. When you log out, `NotifyLogoutAsync` clears everything and pings again. This is why the sidebar switches instantly when you log in/out — no page reload needed.

> **👨‍🔬 Senior Note**: The expiry check is client-side UX only — it prevents loading the app shell for an obviously-dead session. **Security** is still server-side: the API verifies the signature and `exp` on every request. Never trust a client's "I'm logged in" verdict for anything sensitive.

---

## 1.5 The Login Page — Putting It All Together

`clients/admin-blazor/FSH.Admin.Wasm/Pages/Auth/LoginPage.razor` (dashboard has the identical pattern with `"dashboard"` as the app header):

```razor
<MudForm @ref="_form" Validation="@(new DataAnnotationsValidator())">
    <MudTextField @bind-Value="_email" Label="Email"
                  For="@(() => _email)" Required="true"
                  RequiredError="Email is required."
                  Disabled="@_isSubmitting" />
    <MudTextField @bind-Value="_password" Label="Password"
                  For="@(() => _password)" InputType="InputType.Password"
                  Required="true" RequiredError="Password is required."
                  Disabled="@_isSubmitting" Class="mt-4" />
    <MudTextField @bind-Value="_tenant" Label="Tenant"
                  For="@(() => _tenant)"
                  HelperText="Enter 'root' for SuperAdmin access"
                  Disabled="@_isSubmitting" Class="mt-4" />
    <MudButton Variant="Variant.Filled" Color="Color.Primary" FullWidth="true"
               Class="mt-6" Disabled="@_isSubmitting" OnClick="SubmitAsync">
        @if (_isSubmitting)
        {
            <MudProgressCircular Indeterminate="true" Size="Size.Small" Class="mr-2" />
        }
        Sign In
    </MudButton>
</MudForm>
```

And the submit handler:

```csharp
private async Task SubmitAsync()
{
    await _form!.ValidateAsync();
    if (!_form.IsValid) return;

    _isSubmitting = true;
    _error = null;
    try
    {
        var result = await AuthService.LoginAsync(_email, _password, _tenant, "admin");
        await AuthStateProvider.NotifyLoginAsync(result.AccessToken, result.RefreshToken, _tenant);
        await PermissionsProvider.InvalidateCache();

        var returnUrl = Nav.TryGetQueryString<string>("returnUrl", out var url) && url is not null ? url : "/";
        Nav.NavigateTo(returnUrl);
    }
    catch (HttpRequestException ex)
    {
        _error = ex.StatusCode is not null
            ? $"Login failed ({(int)ex.StatusCode})"
            : "Unable to connect to server. Please try again.";
    }
    catch (Exception ex)
    {
        _error = ex.Message;
    }
    finally
    {
        _isSubmitting = false;
    }
}
```

### The 6-Step Login Sequence

| # | Step | What happens | Why |
|---|------|-------------|-----|
| 1 | **Validate** | `_form.ValidateAsync()` | Don't call the API with empty fields |
| 2 | **Submit** | `LoginAsync(email, password, "root", "admin")` | `X-FSH-App: admin` tells the API this is the admin app |
| 3 | **Notify** | `NotifyLoginAsync(...)` | Stores tokens + tenant, pings `AuthorizeView` components |
| 4 | **Invalidate** | `PermissionsProvider.InvalidateCache()` | Force fresh permissions from the API (don't trust stale cache) |
| 5 | **Redirect** | `Nav.NavigateTo(returnUrl ?? "/")` | Send the user where they were headed before login |
| 6 | **Reset** | `finally { _isSubmitting = false; }` | Always re-enable the button — even on error |

> **🐣 Beginner**: Notice the **try/catch/finally** trio — this is the golden pattern for any async operation in Blazor. `try` = attempt, `catch` = handle failure (show error message), `finally` = cleanup (re-enable the button). **Always** reset your loading state in `finally`, not at the end of `try`.

> **🧑‍💻 Junior Gotcha**: `returnUrl` — the classic open-redirect vector. `Nav.TryGetQueryString` gives us the raw value; on the API side this must be validated (only allow relative paths, never `http://evil.com`). For this phase, the client-side redirect is safe because we only navigate within the SPA.

> **👨‍🔬 Senior Note**: Why `InvalidateCache()` instead of just trusting the fresh login? Because the permission cache is app-lifetime in-memory. If user A logs out and user B logs in (same SPA session), B would inherit A's cached permissions. `InvalidateCache` forces the next permission read to hit the API with B's fresh token.

---

## 1.6 The PermissionsProvider — 3-Layer Permission Cache

Permissions gate what the UI shows (buttons, routes, menu items). Fetching them from the API on every render would be wasteful. `clients/BlazorShared/Auth/PermissionsProvider.cs`:

```csharp
public sealed class PermissionsProvider(IAuthService authService, ITokenStore tokenStore, ILogger<PermissionsProvider> logger) : IPermissionsProvider
{
    private string[]? _cached;
    private readonly object _lock = new();

    public bool IsHydrated => Volatile.Read(ref _cached) is not null;

    public async Task<string[]> GetPermissionsAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_cached is not null)
                return _cached;
        }

        var stored = await tokenStore.GetPermissionsAsync();
        if (stored is not null)
        {
            lock (_lock) _cached = stored;
            return stored;
        }

        try
        {
            var permissions = await authService.GetPermissionsAsync(ct);
            await tokenStore.SetPermissionsAsync(permissions);
            lock (_lock) _cached = permissions;
            return permissions;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch permissions");
            return [];
        }
    }

    public Task InvalidateCache()
    {
        lock (_lock) _cached = null;
        return Task.CompletedTask;
    }
}
```

### The 3 Layers

```
Layer 1: In-memory (_cached)          ← fastest, app lifetime
   │  miss (null)
Layer 2: localStorage (token store)    ← survives page refresh
   │  miss (null)
Layer 3: API (authService)             ← always source of truth
   │  success → cache into layer 1 AND layer 2
```

| Layer | Speed | Survives refresh? | Cost |
|-------|-------|-------------------|------|
| 1. In-memory `_cached` | Instant | ❌ | 0 network, 0 IO |
| 2. `localStorage` via token store | ~1ms | ✅ | 1 JS interop call |
| 3. `GET /api/v1/identity/permissions` | ~50-200ms | ✅ (persisted after fetch) | 1 network call |

> **🐣 Beginner**: It's like a water cooler with three levels of access: check your desk drawer (memory) → check the fridge downstairs (localStorage) → walk to the shop (API). You only walk to the shop when both closer options are empty.

> **👨‍🔬 Senior Note**: `lock (_lock)` guards the cache with **double-checked locking** — cheap lock-free reads (layer 1 hit) with the lock only held during mutation. `Volatile.Read` ensures a fresh read of `_cached` without full locking. This is a textbook concurrent-cache pattern. On **failure**, we return `[]` (no permissions) rather than throw — failing closed means the UI shows nothing restricted rather than crashing. Log it, degrade gracefully.

---

## 1.7 The Permission Gate — Hiding UI by Permission

Permissions are strings like `"Permissions.Users.View"`, `"Permissions.Roles.Create"`. The `FshPermissionGate` component wraps content and shows it only if the user has one of the required permissions:

```razor
<FshPermissionGate Perms="@(new[] { MultitenancyPermissions.Tenants.View })">
    <MudButton Color="Color.Primary">Create Tenant</MudButton>
</FshPermissionGate>
```

| Constant class | Permissions it describes |
|----------------|--------------------------|
| `IdentityPermissions` (`clients/BlazorShared/Permissions/IdentityPermissions.cs`) | Users + Roles: View, Search, Create, Update, Delete, Export |
| `MultitenancyPermissions` (`clients/BlazorShared/Permissions/MultitenancyPermissions.cs`) | Tenants: View, Create, Update, Upgrade, Disable, Enable |

> **🐣 Beginner**: Think of permissions as keys to doors. `FshPermissionGate` checks "does the user's keychain contain the key for this door?" If yes → show the door (content). If no → show nothing.

> **👨‍🔬 Senior Note**: Permission-gating the UI is **UX, not security**. The real enforcement happens server-side on every endpoint (`[Authorize(Policy = ...)]`). A savvy user can call the API directly — the UI gate just keeps the UI honest and uncluttered. Always state this in design reviews: **client gates = cosmetics, server gates = security**.

---

## 1.8 The Supporting Auth Pages

### ForgotPasswordPage — the "reset request" flow

```
User enters email + tenant
    → POST /api/v1/identity/forgot-password { email }  (tenant header)
    → API sends reset email with a token link
    → UI shows "Check your email" success alert
```

Key snippet (from `clients/admin-blazor/FSH.Admin.Wasm/Pages/Auth/ForgotPasswordPage.razor`):

```csharp
try
{
    await AuthService.ForgotPasswordAsync(_email, _tenant);
    _success = true;   // show the "check your email" alert
}
catch (HttpRequestException ex) { _error = ...; }
```

> **🐣 Beginner**: **Security by design**: the API *always* returns success-ish even if the email doesn't exist. Otherwise attackers could enumerate valid emails ("that email isn't registered" = fishing net). Never reveal whether an email exists.

### ResetPasswordPage — reads the emailed link's query params

```csharp
protected override void OnInitialized()
{
    var email = Nav.TryGetQueryString<string>("email", out var e) ? e : null;
    var token = Nav.TryGetQueryString<string>("token", out var t) ? t : null;
    var tenant = Nav.TryGetQueryString<string>("tenant", out var tn) ? tn : null;
    // Prefill hidden fields with email + token — user only types new password
}
```

The email contains a link like:
```
https://localhost:5175/reset-password?email=alice%40example.com&token=eyJhbGciOiJ...&tenant=root
```

> **🧑‍💻 Junior Gotcha**: The token contains URL-unsafe characters (`+`, `/`, `=`) → always `Uri.EscapeDataString` when building the link, and query-string parsing handles the unescape. This is why we use the `NavigationExtensions.TryGetQueryString<T>()` helper rather than hand-rolling string splitting.

### ConfirmEmailPage — auto-runs on load

```csharp
protected override async Task OnInitializedAsync()
{
    var userId = ...; var code = ...; var tenant = ...;
    var result = await AuthService.ConfirmEmailAsync(userId, code, tenant);
    // show success state with "Go to login" button
}
```

Three visual states: ⏳ loading (spinner) → ✅ success (green alert + login link) → ❌ error (red alert + resend instructions).

---

## 1.9 The Route Guard — Protecting Pages

`clients/admin-blazor/FSH.Admin.Wasm/App.razor`:

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(Program).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    <RedirectToLogin />
                </NotAuthorized>
                <Authorizing>
                    <div class="d-flex justify-center align-center" style="min-height: 100vh;">
                        <MudProgressCircular Indeterminate="true" />
                    </div>
                </Authorizing>
            </AuthorizeRouteView>
        </Found>
        <NotFound>...</NotFound>
    </Router>
</CascadingAuthenticationState>
```

| Part | Role |
|------|------|
| `CascadingAuthenticationState` | Makes the auth state available to every component below |
| `AuthorizeRouteView` | Checks `[Authorize]` on the page before rendering it |
| `NotAuthorized` → `RedirectToLogin` | Anonymous user → bounce to `/login?returnUrl=<current>` |
| `Authorizing` | While the token is being read/decode — show spinner (prevents layout flash) |

> **🐣 Beginner**: `[Authorize]` on a page = "only logged-in users may enter." `[AllowAnonymous]` on the login page = "everyone may enter, even logged-in users." The login page MUST be `[AllowAnonymous]` or you'd be locked out of the login page forever.

> **👨‍🔬 Senior Note**: `RedirectToLogin` preserves `returnUrl` (via `NavigationManager.ToAbsoluteUri` + query string) so users land back on the page they originally tried to visit. See `clients/BlazorShared/Infrastructure/RedirectToLogin.cs`.

---

## 1.10 Cross-Tab Logout — One Tab Signs Out, All Tabs Follow

Logout in tab A must log out tab B (they share `localStorage`). The `storage` event fires in *other* tabs when localStorage changes:

```csharp
// App.razor.cs (simplified)
private async Task OnStorageEventAsync(string key, string? newValue)
{
    if (key == AdminTokenStore.AccessTokenKey && string.IsNullOrEmpty(newValue))
    {
        await _authStateProvider.NotifyLogoutAsync();
        _nav.NavigateTo("/login?signedOut=You were signed out in another tab.");
    }
}
```

Registered via JS interop in `App.razor.cs` using `IJSRuntime` to add a `storage` event listener in `main.js`.

> **🐣 Beginner**: The `storage` event is a browser feature: when tab A changes localStorage, every *other* tab of the same origin fires the event. It does NOT fire in tab A itself. That's the "cross-tab" magic.

> **👨‍🔬 Senior Note**: Filter by key (`fsh.admin.accessToken`) and check for `null`/empty new value — only a *clear* triggers logout. This also handles the edge case where a session expires in one tab: the refresh failure clears the store → `storage` event → all tabs redirect. This is exactly what the React apps do with a `storage` listener, so behavior is identical across stacks.

---

## 1.11 Error Handling Strategy

| Failure | Where caught | What the user sees |
|---------|--------------|--------------------|
| Wrong credentials (401) | `SubmitAsync` catch | `Login failed (401)` in a red `MudAlert` |
| API down (network) | `SubmitAsync` catch | `Unable to connect to server. Please try again.` |
| Expired session mid-app | `AuthDelegatingHandler` → `ApiRequestException(401)` | Session cleared → login page with reason |
| Refresh token invalid | `AuthDelegatingHandler` catch → `ClearAsync()` | Logged out, redirected to login |
| Permission fetch fails | `PermissionsProvider` catch → `[]` | UI shows no restricted content, app still works |

**The pattern**: every async interaction has (1) a loading state, (2) an error state, (3) a success state. Never leave the UI mid-state.

---

## 1.12 Files Reference (What You Just Learned)

| File | Responsibility |
|------|----------------|
| `clients/BlazorShared/Auth/ITokenStore.cs` | Token storage contract (access, refresh, tenant, permissions) |
| `clients/BlazorShared/Auth/AdminTokenStore.cs` | localStorage impl, `fsh.admin.*` keys |
| `clients/BlazorShared/Auth/DashboardTokenStore.cs` | localStorage impl, `fsh.dashboard.*` keys + impersonation stash/restore |
| `clients/BlazorShared/Auth/AuthStateProvider.cs` | JWT decode → `ClaimsPrincipal`, login/logout notification |
| `clients/BlazorShared/Auth/PermissionsProvider.cs` | 3-layer permission cache (memory → localStorage → API) |
| `clients/BlazorShared/Services/IAuthService.cs` | Auth API contract (login, refresh, permissions, forgot, reset, confirm) |
| `clients/BlazorShared/Services/AuthService.cs` | Auth API implementation (named client `FSH.Auth`, no handler) |
| `clients/BlazorShared/Infrastructure/AuthDelegatingHandler.cs` | Attach token/tenant; 401 → single-flight refresh → retry |
| `clients/BlazorShared/Infrastructure/NavigationExtensions.cs` | `TryGetQueryString<T>()` safe query parsing |
| `clients/BlazorShared/Infrastructure/RedirectToLogin.cs` | Unauthorized → `/login?returnUrl=...` |
| `clients/BlazorShared/Permissions/IdentityPermissions.cs` | User/Role permission constants |
| `clients/BlazorShared/Permissions/MultitenancyPermissions.cs` | Tenant permission constants |
| `clients/admin-blazor/FSH.Admin.Wasm/Pages/Auth/LoginPage.razor` | Admin login form |
| `clients/admin-blazor/FSH.Admin.Wasm/Pages/Auth/ForgotPasswordPage.razor` | Forgot password form |
| `clients/admin-blazor/FSH.Admin.Wasm/Pages/Auth/ResetPasswordPage.razor` | Reset password (reads email link) |
| `clients/admin-blazor/FSH.Admin.Wasm/Pages/Auth/ConfirmEmailPage.razor` | Auto-confirm email |
| `clients/dashboard-blazor/FSH.Dashboard.Wasm/Pages/Auth/LoginPage.razor` | Dashboard login (`X-FSH-App: dashboard`) |
| `clients/dashboard-blazor/FSH.Dashboard.Wasm/Pages/Auth/ForgotPasswordPage.razor` | Dashboard forgot password |
| `clients/dashboard-blazor/FSH.Dashboard.Wasm/Pages/Auth/ResetPasswordPage.razor` | Dashboard reset password |
| `clients/dashboard-blazor/FSH.Dashboard.Wasm/Pages/Auth/ConfirmEmailPage.razor` | Dashboard confirm email |

---

## 1.13 Free Learning Resources

### Beginner Path (no experience)

| Resource | What you'll learn | Time |
|----------|-------------------|------|
| [Microsoft: ASP.NET Core Blazor Authentication](https://learn.microsoft.com/aspnet/core/blazor/security) | Official docs — auth fundamentals in Blazor | 2-4 h |
| [Microsoft: JSON Web Tokens intro](https://jwt.io/introduction) | What JWTs are, header/payload/signature | 30 min |
| [jwt.io Debugger](https://jwt.io/) | Paste any JWT, see its claims decoded | 10 min |
| [CodeProject: JWT Explained Simply](https://www.codeproject.com/Articles/1265737/A-simple-explanation-of-JWT) | JWT without the math | 30 min |
| [MudBlazor Docs: MudForm](https://mudblazor.com/components/form) | Forms, validation, `@ref`, `For=()` | 1 h |
| [MudBlazor Docs: MudAlert/MudProgressCircular](https://mudblazor.com/components/alert) | Status UI components | 30 min |

### Junior Path (knows some C#)

| Resource | What you'll learn | Time |
|----------|-------------------|------|
| [Microsoft: IHttpClientFactory](https://learn.microsoft.com/aspnet/core/fundamentals/http-requests) | Named clients, handlers, why not to new up HttpClient | 1-2 h |
| [Microsoft: Custom DelegatingHandler](https://learn.microsoft.com/dotnet/api/system.net.http.delegatinghandler) | The auth-handler pattern (401→refresh is the classic use case) | 1 h |
| [Learn ASP.NET: Token-based auth in Blazor WASM](https://learn.microsoft.com/aspnet/core/blazor/security/webassembly/standalone-with-authentication-library) | The official WASM auth approach (even if we do custom, the concepts map) | 2 h |
| [OWASP: Session Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html) | What can go wrong with sessions, cookies vs localStorage | 1 h |
| [MudBlazor Docs: MudButton/MudTextField](https://mudblazor.com/components/button) | The building blocks of the login form | 30 min |

### Senior Path (production patterns)

| Resource | What you'll learn | Time |
|----------|-------------------|------|
| [Microsoft: Refresh tokens in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/refresh-tokens) | Rotation, revocation, reuse detection | 1-2 h |
| [Auth0: Token refresh single-flight](https://auth0.com/blog/refresh-tokens-what-are-they-and-when-to-use-them/) | The thundering-herd problem and real-world refresh strategies | 1 h |
| [OWASP: JWT Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html) | alg=none attacks, signature confusion, key confusion | 1 h |
| [PortSwigger: JWT vulnerabilities](https://portswigger.net/web-security/jwt) | Practical attack labs against bad JWT implementations | 2-3 h |
| [Microsoft: Auth State in Blazor](https://learn.microsoft.com/aspnet/core/blazor/security/) | `CascadingAuthenticationState`, `AuthorizeRouteView`, custom providers | 1-2 h |

---

## 1.14 Chapter Quiz

1. **Why does the client use a named HttpClient "FSH.Auth" WITHOUT the AuthDelegatingHandler for login?**
2. **What problem does the `SemaphoreSlim(1, 1)` in AuthDelegatingHandler solve?** (Hint: 10 parallel calls, all get 401...)
3. **What are the three layers of the PermissionsProvider cache, in order of speed?**
4. **Why do admin and dashboard use different localStorage key prefixes (`fsh.admin.*` vs `fsh.dashboard.*`)?**
5. **What does `NotifyAuthenticationStateChanged` do, and why does the login page need to call it?**
6. **Why does `IsTokenExpired` add 10 seconds of buffer to `jwt.ValidTo`?**
7. **The API returns "success" for forgot-password even when the email doesn't exist. Why?**
8. **Where does the actual security enforcement live — the `FshPermissionGate` component or the server?**
9. **What happens in `AuthDelegatingHandler` when the refresh call itself fails?**
10. **What is the `X-FSH-App` header used for, and why is it critical?**

<details>
<summary>Click for answers</summary>

1. Login is anonymous — attaching a token/attempting refresh during login would break it. Separate "anonymous" and "authed" clients.
2. The thundering herd: without the lock, all 10 calls would hit the refresh endpoint simultaneously; refresh token rotation invalidates the old one, so 9 would fail and the user would be logged out.
3. In-memory `_cached` → localStorage via token store → API.
4. Both apps share the same browser origin (localhost). Distinct prefixes prevent the dashboard reading the admin's token.
5. It tells every `AuthorizeView`/`AuthorizeRouteView` to re-evaluate. Without it, the UI wouldn't know the user logged in until a full reload.
6. Clock skew and network-latency edge cases: a token that expires "right now" might still fail at the API, so we treat it as expired 10s early.
7. To prevent account enumeration — attackers must not be able to learn which emails are registered.
8. The server. UI gates are cosmetic; the API enforces permissions on every endpoint.
9. It clears the token store and throws `ApiRequestException(401, "Session expired")` — the UI redirects to login.
10. It tells the API which app (admin vs dashboard) issued the request, enabling app-specific policy enforcement (e.g., root tenant cannot log into the dashboard).
</details>

---

## 1.15 What's Next

**Phase 2 — Admin Feature Pages.** You'll apply this chapter's patterns at scale: Tenants, Users, Roles list+create pages with MudTable, FshPageHeader, FshPermissionGate, and the `PagedResult<T>` search flow — the "admin CRUD" pattern that dominates the rest of the codebase.

---

## Corrections (Phase 7 parity sprint)

The following supersede anything this chapter says about theming and the shell:

- **Theme keys changed.** Dashboard now registers `FshThemeService` with key **`fsh.theme`** and default **`ThemeMode.System`** (Light/Dark/System menu, OS-following) — NOT `fsh.dashboard.theme` (which never existed in code). Admin keeps binary `fsh.admin.theme` (default Dark). Stored values are the raw strings `"light" | "dark" | "system"` — the same keys the React apps use.
- **Sidebar is the React-parity accordion shell.** Both apps render a custom `<aside class="fsh-sidebar">` with `FshNavSection` accordions (single-select, route re-sync, collapsed 52px icon-stack mode). Persistence keys: `fsh.admin.sidebar.collapsed` / `fsh.sidebar.collapsed` with `"true"/"false"` (was `"1"/"0"`).
- **`<base href="/" />` must be the first element in `<head>`** in both `index.html` — a `<base>` placed after the `<link>` tags breaks every full-page load at a sub-route (CSS/JS resolve against the document URL → 404s). Guarded by `IndexHtmlGuardTests`.
- localStorage prefixes: admin `fsh.admin.*`, dashboard `fsh.dashboard.*` — except the theme key `fsh.theme` (shared with React).
