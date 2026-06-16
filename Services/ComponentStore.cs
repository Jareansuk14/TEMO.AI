namespace TEMO.AI;

internal static class ComponentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Component");

    public static IReadOnlyList<SectionDefinition> List()
    {
        EnsureSeeded();

        if (!Directory.Exists(Root)) return [];

        return Directory.GetFiles(Root, "manifest.json", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => ReadDefinition(x!))
            .Where(x => x is not null)
            .Cast<SectionDefinition>()
            .GroupBy(x => x.ComponentName, StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(x => IsNestedStorePath(x.StoreDirectory)).First())
            .OrderBy(x => x.Kind, StringComparer.Ordinal)
            .ThenBy(x => x.Variant, StringComparer.Ordinal)
            .ThenBy(x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
    }

    public static void EnsureSeeded() => Directory.CreateDirectory(Root);

    public static SectionDefinition? Find(string componentName) =>
        List().FirstOrDefault(x => x.ComponentName.Equals(componentName, StringComparison.Ordinal));

    private static string ProjectAstro(string projectPath, string kind) =>
        ProjectPaths.Src(projectPath, Path.Combine("components", "sections", $"{kind}.astro"));

    private static string ProjectCss(string projectPath, string kind) =>
        ProjectPaths.Src(projectPath, Path.Combine("styles", "sections", $"{kind}.css"));

    public static void CopyToProjectIfMissing(string projectPath, SectionDefinition definition)
    {
        if (!IsProject(projectPath)) return;

        var now = DateTime.UtcNow;
        CopyIfMissing(Path.Combine(definition.StoreDirectory, definition.AstroFile),
            ProjectAstro(projectPath, definition.Kind), now);
        CopyIfMissing(Path.Combine(definition.StoreDirectory, definition.CssFile),
            ProjectCss(projectPath, definition.Kind), now);
    }

    public static void CopyToProject(string projectPath, SectionDefinition definition)
    {
        if (!IsProject(projectPath)) return;

        var now = DateTime.UtcNow;
        CopyFile(Path.Combine(definition.StoreDirectory, definition.AstroFile),
            ProjectAstro(projectPath, definition.Kind), now);
        CopyFile(Path.Combine(definition.StoreDirectory, definition.CssFile),
            ProjectCss(projectPath, definition.Kind), now);
    }

    public static void RemoveFromProject(string projectPath, string kind)
    {
        if (!IsProject(projectPath)) return;

        DeleteIfExists(ProjectAstro(projectPath, kind));
        DeleteIfExists(ProjectCss(projectPath, kind));
    }

    private static bool IsProject(string projectPath) => ProjectPaths.IsProject(projectPath);

    private static void CopyIfMissing(string src, string dest, DateTime now)
    {
        if (!File.Exists(dest)) CopyFile(src, dest, now);
    }

    private static void CopyFile(string src, string dest, DateTime now)
    {
        if (!File.Exists(src)) return;

        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(src, dest, overwrite: true);
        try { File.SetLastWriteTimeUtc(dest, now); } catch { }
    }

    private static SectionDefinition? ReadDefinition(string dir)
    {
        var manifestPath = Path.Combine(dir, "manifest.json");
        if (!File.Exists(manifestPath)) return null;

        try
        {
            var manifest = JsonSerializer.Deserialize<ComponentManifest>(File.ReadAllText(manifestPath), JsonOptions);
            if (manifest is null || string.IsNullOrWhiteSpace(manifest.ComponentName)) return null;

            return new SectionDefinition(
                manifest.Kind,
                manifest.Variant,
                manifest.ComponentName,
                manifest.DisplayName,
                $"@/components/sections/{manifest.Kind}.astro",
                $"@/styles/sections/{manifest.Kind}.css",
                dir,
                string.IsNullOrWhiteSpace(manifest.AstroFile) ? $"{manifest.ComponentName}.astro" : manifest.AstroFile,
                string.IsNullOrWhiteSpace(manifest.CssFile) ? $"{manifest.ComponentName}.css" : manifest.CssFile,
                manifest.HasExternalLink);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsNestedStorePath(string path)
    {
        var rel = Path.GetRelativePath(Root, path);
        return rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length >= 2;
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
        }
    }
}
