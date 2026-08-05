using FSH.BlazorShared.Auth;
using Microsoft.JSInterop;

namespace FSH.Admin.Wasm.Auth;

public sealed class AdminTokenStore(IJSRuntime js) : ITokenStore
{
    private const string Prefix = "fsh.admin";
    private const string AccessKey = $"{Prefix}.accessToken";
    private const string RefreshKey = $"{Prefix}.refreshToken";
    private const string TenantKey = $"{Prefix}.tenant";
    private const string PermissionsKey = $"{Prefix}.permissions";

    public event Action? TokensChanged;

    public async Task<string?> GetAccessTokenAsync()
        => await GetItemAsync(AccessKey);

    public async Task<string?> GetRefreshTokenAsync()
        => await GetItemAsync(RefreshKey);

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

    public async Task<string?> GetTenantAsync()
        => await GetItemAsync(TenantKey);

    public async Task SetTenantAsync(string? tenant)
        => await SetItemAsync(TenantKey, tenant);

    public async Task<string[]?> GetPermissionsAsync()
    {
        var json = await GetItemAsync(PermissionsKey);
        return json is not null ? System.Text.Json.JsonSerializer.Deserialize<string[]>(json) : null;
    }

    public async Task SetPermissionsAsync(string[] permissions)
        => await SetItemAsync(PermissionsKey, System.Text.Json.JsonSerializer.Serialize(permissions));

    public async Task ClearPermissionsAsync()
        => await RemoveItemAsync(PermissionsKey);

    // Impersonation never runs inside the admin app - the operator hands the session to
    // the dashboard app via URL hash (BuildHandoffUrl), so there is no stash to check.
    public Task<bool> HasImpersonationStashAsync() => Task.FromResult(false);

    public Task SetFreshTokensAsync(string accessToken, string? refreshToken)
        => SetTokensAsync(accessToken, refreshToken);

    public Task RestoreTokensAsync() => Task.CompletedTask;

    private async Task<string?> GetItemAsync(string key)
        => await js.InvokeAsync<string?>("localStorage.getItem", key);

    private async Task SetItemAsync(string key, string? value)
    {
        if (value is null)
            await js.InvokeVoidAsync("localStorage.removeItem", key);
        else
            await js.InvokeVoidAsync("localStorage.setItem", key, value);
    }

    private async Task RemoveItemAsync(string key)
        => await js.InvokeVoidAsync("localStorage.removeItem", key);
}
