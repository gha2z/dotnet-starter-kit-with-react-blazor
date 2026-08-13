using FSH.BlazorShared.Auth;
using Microsoft.Extensions.Logging;

namespace FSH.Hybrid.Services;

public sealed class MauiAuthStateProvider(
    ITokenStore tokenStore,
    IBiometricService biometricService,
    ILogger<MauiAuthStateProvider> logger)
{
    private const string BiometricLockKey = "fsh.hybrid.biometricLock";

    public bool IsBiometricLockEnabled
    {
        get => Preferences.Default.Get(BiometricLockKey, false);
        set => Preferences.Default.Set(BiometricLockKey, value);
    }

    public bool IsLocked { get; private set; }

    public async Task<bool> OnAppResumedAsync(CancellationToken ct = default)
    {
        if (!IsBiometricLockEnabled)
        {
            return true;
        }

        if (await tokenStore.GetAccessTokenAsync() is null)
        {
            return true;
        }

        var unlocked = await biometricService.AuthenticateAsync("Unlock FSH Hybrid", ct);
        IsLocked = !unlocked;
        if (!unlocked)
        {
            logger.LogWarning("Biometric unlock failed on app resume; session stays locked");
        }

        return unlocked;
    }

    public void Lock() => IsLocked = true;

    public void Unlock() => IsLocked = false;
}
