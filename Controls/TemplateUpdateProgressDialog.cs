namespace TEMO.AI;

internal sealed class TemplateUpdateProgressDialog : Window
{
    private readonly TextBlock _messageText;

    public TemplateUpdateProgressDialog()
    {
        Width = 440;
        Height = 180;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.None;
        ShowInTaskbar = false;
        Ui.StyleDialog(this);

        var frame = new Border
        {
            Background = Ui.Brush(0x101010),
            BorderBrush = Ui.Brush(0x2A2A2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(24, 22, 24, 22),
        };

        var body = new StackPanel();

        body.Children.Add(new TextBlock
        {
            Text = "Update Template",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ui.Brush(0xF0F0F0),
        });

        _messageText = new TextBlock
        {
            Text = "กำลังเตรียมข้อมูล...",
            FontSize = 13,
            Foreground = Ui.Brush(0xA8A8A8),
            Margin = new Thickness(0, 12, 0, 18),
            TextWrapping = TextWrapping.Wrap,
        };
        body.Children.Add(_messageText);

        body.Children.Add(new System.Windows.Controls.ProgressBar
        {
            IsIndeterminate = true,
            Height = 4,
            BorderThickness = new Thickness(0),
            Background = Ui.Brush(0x242424),
            Foreground = Ui.Brush(0xD8D8D8),
        });

        body.Children.Add(new TextBlock
        {
            Text = "กรุณารอสักครู่ ระบบจะปิดหน้าต่างนี้เมื่อเสร็จ",
            FontSize = 11,
            Foreground = Ui.Brush(0x666666),
            Margin = new Thickness(0, 16, 0, 0),
        });

        frame.Child = body;
        Content = frame;
    }

    public void SetMessage(string message)
    {
        _messageText.Text = message;
    }
}
