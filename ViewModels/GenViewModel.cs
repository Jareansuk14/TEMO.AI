namespace TEMO.AI;

internal sealed class GenViewModel
{
    private GenQueueWindow? _window;

    public void OpenQueue(Window owner, Action<string> onProjectCreated)
    {
        if (_window is { IsLoaded: true })
        {
            _window.Activate();
            return;
        }

        _window = new GenQueueWindow { Owner = owner };
        _window.ProjectCreated += onProjectCreated;
        _window.Closed += (_, _) => _window = null;
        _window.Show();
    }
}
