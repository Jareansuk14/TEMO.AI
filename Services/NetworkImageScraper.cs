using Microsoft.Web.WebView2.Core;

namespace TEMO.AI;

internal sealed class ScrapedImage
{
    public required string Url { get; init; }
    public required byte[] Data { get; init; }
    public required string Extension { get; init; }
}

internal enum ImageScrapePhase { LoadingPage, Scrolling, WaitingNetwork, ReadingImage }

internal sealed class ImageScrapeProgress
{
    public required ImageScrapePhase Phase { get; init; }
    public required string Message { get; init; }
    public required int Count { get; init; }
}

internal sealed class NetworkImageScraperOptions
{
    public TimeSpan NavigationTimeout { get; init; } = TimeSpan.FromSeconds(12);
    public TimeSpan InitialSettleDelay { get; init; } = TimeSpan.FromMilliseconds(300);
    public TimeSpan MaxCaptureDuration { get; init; } = TimeSpan.FromSeconds(16);
    public TimeSpan NetworkIdleDelay { get; init; } = TimeSpan.FromMilliseconds(1400);
    public TimeSpan EmptyPageDelay { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan ResponseTimeout { get; init; } = TimeSpan.FromMilliseconds(1600);
    public TimeSpan FinalPendingTimeout { get; init; } = TimeSpan.FromMilliseconds(500);
    public int MaxScrollPasses { get; init; } = 2;
    public int MaxScrollStepsPerPass { get; init; } = 45;
    public int MinImageBytes { get; init; } = 128;
}

internal sealed class NetworkImageScraper : IDisposable
{
    private static readonly Regex ImageExtPattern =
        new(@"\.(jpg|jpeg|png|webp|gif|avif|svg|bmp|ico)($|\?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly CoreWebView2 _core;
    private readonly NetworkImageScraperOptions _options;
    private readonly object _sync = new();
    private readonly Dictionary<string, CapturedImage> _images = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Task> _pending = [];

    private IProgress<ImageScrapeProgress>? _progress;
    private DateTime _lastImageActivityUtc = DateTime.UtcNow;
    private DateTime _lastProgressUtc = DateTime.MinValue;
    private int _lastReportedCount = -1;
    private int _nextOrder;
    private int _sessionId;
    private bool _capturing;

    public NetworkImageScraper(CoreWebView2 core, NetworkImageScraperOptions? options = null)
    {
        _core = core;
        _options = options ?? new NetworkImageScraperOptions();
        _core.WebResourceResponseReceived += OnResponseReceived;
    }

    public void Dispose() => _core.WebResourceResponseReceived -= OnResponseReceived;

    public async Task<List<ScrapedImage>> CaptureAsync(
        string url, IProgress<ImageScrapeProgress>? progress, CancellationToken ct)
    {
        int sessionId;
        lock (_sync)
        {
            _images.Clear();
            _seen.Clear();
            _nextOrder = 0;
            _lastImageActivityUtc = DateTime.UtcNow;
            _lastProgressUtc = DateTime.MinValue;
            _lastReportedCount = -1;
            sessionId = ++_sessionId;
        }

        lock (_pending) _pending.Clear();

        _progress = progress;
        _capturing = true;
        Report(ImageScrapePhase.LoadingPage, "กำลังโหลดหน้าเว็บ...");

        var navDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnNav(object? s, CoreWebView2NavigationCompletedEventArgs e) => navDone.TrySetResult();
        _core.NavigationCompleted += OnNav;

        try
        {
            _core.Navigate(url);

            var navTimeout = Task.Delay(_options.NavigationTimeout, ct);
            await Task.WhenAny(navDone.Task, navTimeout);
            ct.ThrowIfCancellationRequested();

            await Task.Delay(_options.InitialSettleDelay, ct);
            await AutoScrollAsync(ct);
            await WaitForNetworkIdleAsync(ct);
        }
        finally
        {
            _core.NavigationCompleted -= OnNav;
            _capturing = false;
        }

        await WaitForPendingTasksAsync(_options.FinalPendingTimeout, ct);
        return SnapshotImages(sessionId);
    }

    private const string TriggerLazyScript = """
        (function() {
            var attrs = ['data-src', 'data-lazy-src', 'data-original', 'data-url', 'data-image', 'data-full-src'];
            document.querySelectorAll('img').forEach(function(img) {
                for (var i = 0; i < attrs.length; i++) {
                    var value = img.getAttribute(attrs[i]);
                    if (value && (!img.getAttribute('src') || img.getAttribute('src').startsWith('data:'))) {
                        img.setAttribute('src', value);
                        break;
                    }
                }
                if (img.getAttribute('loading') === 'lazy')
                    img.setAttribute('loading', 'eager');
            });
            document.querySelectorAll('source[data-srcset],source[data-src]').forEach(function(source) {
                var srcset = source.getAttribute('data-srcset');
                var src = source.getAttribute('data-src');
                if (srcset && !source.getAttribute('srcset')) source.setAttribute('srcset', srcset);
                if (src && !source.getAttribute('src')) source.setAttribute('src', src);
            });
            window.dispatchEvent(new Event('scroll', { bubbles: true }));
            window.dispatchEvent(new Event('resize', { bubbles: true }));
        })()
        """;

    private const string ScrollStepScript = """
        (function() {
            var root = document.scrollingElement || document.documentElement || document.body;
            var fullHeight = Math.max(
                document.body?.scrollHeight || 0,
                document.documentElement?.scrollHeight || 0,
                root?.scrollHeight || 0);
            var step = Math.max(280, window.innerHeight * 0.75);
            window.scrollBy(0, step);
            Array.from(document.querySelectorAll('main,section,article,div,ul,ol'))
                .filter(function(el) {
                    return el.clientHeight > 220 && el.scrollHeight > el.clientHeight + 120;
                })
                .slice(0, 8)
                .forEach(function(el) {
                    el.scrollTop += Math.max(220, el.clientHeight * 0.75);
                    el.dispatchEvent(new Event('scroll', { bubbles: true }));
                });
            return (window.scrollY + window.innerHeight) >= (fullHeight - 120);
        })()
        """;

    private async Task AutoScrollAsync(CancellationToken ct)
    {
        for (var pass = 0; pass < _options.MaxScrollPasses; pass++)
        {
            ct.ThrowIfCancellationRequested();
            await _core.ExecuteScriptAsync("window.scrollTo(0, 0)");
            await Task.Delay(120, ct);

            for (var step = 0; step < _options.MaxScrollStepsPerPass; step++)
            {
                ct.ThrowIfCancellationRequested();
                Report(ImageScrapePhase.Scrolling, $"กำลังเลื่อนหน้าเว็บ... พบแล้ว {ImageCount} รูป");
                await _core.ExecuteScriptAsync(TriggerLazyScript);
                var atBottom = await _core.ExecuteScriptAsync(ScrollStepScript);
                await Task.Delay(120, ct);

                if (atBottom == "true" && step > 3 && !HasRecentImageActivity(TimeSpan.FromMilliseconds(700)))
                    break;
            }

            await WaitForPendingTasksAsync(TimeSpan.FromMilliseconds(250), ct);
            if (ImageCount > 0 && !HasRecentImageActivity(TimeSpan.FromMilliseconds(900)))
                break;
        }

        await _core.ExecuteScriptAsync("window.scrollTo(0, 0)");
        await Task.Delay(100, ct);
    }

    private async Task WaitForNetworkIdleAsync(CancellationToken ct)
    {
        var started = DateTime.UtcNow;
        while (DateTime.UtcNow - started < _options.MaxCaptureDuration)
        {
            ct.ThrowIfCancellationRequested();
            Report(ImageScrapePhase.WaitingNetwork, $"กำลังตรวจรูปที่โหลดเพิ่ม... พบแล้ว {ImageCount} รูป");
            await WaitForPendingTasksAsync(TimeSpan.FromMilliseconds(180), ct);

            var hasImages = ImageCount > 0;
            if (hasImages && !HasRecentImageActivity(_options.NetworkIdleDelay))
                return;

            if (!hasImages && DateTime.UtcNow - started > _options.EmptyPageDelay)
                return;

            await Task.Delay(120, ct);
        }
    }

    private void OnResponseReceived(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        if (!_capturing) return;

        var resp = e.Response;
        if (resp.StatusCode is < 200 or >= 300) return;

        var uri = e.Request.Uri;
        if (string.IsNullOrEmpty(uri)) return;

        var contentType = resp.Headers.Contains("Content-Type")
            ? resp.Headers.GetHeader("Content-Type")
            : null;

        if (!IsImage(contentType, uri)) return;

        int order, sessionId;
        lock (_sync)
        {
            if (!_capturing || !_seen.Add(uri)) return;
            _lastImageActivityUtc = DateTime.UtcNow;
            order = _nextOrder++;
            sessionId = _sessionId;
        }

        var task = CaptureContentAsync(uri, resp, contentType, order, sessionId);
        lock (_pending)
        {
            _pending.RemoveAll(t => t.IsCompleted);
            _pending.Add(task);
        }
    }

    private async Task CaptureContentAsync(
        string uri, CoreWebView2WebResourceResponseView resp, string? contentType, int order, int sessionId)
    {
        try
        {
            Report(ImageScrapePhase.ReadingImage, $"กำลังอ่านรูป... พบแล้ว {ImageCount} รูป");

            var streamTask = resp.GetContentAsync();
            var streamDone = await Task.WhenAny(streamTask, Task.Delay(_options.ResponseTimeout));
            if (streamDone != streamTask) return;

            await using var stream = await streamTask;
            if (stream is null) return;

            var bytesTask = ReadAllBytesAsync(stream);
            var bytesDone = await Task.WhenAny(bytesTask, Task.Delay(_options.ResponseTimeout));
            if (bytesDone != bytesTask) return;

            var bytes = await bytesTask;
            if (bytes.Length < _options.MinImageBytes) return;

            var ext = ExtFromContentType(contentType) ?? ExtFromUrl(uri);
            int count;
            lock (_sync)
            {
                if (sessionId != _sessionId) return;
                _images[uri] = new CapturedImage(
                    new ScrapedImage { Url = uri, Data = bytes, Extension = ext }, order);
                _lastImageActivityUtc = DateTime.UtcNow;
                count = _images.Count;
            }
            Report(ImageScrapePhase.ReadingImage, $"กำลังดึงรูป... พบแล้ว {count} รูป");
        }
        catch { }
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }

    private async Task WaitForPendingTasksAsync(TimeSpan timeout, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            Task[] snapshot;
            lock (_pending)
            {
                _pending.RemoveAll(t => t.IsCompleted);
                snapshot = _pending.ToArray();
            }

            if (snapshot.Length == 0) return;

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) return;

            await Task.WhenAny(Task.WhenAll(snapshot), Task.Delay(remaining, ct));
        }
    }

    private void Report(ImageScrapePhase phase, string message)
    {
        var count = ImageCount;
        var now = DateTime.UtcNow;
        if (phase is ImageScrapePhase.Scrolling or ImageScrapePhase.WaitingNetwork
            && count == _lastReportedCount
            && now - _lastProgressUtc < TimeSpan.FromMilliseconds(350))
            return;

        _lastReportedCount = count;
        _lastProgressUtc = now;
        _progress?.Report(new ImageScrapeProgress { Phase = phase, Message = message, Count = count });
    }

    private bool HasRecentImageActivity(TimeSpan age)
    {
        lock (_sync) return DateTime.UtcNow - _lastImageActivityUtc < age;
    }

    private int ImageCount
    {
        get { lock (_sync) return _images.Count; }
    }

    private List<ScrapedImage> SnapshotImages(int sessionId)
    {
        lock (_sync)
        {
            if (sessionId != _sessionId) return [];
            return _images.Values.OrderBy(i => i.Order).Select(i => i.Image).ToList();
        }
    }

    private static bool IsImage(string? contentType, string uri)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return true;
            if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("html", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("css", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("font", StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return ImageExtPattern.IsMatch(uri);
    }

    private static string? ExtFromContentType(string? contentType) =>
        contentType?.Split(';')[0].Trim().ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/avif" => ".avif",
            "image/svg+xml" => ".svg",
            "image/bmp" => ".bmp",
            "image/x-icon" or "image/vnd.microsoft.icon" => ".ico",
            _ => null,
        };

    private static string ExtFromUrl(string url)
    {
        var path = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;
        var ext = Path.GetExtension(path.Split('?')[0]);
        return string.IsNullOrEmpty(ext) || ext.Length > 6 ? ".jpg" : ext.ToLowerInvariant();
    }

    private sealed record CapturedImage(ScrapedImage Image, int Order);
}
