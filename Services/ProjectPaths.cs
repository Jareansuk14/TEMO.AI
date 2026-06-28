namespace TEMO.AI;

internal static class ProjectPaths
{
    public const string CompleteMarker = "complete";
    public const string NewMarker = "NEW";

    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Myprojects");

    public static List<string> List() =>
        Directory.Exists(Root)
            ? Directory.GetDirectories(Root)
                .Where(d => File.Exists(Path.Combine(d, "package.json"))
                    && File.Exists(Path.Combine(d, CompleteMarker)))
                .OrderByDescending(Directory.GetLastWriteTimeUtc)
                .ToList()
            : [];

    public static string Src(string root) => Path.Combine(root, "src");

    public static string Src(string root, string rel) => Path.Combine(root, "src", rel);

    public static string Public(string root, string rel) =>
        Path.Combine(root, "public", rel.TrimStart('/').Replace('/', '\\'));

    public static bool IsProject(string root) =>
        !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);

    public static void MarkComplete(string root)
    {
        try { File.WriteAllText(Path.Combine(root, CompleteMarker), ""); } catch { }
    }

    public static void MarkNew(string root)
    {
        try { File.WriteAllText(Path.Combine(root, NewMarker), ""); } catch { }
    }

    public static bool IsNew(string root) =>
        !string.IsNullOrWhiteSpace(root) && File.Exists(Path.Combine(root, NewMarker));

    public static void ClearNew(string root)
    {
        try
        {
            var path = Path.Combine(root, NewMarker);
            if (File.Exists(path)) File.Delete(path);
        }
        catch { }
    }

    public static bool HasAnyNew() =>
        Directory.Exists(Root) && Directory.GetDirectories(Root).Any(IsNew);

    public static void MigrateExisting()
    {
        if (!Directory.Exists(Root)) return;
        foreach (var d in Directory.GetDirectories(Root))
            if (File.Exists(Path.Combine(d, "package.json")) && !File.Exists(Path.Combine(d, CompleteMarker)))
                MarkComplete(d);
    }
}
