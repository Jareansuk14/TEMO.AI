namespace TEMO.AI;

internal sealed class GenDialog : Window
{
    private readonly TextBox _brand;
    private readonly System.Windows.Controls.RadioButton _casino;
    private readonly System.Windows.Controls.RadioButton _lottery;
    private readonly System.Windows.Controls.RadioButton _slot;
    private readonly Button _generateBtn;
    private readonly Button _cancelBtn;
    private readonly TextBlock _status;

    public GenerationOptions? Options { get; private set; }

    public GenDialog()
    {
        Title = "TEMO.AI — สร้างเว็บอัตโนมัติ";
        Width = 460;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        Ui.StyleDialog(this);

        var root = new StackPanel { Margin = new Thickness(22, 20, 22, 22) };

        root.Children.Add(Ui.FieldLabel("ชื่อแบรนด์"));
        _brand = Ui.MakeDarkInput();
        _brand.Margin = new Thickness(0, 0, 0, 16);
        _brand.KeyDown += (_, e) => { if (e.Key == Key.Enter) AddToQueue(); };
        root.Children.Add(_brand);

        root.Children.Add(Ui.FieldLabel("ประเภทเนื้อหาเว็บ"));
        var types = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };
        _casino = MakeRadio("คาสิโน", isDefault: true);
        _lottery = MakeRadio("หวย", isDefault: false);
        _slot = MakeRadio("สล็อต", isDefault: false);
        types.Children.Add(_casino);
        types.Children.Add(_lottery);
        types.Children.Add(_slot);
        root.Children.Add(types);

        _status = new TextBlock
        {
            Text = "",
            FontSize = 12,
            Foreground = Ui.Brush(0x888888),
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 18,
            Margin = new Thickness(0, 0, 0, 14),
        };
        root.Children.Add(_status);

        _generateBtn = Ui.DialogButton("เพิ่มคิว", accent: true);
        _generateBtn.Click += (_, _) => AddToQueue();

        _cancelBtn = Ui.DialogButton("ยกเลิก", accent: false);
        _cancelBtn.Click += (_, _) => Cancel();

        root.Children.Add(Ui.BuildOkCancelRow(_generateBtn, _cancelBtn));

        Content = root;
        Loaded += (_, _) => { _brand.Focus(); _brand.SelectAll(); };
    }

    private System.Windows.Controls.RadioButton MakeRadio(string label, bool isDefault) => new()
    {
        Content = label,
        GroupName = "GenType",
        IsChecked = isDefault,
        Foreground = Ui.Brush(0xDEDEDE),
        FontSize = 13,
        Margin = new Thickness(0, 0, 18, 0),
        VerticalContentAlignment = VerticalAlignment.Center,
    };

    private AiPromptType SelectedType =>
        ContentTypes.FromRadios(_lottery.IsChecked == true, _slot.IsChecked == true);

    private void AddToQueue()
    {
        var brand = _brand.Text.Trim();
        if (string.IsNullOrWhiteSpace(brand))
        {
            _status.Text = "กรุณากรอกชื่อแบรนด์";
            _status.Foreground = Ui.Brush(0xCC6666);
            _brand.Focus();
            return;
        }

        Options = new GenerationOptions(brand, SelectedType, AiModels.TextDefault);
        DialogResult = true;
    }

    private void Cancel()
    {
        DialogResult = false;
    }
}
