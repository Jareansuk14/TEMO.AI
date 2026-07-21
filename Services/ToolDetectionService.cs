using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace TEMO.AI;

public static class ToolDetectionService
{
    private const string CleanupScriptName = "TEMO_AI_Cleanup.bat";
    private const string TempFolderPattern = "TEMO.AI*";

    private static readonly Dictionary<string, string[]> FolderKeywords = new()
    {
        { "dnSpy",         new[] { "dnspy", "dnspyex" } },
        { "ILSpy",         new[] { "ilspy" } },
        { "dotPeek",       new[] { "dotpeek" } },
        { "JustDecompile", new[] { "justdecompile" } },
        { "Reflector",     new[] { "reflector", "netreflector" } },
        { "de4dot",        new[] { "de4dot" } },
        { "ConfuserEx",    new[] { "confuserex" } },
        { "Eazfuscator",   new[] { "eazfuscator" } },
        { "SmartAssembly", new[] { "smartassembly" } },
        { "Reflexil",      new[] { "reflexil" } },
        { "ILDASM",        new[] { "ildasm" } }
    };

    private static readonly HashSet<string> ProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "dnspy", "dnspy-x86", "dnspy-x64", "dnspyex",
        "ilspy", "ilspycmd",
        "dotpeek", "dotpeek64",
        "justdecompile",
        "reflector",
        "de4dot", "de4dot-x64",
        "confuserex", "confuser.cli",
        "ildasm",
        "x64dbg", "x32dbg", "x96dbg",
        "cheatengine", "cheatengine-x86_64",
        "processhacker",
        "pestudio",
        "die"
    };

    private static readonly TimeSpan MonitorInterval = TimeSpan.FromMinutes(1);

    private static System.Threading.Timer? _monitorTimer;
    private static int _terminating;

    public static void CheckAndExitIfSuspiciousToolsFound()
    {
        if (Volatile.Read(ref _terminating) != 0)
            return;

        if (!IsSuspiciousEnvironment())
            return;

        Terminate();
    }

    public static void StartBackgroundMonitoring()
    {
        if (_monitorTimer != null)
            return;

        _monitorTimer = new System.Threading.Timer(
            _ => CheckAndExitIfSuspiciousToolsFound(),
            null,
            MonitorInterval,
            MonitorInterval);
    }

    public static void StopBackgroundMonitoring()
    {
        _monitorTimer?.Dispose();
        _monitorTimer = null;
    }

    private static bool IsSuspiciousEnvironment()
    {
        if (ScanProcesses().Count > 0)
            return true;

        return ScanInstallLocations().Any(kvp => kvp.Value.Count > 0);
    }

    private static void Terminate()
    {
        if (Interlocked.CompareExchange(ref _terminating, 1, 0) != 0)
            return;

        StopBackgroundMonitoring();

        try
        {
            MessageBox.Show("Error 404", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch { }

        RunDeleteScript();

        try
        {
            Application.Current?.Shutdown();
        }
        catch { }

        Environment.Exit(0);
    }

    private static void RunDeleteScript()
    {
        try
        {
            var tempNetPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Temp", ".net");

            var exeName = Path.GetFileName(Environment.ProcessPath ?? string.Empty);
            var scriptPath = Path.Combine(Path.GetTempPath(), CleanupScriptName);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine($"set \"TEMP_NET={tempNetPath}\"");
            sb.AppendLine($"set \"EXE_NAME={exeName}\"");
            sb.AppendLine(":waitloop");
            sb.AppendLine("tasklist /FI \"IMAGENAME eq %EXE_NAME%\" 2>nul | find /I \"%EXE_NAME%\" >nul");
            sb.AppendLine("if %ERRORLEVEL%==0 (");
            sb.AppendLine("    timeout /T 1 /NOBREAK >nul");
            sb.AppendLine("    goto waitloop");
            sb.AppendLine(")");
            sb.AppendLine("if not exist \"%TEMP_NET%\" goto done");
            sb.AppendLine($"for /D %%F in (\"%TEMP_NET%\\{TempFolderPattern}\") do (");
            sb.AppendLine("    rmdir /S /Q \"%%F\"");
            sb.AppendLine(")");
            sb.AppendLine(":done");

            File.WriteAllText(scriptPath, sb.ToString());

            Process.Start(new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch { }
    }

    private static List<string> ScanProcesses()
    {
        var found = new List<string>();

        Process[] processes;
        try
        {
            processes = Process.GetProcesses();
        }
        catch
        {
            return found;
        }

        foreach (var process in processes)
        {
            try
            {
                if (ProcessNames.Contains(process.ProcessName))
                    found.Add(process.ProcessName);
            }
            catch { }
            finally
            {
                process.Dispose();
            }
        }

        return found;
    }

    private static Dictionary<string, List<string>> ScanInstallLocations()
    {
        var results = FolderKeywords.Keys.ToDictionary(k => k, _ => new List<string>());

        foreach (var basePath in GetScanPaths())
        {
            ScanDirectoryEntries(basePath, results, scanFiles: ShouldScanExecutables(basePath));
        }

        return results;
    }

    private static IEnumerable<string> GetScanPaths()
    {
        var paths = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
        };

        return paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(Directory.Exists);
    }

    private static bool ShouldScanExecutables(string basePath)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var downloads = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        return basePath.Equals(desktop, StringComparison.OrdinalIgnoreCase)
            || basePath.Equals(downloads, StringComparison.OrdinalIgnoreCase);
    }

    private static void ScanDirectoryEntries(
        string basePath,
        Dictionary<string, List<string>> results,
        bool scanFiles)
    {
        string[] folders;
        try
        {
            folders = Directory.GetDirectories(basePath);
        }
        catch
        {
            return;
        }

        foreach (var folder in folders)
        {
            var folderName = Path.GetFileName(folder) ?? string.Empty;
            foreach (var kvp in FolderKeywords)
            {
                if (kvp.Value.Any(keyword => MatchesKeyword(folderName, keyword)))
                    results[kvp.Key].Add(folder);
            }
        }

        if (!scanFiles)
            return;

        string[] files;
        try
        {
            files = Directory.GetFiles(basePath, "*.exe");
        }
        catch
        {
            return;
        }

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file) ?? string.Empty;
            foreach (var kvp in FolderKeywords)
            {
                if (kvp.Value.Any(keyword => MatchesKeyword(fileName, keyword)))
                    results[kvp.Key].Add(file);
            }
        }
    }

    private static bool MatchesKeyword(string value, string keyword)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(keyword))
            return false;

        var lower = value.ToLowerInvariant();
        keyword = keyword.ToLowerInvariant();

        if (lower == keyword)
            return true;

        if (lower.StartsWith(keyword + "-", StringComparison.Ordinal)
            || lower.StartsWith(keyword + "_", StringComparison.Ordinal)
            || lower.StartsWith(keyword + ".", StringComparison.Ordinal))
            return true;

        if (lower.EndsWith("-" + keyword, StringComparison.Ordinal)
            || lower.EndsWith("_" + keyword, StringComparison.Ordinal))
            return true;

        return lower
            .Split(new[] { '-', '_', '.', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Contains(keyword, StringComparer.Ordinal);
    }
}
