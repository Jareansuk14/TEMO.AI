namespace TEMO.AI;

internal static class JsonFile
{
    public static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    public static T? Read<T>(string path, JsonSerializerOptions? options = null)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), options ?? CaseInsensitive)
                : default;
        }
        catch { return default; }
    }

    public static void Write<T>(string path, T value, JsonSerializerOptions? options = null)
    {
        try
        {
            EnsureDir(path);
            File.WriteAllText(path, JsonSerializer.Serialize(value, options ?? CaseInsensitive));
        }
        catch { }
    }

    public static JsonObject ReadObject(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? new JsonObject()
                : new JsonObject();
        }
        catch { return new JsonObject(); }
    }

    public static void WriteObject(string path, JsonObject root, bool indented = false)
    {
        try
        {
            EnsureDir(path);
            File.WriteAllText(path, root.ToJsonString(indented ? IndentedOptions : null));
        }
        catch { }
    }

    private static void EnsureDir(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }
}
