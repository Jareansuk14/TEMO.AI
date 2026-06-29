namespace TEMO.AI;

internal static class ImageGroupCatalog
{
    public const string FileName = "image-groups.json";

    private static readonly HashSet<string> HardcodedPrefixes = new(StringComparer.Ordinal)
    {
        "banner", "background", "logo", "play", "btn",
        "game", "promo", "review", "seo", "line", "provider",
    };

    private static List<ImageGroupSpec>? _all;

    public static void Reload() => _all = null;

    public static IReadOnlyList<ImageGroupSpec> All => _all ??= Load();

    public static IEnumerable<ImageGroupSpec> ExtraGroups =>
        All.Where(g => !string.IsNullOrWhiteSpace(g.Key) && !HardcodedPrefixes.Contains(g.Key));

    public static ImageGroupSpec? FindExtra(string id)
    {
        var prefix = Prefix(id);
        if (HardcodedPrefixes.Contains(prefix)) return null;
        return ExtraGroups.FirstOrDefault(g => g.Key == prefix);
    }

    public static ImageGroupSpec? Find(string key) =>
        string.IsNullOrWhiteSpace(key) ? null : All.FirstOrDefault(g => g.Key == key);

    public static ImageGroupSpec? ByPrefix(string id) => Find(Prefix(id));

    public static string ImageTypeOf(string id)
    {
        var prefix = Prefix(id);
        var manifestType = SectionCatalog.All
            .Select(d => d.Spec)
            .FirstOrDefault(s => s.ImageGroup == prefix && !string.IsNullOrWhiteSpace(s.ImageType))
            ?.ImageType;
        return manifestType ?? ByPrefix(id)?.ResolvedImageType ?? "normal";
    }

    public static (int Width, int Height)? Size(string id)
    {
        if (FindExtra(id) is not { } spec) return null;
        if (spec.Width > 0 && spec.Height > 0) return (spec.Width, spec.Height);
        return null;
    }

    public static IEnumerable<string> UsageTokens(ImageGroupSpec spec) =>
        spec.UsageTokens.Count > 0 ? spec.UsageTokens : [spec.TsConst];

    private static string Prefix(string id)
    {
        var dash = id.IndexOf('-');
        return dash > 0 ? id[..dash] : id;
    }

    private static List<ImageGroupSpec> Load()
    {
        var defaults = Defaults();
        var path = ResolvePath();
        if (path is null || JsonFile.Read<List<ImageGroupSpec>>(path) is not { Count: > 0 } loaded)
            return defaults;

        var byKey = defaults.ToDictionary(g => g.Key, StringComparer.Ordinal);
        foreach (var g in loaded)
            if (!string.IsNullOrWhiteSpace(g.Key))
                byKey[g.Key] = g;
        return byKey.Values.ToList();
    }

    private static string? ResolvePath()
    {
        if (Workspace.FindAncestorDir(Path.Combine("Templates", "Component")) is { } local)
        {
            var p = Path.Combine(local, FileName);
            if (File.Exists(p)) return p;
        }
        var shipped = Path.Combine(ComponentStore.Root, FileName);
        return File.Exists(shipped) ? shipped : null;
    }

    private static List<ImageGroupSpec> Defaults() =>
    [
        new() { Key = "background", TsConst = "BACKGROUND", Structure = "named", Role = "site background", Group = "หน้าหลัก", Label = "พื้นหลัง", HasAlt = false, ImageType = "normal", Width = 1920, Height = 1080 },
        new() { Key = "banner", TsConst = "BANNER", Structure = "named", Role = "hero banner", Group = "หน้าหลัก", Label = "Banner", HasAlt = true, ImageType = "normal", UseLogoReference = true, CompositionMin = 1, CompositionMax = 4, Width = 1600, Height = 900 },
        new() { Key = "heroMascot", TsConst = "HERO_MASCOT", Structure = "named", UsageTokens = ["HERO_MASCOT"], Role = "transparent hero character mascot", Group = "HERO", Label = "Hero Mascot", HasAlt = true, ImageType = "transparent", CompositionMin = 1, CompositionMax = 1, Width = 1024, Height = 1024 },
        new() { Key = "bannerMascot", TsConst = "BANNER_MASCOT", Structure = "named", UsageTokens = ["BANNER_MASCOT"], Role = "transparent banner character mascot", Group = "หน้าหลัก", Label = "Banner Mascot", HasAlt = true, ImageType = "transparent", CompositionMin = 1, CompositionMax = 1, Width = 1024, Height = 1024 },
        new() { Key = "seoMascot", TsConst = "SEO_MASCOT", Structure = "named", UsageTokens = ["SEO_MASCOT"], Role = "transparent SEO character mascot", Group = "บทความ SEO", Label = "SEO Mascot", HasAlt = true, ImageType = "transparent", CompositionMin = 1, CompositionMax = 1, Width = 1024, Height = 1024 },
        new() { Key = "gameMascot", TsConst = "GAME_MASCOT", Structure = "named", UsageTokens = ["GAME_MASCOT"], Role = "transparent game character mascot", Group = "เกม", Label = "Game Mascot", HasAlt = true, ImageType = "transparent", CompositionMin = 1, CompositionMax = 1, Width = 1024, Height = 1024 },
        new() { Key = "logo", TsConst = "LOGO", Structure = "named", UsageTokens = ["LOGO", "getLogoSrc"], Role = "brand logo", Group = "Logo & ปุ่ม", Label = "Logo", HasAlt = false, ImageType = "transparent", Transparent = true, Width = 512, Height = 512 },
        new() { Key = "play", TsConst = "PLAY_BUTTON", Structure = "named", Role = "play button", Group = "Logo & ปุ่ม", Label = "Play Button", HasAlt = false, ImageType = "normal", Width = 512, Height = 240 },
        new() { Key = "btn", TsConst = "ACTION_BUTTONS", Structure = "nestedRecord", Role = "action button", Group = "Logo & ปุ่ม", Label = "ปุ่ม {n}", HasAlt = true, ImageType = "button", Transparent = true, Width = 512, Height = 240 },
        new() { Key = "game", TsConst = "GAME_CARDS", Structure = "array", Role = "game character card", Group = "เกม", Label = "เกม{n}", HasAlt = true, ImageType = "normal", Width = 1024, Height = 1536 },
        new() { Key = "promo", TsConst = "PROMOS", Structure = "array", UsageTokens = ["PROMOS", "getPromoPreview"], Role = "promotion card", Group = "โปรโมชั่น", Label = "โปร {n}", HasAlt = true, ImageType = "normal", UseLogoReference = true, CaptionSource = "promo", CompositionMin = 1, CompositionMax = 3, Width = 1536, Height = 864 },
        new() { Key = "review", TsConst = "REVIEWS", Structure = "array", Role = "review", Group = "รีวิว", Label = "รีวิว {n}", HasAlt = true, ImageType = "normal", Width = 1536, Height = 864 },
        new() { Key = "seo", TsConst = "SEO_ARTICLE_IMAGES", Structure = "stringRecord", Role = "SEO article image", Group = "บทความ SEO", Label = "บทความ {n}", HasAlt = true, ImageType = "normal", UseLogoReference = true, CaptionSource = "seo", CompositionMin = 1, CompositionMax = 3, Width = 1536, Height = 864 },
        new() { Key = "line", TsConst = "LINE_QR", Structure = "named", Role = "contact QR", Group = "ติดต่อ", Label = "LINE QR", HasAlt = false, ImageType = "normal", Width = 180, Height = 180 },
        new() { Key = "provider", TsConst = "PROVIDER", Structure = "named", Role = "game providers", Group = "หน้าหลัก", Label = "ค่ายเกม", HasAlt = true, ImageType = "normal", Width = 1362, Height = 382 },
    ];
}
