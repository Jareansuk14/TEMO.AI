namespace TEMO.AI;

internal static class TemplateStore
{
    private const string TemplatesZipUrl = "https://github.com/Jareansuk14/Templates-TEMO.AI/archive/refs/heads/main.zip";
    private const string VersionFileUrl = "https://raw.githubusercontent.com/Jareansuk14/Templates-TEMO.AI/main/version.txt";
    private const string VersionFileName = "version.txt";

    public static string LocalVersionPath => Path.Combine(Root, VersionFileName);

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
        Workspace.WorkspaceTemplatesDir;

    private static bool IsWorkspace(string templatePath) =>
        LocalRoot is { } local &&
        Path.GetFullPath(templatePath).StartsWith(Path.GetFullPath(local), StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> EnumerateRoots()
    {
        if (Workspace.DevLayoutMode)
        {
            if (LocalRoot is { } local) yield return local;
            yield break;
        }
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

    public static async Task EnsureLatestAsync(IProgress<string>? progress = null)
    {
        if (Workspace.DevLayoutMode) return;

        var localVersion = ReadLocalVersion();
        string? remoteVersion = null;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, VersionFileUrl);
            req.Headers.UserAgent.ParseAdd("TEMO.AI");
            using var resp = await Http.SendAsync(req);
            if (resp.IsSuccessStatusCode)
                remoteVersion = (await resp.Content.ReadAsStringAsync()).Trim();
        }
        catch { }

        if (string.IsNullOrWhiteSpace(remoteVersion))
        {
            if (localVersion is null)
                progress?.Report("ไม่สามารถดึงเวอร์ชัน Templates ได้ — กรุณากด Update Template ภายหลัง");
            return;
        }

        if (localVersion is not null && Version.TryParse(localVersion, out var localV)
            && Version.TryParse(remoteVersion, out var remoteV) && localV >= remoteV)
        {
            progress?.Report($"Templates เป็นเวอร์ชันล่าสุดแล้ว ({localVersion})");
            return;
        }

        progress?.Report($"กำลังอัปเดต Templates {localVersion ?? "(ยังไม่มี)"} → {remoteVersion}...");
        await UpdateFromRemoteAsync(progress);
    }

    private static string? ReadLocalVersion()
    {
        try { return File.Exists(LocalVersionPath) ? File.ReadAllText(LocalVersionPath).Trim() : null; }
        catch { return null; }
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

        var providerDir = allDirs.FirstOrDefault(dir =>
            string.Equals(Path.GetFileName(dir), "Provider", StringComparison.OrdinalIgnoreCase));
        if (providerDir is null)
        {
            var ex1Provider = Path.Combine(repositoryRoot, "EX.1", "public", "Provider");
            if (Directory.Exists(ex1Provider)) providerDir = ex1Provider;
        }

        var templateDirs = allDirs
            .Where(dir => dir != componentDir && dir != providerDir)
            .OrderBy(Path.GetFileName)
            .ToList();

        if (componentDir is null && templateDirs.Count == 0 && providerDir is null)
            throw new InvalidOperationException("ไม่พบโฟลเดอร์ Template ใน repository");

        if (componentDir is not null)
        {
            progress?.Report("กำลังอัปเดต Component...");
            Io.DeleteDirectory(ComponentStore.Root, ignoreErrors: false);
            Copy(componentDir, ComponentStore.Root);
        }

        if (providerDir is not null)
        {
            progress?.Report("กำลังอัปเดต Provider...");
            Io.DeleteDirectory(ProviderStore.Root, ignoreErrors: true);
            Copy(providerDir, ProviderStore.Root);
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

        var versionSrc = Path.Combine(repositoryRoot, VersionFileName);
        if (File.Exists(versionSrc))
        {
            Directory.CreateDirectory(Root);
            File.Copy(versionSrc, LocalVersionPath, overwrite: true);
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
