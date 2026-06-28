namespace TEMO.AI;

internal static class Io
{
    [ThreadStatic] private static Dictionary<string, string?>? _session;

    public static void Write(string path, string text)
    {
        File.WriteAllText(path, text, Encoding.UTF8);
        if (_session is { } s) s[path] = text;
    }

    public static string? ReadOrNull(string path)
    {
        if (_session is { } s && s.TryGetValue(path, out var cached)) return cached;
        var value = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;
        if (_session is { } store) store[path] = value;
        return value;
    }

    public static void DeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }

    public static void DeleteFileWithRetry(string path, int attempts = 5)
    {
        if (!File.Exists(path)) return;
        for (var i = 0; i < attempts; i++)
        {
            try { File.Delete(path); return; }
            catch (IOException) { Thread.Sleep(60); }
            catch (UnauthorizedAccessException) { Thread.Sleep(60); }
            catch { return; }
        }
    }

    public static void DeleteDirectory(string path, bool ignoreErrors = true)
    {
        try
        {
            if (!Directory.Exists(path)) return;

            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            foreach (var dir in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories))
                File.SetAttributes(dir, FileAttributes.Normal);

            Directory.Delete(path, recursive: true);
        }
        catch when (ignoreErrors) { }
    }

    public static IDisposable Session() => new Scope();

    private sealed class Scope : IDisposable
    {
        private readonly bool _owner;

        public Scope()
        {
            _owner = _session is null;
            _session ??= new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            if (_owner) _session = null;
        }
    }
}
