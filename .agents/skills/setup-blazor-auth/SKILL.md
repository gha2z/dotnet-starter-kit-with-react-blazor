---
name: setup-blazor-auth
description: Wire up AuthenticationStateProvider + ITokenStore + AuthDelegatingHandler for a Blazor WASM app. Use when setting up auth in a new Blazor WASM project or adding auth to an existing one.
argument-hint: "[admin|dashboard]"
---

# Setup Blazor Auth

Read `.agents/rules/frontend/blazor-shared.md` and the app-specific file (`blazor-admin.md` / `blazor-dashboard.md`) first.

## Step 1 — ITokenStore (JS interop for localStorage)

```csharp
// BlazorShared/Auth/ITokenStore.cs
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
    event Action? TokensChanged;
}

// BlazorShared/Auth/TokenStore.cs
public sealed class TokenStore(IJSRuntime js, string storagePrefix) : ITokenStore
{
    private const string AccessKey = "{prefix}.accessToken";
    private const string RefreshKey = "{prefix}.refreshToken";
    // …

    public async Task<string?> GetAccessTokenAsync()
        => await js.InvokeAsync<string?>("eval", $"localStorage.getItem('{_storagePrefix}.accessToken')");
    // …
}
```

## Step 2 — AuthDelegatingHandler

```csharp
// BlazorShared/Infrastructure/AuthDelegatingHandler.cs
public sealed class AuthDelegatingHandler(ITokenStore tokenStore) : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await tokenStore.GetAccessTokenAsync();
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var tenant = await tokenStore.GetTenantAsync();
        if (tenant is not null)
            request.Headers.TryAddWithoutValidation("tenant", tenant);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var refresh = await tokenStore.GetRefreshTokenAsync();
            if (refresh is not null)
            {
                await RefreshLock.WaitAsync(ct);
                try
                {
                    // Single-flight refresh
                    var newToken = await RefreshAccessTokenAsync(token!, refresh, ct);
                    await tokenStore.SetTokensAsync(newToken.AccessToken, newToken.RefreshToken);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken.AccessToken);
                    response = await base.SendAsync(request, ct);
                }
                finally { RefreshLock.Release(); }
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
            throw new ApiRequestException(response.StatusCode, problem?.Detail ?? "Request failed", problem);
        }

        return response;
    }
}
```

## Step 3 — AuthenticationStateProvider

```csharp
// BlazorShared/Auth/AuthStateProvider.cs
public sealed class AuthStateProvider(ITokenStore tokenStore, HttpClient http) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
        {
            // Try silent refresh
            var refresh = await tokenStore.GetRefreshTokenAsync();
            if (refresh is not null)
            {
                try { /* refresh and retry */ }
                catch { return AnonymousState(); }
            }
            return AnonymousState();
        }

        var claims = DecodeJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
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
        NotifyAuthenticationStateChanged(Task.FromResult(AnonymousState()));
    }
}
```

## Step 4 — Register in Program.cs

```csharp
// Admin: storagePrefix = "fsh.admin"
// Dashboard: storagePrefix = "fsh.dashboard"
builder.Services.AddSingleton<ITokenStore>(sp =>
    new TokenStore(sp.GetRequiredService<IJSRuntime>(), "fsh.admin"));

builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateProvider>());

builder.Services.AddTransient<AuthDelegatingHandler>();
builder.Services.AddHttpClient("FSH", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<AuthDelegatingHandler>();
```

## Validation

- [ ] Login flow works end-to-end: form → API → token stored → redirect
- [ ] Token refresh on 401 works (verify via expired token)
- [ ] Logout clears tokens and redirects
- [ ] Cross-tab logout synced via `storage` event listener
- [ ] `AuthStateProvider.GetAuthenticationStateAsync()` returns correct state on page refresh
