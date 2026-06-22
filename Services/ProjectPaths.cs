namespace TEMO.AI;

internal static class ProjectPaths
{
    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Myprojects");

    public static List<string> List() =>
        Directory.Exists(Root)
            ? Directory.GetDirectories(Root)
                .Where(d => File.Exists(Path.Combine(d, "package.json")))
                .OrderByDescending(Directory.GetLastWriteTimeUtc)
                .ToList()
            : [];

    public static string Src(string root) => Path.Combine(root, "src");

    public static string Src(string root, string rel) => Path.Combine(root, "src", rel);

    public static string Public(string root, string rel) =>
        Path.Combine(root, "public", rel.TrimStart('/').Replace('/', '\\'));

    public static bool IsProject(string root) =>
        !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);
}
