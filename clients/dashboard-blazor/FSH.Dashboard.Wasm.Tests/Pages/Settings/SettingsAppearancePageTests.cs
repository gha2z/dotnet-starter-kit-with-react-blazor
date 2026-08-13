using Bunit;
using FSH.BlazorShared.Theming;
using FSH.Dashboard.Wasm.Pages.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Settings;

public sealed class SettingsAppearancePageTests : TestSetup
{
    private readonly FshThemeService _themeService;

    public SettingsAppearancePageTests()
    {
        _themeService = new InMemoryThemeService(JSInterop.JSRuntime);
        Services.AddSingleton(_themeService);
    }

    [Fact]
    public void Renders_header_and_theme_options()
    {
        var cut = Render<SettingsAppearancePage>();

        cut.Markup.ShouldContain("Appearance");
        cut.Markup.ShouldContain("Light");
        cut.Markup.ShouldContain("System");
        cut.Markup.ShouldContain("Dark");
    }

    [Fact]
    public void Shows_theme_mode_options_from_service_default()
    {
        // Service default is System — the System option is present and marked selected.
        var cut = Render<SettingsAppearancePage>();

        cut.Markup.ShouldContain("System — follow OS");
        cut.WaitForAssertion(() => _themeService.Mode.ShouldBe(ThemeMode.System));
    }

    [Fact]
    public async Task Switching_mode_updates_theme_service()
    {
        var cut = Render<SettingsAppearancePage>();

        await cut.InvokeAsync(() => _themeService.SetModeAsync(ThemeMode.Dark));

        cut.WaitForAssertion(() => _themeService.Mode.ShouldBe(ThemeMode.Dark));
    }

    [Fact]
    public void Renders_all_accent_presets_and_custom_card()
    {
        var cut = Render<SettingsAppearancePage>();

        foreach (var accent in FshAppearanceOptions.Accents)
        {
            cut.Markup.ShouldContain(accent.Label, Case.Insensitive);
        }

        cut.Markup.ShouldContain("Custom");
        cut.Markup.ShouldContain("Pick your own hue");
    }

    [Fact]
    public void Selecting_an_accent_preset_updates_the_service()
    {
        var cut = Render<SettingsAppearancePage>();

        var card = cut.FindAll("button").First(b => b.TextContent.Contains("Indigo"));
        card.Click();

        cut.WaitForAssertion(() => _themeService.AccentId.ShouldBe("indigo"));
    }

    [Fact]
    public void Renders_all_font_options()
    {
        var cut = Render<SettingsAppearancePage>();

        foreach (var font in FshAppearanceOptions.Fonts)
        {
            cut.Markup.ShouldContain(font.Label, Case.Insensitive);
        }
    }

    [Fact]
    public void Selecting_a_font_updates_the_service()
    {
        var cut = Render<SettingsAppearancePage>();

        var card = cut.FindAll("button").First(b => b.TextContent.Contains("Manrope"));
        card.Click();

        cut.WaitForAssertion(() => _themeService.FontId.ShouldBe("manrope"));
    }

    [Fact]
    public async Task Toggling_compact_density_updates_the_service()
    {
        var cut = Render<SettingsAppearancePage>();

        await cut.InvokeAsync(() => _themeService.SetDensityAsync(FshDensityMode.Compact));

        cut.WaitForAssertion(() => _themeService.Density.ShouldBe(FshDensityMode.Compact));
    }

    [Fact]
    public async Task Toggling_reduce_motion_updates_the_service()
    {
        var cut = Render<SettingsAppearancePage>();

        await cut.InvokeAsync(() => _themeService.SetReducedMotionAsync(true));

        cut.WaitForAssertion(() => _themeService.ReducedMotion.ShouldBeTrue());
    }

    [Fact]
    public async Task Applying_custom_accent_sets_service_to_custom()
    {
        var cut = Render<SettingsAppearancePage>();

        await cut.InvokeAsync(() =>
            _themeService.SetCustomAccentAsync(new FshCustomAccentSpec(220, 1.1)));

        cut.WaitForAssertion(() => _themeService.AccentId.ShouldBe(FshAppearanceOptions.CustomAccentId));
        cut.WaitForAssertion(() => _themeService.CustomAccent.Hue.ShouldBe(220));
    }

    /// <summary>
    /// In-memory theme service so page tests never touch the JS module in bUnit's
    /// loose mode — preferences round-trip through a dictionary instead.
    /// </summary>
    private sealed class InMemoryThemeService : FshThemeService
    {
        private readonly Dictionary<string, string?> _stored = new(StringComparer.Ordinal);

        public InMemoryThemeService(IJSRuntime js)
            : base(js, "fsh.theme", ThemeMode.System)
        {
        }

        protected override Task<string?> ReadPreferenceAsync(string key)
            => Task.FromResult(_stored.TryGetValue(key, out var value) ? value : null);

        protected override Task WritePreferenceAsync(string key, string value)
        {
            _stored[key] = value;
            return Task.CompletedTask;
        }
    }
}
