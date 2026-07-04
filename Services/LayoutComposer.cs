namespace TEMO.AI;

internal static class LayoutComposer
{
    private const int OptionalSkipPercent = 35;

    public static IReadOnlyList<LayoutComponent> Compose(string projectPath, Random rng)
    {
        ComponentStore.EnsureSeeded();
        SectionCatalog.Reload();

        var allowedButton = PickButtonSlot(rng);
        var buttonPlaced = false;

        foreach (var slot in ShellSlot.All)
        {
            var variants = SectionCatalog.All.Where(d => d.Slot == slot).ToList();
            if (variants.Count == 0) continue;
            var pool = ButtonPool(variants, IsSlotAllowed(allowedButton, true, slot), false);
            if (pool.Count == 0) pool = variants.ToList();
            var pick = PickVariant(pool, rng);
            if (pick.HasButtons) buttonPlaced = true;
            ComponentStore.CopyToProject(projectPath, pick);
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

            var pool = ButtonPool(variants, IsSlotAllowed(allowedButton, false, variants[0].Kind), transparentUsed);
            if (pool.Count == 0)
            {
                if (!required) continue;
                pool = variants.ToList();
            }

            var pick = PickVariant(pool, rng);
            if (UsesTransparentImage(pick)) transparentUsed = true;
            if (pick.HasButtons) buttonPlaced = true;
            chosen.Add(pick);
        }

        if (!buttonPlaced && allowedButton is not null)
            buttonPlaced = ForceButton(projectPath, chosen, transparentUsed, ref transparentUsed);

        EnsurePromotionForSeoTextOnly(chosen, transparentUsed, rng, ref transparentUsed);

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

    private sealed record ButtonKey(bool IsShell, string Name);

    private static bool IsSlotAllowed(ButtonKey? allowed, bool isShell, string key) =>
        allowed is { IsShell: var shell, Name: var name }
        && shell == isShell
        && string.Equals(name, key, StringComparison.Ordinal);

    private static List<SectionDefinition> ButtonPool(
        IReadOnlyList<SectionDefinition> variants, bool isAllowed, bool transparentUsed)
    {
        var byButton = isAllowed
            ? variants.Where(v => v.HasButtons)
            : variants.Where(v => !v.HasButtons);

        var pool = byButton.Where(v => !transparentUsed || !UsesTransparentImage(v)).ToList();
        if (pool.Count > 0) return pool;
        pool = byButton.ToList();
        return pool.Count > 0 ? pool : variants.ToList();
    }

    private static bool ForceButton(
        string projectPath, List<SectionDefinition> chosen, bool transparentUsed, ref bool transparentUsedRef)
    {
        var heroIdx = chosen.FindIndex(c => string.Equals(c.Kind, "Hero", StringComparison.Ordinal));
        if (heroIdx >= 0)
        {
            var heroButton = SectionCatalog.All.FirstOrDefault(d =>
                string.Equals(d.Kind, "Hero", StringComparison.Ordinal)
                && d.HasButtons && (!transparentUsed || !UsesTransparentImage(d)));
            if (heroButton is not null)
            {
                if (UsesTransparentImage(heroButton)) transparentUsedRef = true;
                chosen[heroIdx] = heroButton;
                return true;
            }
        }

        var bannerButton = SectionCatalog.All.FirstOrDefault(d =>
            d.Slot != "body" && d.HasButtons);
        if (bannerButton is not null)
        {
            ComponentStore.CopyToProject(projectPath, bannerButton);
            return true;
        }
        return false;
    }

    private static void EnsurePromotionForSeoTextOnly(
        List<SectionDefinition> chosen, bool transparentUsed, Random rng, ref bool transparentUsedRef)
    {
        var seo = chosen.FirstOrDefault(c => string.Equals(c.Kind, "Seo", StringComparison.Ordinal));
        if (seo is null || HasImages(seo)) return;
        if (chosen.Any(c => string.Equals(c.Kind, "Promotion", StringComparison.Ordinal))) return;

        var pool = SectionCatalog.ForKind("Promotion")
            .Where(v => !transparentUsed || !UsesTransparentImage(v))
            .ToList();
        if (pool.Count == 0) pool = SectionCatalog.ForKind("Promotion").ToList();
        if (pool.Count == 0) return;

        var pick = PickVariant(pool, rng);
        if (UsesTransparentImage(pick)) transparentUsedRef = true;
        chosen.Add(pick);
    }

    private static bool HasImages(SectionDefinition def) =>
        def.Images.Count > 0 || def.Spec.ImageCount > 0;

    private static ButtonKey? PickButtonSlot(Random rng)
    {
        var candidates = new List<ButtonKey>();
        foreach (var slot in ShellSlot.All)
            if (SectionCatalog.All.Any(d => d.Slot == slot && d.HasButtons))
                candidates.Add(new ButtonKey(true, slot));
        foreach (var kind in SectionCatalog.All
                     .Where(d => d.Slot == "body" && d.HasButtons)
                     .Select(d => d.Kind)
                     .Distinct(StringComparer.Ordinal))
            candidates.Add(new ButtonKey(false, kind));
        return candidates.Count == 0 ? null : candidates[rng.Next(candidates.Count)];
    }

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
