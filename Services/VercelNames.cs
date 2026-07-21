namespace TEMO.AI;

internal static partial class VercelNames
{
    public const int MaxLength = 100;

    public const string ValidNameHint =
        "ใช้ได้เฉพาะ a-z, 0-9, . _ - (ตัวพิมพ์เล็ก) ห้ามขึ้นต้น/ลงท้ายด้วย . _ -";

    public static string Sanitize(string name)
    {
        name = name.ToLowerInvariant();
        name = InvalidCharsRegex().Replace(name, "-");
        name = MultiDashRegex().Replace(name, "-");
        name = name.Trim('.', '_', '-');

        if (name.Length > MaxLength)
            name = name[..MaxLength].Trim('.', '_', '-');

        return string.IsNullOrWhiteSpace(name) ? "project" : name;
    }

    public static string FromPath(string projectPath)
    {
        var folder = Path.GetFileName(
            projectPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return Sanitize(folder);
    }

    public static bool IsAllowedChar(char c) =>
        c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '.' or '_' or '-';

    public static string FilterWhileTyping(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (!IsAllowedChar(c)) continue;
            sb.Append(char.ToLowerInvariant(c));
        }
        var result = sb.ToString();
        return result.Length > MaxLength ? result[..MaxLength] : result;
    }

    public static bool IsValid(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxLength) return false;
        if (name != name.ToLowerInvariant()) return false;
        if (!ValidNameRegex().IsMatch(name)) return false;
        return !MultiDashRegex().IsMatch(name);
    }

    [GeneratedRegex(@"^[a-z0-9](?:[a-z0-9._-]*[a-z0-9])?$")]
    private static partial Regex ValidNameRegex();

    [GeneratedRegex(@"[^a-z0-9._-]+")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"-+")]
    private static partial Regex MultiDashRegex();
}
