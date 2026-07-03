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

        await EnsureTemplatesLatestAsync();

        var login = new LoginWindow();
        MainWindow = login;
        login.Show();
        ShutdownMode = ShutdownMode.OnMainWindowClose;
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
