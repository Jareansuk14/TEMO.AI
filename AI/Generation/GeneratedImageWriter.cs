namespace TEMO.AI.Ai;

internal static class GeneratedImageWriter
{
    public static void Save(string projectPath, ImagePlanItem item, byte[] bytes, string alt)
    {
        var src = string.IsNullOrWhiteSpace(item.Src) ? $"/images/{item.Id}.webp" : EnsureWebp(item.Src);
        var target = ProjectPaths.Public(projectPath, src);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        if (IsResized(item.Id))
        {
            SaveCover(bytes, target, item.Width, item.Height);
            ImagesStore.SaveEntry(projectPath, item.Src, src, alt, item.HasAlt, item.Id);
        }
        else
        {
            SaveOriginal(bytes, target);
            ImagesStore.SaveEntry(projectPath, item.Src, src, alt, item.HasAlt, item.Id);
        }
    }

    public static void SaveButtons(
        string projectPath,
        IReadOnlyList<(ImagePlanItem Item, byte[] Bytes, string Alt)> buttons)
    {
        if (buttons.Count == 0) return;

        var trimmed = buttons
            .Select(b => (b.Item, b.Alt, Bitmap: TrimToWidth(b.Bytes, b.Item.Width)))
            .ToList();
        try
        {
            var maxHeight = trimmed.Max(t => t.Bitmap.Height);
            foreach (var (item, alt, bitmap) in trimmed)
            {
                var src = string.IsNullOrWhiteSpace(item.Src) ? $"/images/{item.Id}.webp" : EnsureWebp(item.Src);
                var target = ProjectPaths.Public(projectPath, src);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);

                ComposeCentered(bitmap, target, item.Width, maxHeight);
                ImagesStore.SaveEntry(projectPath, item.Src, src, alt, item.HasAlt, item.Id, item.Width, maxHeight);
            }
        }
        finally
        {
            foreach (var t in trimmed) t.Bitmap.Dispose();
        }
    }

    private static bool IsResized(string id) =>
        id is "background" or "logo" or "banner";

    private static string EnsureWebp(string src)
    {
        var slash = src.LastIndexOf('/');
        var dir = slash >= 0 ? src[..slash] : "";
        var name = slash >= 0 ? src[(slash + 1)..] : src;
        var dot = name.LastIndexOf('.');
        var stem = dot >= 0 ? name[..dot] : name;
        return string.IsNullOrEmpty(dir) ? $"{stem}.webp" : $"{dir}/{stem}.webp";
    }

    private static readonly SkiaSharp.SKSamplingOptions HighQuality =
        new(SkiaSharp.SKCubicResampler.Mitchell);

    private static void SaveOriginal(byte[] bytes, string target)
    {
        using var bitmap = SkiaSharp.SKBitmap.Decode(bytes);
        if (bitmap is null) throw new InvalidOperationException("AI image decode failed");
        WriteWebp(bitmap, target);
    }

    private static void SaveCover(byte[] bytes, string target, int width, int height)
    {
        using var bitmap = SkiaSharp.SKBitmap.Decode(bytes);
        if (bitmap is null) throw new InvalidOperationException("AI image decode failed");

        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SkiaSharp.SKColors.Transparent);

        var scale = Math.Max(width / (float)bitmap.Width, height / (float)bitmap.Height);
        var drawW = bitmap.Width * scale;
        var drawH = bitmap.Height * scale;
        var dest = new SkiaSharp.SKRect(
            (width - drawW) / 2f, (height - drawH) / 2f,
            (width + drawW) / 2f, (height + drawH) / 2f);

        using var paint = new SkiaSharp.SKPaint { IsAntialias = true };
        using var source = SkiaSharp.SKImage.FromBitmap(bitmap);
        canvas.DrawImage(source, dest, HighQuality, paint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Webp, 95);
        using var stream = File.Create(target);
        data.SaveTo(stream);
    }

    private static SkiaSharp.SKBitmap TrimToWidth(byte[] bytes, int targetWidth)
    {
        using var decoded = SkiaSharp.SKBitmap.Decode(bytes);
        if (decoded is null) throw new InvalidOperationException("AI image decode failed");

        var bounds = AlphaBounds(decoded);
        using var cropped = new SkiaSharp.SKBitmap(bounds.Width, bounds.Height);
        var source = decoded.ExtractSubset(cropped, bounds) ? cropped : decoded;

        var height = Math.Max(1, (int)Math.Round(targetWidth * source.Height / (double)source.Width));
        var info = new SkiaSharp.SKImageInfo(targetWidth, height);
        return source.Resize(info, HighQuality) ?? source.Copy();
    }

    // วางเนื้อหาปุ่มกึ่งกลางบนผืนผ้าใบโปร่งใสขนาด width x height
    private static void ComposeCentered(SkiaSharp.SKBitmap content, string target, int width, int height)
    {
        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SkiaSharp.SKColors.Transparent);

        var x = (width - content.Width) / 2f;
        var y = (height - content.Height) / 2f;
        using var paint = new SkiaSharp.SKPaint { IsAntialias = true };
        using var image = SkiaSharp.SKImage.FromBitmap(content);
        canvas.DrawImage(image, x, y, HighQuality, paint);

        using var snap = surface.Snapshot();
        using var data = snap.Encode(SkiaSharp.SKEncodedImageFormat.Webp, 95);
        using var stream = File.Create(target);
        data.SaveTo(stream);
    }

    private static void WriteWebp(SkiaSharp.SKBitmap bitmap, string target)
    {
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Webp, 95);
        using var stream = File.Create(target);
        data.SaveTo(stream);
    }

    private static SkiaSharp.SKRectI AlphaBounds(SkiaSharp.SKBitmap bitmap, byte threshold = 10)
    {
        int w = bitmap.Width, h = bitmap.Height;
        int minX = w, minY = h, maxX = -1, maxY = -1;

        var pixels = bitmap.Pixels;
        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                if (pixels[row + x].Alpha <= threshold) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < 0) return new SkiaSharp.SKRectI(0, 0, w, h);
        return new SkiaSharp.SKRectI(minX, minY, maxX + 1, maxY + 1);
    }
}
