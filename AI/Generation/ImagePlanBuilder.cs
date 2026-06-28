namespace TEMO.AI.Ai;

internal static class ImagePlanBuilder
{
    public static IReadOnlyList<ImagePlanItem> Build(string projectPath)
    {
        var content = ImagesStore.ReadConfig(projectPath);
        if (content.Length == 0) return [];

        var defs = ImagesStore.DiscoverDefs(content, ImagesStore.IsPlayButtonUsed(projectPath),
            ImagesStore.SeoImageNumbers(projectPath));

        var sources = ReadSourceFiles(projectPath);

        return defs
            .Where(def => IsUsed(sources, def.Id))
            .Select(def => BuildItem(content, def))
            .Where(item => !string.IsNullOrWhiteSpace(item.Src))
            .ToList();
    }

    private static List<string> ReadSourceFiles(string projectPath)
    {
        var src = ProjectPaths.Src(projectPath);
        if (!Directory.Exists(src)) return [];

        var list = new List<string>();
        foreach (var file in Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories)
                     .Where(f => f.EndsWith(".astro", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)))
        {
            if (file.EndsWith(Path.Combine("lib", "images.ts"), StringComparison.OrdinalIgnoreCase)) continue;
            if (Io.ReadOrNull(file) is { } text) list.Add(text);
        }
        return list;
    }

    private static bool IsUsed(List<string> sources, string id) => id switch
    {
        "banner" => Uses(sources, "BANNER"),
        "background" => Uses(sources, "BACKGROUND"),
        "logo" => Uses(sources, "LOGO") || Uses(sources, "getLogoSrc"),
        "play" => Uses(sources, "PLAY_BUTTON"),
        var s when s.StartsWith("btn-") => UsesButton(sources, s["btn-".Length..]),
        var s when s.StartsWith("game-") => Uses(sources, "GAME_CARDS"),
        var s when s.StartsWith("promo-") => Uses(sources, "PROMOS") || Uses(sources, "getPromoPreview"),
        var s when s.StartsWith("seo-") => Uses(sources, "SEO_ARTICLE_IMAGES"),
        _ => true,
    };

    private static bool UsesButton(List<string> sources, string key) =>
        Uses(sources, $"ACTION_BUTTONS.{key}")
        || Uses(sources, $"{{ {key}")
        || Uses(sources, $", {key}")
        || Uses(sources, $"{key} }}");

    private static bool Uses(List<string> sources, string token) =>
        sources.Any(s => s.Contains(token, StringComparison.Ordinal));

    private static ImagePlanItem BuildItem(
        string imagesContent,
        (string Id, string Label, string Group, bool HasAlt) def)
    {
        var (src, alt) = ImagesStore.ReadValues(imagesContent, def.Id);
        var (width, height) = ImageSizeCatalog.Size(def.Id);
        var role = RoleOf(def.Id);
        return new ImagePlanItem(def.Id, def.Label, def.Group, role, src, alt, width, height, def.HasAlt);
    }

    private static string BlockFor(string content, string id) => id switch
    {
        "banner" => TsBlockParser.FirstBlock(content, "export const BANNER") ?? "",
        "background" => TsBlockParser.FirstBlock(content, "export const BACKGROUND") ?? "",
        "logo" => TsBlockParser.FirstBlock(content, "export const LOGO") ?? "",
        "play" => TsBlockParser.FirstBlock(content, "export const PLAY_BUTTON") ?? "",
        var s when s.StartsWith("btn-") => NestedBlock(content, "ACTION_BUTTONS", s["btn-".Length..]),
        var s when s.StartsWith("game-") => ArrayBlock(content, "GAME_CARDS", int.Parse(s["game-".Length..])),
        var s when s.StartsWith("promo-") => ArrayBlock(content, ImagesStore.PromoArray, int.Parse(s["promo-".Length..])),
        var s when s.StartsWith("review-") => ArrayBlock(content, "REVIEWS", int.Parse(s["review-".Length..])),
        var s when s.StartsWith("seo-") => RecordBlock(content, "SEO_ARTICLE_IMAGES", $"seo-{s["seo-".Length..]}"),
        _ => "",
    };

    private static string NestedBlock(string content, string outer, string key)
    {
        var ob = TsBlockParser.FirstBlock(content, $"export const {outer}");
        return ob is null ? "" : TsBlockParser.FirstBlock(ob, $"{key}:") ?? "";
    }

    private static string ArrayBlock(string content, string name, int index)
    {
        var m = TsBlockParser.ArrayMatch(content, name);
        if (!m.Success) return "";
        var blocks = TsBlockParser.AllBlocks(m.Groups[2].Value);
        return index >= 0 && index < blocks.Count ? blocks[index] : "";
    }

    private static string RecordBlock(string content, string name, string key)
    {
        var ob = TsBlockParser.FirstBlock(content, $"export const {name}");
        return ob is null ? "" : TsBlockParser.FirstBlock(ob, $"\"{key}\":") ?? "";
    }

    private static string RoleOf(string id) => id switch
    {
        "banner" => "hero banner",
        "background" => "site background",
        "logo" => "brand logo",
        "play" => "play button",
        var s when s.StartsWith("btn-") => "action button",
        var s when s.StartsWith("game-") => "game card",
        var s when s.StartsWith("promo-") => "promotion card",
        var s when s.StartsWith("seo-") => "SEO article image",
        _ => "website image",
    };
}
