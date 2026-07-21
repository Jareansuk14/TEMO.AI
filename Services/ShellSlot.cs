namespace TEMO.AI;

internal sealed class SlotDef
{
    public string Slot { get; set; } = "";
    public string Path { get; set; } = "";
}

internal static class ShellSlot
{
    public const string FileName = "slots.json";

    private static readonly SlotDef[] Defaults =
    [
        new() { Slot = "header", Path = "components/Header.astro" },
        new() { Slot = "banner", Path = "components/Banner.astro" },
        new() { Slot = "faq", Path = "components/FAQSection.astro" },
        new() { Slot = "page:promotions", Path = "components/page/PromotionsBody.astro" },
        new() { Slot = "page:contact", Path = "components/page/ContactBody.astro" },
    ];

    private static List<SlotDef>? _defs;

    public static void Reload() => _defs = null;

    private static IReadOnlyList<SlotDef> Defs => _defs ??= Load();

    public static IReadOnlyList<string> All => Defs.Select(d => d.Slot).ToList();

    public static string? AstroPath(string projectPath, string slot)
    {
        var rel = Defs.FirstOrDefault(d => d.Slot == slot)?.Path;
        if (string.IsNullOrWhiteSpace(rel)) return null;
        return ProjectPaths.Src(projectPath, rel.Replace('/', Path.DirectorySeparatorChar));
    }

    private static List<SlotDef> Load()
    {
        var path = ResolvePath();
        if (path is null || JsonFile.Read<List<SlotDef>>(path) is not { Count: > 0 } loaded)
            return Defaults.ToList();

        var order = new List<string>(Defaults.Select(d => d.Slot));
        var byName = Defaults.ToDictionary(d => d.Slot, StringComparer.Ordinal);
        foreach (var d in loaded)
        {
            if (string.IsNullOrWhiteSpace(d.Slot)) continue;
            if (!byName.ContainsKey(d.Slot)) order.Add(d.Slot);
            byName[d.Slot] = d;
        }
        return order.Select(s => byName[s]).ToList();
    }

    private static string? ResolvePath()
    {
        if (Workspace.DevLayoutMode)
        {
            if (Workspace.WorkspaceComponentDir is { } local)
            {
                var p = System.IO.Path.Combine(local, FileName);
                if (File.Exists(p)) return p;
            }
            return null;
        }
        var shipped = System.IO.Path.Combine(ComponentStore.Root, FileName);
        return File.Exists(shipped) ? shipped : null;
    }
}
