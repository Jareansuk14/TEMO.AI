namespace TEMO.AI;

internal static class NodeInstallUi
{
    public static bool RunInstall(string script)
    {
        var window = BuildWindow();
        window.Show();

        var success = false;
        var frame = new DispatcherFrame();

        Task.Run(() =>
        {
            NodeRuntime.RunInstaller(script);
            success = NodeRuntime.IsAvailable() || NodeRuntime.IsInstalledOnDisk();
        }).ContinueWith(_ => window.Dispatcher.BeginInvoke(() => frame.Continue = false));

        Dispatcher.PushFrame(frame);
        window.Close();
        return success;
    }

    private static Window BuildWindow()
    {
        var window = new ProgressDialog(
            "กำลังติดตั้งโปรแกรม",
            "กรุณารอสักครู่ ระบบกำลังติดตั้งส่วนประกอบที่จำเป็น...",
            "ห้ามปิดหน้าต่างนี้จนกว่าจะติดตั้งเสร็จ")
        {
            Title = "TEMO.AI",
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };
        return window;
    }
}
