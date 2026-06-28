namespace TEMO.AI;

internal sealed class TemplateGalleryDialog : Window
{
    public const string PreviewRelPath = @"public\Preview.png";

    public string? SelectedTemplate { get; private set; }

    private List<string> _templates;
    private readonly WrapPanel _grid;
    private readonly Button _updateBtn;
    private readonly TextBlock _statusText;

    public TemplateGalleryDialog()
    {
        _templates = TemplateStore.List();

        Title = "เลือก Template — เริ่มโปรเจคใหม่";
        Width = 1000;
        Height = 720;
        Ui.StyleDialog(this);

        var root = new DockPanel();

        var bar = new Border
        {
            Background = Ui.Brush(0x0D0D0D),
            BorderBrush = Ui.Brush(0x1E1E1E),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(18, 14, 16, 14),
        };
        DockPanel.SetDock(bar, Dock.Top);

        var barGrid = new Grid();
        barGrid.ColumnDefinitions.Add(new ColumnDefinition());
        barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var heading = new TextBlock
        {
            Text = "Templates",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0xFFFFFF),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(heading, 0);

        _updateBtn = Ui.DialogButton("Update Template", accent: true);
        _updateBtn.Click += UpdateTemplate_Click;
        Grid.SetColumn(_updateBtn, 1);

        barGrid.Children.Add(heading);
        barGrid.Children.Add(_updateBtn);
        bar.Child = barGrid;
        root.Children.Add(bar);

        var content = new DockPanel();

        _statusText = new TextBlock
        {
            Text = "",
            Foreground = Ui.Brush(0x888888),
            FontSize = 12,
            Margin = new Thickness(18, 12, 18, 0),
        };
        DockPanel.SetDock(_statusText, Dock.Top);
        content.Children.Add(_statusText);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(18, 16, 18, 16),
        };
        _grid = new WrapPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        scroll.Content = _grid;
        content.Children.Add(scroll);
        root.Children.Add(content);

        Content = root;
        RebuildGrid();
    }

    private async void UpdateTemplate_Click(object sender, RoutedEventArgs e)
    {
        _updateBtn.IsEnabled = false;
        _updateBtn.Content = "Updating...";
        _statusText.Text = "กำลังโหลด Templates ใหม่...";

        var progressDialog = new TemplateUpdateProgressDialog { Owner = this };
        var progress = new Progress<string>(progressDialog.SetMessage);
        progressDialog.Show();
        IsEnabled = false;

        try
        {
            await TemplateStore.UpdateFromRemoteAsync(progress);
            _templates = TemplateStore.List();
            RebuildGrid();
            _statusText.Text = $"อัปเดต Templates สำเร็จ ({_templates.Count} รายการ)";
        }
        catch (Exception ex)
        {
            _statusText.Text = $"โหลด Templates ไม่สำเร็จ: {ex.Message}";
        }
        finally
        {
            IsEnabled = true;
            progressDialog.Close();
            _updateBtn.Content = "Update Template";
            _updateBtn.IsEnabled = true;
        }
    }

    private void RebuildGrid()
    {
        _grid.Children.Clear();

        if (_templates.Count == 0)
        {
            _grid.Children.Add(new TextBlock
            {
                Text = "ยังไม่มี Template กด \"Update Template\"",
                Foreground = Ui.Brush(0x555555),
                FontSize = 13,
                Margin = new Thickness(4, 20, 0, 0),
            });
            return;
        }

        foreach (var path in _templates)
            _grid.Children.Add(MakeCard(path));
    }

    private void DeleteTemplate(string templatePath, string name)
    {
        var result = System.Windows.MessageBox.Show(
            this,
            $"ต้องการลบ Template \"{name}\" ออกจากเครื่องหรือไม่?",
            "ลบ Template",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            TemplateStore.Delete(templatePath);
            _templates = TemplateStore.List();
            RebuildGrid();
            _statusText.Text = $"ลบ Template \"{name}\" แล้ว";
        }
        catch (Exception ex)
        {
            _statusText.Text = $"ลบ Template ไม่สำเร็จ: {ex.Message}";
        }
    }

    private Border MakeCard(string templatePath)
    {
        const int CardW = 220;

        var name = new DirectoryInfo(templatePath.TrimEnd('\\', '/')).Name;
        var previewPath = Path.Combine(templatePath, PreviewRelPath);
        bool exists = Directory.Exists(templatePath);

        var thumb = new System.Windows.Controls.Image
        {
            Stretch = System.Windows.Media.Stretch.UniformToFill,
            VerticalAlignment = VerticalAlignment.Top,
            Source = Ui.LoadBitmap(previewPath, CardW),
        };

        var thumbHost = Ui.MakeGalleryThumbHost(thumb);
        thumbHost.MouseDown += (_, _) =>
            new PreviewWindow(previewPath, name) { Owner = this }.ShowDialog();

        var thumbOverlay = new TextBlock
        {
            Text = "ดูเต็ม",
            FontSize = 9,
            Foreground = Ui.Brush(0xCFCFCF),
            Background = Ui.Brush(0xAA000000u),
            Padding = new Thickness(6, 2, 6, 2),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 6, 6),
        };

        var isLocal = TemplateStore.IsLocal(templatePath);
        var badge = new TextBlock
        {
            Text = isLocal ? "Offline" : "Online",
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0xFFFFFF),
            Background = Ui.Brush(isLocal ? 0xAAB58A2Bu : 0xAA2E7D32u),
            Padding = new Thickness(6, 2, 6, 2),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(6, 6, 0, 0),
        };

        var thumbGrid = new Grid();
        thumbGrid.Children.Add(thumbHost);
        thumbGrid.Children.Add(thumbOverlay);
        thumbGrid.Children.Add(badge);

        var nameText = new TextBlock
        {
            Text = name,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = exists ? Ui.Brush(0xEDEDED) : Ui.Brush(0xCC5555),
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 0, 8),
        };

        var useBtn = Ui.DialogButton("ใช้ Template นี้", accent: true);
        useBtn.IsEnabled = exists;
        useBtn.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        useBtn.Click += (_, _) =>
        {
            SelectedTemplate = templatePath;
            DialogResult = true;
        };

        var btnRow = new Grid();
        btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(useBtn, 0);
        btnRow.Children.Add(useBtn);

        if (isLocal)
        {
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });

            var deleteBtn = Ui.DialogDangerButton("ลบ");
            deleteBtn.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            deleteBtn.Margin = new Thickness(6, 0, 0, 0);
            deleteBtn.Click += (_, _) => DeleteTemplate(templatePath, name);
            Grid.SetColumn(deleteBtn, 1);
            btnRow.Children.Add(deleteBtn);
        }

        var info = new StackPanel { Margin = new Thickness(10, 8, 10, 10) };
        info.Children.Add(nameText);
        info.Children.Add(btnRow);

        return Ui.MakeGalleryCard(thumbGrid, info, CardW);
    }
}
