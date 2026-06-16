namespace TEMO.AI;

internal static class ToolDetectionService
{
    private static readonly Dictionary<string, string[]> Tools = new(StringComparer.OrdinalIgnoreCase)
    {
        { "dnSpy",         new[] { "dnspy", "dnspyex" } },
        { "ILSpy",         new[] { "ilspy" } },
        { "dotPeek",       new[] { "dotpeek" } },
        { "JustDecompile", new[] { "justdecompile", "telerik" } },
        { "Reflector",     new[] { "reflector", "redgate" } },
        { "de4dot",        new[] { "de4dot" } },
        { "ConfuserEx",    new[] { "confuser", "confuserex" } },
        { "Eazfuscator",   new[] { "eazfuscator" } },
        { "SmartAssembly", new[] { "smartassembly" } },
        { "Babel",         new[] { "babel" } },
        { "Reflexil",      new[] { "reflexil" } },
        { "ILDASM",        new[] { "ildasm" } },
    };

    public static void CheckAndExitIfSuspiciousToolsFound()
    {
        try
        {
            if (!ScanRootOnly().Any(kvp => kvp.Value.Count > 0))
                return;

            System.Windows.MessageBox.Show("Error 404", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            RunDeleteScript();
            System.Windows.Application.Current?.Shutdown();
            Environment.Exit(0);
        }
        catch { }
    }

    private static void RunDeleteScript()
    {
        try
        {
            var tempNetPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TEMO.AI", "System", "Runtime", "Cache");

            var exeName = Path.GetFileName(Environment.ProcessPath ?? string.Empty);
            var scriptPath = Path.Combine(Path.GetTempPath(), "TEMO_AI_Cleanup.bat");

            var sb = new StringBuilder();
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
            sb.AppendLine("for /D %%F in (\"%TEMP_NET%\\*\") do (");
            sb.AppendLine("    rmdir /S /Q \"%%F\"");
            sb.AppendLine(")");
            sb.AppendLine(":done");

            File.WriteAllText(scriptPath, sb.ToString());

            Process.Start(new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });
        }
        catch { }
    }

    private static Dictionary<string, List<string>> ScanRootOnly()
    {
        var paths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        }.Distinct().ToArray();

        var results = Tools.Keys.ToDictionary(k => k, _ => new List<string>());

        foreach (var basePath in paths)
        {
            if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
                continue;

            string[] folders;
            try { folders = Directory.GetDirectories(basePath); }
            catch { continue; }

            foreach (var folder in folders)
            {
                var folderLower = (Path.GetFileName(folder) ?? string.Empty).ToLowerInvariant();
                foreach (var kvp in Tools)
                    if (kvp.Value.Any(kw => folderLower.Contains(kw)))
                        results[kvp.Key].Add(folder);
            }
        }

        return results;
    }
}
