using Velopack;

namespace TEMO.AI;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        ToolDetectionService.CheckAndExitIfSuspiciousToolsFound();

        ExtractCacheCleanupService.CleanupOldExtractCaches();
        CleanupLegacyExtractCache();
        EnsureNodeAvailable();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    private static void CleanupLegacyExtractCache() => Io.DeleteDirectory(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TEMO.AI", "System"));

    private static void EnsureNodeAvailable()
    {
        if (NodeRuntime.IsAvailable()) return;

        if (NodeRuntime.FindInstaller() is { } script)
        {
            if (NodeInstallUi.RunInstall(script))
            {
                System.Windows.MessageBox.Show(
                    "ติดตั้งสำเร็จ\nโปรแกรมพร้อมใช้งานแล้ว",
                    "TEMO.AI", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }
        }

        NodeRuntime.OpenDownloadPage();
        System.Windows.MessageBox.Show(
            "ไม่พบ Node.js และติดตั้งอัตโนมัติไม่สำเร็จ\n" +
            "โปรแกรมจำเป็นต้องใช้ Node.js ในการรันและพรีวิวโปรเจค\n\n" +
            "ระบบได้เปิดหน้าดาวน์โหลดให้แล้ว กรุณาติดตั้ง Node.js แล้วเปิดโปรแกรมใหม่อีกครั้ง",
            "TEMO.AI", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
    }
}
