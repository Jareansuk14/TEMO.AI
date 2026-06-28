namespace TEMO.AI;

public partial class MainWindow
{
    private void BuildFields() => _vm.Content.BuildFields();

    private void ContentAiToggle_Click(object sender, RoutedEventArgs e) =>
        ToggleFlyout(ContentAiPanel);

    private void ContentAiClose_Click(object sender, RoutedEventArgs e) =>
        ContentAiPanel.Visibility = Visibility.Collapsed;

    private async void ContentAiGen_Click(object sender, RoutedEventArgs e)
    {
        if (!HasOpenProject()) { ShowMsg("⚠️  เปิดโปรเจคก่อน"); return; }
        if (!TryGetApiKey(out var apiKey)) return;

        var brand = ContentStore.CurrentBrandName(_projectPath);
        if (string.IsNullOrWhiteSpace(brand)) { ShowMsg("⚠️  ไม่พบชื่อแบรนด์"); return; }

        var type = ContentTypes.FromRadios(ContentAiLottery.IsChecked == true, ContentAiSlot.IsChecked == true);
        const string model = AiModels.TextDefault;

        var values = CaptureAllBoxValues();
        var selected = _fields.Select(f => f.Section)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet(StringComparer.Ordinal);
        var (prompt, count) = AiPromptBuilder.Build(type, brand, _fields, values, selected);
        if (count == 0) { ShowMsg("ไม่มี field ให้สร้าง"); return; }

        ContentAiGenBtn.IsEnabled = false;
        try
        {
            var text = await RequestOpenAiAsync(model, prompt);
            if (text is null) return;

            var applied = 0;
            foreach (var (id, value) in LineCodec.ParseContent(text))
                if (_boxes.TryGetValue(id, out var box)) { box.Text = value; applied++; }

            if (applied == 0)
            {
                ShowAiOverlayError(text);
                ShowMsg("AI ตอบกลับไม่ตรงรูปแบบ");
                return;
            }

            HideAiOverlay();
            ContentAiPanel.Visibility = Visibility.Collapsed;
            SaveAll_Click(null!, null!);
            ShowMsg($"🤖  อัปเดตเนื้อหา {applied} field");
        }
        finally
        {
            ContentAiGenBtn.IsEnabled = true;
        }
    }

    private void RebuildContentForLayout(string? refreshKind = null)
    {
        if (!HasOpenProject()) return;

        var snapshot = CaptureAllBoxValues();
        var prevSuppress = _suppressSaveTracking;
        _suppressSaveTracking = true;
        try
        {
            BuildFields();
            BuildContentPanel();
            PullAllToBoxes();

            var label = SectionCatalog.ContentLabel(refreshKind);
            if (label is null)
            {
                RestoreBoxValues(snapshot);
            }
            else
            {
                var freshIds = _fields
                    .Where(f => string.Equals(f.Section, label, StringComparison.Ordinal))
                    .Select(f => f.Id)
                    .ToHashSet(StringComparer.Ordinal);

                RestoreBoxValues(snapshot
                    .Where(kv => !freshIds.Contains(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal));
            }
        }
        finally
        {
            _suppressSaveTracking = prevSuppress;
        }
    }

    private void SwapVariantPreservingText(SectionDefinition selected) =>
        ComponentStore.CopyToProject(_projectPath, selected);

    private void BuildContentPanel()
    {
        _boxes.Clear();
        ContentPanel.Children.Clear();
        string? lastSection = null;

        var faqIds = _fields.Where(f => f.Id.StartsWith("faq-")).Select(f => f.Id).ToHashSet();

        foreach (var f in _fields.Where(f => !faqIds.Contains(f.Id)))
        {
            if (f.Section != lastSection)
            {
                if (lastSection != null) ContentPanel.Children.Add(Ui.Divider());
                ContentPanel.Children.Add(Ui.SectionHeader(f.Section));
                lastSection = f.Section;
            }
            ContentPanel.Children.Add(Ui.FieldLabel(f.Label));
            var box = new TextBox
            {
                Style = (Style)FindResource("Input"),
                FontSize = 14,
                TextWrapping = f.Multi ? TextWrapping.Wrap : TextWrapping.NoWrap,
                AcceptsReturn = f.Multi,
                Height = f.Multi ? 100 : double.NaN,
                MinHeight = f.Multi ? 100 : 40,
                VerticalScrollBarVisibility = f.Multi ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
                HorizontalScrollBarVisibility = f.Multi ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Hidden,
                Margin = new Thickness(0, 0, 0, 12),
            };
            _boxes[f.Id] = box;
            WireEditorTracking(box);
            ContentPanel.Children.Add(box);
        }

        var faqNums = _fields.Where(f => f.Id.StartsWith("faq-q-"))
                             .Select(f => int.Parse(f.Id["faq-q-".Length..]))
                             .OrderBy(n => n).ToList();

        if (faqNums.Count > 0)
        {
            if (lastSection != null) ContentPanel.Children.Add(Ui.Divider());

            var headerRow = new Grid { Margin = new Thickness(0, 8, 0, 10) };
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var faqHeader = Ui.SectionHeader("FAQ");
            faqHeader.Margin = new Thickness(0);
            Grid.SetColumn(faqHeader, 0);
            headerRow.Children.Add(faqHeader);

            var addBtn = new Button
            {
                Content = "+ เพิ่มคำถาม",
                Style = (Style)FindResource("Btn"),
                Height = 26,
                Padding = new Thickness(10, 0, 10, 0),
                FontSize = 11,
                IsEnabled = faqNums.Count < 6,
                ToolTip = "เพิ่มคำถาม-คำตอบ (สูงสุด 6)",
            };
            addBtn.Click += (_, _) => AddFaq_Click();
            Grid.SetColumn(addBtn, 1);
            headerRow.Children.Add(addBtn);

            ContentPanel.Children.Add(headerRow);

            foreach (var n in faqNums)
                ContentPanel.Children.Add(MakeFaqGroup(n, faqNums.Count));
        }

        RewireBrandKeywordListener();
    }

    private Border MakeFaqGroup(int n, int totalCount)
    {
        var qBox = new TextBox
        {
            Style = (Style)FindResource("Input"),
            FontSize = 14,
            TextWrapping = TextWrapping.NoWrap,
            Height = double.NaN,
            MinHeight = 40,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            Margin = new Thickness(0, 0, 0, 8),
        };
        _boxes[$"faq-q-{n}"] = qBox;
        WireEditorTracking(qBox);

        var aBox = new TextBox
        {
            Style = (Style)FindResource("Input"),
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Height = 100,
            MinHeight = 100,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 0, 0, 0),
        };
        _boxes[$"faq-a-{n}"] = aBox;
        WireEditorTracking(aBox);

        var deleteBtn = new Button
        {
            Content = Ui.MakeMinusIcon(16, Ui.Brush(0xFF6666)),
            Style = (Style)FindResource("BtnDanger"),
            Width = 28,
            Height = 24,
            Padding = new Thickness(0),
            IsEnabled = totalCount > 2,
            ToolTip = "ลบคำถามนี้",
        };
        deleteBtn.Click += (_, _) => DeleteFaq_Click(n);

        var numRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        numRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        numRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var numLabel = new TextBlock
        {
            Text = $"คำถาม {n}",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ui.Brush(0x777777),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(numLabel, 0);
        numRow.Children.Add(numLabel);
        Grid.SetColumn(deleteBtn, 1);
        numRow.Children.Add(deleteBtn);

        var inner = new StackPanel { Margin = new Thickness(10, 8, 10, 10) };
        inner.Children.Add(numRow);
        inner.Children.Add(Ui.FieldLabel("คำถาม"));
        inner.Children.Add(qBox);
        inner.Children.Add(Ui.FieldLabel("คำตอบ"));
        inner.Children.Add(aBox);

        return new Border
        {
            Background = Ui.Brush(0x141414),
            BorderBrush = Ui.Brush(0x222222),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 0, 0, 10),
            Child = inner,
        };
    }

    private void AddFaq_Click()
    {
        var faqNums = _fields.Where(f => f.Id.StartsWith("faq-q-"))
                             .Select(f => int.Parse(f.Id["faq-q-".Length..]))
                             .OrderBy(n => n).ToList();
        if (faqNums.Count >= 6) return;

        PushUndoHistory();

        var snapshot = CaptureAllBoxValues();
        var faqValues = CaptureFaqValues();
        int newN = faqNums.Count > 0 ? faqNums.Max() + 1 : 1;

        _fields.RemoveAll(f => f.Id.StartsWith("faq-"));
        foreach (var (num, _, _) in faqValues)
            AddFaqFields(num);
        AddFaqFields(newN);

        BuildContentPanel();
        RestoreBoxValues(snapshot);

        SaveFaqBlock();
        TakeSavedSnapshot();
        UpdateUndoButtons();
        ShowMsg($"✅ เพิ่มคำถาม {newN} แล้ว ({faqValues.Count + 1}/6)");
    }

    private void DeleteFaq_Click(int n)
    {
        var faqNums = _fields.Where(f => f.Id.StartsWith("faq-q-"))
                             .Select(f => int.Parse(f.Id["faq-q-".Length..]))
                             .OrderBy(x => x).ToList();
        if (faqNums.Count <= 2) return;

        PushUndoHistory();

        var snapshot = CaptureAllBoxValues();
        var keepValues = CaptureFaqValues().Where(x => x.Num != n).OrderBy(x => x.Num).ToList();

        _fields.RemoveAll(f => f.Id.StartsWith("faq-"));
        for (int i = 0; i < keepValues.Count; i++)
            AddFaqFields(i + 1);

        BuildContentPanel();
        RestoreBoxValues(snapshot);

        for (int i = 0; i < keepValues.Count; i++)
        {
            int seq = i + 1;
            if (_boxes.TryGetValue($"faq-q-{seq}", out var qb)) qb.Text = keepValues[i].Q;
            if (_boxes.TryGetValue($"faq-a-{seq}", out var ab)) ab.Text = keepValues[i].A;
        }

        SaveFaqBlock();
        TakeSavedSnapshot();
        UpdateUndoButtons();
        ShowMsg($"🗑 ลบคำถาม {n} แล้ว ({keepValues.Count}/6)");
    }

    private void AddFaqFields(int n)
    {
        _fields.Add(new($"faq-q-{n}", $"Q{n}", "FAQ", ContentStore.FaqDataRel, ContentStore.FaqConst, "q", false, n - 1));
        _fields.Add(new($"faq-a-{n}", $"A{n}", "FAQ", ContentStore.FaqDataRel, ContentStore.FaqConst, "a", true, n - 1));
    }

    private List<(int Num, string Q, string A)> CaptureFaqValues() =>
        _fields.Where(f => f.Id.StartsWith("faq-q-"))
               .Select(f => int.Parse(f.Id["faq-q-".Length..]))
               .OrderBy(n => n)
               .Select(n =>
               {
                   var q = _boxes.TryGetValue($"faq-q-{n}", out var qb) ? qb.Text : "";
                   var a = _boxes.TryGetValue($"faq-a-{n}", out var ab) ? ab.Text : "";
                   return (n, q, a);
               })
               .ToList();

    private Dictionary<string, string> CaptureAllBoxValues() =>
        _boxes.ToDictionary(kv => kv.Key, kv => kv.Value.Text);

    private void RestoreBoxValues(Dictionary<string, string> snapshot)
    {
        foreach (var (id, text) in snapshot)
            if (_boxes.TryGetValue(id, out var box))
                box.Text = text;
    }

    private void PullAllToBoxes()
    {
        foreach (var (id, value) in ContentStore.Pull(_projectPath, _fields))
            if (_boxes.TryGetValue(id, out var box)) box.Text = value;
    }

    private void SaveAll_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetValidatedSiteUrl(out var siteUrl)) return;

        if (!_isSavingFromUndo)
            PushUndoHistory();

        using var session = Io.Session();
        var changed = ContentStore.Save(_projectPath, _fields, CaptureAllBoxValues());
        SaveLayoutToFile();
        SaveCssVariables();
        SaveSiteSettings(siteUrl);
        SaveKeywords();

        ShowMsg($"Saved · {changed} file(s)");

        _suppressSaveTracking = true;
        try
        {
            PullAllToBoxes();
            LoadLayoutComponents();
            PullSiteSettings();
            PullKeywords();
            TakeSavedSnapshot();
        }
        finally
        {
            _suppressSaveTracking = false;
            UpdateUndoButtons();
        }
    }

    private int SaveFaqBlock() => ContentStore.SaveFaq(_projectPath, _fields, CaptureAllBoxValues());
}
