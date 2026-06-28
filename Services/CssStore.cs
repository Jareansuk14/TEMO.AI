namespace TEMO.AI;

internal static class CssStore
{
    public const string ThemeRel = @"styles\theme.css";

    private static readonly Regex RootBlock = new(@":root\s*\{([\s\S]*?)\}", RegexOptions.Compiled | RegexOptions.Singleline);

    private static string Src(string root) => ProjectPaths.Src(root, ThemeRel);

    public static string Read(string root) => Io.ReadOrNull(Src(root)) ?? "";

    public static List<(string Name, string Value)>? ReadVariables(string root)
    {
        if (Io.ReadOrNull(Src(root)) is not { } content) return null;
        var rootMatch = RootBlock.Match(content);
        return rootMatch.Success ? LineCodec.ParseCss(rootMatch.Groups[1].Value).ToList() : [];
    }

    public static void Save(string root, IReadOnlyDictionary<string, string> values)
    {
        var path = Src(root);
        if (Io.ReadOrNull(path) is not { } content) return;

        var updated = content;
        foreach (var (name, value) in values)
            updated = Regex.Replace(updated,
                $@"(--{Regex.Escape(name)}:\s*)([^;]+)(;)",
                m => m.Groups[1].Value + value.Trim() + m.Groups[3].Value);

        if (updated != content) Io.Write(path, updated);
    }
}
