namespace TEMO.AI;

internal static class ComponentStore
{
    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Component");

    private static string? LocalRoot => Workspace.WorkspaceComponentDir;

    private static List<string> Roots() =>
        Workspace.DevLayoutMode
            ? (LocalRoot is { } l ? [l] : [])
            : [Root];

    public static IReadOnlyList<SectionDefinition> List()
    {
        EnsureSeeded();

        var roots = Roots();
        var collected = new List<(SectionDefinition Def, int Priority, string Root)>();

        for (var i = 0; i < roots.Count; i++)
        {
            var root = roots[i];
            if (!Directory.Exists(root)) continue;

            foreach (var def in Directory.GetFiles(root, "manifest.json", SearchOption.AllDirectories)
                .Select(Path.GetDirectoryName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(x => ReadDefinition(x!))
                .Where(x => x is not null)
                .Cast<SectionDefinition>())
            {
                collected.Add((def, i, root));
            }
        }

        return collected
            .GroupBy(x => x.Def.ComponentName, StringComparer.Ordinal)
            .Select(g => g
                .OrderBy(x => x.Priority)
                .ThenByDescending(x => IsNestedStorePath(x.Root, x.Def.StoreDirectory))
                .First().Def)
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

    private static (string astro, string? css) SlotDest(string projectPath, SectionDefinition d) =>
        ShellSlot.AstroPath(projectPath, d.Slot) is { } astro
            ? (astro, null)
            : (ProjectAstro(projectPath, d.Kind), ProjectCss(projectPath, d.Kind));

    public static SectionDefinition? CurrentForSlot(string projectPath, string slot)
    {
        if (ShellSlot.AstroPath(projectPath, slot) is not { } dest || !File.Exists(dest)) return null;

        string current;
        try { current = File.ReadAllText(dest); }
        catch { return null; }

        foreach (var def in SectionCatalog.All.Where(d => d.Slot == slot))
        {
            var srcFile = Path.Combine(def.StoreDirectory, def.AstroFile);
            try
            {
                if (File.Exists(srcFile) && File.ReadAllText(srcFile) == current) return def;
            }
            catch { }
        }
        return null;
    }

    public static void CopyToProjectIfMissing(string projectPath, SectionDefinition definition)
    {
        if (!IsProject(projectPath)) return;

        var now = DateTime.UtcNow;
        var (astro, css) = SlotDest(projectPath, definition);
        CopyIfMissing(Path.Combine(definition.StoreDirectory, definition.AstroFile), astro, now);
        if (css is not null)
            CopyIfMissing(Path.Combine(definition.StoreDirectory, definition.CssFile), css, now);
    }

    public static void CopyToProject(string projectPath, SectionDefinition definition)
    {
        if (!IsProject(projectPath)) return;

        var now = DateTime.UtcNow;
        var (astro, css) = SlotDest(projectPath, definition);
        CopyFile(Path.Combine(definition.StoreDirectory, definition.AstroFile), astro, now);
        if (css is not null)
            CopyFile(Path.Combine(definition.StoreDirectory, definition.CssFile), css, now);
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
            var manifest = JsonFile.Read<ComponentManifest>(manifestPath);
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
                manifest.HasExternalLink,
                string.IsNullOrWhiteSpace(manifest.Slot) ? "body" : manifest.Slot,
                manifest.Required,
                manifest.Weight,
                manifest.DataFile,
                manifest.DataConst,
                manifest.Repeatable,
                manifest.Fields,
                manifest.Images,
                new ContentSpec(
                    string.IsNullOrWhiteSpace(manifest.ImageRatio) ? "" : manifest.ImageRatio.Trim(),
                    string.IsNullOrWhiteSpace(manifest.ImageType) ? "" : manifest.ImageType.Trim().ToLowerInvariant(),
                    string.IsNullOrWhiteSpace(manifest.ImageGroup) ? "" : manifest.ImageGroup.Trim(),
                    manifest.ImageCount,
                    manifest.HeadingCount));
        }
        catch
        {
            return null;
        }
    }

    private static bool IsNestedStorePath(string root, string path)
    {
        var rel = Path.GetRelativePath(root, path);
        return rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length >= 2;
    }
}
