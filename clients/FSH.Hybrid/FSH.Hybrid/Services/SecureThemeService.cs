using FSH.BlazorShared.Theming;
using Microsoft.JSInterop;

namespace FSH.Hybrid.Services;

/// <summary>
/// <see cref="FshThemeService"/> whose persistence is backed by the platform
/// keychain (SecureStorage) instead of localStorage — the Blazor WebView has no
/// localStorage, so the base implementation's best-effort write silently no-ops.
/// Uses the FshThemeService persistence seams added in wave 16.
/// </summary>
public sealed class SecureThemeService : FshThemeService
{
    private const string StorageKey = "fsh.theme";

    public SecureThemeService(IJSRuntime js)
        : base(js, StorageKey, ThemeMode.System)
    {
    }

    protected override async Task<string?> ReadStoredAsync()
        => await SecureStorage.Default.GetAsync(StorageKey).ConfigureAwait(false);

    protected override async Task WriteStoredAsync(string value)
        => await SecureStorage.Default.SetAsync(StorageKey, value).ConfigureAwait(false);
}
