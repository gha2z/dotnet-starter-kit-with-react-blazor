using FSH.BlazorShared.Auth;

namespace FSH.Hybrid.Services;

public sealed class MauiTokenStore : ITokenStore
{
    private const string AccessKey = "access_token";
    private const string RefreshKey = "refresh_token";
    private const string TenantKey = "tenant";
    private const string PermissionsKey = "permissions";

    public event Action? TokensChanged;

    public async Task<string?> GetAccessTokenAsync()
        => await SecureStorage.GetAsync(AccessKey);

    public async Task<string?> GetRefreshTokenAsync()
        => await SecureStorage.GetAsync(RefreshKey);

    public async Task SetTokensAsync(string accessToken, string? refreshToken)
    {
        await SecureStorage.SetAsync(AccessKey, accessToken);
        if (refreshToken is not null)
            await SecureStorage.SetAsync(RefreshKey, refreshToken);
        TokensChanged?.Invoke();
    }

    public async Task ClearAsync()
    {
        SecureStorage.Remove(AccessKey);
        SecureStorage.Remove(RefreshKey);
        SecureStorage.Remove(TenantKey);
        SecureStorage.Remove(PermissionsKey);
        TokensChanged?.Invoke();
        await Task.CompletedTask;
    }

    public async Task<string?> GetTenantAsync()
        => await SecureStorage.GetAsync(TenantKey);

    public Task SetTenantAsync(string? tenant)
    {
        if (tenant is null)
            SecureStorage.Remove(TenantKey);
        else
            SecureStorage.SetAsync(TenantKey, tenant);
        return Task.CompletedTask;
    }

    public async Task<string[]?> GetPermissionsAsync()
    {
        var json = await SecureStorage.GetAsync(PermissionsKey);
        return json is not null ? System.Text.Json.JsonSerializer.Deserialize<string[]>(json) : null;
    }

    public async Task SetPermissionsAsync(string[] permissions)
        => await SecureStorage.SetAsync(PermissionsKey, System.Text.Json.JsonSerializer.Serialize(permissions));

    public Task ClearPermissionsAsync()
    {
        SecureStorage.Remove(PermissionsKey);
        return Task.CompletedTask;
    }
}
