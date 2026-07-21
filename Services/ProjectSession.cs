namespace TEMO.AI;

/// Shared per-project state. Acts as the seam that ViewModels will consume
/// when the UI is migrated to MVVM; for now MainWindow delegates its fields here.
internal sealed class ProjectSession
{
    public string ProjectPath { get; set; } = "";

    public List<FieldDef> Fields { get; } = [];
    public Dictionary<string, TextBox> Boxes { get; } = [];
    public Dictionary<string, TextBox> CssBoxes { get; } = [];
    public Dictionary<string, TextBox> SiteBoxes { get; } = [];
    public List<LayoutComponent> LayoutComponents { get; } = [];
    public List<ImageEntry> ImageEntries { get; } = [];
}
