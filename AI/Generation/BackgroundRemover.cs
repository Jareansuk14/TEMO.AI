using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace TEMO.AI.Ai;

internal static class BackgroundRemover
{
    private const float Core = 32f;
    private const float Edge = 64f;
    private const byte CropAlphaThreshold = 10;

    public static byte[] Remove(byte[] bytes, bool autoCrop = true)
    {
        using var original = SixLabors.ImageSharp.Image.Load<Rgba32>(bytes);
        int w = original.Width, h = original.Height;
        if (w < 2 || h < 2) return bytes;

        var pixels = new Rgba32[w * h];
        original.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < h; y++)
                accessor.GetRowSpan(y).CopyTo(pixels.AsSpan(y * w, w));
        });

        var bgColors = DetectCornerColors(pixels, w, h);
        var alpha = BuildAlpha(pixels, w, h, bgColors);

        using var result = new Image<Rgba32>(w, h);
        result.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < h; y++)
            {
                var row = accessor.GetRowSpan(y);
                int off = y * w;
                for (int x = 0; x < w; x++)
                {
                    var p = pixels[off + x];
                    p.A = alpha[off + x];
                    row[x] = p;
                }
            }
        });

        if (autoCrop)
        {
            var bounds = GetSubjectBounds(alpha, w, h);
            if (bounds is { } rect && (rect.Width < w || rect.Height < h))
                result.Mutate(ctx => ctx.Crop(rect));
        }

        using var ms = new MemoryStream();
        result.SaveAsPng(ms);
        return ms.ToArray();
    }

    private static byte[] BuildAlpha(Rgba32[] pixels, int w, int h, Rgb[] bgColors)
    {
        var alpha = new byte[w * h];
        Array.Fill(alpha, (byte)255);

        if (bgColors.Length == 0)
            return alpha;

        for (int i = 0; i < pixels.Length; i++)
        {
            float m = MatchDistance(pixels[i], bgColors);
            if (m < Edge)
                alpha[i] = RampAlpha(m);
        }

        return alpha;
    }

    private static byte RampAlpha(float match)
    {
        if (match <= Core) return 0;
        if (match >= Edge) return 255;
        float t = (match - Core) / (Edge - Core);
        int a = (int)(t * 255f + 0.5f);
        return (byte)(a < 0 ? 0 : a > 255 ? 255 : a);
    }

    private static float MatchDistance(Rgba32 p, Rgb[] bgColors)
    {
        float best = float.MaxValue;
        for (int i = 0; i < bgColors.Length; i++)
        {
            float d = Dist(p.R, p.G, p.B, bgColors[i]);
            if (d < best) best = d;
        }
        return best;
    }

    private static float Dist(int r, int g, int b, Rgb c)
    {
        float dr = r - c.R, dg = g - c.G, db = b - c.B;
        return MathF.Sqrt(dr * dr + dg * dg + db * db);
    }

    private static Rgb[] DetectCornerColors(Rgba32[] pixels, int w, int h)
    {
        int patch = Math.Clamp((int)Math.Round(Math.Min(w, h) * 0.05), 4, 32);
        patch = Math.Min(patch, Math.Min(w, h));

        var found = new List<Rgb>();

        void SamplePatch(int startX, int startY)
        {
            int endX = Math.Min(startX + patch, w);
            int endY = Math.Min(startY + patch, h);
            for (int y = startY; y < endY; y++)
            {
                int off = y * w;
                for (int x = startX; x < endX; x++)
                {
                    var p = pixels[off + x];
                    var c = new Rgb(p.R, p.G, p.B);
                    if (found.Any(existing => Dist(c.R, c.G, c.B, existing) < 12f))
                        continue;
                    found.Add(c);
                }
            }
        }

        SamplePatch(0, 0);
        SamplePatch(w - patch, 0);

        return found.ToArray();
    }

    private static SixLabors.ImageSharp.Rectangle? GetSubjectBounds(byte[] alpha, int width, int height)
    {
        int minX = width, minY = height, maxX = -1, maxY = -1;
        for (int y = 0; y < height; y++)
        {
            int off = y * width;
            for (int x = 0; x < width; x++)
            {
                if (alpha[off + x] >= CropAlphaThreshold)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }
        if (maxX < 0) return null;
        return new SixLabors.ImageSharp.Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private readonly record struct Rgb(byte R, byte G, byte B);
}
