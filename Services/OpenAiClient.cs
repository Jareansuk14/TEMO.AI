using System.Net.Http.Headers;

namespace TEMO.AI;

internal static class OpenAiClient
{
    private const string ChatUrl = "https://api.openai.com/v1/chat/completions";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(3) };

    public static async Task<(bool Ok, string Content, string RawJson, string? Error)> ChatAsync(
        string apiKey, string model, object messageContent, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            model,
            messages = new[] { new { role = "user", content = messageContent } }
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, ChatUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
            return (false, "", json, $"❌ API Error {(int)resp.StatusCode} {resp.StatusCode}\n\n{json}");

        var text = JsonNode.Parse(json)?["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? "";
        if (string.IsNullOrWhiteSpace(text))
            return (false, "", json, $"⚠️ AI ตอบกลับมาว่าง\n\nRaw response:\n{json}");

        return (true, text, json, null);
    }
}
