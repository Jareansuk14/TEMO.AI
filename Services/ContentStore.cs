namespace TEMO.AI;

internal static class ContentStore
{
    public const string SiteConfigRel = @"config\site.ts";
    public const string FaqRel = @"components\FAQSection.astro";

    private static string Src(string root, string rel) => ProjectPaths.Src(root, rel);

    public static List<FieldDef> BuildFields(string root, IReadOnlyList<LayoutComponent> layout)
    {
        var fields = new List<FieldDef>();

        var activeKinds = layout
            .Select(x => x.Kind)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToHashSet(StringComparer.Ordinal);

        AddBrandField(root, fields);
        if (activeKinds.Contains("Hero")) AddHeroFields(fields);
        if (activeKinds.Contains("Seo")) AddSeoFields(root, fields);
        if (activeKinds.Contains("Cta")) AddCtaFields(fields);
        if (activeKinds.Contains("Promotion")) AddPromotionComponentFields(fields);
        AddPageFields(fields);
        AddFaqFields(root, fields);

        return fields;
    }

    private static void AddBrandField(string root, List<FieldDef> fields)
    {
        if (BrandExists(root))
            fields.Add(new("brand", "Brand Name", "BRAND", "", "", FieldKind.Brand));
    }

    private static void AddHeroFields(List<FieldDef> fields)
    {
        var heroFile = SectionFile("Hero");
        fields.Add(new("main-seo", "Main Heading", "HERO", heroFile,
            @"(<h1[^>]*\bid=""main-seo""[^>]*>(?:<span[^>]*>[^<]*</span>)?)([\s\S]*?)(</h1>)"));
        fields.Add(new("sub-main-seo", "Hero Description", "HERO", heroFile,
            @"(<p[^>]*\bid=""sub-main-seo""[^>]*>)\s*([\s\S]*?)\s*(</p>)", Multi: true));
    }

    private static void AddSeoFields(string root, List<FieldDef> fields)
    {
        var seoFile = SectionFile("Seo");
        foreach (var n in DiscoverSeoNumbers(root, seoFile))
        {
            fields.Add(new($"seo-{n}-h", $"SEO {n} — Heading", "SEO", seoFile,
                $@"(id:\s*""seo-{n}"",\s*heading:\s*"")([\s\S]*?)("")"));
            fields.Add(new($"seo-{n}-d", $"SEO {n} — Body", "SEO", seoFile,
                $@"(id:\s*""seo-{n}"",\s*heading:\s*""[^""]*"",\s*desc:\s*"")([\s\S]*?)("")", Multi: true));
        }
    }

    private static void AddCtaFields(List<FieldDef> fields)
    {
        var ctaFile = SectionFile("Cta");
        fields.Add(new("cta-seo", "CTA Heading", "CTA", ctaFile,
            @"(<h2[^>]*\bid=""cta-seo""[^>]*>(?:<span[^>]*>[^<]*</span>)?)([\s\S]*?)(</h2>)"));
        fields.Add(new("sub-cta-seo", "CTA Description", "CTA", ctaFile,
            @"(<p[^>]*\bid=""sub-cta-seo""[^>]*>)([\s\S]*?)(</p>)", Multi: true));
    }

    private static void AddPromotionComponentFields(List<FieldDef> fields)
    {
        var promoFile = SectionFile("Promotion");
        fields.Add(new("promo-comp-h", "หัวข้อ Promotion", "PROMOTION SECTION", promoFile,
            @"(<h2[^>]*\bid=""promo-heading""[^>]*>)([\s\S]*?)(</h2>)"));
        fields.Add(new("promo-comp-d", "คำอธิบาย Promotion", "PROMOTION SECTION", promoFile,
            @"(<p[^>]*\bclass=""section-desc""[^>]*>)([\s\S]*?)(</p>)", Multi: true));
    }

    private static void AddPageFields(List<FieldDef> fields)
    {
        fields.Add(new("promotion-seo", "Heading", "PROMOTIONS", @"pages\promotions.astro",
            @"(<h1[^>]*\bid=""promotion-seo""[^>]*>(?:<span[^>]*>[^<]*</span>)?)([\s\S]*?)(</h1>)"));
        fields.Add(new("sub-promo-seo", "Description", "PROMOTIONS", @"pages\promotions.astro",
            @"(<p[^>]*\bid=""sub-promotion-seo""[^>]*>)([\s\S]*?)(</p>)", Multi: true));

        fields.Add(new("contact-seo", "Heading", "CONTACT", @"pages\contact.astro",
            @"(<h1[^>]*\bid=""contact-seo""[^>]*>(?:<span[^>]*>[^<]*</span>)?)([\s\S]*?)(</h1>)"));
        fields.Add(new("sub-cont-seo", "Description", "CONTACT", @"pages\contact.astro",
            @"(<p[^>]*\bid=""sub-contact-seo""[^>]*>)([\s\S]*?)(</p>)", Multi: true));
    }

    private static void AddFaqFields(string root, List<FieldDef> fields)
    {
        foreach (var i in Enumerable.Range(1, DiscoverFaqCount(root)))
        {
            fields.Add(new($"faq-q-{i}", $"Q{i}", "FAQ", FaqRel, "", FieldKind.Faq));
            fields.Add(new($"faq-a-{i}", $"A{i}", "FAQ", FaqRel, "", FieldKind.Faq, Multi: true));
        }
    }

    public static Dictionary<string, string> Pull(string root, IReadOnlyList<FieldDef> fields)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (Io.ReadOrNull(Src(root, SiteConfigRel)) is { } site)
        {
            var brand = Regex.Match(site, @"export\s+const\s+BRAND_NAME\s*=\s*""([^""]+)""");
            if (brand.Success) values["brand"] = brand.Groups[1].Value.Trim();
        }

        foreach (var f in fields)
        {
            if (!f.IsPattern) continue;
            var content = ReadCached(cache, Src(root, f.RelFile));
            if (content is null) continue;
            var m = Regex.Match(content, f.Pattern, RegexOptions.Singleline);
            if (m.Success) values[f.Id] = m.Groups[2].Value.Trim();
        }

        if (ReadCached(cache, Src(root, FaqRel)) is { } faq && faq.Contains("faq-q-"))
        {
            var matches = Regex.Matches(faq, @"\{\s*q:\s*""([\s\S]*?)"",\s*a:\s*""([\s\S]*?)""\s*\}");
            for (int i = 0; i < matches.Count; i++)
            {
                values[$"faq-q-{i + 1}"] = matches[i].Groups[1].Value;
                values[$"faq-a-{i + 1}"] = matches[i].Groups[2].Value;
            }
        }

        return values;
    }

    public static int Save(string root, IReadOnlyList<FieldDef> fields, IReadOnlyDictionary<string, string> values)
    {
        int changed = 0;

        var brand = values.TryGetValue("brand", out var b) ? b.Trim() : "";
        changed += SaveBrand(root, brand);

        var fileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var originals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in fields)
        {
            if (!f.IsPattern || !values.TryGetValue(f.Id, out var value)) continue;
            var path = Src(root, f.RelFile);
            if (!fileMap.TryGetValue(path, out var content))
            {
                if (Io.ReadOrNull(path) is not { } loaded) continue;
                content = loaded;
                originals[path] = content;
            }
            fileMap[path] = Regex.Replace(content, f.Pattern,
                m => m.Groups[1].Value + value + m.Groups[3].Value, RegexOptions.Singleline);
        }
        foreach (var (path, content) in fileMap)
        {
            if (originals.TryGetValue(path, out var orig) && orig == content) continue;
            Io.Write(path, content);
            changed++;
        }

        changed += SaveFaq(root, fields, values);
        return changed;
    }

    public static int SaveFaq(string root, IReadOnlyList<FieldDef> fields, IReadOnlyDictionary<string, string> values)
    {
        var path = Src(root, FaqRel);
        if (!File.Exists(path)) return 0;

        var nums = FaqNumbers(fields);
        if (nums.Count == 0) return 0;

        var sb = new StringBuilder("const faqs = [\n");
        for (int i = 0; i < nums.Count; i++)
        {
            var q = EscapeJsString(values.GetValueOrDefault($"faq-q-{nums[i]}", ""));
            var a = EscapeJsString(values.GetValueOrDefault($"faq-a-{nums[i]}", ""));
            sb.Append($"  {{ q: \"{q}\", a: \"{a}\" }}");
            if (i < nums.Count - 1) sb.Append(',');
            sb.AppendLine();
        }
        sb.Append("];");

        var content = Io.Read(path);
        var updated = Regex.Replace(content, @"const faqs = \[[\s\S]*?\];", _ => sb.ToString());
        if (updated == content) return 0;
        Io.Write(path, updated);
        return 1;
    }

    private static string EscapeJsString(string value) => value
        .Replace("\\", "\\\\")
        .Replace("\"", "\\\"")
        .Replace("\r", "")
        .Replace("\n", "\\n");

    public static List<int> FaqNumbers(IEnumerable<FieldDef> fields) =>
        fields.Where(f => f.Id.StartsWith("faq-q-"))
              .Select(f => int.Parse(f.Id["faq-q-".Length..]))
              .OrderBy(n => n).ToList();

    public static string CurrentBrandName(string root)
    {
        if (Io.ReadOrNull(Src(root, SiteConfigRel)) is not { } content) return "SITE.name";
        var m = Regex.Match(content, @"export\s+const\s+BRAND_NAME\s*=\s*""([^""]+)""");
        return m.Success ? m.Groups[1].Value : "SITE.name";
    }

    private static int SaveBrand(string root, string brandName)
    {
        if (string.IsNullOrEmpty(brandName)) return 0;
        var path = Src(root, SiteConfigRel);
        if (Io.ReadOrNull(path) is not { } content) return 0;

        var updated = Rx.Wrap(content, @"(export\s+const\s+BRAND_NAME\s*=\s*"")[^""]*("")", brandName);
        updated = Rx.Wrap(updated, @"(name:\s*"")[^""]*("")", brandName);
        if (updated == content) return 0;
        Io.Write(path, updated);
        return 1;
    }

    private static string SectionFile(string kind) => $@"components\sections\{kind}.astro";

    private static List<int> DiscoverSeoNumbers(string root, string seoRel)
    {
        if (Io.ReadOrNull(Src(root, seoRel)) is not { } content) return [];
        return Regex.Matches(content, @"id:\s*""seo-(\d+)""")
            .Select(m => int.Parse(m.Groups[1].Value))
            .Distinct().OrderBy(n => n).ToList();
    }

    private static int DiscoverFaqCount(string root)
    {
        if (Io.ReadOrNull(Src(root, FaqRel)) is not { } content) return 0;
        if (!content.Contains("faq-q-")) return 0;
        return Regex.Matches(content, @"\{\s*q:\s*""").Count;
    }

    private static bool BrandExists(string root)
    {
        var srcDir = ProjectPaths.Src(root);
        if (!Directory.Exists(srcDir)) return false;

        if (Io.ReadOrNull(Src(root, SiteConfigRel)) is { } site
            && Regex.IsMatch(site, @"export\s+const\s+BRAND_NAME\s*=\s*"""))
            return true;

        foreach (var file in Directory.EnumerateFiles(srcDir, "*.astro", SearchOption.AllDirectories))
        {
            var text = Io.Read(file);
            if (text.Contains("section-brand") || text.Contains("brand-seo")) return true;
        }
        return false;
    }

    private static string? ReadCached(Dictionary<string, string> cache, string path)
    {
        if (cache.TryGetValue(path, out var content)) return content;
        if (Io.ReadOrNull(path) is not { } loaded) return null;
        cache[path] = loaded;
        return loaded;
    }
}
