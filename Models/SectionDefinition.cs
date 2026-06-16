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
    bool HasExternalLink);

internal static class SectionCatalog
{
    private static IReadOnlyList<SectionDefinition>? _definitions;

    public static IReadOnlyList<SectionDefinition> All => _definitions ??= ComponentStore.List();

    public static void Reload() => _definitions = ComponentStore.List();

    public static SectionDefinition? FindByComponentName(string componentName) =>
        All.FirstOrDefault(x => x.ComponentName.Equals(componentName, StringComparison.Ordinal));

    public static IEnumerable<SectionDefinition> ForKind(string kind) =>
        All.Where(x => x.Kind.Equals(kind, StringComparison.Ordinal));

    public static LayoutComponent ToLayoutComponent(SectionDefinition definition) => new()
    {
        Name = definition.ComponentName,
        DisplayName = definition.DisplayName,
        Kind = definition.Kind,
        Variant = definition.Variant,
        ImportPath = definition.ImportPath,
        CssImportPath = definition.CssImportPath,
        StoreDirectory = definition.StoreDirectory,
        AstroFile = definition.AstroFile,
        CssFile = definition.CssFile,
        HasExternalLink = definition.HasExternalLink,
        CanRemove = true,
        CanChangeVariant = true,
    };
}
