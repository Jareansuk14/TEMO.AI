namespace TEMO.AI;

internal static class SettingsStore
{
    private const string FileName = "temo-settings.json";

    private static string FilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);

    public static string? LoadApiKey() => Get("apiKey");

    public static void SaveApiKey(string key) => Set("apiKey", key);

    public static string? LoadLastProject() => Get("lastProject");

    public static void SaveLastProject(string path) => Set("lastProject", path);

    private static string? Get(string key)
    {
        try { return JsonFile.ReadObject(FilePath)[key]?.GetValue<string>(); }
        catch { return null; }
    }

    private static void Set(string key, string value)
    {
        var root = JsonFile.ReadObject(FilePath);
        root[key] = value;
        JsonFile.WriteObject(FilePath, root);
    }
}
