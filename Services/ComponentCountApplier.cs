namespace TEMO.AI;

internal static class ComponentCountApplier
{
    public static IReadOnlyList<(string Kind, int Headings, int Images)> Apply(
        string root, IReadOnlyList<LayoutComponent> components, Random rng, Action<string> deleteFile)
    {
        var summary = new List<(string, int, int)>();
        foreach (var c in components)
        {
            var def = SectionCatalog.FindByComponentName(c.Name) ?? SectionCatalog.AnyOfKind(c.Kind);
            if (def is null || !def.Spec.IsDefined) continue;

            var counts = ComponentRandomizer.Roll(def.Spec, rng);

            if (def.Spec.HasHeadings && counts.Headings > 0)
                ContentStore.ApplyHeadings(root, def, counts.Headings);

            if (def.Spec.HasImages)
                ImagesStore.ApplyImages(root, def.Spec, counts.Images, deleteFile);

            summary.Add((def.Kind, counts.Headings, counts.Images));
        }
        return summary;
    }
}
