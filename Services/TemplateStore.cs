namespace TEMO.AI;

internal static class TemplateStore
{
    private const string TemplatesZipUrl = "https://github.com/Jareansuk14/Templates-TEMO.AI/archive/refs/heads/main.zip";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(2),
    };

    private static readonly HashSet<string> ExcludedDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules", "dist", ".astro", ".git", ".github", ".vscode",
        ".cache", ".vercel", ".netlify", ".next", ".turbo", ".output", ".idea",
    };

    private const string LocalMarker = ".offline";

    private static readonly HashSet<string> ExcludedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ".DS_Store", "Thumbs.db", LocalMarker,
        ProjectPaths.CompleteMarker, ProjectPaths.NewMarker,
    };

    public static bool IsLocal(string templatePath) =>
        File.Exists(Path.Combine(templatePath, LocalMarker));

    public static void Delete(string templatePath)
    {
        if (IsWorkspace(templatePath))
            throw new InvalidOperationException("ลบ Template ในโฟลเดอร์ Templates ของโปรเจคไม่ได้");
        Io.DeleteDirectory(templatePath, ignoreErrors: false);
    }

    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Templates");

    private static readonly Lazy<string?> LocalRootLazy = new(ResolveLocalRoot);

    public static string? LocalRoot => LocalRootLazy.Value;

    private static string? ResolveLocalRoot() =>
        Workspace.FindAncestorDir(Path.Combine("Templates", ComponentDirName)) is { } componentDir
            ? Path.GetDirectoryName(componentDir)
            : null;

    private static bool IsWorkspace(string templatePath) =>
        LocalRoot is { } local &&
        Path.GetFullPath(templatePath).StartsWith(Path.GetFullPath(local), StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> EnumerateRoots()
    {
        if (LocalRoot is { } local) yield return local;
        yield return Root;
    }

    public static List<string> List()
    {
        var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in EnumerateRoots())
        {
            if (!Directory.Exists(root)) continue;
            foreach (var dir in Directory.GetDirectories(root))
            {
                var name = Path.GetFileName(dir);
                if (ExcludedDirs.Contains(name)
                    || string.Equals(name, ComponentDirName, StringComparison.OrdinalIgnoreCase))
                    continue;
                byName.TryAdd(name, dir);
            }
        }
        return byName.Values.OrderBy(Path.GetFileName).ToList();
    }

    public static async Task UpdateFromRemoteAsync(IProgress<string>? progress = null)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"temo-templates-{Guid.NewGuid():N}");
        var zipPath = Path.Combine(tempRoot, "templates.zip");
        var extractRoot = Path.Combine(tempRoot, "extract");

        try
        {
            progress?.Report("กำลังเตรียมพื้นที่สำหรับดาวน์โหลด...");
            Directory.CreateDirectory(tempRoot);
            Directory.CreateDirectory(extractRoot);

            progress?.Report("กำลังดาวน์โหลด Templates ใหม่...");
            using var request = new HttpRequestMessage(HttpMethod.Get, TemplatesZipUrl);
            request.Headers.UserAgent.ParseAdd("TEMO.AI");

            using (var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using var remote = await response.Content.ReadAsStreamAsync();
                await using var local = File.Create(zipPath);
                await remote.CopyToAsync(local);
            }

            await Task.Run(() => ReplaceTemplatesFromZip(zipPath, extractRoot, progress));
        }
        finally
        {
            Io.DeleteDirectory(tempRoot);
        }
    }

    private const string ComponentDirName = "Component";

    private static void ReplaceTemplatesFromZip(string zipPath, string extractRoot, IProgress<string>? progress)
    {
        progress?.Report("กำลังแตกไฟล์ Templates...");
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractRoot);

        var repositoryRoot = Directory.GetDirectories(extractRoot).SingleOrDefault()
            ?? throw new InvalidOperationException("ไม่พบไฟล์ Template ในไฟล์ที่ดาวน์โหลด");

        var allDirs = Directory.GetDirectories(repositoryRoot)
            .Where(dir => !ExcludedDirs.Contains(Path.GetFileName(dir)))
            .ToList();

        var componentDir = allDirs.FirstOrDefault(dir =>
            string.Equals(Path.GetFileName(dir), ComponentDirName, StringComparison.OrdinalIgnoreCase));

        var templateDirs = allDirs
            .Where(dir => dir != componentDir)
            .OrderBy(Path.GetFileName)
            .ToList();

        if (componentDir is null && templateDirs.Count == 0)
            throw new InvalidOperationException("ไม่พบโฟลเดอร์ Template ใน repository");

        if (componentDir is not null)
        {
            progress?.Report("กำลังอัปเดต Component...");
            Io.DeleteDirectory(ComponentStore.Root, ignoreErrors: false);
            Copy(componentDir, ComponentStore.Root);
        }

        if (templateDirs.Count > 0)
        {
            progress?.Report("กำลังลบ Templates...");
            RemoveRemoteTemplates();

            foreach (var dir in templateDirs)
            {
                var name = Path.GetFileName(dir);
                var dest = Path.Combine(Root, name);

                if (IsLocal(dest))
                {
                    progress?.Report($"ข้าม Template ออฟไลน์: {name}");
                    continue;
                }

                progress?.Report($"กำลังติดตั้ง Template: {name}");
                Copy(dir, dest);
            }
        }
    }

    private static readonly string[] TemplateDirs = ["src", "public", "Font"];

    public static void SaveAsTemplate(string projectPath, string destDir)
    {
        if (Directory.Exists(destDir)) Io.DeleteDirectory(destDir);
        Directory.CreateDirectory(destDir);

        foreach (var name in TemplateDirs)
        {
            var dir = Path.Combine(projectPath, name);
            if (Directory.Exists(dir)) Copy(dir, Path.Combine(destDir, name));
        }

        foreach (var file in Directory.GetFiles(projectPath))
        {
            var name = Path.GetFileName(file);
            if (ExcludedFiles.Contains(name)) continue;
            File.Copy(file, Path.Combine(destDir, name), overwrite: true);
        }

        File.WriteAllText(Path.Combine(destDir, LocalMarker), "");
    }

    public static void Copy(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var name = Path.GetFileName(dir);
            if (ExcludedDirs.Contains(name)) continue;
            Copy(dir, Path.Combine(destDir, name));
        }

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var name = Path.GetFileName(file);
            if (ExcludedFiles.Contains(name)) continue;
            File.Copy(file, Path.Combine(destDir, name), overwrite: true);
        }
    }

    private static void RemoveRemoteTemplates()
    {
        if (!Directory.Exists(Root))
        {
            Directory.CreateDirectory(Root);
            return;
        }

        foreach (var dir in Directory.GetDirectories(Root))
        {
            if (IsLocal(dir)) continue;
            Io.DeleteDirectory(dir, ignoreErrors: false);
        }
    }
}
