namespace TEMO.AI.Ai;

internal static class AiCssGenerator
{
    private const string Prompt =
        "คุณคือ CSS designer มืออาชีพสำหรับเว็บ iGaming ภาษาไทย\n" +
        "ตอบเฉพาะ CSS custom properties ที่ต้องการเปลี่ยนเท่านั้น\n" +
        "รูปแบบ: --ชื่อ-variable: ค่าใหม่;\n" +
        "1 บรรทัดต่อ 1 variable ห้ามมีคำอธิบาย ห้ามมี markdown ห้ามมี comment\n" +
        "--btn-primary-bg ห้ามใช้ gradient\n";

    public static async Task<(bool Ok, string? Error)> ApplyAsync(
        string projectPath,
        GenerationOptions options,
        ThemePalette palette,
        string apiKey,
        Action<string> log,
        CancellationToken ct = default,
        GenerationLog? genLog = null,
        UsageTracker? usage = null)
    {
        var banner = ImagesStore.ReadValues(ImagesStore.ReadConfig(projectPath), "banner").Src;
        var bannerPath = ProjectPaths.Public(projectPath, banner);
        if (!File.Exists(bannerPath))
        {
            log("ข้าม CSS: ไม่พบรูป banner");
            genLog?.Warn("CSS: ข้ามเพราะไม่พบรูป banner");
            return (true, null);
        }

        log("กำลังให้ AI ปรับ CSS");
        genLog?.Section("AI CSS");
        genLog?.Line($"model={options.CssModel} banner={banner}");

        var themeCss = CssStore.Read(projectPath);
        var bytes = await File.ReadAllBytesAsync(bannerPath, ct);
        var mime = MimeOf(bannerPath);
        var text =
            $"{Prompt}\n" +
            $"Brand: {options.Brand}\n" +
            $"Palette: {palette.Name} {palette.Primary} {palette.Accent} {palette.Background}\n" +
            "ปรับ CSS variables ด้านล่างให้เข้ากับรูป banner ที่แนบและคุมโทนเดียวกับ palette\n\n" +
            themeCss;
        genLog?.Prompt("CSS", text);

        object messageContent = new object[]
        {
            new { type = "text", text },
            new { type = "image_url", image_url = new { url = $"data:{mime};base64,{Convert.ToBase64String(bytes)}" } }
        };

        var (ok, response, _, error) = await OpenAiClient.ChatAsync(apiKey, options.CssModel, messageContent, ct, usage);
        if (!ok)
        {
            genLog?.Error($"CSS: {error}");
            return (false, error);
        }
        genLog?.Block("CSS ตอบกลับ", response);

        var updates = LineCodec.ParseCss(response)
            .ToDictionary(x => x.Name, x => x.Value, StringComparer.Ordinal);
        if (updates.Count > 0)
        {
            CssStore.Save(projectPath, updates);
            genLog?.Line($"ปรับ CSS variables แล้ว {updates.Count} ตัว");
        }
        else
        {
            log("ข้าม CSS: AI ไม่ได้ส่งค่า CSS ที่ใช้ได้");
            genLog?.Warn("CSS: AI ไม่ได้ส่งค่า CSS ที่ใช้ได้");
        }
        return (true, null);
    }

    private static string MimeOf(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "image/jpeg",
    };
}
