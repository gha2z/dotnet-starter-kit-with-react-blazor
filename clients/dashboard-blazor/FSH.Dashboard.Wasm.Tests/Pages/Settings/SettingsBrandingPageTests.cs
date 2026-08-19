using Bunit;
using FSH.BlazorShared.Models.Tenants;
using FSH.BlazorShared.Permissions;
using FSH.BlazorShared.Services;
using FSH.Dashboard.Wasm.Pages.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Settings;

public sealed class SettingsBrandingPageTests : TestSetup
{
    private readonly ITenantThemeService _themeService = Substitute.For<ITenantThemeService>();

    public SettingsBrandingPageTests()
    {
        Services.AddSingleton(_themeService);
        PermissionsProvider.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns([MultitenancyPermissions.Tenants.UpdateTheme]);
    }

    private static TenantThemeDto SampleTheme(string primary = "#2563EB")
    {
        var theme = new TenantThemeDto
        {
            LightPalette = new PaletteDto { Primary = primary },
            BrandAssets = new BrandAssetsDto { LogoUrl = "https://cdn.example.com/logo.svg" }
        };
        return theme;
    }

    [Fact]
    public void Renders_loaded_theme_values()
    {
        _themeService.GetCurrentThemeAsync(Arg.Any<CancellationToken>())
            .Returns(SampleTheme("#ABCDEF"));

        var cut = Render<SettingsBrandingPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("#ABCDEF"));
        cut.Markup.ShouldContain("Light palette");
        cut.Markup.ShouldContain("Dark palette");
        cut.Markup.ShouldContain("Brand assets");
        cut.Markup.ShouldContain("Save branding");
        cut.Markup.ShouldContain("Reset to defaults");
        cut.Markup.ShouldContain("Sample tenant page");
    }

    [Fact]
    public void Shows_default_chip_for_pristine_default_theme()
    {
        _themeService.GetCurrentThemeAsync(Arg.Any<CancellationToken>())
            .Returns(new TenantThemeDto { IsDefault = true });

        var cut = Render<SettingsBrandingPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("default"));
        cut.Markup.ShouldNotContain("unsaved");
    }

    [Fact]
    public void Shows_error_band_when_theme_load_fails()
    {
        _themeService.GetCurrentThemeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TenantThemeDto>(new InvalidOperationException("boom")));

        var cut = Render<SettingsBrandingPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load branding"));
    }

    [Fact]
    public void Editing_an_asset_field_marks_dirty_and_saves()
    {
        _themeService.GetCurrentThemeAsync(Arg.Any<CancellationToken>())
            .Returns(SampleTheme());

        var cut = Render<SettingsBrandingPage>();
        cut.WaitForAssertion(() => cut.FindAll("input").Count.ShouldBeGreaterThan(0));

        var textInputs = cut.FindAll("input[type='text']");
        var favicon = textInputs[^1];
        favicon.Input("https://cdn.example.com/favicon.ico");

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("unsaved"));
        var save = cut.FindAll("button").First(b => b.TextContent.Contains("Save branding"));
        save.Attributes["disabled"].ShouldBeNull();

        save.Click();

        cut.WaitForAssertion(() =>
            _themeService.Received(1).UpdateCurrentThemeAsync(
                Arg.Is<TenantThemeDto>(t =>
                    t.BrandAssets.FaviconUrl == "https://cdn.example.com/favicon.ico" &&
                    !t.BrandAssets.DeleteFavicon),
                Arg.Any<CancellationToken>()));
    }

    [Fact]
    public void Reset_dispatches_reset_and_reloads()
    {
        _themeService.GetCurrentThemeAsync(Arg.Any<CancellationToken>())
            .Returns(SampleTheme());

        var cut = Render<SettingsBrandingPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Reset to defaults"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Reset to defaults")).Click();

        cut.WaitForAssertion(() =>
            _themeService.Received(1).ResetCurrentThemeAsync(Arg.Any<CancellationToken>()));
        _themeService.Received(2).GetCurrentThemeAsync(Arg.Any<CancellationToken>());
    }
}

public sealed class SettingsLayoutBrandingGateTests : TestSetup
{
    public SettingsLayoutBrandingGateTests()
    {
        Services.AddSingleton(Substitute.For<ITenantThemeService>());
    }

    private IRenderedComponent<SettingsLayout> RenderLayout()
        => Render<SettingsLayout>(p => p.AddChildContent("child"));

    [Fact]
    public void Branding_link_visible_with_update_theme_permission()
    {
        PermissionsProvider.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns([MultitenancyPermissions.Tenants.UpdateTheme]);

        var cut = RenderLayout();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("/settings/branding"));
    }

    [Fact]
    public void Branding_link_hidden_without_update_theme_permission()
    {
        PermissionsProvider.GetPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns(["Permissions.Multitenancy.View"]);

        var cut = RenderLayout();

        cut.Markup.ShouldNotContain("/settings/branding");
        cut.Markup.ShouldContain("/settings/appearance");
    }
}