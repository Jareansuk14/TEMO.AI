namespace TEMO.AI;

internal sealed class AppUpdateDialog : Window
{
    private readonly TextBlock _titleText;
    private readonly TextBlock _messageText;
    private readonly TextBlock _detailText;
    private readonly System.Windows.Controls.ProgressBar _progressBar;
    private readonly Button _closeButton;

    public AppUpdateDialog()
    {
        Width = 460;
        Height = 230;
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

        _titleText = new TextBlock
        {
            Text = "Checking for updates",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ui.Brush(0xF0F0F0),
        };
        body.Children.Add(_titleText);

        _messageText = new TextBlock
        {
            Text = "กำลังตรวจสอบเวอร์ชันล่าสุด...",
            FontSize = 13,
            Foreground = Ui.Brush(0xA8A8A8),
            Margin = new Thickness(0, 12, 0, 16),
            TextWrapping = TextWrapping.Wrap,
        };
        body.Children.Add(_messageText);

        _progressBar = new System.Windows.Controls.ProgressBar
        {
            IsIndeterminate = true,
            Height = 4,
            BorderThickness = new Thickness(0),
            Background = Ui.Brush(0x242424),
            Foreground = Ui.Brush(0xD8D8D8),
        };
        body.Children.Add(_progressBar);

        _detailText = new TextBlock
        {
            Text = "ต้องเชื่อมต่ออินเทอร์เน็ตเพื่อเข้าใช้งาน",
            FontSize = 11,
            Foreground = Ui.Brush(0x666666),
            Margin = new Thickness(0, 16, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        };
        body.Children.Add(_detailText);

        _closeButton = Ui.DialogButton("ปิดโปรแกรม", accent: false);
        _closeButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
        _closeButton.Margin = new Thickness(0, 18, 0, 0);
        _closeButton.Visibility = Visibility.Collapsed;
        _closeButton.Click += (_, _) => Close();
        body.Children.Add(_closeButton);

        frame.Child = body;
        Content = frame;
    }

    public void SetChecking()
    {
        _titleText.Text = "Checking for updates";
        _messageText.Text = "กำลังตรวจสอบเวอร์ชันล่าสุด...";
        _detailText.Text = "ต้องเชื่อมต่ออินเทอร์เน็ตเพื่อเข้าใช้งาน";
        _progressBar.IsIndeterminate = true;
        _progressBar.Value = 0;
        _closeButton.Visibility = Visibility.Collapsed;
    }

    public void SetDownloading(int percent)
    {
        _titleText.Text = "Update required";
        _messageText.Text = $"กำลังดาวน์โหลดอัปเดต {percent}%";
        _detailText.Text = "โปรแกรมจะติดตั้งและเปิดใหม่อัตโนมัติเมื่อดาวน์โหลดเสร็จ";
        _progressBar.IsIndeterminate = false;
        _progressBar.Value = Math.Clamp(percent, 0, 100);
        _closeButton.Visibility = Visibility.Collapsed;
    }

    public void SetApplying()
    {
        _titleText.Text = "Installing update";
        _messageText.Text = "กำลังติดตั้งอัปเดตและเปิดโปรแกรมใหม่...";
        _detailText.Text = "กรุณารอสักครู่";
        _progressBar.IsIndeterminate = true;
        _closeButton.Visibility = Visibility.Collapsed;
    }

    public void SetBlocked(string message)
    {
        _titleText.Text = "Update required";
        _messageText.Text = message;
        _detailText.Text = "ยังไม่สามารถเข้าโปรแกรมได้จนกว่าจะตรวจสอบหรืออัปเดตสำเร็จ";
        _progressBar.IsIndeterminate = false;
        _progressBar.Value = 0;
        _closeButton.Visibility = Visibility.Visible;
    }
}
