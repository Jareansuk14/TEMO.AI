namespace TEMO.AI;

internal static class RatioMap
{
    public static bool IsValid(string ratio)
    {
        if (string.IsNullOrWhiteSpace(ratio)) return false;
        ratio = ratio.Trim().ToLowerInvariant();
        return ratio == "auto" || Resolve(ratio) is not null;
    }

    public static (int Width, int Height)? Resolve(string? ratio)
    {
        if (string.IsNullOrWhiteSpace(ratio)) return null;
        ratio = ratio.Trim().ToLowerInvariant();
        if (ratio == "auto") return null;

        return ratio switch
        {
            "1:1" => (1024, 1024),
            "16:9" => (1536, 864),
            "9:16" => (864, 1536),
            "4:3" => (1280, 960),
            "3:4" => (960, 1280),
            "3:2" => (1536, 1024),
            "2:3" => (1024, 1536),
            "2:4" => (768, 1536),
            "4:2" => (1536, 768),
            _ => Parse(ratio),
        };
    }

    private static (int Width, int Height)? Parse(string ratio)
    {
        var parts = ratio.Split(':');
        if (parts.Length != 2
            || !double.TryParse(parts[0], out var w) || !double.TryParse(parts[1], out var h)
            || w <= 0 || h <= 0)
            return null;

        const double longEdge = 1536;
        return w >= h
            ? ((int)longEdge, (int)Math.Round(longEdge * h / w))
            : ((int)Math.Round(longEdge * w / h), (int)longEdge);
    }
}
