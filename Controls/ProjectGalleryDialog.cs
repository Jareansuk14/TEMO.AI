namespace TEMO.AI;

internal sealed class ProjectGalleryDialog : Window
{
    public string? SelectedProject { get; private set; }

    private List<string> _allProjects;
    private List<string> _projects;
    private readonly WrapPanel _grid;
    private readonly TextBlock _statusText;
    private readonly TextBox _search;
    private readonly Border _searchHost;

    public ProjectGalleryDialog()
    {
        _allProjects = ProjectPaths.List();
        _projects = _allProjects;

        Title = "โปรเจคทั้งหมด";
        Width = 1000;
        Height = 720;
        Ui.StyleDialog(this);

        var root = new DockPanel();

        var barGrid = new Grid();
        barGrid.ColumnDefinitions.Add(new ColumnDefinition());
        barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var barTitle = new TextBlock
        {
            Text = "โปรเจคทั้งหมด",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0xFFFFFF),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(barTitle, 0);
        barGrid.Children.Add(barTitle);

        _search = Ui.MakeDarkInput();
        _search.Background = Brushes.Transparent;
        _search.BorderThickness = new Thickness(0);
        _search.MinHeight = 0;
        _search.Padding = new Thickness(0);
        _search.VerticalAlignment = VerticalAlignment.Center;
        _search.TextChanged += (_, _) => ApplyFilter();
        _search.KeyDown += (_, e) => { if (e.Key == Key.Escape) CloseSearch(); };
        Grid.SetColumn(_search, 1);

        var searchInner = new Grid();
        searchInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        searchInner.ColumnDefinitions.Add(new ColumnDefinition());
        var magnifier = MakeSearchIcon();
        Grid.SetColumn(magnifier, 0);
        searchInner.Children.Add(magnifier);
        searchInner.Children.Add(_search);

        _searchHost = new Border
        {
            Width = 280,
            Background = Ui.Brush(0x161616),
            BorderBrush = Ui.Brush(0x303030),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 6, 10, 6),
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed,
            ToolTip = "ค้นหาชื่อโฟลเดอร์ (Ctrl+F)",
            Child = searchInner,
        };
        Grid.SetColumn(_searchHost, 1);
        barGrid.Children.Add(_searchHost);

        var bar = new Border
        {
            Background = Ui.Brush(0x0D0D0D),
            BorderBrush = Ui.Brush(0x1E1E1E),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(18, 14, 16, 14),
            Child = barGrid,
        };
        DockPanel.SetDock(bar, Dock.Top);
        root.Children.Add(bar);

        PreviewKeyDown += OnPreviewKeyDown;

        var content = new DockPanel();

        _statusText = new TextBlock
        {
            Foreground = Ui.Brush(0x888888),
            FontSize = 12,
            Margin = new Thickness(18, 12, 18, 0),
        };
        DockPanel.SetDock(_statusText, Dock.Top);
        content.Children.Add(_statusText);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(18, 16, 18, 16),
        };
        _grid = new WrapPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        scroll.Content = _grid;
        content.Children.Add(scroll);
        root.Children.Add(content);

        Content = root;
        RebuildGrid();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (_searchHost.Visibility == Visibility.Visible) CloseSearch();
            else OpenSearch();
            e.Handled = true;
        }
    }

    private void OpenSearch()
    {
        _searchHost.Visibility = Visibility.Visible;
        _search.Focus();
        _search.SelectAll();
    }

    private void CloseSearch()
    {
        _search.Text = "";
        _searchHost.Visibility = Visibility.Collapsed;
    }

    private static FrameworkElement MakeSearchIcon() => new Viewbox
    {
        Width = 16,
        Height = 16,
        Stretch = Stretch.Uniform,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 8, 0),
        Child = new Shapes.Path
        {
            Data = Geometry.Parse("M15.5 14h-.79l-.28-.27a6.5 6.5 0 1 0-.7.7l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0A4.5 4.5 0 1 1 14 9.5 4.5 4.5 0 0 1 9.5 14z"),
            Fill = Ui.Brush(0x888888),
        },
    };

    private void ApplyFilter()
    {
        var query = _search.Text.Trim();
        _projects = string.IsNullOrEmpty(query)
            ? _allProjects
            : _allProjects
                .Where(p => new DirectoryInfo(p.TrimEnd('\\', '/')).Name
                    .Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        RebuildGrid();
    }

    private void RebuildGrid()
    {
        _grid.Children.Clear();

        var hasQuery = _search.Text.Trim().Length > 0;
        _statusText.Text = hasQuery
            ? $"พบ {_projects.Count} จาก {_allProjects.Count} โปรเจค"
            : _projects.Count == 0 ? "" : $"{_projects.Count} โปรเจค";

        if (_projects.Count == 0)
        {
            _grid.Children.Add(new TextBlock
            {
                Text = hasQuery
                    ? "ไม่พบโปรเจคที่ตรงกับคำค้นหา"
                    : "ยังไม่มีโปรเจค — กด New Project เพื่อสร้างโปรเจคใหม่",
                Foreground = Ui.Brush(0x555555),
                FontSize = 13,
                Margin = new Thickness(4, 20, 0, 0),
            });
            return;
        }

        foreach (var path in _projects)
            _grid.Children.Add(MakeCard(path));
    }

    private Border MakeCard(string path)
    {
        var name = new DirectoryInfo(path.TrimEnd('\\', '/')).Name;
        DateTime modified;
        try { modified = Directory.GetLastWriteTime(path); } catch { modified = DateTime.MinValue; }

        var thumb = new Grid();
        thumb.Children.Add(MakeFolderIcon());
        if (ProjectPaths.IsNew(path))
            thumb.Children.Add(MakeNewBadge());

        var iconHost = Ui.MakeGalleryThumbHost(thumb);
        iconHost.MouseDown += (_, _) => Select(path);

        var nameText = new TextBlock
        {
            Text = name,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ui.Brush(0xEDEDED),
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 0, 2),
        };

        var dateText = new TextBlock
        {
            Text = modified == DateTime.MinValue ? "" : modified.ToString("d MMM yyyy HH:mm"),
            FontSize = 11,
            Foreground = Ui.Brush(0x888888),
            Margin = new Thickness(0, 0, 0, 8),
        };

        var openBtn = Ui.DialogButton("เลือกโปรเจค", accent: true);
        openBtn.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        openBtn.Click += (_, _) => Select(path);
        Grid.SetColumn(openBtn, 0);

        var deleteBtn = Ui.DialogDangerButton("ลบ");
        deleteBtn.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        deleteBtn.Margin = new Thickness(6, 0, 0, 0);
        deleteBtn.Click += (_, _) => DeleteProject(path, name);
        Grid.SetColumn(deleteBtn, 1);

        var btnRow = new Grid();
        btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
        btnRow.Children.Add(openBtn);
        btnRow.Children.Add(deleteBtn);

        var info = new StackPanel { Margin = new Thickness(10, 8, 10, 10) };
        info.Children.Add(nameText);
        info.Children.Add(dateText);
        info.Children.Add(btnRow);

        return Ui.MakeGalleryCard(iconHost, info);
    }

    private static Border MakeNewBadge() => new()
    {
        Background = Ui.Brush(0xE23B3B),
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(8, 2, 8, 2),
        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Top,
        Margin = new Thickness(0, 10, 10, 0),
        Child = new TextBlock
        {
            Text = "NEW",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0xFFFFFF),
        },
    };

    private static Viewbox MakeFolderIcon() => new()
    {
        Width = 76,
        Height = 76,
        Stretch = Stretch.Uniform,
        Child = new Shapes.Path
        {
            Data = Geometry.Parse("M10 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"),
            Fill = Ui.Brush(0xDCB67A),
        },
    };

    private void DeleteProject(string path, string name)
    {
        if (System.Windows.MessageBox.Show(this,
                $"ต้องการลบโปรเจค \"{name}\" ออกจากเครื่องถาวรหรือไม่?",
                "ลบโปรเจค", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            Io.DeleteDirectory(path, ignoreErrors: false);
            _allProjects = ProjectPaths.List();
            ApplyFilter();
            _statusText.Text = $"ลบโปรเจค \"{name}\" แล้ว";
        }
        catch (Exception ex)
        {
            _statusText.Text = $"ลบโปรเจคไม่สำเร็จ: {ex.Message}";
        }
    }

    private void Select(string path)
    {
        ProjectPaths.ClearNew(path);
        SelectedProject = path;
        DialogResult = true;
    }
}
