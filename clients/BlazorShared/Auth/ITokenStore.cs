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
    event Action? TokensChanged;
}
