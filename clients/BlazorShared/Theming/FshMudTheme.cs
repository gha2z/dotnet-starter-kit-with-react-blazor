using MudBlazor;

namespace FSH.BlazorShared.Theming;

public static class FshMudTheme
{
    // Brand parity with the React admin (rose accent, dark-mode default, Outfit display face).
    // Light tones darkened to pass WCAG 2.1 AA (>= 4.5:1) on both surface (#FFFFFF) and
    // background (#F5F5F7): #E11D48 -> #D11A42 (4.9/5.3:1), #4A7B8C -> #457383 (4.8/5.2:1).
    private const string LightPrimary = "#D11A42";
    private const string LightSecondary = "#457383";
    private const string DarkPrimary = "#FB7185";

    public static MudTheme CreateAdmin()
    {
        return new MudTheme
        {
            Typography = new Typography
            {
                Default = new DefaultTypography { FontFamily = ["Inter", "Segoe UI", "sans-serif"] },
                H1 = new H1Typography { FontFamily = ["Outfit", "Inter", "sans-serif"] },
                H2 = new H2Typography { FontFamily = ["Outfit", "Inter", "sans-serif"] },
                H3 = new H3Typography { FontFamily = ["Outfit", "Inter", "sans-serif"] },
                H4 = new H4Typography { FontFamily = ["Outfit", "Inter", "sans-serif"] },
                H5 = new H5Typography { FontFamily = ["Outfit", "Inter", "sans-serif"] },
                H6 = new H6Typography { FontFamily = ["Outfit", "Inter", "sans-serif"] },
            },
            PaletteLight = new PaletteLight
            {
                Primary = LightPrimary,
                Secondary = LightSecondary,
                AppbarBackground = "#111113",
                AppbarText = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#1B1B1F",
                Surface = "#FFFFFF",
                Background = "#F5F5F7",
                TextPrimary = "#1B1B1F",
                TextSecondary = "#6B6B76",
                Divider = "#E4E4E9",
                ActionDefault = "#6B6B76",
                ActionDisabled = "#B0B0B8",
                Dark = "#1B1B1F",
                DarkDarken = "#101013",
            },
            PaletteDark = new PaletteDark
            {
                Primary = DarkPrimary,
                PrimaryContrastText = "#121216", // dark text on light rose (white on #FB7185 = 2.7:1, fails AA)
                Secondary = "#6BA3B8",
                AppbarBackground = "#111113",
                AppbarText = "#E0E0E6",
                DrawerBackground = "#17171B",
                DrawerText = "#C5C5CF",
                Surface = "#1F1F24",
                Background = "#121216",
                TextPrimary = "#E4E4EA",
                TextSecondary = "#A0A0AB",
                Divider = "#2C2C33",
                ActionDefault = "#A0A0AB",
                ActionDisabled = "#4A4A52",
                Dark = "#0B0B0E",
                DarkDarken = "#060608",
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "6px",
                DrawerWidthLeft = "260px",
            },
        };
    }

    public static MudTheme CreateDashboard()
        => CreateDashboard(FshAppearanceOptions.GetAccent(FshAppearanceOptions.DefaultAccentId));

    public static MudTheme CreateDashboard(FshAccentOption accent)
    {
        var theme = CreateAdmin();
        theme.PaletteLight.Primary = accent.LightPrimary;
        theme.PaletteDark.Primary = accent.DarkPrimary;
        return theme;
    }
}
