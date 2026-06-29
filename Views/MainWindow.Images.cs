namespace TEMO.AI;

public partial class MainWindow
{
    private List<ImageEntry> _imageEntries => _session.ImageEntries;

    private void ImgAiToggle_Click(object sender, RoutedEventArgs e) =>
        ToggleFlyout(ImgAiPanel);

    private void ImgAiClose_Click(object sender, RoutedEventArgs e) =>
        ImgAiPanel.Visibility = Visibility.Collapsed;

    private async void ImgAiGen_Click(object sender, RoutedEventArgs e)
    {
        if (!HasOpenProject()) { ShowMsg("⚠️  เปิดโปรเจคก่อน"); return; }
        if (_vm.Ai.Busy) { ShowMsg("AI กำลังทำงานอยู่"); return; }
        if (!TryGetApiKey(out var apiKey)) return;

        var brand = ContentStore.CurrentBrandName(_projectPath);
        if (string.IsNullOrWhiteSpace(brand)) { ShowMsg("⚠️  ไม่พบชื่อแบรนด์"); return; }

        var type = ContentTypes.FromRadios(ImgAiLottery.IsChecked == true, ImgAiSlot.IsChecked == true);

        var rng = new Random();
        var palette = PaletteStore.Random(rng);
        var render = ImageRenderCatalog.Random(rng);
        var options = new GenerationOptions(brand, type, AiModels.TextDefault,
            Style: ImageStyleCatalog.RandomForRender(rng, render), Render: render);

        var genLog = new GenerationLog(_projectPath, brand, $"TAB IMG — สร้างรูป/CSS แบรนด์: {brand}", "img");
        genLog.Line($"ContentType: {type} | Style: {options.Style?.Name} | Render: {options.Render?.Name} | Palette: {palette.Name}");

        _vm.Ai.Busy = true;
        ShowAiOverlayLoading();
        ImgAiGenBtn.IsEnabled = false;
        try
        {
            await Task.Run(() => ThemeRandomizer.Apply(_projectPath, palette));

            var (ok, error, count) = await ImageCssRegenerator.RunAsync(
                _projectPath, options, palette, apiKey,
                m => Dispatcher.Invoke(() => AiOverlayStatusText.Text = m), default, genLog);
            if (!ok)
            {
                genLog.Finish(false, error ?? "สร้างรูป/CSS ล้มเหลว");
                ShowAiError(error, "สร้างรูป/CSS ล้มเหลว");
                return;
            }
            genLog.Finish(true, $"สร้างรูป + CSS ใหม่แล้ว ({count} รูป)");

            HideAiOverlay();
            ImgAiPanel.Visibility = Visibility.Collapsed;

            BuildImagesPanel();
            PullImages();
            LoadCssVariables();

            if (_devProcess is { HasExited: false })
                WebView.CoreWebView2?.Reload();

            ShowMsg($"🤖  สร้างรูป + CSS ใหม่แล้ว ({count} รูป)");
        }
        catch (Exception ex)
        {
            ShowAiOverlayError($"❌  {ex.Message}");
        }
        finally
        {
            _vm.Ai.Busy = false;
            ImgAiGenBtn.IsEnabled = true;
        }
    }

    private void BuildImagesPanel() => _vm.Images.LoadEntries();

    private void PullImages()
    {
        if (!_vm.Images.PullValues()) return;
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
                ImagesPanel.Children.Add(Ui.SectionHeader(entry.Group));
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
                Io.DeleteFile(PublicPath(entry.OriginalSrc));

            ConvertToWebP(filePath, targetAbs);
        }
        catch (Exception ex)
        {
            ShowMsg($"⚠️  จัดการไฟล์รูปไม่สำเร็จ: {ex.Message}");
            return;
        }

        entry.SrcValue = newSrc;
        ImagesStore.SaveEntry(_projectPath, oldSrc, entry.SrcValue, entry.AltValue, entry.HasAlt, entry.Id);
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
        ImagesStore.SaveEntry(_projectPath, oldSrc, entry.SrcValue, entry.AltValue, entry.HasAlt, entry.Id);
        entry.OriginalSrc = entry.SrcValue;

        RebuildImageGrid();
        if (entry == _siteQrEntry) RefreshSiteQrThumb();
        ShowMsg($"บันทึกรูปแล้ว: {entry.Label}");
    }

    private static void ConvertToWebP(string sourcePath, string destPath, int quality = 90)
    {
        if (Path.GetExtension(sourcePath).Equals(".svg", StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(sourcePath, destPath, overwrite: true);
            return;
        }
        using var bitmap = SkiaSharp.SKBitmap.Decode(sourcePath);
        if (bitmap is null)
            throw new InvalidOperationException($"ไม่สามารถอ่านไฟล์รูปภาพ: {Path.GetFileName(sourcePath)}");
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Webp, quality);
        using var stream = File.Create(destPath);
        data.SaveTo(stream);
    }
}
