namespace TEMO.AI;

internal sealed class ContentViewModel
{
    private readonly ProjectSession _session;

    public ContentViewModel(ProjectSession session) => _session = session;

    public void BuildFields()
    {
        _session.Fields.Clear();
        _session.Fields.AddRange(
            ContentStore.BuildFields(_session.ProjectPath, _session.LayoutComponents));
    }
}
