namespace TEMO.AI;

internal sealed class OriginalImgPickerDialog : Window
{
    private const string FolderName = "Original-IMG";

    private static readonly string[] ImageExts =
        [".jpg", ".jpeg", ".png", ".webp", ".gif", ".avif", ".svg", ".bmp", ".ico"];

    private const int ThumbDecodeWidth = 220;

    private readonly string _folderPath;
    private readonly WrapPanel _grid;
    private readonly TextBlock _header;
    private readonly UIElement _emptyHint;
    private readonly List<(string Path, System.Windows.Controls.Image Img)> _pending = [];
    private int _count;
    private bool _loadingThumbs;
    private System.Windows.Point? _dragStart;

    public OriginalImgPickerDialog(string projectPath)
    {
        Title = "TEMO.AI — เลือกจากรูปที่ดูดมา";
        Width = 1105;
        Height = 750;
        ResizeMode = ResizeMode.NoResize;
        Ui.StyleDialog(this);

        _folderPath = Path.Combine(projectPath, FolderName);
        var files = Directory.Exists(_folderPath)
            ? Directory.GetFiles(_folderPath)
                .Where(f => ImageExts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(Path.GetFileName)
                .ToArray()
            : [];

        var root = new DockPanel { Background = Ui.Brush(0x0A0A0A) };

        var topBar = new Border
        {
            Background = Ui.Brush(0x0E0E0E),
            BorderBrush = Ui.Brush(0x1E1E1E),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(18, 12, 18, 12),
        };
        DockPanel.SetDock(topBar, Dock.Top);
        _header = new TextBlock
        {
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ui.Brush(0xAAAAAA),
            VerticalAlignment = VerticalAlignment.Center,
        };
        topBar.Child = _header;

        var bottomBar = new Border
        {
            Background = Ui.Brush(0x0E0E0E),
            BorderBrush = Ui.Brush(0x1E1E1E),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 10, 16, 10),
        };
        DockPanel.SetDock(bottomBar, Dock.Bottom);

        var addBtn = Ui.DialogButton("+ เพิ่มจากในเครื่อง", accent: true);
        addBtn.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
        addBtn.Padding = new Thickness(18, 0, 18, 0);
        addBtn.Click += AddFromComputer_Click;

        var cancelBtn = Ui.DialogButton("ปิด", accent: false);
        cancelBtn.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
        cancelBtn.Padding = new Thickness(22, 0, 22, 0);
        cancelBtn.Click += (_, _) => Close();

        var barRow = new DockPanel();
        DockPanel.SetDock(addBtn, Dock.Left);
        barRow.Children.Add(addBtn);
        barRow.Children.Add(cancelBtn);
        bottomBar.Child = barRow;

        _grid = new WrapPanel { Margin = new Thickness(10) };
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _grid,
        };

        _emptyHint = new TextBlock
        {
            Text = "ยังไม่มีรูปภาพ\nกดปุ่ม 'ดูดภาพ' ใน Tab IMG หรือ '+ เพิ่มจากในเครื่อง'",
            FontSize = 14,
            Foreground = Ui.Brush(0x606060),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            IsHitTestVisible = false,
        };

        var center = new Grid();
        center.Children.Add(scroll);
        center.Children.Add(_emptyHint);

        root.Children.Add(topBar);
        root.Children.Add(bottomBar);
        root.Children.Add(center);
        Content = root;

        Loaded += async (_, _) =>
        {
            foreach (var f in files) AddCard(f);
            UpdateState();
            await DrainThumbsAsync();
        };
    }

    private async Task DrainThumbsAsync()
    {
        if (_loadingThumbs) return;
        _loadingThumbs = true;
        try
        {
            while (_pending.Count > 0)
            {
                var (path, img) = _pending[0];
                _pending.RemoveAt(0);
                var bmp = await Task.Run(() => Ui.LoadImagePreview(path, ThumbDecodeWidth));
                if (bmp != null) img.Source = bmp;
            }
        }
        finally
        {
            _loadingThumbs = false;
        }
    }

    private async void AddFromComputer_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new WinForms.OpenFileDialog
        {
            Title = "เพิ่มรูปภาพจากในเครื่อง",
            Multiselect = true,
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp;*.gif;*.avif;*.svg;*.bmp;*.ico|All Files|*.*",
        };
        if (dlg.ShowDialog() != WinForms.DialogResult.OK) return;

        Directory.CreateDirectory(_folderPath);

        foreach (var path in dlg.FileNames)
        {
            if (!File.Exists(path) || !ImageExts.Contains(Path.GetExtension(path).ToLowerInvariant()))
                continue;

            var dest = UniqueDestination(Path.GetFileName(path));
            try
            {
                File.Copy(path, dest);
            }
            catch
            {
                continue;
            }
            AddCard(dest);
        }

        UpdateState();
        await DrainThumbsAsync();
    }

    private string UniqueDestination(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var dest = Path.Combine(_folderPath, fileName);
        for (var i = 1; File.Exists(dest); i++)
            dest = Path.Combine(_folderPath, $"{name}({i}){ext}");
        return dest;
    }

    private void AddCard(string filePath)
    {
        var img = new System.Windows.Controls.Image
        {
            Stretch = System.Windows.Media.Stretch.UniformToFill,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        var thumbHost = new Border
        {
            Height = 110,
            Background = Ui.Brush(0x101010),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            ClipToBounds = true,
            Child = img,
        };

        var frame = Ui.MakeFixedImageCard(thumbHost, Path.GetFileName(filePath), filePath);
        frame.MouseEnter += (_, _) => frame.BorderBrush = Ui.Brush(0x707070);
        frame.MouseLeave += (_, _) => frame.BorderBrush = Ui.Brush(0x282828);
        frame.PreviewMouseLeftButtonDown += (_, e) => _dragStart = e.GetPosition(null);
        frame.MouseMove += (_, e) =>
        {
            if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed || _dragStart is not { } start)
                return;
            var pos = e.GetPosition(null);
            if (Math.Abs(pos.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(pos.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;
            _dragStart = null;
            var data = new System.Windows.DataObject(System.Windows.DataFormats.FileDrop, new[] { filePath });
            System.Windows.DragDrop.DoDragDrop(frame, data, System.Windows.DragDropEffects.Copy);
        };

        _grid.Children.Add(frame);
        _pending.Add((filePath, img));
        _count++;
    }

    private void UpdateState()
    {
        _emptyHint.Visibility = _count == 0 ? Visibility.Visible : Visibility.Collapsed;
        _header.Text = _count > 0
            ? $"Original-IMG  —  {_count} รูปภาพ"
            : "Original-IMG";
    }
}
