namespace TEMO.AI;

internal static class SectionMeta
{
    private sealed record Meta(string DisplayName, string? ContentLabel);

    private static readonly Dictionary<string, Meta> ByKind = new(StringComparer.Ordinal)
    {
        ["Hero"]      = new("Hero Section", "HERO"),
        ["Game"]      = new("Game Section", null),
        ["Seo"]       = new("SEO Section", "SEO"),
        ["Promotion"] = new("Promotion", "PROMOTION SECTION"),
        ["Provider"]  = new("Provider", null),
        ["Cta"]       = new("Call To Action", "CTA"),
    };

    public static string DisplayName(string kind) =>
        ByKind.TryGetValue(kind, out var meta) ? meta.DisplayName : kind;

    public static string? ContentLabel(string? kind) =>
        kind is not null && ByKind.TryGetValue(kind, out var meta) ? meta.ContentLabel : null;
}
