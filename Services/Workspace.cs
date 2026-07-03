namespace TEMO.AI;

internal static class Workspace
{
    public static readonly bool DevLayoutMode = true;

    private static readonly Lazy<string?> WorkspaceTemplatesLazy = new(() => FindAncestorDir("Templates"));
    private static readonly Lazy<string?> WorkspaceComponentLazy = new(() => FindAncestorDir(Path.Combine("Templates", "Component")));

    public static string? WorkspaceTemplatesDir => WorkspaceTemplatesLazy.Value;
    public static string? WorkspaceComponentDir => WorkspaceComponentLazy.Value;

    public static string? FindAncestorDir(string relativePath)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (Directory.Exists(candidate)) return candidate;
        }
        return null;
    }
}
