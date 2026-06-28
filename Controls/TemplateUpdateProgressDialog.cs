namespace TEMO.AI;

internal sealed class TemplateUpdateProgressDialog : ProgressDialog
{
    public TemplateUpdateProgressDialog()
        : base("Update Template", "กำลังเตรียมข้อมูล...",
            "กรุณารอสักครู่ ระบบจะปิดหน้าต่างนี้เมื่อเสร็จ", 440, 180)
    {
    }

    public void SetMessage(string message) => MessageBlock.Text = message;
}
