namespace TEMO.AI;

public partial class MainWindow
{
    private static bool ToggleFlyout(FrameworkElement panel)
    {
        var open = panel.Visibility != Visibility.Visible;
        panel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        return open;
    }

    private void CollapseContentFlyouts()
    {
        AiPanel.Visibility = Visibility.Collapsed;
    }

    private void CollapseCssFlyouts()
    {
        CssImportPanel.Visibility = Visibility.Collapsed;
        CssAiPanel.Visibility = Visibility.Collapsed;
    }

    private void CollapseAllFlyouts()
    {
        CollapseContentFlyouts();
        CollapseCssFlyouts();
        ApiKeyPanel.Visibility = Visibility.Collapsed;
    }
}
