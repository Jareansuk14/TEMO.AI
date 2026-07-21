using System.Globalization;

namespace TEMO.AI.Ai;

internal static class ThemeRandomizer
{
    public static void Apply(string projectPath, ThemePalette palette)
    {
        CssStore.Save(projectPath, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["color-primary"] = palette.Primary,
            ["color-primary-light"] = ToRgba(palette.Primary, 0.14),
            ["color-accent"] = palette.Accent,
            ["color-bg"] = palette.Background,
            ["color-bg-2"] = palette.Surface,
            ["color-bg-3"] = Darken(palette.Surface),
            ["color-border"] = ToRgba(palette.Accent, 0.34),
            ["color-border-subtle"] = ToRgba(palette.Accent, 0.16),
            ["color-text"] = palette.Text,
            ["color-text-muted"] = palette.Muted,
            ["brand-highlight"] = palette.Accent,
            ["glass-bg"] = ToRgba(palette.Background, 0.72),
            ["glass-border"] = $"1px solid {ToRgba(palette.Accent, 0.28)}",
            ["glass-bg-mobile"] = ToRgba(palette.Background, 0.9),
            ["glass-border-mobile"] = $"1px solid {ToRgba(palette.Accent, 0.36)}",
            ["nav-text"] = palette.Text,
            ["nav-active-bg"] = ToRgba(palette.Accent, 0.18),
            ["btn-primary-bg"] = palette.Accent,
            ["btn-primary-text"] = palette.Background,
            ["btn-outline-text"] = palette.Accent,
            ["page-overlay"] = ToRgba(palette.Background, 0.78),
        });
    }

    private static string ToRgba(string hex, double alpha)
    {
        if (!CssColor.TryParse(hex, out var color)) return hex;
        return $"rgba({color.R}, {color.G}, {color.B}, {alpha.ToString("0.##", CultureInfo.InvariantCulture)})";
    }

    private static string Darken(string hex)
    {
        if (!CssColor.TryParse(hex, out var color)) return hex;
        CssColor.ToHsv(color, out var h, out var s, out var v);
        return CssColor.ToHex(CssColor.FromHsv(h, s, Math.Max(0, v * 0.72), color.A));
    }
}
