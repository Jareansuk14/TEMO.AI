namespace TEMO.AI.Ai;

internal static class ImageSizeCatalog
{
    public static (int Width, int Height) Size(string id) => id switch
    {
        "banner" => (1600, 900),
        "logo" => (512, 512),
        "background" => (1920, 1080),
        "play" => (512, 240),
        var s when s.StartsWith("btn-") => (ImagesStore.ButtonWidth, ImagesStore.ButtonHeight),
        var s when s.StartsWith("game-") => (ImagesStore.GameWidth, ImagesStore.GameHeight),
        var s when s.StartsWith("promo-") => (1536, 864),
        var s when s.StartsWith("seo-") => (1536, 864),
        _ => ImageSpecRegistry.Size(id) ?? ImageGroupCatalog.Size(id) ?? (1536, 864),
    };
}
