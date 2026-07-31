using MudBlazor;

namespace FSH.BlazorShared.Theming;

public static class FshMudTheme
{
    public static MudTheme CreateAdmin()
    {
        return new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#5B8C5A",
                Secondary = "#4A7B8C",
                AppbarBackground = "#1B2A2C",
                AppbarText = "#FFFFFF",
                DrawerBackground = "#F5F6F7",
                DrawerText = "#1B2A2C",
                Surface = "#FFFFFF",
                Background = "#F0F2F5",
                TextPrimary = "#1B2A2C",
                TextSecondary = "#5C6B73",
                Divider = "#D0D5DD",
                ActionDefault = "#5C6B73",
                ActionDisabled = "#B0B8C1",
                Dark = "#1B2A2C",
                DarkDarken = "#0F1A1C",
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#7DB87C",
                Secondary = "#6BA3B8",
                AppbarBackground = "#0F1A1C",
                AppbarText = "#E0E3E7",
                DrawerBackground = "#1B2A2C",
                DrawerText = "#C5CDD3",
                Surface = "#1F2E30",
                Background = "#141F21",
                TextPrimary = "#E0E3E7",
                TextSecondary = "#A0AAB3",
                Divider = "#2A3A3D",
                ActionDefault = "#A0AAB3",
                ActionDisabled = "#4A5A5E",
                Dark = "#0A1213",
                DarkDarken = "#050B0C",
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "6px",
                DrawerWidthLeft = "260px",
            },
        };
    }

    public static MudTheme CreateDashboard()
    {
        var theme = CreateAdmin();
        theme.PaletteLight.Primary = "#E85D75";
        theme.PaletteDark.Primary = "#F07A8E";
        return theme;
    }
}
