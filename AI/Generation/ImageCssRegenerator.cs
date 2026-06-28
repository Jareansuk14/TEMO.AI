namespace TEMO.AI.Ai;

internal static class ImageCssRegenerator
{
    public static async Task<(bool Ok, string? Error, int ImageCount)> RunAsync(
        string projectPath,
        GenerationOptions options,
        ThemePalette palette,
        string apiKey,
        Action<string> log,
        CancellationToken ct = default,
        GenerationLog? genLog = null,
        UsageTracker? usage = null)
    {
        var plan = ImagePlanBuilder.Build(projectPath);
        genLog?.Line($"แผนรูปทั้งหมด: {plan.Count}");
        foreach (var p in plan) genLog?.Line($"  - {p.Id} ({p.Width}x{p.Height}) {p.Label}");

        if (plan.Count > 0)
        {
            log($"กำลังสร้างรูปด้วย AI ({plan.Count} รูป)…");
            var (ok, error) = await AiImageGenerator.GenerateAsync(
                projectPath, plan, options, palette, apiKey, log, ct, genLog, usage);
            if (!ok)
            {
                genLog?.Error($"Image: {error}");
                return (false, error ?? "สร้างรูปไม่สำเร็จ", plan.Count);
            }
        }

        var (cssOk, cssError) = await AiCssGenerator.ApplyAsync(
            projectPath, options, palette, apiKey, log, ct, genLog, usage);
        if (!cssOk)
        {
            genLog?.Error($"CSS: {cssError}");
            return (false, cssError ?? "สร้าง CSS ไม่สำเร็จ", plan.Count);
        }

        return (true, null, plan.Count);
    }
}
