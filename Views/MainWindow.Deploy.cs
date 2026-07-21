namespace TEMO.AI;

public partial class MainWindow
{
    private void Deploy_Click(object sender, RoutedEventArgs e)
    {
        if (!HasOpenProject())
        {
            ShowMsg("ยังไม่ได้เลือกโปรเจค กด \"New Project\" หรือ \"Load Project\"");
            return;
        }

        if (_devProcess is { HasExited: false } && !TryStopServer())
            return;

        try
        {
            _vm.Deploy.OpenDeploy(this);
        }
        catch (Exception ex)
        {
            ShowMsg($"เปิดหน้าต่าง Deploy ไม่สำเร็จ:\n{ex.Message}");
        }
    }
}
