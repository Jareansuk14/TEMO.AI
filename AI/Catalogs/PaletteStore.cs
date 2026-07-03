namespace TEMO.AI.Ai;

internal static class PaletteStore
{
    private const string HueCatalog = "palette-hue";
    private const string PresetCatalog = "palette-preset";

    private sealed record Hue(string Name, double Degree);

    private sealed record Tone(string Name, double PrimarySat, double PrimaryVal, double AccentSat, double AccentVal);

    private sealed record Scheme(string Name, double SecondaryOffset);

    private static readonly Hue[] Hues =
    [
        new("Red", 0),
        new("Orange", 30),
        new("Yellow", 60),
        new("Chartreuse", 90),
        new("Green", 120),
        new("Spring", 150),
        new("Cyan", 180),
        new("Azure", 210),
        new("Blue", 240),
        new("Violet", 270),
        new("Magenta", 300),
        new("Rose", 330),
    ];

    private static readonly Tone[] Tones =
    [
        new("Vivid", 0.78, 0.92, 0.70, 1.00),
        new("Neon", 0.92, 0.98, 0.96, 1.00),
        new("Deep", 0.86, 0.64, 0.80, 0.86),
        new("Jewel", 0.74, 0.80, 0.68, 0.96),
        new("Pastel", 0.42, 0.96, 0.52, 0.99),
        new("Muted", 0.48, 0.70, 0.56, 0.84),
    ];

    // ความสัมพันธ์ของสีรองเทียบกับสีหลักบนวงล้อ
    private static readonly Scheme[] Schemes =
    [
        new("Complementary", 180),
        new("SplitA", 150),
        new("SplitB", 210),
        new("Triadic", 120),
        new("Analogous", 40),
    ];

    public static ThemePalette Random(Random rng) =>
        rng.Next(2) == 0 ? FromWheel(rng) : FromPreset(rng);

    private static ThemePalette FromWheel(Random rng)
    {
        var hue = PromptUsedStore.Pick(HueCatalog, Hues, h => h.Name, rng, 1)[0];
        var tone = Tones[rng.Next(Tones.Length)];
        var scheme = Schemes[rng.Next(Schemes.Length)];

        var primaryHue = hue.Degree;
        var secondaryHue = primaryHue + scheme.SecondaryOffset;

        return new ThemePalette(
            $"{hue.Name} {tone.Name}",
            Hex(primaryHue, tone.PrimarySat, tone.PrimaryVal),
            Hex(secondaryHue, tone.PrimarySat, tone.PrimaryVal),
            Hex(primaryHue, tone.AccentSat, tone.AccentVal),
            Hex(primaryHue, 0.85, 0.05),
            Hex(primaryHue, 0.70, 0.11),
            Hex(primaryHue, 0.12, 0.97),
            Hex(primaryHue, 0.20, 0.78));
    }

    private static ThemePalette FromPreset(Random rng)
    {
        var set = PromptUsedStore.Pick(PresetCatalog, PresetPaletteCatalog.All, PresetPaletteCatalog.KeyOf, rng, 1)[0];
        CssColor.TryParse(set.Primary, out var c);
        CssColor.ToHsv(c, out var h, out _, out _);

        return new ThemePalette(
            "Preset",
            set.Primary,
            set.Secondary,
            set.Accent,
            Hex(h, 0.85, 0.05),
            Hex(h, 0.70, 0.11),
            Hex(h, 0.12, 0.97),
            Hex(h, 0.20, 0.78));
    }

    private static string Hex(double hue, double saturation, double value) =>
        CssColor.ToHex(CssColor.FromHsv(hue, saturation, value, 255));
}
