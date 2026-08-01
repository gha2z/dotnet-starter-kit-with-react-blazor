using Microsoft.JSInterop;

namespace FSH.BlazorShared.Theming;

/// <summary>
/// Theme preference as stored/persisted. React parity: the admin app exposes a
/// binary light/dark toggle while the dashboard also supports "system" (follow
/// the OS preference), stored under <c>fsh.theme</c>.
/// </summary>
public enum ThemeMode
{
    Light,
    Dark,
    System,
}

/// <summary>
/// Dark/light theme state persisted to localStorage (React parity: stored per app,
/// resolution order storage → prefers-color-scheme → app default).
/// </summary>
public sealed class FshThemeService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly string _storageKey;
    private readonly ThemeMode _defaultMode;
    private IJSObjectReference? _module;
    private bool _initialized;
    private ThemeMode _mode;
    private bool _isDarkMode;

    public FshThemeService(IJSRuntime js, string storageKey, ThemeMode defaultMode = ThemeMode.Dark)
    {
        _js = js;
        _storageKey = storageKey;
        _defaultMode = defaultMode;
        _mode = defaultMode;
        _isDarkMode = defaultMode != ThemeMode.Light;
    }

    /// <summary>Resolved dark state — the effective value used to style the app.</summary>
    public bool IsDarkMode => _isDarkMode;

    /// <summary>Stored preference: Light, Dark or System.</summary>
    public ThemeMode Mode => _mode;

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        try
        {
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/FSH.BlazorShared/js/fshTheme.js");
            var stored = await _module.InvokeAsync<string?>("getTheme", _storageKey);
            var mode = ParseMode(stored, _defaultMode);
            if (mode == ThemeMode.System)
            {
                // System resolves against the OS preference (React parity); the
                // result is cached for the lifetime of the app session.
                _mode = ThemeMode.System;
                _isDarkMode = await _module.InvokeAsync<bool>("prefersDark");
            }
            else
            {
                _mode = mode;
                _isDarkMode = mode == ThemeMode.Dark;
            }
        }
        catch
        {
            // Fall back to the app default if storage is unavailable.
            _mode = _defaultMode;
            _isDarkMode = _defaultMode == ThemeMode.Dark;
        }
    }

    /// <summary>Binary light/dark toggle (admin parity).</summary>
    public async Task SetAsync(bool isDarkMode)
        => await SetModeAsync(isDarkMode ? ThemeMode.Dark : ThemeMode.Light);

    public async Task SetModeAsync(ThemeMode mode)
    {
        if (_mode == mode)
        {
            return;
        }

        if (mode == ThemeMode.System)
        {
            // React parity: switching to System resolves the OS preference right away.
            try
            {
                _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/FSH.BlazorShared/js/fshTheme.js");
                _isDarkMode = await _module.InvokeAsync<bool>("prefersDark");
            }
            catch
            {
                // Keep the current resolved state if the OS preference is unavailable.
            }
        }

        ApplyMode(mode);
        try
        {
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/FSH.BlazorShared/js/fshTheme.js");
            await _module.InvokeVoidAsync("setTheme", _storageKey, ToStorageValue(mode));
        }
        catch
        {
            // Persistence is best-effort; the in-memory state still applies.
        }
    }

    private void ApplyMode(ThemeMode mode)
    {
        _mode = mode;
        _isDarkMode = mode switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            _ => _isDarkMode, // System resolves against the OS preference (kept at initialize time).
        };
        Changed?.Invoke();
    }

    private static ThemeMode ParseMode(string? stored, ThemeMode fallback)
        => stored switch
        {
            "light" => ThemeMode.Light,
            "dark" => ThemeMode.Dark,
            "system" => ThemeMode.System,
            _ => fallback,
        };

    private static string ToStorageValue(ThemeMode mode)
        => mode switch
        {
            ThemeMode.Dark => "dark",
            ThemeMode.Light => "light",
            _ => "system",
        };

    public ValueTask DisposeAsync()
    {
        try
        {
            return _module?.DisposeAsync() ?? ValueTask.CompletedTask;
        }
        catch
        {
            return ValueTask.CompletedTask;
        }
    }
}
