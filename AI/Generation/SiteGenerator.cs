namespace TEMO.AI.Ai;

internal static class SiteGenerator
{
    public sealed record Result(bool Ok, string Message, string? ProjectPath, double CostThb = 0);

    public static async Task<Result> GenerateAsync(
        GenerationOptions options, string apiKey,
        Action<string> log, CancellationToken ct = default)
    {
        var brand = options.Brand.Trim();
        options = options with { Brand = brand };
        if (string.IsNullOrWhiteSpace(brand)) return new(false, "กรุณากรอกชื่อแบรนด์", null);
        if (string.IsNullOrWhiteSpace(apiKey)) return new(false, "กรุณาตั้งค่า OpenAI API Key ใน TEMO.AI ก่อน", null);

        var templates = TemplateStore.List();
        if (templates.Count == 0) return new(false, "ยังไม่มี Template — อัปเดต Template ก่อน", null);

        var rng = new Random();
        options = options with { Style = ImageStyleCatalog.Random(rng), Render = ImageRenderCatalog.Random(rng) };
        var template = templates[rng.Next(templates.Count)];
        var dest = UniqueProjectPath(brand);
        var genLog = new GenerationLog(dest, brand);
        var usage = new UsageTracker();

        Result Done(bool ok, string msg, string? path)
        {
            genLog.Block("สรุป Token/ค่าใช้จ่าย", usage.Report());
            genLog.Finish(ok, msg);
            return new(ok, msg, path, usage.TotalThb);
        }

        genLog.Line($"Template ที่สุ่มได้: {Path.GetFileName(template)}");
        genLog.Line($"สไตล์ภาพ: {options.Style?.Name} ({options.Style?.Visual})");
        genLog.Line($"Render: {options.Render?.Name} ({options.Render?.Visual})");
        genLog.Line($"TextModel: {options.TextModel} | ImageModel: {options.ImageModel} | CssModel: {options.CssModel}");
        genLog.Line($"ContentType: {options.ContentType} | GameCardCount: {options.GameCardCount}");

        try
        {
            log("กำลังสร้างโปรเจค (คัดลอกต้นแบบ)…");
            genLog.Line("กำลังคัดลอกต้นแบบ");
            await Task.Run(() => TemplateStore.Copy(template, dest), ct);
            AstroProjectSettings.DisableDevToolbar(dest);

            log("กำลังสุ่ม Component…");
            genLog.Section("สุ่ม Layout/Component");
            var composed = await Task.Run(() => LayoutComposer.Compose(dest, rng, options.GameCardCount), ct);
            genLog.Line($"Component ที่สุ่มได้: {composed.Count} ตัว");
            foreach (var c in composed) genLog.Line($"  - {c.Kind}: {c.Name} ({c.Variant})");

            log("กำลังสุ่มธีมสี…");
            var palette = PaletteStore.Random(rng);
            genLog.Section("สุ่มธีมสี (Palette)");
            genLog.Line($"Palette: {palette.Name}");
            genLog.Line($"Primary={palette.Primary} Secondary={palette.Secondary} Accent={palette.Accent} Background={palette.Background} Surface={palette.Surface}");
            await Task.Run(() => ThemeRandomizer.Apply(dest, palette), ct);

            log("กำลังอ่านเนื้อหาเว็บ…");
            var layout = composed.Count > 0 ? composed.ToList() : ReadActiveLayout(dest);
            var fields = ContentStore.BuildFields(dest, layout);
            var values = ContentStore.Pull(dest, fields);
            values["brand"] = brand;

            var selected = fields.Select(f => f.Section).Where(s => !string.IsNullOrEmpty(s)).ToHashSet(StringComparer.Ordinal);
            var (prompt, count) = AiPromptBuilder.Build(options.ContentType, brand, fields, values, selected);
            genLog.Section("AI เนื้อหา (Content)");
            genLog.Line($"จำนวน id ที่ส่ง: {count}");
            genLog.Prompt("Content", prompt);
            if (count > 0)
            {
                log($"กำลังให้ AI สร้างเนื้อหา…");
                genLog.Line($"เรียก ChatAsync model={options.TextModel}");
                var (ok, text, _, error) = await OpenAiClient.ChatAsync(apiKey, options.TextModel, prompt, ct, usage);
                if (!ok)
                {
                    genLog.Error($"Content: {error}");
                    return Done(false, error ?? "เรียก AI ล้มเหลว", dest);
                }
                genLog.Block("Content ตอบกลับ", text);
                foreach (var (id, value) in LineCodec.ParseContent(text))
                    values[id] = value;
            }

            log("กำลังบันทึกเนื้อหา…");
            await Task.Run(() => ContentStore.Save(dest, fields, values), ct);

            var promoCount = rng.Next(ImagesStore.MinPromoCount, ImagesStore.MaxPromos + 1);
            genLog.Section("เตรียมรูป");
            genLog.Line($"จำนวนรูปโปรโมชั่นที่สุ่ม: {promoCount}");
            await Task.Run(() =>
                ImagesStore.SyncStandard(dest, src => Io.DeleteFile(ProjectPaths.Public(dest, src)), promoCount), ct);

            var (genOk, genErr, _) = await ImageCssRegenerator.RunAsync(
                dest, options, palette, apiKey, log, ct, genLog, usage);
            if (!genOk)
                return Done(false, genErr!, dest);

            log("กำลังลบรูปที่ไม่ได้ใช้…");
            var removedImages = await Task.Run(() => UnusedImageCleaner.Run(dest), ct);
            genLog.Line($"ลบรูปไม่ใช้แล้ว: {removedImages} ไฟล์");

            ProjectPaths.MarkComplete(dest);
            ProjectPaths.MarkNew(dest);

            return Done(true, $"สร้างเว็บแล้ว: {Path.GetFileName(dest)}", dest);
        }
        catch (OperationCanceledException)
        {
            genLog.Warn("ยกเลิกการทำงาน");
            return Done(false, "ยกเลิกแล้ว", null);
        }
        catch (Exception ex)
        {
            genLog.Error(ex.ToString());
            return Done(false, $"สร้างเว็บไม่สำเร็จ: {ex.Message}", dest);
        }
    }

    private static List<LayoutComponent> ReadActiveLayout(string projectPath)
    {
        SectionCatalog.Reload();
        LegacySectionRepair.Repair(projectPath);

        var byKind = new Dictionary<string, LayoutComponent>(StringComparer.OrdinalIgnoreCase);

        void Add(SectionDefinition def)
        {
            if (!byKind.ContainsKey(def.Kind))
                byKind[def.Kind] = SectionCatalog.ToLayoutComponent(def);
        }

        if (LayoutStore.ReadIndex(projectPath) is { } index)
        {
            var imports = LayoutStore.ParseSectionImportKinds(index);
            foreach (var name in LayoutStore.ParseComponentNames(index))
            {
                if (name is "BaseLayout" or "Banner") continue;

                if (SectionCatalog.FindByComponentName(name) is { } byName)
                {
                    Add(byName);
                    continue;
                }

                if (imports.TryGetValue(name, out var kind) && SectionCatalog.AnyOfKind(kind) is { } byImport)
                    Add(byImport);
            }
        }

        var dir = ProjectPaths.Src(projectPath, Path.Combine("components", "sections"));
        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.GetFiles(dir, "*.astro"))
            {
                var kind = Path.GetFileNameWithoutExtension(file);
                if (SectionCatalog.AnyOfKind(kind) is { } def)
                    Add(def);
            }
        }

        return byKind.Values.OrderBy(c => c.Kind, StringComparer.Ordinal).ToList();
    }

    private static string UniqueProjectPath(string brand)
    {
        Directory.CreateDirectory(ProjectPaths.Root);

        var baseName = Sanitize(brand);
        var candidate = baseName;
        int i = 2;
        while (Directory.Exists(Path.Combine(ProjectPaths.Root, candidate)))
            candidate = $"{baseName}-{i++}";
        return Path.Combine(ProjectPaths.Root, candidate);
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(name.Trim().Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(clean) ? "site" : clean;
    }
}
