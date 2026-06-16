namespace TEMO.AI;

internal static class Io
{
    [ThreadStatic] private static Dictionary<string, string?>? _session;

    public static string Read(string path) => ReadOrNull(path) ?? File.ReadAllText(path, Encoding.UTF8);

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
