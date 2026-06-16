using System.Globalization;

namespace TEMO.AI;

internal static class CssColor
{
    private const string Pattern = @"#[0-9a-fA-F]{3,8}\b|rgba?\(\s*[^)]*\)";
    private static readonly Color Fallback = Color.FromRgb(0x16, 0x16, 0x16);

    public static bool TryExtract(string? value, out string colorText)
    {
        colorText = "";
        if (string.IsNullOrWhiteSpace(value)) return false;

        var match = Regex.Match(value, Pattern, RegexOptions.IgnoreCase);
        if (!match.Success) return false;

        colorText = match.Value.Trim();
        return true;
    }

    public static string ReplaceFirst(string value, string replacement) =>
        Regex.Replace(value, Pattern, replacement, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));

    public static bool IsRgb(string? colorText) =>
        colorText?.TrimStart().StartsWith("rgb", StringComparison.OrdinalIgnoreCase) == true;

    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static string ToRgba(Color color)
    {
        var alpha = Math.Round(color.A / 255d, 3).ToString("0.###", CultureInfo.InvariantCulture);
        return $"rgba({color.R}, {color.G}, {color.B}, {alpha})";
    }

    public static bool TryParse(string? value, out Color color)
    {
        color = Fallback;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var text = value.Trim();
        return text.StartsWith('#') ? TryParseHex(text, out color) : TryParseRgb(text, out color);
    }

    private static bool TryParseHex(string text, out Color color)
    {
        color = Fallback;
        var hex = text[1..];
        if (hex.Length is 3 or 4)
            hex = string.Concat(hex.Select(c => $"{c}{c}"));

        if (hex.Length == 6
            && TryHex(hex[..2], out var r) && TryHex(hex[2..4], out var g) && TryHex(hex[4..6], out var b))
        {
            color = Color.FromRgb(r, g, b);
            return true;
        }

        if (hex.Length == 8
            && TryHex(hex[..2], out r) && TryHex(hex[2..4], out g)
            && TryHex(hex[4..6], out b) && TryHex(hex[6..8], out var a))
        {
            color = Color.FromArgb(a, r, g, b);
            return true;
        }

        return false;
    }

    private static bool TryParseRgb(string text, out Color color)
    {
        color = Fallback;
        var rgba = Regex.Match(text, @"^rgba?\(([^)]*)\)$", RegexOptions.IgnoreCase);
        if (!rgba.Success) return false;

        var parts = rgba.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 3) return false;

        if (!TryChannel(parts[0], out var r) || !TryChannel(parts[1], out var g) || !TryChannel(parts[2], out var b))
            return false;

        var alpha = parts.Length >= 4 ? ParseAlpha(parts[3]) : (byte)255;
        color = Color.FromArgb(alpha, r, g, b);
        return true;
    }

    public static Color FromHsv(double hue, double saturation, double value, byte alpha)
    {
        hue = ((hue % 360) + 360) % 360;
        var chroma = value * saturation;
        var x = chroma * (1 - Math.Abs((hue / 60) % 2 - 1));
        var m = value - chroma;

        var (r, g, b) = hue switch
        {
            < 60 => (chroma, x, 0d),
            < 120 => (x, chroma, 0d),
            < 180 => (0d, chroma, x),
            < 240 => (0d, x, chroma),
            < 300 => (x, 0d, chroma),
            _ => (chroma, 0d, x)
        };

        return Color.FromArgb(alpha,
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }

    public static void ToHsv(Color color, out double hue, out double saturation, out double value)
    {
        var r = color.R / 255d;
        var g = color.G / 255d;
        var b = color.B / 255d;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        hue = delta == 0 ? 0
            : max == r ? 60 * (((g - b) / delta) % 6)
            : max == g ? 60 * (((b - r) / delta) + 2)
            : 60 * (((r - g) / delta) + 4);
        if (hue < 0) hue += 360;

        saturation = max == 0 ? 0 : delta / max;
        value = max;
    }

    private static bool TryHex(string part, out byte value) =>
        byte.TryParse(part, NumberStyles.HexNumber, null, out value);

    private static bool TryChannel(string part, out byte value)
    {
        value = 0;
        part = part.Trim();
        if (part.EndsWith('%')
            && double.TryParse(part[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
        {
            value = (byte)Math.Clamp(Math.Round(percent * 255 / 100), 0, 255);
            return true;
        }
        if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return false;
        value = (byte)Math.Clamp(Math.Round(number), 0, 255);
        return true;
    }

    private static byte ParseAlpha(string part)
    {
        part = part.Trim();
        if (part.EndsWith('%')
            && double.TryParse(part[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
            return (byte)Math.Clamp(Math.Round(percent * 255 / 100), 0, 255);
        if (double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return (byte)Math.Clamp(Math.Round(number <= 1 ? number * 255 : number), 0, 255);
        return 255;
    }
}
