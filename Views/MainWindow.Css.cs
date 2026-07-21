namespace TEMO.AI;

public partial class MainWindow
{
    private void LoadCssVariables()
    {
        _cssBoxes.Clear();
        _cssColorPreviews.Clear();
        CssPanel.Children.Clear();

        CssPanel.Children.Add(new TextBlock
        {
            Text = "CSS Variables (theme.css :root)",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0xAAAAAA),
            Margin = new Thickness(0, 0, 0, 16)
        });

        var vars = CssStore.ReadVariables(_projectPath);
        if (vars is null)
        {
            CssPanel.Children.Add(new TextBlock
            {
                Text = "⚠️ theme.css not found",
                FontSize = 12,
                Foreground = Ui.Brush(0xFF8888)
            });
            return;
        }

        foreach (var (name, value) in vars)
            CssPanel.Children.Add(BuildCssVariableRow(name, value));
    }

    private Grid BuildCssVariableRow(string varName, string varValue)
    {
        var label = new TextBlock
        {
            Text = $"--{varName}",
            FontSize = 12,
            Foreground = Ui.Brush(0xB8B8B8),
            Margin = new Thickness(0, 0, 0, 5),
            FontFamily = new FontFamily("Consolas")
        };

        var box = new TextBox
        {
            Style = (Style)FindResource("Input"),
            FontSize = 12,
            FontFamily = new FontFamily("Consolas"),
            Text = varValue,
            Height = double.NaN,
            MinHeight = 36,
            TextWrapping = TextWrapping.NoWrap,
            AcceptsReturn = false,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0),
        };

        var colorPreview = new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            BorderBrush = Ui.Brush(0x383838),
            Background = Ui.Brush(0x161616),
            Cursor = Cursors.Hand,
            ToolTip = "คลิกเพื่อเลือกสี",
            Margin = new Thickness(8, 0, 0, 0)
        };

        box.TextChanged += (_, _) =>
        {
            UpdateCssColorPreview(varName);
            if (!_suppressSaveTracking) ScheduleSaveAllUi();
        };
        colorPreview.MouseLeftButtonDown += (_, _) => PickCssColor(varName);

        _cssBoxes[varName] = box;
        _cssColorPreviews[varName] = colorPreview;

        var row = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        row.RowDefinitions.Add(new RowDefinition());
        row.RowDefinitions.Add(new RowDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumnSpan(label, 2);
        row.Children.Add(label);
        Grid.SetRow(box, 1);
        row.Children.Add(box);
        Grid.SetRow(colorPreview, 1);
        Grid.SetColumn(colorPreview, 1);
        row.Children.Add(colorPreview);

        UpdateCssColorPreview(varName);
        return row;
    }

    private void UpdateCssColorPreview(string varName)
    {
        if (!_cssBoxes.TryGetValue(varName, out var box)
            || !_cssColorPreviews.TryGetValue(varName, out var preview))
            return;

        if (CssColor.TryExtract(box.Text, out var colorText) && CssColor.TryParse(colorText, out var color))
        {
            preview.Visibility = Visibility.Visible;
            preview.Background = Ui.Brush(color);
            preview.ToolTip = $"คลิกเพื่อเปลี่ยนสี ({colorText})";
        }
        else
        {
            preview.Visibility = Visibility.Collapsed;
        }
    }

    private void PickCssColor(string varName)
    {
        if (!_cssBoxes.TryGetValue(varName, out var box)) return;

        var currentText = box.Text;
        CssColor.TryExtract(currentText, out var colorText);
        CssColor.TryParse(colorText, out var currentColor);

        var dialog = new ColorPickerDialog(currentColor, CssColor.IsRgb(colorText)) { Owner = this };
        if (dialog.ShowDialog() != true) return;

        var selected = dialog.SelectedFormat == ColorPickerDialog.OutputFormat.Rgba
            ? CssColor.ToRgba(dialog.SelectedColor)
            : CssColor.ToHex(dialog.SelectedColor);
        box.Text = string.IsNullOrWhiteSpace(colorText)
            ? selected
            : CssColor.ReplaceFirst(currentText, selected);
        box.Focus();
        box.CaretIndex = box.Text.Length;
    }

    private void SaveCssVariables() =>
        CssStore.Save(_projectPath, _cssBoxes.ToDictionary(kv => kv.Key, kv => kv.Value.Text));
}
