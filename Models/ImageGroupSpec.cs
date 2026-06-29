namespace TEMO.AI;

internal sealed class ImageGroupSpec
{
    public string Key { get; set; } = "";

    public string TsConst { get; set; } = "";

    public string Structure { get; set; } = "named";

    public List<string> UsageTokens { get; set; } = [];

    public string Role { get; set; } = "";

    public string Group { get; set; } = "";

    public string Label { get; set; } = "";

    public bool HasAlt { get; set; } = true;

    public bool Transparent { get; set; }

    public string ImageType { get; set; } = "";

    public bool UseLogoReference { get; set; }

    public string CaptionSource { get; set; } = "";

    public int CompositionMin { get; set; }

    public int CompositionMax { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public string Ratio { get; set; } = "";

    public string ResolvedImageType =>
        !string.IsNullOrWhiteSpace(ImageType) ? ImageType
        : Transparent ? "transparent"
        : "normal";
}
