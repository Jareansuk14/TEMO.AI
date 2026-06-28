namespace TEMO.AI;

public partial class MainWindow
{
    private static bool ToggleFlyout(FrameworkElement panel)
    {
        var open = panel.Visibility != Visibility.Visible;
        panel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        return open;
    }

    private void CollapseCssFlyouts()
    {
        CssImportPanel.Visibility = Visibility.Collapsed;
        CssAiPanel.Visibility = Visibility.Collapsed;
        ContentAiPanel.Visibility = Visibility.Collapsed;
        ImgAiPanel.Visibility = Visibility.Collapsed;
    }

    private void CollapseAllFlyouts()
    {
        CollapseCssFlyouts();
        ApiKeyPanel.Visibility = Visibility.Collapsed;
    }
}
