namespace TEMO.AI;

public partial class MainWindow
{
    private void Gen_Click(object sender, RoutedEventArgs e) =>
        _vm.Gen.OpenQueue(this, path =>
            Dispatcher.Invoke(() =>
            {
                RefreshNewBadge();
                ShowMsg($"✅  GEN สร้างเว็บเสร็จแล้ว: {Path.GetFileName(path)} — กด Load Project เพื่อเปิด");
            }));
}
