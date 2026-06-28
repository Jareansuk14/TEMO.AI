namespace TEMO.AI.Ai;

internal static class ContentTypes
{
    public static AiPromptType FromRadios(bool lottery, bool slot) =>
        lottery ? AiPromptType.Lottery
        : slot ? AiPromptType.Slot
        : AiPromptType.Casino;
}
