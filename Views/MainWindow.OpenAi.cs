namespace TEMO.AI;

public partial class MainWindow
{
    private bool _aiBusy;
    private DispatcherTimer? _dotTimer;
    private int _dotCount;

    private static string ResolveGptModel(bool full, bool mini) =>
        mini ? "gpt-5-mini" : full ? "gpt-5" : "gpt-5.5";

    private string ContentGptModel =>
        ResolveGptModel(ModelGpt5.IsChecked == true, ModelGpt5Mini.IsChecked == true);

    private string CssGptModel =>
        ResolveGptModel(CssModelGpt5.IsChecked == true, CssModelGpt5Mini.IsChecked == true);

    private bool TryGetApiKey(out string apiKey)
    {
        apiKey = AiKeyBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(apiKey)) return true;
        ShowMsg("กรุณาตั้งค่า OpenAI API Key ก่อน (กด 🔑)");
        ApiKeyPanel.Visibility = Visibility.Visible;
        return false;
    }

    private async Task<string?> RequestOpenAiAsync(string model, object messageContent)
    {
        if (_aiBusy) { ShowMsg("AI กำลังทำงานอยู่"); return null; }
        if (!TryGetApiKey(out var apiKey)) return null;

        _aiBusy = true;
        ShowAiOverlayLoading();
        try
        {
            var (ok, text, _, error) = await OpenAiClient.ChatAsync(apiKey, model, messageContent);
            if (!ok)
            {
                ShowAiOverlayError(error ?? "Unknown error");
                ShowMsg("เกิดข้อผิดพลาดในการเรียก AI");
                return null;
            }
            return text;
        }
        catch (Exception ex)
        {
            ShowAiOverlayError($"❌ {ex.GetType().Name}: {ex.Message}");
            ShowMsg("เกิดข้อผิดพลาดในการเรียก AI");
            return null;
        }
        finally
        {
            _aiBusy = false;
        }
    }

    private async Task RunAiApplyAsync(
        Button button, string model, object messageContent,
        Func<string, int> apply, UIElement panelToClose,
        Func<int, string> successMessage, string noMatchMessage)
    {
        button.IsEnabled = false;
        try
        {
            var text = await RequestOpenAiAsync(model, messageContent);
            if (text is null) return;

            var applied = apply(text);
            if (applied > 0)
            {
                HideAiOverlay();
                panelToClose.Visibility = Visibility.Collapsed;
                SaveAll_Click(null!, null!);
                ShowMsg(successMessage(applied));
            }
            else
            {
                ShowAiOverlayError(text);
                ShowMsg(noMatchMessage);
            }
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private void ShowAiOverlayLoading()
    {
        _dotCount = 0;
        AiOverlayStatusText.Text = "AI กำลังทำงาน";
        AiOverlayLoading.Visibility = Visibility.Visible;
        AiOverlayError.Visibility = Visibility.Collapsed;
        AiOverlay.Visibility = Visibility.Visible;

        _dotTimer?.Stop();
        _dotTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _dotTimer.Tick += (_, _) =>
        {
            _dotCount = (_dotCount + 1) % 4;
            AiOverlayStatusText.Text = "AI กำลังทำงาน" + new string('.', _dotCount);
        };
        _dotTimer.Start();
    }

    private void ShowAiOverlayError(string msg)
    {
        _dotTimer?.Stop();
        AiOverlayLoading.Visibility = Visibility.Collapsed;
        AiOverlayError.Visibility = Visibility.Visible;
        AiOverlayResponseBox.Text = msg;
        AiOverlay.Visibility = Visibility.Visible;
    }

    private void HideAiOverlay()
    {
        _dotTimer?.Stop();
        AiOverlay.Visibility = Visibility.Collapsed;
    }

    private void AiOverlayOk_Click(object sender, RoutedEventArgs e) => HideAiOverlay();

    internal void LoadApiKey()
    {
        var key = SettingsStore.LoadApiKey();
        if (!string.IsNullOrEmpty(key))
            AiKeyBox.Text = key;
    }

    private void ApiKeyToggle_Click(object sender, RoutedEventArgs e) =>
        ApiKeyPanel.Visibility = ApiKeyPanel.Visibility == Visibility.Collapsed
            ? Visibility.Visible : Visibility.Collapsed;

    private void ApiKeySave_Click(object sender, RoutedEventArgs e)
    {
        var key = AiKeyBox.Text.Trim();
        if (!string.IsNullOrEmpty(key)) SettingsStore.SaveApiKey(key);
        ApiKeyPanel.Visibility = Visibility.Collapsed;
        ShowMsg("🔑  บันทึก API Key แล้ว");
    }

    private void ApiKeyClose_Click(object sender, RoutedEventArgs e) =>
        ApiKeyPanel.Visibility = Visibility.Collapsed;
}
