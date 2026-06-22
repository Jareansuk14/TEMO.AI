namespace TEMO.AI;

internal sealed class ProjectGalleryDialog : Window
{
    public string? SelectedProject { get; private set; }

    private List<string> _projects;
    private readonly WrapPanel _grid;
    private readonly TextBlock _statusText;

    public ProjectGalleryDialog()
    {
        _projects = ProjectPaths.List();

        Title = "โปรเจคทั้งหมด";
        Width = 1000;
        Height = 720;
        Ui.StyleDialog(this);

        var root = new DockPanel();

        var bar = new Border
        {
            Background = Ui.Brush(0x0D0D0D),
            BorderBrush = Ui.Brush(0x1E1E1E),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(18, 14, 16, 14),
            Child = new TextBlock
            {
                Text = "โปรเจคทั้งหมด",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Ui.Brush(0xFFFFFF),
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        DockPanel.SetDock(bar, Dock.Top);
        root.Children.Add(bar);

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

    private void RebuildGrid()
    {
        _grid.Children.Clear();
        _statusText.Text = _projects.Count == 0 ? "" : $"{_projects.Count} โปรเจค";

        if (_projects.Count == 0)
        {
            _grid.Children.Add(new TextBlock
            {
                Text = "ยังไม่มีโปรเจค — กด New Project เพื่อสร้างโปรเจคใหม่",
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
        const int CardW = 220;
        const int ThumbH = 175;

        var name = new DirectoryInfo(path.TrimEnd('\\', '/')).Name;
        DateTime modified;
        try { modified = Directory.GetLastWriteTime(path); } catch { modified = DateTime.MinValue; }

        var iconHost = new Border
        {
            Height = ThumbH,
            Background = Ui.Brush(0x070707),
            CornerRadius = new CornerRadius(4, 4, 0, 0),
            Cursor = Cursors.Hand,
            Child = MakeFolderIcon(),
        };
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

        var body = new StackPanel();
        body.Children.Add(iconHost);
        body.Children.Add(info);

        return new Border
        {
            Width = CardW,
            Background = Ui.Brush(0x141414),
            BorderBrush = Ui.Brush(0x282828),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Margin = new Thickness(0, 0, 12, 12),
            Child = body,
        };
    }

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
            DeleteDirectory(path);
            _projects = ProjectPaths.List();
            RebuildGrid();
            _statusText.Text = $"ลบโปรเจค \"{name}\" แล้ว";
        }
        catch (Exception ex)
        {
            _statusText.Text = $"ลบโปรเจคไม่สำเร็จ: {ex.Message}";
        }
    }

    private static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path)) return;

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);

        Directory.Delete(path, recursive: true);
    }

    private void Select(string path)
    {
        SelectedProject = path;
        DialogResult = true;
    }
}
