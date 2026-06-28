namespace TEMO.AI;

internal static class ContentStore
{
    public const string SiteConfigRel = @"config\site.ts";
    public const string FaqDataRel = @"data\content\faq.ts";
    public const string FaqConst = "FAQ";

    private static readonly Regex BrandNamePrefix = new(@"export\s+const\s+BRAND_NAME\s*=\s*""", RegexOptions.Compiled);
    private static readonly Regex BrandNameAny = new(@"export\s+const\s+BRAND_NAME\s*=\s*""([^""]*)""", RegexOptions.Compiled);
    private static readonly Regex BrandNameValue = new(@"export\s+const\s+BRAND_NAME\s*=\s*""([^""]+)""", RegexOptions.Compiled);

    private static string Src(string root, string rel) =>
        ProjectPaths.Src(root, rel.Replace('/', '\\'));

    public static List<FieldDef> BuildFields(string root, IReadOnlyList<LayoutComponent> layout)
    {
        var fields = new List<FieldDef>();
        AddBrandField(root, fields);

        foreach (var lc in layout)
        {
            var def = SectionCatalog.FindByComponentName(lc.Name) ?? SectionCatalog.AnyOfKind(lc.Kind);
            if (def is not null) AddDefFields(root, fields, def);
        }

        AddKindFields(root, fields, "PromotionsPage");
        AddKindFields(root, fields, "ContactPage");
        AddKindFields(root, fields, "Faq");

        return fields;
    }

    private static void AddKindFields(string root, List<FieldDef> fields, string kind)
    {
        if (SectionCatalog.AnyOfKind(kind) is { } def) AddDefFields(root, fields, def);
    }

    private static void AddDefFields(string root, List<FieldDef> fields, SectionDefinition def)
    {
        if (def.Fields.Count == 0 || string.IsNullOrEmpty(def.DataFile)) return;

        if (def.Repeatable)
        {
            var content = Io.ReadOrNull(Src(root, def.DataFile));
            var count = content is null ? 0 : TsBlockParser.CountArray(content, def.DataConst);
            for (var n = 1; n <= count; n++)
                foreach (var mf in def.Fields)
                    fields.Add(MakeField(mf, def, n));
        }
        else
        {
            foreach (var mf in def.Fields)
                fields.Add(MakeField(mf, def, 0));
        }
    }

    private static FieldDef MakeField(ManifestField mf, SectionDefinition def, int n)
    {
        var arr = n > 0;
        var id = arr ? mf.Id.Replace("{n}", n.ToString()) : mf.Id;
        var label = arr ? mf.Label.Replace("{n}", n.ToString()) : mf.Label;
        var multi = mf.Type is "body" or "richtext";
        return new FieldDef(id, label, mf.Group, def.DataFile, def.DataConst, mf.Key, multi, arr ? n - 1 : -1);
    }

    private static void AddBrandField(string root, List<FieldDef> fields)
    {
        if (Io.ReadOrNull(Src(root, SiteConfigRel)) is { } site
            && BrandNamePrefix.IsMatch(site))
            fields.Add(new("brand", "Brand Name", "BRAND", SiteConfigRel, "", "brand"));
    }

    public static Dictionary<string, string> Pull(string root, IReadOnlyList<FieldDef> fields)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var f in fields)
        {
            if (f.IsBrand)
            {
                if (ReadCached(cache, Src(root, SiteConfigRel)) is { } site
                    && BrandNameAny.Match(site) is { Success: true } bm)
                    values["brand"] = bm.Groups[1].Value.Trim();
                continue;
            }

            if (ReadCached(cache, Src(root, f.DataFile)) is not { } content) continue;

            var raw = f.IsArray
                ? ArrayItemProp(content, f.DataConst, f.ArrayIndex, f.Key)
                : ObjectProp(content, f.DataConst, f.Key);

            values[f.Id] = Unescape(raw);
        }

        return values;
    }

    public static int Save(string root, IReadOnlyList<FieldDef> fields, IReadOnlyDictionary<string, string> values)
    {
        var changed = SaveBrand(root, values.TryGetValue("brand", out var b) ? b.Trim() : "");

        foreach (var fileGroup in fields.Where(f => !f.IsBrand && f.DataFile.Length > 0)
                     .GroupBy(f => f.DataFile, StringComparer.OrdinalIgnoreCase))
        {
            var path = Src(root, fileGroup.Key);
            if (Io.ReadOrNull(path) is not { } content) continue;
            var orig = content;

            foreach (var constGroup in fileGroup.Where(f => !f.IsArray).GroupBy(f => f.DataConst))
                content = SaveObject(content, constGroup.Key, constGroup, values);

            foreach (var constGroup in fileGroup.Where(f => f.IsArray).GroupBy(f => f.DataConst))
                content = RebuildArray(content, constGroup.Key, constGroup.ToList(), values);

            if (content != orig) { Io.Write(path, content); changed++; }
        }

        return changed;
    }

    public static int SaveFaq(string root, IReadOnlyList<FieldDef> fields, IReadOnlyDictionary<string, string> values)
    {
        var path = Src(root, FaqDataRel);
        if (Io.ReadOrNull(path) is not { } content) return 0;

        var faqFields = fields.Where(f => f.DataConst == FaqConst && f.IsArray).ToList();
        var updated = RebuildArray(content, FaqConst, faqFields, values);
        if (updated == content) return 0;
        Io.Write(path, updated);
        return 1;
    }

    public static string CurrentBrandName(string root)
    {
        if (Io.ReadOrNull(Src(root, SiteConfigRel)) is not { } content) return "SITE.name";
        var m = BrandNameValue.Match(content);
        return m.Success ? m.Groups[1].Value : "SITE.name";
    }

    private static string SaveObject(string content, string constName, IEnumerable<FieldDef> group, IReadOnlyDictionary<string, string> values)
    {
        if (TsBlockParser.FirstBlock(content, $"export const {constName}") is not { } block) return content;
        var updated = block;
        foreach (var f in group)
            if (values.TryGetValue(f.Id, out var v))
                updated = WriteProp(updated, f.Key, v);
        return updated == block ? content : ReplaceFirst(content, block, updated);
    }

    private static string RebuildArray(string content, string constName, List<FieldDef> group, IReadOnlyDictionary<string, string> values)
    {
        var m = TsBlockParser.ArrayMatch(content, constName);
        if (!m.Success) return content;

        var existing = TsBlockParser.AllBlocks(m.Groups[2].Value);
        var byIndex = group.GroupBy(f => f.ArrayIndex).ToDictionary(g => g.Key, g => g.ToList());
        var count = group.Count > 0 ? group.Max(f => f.ArrayIndex) + 1 : existing.Count;

        var items = new List<string>();
        for (var i = 0; i < count; i++)
        {
            byIndex.TryGetValue(i, out var fs);
            var block = i < existing.Count ? existing[i].Trim() : DefaultBlock(fs);
            if (fs is not null)
                foreach (var f in fs)
                    if (values.TryGetValue(f.Id, out var v))
                        block = WriteProp(block, f.Key, v);
            items.Add("  " + block);
        }

        var body = items.Count == 0 ? "" : "\n" + string.Join(",\n", items) + "\n";
        return content[..m.Index] + m.Groups[1].Value + body + m.Groups[3].Value + content[(m.Index + m.Length)..];
    }

    private static string DefaultBlock(List<FieldDef>? fields) =>
        fields is null || fields.Count == 0
            ? "{}"
            : "{ " + string.Join(", ", fields.Select(f => $"{f.Key}: \"\"")) + " }";

    private static string WriteProp(string block, string key, string value)
    {
        var escaped = Escape(value);
        var rx = new Regex($@"(\b{Regex.Escape(key)}:\s*"")(?:\\.|[^""\\])*("")");
        if (rx.IsMatch(block))
            return rx.Replace(block, m => m.Groups[1].Value + escaped + m.Groups[2].Value, 1);

        var insert = $"{key}: \"{escaped}\"";
        var trimmed = block.TrimEnd();
        if (!trimmed.EndsWith('}')) return block;
        var inner = trimmed[1..^1].Trim();
        return inner.Length == 0 ? $"{{ {insert} }}" : $"{{ {inner}, {insert} }}";
    }

    private static string ObjectProp(string content, string constName, string key) =>
        TsBlockParser.FirstBlock(content, $"export const {constName}") is { } block
            ? ReadProp(block, key) : "";

    private static string ArrayItemProp(string content, string constName, int index, string key)
    {
        var m = TsBlockParser.ArrayMatch(content, constName);
        if (!m.Success) return "";
        var blocks = TsBlockParser.AllBlocks(m.Groups[2].Value);
        return index >= 0 && index < blocks.Count ? ReadProp(blocks[index], key) : "";
    }

    private static string ReadProp(string block, string key)
    {
        var m = Regex.Match(block, $@"\b{Regex.Escape(key)}:\s*""((?:\\.|[^""\\])*)""");
        return m.Success ? m.Groups[1].Value : "";
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");

    private static string Unescape(string value)
    {
        if (value.IndexOf('\\') < 0) return value;
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                var c = value[++i];
                sb.Append(c switch { 'n' => '\n', 't' => '\t', _ => c });
            }
            else sb.Append(value[i]);
        }
        return sb.ToString();
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

    private static string ReplaceFirst(string content, string find, string repl)
    {
        var idx = content.IndexOf(find, StringComparison.Ordinal);
        return idx < 0 ? content : content[..idx] + repl + content[(idx + find.Length)..];
    }

    private static string? ReadCached(Dictionary<string, string> cache, string path)
    {
        if (cache.TryGetValue(path, out var content)) return content;
        if (Io.ReadOrNull(path) is not { } loaded) return null;
        cache[path] = loaded;
        return loaded;
    }
}
