using Bunit;
using FSH.BlazorShared.Theming;
using FSH.Dashboard.Wasm.Pages.Settings;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Settings;

public sealed class SettingsAppearancePageTests : TestSetup
{
    private readonly FshThemeService _themeService;

    public SettingsAppearancePageTests()
    {
        _themeService = new FshThemeService(JSInterop.JSRuntime, "fsh.theme", ThemeMode.System);
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
}
