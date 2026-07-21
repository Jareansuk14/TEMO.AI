using System.Windows.Media.Effects;

namespace TEMO.AI;

internal sealed class ColorPickerDialog : Window
{
    public enum OutputFormat { Hex, Rgba }

    private const double ColorAreaWidth = 296;
    private const double ColorAreaHeight = 112;
    private const double HueBarWidth = 296;
    private const double HueBarHeight = 16;

    private readonly Border _preview;
    private Border _hueSurface = null!;
    private readonly TextBox _valueBox;
    private readonly Button _hexTab;
    private readonly Button _rgbaTab;
    private readonly FrameworkElement _alphaRow;
    private readonly Slider _alphaSlider;
    private readonly TextBlock _alphaValue;
    private readonly Shapes.Ellipse _colorThumb;
    private readonly Shapes.Ellipse _hueThumb;

    private bool _updating;
    private bool _draggingColor;
    private bool _draggingHue;
    private double _hue;
    private double _saturation;
    private double _value;
    private byte _alpha;
    private OutputFormat _format;

    public Color SelectedColor { get; private set; }
    public OutputFormat SelectedFormat => _format;

    public ColorPickerDialog(Color initialColor, bool initialIsRgba)
    {
        SelectedColor = initialColor;
        _alpha = initialColor.A;
        _format = initialIsRgba ? OutputFormat.Rgba : OutputFormat.Hex;

        Title = "Color";
        Width = 400;
        Height = 530;
        ResizeMode = ResizeMode.NoResize;
        Ui.StyleDialog(this);

        var panel = new StackPanel();
        Content = new Border
        {
            Background = Ui.Brush(0x101010),
            BorderBrush = Ui.Brush(0x282828),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(18),
            Child = panel
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Color Picker",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0xFFFFFF),
            Margin = new Thickness(0, 0, 0, 4)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Select color for CSS",
            FontSize = 12,
            Foreground = Ui.Brush(0x757575),
            Margin = new Thickness(0, 0, 0, 16)
        });

        _preview = new Border
        {
            Height = 42,
            CornerRadius = new CornerRadius(8),
            BorderBrush = Ui.Brush(0x3A3A3A),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 12)
        };
        panel.Children.Add(_preview);

        _colorThumb = MakeThumb();
        panel.Children.Add(BuildColorArea());

        _hueThumb = MakeThumb();
        panel.Children.Add(BuildHueBar());

        _hexTab = MakeTab("HEX", OutputFormat.Hex);
        _rgbaTab = MakeTab("RGBA", OutputFormat.Rgba);
        panel.Children.Add(BuildFormatTabs());

        (_alphaRow, _alphaSlider, _alphaValue) = BuildAlphaRow();
        panel.Children.Add(_alphaRow);

        _valueBox = new TextBox
        {
            Background = Ui.Brush(0x161616),
            Foreground = Ui.Brush(0xF0F0F0),
            BorderBrush = Ui.Brush(0x303030),
            BorderThickness = new Thickness(1),
            CaretBrush = Ui.Brush(0xFFFFFF),
            Padding = new Thickness(10, 8, 10, 8),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 14)
        };
        _valueBox.LostFocus += (_, _) => ApplyValueBox();
        _valueBox.KeyDown += (_, e) => { if (e.Key == Key.Enter) ApplyValueBox(); };
        panel.Children.Add(_valueBox);
        panel.Children.Add(BuildActions());

        ApplyFormat(_format);
        SetColor(initialColor);
    }

    private Canvas BuildColorArea()
    {
        var canvas = new Canvas
        {
            Width = ColorAreaWidth,
            Height = ColorAreaHeight,
            Cursor = Cursors.Cross,
            Margin = new Thickness(0, 0, 0, 12)
        };

        _hueSurface = new Border
        {
            Width = ColorAreaWidth,
            Height = ColorAreaHeight,
            CornerRadius = new CornerRadius(9),
            Background = Ui.Brush(0x00FF40)
        };
        canvas.Children.Add(_hueSurface);
        canvas.Children.Add(MakeOverlay(new LinearGradientBrush(
            Color.FromRgb(0xFF, 0xFF, 0xFF),
            Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF),
            new Point(0, 0.5), new Point(1, 0.5))));
        canvas.Children.Add(MakeOverlay(new LinearGradientBrush(
            Color.FromArgb(0x00, 0x00, 0x00, 0x00),
            Color.FromRgb(0x00, 0x00, 0x00),
            new Point(0.5, 0), new Point(0.5, 1))));
        canvas.Children.Add(_colorThumb);

        canvas.MouseLeftButtonDown += (_, e) =>
        {
            _draggingColor = true;
            canvas.CaptureMouse();
            UpdateColorFromPoint(e.GetPosition(canvas));
        };
        canvas.MouseMove += (_, e) =>
        {
            if (_draggingColor) UpdateColorFromPoint(e.GetPosition(canvas));
        };
        canvas.MouseLeftButtonUp += (_, _) =>
        {
            _draggingColor = false;
            canvas.ReleaseMouseCapture();
        };
        return canvas;
    }

    private Canvas BuildHueBar()
    {
        var canvas = new Canvas
        {
            Width = HueBarWidth,
            Height = 24,
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 0, 14)
        };
        canvas.Children.Add(new Border
        {
            Width = HueBarWidth,
            Height = HueBarHeight,
            CornerRadius = new CornerRadius(8),
            Background = MakeHueBrush()
        });
        Canvas.SetTop(_hueThumb, -1);
        canvas.Children.Add(_hueThumb);

        canvas.MouseLeftButtonDown += (_, e) =>
        {
            _draggingHue = true;
            canvas.CaptureMouse();
            UpdateHueFromPoint(e.GetPosition(canvas));
        };
        canvas.MouseMove += (_, e) =>
        {
            if (_draggingHue) UpdateHueFromPoint(e.GetPosition(canvas));
        };
        canvas.MouseLeftButtonUp += (_, _) =>
        {
            _draggingHue = false;
            canvas.ReleaseMouseCapture();
        };
        return canvas;
    }

    private Grid BuildFormatTabs()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(_hexTab);
        Grid.SetColumn(_rgbaTab, 2);
        grid.Children.Add(_rgbaTab);
        return grid;
    }

    private Button MakeTab(string text, OutputFormat format)
    {
        var tab = new Button
        {
            Content = text,
            Height = 34,
            Cursor = Cursors.Hand,
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            BorderThickness = new Thickness(1)
        };
        tab.Click += (_, _) => ApplyFormat(format);
        return tab;
    }

    private (FrameworkElement Row, Slider Slider, TextBlock Value) BuildAlphaRow()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var label = new TextBlock
        {
            Text = "ALPHA",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0x757575),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };

        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 1,
            SmallChange = 0.01,
            LargeChange = 0.1,
            VerticalAlignment = VerticalAlignment.Center
        };
        slider.ValueChanged += (_, _) =>
        {
            if (_updating) return;
            _alpha = (byte)Math.Round(slider.Value * 255);
            UpdateSelectedFromHsv();
        };

        var value = new TextBlock
        {
            FontSize = 12,
            FontFamily = new FontFamily("Consolas"),
            Foreground = Ui.Brush(0xC8C8C8),
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 36,
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(10, 0, 0, 0)
        };

        Grid.SetColumn(slider, 1);
        Grid.SetColumn(value, 2);
        grid.Children.Add(label);
        grid.Children.Add(slider);
        grid.Children.Add(value);
        return (grid, slider, value);
    }

    private Grid BuildActions()
    {
        var actions = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var cancel = Ui.DialogButton("Cancel", accent: false);
        cancel.Click += (_, _) => { DialogResult = false; Close(); };
        actions.Children.Add(cancel);

        var ok = Ui.DialogButton("OK", accent: true);
        ok.Click += (_, _) => { DialogResult = true; Close(); };
        Grid.SetColumn(ok, 2);
        actions.Children.Add(ok);
        return actions;
    }

    private static Border MakeOverlay(Brush background) => new()
    {
        Width = ColorAreaWidth,
        Height = ColorAreaHeight,
        CornerRadius = new CornerRadius(9),
        Background = background
    };

    private static Shapes.Ellipse MakeThumb() => new()
    {
        Width = 16,
        Height = 16,
        StrokeThickness = 2,
        Stroke = Ui.Brush(0xFFFFFF),
        Fill = Brushes.Transparent,
        Effect = new DropShadowEffect
        {
            Color = Color.FromRgb(0x00, 0x00, 0x00),
            BlurRadius = 4,
            ShadowDepth = 0,
            Opacity = 0.75
        }
    };

    private static LinearGradientBrush MakeHueBrush()
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x00, 0x00), 0.00));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xFF, 0x00), 0.17));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0xFF, 0x00), 0.33));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0xFF, 0xFF), 0.50));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0x00, 0xFF), 0.67));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x00, 0xFF), 0.83));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x00, 0x00), 1.00));
        return brush;
    }

    private void ApplyFormat(OutputFormat format)
    {
        _format = format;

        StyleTab(_hexTab, format == OutputFormat.Hex);
        StyleTab(_rgbaTab, format == OutputFormat.Rgba);
        _alphaRow.Visibility = format == OutputFormat.Rgba ? Visibility.Visible : Visibility.Collapsed;

        if (format == OutputFormat.Hex) _alpha = 255;
        UpdateSelectedFromHsv();
    }

    private static void StyleTab(Button tab, bool active)
    {
        tab.Background = Ui.Brush(active ? 0xE8E8E8u : 0x1A1A1Au);
        tab.Foreground = Ui.Brush(active ? 0x0A0A0Au : 0x9A9A9Au);
        tab.BorderBrush = Ui.Brush(active ? 0xE8E8E8u : 0x2E2E2Eu);
    }

    private void SetColor(Color color)
    {
        _alpha = _format == OutputFormat.Hex ? (byte)255 : color.A;
        CssColor.ToHsv(color, out _hue, out _saturation, out _value);
        UpdateSelectedFromHsv();
    }

    private void UpdateColorFromPoint(Point point)
    {
        if (_updating) return;
        _saturation = Math.Clamp(point.X / ColorAreaWidth, 0, 1);
        _value = 1 - Math.Clamp(point.Y / ColorAreaHeight, 0, 1);
        UpdateSelectedFromHsv();
    }

    private void UpdateHueFromPoint(Point point)
    {
        if (_updating) return;
        _hue = Math.Clamp(point.X / HueBarWidth, 0, 1) * 360;
        UpdateSelectedFromHsv();
    }

    private void UpdateSelectedFromHsv()
    {
        _updating = true;
        var rgb = CssColor.FromHsv(_hue, _saturation, _value, _alpha);
        SelectedColor = rgb;
        _valueBox.Text = _format == OutputFormat.Rgba ? CssColor.ToRgba(rgb) : CssColor.ToHex(rgb);
        _preview.Background = Ui.Brush(rgb);
        _hueSurface.Background = Ui.Brush(CssColor.FromHsv(_hue, 1, 1, 255));
        _alphaSlider.Value = _alpha / 255d;
        _alphaValue.Text = Math.Round(_alpha / 255d, 2).ToString("0.##");
        Canvas.SetLeft(_colorThumb, _saturation * ColorAreaWidth - _colorThumb.Width / 2);
        Canvas.SetTop(_colorThumb, (1 - _value) * ColorAreaHeight - _colorThumb.Height / 2);
        Canvas.SetLeft(_hueThumb, (_hue / 360) * HueBarWidth - _hueThumb.Width / 2);
        _updating = false;
    }

    private void ApplyValueBox()
    {
        if (_updating) return;
        if (CssColor.TryParse(_valueBox.Text, out var color))
            SetColor(color);
    }
}
