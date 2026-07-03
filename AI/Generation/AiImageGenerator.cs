namespace TEMO.AI.Ai;

internal static class AiImageGenerator
{
    private const string TransparentModel = AiModels.ImageTransparent;
    private const int MaxModerationRetries = 3;
    private const int MaxConcurrency = 5;

    public static async Task<(bool Ok, string? Error)> GenerateAsync(
        string projectPath,
        IReadOnlyList<ImagePlanItem> plan,
        GenerationOptions options,
        ThemePalette palette,
        string apiKey,
        Action<string> log,
        CancellationToken ct = default,
        GenerationLog? genLog = null,
        UsageTracker? usage = null)
    {
        var ctx = new GenContext(projectPath, options, palette, apiKey, log, ct, genLog, usage);

        var ordered = plan
            .OrderBy(p => p.Id == "logo" ? 0 : 1)
            .ThenBy(p => p.Id, StringComparer.Ordinal)
            .ToList();

        ctx.Promos = AssignPromos(ordered, ctx.Rng);
        ctx.Slogans = AssignSlogans(ordered, ctx.Rng);
        ctx.Realistic = ImageRenderCatalog.IsRealistic(options.Render ?? ImageRenderCatalog.All[0]);
        ctx.MainCast = CompositionCatalog.PickMainCast(ctx.Rng, ctx.Realistic);
        ctx.GameMascot = ActiveGame.IsMascot(projectPath);
        AssignGame(ctx, ordered);

        ctx.GenLog?.Section("AI รูป — เตรียมการสุ่ม");
        ctx.GenLog?.Line($"จำนวนรูปทั้งหมด: {ordered.Count}");
        ctx.GenLog?.Line($"Concurrency สูงสุด: {MaxConcurrency}");
        ctx.GenLog?.Block("ตัวละครหลัก (MainCast)", ctx.MainCast.Count == 0 ? "(ไม่มี)" : string.Join("\n", ctx.MainCast));
        if (ctx.GameChars.Count > 0)
            ctx.GenLog?.Block($"เกม (mascot={ctx.GameMascot})",
                string.Join("\n", ctx.GameChars.Select(g => $"{g.Key}: {g.Value} + provider {(ctx.GameRefs.ContainsKey(g.Key) ? "yes" : "no")}")));
        ctx.GenLog?.Block("Promo ที่สุ่มได้", ctx.Promos.Count == 0 ? "(ไม่มี)" : string.Join("\n", ctx.Promos.Select(p => $"{p.Key}: {p.Value}")));
        ctx.GenLog?.Block("Slogan ที่สุ่มได้", ctx.Slogans.Count == 0 ? "(ไม่มี)" : string.Join("\n", ctx.Slogans.Select(p => $"{p.Key}: {p.Value}")));

        var logoItem = ordered.FirstOrDefault(p => p.Id == "logo");
        var firstButton = ordered.FirstOrDefault(p => p.Id.StartsWith("btn-", StringComparison.Ordinal));
        var remaining = ordered.Where(p => p != logoItem && p != firstButton).ToList();

        if (logoItem is not null)
        {
            ctx.GenLog?.Section("ระยะ 1 — logo (บังคับ)");
            var r = await GenSingleAsync(ctx, logoItem);
            if (!r.Ok)
            {
                ctx.GenLog?.Error($"logo ล้มเหลว: {r.Error}");
                return (false, $"logo: {r.Error}");
            }
            ctx.LogoReference = ToPng(r.Bytes!);
            GeneratedImageWriter.Save(ctx.ProjectPath, logoItem, r.Bytes!, ImagePromptCatalog.Alt(logoItem, ctx.Options, r.Caption));
            ctx.GenLog?.Line("logo สำเร็จ — เก็บเป็น reference");
        }

        if (firstButton is not null)
        {
            ctx.GenLog?.Section($"ระยะ 1 — {firstButton.Id} (บังคับ, เป็น reference ปุ่มอื่น)");
            var r = await GenSingleAsync(ctx, firstButton);
            if (!r.Ok)
            {
                ctx.GenLog?.Error($"{firstButton.Id} ล้มเหลว: {r.Error}");
                return (false, $"{firstButton.Id}: {r.Error}");
            }
            ctx.ButtonReference = ToPng(r.Bytes!);
            ctx.Buttons.Add((firstButton, r.Bytes!, ImagePromptCatalog.Alt(firstButton, ctx.Options)));
            ctx.GenLog?.Line($"{firstButton.Id} สำเร็จ — เก็บเป็น button reference");
        }

        ctx.GenLog?.Section($"ระยะ 2 — parallel (สูงสุด {MaxConcurrency})");
        var done = 0;
        var total = remaining.Count;
        var results = new (ImagePlanItem Item, byte[] Bytes, string Caption)[remaining.Count];
        var failedIdx = new List<int>();
        var failedLock = new object();
        using var sem = new SemaphoreSlim(MaxConcurrency);

        var tasks = remaining.Select(async (item, idx) =>
        {
            await sem.WaitAsync(ct);
            try
            {
                if (ctx.BillingHit) { lock (failedLock) failedIdx.Add(idx); return; }
                var current = Interlocked.Increment(ref done);
                log($"กำลังสร้างรูป {current}/{total}: {item.Label}");
                var r = await GenSingleAsync(ctx, item);
                if (r.Ok) results[idx] = (item, r.Bytes!, r.Caption ?? "");
                else lock (failedLock) failedIdx.Add(idx);
            }
            finally { sem.Release(); }
        }).ToList();
        await Task.WhenAll(tasks);

        if (ctx.BillingHit)
        {
            ctx.GenLog?.Error("Billing hard limit reached — ยกเลิกการสร้างที่เหลือ");
            return (false, ctx.BillingError);
        }

        SaveResults(ctx, results);

        ctx.GenLog?.Section($"ระยะ 3 — retry รูปที่ล้มเหลว ({failedIdx.Count} รูป)");
        var stillFailed = new List<string>();
        foreach (var idx in failedIdx)
        {
            if (ctx.BillingHit) return (false, ctx.BillingError);
            var item = remaining[idx];
            log($"retry รูป {item.Label}");
            ctx.GenLog?.Line($"retry: {item.Label}");
            var r = await GenSingleAsync(ctx, item);
            if (r.Ok)
            {
                var single = new (ImagePlanItem, byte[], string)[1] { (item, r.Bytes!, r.Caption ?? "") };
                SaveResults(ctx, single);
                ctx.GenLog?.Line($"retry สำเร็จ: {item.Label}");
            }
            else
            {
                log($"❌ รูป {item.Label} ล้มเหลว: {r.Error}");
                ctx.GenLog?.Error($"retry ล้มเหลว: {item.Label}: {r.Error}");
                stillFailed.Add(item.Label);
            }
        }

        GeneratedImageWriter.SaveButtons(ctx.ProjectPath, ctx.Buttons);
        ctx.GenLog?.Line($"บันทึกปุ่มรวม: {ctx.Buttons.Count} ปุ่ม");

        if (stillFailed.Count == 0)
        {
            ctx.GenLog?.Line("สร้างรูปทั้งหมดสำเร็จ");
            return (true, null);
        }
        ctx.GenLog?.Warn($"รูปที่ยังล้มเหลว: {string.Join(", ", stillFailed)}");
        return (true, $"สร้างเสร็จแล้ว แต่มี {stillFailed.Count} รูปล้มเหลว: {string.Join(", ", stillFailed)}");
    }

    private static void SaveResults(GenContext ctx, (ImagePlanItem Item, byte[] Bytes, string Caption)[] results)
    {
        foreach (var (item, bytes, caption) in results)
        {
            if (bytes.Length == 0) continue;
            if (item.Id.StartsWith("btn-", StringComparison.Ordinal))
                ctx.Buttons.Add((item, bytes, ImagePromptCatalog.Alt(item, ctx.Options)));
            else
                GeneratedImageWriter.Save(ctx.ProjectPath, item, bytes, ImagePromptCatalog.Alt(item, ctx.Options, caption));
        }
    }

    private static async Task<(bool Ok, byte[]? Bytes, string? Caption, string? Error)> GenSingleAsync(GenContext ctx, ImagePlanItem item)
    {
        var isGame = item.Id.StartsWith("game-", StringComparison.Ordinal);
        var imageType = isGame
            ? (ctx.GameMascot ? "transparent" : "normal")
            : ImageGroupCatalog.ImageTypeOf(item.Id);
        var isButton = imageType == "button";
        var transparent = imageType is "button" or "transparent";
        var useLogo = !isGame && ctx.LogoReference is not null && NeedsLogo(item.Id);
        var useButtonRef = isButton && ctx.ButtonReference is not null;
        var gameRef = isGame ? ctx.GameRefs.GetValueOrDefault(item.Id) : null;
        var model = transparent ? TransparentModel : ctx.Options.ImageModel;
        var sizeOverride = item.Id == "banner" ? "auto" : null;
        ctx.GenLog?.Section($"สร้างรูป: {item.Id} ({item.Width}x{item.Height})");
        ctx.GenLog?.Line($"model={model} transparent={transparent} useLogo={useLogo} useButtonRef={useButtonRef} gameRef={(gameRef is not null)} size={(sizeOverride ?? "computed")}");

        for (var attempt = 0; attempt <= MaxModerationRetries; attempt++)
        {
            ctx.Ct.ThrowIfCancellationRequested();
            if (ctx.BillingHit) return (false, null, null, ctx.BillingError);
            string composition; string caption;
            lock (ctx.RngSync)
            {
                if (isGame)
                {
                    composition = ctx.GameChars.GetValueOrDefault(item.Id, "");
                    caption = "";
                }
                else
                {
                    var (compMin, compMax) = CompositionRange(item.Id);
                    composition = CompositionCatalog.Compose(ctx.Rng, ctx.MainCast, compMin, compMax);
                    caption = CaptionFor(item.Id, ctx.Promos, ctx.Slogans, ctx.Rng, attempt > 0);
                }
            }
            if (attempt > 0) ctx.GenLog?.Line($"attempt {attempt + 1}: re-roll composition/caption");
            ctx.GenLog?.Line($"composition: {(string.IsNullOrEmpty(composition) ? "(ไม่มี)" : composition)}");
            if (!string.IsNullOrEmpty(caption)) ctx.GenLog?.Line($"caption: {caption}");
            var prompt = isGame
                ? ImagePromptCatalog.BuildGamePrompt(item, ctx.Options, ctx.Palette, composition, transparent, gameRef is not null)
                : useButtonRef
                    ? ImagePromptCatalog.BuildButtonReferencePrompt(item)
                    : ImagePromptCatalog.BuildPrompt(item, ctx.Options, ctx.Palette, composition, caption, useLogo);
            ctx.GenLog?.Prompt(item.Id + (attempt > 0 ? $"-retry{attempt}" : ""), prompt);

            var (ok, bytes, _, error) =
                gameRef is not null
                    ? await OpenAiClient.GenerateImageWithReferenceAsync(ctx.ApiKey, model, prompt, gameRef, item.Width, item.Height, ctx.Ct, transparent: transparent, tracker: ctx.Usage, sizeOverride: sizeOverride)
                    : useButtonRef
                        ? await OpenAiClient.GenerateImageWithReferenceAsync(ctx.ApiKey, model, prompt, ctx.ButtonReference!, item.Width, item.Height, ctx.Ct, transparent: true, tracker: ctx.Usage)
                        : useLogo
                            ? await OpenAiClient.GenerateImageWithReferenceAsync(ctx.ApiKey, model, prompt, ctx.LogoReference!, item.Width, item.Height, ctx.Ct, tracker: ctx.Usage, sizeOverride: sizeOverride)
                            : await OpenAiClient.GenerateImageAsync(ctx.ApiKey, model, prompt, item.Width, item.Height, transparent, ctx.Ct, tracker: ctx.Usage, sizeOverride: sizeOverride);

            if (ok)
            {
                var outBytes = transparent ? BackgroundRemover.Remove(bytes, autoCrop: !isGame) : bytes;
                ctx.GenLog?.Line($"สำเร็จ ได้รูป {bytes.Length} bytes{(transparent ? (isGame ? " (ลบพื้นหลัง)" : " (ลบพื้นหลัง+ครอป)") : "")}");
                return (true, outBytes, caption, null);
            }

            ctx.GenLog?.Error($"{item.Id}: {error}");
            if (OpenAiClient.IsBillingLimitReached(error))
            {
                ctx.BillingHit = true;
                ctx.BillingError = error;
                ctx.GenLog?.Error("Billing hard limit reached — หยุดสร้างรูปทั้งหมด");
                return (false, null, null, error);
            }
            if (OpenAiClient.IsModerationBlocked(error) && attempt < MaxModerationRetries)
            {
                ctx.Log($"รูป {item.Label} โดน safety block — สุ่มใหม่ ({attempt + 1}/{MaxModerationRetries})");
                ctx.GenLog?.Warn($"{item.Id} safety block — สุ่มใหม่ ({attempt + 1}/{MaxModerationRetries})");
                continue;
            }
            return (false, null, null, error);
        }
        ctx.GenLog?.Error($"{item.Id}: moderation retries exhausted");
        return (false, null, null, "moderation retries exhausted");
    }

    private static void AssignGame(GenContext ctx, IReadOnlyList<ImagePlanItem> items)
    {
        var ids = items.Where(p => p.Id.StartsWith("game-", StringComparison.Ordinal))
            .Select(p => p.Id).ToList();
        if (ids.Count == 0) return;

        var chars = CompositionCatalog.PickGameCast(ctx.Rng, ids.Count);
        var providers = LoadProviderPool().OrderBy(_ => ctx.Rng.Next()).ToList();

        for (var i = 0; i < ids.Count; i++)
        {
            if (chars.Count > 0) ctx.GameChars[ids[i]] = chars[i];
            if (providers.Count > 0) ctx.GameRefs[ids[i]] = providers[i % providers.Count];
        }
    }

    private static List<byte[]> LoadProviderPool()
    {
        if (!Directory.Exists(ProviderStore.Root)) return [];

        var list = new List<byte[]>();
        foreach (var f in Directory.EnumerateFiles(ProviderStore.Root, "*.*")
                    .Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                                || p.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)))
        {
            try { list.Add(ToPng(File.ReadAllBytes(f))); } catch { }
        }
        return list;
    }

    private static Dictionary<string, string> AssignPromos(IReadOnlyList<ImagePlanItem> items, Random rng)
    {
        var ids = items.Where(p => p.Id.StartsWith("promo-", StringComparison.Ordinal)).Select(p => p.Id).ToList();
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (ids.Count == 0) return map;
        var picks = PromotionCatalog.Pick(rng, ids.Count);
        for (var i = 0; i < ids.Count; i++) map[ids[i]] = picks[i % picks.Count];
        return map;
    }

    private static Dictionary<string, string> AssignSlogans(IReadOnlyList<ImagePlanItem> items, Random rng)
    {
        var ids = items.Where(p => p.Id.StartsWith("seo-", StringComparison.Ordinal)).Select(p => p.Id).ToList();
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (ids.Count == 0) return map;
        var picks = SeoSloganCatalog.Pick(rng, ids.Count);
        for (var i = 0; i < ids.Count; i++) map[ids[i]] = picks[i % picks.Count];
        return map;
    }

    private static string CaptionFor(
        string id, IReadOnlyDictionary<string, string> promos,
        IReadOnlyDictionary<string, string> slogans, Random rng, bool reroll)
    {
        return (ImageGroupCatalog.ByPrefix(id)?.CaptionSource ?? "") switch
        {
            "promo" => !reroll && promos.TryGetValue(id, out var p) ? p : PromotionCatalog.Pick(rng, 1).FirstOrDefault() ?? "",
            "seo" => !reroll && slogans.TryGetValue(id, out var s) ? s : SeoSloganCatalog.Pick(rng, 1).FirstOrDefault() ?? "",
            _ => "",
        };
    }

    private static bool NeedsLogo(string id) =>
        ImageGroupCatalog.ByPrefix(id)?.UseLogoReference ?? false;

    private static (int Min, int Max) CompositionRange(string id)
    {
        if (ImageGroupCatalog.ByPrefix(id) is not { } g) return (0, 0);
        return (g.CompositionMin, g.CompositionMax);
    }

    private static byte[] ToPng(byte[] bytes)
    {
        using var bitmap = SkiaSharp.SKBitmap.Decode(bytes);
        if (bitmap is null) return bytes;
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private sealed class GenContext
    {
        public readonly string ProjectPath;
        public readonly GenerationOptions Options;
        public readonly ThemePalette Palette;
        public readonly string ApiKey;
        public readonly Action<string> Log;
        public readonly CancellationToken Ct;
        public readonly Random Rng = new();
        public readonly object RngSync = new();
        public readonly GenerationLog? GenLog;
        public readonly UsageTracker? Usage;

        public Dictionary<string, string> Promos = [];
        public Dictionary<string, string> Slogans = [];
        public Dictionary<string, string> GameChars = [];
        public Dictionary<string, byte[]> GameRefs = [];
        public bool GameMascot;
        public List<string> MainCast = [];
        public bool Realistic;
        public List<(ImagePlanItem Item, byte[] Bytes, string Alt)> Buttons = [];
        public byte[]? LogoReference;
        public byte[]? ButtonReference;
        public volatile bool BillingHit;
        public string? BillingError;

        public GenContext(string projectPath, GenerationOptions options, ThemePalette palette,
            string apiKey, Action<string> log, CancellationToken ct, GenerationLog? genLog, UsageTracker? usage)
        {
            ProjectPath = projectPath; Options = options; Palette = palette;
            ApiKey = apiKey; Log = log; Ct = ct; GenLog = genLog; Usage = usage;
        }
    }
}
