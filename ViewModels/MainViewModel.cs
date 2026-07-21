namespace TEMO.AI;

/// Root coordinator that owns the shared <see cref="ProjectSession"/> and the
/// per-cluster view models. MainWindow delegates to these as the UI migrates to MVVM.
internal sealed class MainViewModel
{
    public ProjectSession Session { get; } = new();

    public DeployViewModel Deploy { get; }
    public GenViewModel Gen { get; } = new();
    public AiOverlayViewModel Ai { get; } = new();
    public ContentViewModel Content { get; }
    public ImagesViewModel Images { get; }

    public MainViewModel()
    {
        Deploy = new DeployViewModel(Session);
        Content = new ContentViewModel(Session);
        Images = new ImagesViewModel(Session);
    }
}
