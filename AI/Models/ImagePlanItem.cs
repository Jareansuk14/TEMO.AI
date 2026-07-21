namespace TEMO.AI.Ai;

internal sealed record ImagePlanItem(
    string Id,
    string Label,
    string Group,
    string Role,
    string Src,
    string Alt,
    int Width,
    int Height,
    bool HasAlt);
