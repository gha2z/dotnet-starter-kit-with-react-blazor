using System.Globalization;

namespace FSH.BlazorShared.Theming;

/// <summary>
/// Selectable accent palette. Mirrors the React dashboard's six brand accents
/// (appearance-options.ts) mapped to AA-passing light/dark primary tones. The
/// <see cref="Swatch"/> is the light primary used to render the option card.
/// </summary>
public sealed record FshAccentOption(string Id, string Label, string Description, string LightPrimary, string DarkPrimary)
{
    public string Swatch => LightPrimary;
}

/// <summary>
/// Selectable UI font family (React parity: appearance-options.ts). The family
/// is applied to the MudBlazor typography default face. <see cref="Family"/> is
/// the full CSS stack used for previews; <see cref="GoogleQuery"/> is the
/// family=… segment for the on-demand Google Fonts stylesheet (the nine
/// non-boot families load only when selected — same map as React's lazy loader).
/// </summary>
public sealed record FshFontOption(string Id, string Label, string Description, string Family, string GoogleQuery)
{
    /// <summary>First face of the CSS stack, e.g. "Figtree" (unquoted).</summary>
    public string FamilyName => Family.Split(',')[0].Trim().Trim('\'');
}

public enum FshDensityMode
{
    Comfortable,
    Compact,
}

/// <summary>
/// User-defined accent derived from a single hue. Mirrors the React dashboard's
/// custom-accent dialog: the user picks a hue (0-360) plus a chroma scale where
/// 1.0 equals the indigo template intensity, and the brand stops are derived
/// from that hue keeping the (lightness, chroma) ladder constant.
/// </summary>
public sealed record FshCustomAccentSpec(double Hue, double Chroma)
{
    public static FshCustomAccentSpec Default => new(12, 1.0);

    public static FshCustomAccentSpec Parse(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return Default;
        }

        var parts = stored.Split(';', StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var h)
            || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var c))
        {
            return Default;
        }

        return new FshCustomAccentSpec(h, c);
    }

    public string ToStorage()
        => $"{Hue:0.#};{Chroma:0.###}";
}

/// <summary>
/// Canonical appearance options shared by the Blazor apps. Storage keys and
/// defaults match the React dashboard so both surfaces read the same values.
/// </summary>
public static class FshAppearanceOptions
{
    public const string DefaultAccentId = "rose";
    public const string DefaultFontId = "figtree";
    public const string CustomAccentId = "custom";

    public const string AccentStorageKey = "fsh.accent";
    public const string AccentCustomStorageKey = "fsh.accent.custom";
    public const string FontStorageKey = "fsh.font";
    public const string DensityStorageKey = "fsh.density";
    public const string ReduceMotionStorageKey = "fsh.reduce-motion";

    // AA-passing pairs (>= 4.5:1): light primaries hold white contrast text on
    // #FFFFFF/#F5F5F7; dark primaries are mid-light 400-level tones that hold
    // the #121216 contrast text used across the dark palette.
    public static readonly FshAccentOption[] Accents =
    [
        new("rose",    "Rose",    "Editorial, warm-paper default.", "#D11A42", "#FB7185"),
        new("indigo",  "Indigo",  "Confident tech-forward chassis.", "#4338CA", "#818CF8"),
        new("violet",  "Violet",  "Saturated, expressive.", "#6D28D9", "#A78BFA"),
        new("sky",     "Sky",     "Cool, calm, professional.", "#0369A1", "#38BDF8"),
        new("emerald", "Emerald", "Fresh, success-leaning.", "#047857", "#34D399"),
        new("amber",   "Amber",   "Warm, energetic.", "#B45309", "#FBBF24"),
    ];

    public static readonly FshFontOption[] Fonts =
    [
        new("figtree",       "Figtree",       "Friendly, approachable.", "'Figtree', 'Inter', 'Segoe UI', sans-serif", "Figtree:wght@300..900"),
        new("geist",         "Geist",         "Designed for screens. Default.", "'Geist', 'Inter', 'Segoe UI', sans-serif", "Geist:wght@100..900"),
        new("inter-tight",   "Inter Tight",   "Tighter modern Inter.", "'Inter Tight', 'Inter', 'Segoe UI', sans-serif", "Inter+Tight:wght@100..900"),
        new("dm-sans",       "DM Sans",       "Geometric, friendly.", "'DM Sans', 'Inter', 'Segoe UI', sans-serif", "DM+Sans:opsz,wght@9..40,100..1000"),
        new("ibm-plex",      "IBM Plex Sans", "Editorial grotesque.", "'IBM Plex Sans', 'Inter', 'Segoe UI', sans-serif", "IBM+Plex+Sans:wght@100;200;300;400;500;600;700"),
        new("manrope",       "Manrope",       "Warm, geometric.", "'Manrope', 'Inter', 'Segoe UI', sans-serif", "Manrope:wght@200..800"),
        new("plus-jakarta",  "Plus Jakarta Sans", "Modern, lightly geometric.", "'Plus Jakarta Sans', 'Inter', 'Segoe UI', sans-serif", "Plus+Jakarta+Sans:wght@200..800"),
        new("outfit",        "Outfit",        "Confident geometric sans.", "'Outfit', 'Inter', 'Segoe UI', sans-serif", "Outfit:wght@100..900"),
        new("sora",          "Sora",          "Distinctive, contemporary.", "'Sora', 'Inter', 'Segoe UI', sans-serif", "Sora:wght@100..800"),
        new("lexend",        "Lexend",        "Tuned for reading speed.", "'Lexend', 'Inter', 'Segoe UI', sans-serif", "Lexend:wght@100..900"),
        new("onest",         "Onest",         "Clean, neutral grotesque.", "'Onest', 'Inter', 'Segoe UI', sans-serif", "Onest:wght@100..900"),
        new("roboto-flex",   "Roboto Flex",   "Google's flagship variable.", "'Roboto Flex', 'Inter', 'Segoe UI', sans-serif", "Roboto+Flex:opsz,wght@8..144,100..1000"),
    ];

    public static FshAccentOption GetAccent(string id)
        => Accents.FirstOrDefault(a => a.Id == id) ?? Accents[0];

    public static FshFontOption GetFont(string id)
        => Fonts.FirstOrDefault(f => f.Id == id) ?? Fonts[0];

    /// <summary>
    /// Resolves an accent option for the current theme. The custom accent id
    /// resolves to a synthesized option whose light/dark primaries come from the
    /// hue+chroma spec (React parity: buildCustomBrandStops).
    /// </summary>
    public static FshAccentOption ResolveAccent(string accentId, FshCustomAccentSpec custom)
        => accentId == CustomAccentId ? CreateCustomAccent(custom) : GetAccent(accentId);

    /// <summary>
    /// Builds an accent option from a hue/chroma spec. Light primary is the 600
    /// stop (oklch 0.555, chroma 0.220 scaled); dark primary is the 400 stop
    /// (oklch 0.720, chroma 0.180 scaled) - the same ladder the React app uses.
    /// </summary>
    public static FshAccentOption CreateCustomAccent(FshCustomAccentSpec spec)
    {
        var h = ((spec.Hue % 360) + 360) % 360;
        var c = Math.Clamp(spec.Chroma, 0.4, 1.4);

        // Indigo ladder: 600 stop (light primary) and 400 stop (dark primary).
        var light = OklchToHex(0.555, 0.220 * c, h);
        var dark = OklchToHex(0.720, 0.180 * c, h);

        return new FshAccentOption(
            CustomAccentId,
            "Custom",
            $"Your own hue · {h:0}°",
            light,
            dark);
    }

    /// <summary>Converts an OKLCH colour to an sRGB hex string (React parity: oklch() CSS).</summary>
    public static string OklchToHex(double l, double c, double h)
    {
        var hRad = h * Math.PI / 180.0;
        var a = c * Math.Cos(hRad);
        var b = c * Math.Sin(hRad);

        // OKLab -> LMS (cubic root of the linearised values).
        var l_ = l + 0.3963377774 * a + 0.2158037573 * b;
        var m_ = l - 0.1055613458 * a - 0.0638541728 * b;
        var s_ = l - 0.0894841775 * a - 1.2914855480 * b;
        var lCubed = l_ * l_ * l_;
        var mCubed = m_ * m_ * m_;
        var sCubed = s_ * s_ * s_;

        // LMS -> linear sRGB.
        var rLin = +4.0767416621 * lCubed - 3.3077115913 * mCubed + 0.2309699292 * sCubed;
        var gLin = -1.2684380046 * lCubed + 2.6097574011 * mCubed - 0.3413193965 * sCubed;
        var bLin = -0.0041960863 * lCubed - 0.7034186147 * mCubed + 1.7076147010 * sCubed;

        return $"#{ToByte(ToSrgb(rLin)):X2}{ToByte(ToSrgb(gLin)):X2}{ToByte(ToSrgb(bLin)):X2}";

        static double ToSrgb(double c)
            => c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1.0 / 2.4) - 0.055;

        static int ToByte(double c)
            => (int)Math.Round(Math.Clamp(c, 0, 1) * 255);
    }
}
