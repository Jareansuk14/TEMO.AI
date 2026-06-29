namespace TEMO.AI;

internal sealed record ComponentCounts(int Headings, int Images);

internal static class ComponentRandomizer
{
    public static ComponentCounts Roll(ContentSpec spec, Random rng)
    {
        var headings = RollRange(spec.HeadingMin, spec.HeadingMax, rng);
        var images = RollRange(spec.ImageMin, spec.ImageMax, rng);

        switch ((spec.Link ?? "none").ToLowerInvariant())
        {
            case "imagesfollowheadings":
                images = headings;
                break;
            case "headingsfollowimages":
                headings = images;
                break;
        }

        return new ComponentCounts(headings, images);
    }

    private static int RollRange(int min, int max, Random rng)
    {
        if (min < 0) min = 0;
        if (max <= 0) return min;
        if (min > max) min = max;
        return min == max ? min : rng.Next(min, max + 1);
    }
}
