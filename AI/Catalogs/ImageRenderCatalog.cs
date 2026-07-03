namespace TEMO.AI.Ai;

internal static class ImageRenderCatalog
{
    public static readonly ImageStyle[] Realistic =
    [
        new("Studio portrait", "Studio portrait"),
        new("Street photography", "Street photography"),
        new("Action freeze frame", "Action freeze frame"),
        new("Cyberpunk neon", "Cyberpunk neon"),
        new("สดใส ร่าเริง", "สดใส ร่าเริง"),
        new("เคร่งขรึม น่าเกรงขาม", "เคร่งขรึม น่าเกรงขาม"),
        new("Fashion", "Fashion"),
        new("ป้ายโฆษณา", "ป้ายโฆษณา"),
        new("Portrait ศิลปะ", "Portrait ศิลปะ"),
        new("ไลฟ์สไตล์", "ไลฟ์สไตล์"),
        new("โปรไฟล์ธุรกิจ", "โปรไฟล์ธุรกิจ"),
        new("ความงามธรรมชาติ", "ความงามธรรมชาติ"),
        new("สง่าผ่าเผย", "สง่าผ่าเผย"),
    ];

    public static readonly ImageStyle[] Stylized =
    [
        new("Chibi", "Chibi"),
        new("Kawaii", "Kawaii"),
        new("Disney Animation", "Disney Animation"),
        new("Pixar 3D", "Pixar 3D"),
        new("DreamWorks Style", "DreamWorks Style"),
        new("Illumination Style", "Illumination Style"),
        new("Western comic", "Western comic"),
        new("Manhwa style", "Manhwa style"),
        new("Manhua style", "Manhua style"),
        new("Plastic Toy Render Fortnite Style", "Plastic Toy Render Fortnite Style"),
        new("Genshin Impact Style", "Genshin Impact Style"),
        new("Honkai Star Rail Style", "Honkai Star Rail Style"),
        new("Toon shading", "Toon shading"),
        new("Water color", "Water color"),
        new("Colored pencils", "Colored pencils"),
        new("Oil painting", "Oil painting"),
        new("Pastel", "Pastel"),
        new("Ink blush", "Ink blush"),
        new("Storybook", "Storybook"),
        new("Fantasy", "Fantasy"),
        new("Cyberpunk", "Cyberpunk"),
        new("Steampunk", "Steampunk"),
        new("Pop art", "Pop art"),
        new("Japanese woodblock", "Japanese woodblock"),
        new("Thai traditional painting", "Thai traditional painting"),
        new("Byzantine icon", "Byzantine icon"),
        new("Pop surrealism", "Pop surrealism"),
        new("Cybernetic/Techwar style", "Cybernetic/Techwar style"),
        new("Biomachanical art", "Biomachanical art"),
        new("Bieampunk fantasy", "Bieampunk fantasy"),
        new("Native American style", "Native American style"),
        new("Traffic street art", "Traffic street art"),
    ];

    public static readonly ImageStyle[] All = [.. Realistic, .. Stylized];

    private const string KeyRealistic = "render-realistic";
    private const string KeyStylized  = "render-stylized";

    public static ImageStyle Random(Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        var (pool, key) = rng.Next(2) == 0 ? (Realistic, KeyRealistic) : (Stylized, KeyStylized);
        return PromptUsedStore.Pick(key, pool, s => s.Name, rng, 1)[0];
    }

    public static ImageStyle RandomRealistic(Random rng) =>
        PromptUsedStore.Pick(KeyRealistic, Realistic, s => s.Name, rng, 1)[0];

    public static ImageStyle RandomStylized(Random rng) =>
        PromptUsedStore.Pick(KeyStylized, Stylized, s => s.Name, rng, 1)[0];

    public static bool IsRealistic(ImageStyle render) => Array.IndexOf(Realistic, render) >= 0;
}
