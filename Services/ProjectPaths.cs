namespace TEMO.AI;

internal static class ProjectPaths
{
    public static string Src(string root) => Path.Combine(root, "src");

    public static string Src(string root, string rel) => Path.Combine(root, "src", rel);

    public static string Public(string root, string rel) =>
        Path.Combine(root, "public", rel.TrimStart('/').Replace('/', '\\'));

    public static bool IsProject(string root) =>
        !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);
}
