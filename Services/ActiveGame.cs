namespace TEMO.AI;

internal static class ActiveGame
{
    public static bool IsMascot(string root)
    {
        var astro = Io.ReadOrNull(ProjectPaths.Src(root, @"components\sections\Game.astro"));
        return astro is not null && astro.Contains("games-mascot", StringComparison.Ordinal);
    }
}
