namespace TEMO.AI;

public partial class MainWindow
{
    private static readonly (string Key, string Label)[] AiSections =
    [
        ("HERO", "HERO"),
        ("SEO", "SEO"),
        ("CTA", "CTA"),
        ("PROMOTION-SECTION", "PROMOTION-SECTION"),
        ("PROMOTIONS-PAGE", "PROMOTIONS-PAGE"),
        ("CONTACT", "CONTACT"),
        ("FAQ", "FAQ"),
    ];

    private readonly Dictionary<string, System.Windows.Controls.CheckBox> _aiSectionChecks = new(StringComparer.Ordinal);

    private void AiToggle_Click(object sender, RoutedEventArgs e)
    {
        RebuildAiSectionList();
        ToggleFlyout(AiPanel);
    }

    private void AiClose_Click(object sender, RoutedEventArgs e) =>
        AiPanel.Visibility = Visibility.Collapsed;

    private void RebuildAiSectionList()
    {
        var prevChecked = new HashSet<string>(
            _aiSectionChecks.Where(kv => kv.Value.IsChecked == true).Select(kv => kv.Key),
            StringComparer.Ordinal);
        bool hadAny = _aiSectionChecks.Count > 0;

        AiSectionList.Children.Clear();
        _aiSectionChecks.Clear();

        foreach (var (key, label) in AiSections)
        {
            if (!_fields.Any(f => IsSectionSelected(key, f.Id))) continue;

            var chk = new System.Windows.Controls.CheckBox
            {
                Content = label,
                IsChecked = !hadAny || prevChecked.Contains(key),
                Style = (Style)FindResource("SectionCheck"),
                Margin = new Thickness(0, 0, 5, 5),
            };
            chk.Checked += (_, _) => UpdateAiGenButton();
            chk.Unchecked += (_, _) => UpdateAiGenButton();

            _aiSectionChecks[key] = chk;
            AiSectionList.Children.Add(chk);
        }

        UpdateAiGenButton();
    }

    private void UpdateAiGenButton()
    {
        AiGenBtn.IsEnabled = _aiSectionChecks.Values.Any(chk => chk.IsChecked == true);
    }

    private HashSet<string> GetAiSelectedSections()
    {
        var set = new HashSet<string> { "BRAND" };
        foreach (var (key, chk) in _aiSectionChecks)
            if (chk.IsChecked == true) set.Add(key);
        return set;
    }

    private async void AiGen_Click(object sender, RoutedEventArgs e)
    {
        var (prompt, count) = BuildPromptBody(GetAiSelectedSections());
        if (count == 0) { ShowMsg("ไม่มีข้อมูลในส่วนที่เลือก"); return; }

        await RunAiApplyAsync(AiGenBtn, ContentGptModel, prompt,
            text => LineCodec.ApplyContent(text, _boxes), AiPanel,
            applied => $"AI applied {applied} field(s) — บันทึกอัตโนมัติแล้ว!",
            "AI ตอบกลับแล้ว — ไม่พบ id= ที่ตรงกัน");
    }
}
