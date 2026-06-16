namespace TEMO.AI;

internal static partial class VercelCli
{
    public static async Task<bool> IsLoggedInAsync(string cwd)
    {
        var r = await RunAsync("teams ls --format=json --non-interactive", cwd);
        return r.Success;
    }

    public static void ClearProjectLink(string cwd)
    {
        var dir = Path.Combine(cwd, ".vercel");
        if (!Directory.Exists(dir)) return;
        try { Directory.Delete(dir, recursive: true); }
        catch { }
    }

    public static async Task<List<VercelScopeOption>> GetTeamsAsync(string cwd)
    {
        var r = await RunAsync("teams ls --format=json --non-interactive", cwd);
        if (!r.Success) return [];

        var json = ExtractJson(r.Output);
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            var teams = JsonNode.Parse(json)?["teams"]?.AsArray();
            if (teams is null) return [];

            return teams
                .Select(t => new VercelScopeOption(
                    t?["name"]?.GetValue<string>() ?? "",
                    t?["slug"]?.GetValue<string>() ?? "",
                    t?["current"]?.GetValue<bool>() ?? false))
                .Where(t => !string.IsNullOrWhiteSpace(t.Slug))
                .ToList();
        }
        catch { return []; }
    }

    public static async Task<List<VercelProjectOption>> GetProjectsAsync(string cwd, string scope)
    {
        var r = await RunAsync($"projects ls --format=json --scope {scope} --non-interactive", cwd);
        if (!r.Success) return [];

        var json = ExtractJson(r.Output);
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            var projects = JsonNode.Parse(json)?["projects"]?.AsArray();
            if (projects is null) return [];

            return projects
                .Select(p => new VercelProjectOption(
                    p?["name"]?.GetValue<string>() ?? "",
                    p?["id"]?.GetValue<string>() ?? "",
                    p?["latestProductionUrl"]?.GetValue<string>()))
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .ToList();
        }
        catch { return []; }
    }

    public static Task<VercelCliResult> CreateProjectAsync(string cwd, string scope, string name, Action<string>? onLog = null) =>
        RunAsync($"project add {name} --scope {scope} --non-interactive", cwd, onLog);

    public static Task<VercelCliResult> LinkAsync(string cwd, string scope, string project, Action<string>? onLog = null) =>
        RunAsync($"link --yes --project {project} --scope {scope} --non-interactive", cwd, onLog);

    public static Task<VercelCliResult> DeployAsync(string cwd, string scope, Action<string>? onLog, CancellationToken ct = default) =>
        RunAsync($"deploy --prod --yes --logs --scope {scope} --non-interactive", cwd, onLog, ct);

    public static Task<VercelCliResult> LogoutAsync(string cwd) =>
        RunAsync("logout --non-interactive", cwd);

    public static async Task StartLoginAsync(string cwd, Action<string>? onMessage, CancellationToken ct = default)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var openedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c npx --yes vercel login",
                WorkingDirectory = cwd,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            },
            EnableRaisingEvents = true,
        };

        void HandleLine(string? line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            onMessage?.Invoke(line);

            foreach (var url in ExtractUrls(line))
            {
                if (!url.Contains("vercel.com", StringComparison.OrdinalIgnoreCase)) continue;
                if (!openedUrls.Add(url)) continue;
                try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                catch { }
            }
        }

        process.OutputDataReceived += (_, e) => HandleLine(e.Data);
        process.ErrorDataReceived += (_, e) => HandleLine(e.Data);
        process.Exited += (_, _) =>
        {
            try { completion.TrySetResult(); }
            finally { process.Dispose(); }
        };

        using var reg = ct.CanBeCanceled
            ? ct.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch { }
            })
            : default;

        try
        {
            if (!process.Start()) { process.Dispose(); return; }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch { process.Dispose(); return; }

        await completion.Task;
    }

    public static async Task<VercelCliResult> RunAsync(string arguments, string cwd,
        Action<string>? onLog = null, CancellationToken ct = default)
    {
        var completion = new TaskCompletionSource<VercelCliResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = new StringBuilder();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c npx --yes vercel {arguments}",
                WorkingDirectory = cwd,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            },
            EnableRaisingEvents = true,
        };

        void Append(string? line)
        {
            if (line is null) return;
            output.AppendLine(line);
            onLog?.Invoke(line);
        }

        process.OutputDataReceived += (_, e) => Append(e.Data);
        process.ErrorDataReceived += (_, e) => Append(e.Data);
        process.Exited += (_, _) =>
        {
            try { completion.TrySetResult(new VercelCliResult(process.ExitCode, output.ToString())); }
            finally { process.Dispose(); }
        };

        using var reg = ct.CanBeCanceled
            ? ct.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch { }
            })
            : default;

        try
        {
            if (!process.Start())
            {
                process.Dispose();
                return new VercelCliResult(-1, "Failed to start Vercel CLI.");
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.StandardInput.Close();
        }
        catch (Exception ex)
        {
            process.Dispose();
            completion.TrySetException(ex);
        }

        return await completion.Task;
    }

    private static IEnumerable<string> ExtractUrls(string line)
    {
        foreach (Match match in UrlRegex().Matches(line))
            yield return match.Value.TrimEnd('.', ',', ';', ')');
    }

    [GeneratedRegex(@"https?://[^\s<>""']+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    private static string ExtractJson(string output)
    {
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var jsonStartLine = lines.Select((l, i) => (l, i))
            .FirstOrDefault(x => x.l.TrimStart().StartsWith('{'));

        if (jsonStartLine.l is null) return "";

        var joined = string.Join("\n", lines.Skip(jsonStartLine.i));
        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = 0; i < joined.Length; i++)
        {
            var c = joined[i];

            if (escape) { escape = false; continue; }
            if (c == '\\' && inString) { escape = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;

            if (c == '{') depth++;
            else if (c == '}')
            {
                if (--depth == 0) return joined[..(i + 1)];
            }
        }

        return "";
    }
}
