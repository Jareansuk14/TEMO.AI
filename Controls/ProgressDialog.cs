namespace TEMO.AI;

internal class ProgressDialog : Window
{
    protected readonly TextBlock TitleBlock;
    protected readonly TextBlock MessageBlock;
    protected readonly TextBlock DetailBlock;
    protected readonly System.Windows.Controls.ProgressBar Bar;
    protected readonly StackPanel Body;

    public ProgressDialog(
        string title, string message, string detail,
        double width = 460, double height = 230)
    {
        Width = width;
        Height = height;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.None;
        ShowInTaskbar = false;
        Ui.StyleDialog(this);

        Body = new StackPanel();

        TitleBlock = new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ui.Brush(0xF0F0F0),
        };
        Body.Children.Add(TitleBlock);

        MessageBlock = new TextBlock
        {
            Text = message,
            FontSize = 13,
            Foreground = Ui.Brush(0xA8A8A8),
            Margin = new Thickness(0, 12, 0, 16),
            TextWrapping = TextWrapping.Wrap,
        };
        Body.Children.Add(MessageBlock);

        Bar = new System.Windows.Controls.ProgressBar
        {
            IsIndeterminate = true,
            Height = 4,
            BorderThickness = new Thickness(0),
            Background = Ui.Brush(0x242424),
            Foreground = Ui.Brush(0xD8D8D8),
        };
        Body.Children.Add(Bar);

        DetailBlock = new TextBlock
        {
            Text = detail,
            FontSize = 11,
            Foreground = Ui.Brush(0x666666),
            Margin = new Thickness(0, 16, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        };
        Body.Children.Add(DetailBlock);

        Content = new Border
        {
            Background = Ui.Brush(0x101010),
            BorderBrush = Ui.Brush(0x2A2A2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(24, 22, 24, 22),
            Child = Body,
        };
    }
}
