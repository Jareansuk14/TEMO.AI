namespace TEMO.AI;

public partial class MainWindow
{
    private readonly List<ImageEntry> _imageEntries = [];

    private void BuildImagesPanel()
    {
        _imageEntries.Clear();
        var content = ImagesStore.ReadConfig(_projectPath);
        var defs = ImagesStore.DiscoverDefs(content, ImagesStore.IsPlayButtonUsed(_projectPath),
            ImagesStore.SeoImageNumbers(_projectPath));
        foreach (var (id, label, group, hasAlt) in defs)
            _imageEntries.Add(new ImageEntry { Id = id, Label = label, Group = group, HasAlt = hasAlt });
    }

    private void PullImages()
    {
        var content = ImagesStore.ReadConfig(_projectPath);
        if (content.Length == 0) return;

        foreach (var e in _imageEntries)
        {
            var (src, alt) = ImagesStore.ReadValues(content, e.Id);
            e.SrcValue = src;
            e.AltValue = alt;
            e.OriginalSrc = src;
        }

        RebuildImageGrid();
    }

    private void RebuildImageGrid()
    {
        ImagesPanel.Children.Clear();
        string? lastGroup = null;
        WrapPanel? wrap = null;

        foreach (var entry in _imageEntries)
        {
            if (entry.Group != lastGroup)
            {
                if (lastGroup != null) ImagesPanel.Children.Add(Ui.Divider());
                ImagesPanel.Children.Add(entry.Group == PromoGroup
                    ? MakePromoHeader()
                    : Ui.SectionHeader(entry.Group));
                wrap = new WrapPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Margin = new Thickness(0, 4, 0, 0),
                };
                ImagesPanel.Children.Add(wrap);
                lastGroup = entry.Group;
            }
            wrap!.Children.Add(MakeImageCard(entry));
        }
    }

    private const string PromoGroup = "โปรโมชั่น";

    private int PromoCount => _imageEntries.Count(e => e.Id.StartsWith("promo-"));

    private UIElement MakePromoHeader()
    {
        var grid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var header = Ui.SectionHeader(PromoGroup);
        header.Margin = new Thickness(0);
        header.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(header, 0);
        grid.Children.Add(header);

        var buttons = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var removeBtn = new Button
        {
            Content = "− ลบรูป",
            Style = (Style)FindResource("Btn"),
            Height = 26,
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = 11,
            Margin = new Thickness(0, 0, 6, 0),
            IsEnabled = PromoCount > ImagesStore.MinPromos,
        };
        removeBtn.Click += (_, _) => RemovePromoImage();

        var addBtn = new Button
        {
            Content = "+ เพิ่มรูป",
            Style = (Style)FindResource("Btn"),
            Height = 26,
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = 11,
            IsEnabled = PromoCount < ImagesStore.MaxPromos,
        };
        addBtn.Click += (_, _) => AddPromoImage();

        buttons.Children.Add(removeBtn);
        buttons.Children.Add(addBtn);
        Grid.SetColumn(buttons, 1);
        grid.Children.Add(buttons);

        return grid;
    }

    private void AddPromoImage()
    {
        if (!HasOpenProject()) { ShowMsg("⚠️  เปิดโปรเจคก่อน"); return; }

        var ok = ImagesStore.RewritePromos(_projectPath, blocks =>
        {
            if (blocks.Count >= ImagesStore.MaxPromos) { ShowMsg($"เพิ่มรูปโปรโมชั่นได้สูงสุด {ImagesStore.MaxPromos} รูป"); return false; }
            int next = ImagesStore.NextPromoNumber(blocks);
            blocks.Add($"{{ src: \"/images/promo{next}.webp\", alt: \"\", width: 800, height: 500 }}");
            return true;
        });
        if (!ok) return;

        BuildImagesPanel();
        PullImages();
        ShowMsg("เพิ่มช่องรูปโปรโมชั่นแล้ว — คลิกที่การ์ดเพื่ออัปโหลดรูป");
    }

    private void RemovePromoImage()
    {
        if (!HasOpenProject()) { ShowMsg("⚠️  เปิดโปรเจคก่อน"); return; }

        var removedSrc = "";
        var ok = ImagesStore.RewritePromos(_projectPath, blocks =>
        {
            if (blocks.Count <= ImagesStore.MinPromos) { ShowMsg($"ต้องมีรูปโปรโมชั่นอย่างน้อย {ImagesStore.MinPromos} รูป"); return false; }
            removedSrc = TsBlockParser.QuotedVal(blocks[^1], "src");
            blocks.RemoveAt(blocks.Count - 1);
            return true;
        });
        if (!ok) return;

        if (!string.IsNullOrEmpty(removedSrc)) TryDelete(PublicPath(removedSrc));
        BuildImagesPanel();
        PullImages();
        ShowMsg("ลบรูปโปรโมชั่นแล้ว");
    }

    private Border MakeImageCard(ImageEntry entry)
    {
        const int CardW = 108;
        const int CardH = 72;

        var thumb = new System.Windows.Controls.Image
        {
            Width = CardW,
            Height = CardH,
            Stretch = System.Windows.Media.Stretch.UniformToFill,
            ClipToBounds = true,
        };
        LoadThumbnail(thumb, entry.SrcValue);

        var label = new TextBlock
        {
            Text = entry.Label,
            FontSize = 10,
            Foreground = Ui.Brush(0x888888),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = CardW,
            Margin = new Thickness(0, 5, 0, 0),
        };

        var card = new Border
        {
            Background = Ui.Brush(0x1A1A1A),
            BorderBrush = Ui.Brush(0x282828),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6),
            Margin = new Thickness(0, 0, 8, 8),
            Cursor = Cursors.Hand,
            Tag = entry,
            Child = new StackPanel { Children = { thumb, label } },
        };
        card.MouseDown += ImageCard_Click;
        WireImageDrop(card, entry);
        Ui.WireCardHover(card);
        return card;
    }

    private static readonly string[] DropImageExts =
        [".jpg", ".jpeg", ".png", ".webp", ".gif", ".avif", ".svg", ".bmp", ".ico"];

    private static bool IsDroppableImage(string p) =>
        File.Exists(p) && DropImageExts.Contains(Path.GetExtension(p).ToLowerInvariant());

    private void WireImageDrop(Border card, ImageEntry entry)
    {
        card.AllowDrop = true;
        card.DragOver += ImageCard_DragOver;
        card.Drop += (_, e) =>
        {
            if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] paths
                && paths.FirstOrDefault(IsDroppableImage) is { } file)
                ApplyImageFile(entry, file);
            e.Handled = true;
        };
    }

    private static void ImageCard_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
            ? System.Windows.DragDropEffects.Copy
            : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private static string WithExt(string src, string ext)
    {
        var slash = src.LastIndexOf('/');
        var dir = slash >= 0 ? src[..slash] : "";
        var name = slash >= 0 ? src[(slash + 1)..] : src;
        var dot = name.LastIndexOf('.');
        var stem = dot >= 0 ? name[..dot] : name;
        return string.IsNullOrEmpty(dir) ? $"{stem}{ext}" : $"{dir}/{stem}{ext}";
    }

    private void ApplyImageFile(ImageEntry entry, string filePath)
    {
        if (!HasOpenProject()) { ShowMsg("⚠️  เปิดโปรเจคก่อน"); return; }

        var ext = Path.GetExtension(filePath).Equals(".svg", StringComparison.OrdinalIgnoreCase) ? ".svg" : ".webp";
        var baseSrc = string.IsNullOrEmpty(entry.SrcValue) ? $"/images/{entry.Id}{ext}" : entry.SrcValue;
        var newSrc = WithExt(baseSrc, ext);
        var oldSrc = entry.SrcValue;

        try
        {
            var targetAbs = PublicPath(newSrc);
            Directory.CreateDirectory(Path.GetDirectoryName(targetAbs)!);

            if (!newSrc.Equals(entry.OriginalSrc, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(entry.OriginalSrc))
                TryDelete(PublicPath(entry.OriginalSrc));

            ConvertToWebP(filePath, targetAbs);
        }
        catch (Exception ex)
        {
            ShowMsg($"⚠️  จัดการไฟล์รูปไม่สำเร็จ: {ex.Message}");
            return;
        }

        entry.SrcValue = newSrc;
        ImagesStore.SaveEntry(_projectPath, oldSrc, entry.SrcValue, entry.AltValue, entry.HasAlt);
        entry.OriginalSrc = entry.SrcValue;

        RebuildImageGrid();
        if (entry == _siteQrEntry) RefreshSiteQrThumb();
        ShowMsg($"แทนที่รูปแล้ว: {entry.Label}");
    }

    private OriginalImgPickerDialog? _imgPicker;

    private void OpenOriginalImgPicker_Click(object sender, RoutedEventArgs e)
    {
        if (!HasOpenProject()) { ShowMsg("⚠️  เปิดโปรเจคก่อน"); return; }

        if (_imgPicker is { IsLoaded: true })
        {
            _imgPicker.Activate();
            return;
        }

        _imgPicker = new OriginalImgPickerDialog(_projectPath) { Owner = this };
        _imgPicker.Closed += (_, _) => _imgPicker = null;
        _imgPicker.Show();
    }

    private void LoadThumbnail(System.Windows.Controls.Image target, string src) =>
        target.Source = string.IsNullOrEmpty(src) ? null : Ui.LoadThumbnail(PublicPath(src), 108);

    private void ImageCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border { Tag: ImageEntry entry })
            OpenImageEditDialog(entry);
    }

    private void ScrapeImages_Click(object sender, RoutedEventArgs e)
    {
        if (!HasOpenProject())
        {
            ShowMsg("⚠️  เปิดโปรเจคก่อนจึงจะดูดภาพได้");
            return;
        }

        new ImageScrapeDialog(_projectPath) { Owner = this }.ShowDialog();
    }

    private void OpenImageEditDialog(ImageEntry entry)
    {
        var dialog = new ImageEditDialog(
            entry.Label, entry.SrcValue, entry.AltValue, entry.HasAlt, _projectPath)
        {
            Owner = this,
        };
        if (dialog.ShowDialog() != true) return;

        var oldSrc = entry.SrcValue;
        var srcChanged = !oldSrc.Equals(dialog.ResultSrc, StringComparison.OrdinalIgnoreCase);

        try
        {
            if (srcChanged)
            {
                var oldAbs = PublicPath(oldSrc);
                var newAbs = PublicPath(dialog.ResultSrc);
                if (File.Exists(oldAbs))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newAbs)!);
                    File.Move(oldAbs, newAbs, overwrite: true);
                }
            }
        }
        catch (Exception ex)
        {
            ShowMsg($"⚠️  จัดการไฟล์รูปไม่สำเร็จ: {ex.Message}");
            return;
        }

        entry.SrcValue = dialog.ResultSrc;
        entry.AltValue = dialog.ResultAlt;
        ImagesStore.SaveEntry(_projectPath, oldSrc, entry.SrcValue, entry.AltValue, entry.HasAlt);
        entry.OriginalSrc = entry.SrcValue;

        RebuildImageGrid();
        if (entry == _siteQrEntry) RefreshSiteQrThumb();
        ShowMsg($"บันทึกรูปแล้ว: {entry.Label}");
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static void ConvertToWebP(string sourcePath, string destPath, int quality = 90)
    {
        if (Path.GetExtension(sourcePath).Equals(".svg", StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(sourcePath, destPath, overwrite: true);
            return;
        }
        using var bitmap = SkiaSharp.SKBitmap.Decode(sourcePath);
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Webp, quality);
        using var stream = File.Create(destPath);
        data.SaveTo(stream);
    }
}
