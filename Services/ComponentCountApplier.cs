namespace TEMO.AI;

internal static class ComponentCountApplier
{
    private const int GameMin = 4, GameMax = 6;
    private const int FaqMin = 2, FaqMax = 6;

    public static IReadOnlyList<(string Kind, int Headings, int Images)> Apply(
        string root, IReadOnlyList<LayoutComponent> components, Random rng, Action<string> deleteFile)
    {
        var summary = new List<(string, int, int)>();
        summary.AddRange(ApplyFixedHeadings(root, components, deleteFile));
        summary.AddRange(ApplyFixedImages(root, components, deleteFile));

        foreach (var c in components)
        {
            if (!c.Kind.Equals("Game", StringComparison.OrdinalIgnoreCase)) continue;
            if ((SectionCatalog.FindByComponentName(c.Name) ?? SectionCatalog.AnyOfKind(c.Kind)) is not { } def) continue;
            if (string.IsNullOrWhiteSpace(def.Spec.ImageGroup)) continue;

            var count = rng.Next(GameMin, GameMax + 1);
            ImagesStore.ApplyImages(root, def.Spec, count, deleteFile);
            summary.Add((def.Kind, 0, count));
        }

        if (SectionCatalog.AnyOfKind("Faq") is { } faqDef)
        {
            var faqCount = rng.Next(FaqMin, FaqMax + 1);
            ContentStore.ApplyHeadings(root, faqDef, faqCount);
            summary.Add(("Faq", faqCount, 0));
        }

        return summary;
    }

    public static IReadOnlyList<(string Kind, int Headings, int Images)> ApplyFixedImages(
        string root, IReadOnlyList<LayoutComponent> components, Action<string> deleteFile)
    {
        var summary = new List<(string, int, int)>();

        foreach (var def in ActiveDefinitions(root, components))
        {
            if (def.Spec.ImageCount <= 0 || string.IsNullOrWhiteSpace(def.Spec.ImageGroup)) continue;

            ImagesStore.ApplyImages(root, def.Spec, def.Spec.ImageCount, deleteFile);
            summary.Add((def.Kind, 0, def.Spec.ImageCount));
        }

        return summary;
    }

    public static IReadOnlyList<(string Kind, int Headings, int Images)> ApplyFixedHeadings(
        string root, IReadOnlyList<LayoutComponent> components, Action<string> _)
    {
        var summary = new List<(string, int, int)>();

        foreach (var def in ActiveDefinitions(root, components))
        {
            if (def.Spec.HeadingCount <= 0 || !def.Repeatable) continue;

            ContentStore.ApplyHeadings(root, def, def.Spec.HeadingCount);
            summary.Add((def.Kind, def.Spec.HeadingCount, 0));
        }

        return summary;
    }

    private static IEnumerable<SectionDefinition> ActiveDefinitions(string root, IReadOnlyList<LayoutComponent> components)
    {
        var body = components
            .Select(c => SectionCatalog.FindByComponentName(c.Name) ?? SectionCatalog.AnyOfKind(c.Kind))
            .Where(x => x is not null)
            .Cast<SectionDefinition>();

        var slots = ShellSlot.All
            .Select(slot => ComponentStore.CurrentForSlot(root, slot))
            .Where(x => x is not null)
            .Cast<SectionDefinition>();

        return body.Concat(slots)
            .GroupBy(x => x.ComponentName, StringComparer.Ordinal)
            .Select(g => g.First());
    }
}
