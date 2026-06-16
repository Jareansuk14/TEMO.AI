namespace TEMO.AI;

internal static class NodeInstallUi
{
    public static bool RunInstall(string script)
    {
        var window = BuildWindow();
        window.Show();

        var success = false;
        var frame = new DispatcherFrame();

        Task.Run(() =>
        {
            NodeRuntime.RunInstaller(script);
            success = NodeRuntime.IsAvailable() || NodeRuntime.IsInstalledOnDisk();
        }).ContinueWith(_ => window.Dispatcher.BeginInvoke(() => frame.Continue = false));

        Dispatcher.PushFrame(frame);
        window.Close();
        return success;
    }

    private static Window BuildWindow()
    {
        var window = new Window
        {
            Title = "TEMO.AI",
            Width = 460,
            Height = 230,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            Topmost = true,
        };
        Ui.StyleDialog(window);
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

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
            Text = "กำลังติดตั้งโปรแกรม",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ui.Brush(0xF0F0F0),
        });

        body.Children.Add(new TextBlock
        {
            Text = "กรุณารอสักครู่ ระบบกำลังติดตั้งส่วนประกอบที่จำเป็น...",
            FontSize = 13,
            Foreground = Ui.Brush(0xA8A8A8),
            Margin = new Thickness(0, 12, 0, 16),
            TextWrapping = TextWrapping.Wrap,
        });

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
            Text = "ห้ามปิดหน้าต่างนี้จนกว่าจะติดตั้งเสร็จ",
            FontSize = 11,
            Foreground = Ui.Brush(0x666666),
            Margin = new Thickness(0, 16, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        });

        frame.Child = body;
        window.Content = frame;
        return window;
    }
}
