namespace TEMO.AI.Ai;

internal static class PaletteStore
{
    public static readonly IReadOnlyList<ThemePalette> Presets = Build();

    private static ThemePalette[] Build()
    {
        var presets = new ThemePalette[100];
        for (var i = 0; i < presets.Length; i++)
        {
            var hue = i * 360.0 / presets.Length;
            var secondaryHue = hue + 140 + i % 3 * 20;

            presets[i] = new ThemePalette(
                $"Palette {i + 1:D3}",
                Hex(hue, 0.72, 0.90),
                Hex(secondaryHue, 0.66, 0.82),
                Hex(hue, 0.55, 0.98),
                Hex(hue, 0.85, 0.05),
                Hex(hue, 0.70, 0.11),
                Hex(hue, 0.15, 0.97),
                Hex(hue, 0.22, 0.78));
        }
        return presets;
    }

    private static string Hex(double hue, double saturation, double value) =>
        CssColor.ToHex(CssColor.FromHsv(hue, saturation, value, 255));

    public static ThemePalette Random(Random rng) => Presets[rng.Next(Presets.Count)];
}
