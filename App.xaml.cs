namespace TEMO.AI;

public partial class App : System.Windows.Application
{
    private DispatcherTimer? _protectionTimer;

    protected override async void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);

        ToolDetectionService.CheckAndExitIfSuspiciousToolsFound();
        StartPeriodicProtectionScan();

        if (!await AppUpdateService.EnsureReadyToStartAsync())
        {
            Shutdown();
            return;
        }

        await EnsureTemplatesLatestAsync();

        if (await AuthSession.TryRestoreSessionAsync())
        {
            ShowMainWindow();
            return;
        }

        var login = new LoginWindow();
        MainWindow = login;
        login.Show();
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    internal static void ShowMainWindow()
    {
        var main = new MainWindow();
        Current.MainWindow = main;
        main.Show();
        Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _protectionTimer?.Stop();
        base.OnExit(e);
    }

    private void StartPeriodicProtectionScan()
    {
        _protectionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(60)
        };
        _protectionTimer.Tick += (_, _) => ToolDetectionService.CheckAndExitIfSuspiciousToolsFound();
        _protectionTimer.Start();
    }

    private static async Task EnsureTemplatesLatestAsync()
    {
        if (Workspace.DevLayoutMode) return;

        var dialog = new TemplateUpdateProgressDialog();
        dialog.Show();
        var progress = new Progress<string>(dialog.SetMessage);
        try
        {
            await TemplateStore.EnsureLatestAsync(progress);
        }
        catch { }
        finally
        {
            dialog.Close();
        }
    }
}
