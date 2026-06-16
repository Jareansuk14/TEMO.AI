using System.Windows.Media.Animation;

namespace TEMO.AI;

internal sealed class ImageScrapeDialog : Window
{
    private sealed class Card
    {
        public required ScrapedImage Image { get; init; }
        public required Border Frame { get; init; }
        public required Border Check { get; init; }
        public bool Selected;
    }

    private const string FolderName = "Original-IMG";

    private readonly string _projectPath;
    private readonly TextBox _urlBox;
    private readonly Button _fetchBtn;
    private readonly Button _downloadBtn;
    private readonly Button _selectAllBtn;
    private readonly TextBlock _status;
    private readonly WrapPanel _grid;
    private readonly ScrollViewer _scroll;
    private readonly Border _toast;
    private readonly TextBlock _toastText;
    private readonly Microsoft.Web.WebView2.Wpf.WebView2 _web;
    private readonly List<Card> _cards = [];

    private NetworkImageScraper? _scraper;
    private CancellationTokenSource? _cts;
    private bool _allSelected;

    public ImageScrapeDialog(string projectPath)
    {
        _projectPath = projectPath;

        Title = "TEMO.AI — ดูดภาพจากเว็บ";
        Width = 1105;
        Height = 770;
        ResizeMode = ResizeMode.NoResize;
        Ui.StyleDialog(this);

        var root = new DockPanel();

        var topBar = new Border
        {
            Background = Ui.Brush(0x0E0E0E),
            BorderBrush = Ui.Brush(0x1E1E1E),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 12, 16, 12),
        };
        DockPanel.SetDock(topBar, Dock.Top);

        var topGrid = new Grid();
        topGrid.ColumnDefinitions.Add(new ColumnDefinition());
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _urlBox = Ui.MakeDarkInput("https://");
        _urlBox.KeyDown += (_, e) => { if (e.Key == Key.Enter) StartFetch(); };
        Grid.SetColumn(_urlBox, 0);

        _fetchBtn = Ui.DialogButton("ดึงรูป", accent: true);
        _fetchBtn.Padding = new Thickness(22, 0, 22, 0);
        _fetchBtn.Click += (_, _) => StartFetch();
        Grid.SetColumn(_fetchBtn, 2);

        topGrid.Children.Add(_urlBox);
        topGrid.Children.Add(_fetchBtn);
        topBar.Child = topGrid;

        var bottomBar = new Border
        {
            Background = Ui.Brush(0x0E0E0E),
            BorderBrush = Ui.Brush(0x1E1E1E),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 10, 16, 10),
        };
        DockPanel.SetDock(bottomBar, Dock.Bottom);

        var bottomGrid = new Grid();
        bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bottomGrid.ColumnDefinitions.Add(new ColumnDefinition());
        bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _selectAllBtn = Ui.DialogButton("เลือกทั้งหมด", accent: false);
        _selectAllBtn.IsEnabled = false;
        _selectAllBtn.Click += (_, _) => ToggleSelectAll();
        Grid.SetColumn(_selectAllBtn, 0);

        _status = new TextBlock
        {
            Text = "พร้อมใช้งาน — ใส่ URL แล้วกดดึงรูป",
            Foreground = Ui.Brush(0x808080),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 14, 0),
        };
        Grid.SetColumn(_status, 1);

        _downloadBtn = Ui.DialogButton("โหลดที่เลือก", accent: true);
        _downloadBtn.Padding = new Thickness(22, 0, 22, 0);
        _downloadBtn.IsEnabled = false;
        _downloadBtn.Click += (_, _) => DownloadSelected();
        Grid.SetColumn(_downloadBtn, 2);

        var closeBtn = Ui.DialogButton("ปิด", accent: false);
        closeBtn.Padding = new Thickness(18, 0, 18, 0);
        closeBtn.Click += (_, _) => Close();
        Grid.SetColumn(closeBtn, 4);

        bottomGrid.Children.Add(_selectAllBtn);
        bottomGrid.Children.Add(_status);
        bottomGrid.Children.Add(_downloadBtn);
        bottomGrid.Children.Add(closeBtn);
        bottomBar.Child = bottomGrid;

        _grid = new WrapPanel { Margin = new Thickness(10) };
        _scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _grid,
        };

        _toastText = new TextBlock
        {
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ui.Brush(0xEEEEEE),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _toast = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(230, 0x18, 0x18, 0x18)),
            BorderBrush = Ui.Brush(0x404040),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(36, 16, 36, 16),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
            Child = _toastText,
        };
        _web = new Microsoft.Web.WebView2.Wpf.WebView2
        {
            Visibility = Visibility.Collapsed,
            DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 10, 10, 10),
        };

        var contentArea = new Grid();
        contentArea.Children.Add(_web);
        contentArea.Children.Add(_scroll);
        contentArea.Children.Add(_toast);

        root.Children.Add(topBar);
        root.Children.Add(bottomBar);
        root.Children.Add(contentArea);

        Content = root;
        Closed += (_, _) => { _cts?.Cancel(); _web.Dispose(); };
        Loaded += async (_, _) => { _urlBox.Focus(); _urlBox.SelectAll(); await EnsureWebAsync(); };
    }

    private async Task<bool> EnsureWebAsync()
    {
        if (_scraper is not null) return true;
        try
        {
            await _web.EnsureCoreWebView2Async();
            _scraper = new NetworkImageScraper(_web.CoreWebView2);
            return true;
        }
        catch (Exception ex)
        {
            _status.Text = $"⚠️  เริ่ม WebView2 ไม่สำเร็จ: {ex.Message}";
            return false;
        }
    }

    private async void StartFetch()
    {
        var url = _urlBox.Text.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _status.Text = "⚠️  URL ต้องขึ้นต้นด้วย http:// หรือ https://";
            return;
        }

        if (!await EnsureWebAsync()) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        _fetchBtn.IsEnabled = false;
        _downloadBtn.IsEnabled = false;
        _selectAllBtn.IsEnabled = false;
        _allSelected = false;
        _selectAllBtn.Content = "เลือกทั้งหมด";
        _cards.Clear();
        _grid.Children.Clear();
        ShowLoading("กำลังดึงรูป...");
        _status.Text = "กำลังดึงรูป...";

        string? doneMessage = null;
        try
        {
            var progress = new Progress<ImageScrapeProgress>(p =>
            {
                ShowLoading("กำลังดึงรูป...");
                _status.Text = "กำลังดึงรูป...";
            });
            var images = await _scraper!.CaptureAsync(url, progress, ct);

            if (ct.IsCancellationRequested) return;

            await RenderCardsAsync(images, ct);

            _status.Text = images.Count > 0
                ? $"ดึงรูปเสร็จแล้วเจอ {images.Count} รูป — ติกเลือกรูปที่ต้องการ"
                : "ไม่พบรูปในหน้านี้";
            if (images.Count > 0)
                doneMessage = $"ดึงรูปเสร็จแล้วเจอ {images.Count} รูป";
            _selectAllBtn.IsEnabled = images.Count > 0;
            UpdateDownloadState();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _status.Text = $"⚠️  ดึงรูปไม่สำเร็จ: {ex.Message}";
        }
        finally
        {
            HideLoading();
            if (doneMessage is not null)
                ShowToast(doneMessage);
            try { _web.CoreWebView2?.Navigate("about:blank"); } catch { }
            if (!ct.IsCancellationRequested) _fetchBtn.IsEnabled = true;
        }
    }

    private async Task RenderCardsAsync(IReadOnlyList<ScrapedImage> images, CancellationToken ct)
    {
        const int batchSize = 32;
        for (var i = 0; i < images.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            AddCard(images[i]);

            var done = i + 1;
            if (done % batchSize == 0 || done == images.Count)
            {
                ShowLoading("กำลังดึงรูป...");
                await Task.Yield();
            }
        }
    }

    private void AddCard(ScrapedImage image)
    {
        var thumbHost = Ui.MakeThumbHost(MakePreview(image, 300), image.Extension, 110);

        var check = new Border
        {
            Width = 20,
            Height = 20,
            CornerRadius = new CornerRadius(3),
            Background = Ui.Brush(0x101010),
            BorderBrush = Ui.Brush(0x555555),
            BorderThickness = new Thickness(1.5),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(6),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = "✓",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Ui.Brush(0x0A0A0A),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Hidden,
            },
        };

        var imageArea = new Grid();
        imageArea.Children.Add(thumbHost);
        imageArea.Children.Add(check);

        var label = $"{image.Extension.TrimStart('.').ToUpperInvariant()} · {FormatSize(image.Data.Length)}";
        var frame = Ui.MakeFixedImageCard(imageArea, label, image.Url);
        var card = new Card { Image = image, Frame = frame, Check = check };
        frame.MouseDown += (_, _) => ToggleCard(card);
        _cards.Add(card);
        _grid.Children.Add(frame);
    }

    private void ToggleCard(Card card)
    {
        card.Selected = !card.Selected;
        ApplyCardVisual(card);
        UpdateDownloadState();
    }

    private static void ApplyCardVisual(Card card)
    {
        card.Frame.BorderBrush = Ui.Brush(card.Selected ? 0xE8E8E8u : 0x282828u);
        card.Frame.Background = Ui.Brush(card.Selected ? 0x202020u : 0x1A1A1Au);
        card.Check.Background = Ui.Brush(card.Selected ? 0xE8E8E8u : 0x101010u);
        card.Check.BorderBrush = Ui.Brush(card.Selected ? 0xE8E8E8u : 0x555555u);
        ((TextBlock)card.Check.Child).Visibility = card.Selected ? Visibility.Visible : Visibility.Hidden;
    }

    private void ToggleSelectAll()
    {
        _allSelected = !_allSelected;
        foreach (var card in _cards)
        {
            card.Selected = _allSelected;
            ApplyCardVisual(card);
        }
        _selectAllBtn.Content = _allSelected ? "ไม่เลือกทั้งหมด" : "เลือกทั้งหมด";
        UpdateDownloadState();
    }

    private void UpdateDownloadState()
    {
        var count = _cards.Count(c => c.Selected);
        _downloadBtn.IsEnabled = count > 0;
        _downloadBtn.Content = count > 0 ? $"โหลดที่เลือก ({count})" : "โหลดที่เลือก";
    }

    private void DownloadSelected()
    {
        var selected = _cards.Where(c => c.Selected).ToList();
        if (selected.Count == 0) return;

        var targetDir = Path.Combine(_projectPath, FolderName);
        try
        {
            Directory.CreateDirectory(targetDir);
        }
        catch (Exception ex)
        {
            _status.Text = $"⚠️  สร้างโฟลเดอร์ไม่สำเร็จ: {ex.Message}";
            return;
        }

        var saved = 0;
        foreach (var card in selected)
        {
            try
            {
                var fileName = BaseName(card.Image.Url) + card.Image.Extension;
                File.WriteAllBytes(Path.Combine(targetDir, fileName), card.Image.Data);
                saved++;
            }
            catch { }
        }

        _status.Text = $"✓  บันทึก {saved} รูป → {FolderName}";
        ShowToast($"✓  บันทึก {saved} รูป เรียบร้อยแล้ว");
    }

    private void ShowLoading(string message)
    {
        _toast.BeginAnimation(OpacityProperty, null);
        _toastText.Text = message;
        _toast.Opacity = 1;
        _toast.Visibility = Visibility.Visible;
    }

    private void HideLoading()
    {
        _toast.BeginAnimation(OpacityProperty, null);
        _toast.Visibility = Visibility.Collapsed;
    }

    private void ShowToast(string message)
    {
        _toast.BeginAnimation(OpacityProperty, null);
        _toastText.Text = message;
        _toast.Opacity = 1;
        _toast.Visibility = Visibility.Visible;

        var fade = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(600)))
        {
            BeginTime = TimeSpan.FromMilliseconds(1800),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
        };
        fade.Completed += (_, _) => _toast.Visibility = Visibility.Collapsed;
        _toast.BeginAnimation(OpacityProperty, fade);
    }

    private static BitmapImage? MakePreview(ScrapedImage image, int decodeWidth)
    {
        if (IsSvg(image))
            return Ui.BitmapFromSvgBytes(image.Data, decodeWidth);
        return Ui.BitmapFromBytes(image.Data, decodeWidth) ?? Ui.BitmapFromSvgBytes(image.Data, decodeWidth);
    }

    private static bool IsSvg(ScrapedImage image)
    {
        if (image.Extension.Equals(".svg", StringComparison.OrdinalIgnoreCase)) return true;
        var head = Encoding.UTF8.GetString(image.Data, 0, Math.Min(image.Data.Length, 256))
            .TrimStart('\uFEFF', ' ', '\n', '\r', '\t');
        return head.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            || (head.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
                && head.Contains("<svg", StringComparison.OrdinalIgnoreCase));
    }

    private static string BaseName(string url)
    {
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return "image";
        try
        {
            var path = new Uri(url).AbsolutePath;
            var name = Path.GetFileNameWithoutExtension(Uri.UnescapeDataString(path));
            name = Sanitize(name);
            return string.IsNullOrWhiteSpace(name) ? "image" : name;
        }
        catch
        {
            return "image";
        }
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '-');
        name = name.Trim('-', '.', ' ');
        return name.Length > 60 ? name[..60] : name;
    }

    private static string FormatSize(int bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / 1024d / 1024d:0.0} MB",
        >= 1024 => $"{bytes / 1024d:0} KB",
        _ => $"{bytes} B",
    };
}
