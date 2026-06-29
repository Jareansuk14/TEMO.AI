namespace TEMO.AI;

public partial class MainWindow
{
    private DispatcherTimer? _dotTimer;
    private int _dotCount;

    private string CssGptModel =>
        AiModels.ResolveGpt(CssModelGpt5.IsChecked == true, CssModelGpt5Mini.IsChecked == true);

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
        if (_vm.Ai.Busy) { ShowMsg("AI กำลังทำงานอยู่"); return null; }
        if (!TryGetApiKey(out var apiKey)) return null;

        _vm.Ai.Busy = true;
        ShowAiOverlayLoading();
        try
        {
            var (ok, text, _, error) = await OpenAiClient.ChatAsync(apiKey, model, messageContent);
            if (!ok)
            {
                ShowAiError(error, "Unknown error");
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
            _vm.Ai.Busy = false;
        }
    }

    private async Task RunAiApplyAsync(
        Button button, string model, object messageContent,
        Func<string, int> apply, UIElement panelToClose,
        Func<int, string> successMessage, string noMatchMessage,
        GenerationLog? genLog = null)
    {
        button.IsEnabled = false;
        try
        {
            var text = await RequestOpenAiAsync(model, messageContent);
            if (text is null) { genLog?.Finish(false, "เรียก AI ล้มเหลว"); return; }

            genLog?.Block("ตอบกลับ", text);
            var applied = apply(text);
            if (applied > 0)
            {
                genLog?.Finish(true, successMessage(applied));
                HideAiOverlay();
                panelToClose.Visibility = Visibility.Collapsed;
                SaveAll_Click(null!, null!);
                ShowMsg(successMessage(applied));
            }
            else
            {
                genLog?.Finish(false, noMatchMessage);
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

    private void ShowAiError(string? error, string fallback)
    {
        if (AiOverlayViewModel.IsBillingLimit(error))
        {
            HideAiOverlay();
            CreditDialog.Show(this);
            ShowMsg(AiModels.BillingLimitMessage);
        }
        else
        {
            ShowAiOverlayError(error ?? fallback);
            ShowMsg("เกิดข้อผิดพลาดในการเรียก AI");
        }
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
        var key = _vm.Ai.LoadApiKey();
        if (!string.IsNullOrEmpty(key))
            AiKeyBox.Text = key;
    }

    private void ApiKeyToggle_Click(object sender, RoutedEventArgs e) =>
        ApiKeyPanel.Visibility = ApiKeyPanel.Visibility == Visibility.Collapsed
            ? Visibility.Visible : Visibility.Collapsed;

    private void ApiKeySave_Click(object sender, RoutedEventArgs e)
    {
        var key = AiKeyBox.Text.Trim();
        if (!string.IsNullOrEmpty(key)) _vm.Ai.SaveApiKey(key);
        ApiKeyPanel.Visibility = Visibility.Collapsed;
        ShowMsg("🔑  บันทึก API Key แล้ว");
    }

    private void ApiKeyClose_Click(object sender, RoutedEventArgs e) =>
        ApiKeyPanel.Visibility = Visibility.Collapsed;
}
