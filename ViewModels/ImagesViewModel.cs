namespace TEMO.AI;

internal sealed class ImagesViewModel
{
    private readonly ProjectSession _session;

    public ImagesViewModel(ProjectSession session) => _session = session;

    public int PromoCount => _session.ImageEntries.Count(e => e.Id.StartsWith("promo-"));

    public void LoadEntries()
    {
        _session.ImageEntries.Clear();
        var content = ImagesStore.ReadConfig(_session.ProjectPath);
        var defs = ImagesStore.DiscoverDefs(content,
            ImagesStore.IsPlayButtonUsed(_session.ProjectPath),
            ImagesStore.SeoImageNumbers(_session.ProjectPath));
        foreach (var (id, label, group, hasAlt) in defs)
            _session.ImageEntries.Add(new ImageEntry { Id = id, Label = label, Group = group, HasAlt = hasAlt });
    }

    public bool PullValues()
    {
        var content = ImagesStore.ReadConfig(_session.ProjectPath);
        if (content.Length == 0) return false;

        foreach (var e in _session.ImageEntries)
        {
            var (src, alt) = ImagesStore.ReadValues(content, e.Id);
            e.SrcValue = src;
            e.AltValue = alt;
            e.OriginalSrc = src;
        }
        return true;
    }
}
