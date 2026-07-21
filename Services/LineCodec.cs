namespace TEMO.AI;

internal static class LineCodec
{
    private static readonly Regex IdLine = new(
        @"id\s*=\s*([^\n""]+?)\s*""([\s\S]*?)""(?=\r?\n\s*id\s*=|\s*$)", RegexOptions.Compiled);

    private static readonly Regex CssVar = new(
        @"--([a-zA-Z0-9_-]+):\s*([^;]+);", RegexOptions.Compiled);

    public static IEnumerable<(string Id, string Value)> ParseContent(string text)
    {
        text = StripMarkdownFence(text);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match m in IdLine.Matches(text))
        {
            var id = FromExportId(m.Groups[1].Value.Trim());
            if (seen.Add(id))
                yield return (id, m.Groups[2].Value);
        }

        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;
            var m = Regex.Match(trimmed, @"^id\s*=\s*([^\s""]+)\s*""(.*)""\s*$");
            if (!m.Success) continue;
            var id = FromExportId(m.Groups[1].Value);
            if (seen.Add(id))
                yield return (id, m.Groups[2].Value);
        }
    }

    private static string StripMarkdownFence(string text)
    {
        text = text.Trim();
        if (!text.StartsWith("```", StringComparison.Ordinal)) return text;
        var start = text.IndexOf('\n');
        if (start < 0) return text;
        var end = text.LastIndexOf("```", StringComparison.Ordinal);
        return end <= start ? text[(start + 1)..].Trim() : text[(start + 1)..end].Trim();
    }

    public static IEnumerable<(string Name, string Value)> ParseCss(string text) =>
        CssVar.Matches(text).Select(m => (m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim()));

    public static string FormatContentLine(string fieldId, string value) =>
        $"id={ToExportId(fieldId)}\"{value}\"";

    public static int ApplyCss(string text, IReadOnlyDictionary<string, TextBox> boxes)
    {
        int applied = 0;
        foreach (var (name, value) in ParseCss(text))
            if (boxes.TryGetValue(name, out var box)) { box.Text = value; applied++; }
        return applied;
    }

    public static string ToExportId(string id)
    {
        if (id == "brand") return "brand-seo";
        if (id == "promo-comp-h") return "promo-comp";
        if (id == "promo-comp-d") return "sub-promo-comp";
        var legacyH = Regex.Match(id, @"^seo-(\d+)-h$");
        if (legacyH.Success) return $"seo-{legacyH.Groups[1].Value}";
        var legacyD = Regex.Match(id, @"^seo-(\d+)-d$");
        if (legacyD.Success) return $"sub-seo-{legacyD.Groups[1].Value}";
        return id;
    }

    public static string FromExportId(string exportId)
    {
        if (exportId == "brand-seo") return "brand";
        if (exportId == "promo-comp") return "promo-comp-h";
        if (exportId == "sub-promo-comp") return "promo-comp-d";
        return exportId;
    }
}
