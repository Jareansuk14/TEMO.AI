namespace TEMO.AI;

public partial class MainWindow
{
    private const string CssSystemPrompt =
        "คุณคือ CSS designer มืออาชีพที่เชี่ยวชาญด้านเว็บไซต์คาสิโนออนไลน์\n" +
        "ต่อไปนี้คือ CSS custom properties (:root variables) ของเว็บไซต์ แต่ละ section มี comment อธิบายไว้แล้ว\n\n" +
        "กฎการตอบกลับ (ห้ามละเมิดเด็ดขาด):\n" +
        "1. ตอบเฉพาะ CSS variable ที่ต้องการเปลี่ยนเท่านั้น\n" +
        "2. รูปแบบ: --ชื่อ-variable: ค่าใหม่;\n" +
        "3. 1 บรรทัดต่อ 1 variable ห้ามมีข้อความอื่น comment หรือ section header ใดๆ\n" +
        "4. ถ้า value มีหลายบรรทัด (เช่น gradient หรือ shadow) ให้เขียนบรรทัดเดียวทั้งหมด\n\n" +
        "5. --btn-primary-bg: ห้ามใช้ gradient \n" +
        "ตัวอย่างรูปแบบที่ถูกต้อง:\n" +
        "--color-primary: #FF5733;";

    private const string CssFixedPrompt = "แก้ไข css ให้เป็นโทนเดียวกับรูปที่แนบไป";

    private void CssExport_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ReadThemeCss())) { ShowMsg("ไม่พบ theme.css"); return; }

        Clipboard.SetText(BuildCssBlock());
        ShowMsg($"CSS copied ({_cssBoxes.Count} variables)");
        CollapseCssFlyouts();
    }

    private void CssImportToggle_Click(object sender, RoutedEventArgs e)
    {
        if (ToggleFlyout(CssImportPanel))
        {
            CssAiPanel.Visibility = Visibility.Collapsed;
            CssImportBox.Focus();
        }
    }

    private void CssAiToggle_Click(object sender, RoutedEventArgs e)
    {
        if (ToggleFlyout(CssAiPanel))
            CssImportPanel.Visibility = Visibility.Collapsed;
    }

    private void CssAiClose_Click(object sender, RoutedEventArgs e) =>
        CssAiPanel.Visibility = Visibility.Collapsed;

    private void CssImportClose_Click(object sender, RoutedEventArgs e)
    {
        CssImportPanel.Visibility = Visibility.Collapsed;
        CssImportBox.Clear();
    }

    private string ReadThemeCss() => CssStore.Read(_projectPath);

    private string BuildCssBlock()
    {
        var sb = new StringBuilder();
        sb.AppendLine(CssSystemPrompt);
        sb.AppendLine(ReadThemeCss());
        return sb.ToString();
    }

    private void CssImportApply_Click(object sender, RoutedEventArgs e)
    {
        var text = CssImportBox.Text;
        if (string.IsNullOrWhiteSpace(text)) return;

        var applied = LineCodec.ApplyCss(text, _cssBoxes);
        ShowMsg(applied > 0 ? $"📥  Applied {applied} CSS variable(s)" : "ไม่พบ CSS variable ที่ตรงกัน");
        if (applied > 0)
        {
            CssImportPanel.Visibility = Visibility.Collapsed;
            CssImportBox.Clear();
        }
    }

    private (string base64, string mime)? GetBannerImageData()
    {
        var banner = _imageEntries.FirstOrDefault(en => en.Id == "banner");
        if (banner is null || string.IsNullOrEmpty(banner.SrcValue)) return null;

        var filePath = PublicPath(banner.SrcValue);
        if (!File.Exists(filePath)) return null;

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var mime = ext switch
        {
            ".png"  => "image/png",
            ".gif"  => "image/gif",
            ".webp" => "image/webp",
            _       => "image/jpeg",
        };

        var base64 = Convert.ToBase64String(File.ReadAllBytes(filePath));
        return (base64, mime);
    }

    private async void CssAiGen_Click(object sender, RoutedEventArgs e)
    {
        if (_cssBoxes.Count == 0) { ShowMsg("ยังไม่มี CSS variables"); return; }

        var imageData = GetBannerImageData();
        if (imageData is null)
        {
            ShowMsg("ไม่พบรูป Banner — กรุณาตั้งค่ารูป Banner ใน tab IMG ก่อน");
            return;
        }

        var (base64, mime) = imageData.Value;
        var promptText = BuildCssBlock() + "\n" + CssFixedPrompt;

        object messageContent = new object[]
        {
            new { type = "text",      text = promptText },
            new { type = "image_url", image_url = new { url = $"data:{mime};base64,{base64}" } }
        };

        var brand = ContentStore.CurrentBrandName(_projectPath);
        var genLog = new GenerationLog(_projectPath, brand, $"TAB CSS — ปรับ CSS แบรนด์: {brand}", "css");
        genLog.Line($"CssModel: {CssGptModel} | variables: {_cssBoxes.Count}");
        genLog.Prompt("CSS", promptText);

        await RunAiApplyAsync(CssAiGenBtn, CssGptModel, messageContent,
            text => LineCodec.ApplyCss(text, _cssBoxes), CssAiPanel,
            applied => $"🤖  CSS applied {applied} variable(s) — บันทึกอัตโนมัติแล้ว!",
            "AI ตอบกลับแล้ว — ไม่พบ CSS variable ที่ตรงกัน", genLog);
    }
}
