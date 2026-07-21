namespace TEMO.AI;

internal sealed class PreviewWindow : Window
{
    public PreviewWindow(string imagePath, string title)
    {
        Title = $"พรีวิว — {title}";
        Width = 560;
        Height = 860;
        Ui.StyleDialog(this);

        var root = new DockPanel();

        var bar = new Border
        {
            Background = Ui.Brush(0x0D0D0D),
            BorderBrush = Ui.Brush(0x1E1E1E),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 10, 12, 10),
        };
        DockPanel.SetDock(bar, Dock.Top);

        var barGrid = new Grid();
        barGrid.ColumnDefinitions.Add(new ColumnDefinition());
        barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var heading = new TextBlock
        {
            Text = title,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0xEDEDED),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(heading, 0);

        var closeBtn = Ui.DialogButton("✕  ปิด", accent: false);
        closeBtn.Click += (_, _) => Close();
        Grid.SetColumn(closeBtn, 1);

        barGrid.Children.Add(heading);
        barGrid.Children.Add(closeBtn);
        bar.Child = barGrid;
        root.Children.Add(bar);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = Ui.Brush(0x070707),
        };

        scroll.Content = new System.Windows.Controls.Image
        {
            Stretch = System.Windows.Media.Stretch.Uniform,
            StretchDirection = StretchDirection.Both,
            Source = Ui.LoadBitmap(imagePath),
        };
        root.Children.Add(scroll);

        Content = root;
    }
}
