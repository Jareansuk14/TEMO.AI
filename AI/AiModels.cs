namespace TEMO.AI.Ai;

internal static class AiModels
{
    public const string TextDefault = "gpt-5.5";
    public const string Image = "gpt-image-2";
    public const string ImageTransparent = "gpt-image-2";
    public const string CssDefault = "gpt-5.5";

    public const string BillingLimitMessage = "เครดิตของท่านหมดแล้ว กรุณาเติมเครดิต!!";

    public static string ResolveGpt(bool full, bool mini) =>
        mini ? "gpt-5-mini" : full ? "gpt-5" : TextDefault;
}
