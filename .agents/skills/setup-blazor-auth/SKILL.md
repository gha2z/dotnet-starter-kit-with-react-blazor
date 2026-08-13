---
name: setup-blazor-auth
description: Wire up per-app JWT auth for a Blazor WASM app — ITokenStore (localStorage, no eval) + AuthStateProvider + AuthDelegatingHandler + PermissionsProvider. Use when setting up auth in a new Blazor WASM project or adding auth to an existing one.
argument-hint: "[admin|dashboard]"
---

# Setup Blazor Auth

Read `.agents/rules/frontend/blazor-shared.md` and the app-specific file (`blazor-admin.md` / `blazor-dashboard.md`) first.

This skill covers the **WASM apps** (`admin-blazor`, `dashboard-blazor`). The **MAUI Hybrid** app
uses the same `ITokenStore` contract but a native `MauiTokenStore` backed by `SecureStorage` — see
`.agents/rules/frontend/maui-hybrid.md`.

Each WASM app owns a **per-app token store** (`AdminTokenStore` / `DashboardTokenStore`) — there is
no shared `TokenStore` class. Keys are prefixed per app (`fsh.admin.*` / `fsh.dashboard.*`).

## Step 1 — ITokenStore (`BlazorShared/Auth/ITokenStore.cs`)

```csharp
public interface ITokenStore
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SetTokensAsync(string accessToken, string? refreshToken);
    Task ClearAsync();
    Task<string?> GetTenantAsync();
    Task SetTenantAsync(string? tenant);
    Task<string[]?> GetPermissionsAsync();
    Task SetPermissionsAsync(string[] permissions);
    Task ClearPermissionsAsync();
    event Action? TokensChanged;
}
```

## Step 2 — Per-app token store with modern JS interop

**Never use `eval`.** Invoke the `localStorage` API directly by name (as the real stores do):

```csharp
// admin-blazor/FSH.Admin.Wasm/Auth/AdminTokenStore.cs
public sealed class AdminTokenStore(IJSRuntime js) : ITokenStore
{
    private const string Prefix = "fsh.admin";
    private const string AccessKey = $"{Prefix}.accessToken";
    private const string RefreshKey = $"{Prefix}.refreshToken";
    private const string TenantKey = $"{Prefix}.tenant";
    private const string PermissionsKey = $"{Prefix}.permissions";

    public event Action? TokensChanged;

    public async Task<string?> GetAccessTokenAsync() => await GetItemAsync(AccessKey);
    public async Task<string?> GetRefreshTokenAsync() => await GetItemAsync(RefreshKey);

    public async Task SetTokensAsync(string accessToken, string? refreshToken)
    {
        await SetItemAsync(AccessKey, accessToken);
        await SetItemAsync(RefreshKey, refreshToken);
        TokensChanged?.Invoke();
    }

    public async Task ClearAsync()
    {
        await RemoveItemAsync(AccessKey);
        await RemoveItemAsync(RefreshKey);
        await RemoveItemAsync(TenantKey);
        await RemoveItemAsync(PermissionsKey);
        TokensChanged?.Invoke();
    }

    public Task<string?> GetTenantAsync() => GetItemAsync(TenantKey);
    public Task SetTenantAsync(string? tenant) => SetItemAsync(TenantKey, tenant);

    public async Task<string[]?> GetPermissionsAsync()
    {
        var json = await GetItemAsync(PermissionsKey);
        return json is not null ? System.Text.Json.JsonSerializer.Deserialize<string[]>(json) : null;
    }

    public Task SetPermissionsAsync(string[] permissions)
        => SetItemAsync(PermissionsKey, System.Text.Json.JsonSerializer.Serialize(permissions));

    public Task ClearPermissionsAsync() => RemoveItemAsync(PermissionsKey);

    private Task<string?> GetItemAsync(string key) => js.InvokeAsync<string?>("localStorage.getItem", key);

    private async Task SetItemAsync(string key, string? value)
    {
        if (value is null)
            await js.InvokeVoidAsync("localStorage.removeItem", key);
        else
            await js.InvokeVoidAsync("localStorage.setItem", key, value);
    }

    private Task RemoveItemAsync(string key) => js.InvokeVoidAsync("localStorage.removeItem", key);
}
```

`DashboardTokenStore` is identical but with `Prefix = "fsh.dashboard"` plus **impersonation stashing**:
`StashTokensAsync()` saves the real tokens to `fsh.dashboard.impersonation.*` before login-as; `RestoreTokensAsync()` swaps them back.

JS-interop note: direct `localStorage.*` invocation is the established pattern here. If you add other
interop, prefer a typed wrapper or collocated `.razor.js` module over raw string calls — see the
`use-js-interop` skill. The theme service already imports a module (`./_content/FSH.BlazorShared/js/fshTheme.js`).

## Step 3 — AuthDelegatingHandler (`BlazorShared/Infrastructure/AuthDelegatingHandler.cs`)

Adds `Bearer` + `tenant` headers, single-flights a 401 refresh via the **anonymous** named client
`FSH.Auth`, disposes the old response before retrying, and clears the session on refresh failure:

```csharp
public sealed class AuthDelegatingHandler(ITokenStore tokenStore, IHttpClientFactory httpClientFactory) : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenStore.GetAccessTokenAsync();
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var tenant = await tokenStore.GetTenantAsync();
        if (tenant is not null)
            request.Headers.TryAddWithoutValidation("tenant", tenant);

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

                    response.Dispose(); // dispose old response before retry
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

    private async Task<RefreshResponse> RefreshAccessTokenAsync(string token, string refreshToken, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient("FSH.Auth"); // no auth handler — login/refresh only
        var tenant = await tokenStore.GetTenantAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/token/refresh")
        {
            Content = JsonContent.Create(new { token, refreshToken }),
        };
        if (tenant is not null)
            request.Headers.TryAddWithoutValidation("tenant", tenant);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RefreshResponse>(ct)
               ?? throw new InvalidOperationException("Null refresh response");
    }

    private sealed record RefreshResponse(string Token, string? RefreshToken);
}

public sealed class ApiRequestException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
```

## Step 4 — AuthStateProvider (`BlazorShared/Auth/AuthStateProvider.cs`)

Builds the principal from the JWT, normalizes role claims, and hydrates **permission claims** via
`IPermissionsProvider` (so `[Authorize(Policy=…)]` and `<FshPermissionGate>` work):

```csharp
public sealed class AuthStateProvider(ITokenStore tokenStore, IPermissionsProvider permissionsProvider)
    : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> AnonymousState =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
            return await AnonymousState;

        var principal = await CreatePrincipalAsync(token);
        return new AuthenticationState(principal);
    }

    public async Task NotifyLoginAsync(string accessToken, string? refreshToken, string tenant)
    {
        await tokenStore.SetTokensAsync(accessToken, refreshToken);
        await tokenStore.SetTenantAsync(tenant);
        await permissionsProvider.ResetAsync();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task NotifyLogoutAsync()
    {
        await tokenStore.ClearAsync();
        await permissionsProvider.ResetAsync();
        NotifyAuthenticationStateChanged(AnonymousState);
    }

    // Re-evaluates gates/nav after a permission change (e.g. impersonation) without re-login.
    public async Task RefreshAsync()
    {
        await permissionsProvider.InvalidateCache();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(string token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, "jwt");

        foreach (var roleClaim in identity.FindAll(c => c.Type is "role" or "roles" or ClaimTypes.Role))
            if (roleClaim.Type != ClaimTypes.Role)
                identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));

        foreach (var permission in await permissionsProvider.GetPermissionsAsync())
            identity.AddClaim(new Claim("permission", permission));

        return new ClaimsPrincipal(identity);
    }

    private static bool IsTokenExpired(string token)
        => new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo <= DateTime.UtcNow.AddSeconds(10);
}
```

## Step 5 — PermissionsProvider (`BlazorShared/Auth/PermissionsProvider.cs`)

`IPermissionsProvider.GetPermissionsAsync()` resolves in order: in-memory cache → persisted
`localStorage` copy → server (`authService.GetPermissionsAsync`), then caches the server result.
`InvalidateCache()` drops only memory; `ResetAsync()` drops memory **and** the persisted copy
(login-time reset).

## Step 6 — Register in Program.cs

```csharp
// Admin (FSH.Admin.Wasm) — dashboard swaps in DashboardTokenStore and has no [Authorize] policies
builder.Services.AddSingleton<ITokenStore>(sp =>
    new AdminTokenStore(sp.GetRequiredService<IJSRuntime>()));
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateProvider>());
builder.Services.AddScoped<IPermissionsProvider, PermissionsProvider>();

// Anonymous client — login/refresh/token endpoints only (no auth handler)
builder.Services.AddHttpClient("FSH.Auth", (sp, client) =>
{
    var config = sp.GetRequiredService<IRuntimeConfigService>();
    client.BaseAddress = RuntimeConfigService.ResolveApiBase(baseAddress, config.ApiBaseUrl);
});

builder.Services.AddScoped<IAuthService, AuthService>();

// Authenticated client — every other API call
builder.Services.AddTransient<AuthDelegatingHandler>();
builder.Services.AddHttpClient("FSH.Api", (sp, client) =>
{
    var config = sp.GetRequiredService<IRuntimeConfigService>();
    client.BaseAddress = RuntimeConfigService.ResolveApiBase(baseAddress, config.ApiBaseUrl);
})
.AddHttpMessageHandler<AuthDelegatingHandler>();
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("FSH.Api"));

// Admin-only: authorization policies per permission claim (dashboard uses AddAuthorizationCore() bare)
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("Permissions.Users.View", p => p.RequireClaim("permission", "Permissions.Users.View"));
    // … one policy per permission used by gates/nav
});
```

## Validation

- [ ] Login flow works end-to-end: form → `IAuthService` → token stored in localStorage → `AuthStateProvider` notifies → redirect
- [ ] Token refresh on 401 works (verify via expired token); old response is disposed before the retry
- [ ] Refresh failure clears tokens and surfaces `ApiRequestException(401, …)`
- [ ] Logout clears tokens + permissions and redirects
- [ ] `AuthStateProvider.GetAuthenticationStateAsync()` returns the correct state on page refresh
- [ ] Permission claims hydrated (gates/nav re-evaluate) after login and after impersonation (`RefreshAsync`)
