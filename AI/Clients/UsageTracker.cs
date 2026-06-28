using System.Globalization;
using System.Text;

namespace TEMO.AI.Ai;

internal sealed class UsageTracker
{
    private const double TextInputPerMillion = 5.0;
    private const double ImageInputPerMillion = 8.0;
    private const double OutputPerMillion = 30.0;
    public const double ThbPerUsd = 34.0;

    private readonly object _sync = new();
    private readonly Dictionary<string, Entry> _byModel = new(StringComparer.Ordinal);

    public void Add(string model, long inputTokens, long outputTokens, long imageInputTokens)
    {
        if (string.IsNullOrEmpty(model)) return;
        if (inputTokens <= 0 && outputTokens <= 0) return;
        lock (_sync)
        {
            if (!_byModel.TryGetValue(model, out var e)) e = new Entry();
            e.InputTokens += inputTokens;
            e.OutputTokens += outputTokens;
            e.ImageInputTokens += imageInputTokens;
            _byModel[model] = e;
        }
    }

    public double TotalUsd
    {
        get
        {
            lock (_sync)
            {
                double sum = 0;
                foreach (var (_, e) in _byModel)
                {
                    var textIn = Math.Max(0, e.InputTokens - e.ImageInputTokens);
                    sum += textIn / 1_000_000.0 * TextInputPerMillion
                         + e.ImageInputTokens / 1_000_000.0 * ImageInputPerMillion
                         + e.OutputTokens / 1_000_000.0 * OutputPerMillion;
                }
                return sum;
            }
        }
    }

    public double TotalThb => TotalUsd * ThbPerUsd;

    public string Report()
    {
        var sb = new StringBuilder();
        lock (_sync)
        {
            if (_byModel.Count == 0)
            {
                sb.AppendLine("(ไม่มีการใช้งาน API)");
                sb.AppendLine($"รวม: 0.00 ฿ (0.00 USD)");
                return sb.ToString();
            }
            sb.AppendLine("อัตรา: text in $5/1M • image in $8/1M • output $30/1M • 34 บาท/USD");
            sb.AppendLine();
            foreach (var (model, e) in _byModel)
            {
                var textIn = Math.Max(0, e.InputTokens - e.ImageInputTokens);
                var usd = textIn / 1_000_000.0 * TextInputPerMillion
                        + e.ImageInputTokens / 1_000_000.0 * ImageInputPerMillion
                        + e.OutputTokens / 1_000_000.0 * OutputPerMillion;
                sb.AppendLine($"{model}");
                sb.AppendLine($"  input: {e.InputTokens:N0} (text {textIn:N0} + image {e.ImageInputTokens:N0}) | output: {e.OutputTokens:N0}");
                sb.AppendLine($"  {usd * ThbPerUsd:N2} ฿ ({usd:N4} USD)");
            }
            sb.AppendLine();
            sb.AppendLine($"รวมทั้งหมด: {TotalThb:N2} ฿ ({TotalUsd:N4} USD)");
        }
        return sb.ToString();
    }

    private struct Entry
    {
        public long InputTokens;
        public long OutputTokens;
        public long ImageInputTokens;
    }

    public static (long input, long output, long imageInput) ParseChatUsage(System.Text.Json.Nodes.JsonNode? usage)
    {
        if (usage is null) return (0, 0, 0);
        var input = usage["prompt_tokens"]?.GetValue<long>() ?? 0;
        var output = usage["completion_tokens"]?.GetValue<long>() ?? 0;
        return (input, output, 0);
    }

    public static (long input, long output, long imageInput) ParseImageUsage(System.Text.Json.Nodes.JsonNode? usage)
    {
        if (usage is null) return (0, 0, 0);
        var input = usage["input_tokens"]?.GetValue<long>() ?? 0;
        var output = usage["output_tokens"]?.GetValue<long>() ?? 0;
        var imageInput = usage["input_tokens_details"]?["image_tokens"]?.GetValue<long>() ?? 0;
        return (input, output, imageInput);
    }
}
