namespace FSH.BlazorShared.Services;

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
