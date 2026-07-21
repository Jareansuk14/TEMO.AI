namespace TEMO.AI;

internal sealed class AppUpdateDialog : ProgressDialog
{
    private readonly Button _closeButton;

    public AppUpdateDialog()
        : base("Checking for updates", "กำลังตรวจสอบเวอร์ชันล่าสุด...",
            "ต้องเชื่อมต่ออินเทอร์เน็ตเพื่อเข้าใช้งาน")
    {
        _closeButton = Ui.DialogButton("ปิดโปรแกรม", accent: false);
        _closeButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
        _closeButton.Margin = new Thickness(0, 18, 0, 0);
        _closeButton.Visibility = Visibility.Collapsed;
        _closeButton.Click += (_, _) => Close();
        Body.Children.Add(_closeButton);
    }

    public void SetChecking()
    {
        TitleBlock.Text = "Checking for updates";
        MessageBlock.Text = "กำลังตรวจสอบเวอร์ชันล่าสุด...";
        DetailBlock.Text = "ต้องเชื่อมต่ออินเทอร์เน็ตเพื่อเข้าใช้งาน";
        Bar.IsIndeterminate = true;
        Bar.Value = 0;
        _closeButton.Visibility = Visibility.Collapsed;
    }

    public void SetDownloading(int percent)
    {
        TitleBlock.Text = "Update required";
        MessageBlock.Text = $"กำลังดาวน์โหลดอัปเดต {percent}%";
        DetailBlock.Text = "โปรแกรมจะติดตั้งและเปิดใหม่อัตโนมัติเมื่อดาวน์โหลดเสร็จ";
        Bar.IsIndeterminate = false;
        Bar.Value = Math.Clamp(percent, 0, 100);
        _closeButton.Visibility = Visibility.Collapsed;
    }

    public void SetApplying()
    {
        TitleBlock.Text = "Installing update";
        MessageBlock.Text = "กำลังติดตั้งอัปเดตและเปิดโปรแกรมใหม่...";
        DetailBlock.Text = "กรุณารอสักครู่";
        Bar.IsIndeterminate = true;
        _closeButton.Visibility = Visibility.Collapsed;
    }

    public void SetBlocked(string message)
    {
        TitleBlock.Text = "Update required";
        MessageBlock.Text = message;
        DetailBlock.Text = "ยังไม่สามารถเข้าโปรแกรมได้จนกว่าจะตรวจสอบหรืออัปเดตสำเร็จ";
        Bar.IsIndeterminate = false;
        Bar.Value = 0;
        _closeButton.Visibility = Visibility.Visible;
    }
}
