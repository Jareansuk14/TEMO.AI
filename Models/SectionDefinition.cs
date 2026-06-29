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
    IReadOnlyList<ManifestField> Fields,
    IReadOnlyList<ManifestImage> Images,
    bool SelectByCount,
    ContentSpec Spec);

internal sealed record ContentSpec(
    int HeadingMin,
    int HeadingMax,
    int ImageMin,
    int ImageMax,
    string ImageRatio,
    string ImageType,
    string Link,
    string ImageGroup)
{
    public static readonly ContentSpec Empty = new(0, 0, 0, 0, "", "", "none", "");

    public bool HasHeadings => HeadingMax > 0;
    public bool HasImages => ImageMax > 0 || !string.IsNullOrWhiteSpace(ImageGroup);
    public bool IsDefined => HasHeadings || HasImages;
}
