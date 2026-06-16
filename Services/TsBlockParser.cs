namespace TEMO.AI;

internal static class TsBlockParser
{
    public static string? FirstBlock(string text, string anchor)
    {
        var ai = text.IndexOf(anchor, StringComparison.Ordinal);
        if (ai < 0) return null;
        var ob = text.IndexOf('{', ai);
        if (ob < 0) return null;
        int depth = 0, i = ob;
        while (i < text.Length)
        {
            if      (text[i] == '{') depth++;
            else if (text[i] == '}') { if (--depth == 0) break; }
            i++;
        }
        return i < text.Length ? text[ob..(i + 1)] : null;
    }

    public static List<string> AllBlocks(string text)
    {
        var result = new List<string>();
        int i = 0;
        while (i < text.Length)
        {
            var s = text.IndexOf('{', i);
            if (s < 0) break;
            int depth = 0, j = s;
            while (j < text.Length)
            {
                if      (text[j] == '{') depth++;
                else if (text[j] == '}') { if (--depth == 0) break; }
                j++;
            }
            result.Add(text[s..(j + 1)]);
            i = j + 1;
        }
        return result;
    }

    public static string QuotedVal(string block, string key)
    {
        var m = Regex.Match(block, $@"\b{Regex.Escape(key)}:\s*""([^""]*)""");
        return m.Success ? m.Groups[1].Value : "";
    }

    public static Match ArrayMatch(string content, string name) =>
        Regex.Match(content, $@"(export\s+const\s+{Regex.Escape(name)}\b[^=]*=\s*\[)([\s\S]*?)(\];)");

    public static int CountArray(string content, string name)
    {
        var m = ArrayMatch(content, name);
        return m.Success ? AllBlocks(m.Groups[2].Value).Count : 0;
    }
}
