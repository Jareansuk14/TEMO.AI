using Microsoft.Win32;

namespace TEMO.AI;

internal static class NodeRuntime
{
    public const string DownloadUrl = "https://nodejs.org/en/download";
    private const string InstallerScript = "install-node.bat";

    private static bool _available;

    public static bool IsAvailable()
    {
        if (_available) return true;
        _available = Probe();
        return _available;
    }

    private static bool Probe()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c node -v",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (proc is null) return false;
            if (!proc.WaitForExit(5000))
            {
                try { proc.Kill(entireProcessTree: true); } catch { }
                return false;
            }
            return proc.ExitCode == 0;
        }
        catch { return false; }
    }

    public static bool IsInstalledOnDisk() => NodeExePaths().Any(File.Exists);

    public static void OpenDownloadPage()
    {
        try { Process.Start(new ProcessStartInfo(DownloadUrl) { UseShellExecute = true }); }
        catch { }
    }

    public static string? FindInstaller()
    {
        foreach (var dir in BaseDirs())
        {
            if (string.IsNullOrEmpty(dir)) continue;
            var path = Path.Combine(dir, InstallerScript);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    public static void RunInstaller(string script)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{script}\"",
                WorkingDirectory = Path.GetDirectoryName(script)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (proc is not null)
            {
                proc.OutputDataReceived += static (_, _) => { };
                proc.ErrorDataReceived += static (_, _) => { };
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit(300000);
            }
        }
        catch { }

        RefreshPath();
        EnsureNodeOnPath();
        _available = false;
    }

    private static void EnsureNodeOnPath()
    {
        if (NodeExePaths().FirstOrDefault(File.Exists) is not { } nodeExe) return;
        if (Path.GetDirectoryName(nodeExe) is not { } dir || string.IsNullOrEmpty(dir)) return;

        var current = Environment.GetEnvironmentVariable("PATH") ?? "";
        var already = current.Split(';')
            .Any(p => string.Equals(p.Trim().TrimEnd('\\'), dir.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
        if (!already)
            Environment.SetEnvironmentVariable("PATH", dir + ";" + current);
    }

    private static IEnumerable<string> NodeExePaths()
    {
        foreach (var key in new[] { "ProgramFiles", "ProgramW6432", "ProgramFiles(x86)" })
        {
            var root = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(root))
                yield return Path.Combine(root, "nodejs", "node.exe");
        }
    }

    private static void RefreshPath()
    {
        try
        {
            var machine = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment",
                "Path", "") as string ?? "";
            var user = Registry.GetValue(@"HKEY_CURRENT_USER\Environment", "Path", "") as string ?? "";

            var merged = string.Join(';', new[] { machine, user }.Where(p => !string.IsNullOrWhiteSpace(p)));
            if (!string.IsNullOrWhiteSpace(merged))
                Environment.SetEnvironmentVariable("PATH", merged);
        }
        catch { }
    }

    private static IEnumerable<string?> BaseDirs()
    {
        yield return Path.GetDirectoryName(Environment.ProcessPath);
        yield return AppContext.BaseDirectory;
    }
}
