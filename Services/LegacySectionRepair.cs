namespace TEMO.AI;

internal static class LegacySectionRepair
{
    public static void Repair(string projectPath, IReadOnlyList<LayoutComponent>? composed = null)
    {
        if (!ProjectPaths.IsProject(projectPath)) return;

        SectionCatalog.Reload();
        ComponentStore.EnsureSeeded();

        var byKind = new Dictionary<string, LayoutComponent>(StringComparer.OrdinalIgnoreCase);
        if (composed is not null)
        {
            foreach (var comp in composed)
                if (!string.IsNullOrWhiteSpace(comp.Kind))
                    byKind[comp.Kind] = comp;
        }

        if (LayoutStore.ReadIndex(projectPath) is { } index)
        {
            var imports = LayoutStore.ParseSectionImportKinds(index);
            foreach (var name in LayoutStore.ParseComponentNames(index))
            {
                if (SectionCatalog.FindByComponentName(name) is { } byName)
                {
                    byKind.TryAdd(byName.Kind, SectionCatalog.ToLayoutComponent(byName));
                    continue;
                }

                if (!imports.TryGetValue(name, out var kind)
                    || byKind.ContainsKey(kind)
                    || SectionCatalog.AnyOfKind(kind) is not { } fallback)
                    continue;

                byKind[kind] = SectionCatalog.ToLayoutComponent(fallback);
            }
        }

        var dir = ProjectPaths.Src(projectPath, Path.Combine("components", "sections"));
        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.GetFiles(dir, "*.astro"))
        {
            var kind = Path.GetFileNameWithoutExtension(file);
            if (!IsLegacyHardcoded(file)) continue;

            SectionDefinition? def = null;
            if (byKind.TryGetValue(kind, out var comp))
                def = SectionCatalog.FindByComponentName(comp.Name) ?? SectionCatalog.AnyOfKind(kind);
            else
                def = SectionCatalog.AnyOfKind(kind);

            if (def is { DataFile.Length: > 0 })
                ComponentStore.CopyToProject(projectPath, def);
        }

        RepairStaleIndex(projectPath, byKind);
    }

    private static void RepairStaleIndex(string projectPath, Dictionary<string, LayoutComponent> byKind)
    {
        if (LayoutStore.ReadIndex(projectPath) is not { } index || byKind.Count == 0) return;

        var stale = LayoutStore.ParseSectionImportKinds(index).Keys
            .Any(name => SectionCatalog.FindByComponentName(name) is null);
        if (!stale) return;

        var ordered = byKind.Values
            .Where(c => string.Equals(c.Slot, "body", StringComparison.Ordinal))
            .OrderBy(c => c.Weight)
            .ThenBy(c => c.Kind, StringComparer.Ordinal)
            .ToList();
        if (ordered.Count == 0) return;

        var banner = LayoutStore.ParseBannerLine(index);
        if (LayoutStore.BuildIndex(index, banner, ordered) is not { } built) return;

        LayoutStore.WriteIndex(projectPath, built);

        var defs = ordered
            .Select(c => SectionCatalog.FindByComponentName(c.Name) ?? SectionCatalog.AnyOfKind(c.Kind))
            .Where(d => d is not null)
            .Cast<SectionDefinition>()
            .ToList();
        if (defs.Count > 0)
            ProjectComponentSync.RemoveOrphans(projectPath, defs);
    }

    private static bool IsLegacyHardcoded(string path)
    {
        try
        {
            return !File.ReadAllText(path).Contains("@/data/content/", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
