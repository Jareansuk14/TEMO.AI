using SkiaSharp;
using Svg.Skia;
using Binding = System.Windows.Data.Binding;
using RelativeSource = System.Windows.Data.RelativeSource;
using RelativeSourceMode = System.Windows.Data.RelativeSourceMode;

namespace TEMO.AI;

internal static class Ui
{
    public static SolidColorBrush Brush(uint rgb) => new(Color.FromRgb(
        (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));

    public static SolidColorBrush Brush(Color color) => new(color);

    public static TextBox MakeInput(FrameworkElement host, double fontSize = 14, double minHeight = 40) => new()
    {
        Style = host.TryFindResource("Input") as Style,
        FontSize = fontSize,
        MinHeight = minHeight,
    };

    public static void WireCardHover(Border card, uint normalRgb = 0x282828, uint hoverRgb = 0x555555)
    {
        var normal = Brush(normalRgb);
        card.BorderBrush = normal;
        card.MouseEnter += (_, _) => card.BorderBrush = Brush(hoverRgb);
        card.MouseLeave += (_, _) => card.BorderBrush = normal;
    }

    public static void StyleDialog(Window window)
    {
        window.Background = Brush(0x0A0A0A);
        window.Foreground = Brush(0xDEDEDE);
        window.FontFamily = new FontFamily("Segoe UI");
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    public static Button DialogButton(string content, bool accent) =>
        MakeDialogButton(content, accent ? 0xE8E8E8u : 0x202020u, accent ? 0xCCCCCCu : 0x2C2C2Cu,
            accent ? 0xD4D4D4u : 0x2C2C2Cu, accent ? 0x0A0A0Au : 0xB0B0B0u, accent);

    public static Button DialogDangerButton(string content) =>
        MakeDialogButton(content, 0xC92424u, 0xA81E1Eu, 0xA81E1Eu, 0xF5F5F5u, bold: false);

    private static Button MakeDialogButton(string content, uint bg, uint border, uint hoverBg, uint fg, bool bold)
    {
        var bd = new FrameworkElementFactory(typeof(Border)) { Name = "bd" };
        bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        bd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        bd.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
        bd.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));

        var cp = new FrameworkElementFactory(typeof(ContentPresenter));
        cp.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
        cp.SetValue(FrameworkElement.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
        cp.SetBinding(FrameworkElement.MarginProperty,
            new Binding("Padding") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        bd.AppendChild(cp);

        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty, Brush(hoverBg), "bd"));

        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.35));

        var ct = new ControlTemplate(typeof(Button)) { VisualTree = bd };
        ct.Triggers.Add(hover);
        ct.Triggers.Add(disabled);

        return new Button
        {
            Content = content,
            Height = 36,
            Padding = new Thickness(14, 0, 14, 0),
            FontSize = 12,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Background = Brush(bg),
            BorderBrush = Brush(border),
            BorderThickness = new Thickness(1),
            Foreground = Brush(fg),
            Cursor = Cursors.Hand,
            FocusVisualStyle = null,
            Template = ct,
        };
    }

    public static TextBlock SectionLabel(string text) => new()
    {
        Text = text,
        FontSize = 14,
        FontWeight = FontWeights.SemiBold,
        Foreground = Brush(0xEDEDED),
        VerticalAlignment = VerticalAlignment.Center,
    };

    public static void RunOnUi(DispatcherObject owner, Action action)
    {
        if (owner.Dispatcher.CheckAccess()) action();
        else owner.Dispatcher.BeginInvoke(action);
    }

    public static Viewbox MakeMinusIcon(double size, Brush color)
    {
        var canvas = new Canvas { Width = 24, Height = 24 };

        var ring = new Shapes.Ellipse
        {
            Width = 18,
            Height = 18,
            Stroke = color,
            StrokeThickness = 1.75,
            Fill = Brushes.Transparent,
        };
        Canvas.SetLeft(ring, 3);
        Canvas.SetTop(ring, 3);

        var bar = new Shapes.Path
        {
            Data = Geometry.Parse("M8 12H16"),
            Stroke = color,
            StrokeThickness = 2,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        };

        canvas.Children.Add(ring);
        canvas.Children.Add(bar);

        return new Viewbox { Width = size, Height = size, Stretch = Stretch.Uniform, Child = canvas };
    }

    private static readonly Dictionary<string, (long Stamp, BitmapImage? Image)> ThumbCache = new(StringComparer.OrdinalIgnoreCase);

    public static BitmapImage? LoadThumbnail(string path, int decodeWidth)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        long stamp;
        try { stamp = File.GetLastWriteTimeUtc(path).Ticks; } catch { stamp = 0; }

        var key = $"{path}|{decodeWidth}";
        if (ThumbCache.TryGetValue(key, out var cached) && cached.Stamp == stamp)
            return cached.Image;

        var image = LoadBitmap(path, decodeWidth);
        ThumbCache[key] = (stamp, image);
        return image;
    }

    public static BitmapImage? LoadBitmap(string path, int decodeWidth = 0)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            if (decodeWidth > 0) bmp.DecodePixelWidth = decodeWidth;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }

    public static BitmapImage? BitmapFromBytes(byte[] data, int decodeWidth = 0)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = new MemoryStream(data, writable: false);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            if (decodeWidth > 0) bmp.DecodePixelWidth = decodeWidth;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }

    public static BitmapImage? BitmapFromSvgBytes(byte[] data, int decodeWidth = 300)
    {
        try
        {
            using var input = new MemoryStream(data, writable: false);
            using var svg = new SKSvg();
            if (svg.Load(input) is null || svg.Picture is null) return null;
            return RenderSvgPicture(svg.Picture, decodeWidth);
        }
        catch { return null; }
    }

    public static BitmapImage? BitmapFromSvgFile(string path, int decodeWidth = 300)
    {
        if (!File.Exists(path)) return null;
        try
        {
            using var svg = new SKSvg();
            if (svg.Load(path) is null || svg.Picture is null) return null;
            return RenderSvgPicture(svg.Picture, decodeWidth);
        }
        catch { return null; }
    }

    public static BitmapImage? LoadImagePreview(string path, int decodeWidth = 300)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext == ".svg" ? BitmapFromSvgFile(path, decodeWidth) : LoadBitmap(path, decodeWidth);
    }

    private static BitmapImage? RenderSvgPicture(SKPicture picture, int decodeWidth)
    {
        var rect = picture.CullRect;
        var w = rect.Width > 0 ? rect.Width : 256f;
        var h = rect.Height > 0 ? rect.Height : 256f;
        var scale = decodeWidth > 0 ? decodeWidth / Math.Max(w, h) : 1f;

        var info = new SKImageInfo(Math.Max(1, (int)(w * scale)), Math.Max(1, (int)(h * scale)));
        using var surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Scale(scale);
        surface.Canvas.DrawPicture(picture);
        surface.Canvas.Flush();

        using var snapshot = surface.Snapshot();
        using var encoded = snapshot.Encode(SKEncodedImageFormat.Png, 100);
        return BitmapFromBytes(encoded.ToArray());
    }

    public static Border MakeThumbHost(BitmapImage? source, string extLabel, int height)
    {
        UIElement child = source is not null
            ? new System.Windows.Controls.Image
            {
                Source = source,
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            }
            : new TextBlock
            {
                Text = extLabel.TrimStart('.').ToUpperInvariant(),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brush(0x707070),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

        return new Border
        {
            Height = height,
            Background = Brush(0x101010),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            ClipToBounds = true,
            Child = child,
        };
    }

    public static Border MakeImageCard(UIElement content, string tooltip) => new()
    {
        Background = Brush(0x1A1A1A),
        BorderBrush = Brush(0x282828),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(6),
        Margin = new Thickness(4),
        Cursor = Cursors.Hand,
        Child = content,
        ToolTip = tooltip,
    };

    public static Border MakeFixedImageCard(UIElement preview, string label, string tooltip, int width = 168)
    {
        var labelBlock = new TextBlock
        {
            Text = label,
            FontSize = 10,
            Foreground = Brush(0x888888),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        var frame = MakeImageCard(new StackPanel { Children = { preview, labelBlock } }, tooltip);
        frame.Width = width;
        return frame;
    }

    public static TextBlock SectionHeader(string text) => new()
    {
        Text = text,
        FontSize = 13,
        FontWeight = FontWeights.Bold,
        Foreground = Brush(0xAAAAAA),
        Margin = new Thickness(0, 8, 0, 10),
    };

    public static TextBlock FieldLabel(string text) => new()
    {
        Text = text,
        FontSize = 13,
        Foreground = Brush(0xB8B8B8),
        Margin = new Thickness(0, 0, 0, 5),
    };

    public static Shapes.Rectangle Divider() => new()
    {
        Height = 1,
        Fill = Brush(0x222222),
        Margin = new Thickness(0, 4, 0, 12),
    };

    public static TextBox MakeDarkInput(string value = "", bool multiline = false) => new()
    {
        Text = value,
        Background = Brush(0x161616),
        Foreground = Brush(0xDEDEDE),
        BorderBrush = Brush(0x282828),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(10, 8, 10, 8),
        FontSize = 13,
        CaretBrush = Brushes.White,
        SelectionBrush = Brush(0x505050),
        MinHeight = multiline ? 80 : 40,
        VerticalContentAlignment = VerticalAlignment.Center,
        TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
        AcceptsReturn = multiline,
        VerticalScrollBarVisibility = multiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
    };
}
