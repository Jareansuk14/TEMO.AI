namespace TEMO.AI;

public partial class MainWindow
{
    private readonly Dictionary<string, List<TextBox>> _kwBoxes = [];
    private readonly Dictionary<string, StackPanel> _kwRowPanels = [];
    private readonly Dictionary<string, Button> _kwAddBtns = [];
    private readonly List<TextBox> _kwAutoBoxes = [];
    private TextBox? _brandKwListener;

    private const int MaxUserKeywords = 4;

    private static readonly (string PageId, string Label, string RelFile, string VarName)[] KeywordPages =
    [
        ("index",   "หน้าแรก",   @"pages\index.astro",      "indexKeywords"),
        ("promo",   "โปรโมชั่น", @"pages\promotions.astro",  "promoKeywords"),
        ("contact", "ติดต่อ",    @"pages\contact.astro",     "contactKeywords"),
    ];

    private void BuildKeywordsPanel()
    {
        _kwBoxes.Clear();
        _kwRowPanels.Clear();
        _kwAddBtns.Clear();
        _kwAutoBoxes.Clear();
        KeywordsPanel.Children.Clear();

        var brandName = GetCurrentBrandName();

        var innerTc = new System.Windows.Controls.TabControl
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
        };

        if (TryFindResource("InnerTabControl") is Style tcStyle)
            innerTc.Style = tcStyle;

        foreach (var (pageId, label, _, _) in KeywordPages)
        {
            _kwBoxes[pageId] = [];

            var pagePanel = new StackPanel { Margin = new Thickness(16, 14, 16, 16) };

            pagePanel.Children.Add(new TextBlock
            {
                Text = "Keyword 1 (อัตโนมัติ)",
                FontSize = 12,
                Foreground = Ui.Brush(0x777777),
                Margin = new Thickness(0, 0, 0, 5),
            });

            var autoRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            autoRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            autoRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var autoBox = new TextBox
            {
                Style = (Style)FindResource("Input"),
                Text = brandName,
                IsReadOnly = true,
                FontSize = 14,
                Foreground = Ui.Brush(0x555555),
                MinHeight = 40,
            };
            Grid.SetColumn(autoBox, 0);
            autoRow.Children.Add(autoBox);
            _kwAutoBoxes.Add(autoBox);

            var spacer = new Border { Width = 36, Margin = new Thickness(6, 0, 0, 0) };
            Grid.SetColumn(spacer, 1);
            autoRow.Children.Add(spacer);

            pagePanel.Children.Add(autoRow);

            var rowsPanel = new StackPanel();
            _kwRowPanels[pageId] = rowsPanel;
            pagePanel.Children.Add(rowsPanel);

            var addBtn = new Button
            {
                Content = "+  เพิ่ม Keyword",
                Style = (Style)FindResource("Btn"),
                Height = 34,
                FontSize = 12,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 6, 0, 0),
                Tag = pageId,
            };
            addBtn.Click += AddKeyword_Click;
            _kwAddBtns[pageId] = addBtn;
            pagePanel.Children.Add(addBtn);

            var tabItem = new TabItem
            {
                Header = label,
                Content = new ScrollViewer
                {
                    Content = pagePanel,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                },
            };

            if (TryFindResource("InnerTabItem") is Style tiStyle)
                tabItem.Style = tiStyle;

            innerTc.Items.Add(tabItem);
        }

        KeywordsPanel.Children.Add(innerTc);

        if (_brandKwListener is not null)
            _brandKwListener.TextChanged -= OnBrandTextChanged;

        if (_boxes.TryGetValue("brand", out var brandBox))
        {
            _brandKwListener = brandBox;
            brandBox.TextChanged += OnBrandTextChanged;
        }
        else _brandKwListener = null;
    }

    private void OnBrandTextChanged(object sender, TextChangedEventArgs e)
    {
        var name = ((TextBox)sender).Text.Trim();
        foreach (var box in _kwAutoBoxes)
            box.Text = name;
        if (!_suppressSaveTracking) ScheduleSaveAllUi();
    }

    private void AddKeyword_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string pageId) return;
        if (_kwBoxes[pageId].Count >= MaxUserKeywords) return;
        AddKeywordRow(pageId, "");
    }

    private void RemoveKeyword_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var (pageId, rowGrid, rowLabel) = ((string, Grid, TextBlock))btn.Tag;

        var rowsPanel = _kwRowPanels[pageId];
        var box = rowGrid.Children.OfType<TextBox>().FirstOrDefault();
        if (box != null) _kwBoxes[pageId].Remove(box);

        rowsPanel.Children.Remove(rowLabel);
        rowsPanel.Children.Remove(rowGrid);

        RenumberKeywordLabels(pageId);
        UpdateAddButton(pageId);
    }

    private void AddKeywordRow(string pageId, string value)
    {
        var rowsPanel = _kwRowPanels[pageId];
        var kwNum = _kwBoxes[pageId].Count + 2;

        var label = new TextBlock
        {
            Text = $"Keyword {kwNum}",
            FontSize = 12,
            Foreground = Ui.Brush(0x777777),
            Margin = new Thickness(0, 0, 0, 5),
        };

        var row = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var box = new TextBox
        {
            Style = (Style)FindResource("Input"),
            Text = value,
            FontSize = 14,
            MinHeight = 40,
        };
        Grid.SetColumn(box, 0);
        row.Children.Add(box);

        var removeBtn = new Button
        {
            Content = "−",
            Style = (Style)FindResource("BtnDanger"),
            Width = 36,
            Height = 36,
            FontSize = 18,
            Padding = new Thickness(0),
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Tag = (pageId, row, label),
        };
        removeBtn.Click += RemoveKeyword_Click;
        Grid.SetColumn(removeBtn, 1);
        row.Children.Add(removeBtn);

        rowsPanel.Children.Add(label);
        rowsPanel.Children.Add(row);
        _kwBoxes[pageId].Add(box);
        WireEditorTracking(box);

        RenumberKeywordLabels(pageId);
        UpdateAddButton(pageId);
    }

    private void RenumberKeywordLabels(string pageId)
    {
        var rowsPanel = _kwRowPanels[pageId];
        int kwNum = 2;
        foreach (UIElement child in rowsPanel.Children)
            if (child is TextBlock lbl)
                lbl.Text = $"Keyword {kwNum++}";
    }

    private void UpdateAddButton(string pageId)
    {
        if (_kwAddBtns.TryGetValue(pageId, out var btn))
            btn.IsEnabled = _kwBoxes[pageId].Count < MaxUserKeywords;
    }

    private void PullKeywords()
    {
        foreach (var (pageId, _, relFile, varName) in KeywordPages)
        {
            if (!_kwRowPanels.TryGetValue(pageId, out var rowsPanel)) continue;
            if (Io.ReadOrNull(SrcPath(relFile)) is not { } content) continue;

            var arrayMatch = Regex.Match(content,
                $@"const {Regex.Escape(varName)}\s*:\s*string\[\]\s*=\s*\[((?:""[^""]*""(?:,\s*)?)*)\]");
            if (!arrayMatch.Success) continue;

            var items = Regex.Matches(arrayMatch.Groups[1].Value, @"""([^""]*)""")
                             .Select(m => m.Groups[1].Value)
                             .ToList();

            _kwBoxes[pageId].Clear();
            rowsPanel.Children.Clear();

            foreach (var item in items)
                AddKeywordRow(pageId, item);
        }
    }

    private void SaveKeywords()
    {
        foreach (var (pageId, _, relFile, varName) in KeywordPages)
        {
            if (!_kwBoxes.TryGetValue(pageId, out var boxes)) continue;
            var path = SrcPath(relFile);
            if (Io.ReadOrNull(path) is not { } content) continue;

            static string Sanitise(string v) =>
                v.Trim().Replace("\"", "").Replace("\r", "").Replace("\n", " ");

            var arrayContent = boxes.Count == 0
                ? ""
                : string.Join(", ", boxes.Select(b => $"\"{Sanitise(b.Text)}\""));

            var updated = Regex.Replace(content,
                $@"const {Regex.Escape(varName)}\s*:\s*string\[\]\s*=\s*\[[^\n]*\];",
                $"const {varName}: string[] = [{arrayContent}];");

            if (updated != content) Io.Write(path, updated);
        }
    }

    private string GetCurrentBrandName() => ContentStore.CurrentBrandName(_projectPath);
}
