using System.Windows.Media.Animation;

namespace TEMO.AI;

internal sealed class LoadingPanel : Grid
{
    private readonly TextBlock _message;
    private readonly Border _spinner;
    private readonly RotateTransform _spinnerRotate;
    private UIElement? _content;

    public LoadingPanel()
    {
        _message = new TextBlock
        {
            FontSize = 13.5,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 14, 16, 14),
            Visibility = Visibility.Collapsed,
        };
        Children.Add(_message);

        (_spinner, _spinnerRotate) = CreateSpinner();
        _spinner.Visibility = Visibility.Collapsed;
        Children.Add(_spinner);
    }

    public void ShowSpinner()
    {
        HideContent();
        _message.Visibility = Visibility.Collapsed;
        _spinner.Visibility = Visibility.Visible;
        StartSpinnerAnimation();
    }

    public void ShowBlank()
    {
        HideContent();
        _message.Visibility = Visibility.Collapsed;
        StopSpinnerAnimation();
        _spinner.Visibility = Visibility.Collapsed;
    }

    public void ShowEmpty(string text) => ShowMessage(text, 0x8A8A8A);

    public void ShowContent(UIElement content)
    {
        if (!ReferenceEquals(_content, content))
        {
            if (_content is not null) Children.Remove(_content);
            _content = content;
            Children.Add(content);
        }

        _message.Visibility = Visibility.Collapsed;
        StopSpinnerAnimation();
        _spinner.Visibility = Visibility.Collapsed;
        _content.Visibility = Visibility.Visible;
    }

    private void ShowMessage(string text, uint color)
    {
        _message.Text = text;
        _message.Foreground = Ui.Brush(color);
        _message.Visibility = Visibility.Visible;
        StopSpinnerAnimation();
        _spinner.Visibility = Visibility.Collapsed;
        HideContent();
    }

    private void HideContent()
    {
        if (_content is not null) _content.Visibility = Visibility.Collapsed;
    }

    private void StartSpinnerAnimation()
    {
        _spinnerRotate.BeginAnimation(
            RotateTransform.AngleProperty,
            new DoubleAnimation(0, 360, TimeSpan.FromSeconds(0.75))
            {
                RepeatBehavior = RepeatBehavior.Forever,
            });
    }

    private void StopSpinnerAnimation()
    {
        _spinnerRotate.BeginAnimation(RotateTransform.AngleProperty, null);
        _spinnerRotate.Angle = 0;
    }

    private static (Border border, RotateTransform rotate) CreateSpinner()
    {
        const double size = 32;
        const double center = size / 2;

        var rotate = new RotateTransform { CenterX = center, CenterY = center };
        var border = new Border
        {
            Width = size,
            Height = size,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransform = rotate,
            Child = new Shapes.Ellipse
            {
                Stroke = Ui.Brush(0xC8C8C8),
                StrokeThickness = 3,
                Width = size,
                Height = size,
                StrokeDashArray = [5, 3],
                StrokeDashCap = PenLineCap.Round,
                RenderTransformOrigin = new Point(0.5, 0.5),
            },
        };

        return (border, rotate);
    }
}
