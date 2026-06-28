namespace TEMO.AI;

internal static class VercelAuthStore
{
    private static readonly string[] AuthPaths =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vercel", "auth.json"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "xdg.data", "com.vercel.cli", "auth.json"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "com.vercel.cli", "auth.json"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "com.vercel.cli", "auth.json"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vercel", "auth.json"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "vercel", "auth.json"),
    ];

    public static string? TryGetToken()
    {
        foreach (var path in AuthPaths)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
                var token = JsonFile.ReadObject(path)["token"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(token)) return token;
            }
            catch { }
        }
        return null;
    }
}
