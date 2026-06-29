namespace TEMO.AI;

internal static class SectionCatalog
{
    private static IReadOnlyList<SectionDefinition>? _definitions;

    public static IReadOnlyList<string> Warnings { get; private set; } = [];

    public static IReadOnlyList<SectionDefinition> All => _definitions ??= Build();

    private static IReadOnlyList<SectionDefinition> Build()
    {
        var defs = ComponentStore.List();
        Warnings = ComponentValidator.Validate(defs);
        foreach (var w in Warnings)
            System.Diagnostics.Debug.WriteLine($"[ComponentValidator] {w}");
        return defs;
    }

    public static void Reload()
    {
        _definitions = null;
        ImageSpecRegistry.Reload();
        ImageGroupCatalog.Reload();
        ShellSlot.Reload();
    }

    public static SectionDefinition? FindByComponentName(string componentName) =>
        All.FirstOrDefault(x => x.ComponentName.Equals(componentName, StringComparison.Ordinal));

    public static IEnumerable<SectionDefinition> ForKind(string kind) =>
        All.Where(x => x.Kind.Equals(kind, StringComparison.Ordinal));

    public static SectionDefinition? AnyOfKind(string kind) => ForKind(kind).FirstOrDefault();

    public static string? ContentLabel(string? kind) =>
        kind is not null && AnyOfKind(kind) is { Fields.Count: > 0 } def
            ? def.Fields[0].Group
            : null;

    public static string DisplayName(string kind) =>
        AnyOfKind(kind)?.DisplayName ?? kind;

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
        Slot = definition.Slot,
        Required = definition.Required,
        Weight = definition.Weight,
        CanRemove = !definition.Required,
        CanChangeVariant = true,
    };
}
