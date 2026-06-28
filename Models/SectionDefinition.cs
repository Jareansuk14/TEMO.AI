namespace TEMO.AI;

internal sealed record SectionDefinition(
    string Kind,
    string Variant,
    string ComponentName,
    string DisplayName,
    string ImportPath,
    string CssImportPath,
    string StoreDirectory,
    string AstroFile,
    string CssFile,
    bool HasExternalLink,
    string Slot,
    bool Required,
    int Weight,
    string DataFile,
    string DataConst,
    bool Repeatable,
    IReadOnlyList<ManifestField> Fields);
