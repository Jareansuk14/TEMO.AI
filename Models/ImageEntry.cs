namespace TEMO.AI;

internal sealed class ImageEntry
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public string Group { get; init; } = "";
    public bool HasAlt { get; init; }

    public string SrcValue { get; set; } = "";
    public string AltValue { get; set; } = "";
    public string OriginalSrc { get; set; } = "";
}
