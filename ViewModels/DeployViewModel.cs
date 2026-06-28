namespace TEMO.AI;

internal sealed class DeployViewModel
{
    private readonly ProjectSession _session;

    public DeployViewModel(ProjectSession session) => _session = session;

    public bool CanDeploy => ProjectPaths.IsProject(_session.ProjectPath);

    public void OpenDeploy(Window owner) =>
        new VercelDeployDialog(_session.ProjectPath) { Owner = owner }.ShowDialog();
}
