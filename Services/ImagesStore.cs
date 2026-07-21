namespace TEMO.AI;

internal static class ImagesStore
{
    public const string Rel = @"lib\images.ts";

    public const string PromoArray = "PROMOS";
    public const int ButtonWidth = 512;
    public const int ButtonHeight = 240;
    public const int GameWidth = 1024;
    public const int GameHeight = 1536;

    private static readonly Regex WidthPattern = new(@"(\bwidth:\s*)\d+", RegexOptions.Compiled);
    private static readonly Regex HeightPattern = new(@"(\bheight:\s*)\d+", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex SeoImagesBlock = new(
        @"(export\s+const\s+SEO_ARTICLE_IMAGES\b[^=]*=\s*\{)([\s\S]*?)(\n\};)", RegexOptions.Compiled);
    private static readonly Regex SeoKeyPattern = new(@"""seo-(\d+)""\s*:", RegexOptions.Compiled);
    private static readonly Regex ButtonKeyPattern = new(@"(\w+)\s*:\s*\{", RegexOptions.Compiled);
    private static readonly Regex AltValuePattern = new(@"(\balt:\s*"")[^""]*("")", RegexOptions.Compiled);

    private static string Path(string root) => ProjectPaths.Src(root, Rel);

    public static string ReadConfig(string root) => Io.ReadOrNull(Path(root)) ?? "";

    private static string SeoFile(string root) =>
        ProjectPaths.Src(root, @"components\sections\Seo.astro");

    public static IReadOnlyList<int> SeoImageNumbers(string root)
    {
        if (Io.ReadOrNull(SeoFile(root)) is not { } seo || !seo.Contains("SEO_ARTICLE_IMAGES"))
            return [];

        if (Io.ReadOrNull(Path(root)) is not { } text) return [];
        var m = SeoImagesBlock.Match(text);
        if (!m.Success) return [];

        return SeoKeyPattern.Matches(m.Groups[2].Value)
            .Select(km => int.Parse(km.Groups[1].Value))
            .Distinct().OrderBy(n => n).ToList();
    }

    public static void SyncStandard(string root) => NormalizeStandardDimensions(root);

    public static void ApplyImages(string root, ContentSpec spec, int count, Action<string> deleteFile)
    {
        if (count < 0 || string.IsNullOrWhiteSpace(spec.ImageGroup)) return;
        if (ImageGroupCatalog.Find(spec.ImageGroup) is not { } group) return;

        var size = RatioMap.Resolve(spec.ImageRatio)
            ?? (group.Width > 0 && group.Height > 0 ? (group.Width, group.Height) : (1536, 864));

        switch (group.Structure)
        {
            case "array":
                ResizeArrayGroup(root, group, count, size, deleteFile);
                break;
            case "stringRecord":
                ResizeRecordGroup(root, group, count, size, deleteFile);
                break;
            case "named":
                ResizeNamedGroup(root, group, size);
                break;
        }
    }

    private static void ResizeArrayGroup(string root, ImageGroupSpec g, int count, (int Width, int Height) size, Action<string> deleteFile)
    {
        var path = Path(root);
        if (Io.ReadOrNull(path) is not { } text) return;
        var m = TsBlockParser.ArrayMatch(text, g.TsConst);
        if (!m.Success) return;

        var blocks = TsBlockParser.AllBlocks(m.Groups[2].Value);
        var removed = new List<string>();
        if (blocks.Count > count)
        {
            for (int i = count; i < blocks.Count; i++)
                removed.Add(TsBlockParser.QuotedVal(blocks[i], "src"));
            blocks = blocks.Take(count).ToList();
        }
        else
        {
            for (int i = blocks.Count; i < count; i++)
                blocks.Add($"{{ src: \"/images/{g.Key}{i + 1}.webp\", alt: \"\", width: {size.Width}, height: {size.Height} }}");
        }

        blocks = blocks.Select(b => NormalizeImageBlock(b, size)).ToList();
        WriteArrayBody(path, text, m, blocks);
        DeleteAll(removed, deleteFile);
    }

    private static void ResizeRecordGroup(string root, ImageGroupSpec g, int count, (int Width, int Height) size, Action<string> deleteFile)
    {
        var path = Path(root);
        if (Io.ReadOrNull(path) is not { } text) return;
        if (TsBlockParser.FirstBlock(text, $"export const {g.TsConst}") is not { } block) return;

        var existing = new Dictionary<int, string>();
        foreach (Match km in Regex.Matches(block, $"\"{Regex.Escape(g.Key)}-(\\d+)\"\\s*:"))
            if (TsBlockParser.FirstBlock(block[km.Index..], $"\"{g.Key}-{km.Groups[1].Value}\":") is { } b)
                existing[int.Parse(km.Groups[1].Value)] = b;

        var removed = existing.Where(kv => kv.Key > count)
            .Select(kv => TsBlockParser.QuotedVal(kv.Value, "src")).ToList();

        var sb = new StringBuilder("{\n");
        for (int k = 1; k <= count; k++)
        {
            var item = existing.TryGetValue(k, out var b)
                ? WhitespaceRun.Replace(b, " ").Trim()
                : $"{{ src: \"/images/{g.Key}{k}.webp\", alt: \"\", width: {size.Width}, height: {size.Height} }}";
            item = NormalizeImageBlock(item, size);
            sb.Append($"  \"{g.Key}-{k}\": {item},\n");
        }
        sb.Append('}');

        var updated = text.Replace(block, sb.ToString());
        if (updated != text) Io.Write(path, updated);
        DeleteAll(removed, deleteFile);
    }

    private static void ResizeNamedGroup(string root, ImageGroupSpec g, (int Width, int Height) size)
    {
        var path = Path(root);
        if (Io.ReadOrNull(path) is not { } text) return;
        if (TsBlockParser.FirstBlock(text, $"export const {g.TsConst}") is not { } block) return;
        var updated = text.Replace(block, NormalizeSingleImageBlock(block, size));
        if (updated != text) Io.Write(path, updated);
    }

    public static void NormalizeStandardDimensions(string root)
    {
        var path = Path(root);
        if (Io.ReadOrNull(path) is not { } text) return;
        var updated = NormalizeNamedConstDimension(text, "BANNER", "banner");
        updated = NormalizeNamedConstDimension(updated, "BACKGROUND", "background");
        updated = NormalizeNamedConstDimension(updated, "LOGO", "logo");
        updated = NormalizeActionButtonDimensions(updated);
        updated = NormalizeGameCardDimensions(updated);
        updated = NormalizePromoDimensions(updated);
        updated = NormalizeSeoDimensions(updated);
        if (updated != text) Io.Write(path, updated);
    }

    private static string NormalizeNamedConstDimension(string text, string name, string id)
    {
        if (TsBlockParser.FirstBlock(text, $"export const {name}") is not { } block) return text;
        return text.Replace(block, NormalizeSingleImageBlock(block, ImageSizeCatalog.Size(id)));
    }

    private static string NormalizeActionButtonDimensions(string text)
    {
        if (TsBlockParser.FirstBlock(text, "export const ACTION_BUTTONS") is not { } block) return text;
        var updated = block;
        foreach (Match match in ButtonKeyPattern.Matches(block))
        {
            var key = match.Groups[1].Value;
            if (TsBlockParser.FirstBlock(block[match.Index..], $"{key}:") is not { } itemBlock) continue;
            var width = ImageSizeCatalog.Size($"btn-{key}").Width;
            var normalized = WidthPattern.Replace(itemBlock, m => m.Groups[1].Value + width);
            normalized = EnsureNumberField(normalized, "width", width);
            updated = updated.Replace(itemBlock, normalized);
        }
        return text.Replace(block, updated);
    }

    private static string NormalizeGameCardDimensions(string text)
    {
        var m = TsBlockParser.ArrayMatch(text, "GAME_CARDS");
        if (!m.Success) return text;
        var blocks = TsBlockParser.AllBlocks(m.Groups[2].Value)
            .Select((b, i) => NormalizeImageBlock(b, ImageSizeCatalog.Size($"game-{i}")))
            .ToList();
        var rebuilt = "\n" + string.Join("\n", blocks.Select(b => "  " + WhitespaceRun.Replace(b, " ").Trim() + ",")) + "\n";
        return text[..m.Groups[2].Index] + rebuilt + text[(m.Groups[2].Index + m.Groups[2].Length)..];
    }

    private static string NormalizePromoDimensions(string text)
    {
        var m = TsBlockParser.ArrayMatch(text, PromoArray);
        if (!m.Success) return text;
        var blocks = TsBlockParser.AllBlocks(m.Groups[2].Value)
            .Select((b, i) => NormalizeImageBlock(b, ImageSizeCatalog.Size($"promo-{i}")))
            .ToList();
        var rebuilt = "\n" + string.Join("\n", blocks.Select(b => "  " + WhitespaceRun.Replace(b, " ").Trim() + ",")) + "\n";
        return text[..m.Groups[2].Index] + rebuilt + text[(m.Groups[2].Index + m.Groups[2].Length)..];
    }

    private static string NormalizeSeoDimensions(string text)
    {
        if (TsBlockParser.FirstBlock(text, "export const SEO_ARTICLE_IMAGES") is not { } block) return text;
        var updated = block;
        foreach (Match match in SeoKeyPattern.Matches(block))
        {
            var key = match.Groups[1].Value;
            if (TsBlockParser.FirstBlock(block[match.Index..], $"\"seo-{key}\":") is not { } itemBlock) continue;
            updated = updated.Replace(itemBlock, NormalizeImageBlock(itemBlock, ImageSizeCatalog.Size($"seo-{key}")));
        }
        return text.Replace(block, updated);
    }

    private static string NormalizeImageBlock(string block, (int Width, int Height) size)
    {
        var updated = WidthPattern.Replace(block, m => m.Groups[1].Value + size.Width);
        updated = HeightPattern.Replace(updated, m => m.Groups[1].Value + size.Height);
        return updated;
    }

    private static string NormalizeSingleImageBlock(string block, (int Width, int Height) size)
    {
        var updated = NormalizeImageBlock(block, size);
        updated = EnsureNumberField(updated, "width", size.Width);
        updated = EnsureNumberField(updated, "height", size.Height);
        return updated;
    }

    private static string EnsureNumberField(string block, string key, int value)
    {
        if (Regex.IsMatch(block, $@"\b{Regex.Escape(key)}:\s*\d+")) return block;

        var insertAt = block.LastIndexOf('}');
        if (insertAt < 0) return block;

        var before = block[..insertAt].TrimEnd();
        var after = block[insertAt..];
        var comma = before.EndsWith(',') ? "" : ",";
        return $"{before}{comma}\n  {key}: {value},\n{after}";
    }

    private static void WriteArrayBody(string path, string text, Match arrayMatch, List<string> blocks)
    {
        var rebuilt = "\n" + string.Join("\n",
            blocks.Select(b => "  " + WhitespaceRun.Replace(b, " ").Trim() + ",")) + "\n";
        var updated = text[..arrayMatch.Groups[2].Index] + rebuilt
            + text[(arrayMatch.Groups[2].Index + arrayMatch.Groups[2].Length)..];
        Io.Write(path, updated);
    }

    private static void DeleteAll(IEnumerable<string> srcs, Action<string> deleteFile)
    {
        foreach (var src in srcs)
            if (!string.IsNullOrEmpty(src)) deleteFile(src);
    }

    public static IReadOnlyList<(string Id, string Label, string Group, bool HasAlt)> DiscoverDefs(
        string content, bool includePlayButton, IReadOnlyList<int> seoNumbers)
    {
        var defs = new List<(string, string, string, bool)>
        {
            ("banner",     "Banner",   "หน้าหลัก",   true),
            ("background", "พื้นหลัง", "หน้าหลัก",   false),
            ("logo",       "Logo",     "Logo & ปุ่ม", false),
        };

        if (TsBlockParser.FirstBlock(content, "export const ACTION_BUTTONS") is { } btnBlock)
        {
            var keys = ButtonKeyPattern.Matches(btnBlock).Select(m => m.Groups[1].Value).ToList();
            for (int i = 0; i < keys.Count; i++)
                defs.Add(($"btn-{keys[i]}", $"ปุ่ม {i + 1}", "Logo & ปุ่ม", true));
        }

        if (includePlayButton)
            defs.Add(("play", "Play Button", "Logo & ปุ่ม", false));

        for (int i = 0; i < TsBlockParser.CountArray(content, "GAME_CARDS"); i++)
            defs.Add(($"game-{i}", $"เกม{i + 1}", "เกม", true));

        for (int i = 0; i < TsBlockParser.CountArray(content, PromoArray); i++)
            defs.Add(($"promo-{i}", $"โปร {i + 1}", "โปรโมชั่น", true));

        for (int i = 0; i < TsBlockParser.CountArray(content, "REVIEWS"); i++)
            defs.Add(($"review-{i}", $"รีวิว {i + 1}", "รีวิว", true));

        foreach (var n in seoNumbers)
            defs.Add(($"seo-{n}", $"บทความ {n}", "บทความ SEO", true));

        defs.AddRange(DiscoverExtraDefs(content));

        return defs;
    }
    private static IEnumerable<(string, string, string, bool)> DiscoverExtraDefs(string content)
    {
        var defs = new List<(string, string, string, bool)>();
        foreach (var g in ImageGroupCatalog.ExtraGroups)
        {
            if (string.IsNullOrWhiteSpace(g.TsConst)) continue;
            var label = string.IsNullOrWhiteSpace(g.Label) ? g.Key : g.Label;

            switch (g.Structure)
            {
                case "named":
                    if (TsBlockParser.FirstBlock(content, $"export const {g.TsConst}") is not null)
                        defs.Add((g.Key, label.Replace("{n}", ""), g.Group, g.HasAlt));
                    break;

                case "nestedRecord":
                    if (TsBlockParser.FirstBlock(content, $"export const {g.TsConst}") is { } nested)
                    {
                        var keys = ButtonKeyPattern.Matches(nested).Select(m => m.Groups[1].Value).ToList();
                        for (int i = 0; i < keys.Count; i++)
                            defs.Add(($"{g.Key}-{keys[i]}", label.Replace("{n}", (i + 1).ToString()), g.Group, g.HasAlt));
                    }
                    break;

                case "array":
                    for (int i = 0; i < TsBlockParser.CountArray(content, g.TsConst); i++)
                        defs.Add(($"{g.Key}-{i}", label.Replace("{n}", (i + 1).ToString()), g.Group, g.HasAlt));
                    break;

                case "stringRecord":
                    if (TsBlockParser.FirstBlock(content, $"export const {g.TsConst}") is { } rec)
                        foreach (Match km in Regex.Matches(rec, "\"([^\"]+)\"\\s*:"))
                            defs.Add((km.Groups[1].Value, label, g.Group, g.HasAlt));
                    break;
            }
        }
        return defs;
    }

    public static bool IsPlayButtonUsed(string root)
    {
        var srcDir = ProjectPaths.Src(root);
        if (!Directory.Exists(srcDir)) return false;
        foreach (var file in Directory.EnumerateFiles(srcDir, "*.astro", SearchOption.AllDirectories))
            if (Io.ReadOrNull(file) is { } text && text.Contains("PLAY_BUTTON"))
                return true;
        return false;
    }

    public static (string Src, string Alt) ReadValues(string content, string id) => id switch
    {
        "banner"     => ReadNamedConst(content, "BANNER"),
        "background" => ReadNamedConst(content, "BACKGROUND"),
        "logo"       => ReadNamedConst(content, "LOGO"),
        "play"       => ReadNamedConst(content, "PLAY_BUTTON"),
        "line-qr"    => ReadNamedConst(content, "LINE_QR"),
        var s when s.StartsWith("btn-")    => ReadNestedKey(content, "ACTION_BUTTONS", s["btn-".Length..]),
        var s when s.StartsWith("game-")   => ReadArrayAt(content, "GAME_CARDS", int.Parse(s["game-".Length..])),
        var s when s.StartsWith("promo-")  => ReadArrayAt(content, PromoArray, int.Parse(s["promo-".Length..])),
        var s when s.StartsWith("review-") => ReadArrayAt(content, "REVIEWS", int.Parse(s["review-".Length..])),
        var s when s.StartsWith("seo-")    => ReadRecordAt(content, "SEO_ARTICLE_IMAGES", $"seo-{s["seo-".Length..]}"),
        _ => ReadExtra(content, id),
    };

    private static (string Src, string Alt) ReadExtra(string content, string id)
    {
        if (ImageGroupCatalog.FindExtra(id) is not { } g) return ("", "");

        return g.Structure switch
        {
            "named" => ReadNamedConst(content, g.TsConst),
            "nestedRecord" => ReadNestedKey(content, g.TsConst, id[(g.Key.Length + 1)..]),
            "array" => int.TryParse(id[(g.Key.Length + 1)..], out var idx)
                ? ReadArrayAt(content, g.TsConst, idx) : ("", ""),
            "stringRecord" => ReadRecordAt(content, g.TsConst, id),
            _ => ("", ""),
        };
    }

    public static (string Src, string Alt) ReadNamedConst(string content, string name)
    {
        var block = TsBlockParser.FirstBlock(content, $"export const {name}");
        return block is null ? ("", "") : (TsBlockParser.QuotedVal(block, "src"), TsBlockParser.QuotedVal(block, "alt"));
    }

    private static (string Src, string Alt) ReadNestedKey(string content, string outer, string key)
    {
        var ob = TsBlockParser.FirstBlock(content, $"export const {outer}");
        if (ob is null) return ("", "");
        var ib = TsBlockParser.FirstBlock(ob, $"{key}:");
        return ib is null ? ("", "") : (TsBlockParser.QuotedVal(ib, "src"), TsBlockParser.QuotedVal(ib, "alt"));
    }

    private static (string Src, string Alt) ReadArrayAt(string content, string name, int index)
    {
        var m = TsBlockParser.ArrayMatch(content, name);
        if (!m.Success) return ("", "");
        var blocks = TsBlockParser.AllBlocks(m.Groups[2].Value);
        return index < blocks.Count
            ? (TsBlockParser.QuotedVal(blocks[index], "src"), TsBlockParser.QuotedVal(blocks[index], "alt"))
            : ("", "");
    }

    private static (string Src, string Alt) ReadRecordAt(string content, string name, string key)
    {
        var ob = TsBlockParser.FirstBlock(content, $"export const {name}");
        if (ob is null) return ("", "");
        var ib = TsBlockParser.FirstBlock(ob, $"\"{key}\":");
        return ib is null ? ("", "") : (TsBlockParser.QuotedVal(ib, "src"), TsBlockParser.QuotedVal(ib, "alt"));
    }

    private static string? ReadEntryBlock(string content, string outer, string key)
    {
        var ob = TsBlockParser.FirstBlock(content, $"export const {outer}");
        return ob is null ? null : TsBlockParser.FirstBlock(ob, $"{key}:");
    }

    private static string? ReadArrayBlock(string content, string name, int index)
    {
        var m = TsBlockParser.ArrayMatch(content, name);
        if (!m.Success) return null;
        var blocks = TsBlockParser.AllBlocks(m.Groups[2].Value);
        return index >= 0 && index < blocks.Count ? blocks[index] : null;
    }

    private static string? ReadRecordBlock(string content, string name, string key)
    {
        var ob = TsBlockParser.FirstBlock(content, $"export const {name}");
        return ob is null ? null : TsBlockParser.FirstBlock(ob, $"\"{key}\":");
    }

    public static void SaveEntry(string root, string oldSrc, string newSrc, string newAlt, bool hasAlt,
        string? id = null, int? width = null, int? height = null)
    {
        var path = Path(root);
        if (Io.ReadOrNull(path) is not { } text) return;

        if (oldSrc != newSrc)
            text = ReplaceFirst(text, $"src: \"{oldSrc}\"", $"src: \"{newSrc}\"");

        if (hasAlt)
            text = ReplaceAltNearSrc(text, newSrc, newAlt);

        if (!string.IsNullOrWhiteSpace(id))
            text = NormalizeEntryDimension(text, id, width, height);

        Io.Write(path, text);
    }

    private static string NormalizeEntryDimension(string text, string id, int? width = null, int? height = null)
    {
        var block = id switch
        {
            "banner" => TsBlockParser.FirstBlock(text, "export const BANNER"),
            "background" => TsBlockParser.FirstBlock(text, "export const BACKGROUND"),
            "logo" => TsBlockParser.FirstBlock(text, "export const LOGO"),
            var s when s.StartsWith("btn-") => ReadEntryBlock(text, "ACTION_BUTTONS", s["btn-".Length..]),
            var s when s.StartsWith("game-") => ReadArrayBlock(text, "GAME_CARDS", int.Parse(s["game-".Length..])),
            var s when s.StartsWith("promo-") => ReadArrayBlock(text, PromoArray, int.Parse(s["promo-".Length..])),
            var s when s.StartsWith("seo-") => ReadRecordBlock(text, "SEO_ARTICLE_IMAGES", $"seo-{s["seo-".Length..]}"),
            _ => null,
        };

        if (block is null) return text;

        var size = width.HasValue && height.HasValue
            ? (width.Value, height.Value)
            : ImageSizeCatalog.Size(id);

        return text.Replace(block, NormalizeSingleImageBlock(block, size));
    }

    private static string ReplaceFirst(string text, string search, string replacement)
    {
        var idx = text.IndexOf(search, StringComparison.Ordinal);
        return idx < 0 ? text : text[..idx] + replacement + text[(idx + search.Length)..];
    }

    private static string ReplaceAltNearSrc(string text, string src, string newAlt)
    {
        var token = $"src: \"{src}\"";
        var idx = text.IndexOf(token, StringComparison.Ordinal);
        if (idx < 0) return text;

        var blockStart = text.LastIndexOf('{', idx);
        var blockEnd   = text.IndexOf('}', idx + token.Length);
        if (blockStart < 0 || blockEnd < 0) return text;

        var block    = text[blockStart..(blockEnd + 1)];
        var newBlock = AltValuePattern.Replace(block,
            m => m.Groups[1].Value + newAlt + m.Groups[2].Value);

        return text[..blockStart] + newBlock + text[(blockEnd + 1)..];
    }
}
