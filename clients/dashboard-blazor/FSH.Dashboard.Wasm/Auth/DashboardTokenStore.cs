using FSH.BlazorShared.Auth;
using Microsoft.JSInterop;

namespace FSH.Dashboard.Wasm.Auth;

public sealed class DashboardTokenStore(IJSRuntime js) : ITokenStore
{
    private const string Prefix = "fsh.dashboard";
    private const string AccessKey = $"{Prefix}.accessToken";
    private const string RefreshKey = $"{Prefix}.refreshToken";
    private const string TenantKey = $"{Prefix}.tenant";
    private const string PermissionsKey = $"{Prefix}.permissions";
    private const string ImpersonationAccessKey = $"{Prefix}.impersonation.accessToken";
    private const string ImpersonationRefreshKey = $"{Prefix}.impersonation.refreshToken";

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
        await RemoveItemAsync(ImpersonationAccessKey);
        await RemoveItemAsync(ImpersonationRefreshKey);
        TokensChanged?.Invoke();
    }

    public Task<string?> GetTenantAsync()
        => GetItemAsync(TenantKey);

    public Task SetTenantAsync(string? tenant)
        => SetItemAsync(TenantKey, tenant);

    public async Task<string[]?> GetPermissionsAsync()
    {
        var json = await GetItemAsync(PermissionsKey);
        return json is not null ? System.Text.Json.JsonSerializer.Deserialize<string[]>(json) : null;
    }

    public Task SetPermissionsAsync(string[] permissions)
        => SetItemAsync(PermissionsKey, System.Text.Json.JsonSerializer.Serialize(permissions));

    public async Task ClearPermissionsAsync()
        => await RemoveItemAsync(PermissionsKey);

    // Impersonation helpers
    public async Task StashTokensAsync()
    {
        var access = await GetAccessTokenAsync();
        var refresh = await GetRefreshTokenAsync();
        await SetItemAsync(ImpersonationAccessKey, access);
        await SetItemAsync(ImpersonationRefreshKey, refresh);
    }

    public async Task RestoreTokensAsync()
    {
        var access = await GetItemAsync(ImpersonationAccessKey);
        var refresh = await GetItemAsync(ImpersonationRefreshKey);
        await SetTokensAsync(access!, refresh);
        await RemoveItemAsync(ImpersonationAccessKey);
        await RemoveItemAsync(ImpersonationRefreshKey);
    }

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
