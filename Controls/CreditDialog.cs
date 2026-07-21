namespace TEMO.AI;

internal static class CreditDialog
{
    public static void Show(Window owner, string? subtitle = null)
    {
        var icon = new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(32),
            Background = Ui.Brush(0x2A1410),
            BorderBrush = Ui.Brush(0xE0533B),
            BorderThickness = new Thickness(2),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 18),
            Child = new TextBlock
            {
                Text = "!",
                FontSize = 36,
                FontWeight = FontWeights.Bold,
                Foreground = Ui.Brush(0xE0533B),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };

        var title = new TextBlock
        {
            Text = "เครดิตของท่านหมดแล้ว",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0xFFFFFF),
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
        };

        var subtitleBlock = new TextBlock
        {
            Text = subtitle ?? "กรุณาเติมเครดิตก่อนใช้งานต่อ",
            FontSize = 13,
            Foreground = Ui.Brush(0xB0B0B0),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 22),
        };

        var close = Ui.DialogButton("รับทราบ", accent: true);
        close.MinWidth = 140;
        close.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

        var panel = new StackPanel { Margin = new Thickness(30, 28, 30, 26) };
        panel.Children.Add(icon);
        panel.Children.Add(title);
        panel.Children.Add(subtitleBlock);
        panel.Children.Add(close);

        var dialog = new Window
        {
            Title = "เครดิตหมด",
            Width = 440,
            SizeToContent = SizeToContent.Height,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            Content = panel,
        };
        Ui.StyleDialog(dialog);
        close.Click += (_, _) => dialog.Close();
        dialog.ShowDialog();
    }
}
