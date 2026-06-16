namespace TEMO.AI;

internal static class ImagesStore
{
    public const string Rel = @"lib\images.ts";

    public const string PromoArray = "PROMOS";
    public const int MinPromos = 1;
    public const int MaxPromos = 6;

    private static string Path(string root) => ProjectPaths.Src(root, Rel);

    public static string ReadConfig(string root) => Io.ReadOrNull(Path(root)) ?? "";

    private static string SeoFile(string root) =>
        ProjectPaths.Src(root, @"components\sections\Seo.astro");

    public static IReadOnlyList<int> SeoImageNumbers(string root)
    {
        if (Io.ReadOrNull(SeoFile(root)) is not { } content) return [];
        if (!content.Contains("SEO_ARTICLE_IMAGES")) return [];
        return Regex.Matches(content, @"id:\s*""seo-(\d+)""")
            .Select(m => int.Parse(m.Groups[1].Value))
            .Distinct().OrderBy(n => n).ToList();
    }

    private static string GameFile(string root) =>
        ProjectPaths.Src(root, @"components\sections\Game.astro");

    public static void SyncGameImages(string root, Action<string> deleteFile)
    {
        if (Io.ReadOrNull(GameFile(root)) is not { } game) return;
        var slot = Regex.Match(game, @"GAME_CARDS\.slice\(\s*0\s*,\s*(\d+)\s*\)");
        if (!slot.Success) return;
        int target = int.Parse(slot.Groups[1].Value);

        var path = Path(root);
        if (Io.ReadOrNull(path) is not { } text) return;
        var m = TsBlockParser.ArrayMatch(text, "GAME_CARDS");
        if (!m.Success) return;

        var blocks = TsBlockParser.AllBlocks(m.Groups[2].Value);
        if (blocks.Count == target) return;

        var removed = new List<string>();
        if (blocks.Count > target)
        {
            for (int i = target; i < blocks.Count; i++)
                removed.Add(TsBlockParser.QuotedVal(blocks[i], "src"));
            blocks = blocks.Take(target).ToList();
        }
        else
        {
            for (int i = blocks.Count; i < target; i++)
                blocks.Add($"{{ alt: \"\", src: \"/images/game{i + 1}.webp\", width: 1200, height: 801 }}");
        }

        WriteArrayBody(path, text, m, blocks);
        DeleteAll(removed, deleteFile);
    }

    public static void SyncSeoImages(string root, Action<string> deleteFile)
    {
        if (Io.ReadOrNull(SeoFile(root)) is not { } seo) return;
        int target = seo.Contains("SEO_ARTICLE_IMAGES")
            ? Regex.Matches(seo, @"id:\s*""seo-(\d+)""").Select(x => int.Parse(x.Groups[1].Value)).Distinct().Count()
            : 0;

        var path = Path(root);
        if (Io.ReadOrNull(path) is not { } text) return;

        var m = Regex.Match(text, @"(export\s+const\s+SEO_ARTICLE_IMAGES\b[^=]*=\s*\{)([\s\S]*?)(\n\};)");
        if (!m.Success) return;
        var body = m.Groups[2].Value;

        var existing = new Dictionary<int, string>();
        foreach (Match km in Regex.Matches(body, @"""seo-(\d+)""\s*:"))
            if (TsBlockParser.FirstBlock(body[km.Index..], $"\"seo-{km.Groups[1].Value}\":") is { } block)
                existing[int.Parse(km.Groups[1].Value)] = block;

        var removed = existing.Where(kv => kv.Key > target)
            .Select(kv => TsBlockParser.QuotedVal(kv.Value, "src")).ToList();

        var sb = new StringBuilder("\n");
        for (int k = 1; k <= target; k++)
        {
            var block = existing.TryGetValue(k, out var b)
                ? Regex.Replace(b, @"\s+", " ").Trim()
                : $"{{ src: \"/images/seo{k}.webp\", alt: \"\", width: 1200, height: 556 }}";
            sb.Append($"  \"seo-{k}\": {block},\n");
        }

        var updated = text[..m.Groups[2].Index] + sb + text[(m.Groups[2].Index + m.Groups[2].Length)..];
        if (updated != text) Io.Write(path, updated);
        DeleteAll(removed, deleteFile);
    }

    private static void WriteArrayBody(string path, string text, Match arrayMatch, List<string> blocks)
    {
        var rebuilt = "\n" + string.Join("\n",
            blocks.Select(b => "  " + Regex.Replace(b, @"\s+", " ").Trim() + ",")) + "\n";
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
            var keys = Regex.Matches(btnBlock, @"(\w+)\s*:\s*\{").Select(m => m.Groups[1].Value).ToList();
            for (int i = 0; i < keys.Count; i++)
                defs.Add(($"btn-{keys[i]}", $"ปุ่ม {i + 1}", "Logo & ปุ่ม", true));
        }

        if (includePlayButton)
            defs.Add(("play", "Play Button", "Logo & ปุ่ม", false));

        for (int i = 0; i < TsBlockParser.CountArray(content, "GAME_CARDS"); i++)
            defs.Add(($"game-{i}", $"เกม{i + 1}", "เกม", true));

        if (content.Contains("export const PROVIDER"))
            defs.Add(("provider", "ค่ายเกม", "ค่ายเกม", true));

        for (int i = 0; i < TsBlockParser.CountArray(content, PromoArray); i++)
            defs.Add(($"promo-{i}", $"โปร {i + 1}", "โปรโมชั่น", true));

        for (int i = 0; i < TsBlockParser.CountArray(content, "REVIEWS"); i++)
            defs.Add(($"review-{i}", $"รีวิว {i + 1}", "รีวิว", true));

        foreach (var n in seoNumbers)
            defs.Add(($"seo-{n}", $"บทความ {n}", "บทความ SEO", true));

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
        "provider"   => ReadNamedConst(content, "PROVIDER"),
        "play"       => ReadNamedConst(content, "PLAY_BUTTON"),
        "line-qr"    => ReadNamedConst(content, "LINE_QR"),
        var s when s.StartsWith("btn-")    => ReadNestedKey(content, "ACTION_BUTTONS", s["btn-".Length..]),
        var s when s.StartsWith("game-")   => ReadArrayAt(content, "GAME_CARDS", int.Parse(s["game-".Length..])),
        var s when s.StartsWith("promo-")  => ReadArrayAt(content, PromoArray, int.Parse(s["promo-".Length..])),
        var s when s.StartsWith("review-") => ReadArrayAt(content, "REVIEWS", int.Parse(s["review-".Length..])),
        var s when s.StartsWith("seo-")    => ReadRecordAt(content, "SEO_ARTICLE_IMAGES", $"seo-{s["seo-".Length..]}"),
        _ => ("", ""),
    };

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

    public static int PromoCount(string content) => TsBlockParser.CountArray(content, PromoArray);

    public static bool RewritePromos(string root, Func<List<string>, bool> transform)
    {
        var path = Path(root);
        if (Io.ReadOrNull(path) is not { } text) return false;

        var m = TsBlockParser.ArrayMatch(text, PromoArray);
        if (!m.Success) return false;

        var blocks = TsBlockParser.AllBlocks(m.Groups[2].Value);
        if (!transform(blocks)) return false;

        var rebuilt = "\n" + string.Join("\n",
            blocks.Select(b => "  " + Regex.Replace(b, @"\s+", " ").Trim() + ",")) + "\n";

        var updated = text[..m.Groups[2].Index] + rebuilt + text[(m.Groups[2].Index + m.Groups[2].Length)..];
        Io.Write(path, updated);
        return true;
    }

    public static int NextPromoNumber(IEnumerable<string> blocks)
    {
        int max = 0;
        foreach (var b in blocks)
        {
            var mm = Regex.Match(TsBlockParser.QuotedVal(b, "src"), @"promo(\d+)");
            if (mm.Success && int.TryParse(mm.Groups[1].Value, out var n) && n > max) max = n;
        }
        return max + 1;
    }

    public static void SaveEntry(string root, string oldSrc, string newSrc, string newAlt, bool hasAlt)
    {
        var path = Path(root);
        if (Io.ReadOrNull(path) is not { } text) return;

        if (oldSrc != newSrc)
            text = text.Replace($"src: \"{oldSrc}\"", $"src: \"{newSrc}\"");

        if (hasAlt)
            text = ReplaceAltNearSrc(text, newSrc, newAlt);

        Io.Write(path, text);
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
        var newBlock = Regex.Replace(block, @"(\balt:\s*"")[^""]*("")",
            m => m.Groups[1].Value + newAlt + m.Groups[2].Value);

        return text[..blockStart] + newBlock + text[(blockEnd + 1)..];
    }
}
