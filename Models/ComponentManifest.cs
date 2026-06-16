namespace TEMO.AI;

internal sealed class ComponentManifest
{
    public string Kind { get; set; } = "";
    public string Variant { get; set; } = "";
    public string ComponentName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string AstroFile { get; set; } = "";
    public string CssFile { get; set; } = "";
    public bool HasExternalLink { get; set; }
}
