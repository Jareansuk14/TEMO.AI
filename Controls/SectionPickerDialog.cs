namespace TEMO.AI;

internal sealed class SectionPickerDialog : Window
{
    private sealed record SectionKindOption(string Kind, string DisplayName);

    private readonly System.Windows.Controls.ListBox _list = new();
    private readonly System.Windows.Controls.ListBox _kindList = new();
    private readonly List<SectionDefinition> _sections;
    private readonly bool _chooseKindFirst;

    public SectionDefinition? SelectedSection { get; private set; }

    public SectionPickerDialog(IEnumerable<SectionDefinition> sections, string title = "เลือก Section", bool chooseKindFirst = false)
    {
        _sections = sections
            .OrderBy(x => x.Kind, StringComparer.Ordinal)
            .ThenBy(x => x.Variant, StringComparer.Ordinal)
            .ToList();
        _chooseKindFirst = chooseKindFirst;

        Title = title;
        Width = chooseKindFirst ? 720 : 460;
        Height = 560;
        ResizeMode = ResizeMode.NoResize;
        Ui.StyleDialog(this);

        var root = new Grid { Margin = new Thickness(22, 20, 22, 22) };
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        StyleList(_list);
        _list.DisplayMemberPath = nameof(SectionDefinition.DisplayName);
        _list.Margin = new Thickness(0, 0, 0, 14);
        _list.MouseDoubleClick += (_, _) => Confirm();
        _list.KeyDown += (_, e) => { if (e.Key == Key.Enter) Confirm(); };

        var content = chooseKindFirst ? BuildKindFirstView() : BuildSingleListView();
        Grid.SetRow(content, 0);
        root.Children.Add(content);

        var buttons = BuildButtons();
        Grid.SetRow(buttons, 1);
        root.Children.Add(buttons);

        Content = root;
        Loaded += (_, _) => FocusInitialList();
    }

    private UIElement BuildSingleListView()
    {
        foreach (var section in _sections)
            _list.Items.Add(section);
        return _list;
    }

    private UIElement BuildKindFirstView()
    {
        StyleList(_kindList);
        _kindList.DisplayMemberPath = nameof(SectionKindOption.DisplayName);
        _kindList.Margin = new Thickness(0, 0, 12, 14);
        _kindList.SelectionChanged += (_, _) => PopulateVariants();
        _kindList.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && _list.Items.Count > 0)
                _list.Focus();
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());

        var sectionLabel = MakeLabel("1. เลือก Section");
        var variantLabel = MakeLabel("2. เลือกรูปแบบ");
        Grid.SetColumn(sectionLabel, 0);
        Grid.SetColumn(variantLabel, 1);
        Grid.SetColumn(_kindList, 0);
        Grid.SetRow(_kindList, 1);
        Grid.SetColumn(_list, 1);
        Grid.SetRow(_list, 1);

        grid.Children.Add(sectionLabel);
        grid.Children.Add(variantLabel);
        grid.Children.Add(_kindList);
        grid.Children.Add(_list);

        PopulateKinds();
        return grid;
    }

    private void PopulateKinds()
    {
        foreach (var option in _sections
            .GroupBy(x => x.Kind, StringComparer.Ordinal)
            .Select(g => new SectionKindOption(g.Key, SectionCatalog.DisplayName(g.Key)))
            .OrderBy(x => x.DisplayName, StringComparer.Ordinal))
        {
            _kindList.Items.Add(option);
        }
    }

    private Grid BuildButtons()
    {
        var okBtn = Ui.DialogButton("ใช้ Section นี้", accent: true);
        okBtn.Click += (_, _) => Confirm();

        var cancelBtn = Ui.DialogButton("ยกเลิก", accent: false);
        cancelBtn.Click += (_, _) => DialogResult = false;

        return Ui.BuildOkCancelRow(okBtn, cancelBtn);
    }

    private void FocusInitialList()
    {
        var list = _chooseKindFirst ? _kindList : _list;
        if (list.Items.Count == 0) return;
        list.SelectedIndex = 0;
        list.Focus();
    }

    private static void StyleList(System.Windows.Controls.ListBox list)
    {
        list.Background = Ui.Brush(0x111111);
        list.BorderBrush = Ui.Brush(0x282828);
        list.BorderThickness = new Thickness(1);
        list.Foreground = Ui.Brush(0xDEDEDE);
    }

    private static TextBlock MakeLabel(string text) => new()
    {
        Text = text,
        Foreground = Ui.Brush(0xB8B8B8),
        FontSize = 13,
        Margin = new Thickness(0, 0, 0, 8),
    };

    private void PopulateVariants()
    {
        _list.Items.Clear();
        if (_kindList.SelectedItem is not SectionKindOption option) return;

        foreach (var section in _sections.Where(x => x.Kind.Equals(option.Kind, StringComparison.Ordinal)))
            _list.Items.Add(section);

        if (_list.Items.Count > 0)
            _list.SelectedIndex = 0;
    }

    private void Confirm()
    {
        if (_list.SelectedItem is not SectionDefinition section) return;
        SelectedSection = section;
        DialogResult = true;
    }
}
