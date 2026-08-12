namespace FSH.BlazorShared.Models.Tenants;

public sealed class TenantThemeDto
{
    public PaletteDto LightPalette { get; set; } = new();
    public PaletteDto DarkPalette { get; set; } = new();
    public BrandAssetsDto BrandAssets { get; set; } = new();
    public TypographyDto Typography { get; set; } = new();
    public LayoutDto Layout { get; set; } = new();
    public bool IsDefault { get; set; }
}

public sealed class PaletteDto
{
    public string Primary { get; set; } = "#2563EB";
    public string Secondary { get; set; } = "#0F172A";
    public string Tertiary { get; set; } = "#6366F1";
    public string Background { get; set; } = "#F8FAFC";
    public string Surface { get; set; } = "#FFFFFF";
    public string Error { get; set; } = "#DC2626";
    public string Warning { get; set; } = "#F59E0B";
    public string Success { get; set; } = "#16A34A";
    public string Info { get; set; } = "#0284C7";

    public static PaletteDto DefaultLight => new();

    public static PaletteDto DefaultDark => new()
    {
        Primary = "#38BDF8",
        Secondary = "#94A3B8",
        Tertiary = "#818CF8",
        Background = "#0B1220",
        Surface = "#111827",
        Error = "#F87171",
        Warning = "#FBBF24",
        Success = "#22C55E",
        Info = "#38BDF8"
    };
}

public sealed class BrandAssetsDto
{
    public string? LogoUrl { get; set; }
    public string? LogoDarkUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public bool DeleteLogo { get; set; }
    public bool DeleteLogoDark { get; set; }
    public bool DeleteFavicon { get; set; }
}

public sealed class TypographyDto
{
    public string FontFamily { get; set; } = "Inter, sans-serif";
    public string HeadingFontFamily { get; set; } = "Inter, sans-serif";
    public double FontSizeBase { get; set; } = 14;
    public double LineHeightBase { get; set; } = 1.5;
}

public sealed class LayoutDto
{
    public string BorderRadius { get; set; } = "4px";
    public int DefaultElevation { get; set; } = 1;
}
