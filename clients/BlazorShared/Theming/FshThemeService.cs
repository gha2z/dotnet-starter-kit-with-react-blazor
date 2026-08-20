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
/// Non-sealed so non-browser hosts (MAUI Hybrid) can override the persistence
/// seams (<see cref="ReadStoredAsync"/> / <see cref="WriteStoredAsync"/>) to
/// back the same state with e.g. SecureStorage.
/// </summary>
public class FshThemeService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly string _storageKey;
    private readonly ThemeMode _defaultMode;
    private IJSObjectReference? _module;
    private bool _initialized;
    private ThemeMode _mode;
    private bool _isDarkMode;

    private string _accentId = FshAppearanceOptions.DefaultAccentId;
    private string _fontId = FshAppearanceOptions.DefaultFontId;
    private FshDensityMode _density = FshDensityMode.Comfortable;
    private bool _reducedMotion;
    private FshCustomAccentSpec _customAccent = FshCustomAccentSpec.Default;

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

    /// <summary>Selected accent id (React parity: <c>fsh.accent</c>, default <c>rose</c>).</summary>
    public string AccentId => _accentId;

    /// <summary>Selected font id (React parity: <c>fsh.font</c>, default <c>figtree</c>).</summary>
    public string FontId => _fontId;

    /// <summary>UI density (React parity: <c>fsh.density</c>, default comfortable).</summary>
    public FshDensityMode Density => _density;

    /// <summary>Forces reduced motion regardless of the OS setting (React parity: <c>fsh.reduce-motion</c>).</summary>
    public bool ReducedMotion => _reducedMotion;

    /// <summary>Custom accent hue/chroma spec (React parity: <c>fsh.accent.custom</c>).</summary>
    public FshCustomAccentSpec CustomAccent => _customAccent;

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
            var stored = await ReadStoredAsync();
            var mode = ParseMode(stored, _defaultMode);
            if (mode == ThemeMode.System)
            {
                // System resolves against the OS preference (React parity); the
                // result is cached for the lifetime of the app session.
                _mode = ThemeMode.System;
                _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/FSH.BlazorShared/js/fshTheme.js");
                _isDarkMode = await _module.InvokeAsync<bool>("prefersDark");
            }
            else
            {
                _mode = mode;
                _isDarkMode = mode == ThemeMode.Dark;
            }

            await LoadAppearanceAsync();
            await LoadSelectedFontAsync();
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
            await WriteStoredAsync(ToStorageValue(mode));
        }
        catch
        {
            // Persistence is best-effort; the in-memory state still applies.
        }
    }

    /// <summary>
    /// Reads the persisted preference (raw storage value: "light" | "dark" | "system",
    /// or null when nothing is stored). Base implementation reads localStorage via the
    /// fshTheme.js module; non-browser hosts (MAUI Hybrid) override to use their own
    /// storage (e.g. SecureStorage).
    /// </summary>
    protected virtual async Task<string?> ReadStoredAsync()
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/FSH.BlazorShared/js/fshTheme.js");
        return await _module.InvokeAsync<string?>("getTheme", _storageKey);
    }

    /// <summary>
    /// Persists the chosen mode as a raw storage value. Base implementation writes
    /// localStorage via the fshTheme.js module; non-browser hosts override to use
    /// their own storage. Best-effort — exceptions are swallowed by the caller.
    /// </summary>
    protected virtual async Task WriteStoredAsync(string value)
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/FSH.BlazorShared/js/fshTheme.js");
        await _module.InvokeVoidAsync("setTheme", _storageKey, value);
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

    public async Task SetAccentAsync(string accentId)
    {
        if (string.IsNullOrEmpty(accentId) || accentId == _accentId)
        {
            return;
        }

        _accentId = accentId;
        Changed?.Invoke();
        await WritePreferenceAsync(FshAppearanceOptions.AccentStorageKey, accentId);
    }

    /// <summary>
    /// Activates the custom accent and persists its hue/chroma spec
    /// (React parity: setCustomAccent + setAccent("custom")).
    /// </summary>
    public async Task SetCustomAccentAsync(FshCustomAccentSpec spec)
    {
        _customAccent = spec;
        if (_accentId != FshAppearanceOptions.CustomAccentId)
        {
            _accentId = FshAppearanceOptions.CustomAccentId;
            Changed?.Invoke();
        }

        await WritePreferenceAsync(FshAppearanceOptions.AccentCustomStorageKey, spec.ToStorage());
        await WritePreferenceAsync(FshAppearanceOptions.AccentStorageKey, FshAppearanceOptions.CustomAccentId);
    }

    public async Task SetFontAsync(string fontId)
    {
        if (string.IsNullOrEmpty(fontId) || fontId == _fontId)
        {
            return;
        }

        _fontId = fontId;
        Changed?.Invoke();
        await WritePreferenceAsync(FshAppearanceOptions.FontStorageKey, fontId);
        await LoadSelectedFontAsync();
    }

    /// <summary>
    /// Fetches the selected family's stylesheet on demand (React parity: the
    /// Appearance page lazy-loads the non-boot fonts). Idempotent — re-points a
    /// single <c>&lt;link&gt;</c> so repeated changes never stack stylesheets.
    /// Best-effort: if fonts.googleapis.com is unreachable the CSS stacks fall
    /// back to Inter/Segoe UI.
    /// </summary>
    private async Task LoadSelectedFontAsync()
    {
        try
        {
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/FSH.BlazorShared/js/fshTheme.js");
            await _module.InvokeVoidAsync("loadFont", FshAppearanceOptions.GetFont(_fontId).GoogleQuery);
        }
        catch
        {
            // Offline / CSP-blocked: the fallback stacks still render.
        }
    }

    public async Task SetDensityAsync(FshDensityMode density)
    {
        if (density == _density)
        {
            return;
        }

        _density = density;
        Changed?.Invoke();
        await WritePreferenceAsync(FshAppearanceOptions.DensityStorageKey, density == FshDensityMode.Compact ? "compact" : "comfortable");
    }

    public async Task SetReducedMotionAsync(bool reducedMotion)
    {
        if (reducedMotion == _reducedMotion)
        {
            return;
        }

        _reducedMotion = reducedMotion;
        Changed?.Invoke();
        await WritePreferenceAsync(FshAppearanceOptions.ReduceMotionStorageKey, reducedMotion ? "true" : "false");
    }

    private async Task LoadAppearanceAsync()
    {
        var storedAccent = ParseStoredPreference(await ReadPreferenceAsync(FshAppearanceOptions.AccentStorageKey));
        _accentId = storedAccent is { Length: > 0 }
            ? (storedAccent == FshAppearanceOptions.CustomAccentId
                ? FshAppearanceOptions.CustomAccentId
                : FshAppearanceOptions.GetAccent(storedAccent).Id)
            : FshAppearanceOptions.DefaultAccentId;

        if (_accentId == FshAppearanceOptions.CustomAccentId)
        {
            _customAccent = FshCustomAccentSpec.Parse(
                ParseStoredPreference(await ReadPreferenceAsync(FshAppearanceOptions.AccentCustomStorageKey)));
        }

        _fontId = ParseStoredPreference(await ReadPreferenceAsync(FshAppearanceOptions.FontStorageKey)) ?? FshAppearanceOptions.DefaultFontId;

        var storedDensity = ParseStoredPreference(await ReadPreferenceAsync(FshAppearanceOptions.DensityStorageKey));
        _density = storedDensity == "compact" ? FshDensityMode.Compact : FshDensityMode.Comfortable;

        _reducedMotion = ParseStoredPreference(await ReadPreferenceAsync(FshAppearanceOptions.ReduceMotionStorageKey)) == "true";
    }

    private static string? ParseStoredPreference(string? stored)
        => string.IsNullOrWhiteSpace(stored) ? null : stored;

    /// <summary>
    /// Reads a persisted appearance preference (accent/font/density/motion). Base
    /// implementation reads localStorage via the fshTheme.js module; non-browser
    /// hosts (MAUI Hybrid) override to use their own storage.
    /// </summary>
    protected virtual async Task<string?> ReadPreferenceAsync(string key)
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/FSH.BlazorShared/js/fshTheme.js");
        return await _module.InvokeAsync<string?>("getPreference", key);
    }

    /// <summary>
    /// Persists an appearance preference as a raw storage value. Base implementation
    /// writes localStorage via the fshTheme.js module; non-browser hosts override to
    /// use their own storage. Best-effort — exceptions are swallowed by the caller.
    /// </summary>
    protected virtual async Task WritePreferenceAsync(string key, string value)
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/FSH.BlazorShared/js/fshTheme.js");
        await _module.InvokeVoidAsync("setPreference", key, value);
    }

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
