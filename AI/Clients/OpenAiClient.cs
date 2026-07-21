using System.Net;
using System.Net.Http.Headers;

namespace TEMO.AI.Ai;

internal static class OpenAiClient
{
    private const string ChatUrl = "https://api.openai.com/v1/chat/completions";
    private const string ImageUrl = "https://api.openai.com/v1/images/generations";
    private const string ImageEditUrl = "https://api.openai.com/v1/images/edits";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(8) };
    private const int MaxTransientRetries = 3;

    private static readonly int[] RetryableStatusCodes =
    [
        408, // Request Timeout
        429, // Too Many Requests
        500, // Internal Server Error
        502, // Bad Gateway
        503, // Service Unavailable
        504, // Gateway Timeout
    ];

    private static async Task<(HttpStatusCode Status, string Body)> SendWithRetryAsync(
        Func<HttpRequestMessage> createRequest, CancellationToken ct)
    {
        for (var attempt = 0; attempt <= MaxTransientRetries; attempt++)
        {
            HttpResponseMessage? resp = null;
            try
            {
                using var req = createRequest();
                resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
                var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                if (!RetryableStatusCodes.Contains((int)resp.StatusCode) || attempt == MaxTransientRetries)
                    return (resp.StatusCode, body);
            }
            catch (HttpRequestException) when (attempt < MaxTransientRetries)
            {
                // เครือข่ายหลุด/รีเซ็ตก่อนได้ headers — ลองใหม่
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested && attempt < MaxTransientRetries)
            {
                // timeout ชั่วคราว — ลองใหม่
            }
            finally
            {
                resp?.Dispose();
            }

            await Task.Delay(TimeSpan.FromSeconds(2 * (attempt + 1)), ct).ConfigureAwait(false);
        }

        throw new UnreachableException();
    }

    public static async Task<(bool Ok, string Content, string RawJson, string? Error)> ChatAsync(
        string apiKey, string model, object messageContent, CancellationToken ct = default, UsageTracker? tracker = null)
    {
        var body = JsonSerializer.Serialize(new
        {
            model,
            messages = new[] { new { role = "user", content = messageContent } }
        });

        var (status, json) = await SendWithRetryAsync(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, ChatUrl)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return req;
        }, ct).ConfigureAwait(false);

        if (status != HttpStatusCode.OK)
            return (false, "", json, $"❌ API Error {(int)status} {status}\n\n{json}");

        var text = JsonNode.Parse(json)?["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? "";
        if (string.IsNullOrWhiteSpace(text))
            return (false, "", json, $"⚠️ AI ตอบกลับมาว่าง\n\nRaw response:\n{json}");

        if (tracker is not null)
        {
            var (i, o, _) = UsageTracker.ParseChatUsage(JsonNode.Parse(json)?["usage"]);
            tracker.Add(model, i, o, 0);
        }
        return (true, text, json, null);
    }

    public static async Task<(bool Ok, byte[] Bytes, string RawJson, string? Error)> GenerateImageAsync(
        string apiKey, string model, string prompt, int width, int height, bool transparent = false, CancellationToken ct = default, UsageTracker? tracker = null, string? sizeOverride = null)
    {
        var body = JsonSerializer.Serialize(new
        {
            model,
            prompt,
            size = sizeOverride ?? ImageSize(width, height),
            quality = "medium",
            background = transparent ? "auto" : "opaque",
            n = 1
        });

        var (status, json) = await SendWithRetryAsync(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, ImageUrl)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return req;
        }, ct).ConfigureAwait(false);

        if (status != HttpStatusCode.OK)
            return (false, [], json, $"❌ Image API Error {(int)status} {status}\n\n{json}");

        var b64 = JsonNode.Parse(json)?["data"]?[0]?["b64_json"]?.GetValue<string>() ?? "";
        if (string.IsNullOrWhiteSpace(b64))
            return (false, [], json, $"⚠️ AI ไม่ส่งรูปกลับมา\n\nRaw response:\n{json}");

        if (tracker is not null)
        {
            var (i, o, img) = UsageTracker.ParseImageUsage(JsonNode.Parse(json)?["usage"]);
            tracker.Add(model, i, o, img);
        }
        return (true, Convert.FromBase64String(b64), json, null);
    }

    public static async Task<(bool Ok, byte[] Bytes, string RawJson, string? Error)> GenerateImageWithReferenceAsync(
        string apiKey, string model, string prompt, byte[] referencePng, int width, int height, CancellationToken ct = default, bool transparent = false, UsageTracker? tracker = null, string? sizeOverride = null)
    {
        var (status, json) = await SendWithRetryAsync(() =>
        {
            var form = new MultipartFormDataContent
            {
                { new StringContent(model), "model" },
                { new StringContent(prompt), "prompt" },
                { new StringContent(sizeOverride ?? ImageSize(width, height)), "size" },
                { new StringContent("medium"), "quality" },
                { new StringContent(transparent ? "auto" : "opaque"), "background" },
                { new StringContent("1"), "n" },
            };

            var image = new ByteArrayContent(referencePng);
            image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(image, "image", "logo.png");

            var req = new HttpRequestMessage(HttpMethod.Post, ImageEditUrl) { Content = form };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return req;
        }, ct).ConfigureAwait(false);

        if (status != HttpStatusCode.OK)
            return (false, [], json, $"❌ Image Edit API Error {(int)status} {status}\n\n{json}");

        var b64 = JsonNode.Parse(json)?["data"]?[0]?["b64_json"]?.GetValue<string>() ?? "";
        if (string.IsNullOrWhiteSpace(b64))
            return (false, [], json, $"⚠️ AI ไม่ส่งรูปกลับมา\n\nRaw response:\n{json}");

        if (tracker is not null)
        {
            var (i, o, img) = UsageTracker.ParseImageUsage(JsonNode.Parse(json)?["usage"]);
            tracker.Add(model, i, o, img);
        }
        return (true, Convert.FromBase64String(b64), json, null);
    }

    public static bool IsModerationBlocked(string? error) =>
        !string.IsNullOrWhiteSpace(error)
        && (error.Contains("moderation_blocked", StringComparison.OrdinalIgnoreCase)
            || error.Contains("safety_violations", StringComparison.OrdinalIgnoreCase));

    public static bool IsBillingLimitReached(string? error) =>
        !string.IsNullOrWhiteSpace(error)
        && (error.Contains("billing_hard_limit_reached", StringComparison.OrdinalIgnoreCase)
            || error.Contains("billing_limit_user_error", StringComparison.OrdinalIgnoreCase)
            || error.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase));

    private static string ImageSize(int width, int height)
    {
        var ratio = width / (double)Math.Max(1, height);
        if (ratio > 1.2) return "1536x1024";
        if (ratio < 0.82) return "1024x1536";
        return "1024x1024";
    }
}
