namespace FSH.BlazorShared.Auth;

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

    // Impersonation helpers (no-op stores simply report "no stash" and forward fresh tokens).
    Task<bool> HasImpersonationStashAsync();
    Task SetFreshTokensAsync(string accessToken, string? refreshToken);
    Task RestoreTokensAsync();

    event Action? TokensChanged;
}
