using System.Text.RegularExpressions;

namespace TEMO.AI.Ai;

internal static class UnusedImageCleaner
{
    private static readonly Regex ImageRefPattern = new(@"[\""'(]?(/?images/[A-Za-z0-9_\-./]+)", RegexOptions.Compiled);
    private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
        { ".webp", ".png", ".jpg", ".jpeg", ".gif", ".avif", ".svg", ".bmp", ".ico" };
    private static readonly string[] SkipDirs = ["node_modules", ".git", "dist", ".understand-anything", "public"];

    public static int Run(string projectPath)
    {
        if (!Directory.Exists(projectPath)) return 0;

        using var session = Io.Session();
        var referenced = CollectReferenced(projectPath);
        var imagesDir = Path.Combine(projectPath, "public", "images");
        if (!Directory.Exists(imagesDir)) return 0;

        var removed = 0;
        foreach (var file in Directory.EnumerateFiles(imagesDir, "*", SearchOption.AllDirectories))
        {
            if (!ImageExts.Contains(Path.GetExtension(file))) continue;
            var rel = Path.GetRelativePath(projectPath + Path.DirectorySeparatorChar + "public", file)
                .Replace('\\', '/').TrimStart('/');
            if (referenced.Contains(rel)) continue;
            Io.DeleteFile(file);
            removed++;
        }
        return removed;
    }

    private static HashSet<string> CollectReferenced(string projectPath)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(projectPath, "*", SearchOption.AllDirectories)
                     .Where(f => ShouldScan(projectPath, f)))
        {
            var text = Io.ReadOrNull(file);
            if (string.IsNullOrEmpty(text)) continue;
            foreach (Match m in ImageRefPattern.Matches(text))
            {
                var path = m.Groups[1].Value.TrimStart('/').TrimEnd(')', '"', '\'');
                if (path.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
                    set.Add(path);
            }
        }
        return set;
    }

    private static bool ShouldScan(string projectPath, string file)
    {
        var rel = Path.GetRelativePath(projectPath, file);
        foreach (var skip in SkipDirs)
            if (rel.StartsWith(skip + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return false;
        return true;
    }
}
