using Velopack;
using Velopack.Sources;

namespace TEMO.AI;

internal static class AppUpdateService
{
    public static async Task<bool> EnsureReadyToStartAsync()
    {
        var dialog = new AppUpdateDialog();
        var manager = new UpdateManager(new GithubSource(VaultGate.Get(Vk.V4), accessToken: null, prerelease: false));

        if (!manager.IsInstalled)
        {
#if DEBUG
            return true;
#else
            dialog.SetBlocked("กรุณาติดตั้งโปรแกรมผ่าน installer ก่อนเข้าใช้งาน");
            dialog.Show();
            await WaitUntilClosedAsync(dialog);
            return false;
#endif
        }

        try
        {
            dialog.SetChecking();
            dialog.Show();

            if (manager.UpdatePendingRestart is not null)
            {
                dialog.SetApplying();
                manager.ApplyUpdatesAndRestart(manager.UpdatePendingRestart);
                return false;
            }

            var update = await manager.CheckForUpdatesAsync();
            if (update is null)
                return true;

            await manager.DownloadUpdatesAsync(update, percent =>
                dialog.Dispatcher.InvokeAsync(() => dialog.SetDownloading(percent)));

            dialog.SetApplying();
            manager.ApplyUpdatesAndRestart(update);
            return false;
        }
        catch (Exception ex)
        {
            dialog.SetBlocked($"ตรวจสอบอัปเดตไม่สำเร็จ: {ex.Message}");
            await WaitUntilClosedAsync(dialog);
            return false;
        }
        finally
        {
            if (dialog.IsVisible)
                dialog.Close();
        }
    }

    private static Task WaitUntilClosedAsync(Window window)
    {
        var tcs = new TaskCompletionSource();
        window.Closed += (_, _) => tcs.TrySetResult();
        return tcs.Task;
    }
}
