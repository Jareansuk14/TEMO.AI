namespace TEMO.AI;

internal static class ComponentValidator
{
    public static IReadOnlyList<string> Validate(IReadOnlyList<SectionDefinition> defs)
    {
        var warnings = new List<string>();
        if (defs.Count == 0) return warnings;

        var knownSlots = new HashSet<string>(ShellSlot.All, StringComparer.Ordinal) { "body" };

        foreach (var dup in defs
                     .GroupBy(d => d.ComponentName, StringComparer.Ordinal)
                     .Where(g => g.Count() > 1))
        {
            warnings.Add($"componentName ซ้ำ: \"{dup.Key}\" ({dup.Count()} ที่)");
        }

        foreach (var d in defs)
        {
            var who = $"[{d.ComponentName}]";

            if (string.IsNullOrWhiteSpace(d.Kind))
                warnings.Add($"{who} ไม่มี kind");
            if (string.IsNullOrWhiteSpace(d.Variant))
                warnings.Add($"{who} ไม่มี variant");
            if (string.IsNullOrWhiteSpace(d.DisplayName))
                warnings.Add($"{who} ไม่มี displayName");

            if (!knownSlots.Contains(d.Slot))
                warnings.Add($"{who} slot ไม่รู้จัก: \"{d.Slot}\" (รองรับ: {string.Join(", ", knownSlots)})");

            var astroPath = Path.Combine(d.StoreDirectory, d.AstroFile);
            if (string.IsNullOrWhiteSpace(d.AstroFile))
                warnings.Add($"{who} ไม่ได้ระบุ astroFile");
            else if (!File.Exists(astroPath))
                warnings.Add($"{who} ไม่พบไฟล์ astro: {d.AstroFile}");

            if (!string.IsNullOrWhiteSpace(d.CssFile)
                && !File.Exists(Path.Combine(d.StoreDirectory, d.CssFile)))
                warnings.Add($"{who} ไม่พบไฟล์ css: {d.CssFile}");

            if (d.Repeatable && (string.IsNullOrWhiteSpace(d.DataFile) || string.IsNullOrWhiteSpace(d.DataConst)))
                warnings.Add($"{who} repeatable=true แต่ขาด dataFile/dataConst");

            if (d.Fields.Count > 0 && string.IsNullOrWhiteSpace(d.DataFile))
                warnings.Add($"{who} มี fields แต่ไม่ได้ระบุ dataFile");

            foreach (var f in d.Fields)
            {
                if (string.IsNullOrWhiteSpace(f.Id))
                    warnings.Add($"{who} มี field ที่ไม่มี id");
                if (string.IsNullOrWhiteSpace(f.Key))
                    warnings.Add($"{who} field \"{f.Id}\" ไม่มี key");

                var hasPlaceholder = f.Id.Contains("{n}") || f.Label.Contains("{n}");
                if (d.Repeatable && !hasPlaceholder)
                    warnings.Add($"{who} field \"{f.Id}\" ควรมี {{n}} เพราะ repeatable=true (กัน id ชนกัน)");
                if (!d.Repeatable && hasPlaceholder)
                    warnings.Add($"{who} field \"{f.Id}\" มี {{n}} แต่ repeatable=false ({{n}} จะไม่ถูกแทนค่า)");
            }

            foreach (var img in d.Images)
            {
                if (string.IsNullOrWhiteSpace(img.Id))
                    warnings.Add($"{who} มี image ที่ไม่มี id");
                var hasSize = img.Width > 0 && img.Height > 0;
                if (!hasSize && string.IsNullOrWhiteSpace(img.Ratio))
                    warnings.Add($"{who} image \"{img.Id}\" ไม่มีทั้ง ratio และ width/height (จะใช้ขนาด default)");
            }

            if (d.Spec.HeadingCount < 0)
                warnings.Add($"{who} headingCount ต้องไม่ติดลบ");
            if (d.Spec.HeadingCount > 0 && !d.Repeatable)
                warnings.Add($"{who} headingCount > 0 แต่ repeatable=false (ไม่มี array ให้ปรับจำนวน)");

            ValidateSpec(d, who, warnings);
        }

        return warnings;
    }

    private static readonly string[] KnownImageTypes = ["normal", "transparent", "button"];

    private static void ValidateSpec(SectionDefinition d, string who, List<string> warnings)
    {
        var spec = d.Spec;
        if (!spec.HasImageGroup) return;

        if (!string.IsNullOrWhiteSpace(spec.ImageType)
            && !KnownImageTypes.Contains(spec.ImageType, StringComparer.OrdinalIgnoreCase))
            warnings.Add($"{who} imageType ไม่รู้จัก: \"{spec.ImageType}\" (รองรับ: {string.Join(", ", KnownImageTypes)})");

        if (!string.IsNullOrWhiteSpace(spec.ImageRatio) && !RatioMap.IsValid(spec.ImageRatio))
            warnings.Add($"{who} imageRatio ผิดรูปแบบ: \"{spec.ImageRatio}\" (เช่น 3:4, 1:1, 16:9, auto)");

        if (ImageGroupCatalog.All.All(g => !string.Equals(g.Key, spec.ImageGroup, StringComparison.Ordinal)))
            warnings.Add($"{who} imageGroup ไม่พบใน image-groups.json: \"{spec.ImageGroup}\"");

        if (spec.ImageCount < 0)
            warnings.Add($"{who} imageCount ต้องไม่ติดลบ");
    }
}
