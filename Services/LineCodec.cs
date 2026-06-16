namespace TEMO.AI;

internal static class LineCodec
{
    private static readonly Regex IdLine = new(
        @"id\s*=\s*([^\n""]+?)\s*""([\s\S]*?)""(?=\r?\n\s*id\s*=|\s*$)", RegexOptions.Compiled);

    private static readonly Regex CssVar = new(
        @"--([a-zA-Z0-9_-]+):\s*([^;]+);", RegexOptions.Compiled);

    public static IEnumerable<(string Id, string Value)> ParseContent(string text) =>
        IdLine.Matches(text).Select(m => (FromExportId(m.Groups[1].Value.Trim()), m.Groups[2].Value));

    public static IEnumerable<(string Name, string Value)> ParseCss(string text) =>
        CssVar.Matches(text).Select(m => (m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim()));

    public static string FormatContentLine(string fieldId, string value) =>
        $"id={ToExportId(fieldId)}\"{value}\"";

    public static int ApplyContent(string text, IReadOnlyDictionary<string, TextBox> boxes)
    {
        int applied = 0;
        foreach (var (id, value) in ParseContent(text))
            if (boxes.TryGetValue(id, out var box)) { box.Text = value; applied++; }
        return applied;
    }

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
        var seoH = Regex.Match(id, @"^seo-(\d+)-h$");
        if (seoH.Success) return $"seo-{seoH.Groups[1].Value}";
        var seoD = Regex.Match(id, @"^seo-(\d+)-d$");
        if (seoD.Success) return $"sub-seo-{seoD.Groups[1].Value}";
        return id;
    }

    public static string FromExportId(string exportId)
    {
        if (exportId == "brand-seo") return "brand";
        if (exportId == "promo-comp") return "promo-comp-h";
        if (exportId == "sub-promo-comp") return "promo-comp-d";
        var seoH = Regex.Match(exportId, @"^seo-(\d+)$");
        if (seoH.Success) return $"seo-{seoH.Groups[1].Value}-h";
        var seoD = Regex.Match(exportId, @"^sub-seo-(\d+)$");
        if (seoD.Success) return $"seo-{seoD.Groups[1].Value}-d";
        return exportId;
    }
}
