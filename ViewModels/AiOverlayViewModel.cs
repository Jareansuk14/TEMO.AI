namespace TEMO.AI;

internal sealed class AiOverlayViewModel
{
    public bool Busy { get; set; }

    public const string BillingLimitMessage = AiModels.BillingLimitMessage;

    public static bool IsBillingLimit(string? error) => OpenAiClient.IsBillingLimitReached(error);

    public string? LoadApiKey() => SettingsStore.LoadApiKey();

    public void SaveApiKey(string key) => SettingsStore.SaveApiKey(key);
}
