namespace TEMO.AI;

internal static class TsBlockParser
{
    public static string? FirstBlock(string text, string anchor)
    {
        var ai = text.IndexOf(anchor, StringComparison.Ordinal);
        if (ai < 0) return null;
        var ob = text.IndexOf('{', ai);
        if (ob < 0) return null;
        var close = MatchBrace(text, ob);
        return close < 0 ? null : text[ob..(close + 1)];
    }

    public static List<string> AllBlocks(string text)
    {
        var result = new List<string>();
        char quote = '\0';
        var escape = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (escape) { escape = false; continue; }
            if (quote != '\0')
            {
                if (c == '\\') escape = true;
                else if (c == quote) quote = '\0';
                continue;
            }
            if (c is '"' or '\'' or '`') { quote = c; continue; }
            if (c != '{') continue;

            var close = MatchBrace(text, i);
            if (close < 0) break;
            result.Add(text[i..(close + 1)]);
            i = close;
        }
        return result;
    }

    private static int MatchBrace(string text, int open)
    {
        var depth = 0;
        char quote = '\0';
        var escape = false;

        for (var i = open; i < text.Length; i++)
        {
            var c = text[i];
            if (escape) { escape = false; continue; }
            if (quote != '\0')
            {
                if (c == '\\') escape = true;
                else if (c == quote) quote = '\0';
                continue;
            }
            switch (c)
            {
                case '"' or '\'' or '`': quote = c; break;
                case '{': depth++; break;
                case '}' when --depth == 0: return i;
            }
        }
        return -1;
    }

    public static string QuotedVal(string block, string key)
    {
        var m = Regex.Match(block, $@"\b{Regex.Escape(key)}:\s*""((?:\\.|[^""\\])*)""");
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
