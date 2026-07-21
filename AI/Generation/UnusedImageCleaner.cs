using System.Text.RegularExpressions;

namespace TEMO.AI.Ai;

internal static class UnusedImageCleaner
{
    private static readonly Regex ImageRefPattern = new(@"[\""'(]?(/?images/[A-Za-z0-9_\-./]+)", RegexOptions.Compiled);
    private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
        { ".webp", ".png", ".jpg", ".jpeg", ".gif", ".avif", ".svg", ".bmp", ".ico" };

    public static int Run(string projectPath)
    {
        if (!Directory.Exists(projectPath)) return 0;

        using var session = Io.Session();
        var imagesDir = Path.Combine(projectPath, "public", "images");
        if (!Directory.Exists(imagesDir)) return 0;

        var usageText = ImageUsage.CollectUsageText(projectPath);
        var literal = ExtractRefs(usageText);
        var imagesTs = Io.ReadOrNull(ProjectPaths.Src(projectPath, ImagesStore.Rel)) ?? "";

        var removed = 0;
        foreach (var file in Directory.EnumerateFiles(imagesDir, "*", SearchOption.AllDirectories))
        {
            if (!ImageExts.Contains(Path.GetExtension(file))) continue;
            var rel = Path.GetRelativePath(projectPath + Path.DirectorySeparatorChar + "public", file)
                .Replace('\\', '/').TrimStart('/');
            if (literal.Contains(rel) || UsedViaImagesTs(imagesTs, usageText, rel)) continue;
            Io.DeleteFile(file);
            removed++;
        }
        return removed;
    }

    private static bool UsedViaImagesTs(string imagesTs, string usageText, string rel)
    {
        var idx = imagesTs.IndexOf(rel, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;
        var declIdx = imagesTs.LastIndexOf("export const ", idx, StringComparison.Ordinal);
        if (declIdx < 0) return false;
        var start = declIdx + "export const ".Length;
        var end = start;
        while (end < imagesTs.Length && (char.IsLetterOrDigit(imagesTs[end]) || imagesTs[end] == '_')) end++;
        return ImageUsage.ConstNameUsed(usageText, imagesTs[start..end]);
    }

    private static HashSet<string> ExtractRefs(string text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in ImageRefPattern.Matches(text))
        {
            var path = m.Groups[1].Value.TrimStart('/').TrimEnd(')', '"', '\'');
            if (path.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
                set.Add(path);
        }
        return set;
    }
}
