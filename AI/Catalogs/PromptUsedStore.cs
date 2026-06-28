using System.Text.Encodings.Web;
using System.Text.Json;

namespace TEMO.AI.Ai;

internal static class PromptUsedStore
{
    private const string FileName = "promptused.json";

    private static readonly string FilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly object Lock = new();
    private static Dictionary<string, List<string>> _used = new(StringComparer.Ordinal);

    static PromptUsedStore() => Load();

    private static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                _used = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
                    File.ReadAllText(FilePath)) ?? new(StringComparer.Ordinal);
            }
        }
        catch { _used = new(StringComparer.Ordinal); }
    }

    private static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_used, Indented));
        }
        catch { }
    }

    private static List<string> UsedFor(string catalog)
    {
        if (!_used.TryGetValue(catalog, out var list))
            _used[catalog] = list = new List<string>();
        return list;
    }

    /// <summary>
    /// สุ่ม <paramref name="count" /> รายการจาก <paramref name="pool" /> โดยข้ามรายการที่เคยใช้แล้ว
    /// ถ้าเหลือไม่พอ <paramref name="count" /> จะรีเซ็ตรายการที่ใช้แล้วของ catalog นั้นทั้งหมดแล้วสุ่มใหม่
    /// </summary>
    public static List<T> Pick<T>(
        string catalog, IReadOnlyList<T> pool, Func<T, string> key, Random rng, int count)
    {
        if (pool.Count == 0 || count <= 0) return new List<T>();

        lock (Lock)
        {
            var used = UsedFor(catalog);
            var available = pool.Where(x => !used.Contains(key(x))).ToList();
            if (available.Count < count)
            {
                used.Clear();
                available = pool.ToList();
            }

            var picked = available.OrderBy(_ => rng.Next()).Take(count).ToList();
            foreach (var p in picked) used.Add(key(p));
            Save();
            return picked;
        }
    }

    public static T PickAtomic<T>(string catalog, Func<HashSet<string>, T> operate)
    {
        lock (Lock)
        {
            var usedList = UsedFor(catalog);
            var used = new HashSet<string>(usedList, StringComparer.Ordinal);
            var result = operate(used);
            usedList.Clear();
            usedList.AddRange(used);
            Save();
            return result;
        }
    }
}
