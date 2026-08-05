using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.Logging;

namespace FSH.Hybrid.Services;

public sealed class MauiAuthService(
    IAuthService authService,
    IBiometricService biometricService,
    AuthStateProvider authStateProvider,
    ITokenStore tokenStore,
    ILogger<MauiAuthService> logger) : IAuthService
{
    public const string AppHeader = "dashboard";

    public async Task<bool> LoginAsync(string email, string password, string tenant, CancellationToken ct = default)
    {
        await LoginAsync(email, password, tenant, AppHeader, ct);
        return true;
    }

    public async Task<TokenResponse> LoginAsync(string email, string password, string tenant, string appHeader, CancellationToken ct = default)
    {
        var result = await authService.LoginAsync(email, password, tenant, appHeader, ct);
        await authStateProvider.NotifyLoginAsync(result.AccessToken, result.RefreshToken, tenant);
        return result;
    }

    public async Task<bool> TryBiometricUnlockAsync(string reason, CancellationToken ct = default)
    {
        if (await tokenStore.GetAccessTokenAsync() is null)
        {
            logger.LogDebug("Biometric unlock skipped: no stored session");
            return false;
        }

        if (!await biometricService.IsAvailableAsync(ct))
        {
            logger.LogDebug("Biometric unlock skipped: not available on this device");
            return false;
        }

        if (!await biometricService.AuthenticateAsync(reason, ct))
        {
            logger.LogWarning("Biometric verification failed");
            return false;
        }

        await authStateProvider.RefreshAsync();
        return true;
    }

    public Task<RefreshResponse> RefreshAsync(string accessToken, string refreshToken, CancellationToken ct = default)
        => authService.RefreshAsync(accessToken, refreshToken, ct);

    public Task<string[]> GetPermissionsAsync(CancellationToken ct = default)
        => authService.GetPermissionsAsync(ct);

    public Task ForgotPasswordAsync(string email, string tenant, CancellationToken ct = default)
        => authService.ForgotPasswordAsync(email, tenant, ct);

    public Task ResetPasswordAsync(string email, string password, string token, string tenant, CancellationToken ct = default)
        => authService.ResetPasswordAsync(email, password, token, tenant, ct);

    public Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken ct = default)
        => authService.ConfirmEmailAsync(userId, code, tenant, ct);
}
