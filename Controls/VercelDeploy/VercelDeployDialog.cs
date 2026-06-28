namespace TEMO.AI;

internal sealed partial class VercelDeployDialog : Window
{
    private readonly VercelDeployService _service;
    private readonly string _projectPath;

    private ColumnDefinition _logColumn = null!;
    private Button _loginBtn = null!;
    private Button _logoutBtn = null!;
    private Button _createProjectBtn = null!;
    private Button _deployBtn = null!;
    private Button _logToggleBtn = null!;
    private TextBlock _accountNameText = null!;
    private TextBlock _statusText = null!;
    private LoadingPanel _projectPanel = null!;
    private VercelScopeOption? _currentScope;
    private DataGrid _projectTable = null!;
    private TextBox _logBox = null!;

    private bool _logVisible;
    private bool _loginInProgress;
    private int _operationCount;
    private bool _suppressProjectLoad;
    private int _projectLoadGeneration;
    private CancellationTokenSource? _loginPollCts;
    private CancellationTokenSource? _deployCts;
    private List<VercelProjectOption> _projects = [];

    public VercelDeployDialog(string projectPath)
    {
        _projectPath = projectPath;
        _service = new VercelDeployService(projectPath);

        Title = "Deploy to Vercel";
        Width = 1000;
        Height = 800;
        MinWidth = 1000;
        MaxWidth = 1000;
        MinHeight = 800;
        MaxHeight = 800;
        ResizeMode = ResizeMode.NoResize;
        Ui.StyleDialog(this);
        Background = Ui.Brush(0x0B0B0B);

        Content = BuildContent();

        Loaded += (_, _) => _ = LoadTeamsSafeAsync();
        Closed += (_, _) =>
        {
            _loginPollCts?.Cancel();
            _loginPollCts?.Dispose();
            _deployCts?.Cancel();
            _deployCts?.Dispose();
            StopLogTimer();
        };
    }

    private void BeginOp()
    {
        if (++_operationCount == 1) ApplyBusyState(true);
    }

    private void EndOp()
    {
        if (--_operationCount <= 0)
        {
            _operationCount = 0;
            ApplyBusyState(false);
        }
    }

    private void ApplyBusyState(bool busy)
    {
        _loginBtn.IsEnabled = !busy;
        _logoutBtn.IsEnabled = !busy && _currentScope is not null;
        _deployBtn.IsEnabled = !busy;
        UpdateCreateProjectButton(busy);
        _projectTable.IsEnabled = !busy;
    }

    private void UpdateCreateProjectButton(bool busy = false)
    {
        _createProjectBtn.IsEnabled = !busy && _currentScope is not null;
    }
}
