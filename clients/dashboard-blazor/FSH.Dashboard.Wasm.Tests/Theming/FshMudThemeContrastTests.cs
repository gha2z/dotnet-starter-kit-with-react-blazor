using FSH.BlazorShared.Theming;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Theming;

/// <summary>WCAG 2.1 AA contrast audit for the shared FSH MudBlazor palettes.
/// Text pairs need >= 4.5:1; primary contrast text (filled buttons) needs
/// >= 4.5:1 on the primary background. Divider rules are intentionally
/// decorative (audited 2026-08-07: 1.3:1 light / 1.2:1 dark — page headings
/// and hover states carry the boundary affordance).</summary>
public sealed class FshMudThemeContrastTests
{
    private const double AaText = 4.5;

    private static double Luminance(MudBlazor.Utilities.MudColor color)
    {
        // MudColor.Value is "#rrggbbaa" (or shorter when no alpha); keep RGB channels.
        var hex = color.Value.TrimStart('#')[..6];
        var r = Convert.ToInt32(hex[..2], 16) / 255d;
        var g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255d;
        var b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255d;
        static double Linear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        return 0.2126 * Linear(r) + 0.7152 * Linear(g) + 0.0722 * Linear(b);
    }

    private static double Ratio(MudBlazor.Utilities.MudColor fg, MudBlazor.Utilities.MudColor bg)
    {
        var l1 = Luminance(fg);
        var l2 = Luminance(bg);
        if (l1 < l2)
        {
            (l1, l2) = (l2, l1);
        }

        return (l1 + 0.05) / (l2 + 0.05);
    }

    private static void AssertAa(string pair, double ratio, double minimum = AaText)
        => ratio.ShouldBeGreaterThanOrEqualTo(minimum, $"{pair} contrast {ratio:F2}:1 must be >= {minimum}:1");

    [Fact]
    public void Light_Palette_Meets_WCAG_AA_For_Text_And_Contrast_Pairs()
    {
        var p = FshMudTheme.CreateDashboard().PaletteLight;

        AssertAa("Primary on Surface", Ratio(p.Primary!, p.Surface!));
        AssertAa("Primary on Background", Ratio(p.Primary!, p.Background!));
        AssertAa("PrimaryContrastText on Primary", Ratio(p.PrimaryContrastText!, p.Primary!));
        AssertAa("Secondary on Surface", Ratio(p.Secondary!, p.Surface!));
        AssertAa("Secondary on Background", Ratio(p.Secondary!, p.Background!));
        AssertAa("TextPrimary on Surface", Ratio(p.TextPrimary!, p.Surface!));
        AssertAa("TextSecondary on Surface", Ratio(p.TextSecondary!, p.Surface!));
        AssertAa("TextSecondary on Background", Ratio(p.TextSecondary!, p.Background!));
        AssertAa("AppbarText on AppbarBackground", Ratio(p.AppbarText!, p.AppbarBackground!));
        AssertAa("DrawerText on DrawerBackground", Ratio(p.DrawerText!, p.DrawerBackground!));
        AssertAa("ActionDefault on Surface", Ratio(p.ActionDefault!, p.Surface!));
    }

    [Fact]
    public void Dark_Palette_Meets_WCAG_AA_For_Text_And_Contrast_Pairs()
    {
        var p = FshMudTheme.CreateDashboard().PaletteDark;

        AssertAa("Primary on Surface", Ratio(p.Primary!, p.Surface!));
        AssertAa("Primary on Background", Ratio(p.Primary!, p.Background!));
        AssertAa("PrimaryContrastText on Primary", Ratio(p.PrimaryContrastText!, p.Primary!));
        AssertAa("Secondary on Surface", Ratio(p.Secondary!, p.Surface!));
        AssertAa("Secondary on Background", Ratio(p.Secondary!, p.Background!));
        AssertAa("TextPrimary on Surface", Ratio(p.TextPrimary!, p.Surface!));
        AssertAa("TextSecondary on Surface", Ratio(p.TextSecondary!, p.Surface!));
        AssertAa("AppbarText on AppbarBackground", Ratio(p.AppbarText!, p.AppbarBackground!));
        AssertAa("DrawerText on DrawerBackground", Ratio(p.DrawerText!, p.DrawerBackground!));
        AssertAa("ActionDefault on Surface", Ratio(p.ActionDefault!, p.Surface!));
    }
}
