namespace TEMO.AI;

internal static class LayoutComposer
{
    private const int OptionalSkipPercent = 35;

    public static IReadOnlyList<LayoutComponent> Compose(string projectPath, Random rng)
    {
        ComponentStore.EnsureSeeded();
        SectionCatalog.Reload();

        foreach (var slot in ShellSlot.All)
        {
            var variants = SectionCatalog.All.Where(d => d.Slot == slot).ToList();
            if (variants.Count == 0) continue;
            ComponentStore.CopyToProject(projectPath, variants[rng.Next(variants.Count)]);
        }

        var chosen = new List<SectionDefinition>();
        foreach (var group in SectionCatalog.All
                     .Where(d => d.Slot == "body")
                     .GroupBy(d => d.Kind, StringComparer.Ordinal))
        {
            var variants = group.ToList();
            var required = variants.Any(v => v.Required);
            if (!required && rng.Next(100) < OptionalSkipPercent) continue;
            chosen.Add(variants[rng.Next(variants.Count)]);
        }

        var ordered = chosen
            .OrderBy(d => d.Weight)
            .ThenBy(d => d.Kind, StringComparer.Ordinal)
            .ToList();
        if (ordered.Count == 0) return Array.Empty<LayoutComponent>();

        var components = ordered.Select(SectionCatalog.ToLayoutComponent).ToList();

        foreach (var def in ordered)
            ComponentStore.CopyToProject(projectPath, def);

        if (LayoutStore.ReadIndex(projectPath) is { } index)
        {
            var banner = LayoutStore.ParseBannerLine(index);
            if (LayoutStore.BuildIndex(index, banner, components) is { } built)
                LayoutStore.WriteIndex(projectPath, built);
        }

        ProjectComponentSync.RemoveOrphans(projectPath, ordered);
        LegacySectionRepair.Repair(projectPath, components);
        return components;
    }
}
