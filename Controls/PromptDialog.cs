namespace TEMO.AI;

internal sealed class PromptDialog : Window
{
    private readonly TextBox _input;
    private readonly TextBlock? _hintText;
    private readonly Func<string, bool>? _validate;
    private readonly string? _invalidMessage;

    public string Value => _input.Text.Trim();

    public PromptDialog(
        string title,
        string label,
        string defaultValue = "",
        Func<string, bool>? validate = null,
        string? invalidMessage = null,
        bool filterInput = false,
        int maxLength = 0)
    {
        Title = title;
        Width = 440;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        Ui.StyleDialog(this);

        _validate = validate;
        _invalidMessage = invalidMessage;

        var root = new StackPanel { Margin = new Thickness(22, 20, 22, 22) };

        root.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 12,
            Foreground = Ui.Brush(0xB8B8B8),
            Margin = new Thickness(0, 0, 0, 6),
        });

        if (filterInput && !string.IsNullOrEmpty(invalidMessage))
        {
            _hintText = new TextBlock
            {
                Text = invalidMessage,
                FontSize = 11,
                Foreground = Ui.Brush(0x666666),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8),
            };
            root.Children.Add(_hintText);
        }

        _input = Ui.MakeDarkInput(defaultValue);
        _input.Margin = new Thickness(0, 0, 0, 16);
        _input.KeyDown += (_, e) => { if (e.Key == Key.Enter) Confirm(); };

        if (filterInput)
        {
            _input.PreviewTextInput += OnPreviewTextInput;
            System.Windows.DataObject.AddPastingHandler(_input, OnPaste);
            _input.TextChanged += (_, _) => ApplyInputFilter();
            if (maxLength > 0) _input.MaxLength = maxLength;
        }

        root.Children.Add(_input);

        var okBtn = Ui.DialogButton("ตกลง", accent: true);
        okBtn.Click += (_, _) => Confirm();

        var cancelBtn = Ui.DialogButton("ยกเลิก", accent: false);
        cancelBtn.Click += (_, _) => { DialogResult = false; };

        root.Children.Add(Ui.BuildOkCancelRow(okBtn, cancelBtn));

        Content = root;
        Loaded += (_, _) => { _input.Focus(); _input.SelectAll(); };
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = !IsAllowedText(e.Text);

    private void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(System.Windows.DataFormats.Text)) return;

        var pasted = e.DataObject.GetData(System.Windows.DataFormats.Text) as string ?? "";
        if (IsAllowedText(pasted)) return;

        e.CancelCommand();

        var filtered = VercelNames.FilterWhileTyping(pasted);
        if (string.IsNullOrEmpty(filtered)) return;

        var caret = _input.CaretIndex;
        var text = _input.Text;
        var selStart = _input.SelectionStart;
        var selLen = _input.SelectionLength;
        var merged = text[..selStart] + filtered + text[(selStart + selLen)..];
        _input.Text = VercelNames.FilterWhileTyping(merged);
        _input.CaretIndex = Math.Min(caret + filtered.Length, _input.Text.Length);
    }

    private void ApplyInputFilter()
    {
        var caret = _input.CaretIndex;
        var filtered = VercelNames.FilterWhileTyping(_input.Text);
        if (filtered == _input.Text) return;

        _input.Text = filtered;
        _input.CaretIndex = Math.Min(caret, filtered.Length);
    }

    private static bool IsAllowedText(string text) => text.All(VercelNames.IsAllowedChar);

    private void Confirm()
    {
        var value = Value;
        if (string.IsNullOrWhiteSpace(value)) return;

        if (_validate is not null && !_validate(value))
        {
            if (_hintText is not null && !string.IsNullOrEmpty(_invalidMessage))
            {
                _hintText.Text = _invalidMessage;
                _hintText.Foreground = Ui.Brush(0xCC6666);
            }
            _input.BorderBrush = Ui.Brush(0xCC4444);
            _input.Focus();
            _input.SelectAll();
            return;
        }

        DialogResult = true;
    }
}
