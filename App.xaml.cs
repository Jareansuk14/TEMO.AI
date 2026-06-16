namespace TEMO.AI;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);

        if (!await AppUpdateService.EnsureReadyToStartAsync())
        {
            Shutdown();
            return;
        }

        var login = new LoginWindow();
        MainWindow = login;
        login.Show();
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }
}
