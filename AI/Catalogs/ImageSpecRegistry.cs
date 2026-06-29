namespace TEMO.AI.Ai;

internal static class ImageSpecRegistry
{
    private static Dictionary<string, ManifestImage>? _byPrefix;

    public static void Reload() => _byPrefix = null;

    private static Dictionary<string, ManifestImage> Map => _byPrefix ??= Build();

    private static Dictionary<string, ManifestImage> Build()
    {
        var map = new Dictionary<string, ManifestImage>(StringComparer.Ordinal);
        foreach (var def in SectionCatalog.All)
            foreach (var img in def.Images)
            {
                if (string.IsNullOrWhiteSpace(img.Id)) continue;
                var prefix = Prefix(img.Id);
                if (!map.TryGetValue(prefix, out var existing) || (HasSize(img) && !HasSize(existing)))
                    map[prefix] = img;
            }
        return map;
    }

    public static ManifestImage? Find(string id) =>
        Map.TryGetValue(Prefix(id), out var spec) ? spec : null;

    public static (int Width, int Height)? Size(string id)
    {
        if (Find(id) is not { } spec) return null;
        if (HasSize(spec)) return (spec.Width, spec.Height);
        return RatioMap.Resolve(spec.Ratio);
    }

    public static string? Role(string id) =>
        Find(id)?.Role is { Length: > 0 } role ? role : null;

    public static bool? Required(string id) =>
        Find(id) is { } spec ? spec.Required : null;

    private static bool HasSize(ManifestImage i) => i.Width > 0 && i.Height > 0;

    private static string Prefix(string id)
    {
        var dash = id.IndexOf('-');
        return dash > 0 ? id[..dash] : id;
    }
}
