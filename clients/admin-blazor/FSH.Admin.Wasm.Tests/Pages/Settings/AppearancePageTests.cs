using Bunit;
using FSH.Admin.Wasm.Pages.Settings;
using FSH.BlazorShared.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Settings;

public class AppearancePageTests : TestSetup
{
    private readonly FshThemeService _theme;

    public AppearancePageTests()
    {
        var js = Substitute.For<IJSRuntime>();
        _theme = new FshThemeService(js, "fsh.admin.theme", ThemeMode.Dark);
        Services.AddSingleton(_theme);
    }

    [Fact]
    public void Renders_both_theme_cards()
    {
        var cut = Render<AppearancePage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Dark");
            cut.Markup.ShouldContain("Light");
            cut.Markup.ShouldContain("Console-default. Lower glare for long sessions.");
            cut.Markup.ShouldContain("Paper-white surfaces, magazine-print mood.");
        });
    }

    [Fact]
    public void Light_card_switches_theme_to_light()
    {
        var cut = Render<AppearancePage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Light"));

        cut.FindAll(".fsh-theme-card").First(c => c.TextContent.Contains("Light")).Click();

        cut.WaitForAssertion(() => _theme.Mode.ShouldBe(ThemeMode.Light));
        cut.WaitForAssertion(() => _theme.IsDarkMode.ShouldBeFalse());
    }

    [Fact]
    public async Task Dark_card_switches_theme_back_to_dark()
    {
        await _theme.SetAsync(false);
        var cut = Render<AppearancePage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Dark"));

        cut.FindAll(".fsh-theme-card").First(c => c.TextContent.Contains("Dark")).Click();

        cut.WaitForAssertion(() => _theme.Mode.ShouldBe(ThemeMode.Dark));
        cut.WaitForAssertion(() => _theme.IsDarkMode.ShouldBeTrue());
    }

    [Fact]
    public void Active_badge_marks_the_active_theme_card()
    {
        var cut = Render<AppearancePage>();

        cut.WaitForAssertion(() =>
        {
            var activeCard = cut.FindAll(".fsh-theme-card").Single(c => c.TextContent.Contains("Active"));
            activeCard.TextContent.ShouldContain("Dark");
        });
    }

    [Fact]
    public void Active_badge_follows_theme_switch()
    {
        var cut = Render<AppearancePage>();
        cut.WaitForAssertion(() => cut.FindAll(".fsh-theme-card").Count.ShouldBe(2));

        cut.FindAll(".fsh-theme-card").First(c => c.TextContent.Contains("Light")).Click();

        cut.WaitForAssertion(() =>
        {
            var activeCard = cut.FindAll(".fsh-theme-card").Single(c => c.TextContent.Contains("Active"));
            activeCard.TextContent.ShouldContain("Light");
        });
    }

    [Fact]
    public void Density_section_shows_disabled_compact_placeholder()
    {
        var cut = Render<AppearancePage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Density");
            cut.Markup.ShouldContain("Compact mode will reduce card padding and row height for data-dense screens");
            var button = cut.FindAll("button").Single(b => b.TextContent.Contains("Compact rows"));
            button.TextContent.ShouldContain("coming soon");
            button.HasAttribute("disabled").ShouldBeTrue();
        });
    }
}
