namespace TEMO.AI;

internal static class LayoutStore
{
    public const string IndexRel = @"pages\index.astro";

    private const string GeneratedCssImport = "import \"@/styles/sections.generated.css\";";

    private static readonly Regex BannerTag = new(@"[ \t]*<Banner(\s[^/]*)?\s*/?>[ \t]*", RegexOptions.Compiled);
    private static readonly Regex ComponentTag = new(@"<(\w+)(\s+[^>]*)?\s*/?>", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex SectionImport = new(
        @"import\s+(\w+)\s+from\s+""@/components/sections/(\w+)\.astro""", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex BaseLayoutBlock = new(
        @"(<BaseLayout\b[^>]*>\s*)([\s\S]*?)(\s*</BaseLayout>)", RegexOptions.Compiled);
    private static readonly Regex SectionImportLine = new(
        @"^import\s+\w+\s+from\s+""@/components/sections/[^""]+"";\s*\r?\n", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex GeneratedCssLine = new(
        @"^import\s+""@/styles/sections\.generated\.css"";\s*\r?\n", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex AnyImportLine = new(@"^import\s+.+?;\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    public static string? ReadIndex(string root) => Io.ReadOrNull(ProjectPaths.Src(root, IndexRel));

    public static void WriteIndex(string root, string content) =>
        Io.Write(ProjectPaths.Src(root, IndexRel), content);

    public static string ParseBannerLine(string content)
    {
        var m = BannerTag.Match(content);
        return m.Success ? m.Value.TrimEnd() : "  <Banner />";
    }

    public static IEnumerable<string> ParseComponentNames(string content) =>
        ComponentTag.Matches(content).Select(m => m.Groups[1].Value);

    public static Dictionary<string, string> ParseSectionImportKinds(string content)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in SectionImport.Matches(content))
            map.TryAdd(m.Groups[1].Value, m.Groups[2].Value);
        return map;
    }

    public static string? BuildIndex(string content, string bannerLine, IReadOnlyList<LayoutComponent> components)
    {
        content = SyncImports(content, components);

        var match = BaseLayoutBlock.Match(content);
        if (!match.Success) return null;

        var sb = new StringBuilder();
        sb.Append(content[..match.Groups[1].Index]);
        sb.Append(match.Groups[1].Value);
        sb.AppendLine(bannerLine);
        foreach (var comp in components)
        {
            sb.Append("  <").Append(comp.Name);
            if (comp.HasExternalLink) sb.Append(" externalLink={EXTERNAL_LINK}");
            sb.AppendLine(" />");
        }
        sb.Append("</BaseLayout>");
        sb.Append(content[(match.Groups[3].Index + match.Groups[3].Length)..]);
        return sb.ToString();
    }

    private static string SyncImports(string content, IReadOnlyList<LayoutComponent> components)
    {
        content = SectionImportLine.Replace(content, "");
        content = GeneratedCssLine.Replace(content, "");

        var imports = components
            .Where(x => !string.IsNullOrWhiteSpace(x.ImportPath))
            .GroupBy(x => x.Name, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => $"import {x.Name} from \"{x.ImportPath}\";")
            .ToList();
        imports.Add(GeneratedCssImport);

        var importMatches = AnyImportLine.Matches(content);
        if (importMatches.Count == 0)
            return string.Join(Environment.NewLine, imports) + Environment.NewLine + content;

        var last = importMatches[^1];
        var insertAt = last.Index + last.Length;
        var prefix = content[..insertAt].TrimEnd();
        var suffix = content[insertAt..].TrimStart('\r', '\n');

        return prefix + Environment.NewLine + string.Join(Environment.NewLine, imports) + Environment.NewLine + suffix;
    }
}
