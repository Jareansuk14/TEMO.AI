namespace TEMO.AI;

internal static class SettingsStore
{
    private const string FileName = "temo-settings.json";

    private static string FilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);

    public static string? LoadApiKey() => Get("apiKey");

    public static void SaveApiKey(string key) => Set("apiKey", key);

    public static string? LoadLastProject() => Get("lastProject");

    public static void SaveLastProject(string path) => Set("lastProject", path);

    public static string? LoadPrompt(string type) => Get(PromptKey(type));

    public static void SavePrompt(string type, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) Remove(PromptKey(type));
        else Set(PromptKey(type), value);
    }

    private static string PromptKey(string type) => "prompt." + type.ToLowerInvariant();

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

    private static void Remove(string key)
    {
        var root = JsonFile.ReadObject(FilePath);
        if (root.Remove(key)) JsonFile.WriteObject(FilePath, root);
    }
}
