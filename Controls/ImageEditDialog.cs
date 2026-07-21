namespace TEMO.AI;

internal sealed class ImageEditDialog : Window
{
    public string ResultSrc { get; private set; }
    public string ResultAlt { get; private set; }

    private readonly TextBox _nameBox;
    private readonly TextBox? _altBox;
    private readonly string _projectPath;
    private readonly string _dir;
    private readonly string _ext;

    public ImageEditDialog(string label, string srcValue, string altValue, bool hasAlt, string projectPath)
    {
        _projectPath = projectPath;
        ResultSrc = srcValue;
        ResultAlt = altValue;

        var lastSlash = srcValue.LastIndexOf('/');
        _dir = lastSlash >= 0 ? srcValue[..lastSlash] : "";
        var filename = lastSlash >= 0 ? srcValue[(lastSlash + 1)..] : srcValue;
        var dotIdx = filename.LastIndexOf('.');
        var nameOnly = dotIdx >= 0 ? filename[..dotIdx] : filename;
        _ext = dotIdx >= 0 ? filename[dotIdx..] : "";

        Title = $"TEMO.AI — {label}";
        Width = 500;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        Ui.StyleDialog(this);

        var root = new StackPanel { Margin = new Thickness(22, 18, 22, 22) };

        root.Children.Add(new System.Windows.Controls.Image
        {
            MaxHeight = 190,
            Stretch = System.Windows.Media.Stretch.Uniform,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16),
            Source = Ui.LoadBitmap(PublicPath(srcValue), 456),
        });

        root.Children.Add(DlgLabel("ชื่อรูป"));

        var nameRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        nameRow.ColumnDefinitions.Add(new ColumnDefinition());
        nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _nameBox = Ui.MakeDarkInput(nameOnly);
        _nameBox.Margin = new Thickness(0);
        _nameBox.BorderThickness = new Thickness(1, 1, 0, 1);
        Grid.SetColumn(_nameBox, 0);

        var extBadge = new Border
        {
            Background = Ui.Brush(0x101010),
            BorderBrush = Ui.Brush(0x282828),
            BorderThickness = new Thickness(1, 1, 1, 1),
            Padding = new Thickness(10, 0, 10, 0),
            MinWidth = 54,
            Child = new TextBlock
            {
                Text = _ext,
                Foreground = Ui.Brush(0x686868),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            },
        };
        Grid.SetColumn(extBadge, 1);

        nameRow.Children.Add(_nameBox);
        nameRow.Children.Add(extBadge);
        root.Children.Add(nameRow);

        if (hasAlt)
        {
            root.Children.Add(DlgLabel("Alt Text"));
            _altBox = Ui.MakeDarkInput(altValue, multiline: true);
            _altBox.Margin = new Thickness(0, 0, 0, 12);
            root.Children.Add(_altBox);
        }

        var saveBtn = Ui.DialogButton("บันทึก", accent: true);
        saveBtn.Click += Save_Click;

        var cancelBtn = Ui.DialogButton("✕", accent: false);
        cancelBtn.Width = 38;
        cancelBtn.Padding = new Thickness(0);
        cancelBtn.Margin = new Thickness(6, 0, 0, 0);
        cancelBtn.Click += (_, _) => { DialogResult = false; };

        var actionRow = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0),
            Children = { saveBtn, cancelBtn },
        };

        root.Children.Add(actionRow);
        Content = root;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = SanitizeFileName(_nameBox.Text);
        ResultSrc = string.IsNullOrEmpty(name) ? ResultSrc : $"{_dir}/{name}{_ext}";
        ResultAlt = _altBox?.Text ?? "";
        DialogResult = true;
    }

    private static string SanitizeFileName(string raw)
    {
        var name = raw.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '-');
        return name.Trim('-', '.', ' ');
    }

    private string PublicPath(string src) =>
        Path.Combine(_projectPath, "public", src.TrimStart('/').Replace('/', '\\'));

    private static TextBlock DlgLabel(string text) => new()
    {
        Text = text,
        FontSize = 12,
        Foreground = Ui.Brush(0xB8B8B8),
        Margin = new Thickness(0, 0, 0, 5),
    };
}
