namespace TEMO.AI;

internal sealed class LayoutComponent
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Variant { get; set; } = "";
    public string ImportPath { get; set; } = "";
    public string CssImportPath { get; set; } = "";
    public string StoreDirectory { get; set; } = "";
    public string AstroFile { get; set; } = "";
    public string CssFile { get; set; } = "";
    public bool HasExternalLink { get; set; }
    public bool CanRemove { get; set; } = true;
    public bool CanChangeVariant { get; set; } = true;

    public override string ToString() => DisplayName;
}
