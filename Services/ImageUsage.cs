namespace TEMO.AI;

internal static class ImageUsage
{
    private static readonly HashSet<string> AlwaysShow = new(StringComparer.Ordinal)
        { "banner", "background", "logo", "play", "line", "btn" };

    public static string CollectUsageText(string root)
    {
        var src = ProjectPaths.Src(root);
        if (!Directory.Exists(src)) return "";

        var imagesTs = ProjectPaths.Src(root, ImagesStore.Rel);
        var sb = new StringBuilder();
        foreach (var f in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
        {
            if (string.Equals(f, imagesTs, StringComparison.OrdinalIgnoreCase)) continue;
            if (Io.ReadOrNull(f) is { } t) sb.Append(t).Append('\n');
        }
        return sb.ToString();
    }

    private static string Prefix(string id)
    {
        var dash = id.IndexOf('-');
        return dash > 0 ? id[..dash] : id;
    }

    public static bool ConstUsed(string usageText, string id)
    {
        if (AlwaysShow.Contains(Prefix(id))) return true;
        if (ImageGroupCatalog.ByPrefix(id) is not { } spec) return true;
        foreach (var token in ImageGroupCatalog.UsageTokens(spec))
            if (!string.IsNullOrEmpty(token) && usageText.Contains(token, StringComparison.Ordinal))
                return true;
        return false;
    }

    public static bool ShowOnSite(string root, string usageText, string id, string src) =>
        ConstUsed(usageText, id)
        && !string.IsNullOrEmpty(src)
        && File.Exists(ProjectPaths.Public(root, src));

    private static readonly HashSet<string> CoreConsts = new(StringComparer.Ordinal)
        { "BANNER", "BACKGROUND", "LOGO" };

    public static bool ConstNameUsed(string usageText, string tsConst)
    {
        if (CoreConsts.Contains(tsConst)) return true;
        var spec = ImageGroupCatalog.All.FirstOrDefault(g => g.TsConst == tsConst);
        var tokens = spec is null ? [tsConst] : ImageGroupCatalog.UsageTokens(spec);
        foreach (var token in tokens)
            if (!string.IsNullOrEmpty(token) && usageText.Contains(token, StringComparison.Ordinal))
                return true;
        return false;
    }
}
