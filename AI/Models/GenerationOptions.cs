namespace TEMO.AI.Ai;

internal sealed record GenerationOptions(
    string Brand,
    AiPromptType ContentType,
    int? GameCardCount,
    string TextModel,
    string ImageModel = AiModels.Image,
    string CssModel = AiModels.CssDefault,
    ImageStyle? Style = null,
    ImageStyle? Render = null);
