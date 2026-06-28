namespace TEMO.AI;

internal sealed class GenDialog : Window
{
    private readonly TextBox _brand;
    private readonly System.Windows.Controls.RadioButton _casino;
    private readonly System.Windows.Controls.RadioButton _lottery;
    private readonly System.Windows.Controls.RadioButton _slot;
    private readonly System.Windows.Controls.ComboBox _gameCount;
    private readonly Button _generateBtn;
    private readonly Button _cancelBtn;
    private readonly TextBlock _status;

    public GenerationOptions? Options { get; private set; }

    public GenDialog()
    {
        Title = "TEMO.GEN — สร้างเว็บอัตโนมัติ";
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

        root.Children.Add(Ui.FieldLabel("จำนวนการ์ดเกม"));
        _gameCount = new System.Windows.Controls.ComboBox
        {
            Height = 34,
            Margin = new Thickness(0, 0, 0, 16),
            Background = Ui.Brush(0x111111),
            Foreground = Ui.Brush(0xDEDEDE),
            BorderBrush = Ui.Brush(0x333333),
            SelectedValuePath = "Tag",
        };
        _gameCount.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "ไม่ใช้การ์ดเกม", Tag = "" });
        _gameCount.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "2 รูป", Tag = "2" });
        _gameCount.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "4 รูป", Tag = "4" });
        _gameCount.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "6 รูป", Tag = "6" });
        _gameCount.SelectedIndex = 0;
        root.Children.Add(_gameCount);

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

    private int? SelectedGameCount =>
        int.TryParse(_gameCount.SelectedValue?.ToString(), out var count) ? count : null;

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

        Options = new GenerationOptions(brand, SelectedType, SelectedGameCount, AiModels.TextDefault);
        DialogResult = true;
    }

    private void Cancel()
    {
        DialogResult = false;
    }
}
