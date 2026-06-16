namespace TEMO.AI;

internal static class AstroProjectSettings
{
    private const string SettingsRel = @".astro\settings.json";

    public static void DisableDevToolbar(string projectPath)
    {
        if (!ProjectPaths.IsProject(projectPath)) return;

        try
        {
            var path = Path.Combine(projectPath, SettingsRel);
            var root = JsonFile.ReadObject(path);

            var devToolbar = root["devToolbar"] as JsonObject ?? new JsonObject();
            if (devToolbar["enabled"]?.GetValue<bool>() == false && root["devToolbar"] is not null)
                return;

            devToolbar["enabled"] = false;
            root["devToolbar"] = devToolbar;

            JsonFile.WriteObject(path, root, indented: true);
        }
        catch { }
    }
}
