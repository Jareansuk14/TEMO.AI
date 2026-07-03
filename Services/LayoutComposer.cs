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
            ComponentStore.CopyToProject(projectPath, PickVariant(variants, rng));
        }

        var groups = SectionCatalog.All
            .Where(d => d.Slot == "body")
            .GroupBy(d => d.Kind, StringComparer.Ordinal)
            .Select(g => g.ToList())
            .ToList();
        Shuffle(groups, rng);

        var chosen = new List<SectionDefinition>();
        var transparentUsed = false;
        foreach (var variants in groups)
        {
            var required = variants.Any(v => v.Required);
            if (!required && rng.Next(100) < OptionalSkipPercent) continue;

            var pool = transparentUsed
                ? variants.Where(v => !UsesTransparentImage(v)).ToList()
                : variants;
            if (pool.Count == 0)
            {
                if (!required) continue;
                pool = variants;
            }

            var pick = PickVariant(pool, rng);
            if (UsesTransparentImage(pick)) transparentUsed = true;
            chosen.Add(pick);
        }

        var ordered = chosen;
        Shuffle(ordered, rng);
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

    private static SectionDefinition PickVariant(IReadOnlyList<SectionDefinition> variants, Random rng)
    {
        var bucketGroups = variants
            .Select(v => (Bucket: FolderBucket(v), Variant: v))
            .Where(x => x.Bucket is not null)
            .GroupBy(x => x.Bucket!, x => x.Variant)
            .ToList();

        if (bucketGroups.Count > 1 && bucketGroups.Sum(g => g.Count()) == variants.Count)
        {
            var group = bucketGroups[rng.Next(bucketGroups.Count)].ToList();
            return group[rng.Next(group.Count)];
        }

        var countGroups = variants
            .Where(v => v.Spec.ImageCount > 0)
            .GroupBy(v => v.Spec.ImageCount.ToString())
            .ToList();

        if (countGroups.Count > 1 && countGroups.Sum(g => g.Count()) == variants.Count)
        {
            var group = countGroups[rng.Next(countGroups.Count)].ToList();
            return group[rng.Next(group.Count)];
        }

        return variants[rng.Next(variants.Count)];
    }

    private static bool UsesTransparentImage(SectionDefinition def) =>
        string.Equals(def.Spec.ImageType, "transparent", StringComparison.OrdinalIgnoreCase)
        || def.Images.Any(img => img.Role.Contains("transparent", StringComparison.OrdinalIgnoreCase));

    private static string? FolderBucket(SectionDefinition def)
    {
        var parent = Directory.GetParent(def.StoreDirectory)?.Name;
        return parent is not null && parent.StartsWith("IMG", StringComparison.OrdinalIgnoreCase)
            ? parent.ToUpperInvariant()
            : null;
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
