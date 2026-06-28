namespace TEMO.AI;

internal sealed class ComponentManifest
{
    public string Kind { get; set; } = "";
    public string Variant { get; set; } = "";
    public string ComponentName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string AstroFile { get; set; } = "";
    public string CssFile { get; set; } = "";
    public string Slot { get; set; } = "body";
    public bool Required { get; set; }
    public int Weight { get; set; } = 100;
    public bool HasExternalLink { get; set; }
    public string DataFile { get; set; } = "";
    public string DataConst { get; set; } = "";
    public bool Repeatable { get; set; }
    public List<ManifestField> Fields { get; set; } = [];
}

internal sealed class ManifestField
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Type { get; set; } = "text";
    public string Group { get; set; } = "";
}
