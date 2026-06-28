namespace TEMO.AI;

internal static class ShellSlot
{
    public static readonly string[] All =
        ["header", "banner", "faq", "page:promotions", "page:contact"];

    public static string? AstroPath(string projectPath, string slot)
    {
        var src = ProjectPaths.Src(projectPath);
        string C(params string[] parts) => Path.Combine(new[] { src }.Concat(parts).ToArray());
        return slot switch
        {
            "header" => C("components", "Header.astro"),
            "banner" => C("components", "Banner.astro"),
            "faq" => C("components", "FAQSection.astro"),
            "page:promotions" => C("components", "page", "PromotionsBody.astro"),
            "page:contact" => C("components", "page", "ContactBody.astro"),
            _ => null,
        };
    }
}
