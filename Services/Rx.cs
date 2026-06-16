namespace TEMO.AI;

internal static class Rx
{
    public static string Wrap(string input, string pattern, string value, RegexOptions options = RegexOptions.None) =>
        Regex.Replace(input, pattern, m => m.Groups[1].Value + value + m.Groups[2].Value, options);
}
