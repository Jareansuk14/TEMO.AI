using Binding = System.Windows.Data.Binding;

namespace TEMO.AI;

internal sealed partial class GenQueueWindow : Window
{
    private static readonly string[] Stages =
    [
        "สร้างไฟล์พื้นฐาน",
        "สร้าง Layout",
        "สร้างเนื้อหา",
        "สร้างรูปภาพ",
        "สร้าง CSS",
        "เสร็จสิ้น",
    ];

    private const uint DoneColor = 0x3FD17A;
    private const uint CurrentColor = 0xF5A623;
    private const uint ErrorColor = 0xE0533B;
    private const uint PendingColor = 0x5A5A5A;
    private const double NodeSize = 30;
    private const double HeaderHeight = 76;

    private static readonly StatusBrushConverter StatusConverter = new();
    private static readonly QueueIndexConverter IndexConverter = new();
    private static readonly PendingVisibilityConverter PendingVisibility = new();
    private static readonly DetailBrushConverter DetailBrush = new();

    private readonly List<GenQueueItem> _queue = [];
    private readonly System.Windows.Controls.ListBox _queueList;
    private readonly StackPanel _flowPanel;
    private readonly TextBlock _flowTitle;
    private readonly TextBlock _statusBar;
    private readonly System.Windows.Controls.Button _startBtn;
    private readonly System.Windows.Controls.Button _addBtn;
    private bool _running;

    public event Action<string>? ProjectCreated;

    public GenQueueWindow()
    {
        Title = "TEMO.GEN";
        Width = 900;
        Height = 800;
        ResizeMode = ResizeMode.NoResize;
        Ui.StyleDialog(this);

        _addBtn = Ui.DialogButton("+ เพิ่มคิว", accent: false);
        _addBtn.MinWidth = 104;
        _addBtn.Margin = new Thickness(0, 0, 8, 0);
        _addBtn.Click += (_, _) => AddQueue();

        _startBtn = Ui.DialogButton("เริ่มทำงาน", accent: true);
        _startBtn.MinWidth = 116;
        _startBtn.Click += async (_, _) => await StartQueueAsync();

        _queueList = new System.Windows.Controls.ListBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = Ui.Brush(0xE5E5E5),
            ItemTemplate = QueueTemplate(),
            ItemContainerStyle = QueueItemStyle(),
            Padding = new Thickness(14, 12, 14, 12),
            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch,
            AlternationCount = 1000,
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(_queueList, ScrollBarVisibility.Disabled);
        _queueList.SelectionChanged += (_, _) => RenderSelected();

        _flowPanel = new StackPanel { Margin = new Thickness(34, 26, 28, 24) };
        _flowTitle = Text("เลือกรายการในคิวเพื่อดูความคืบหน้า", 0x707070, 12);
        _statusBar = Text("ยังไม่มีคิว", 0x8A8A8A, 12);
        _statusBar.TextTrimming = TextTrimming.CharacterEllipsis;

        Content = BuildLayout();
        RenderFlow(null);
        UpdateButtons();
    }

    private UIElement BuildLayout()
    {
        var grid = new Grid { Background = Ui.Brush(0x0A0A0A) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(540) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        var left = LeftColumn();
        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var right = RightColumn();
        Grid.SetColumn(right, 1);
        grid.Children.Add(right);
        return grid;
    }

    private UIElement LeftColumn()
    {
        var dock = new DockPanel { Background = Ui.Brush(0x111111), LastChildFill = true };

        var title = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        title.Children.Add(Text("TEMO.GEN", 0xFFFFFF, 21, FontWeights.Bold));
        title.Children.Add(Text("ตัวสร้างเว็บอัตโนมัติ • ระบบคิว", 0x606060, 11, margin: new Thickness(0, 3, 0, 0)));
        var titleBar = Bar(0x0D0D0D, new Thickness(20, 0, 20, 0), title);
        titleBar.Height = HeaderHeight;
        DockTop(dock, titleBar);

        var buttons = new Grid();
        buttons.ColumnDefinitions.Add(new ColumnDefinition());
        buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        buttons.ColumnDefinitions.Add(new ColumnDefinition());
        _addBtn.MinWidth = 0;
        _addBtn.Margin = new Thickness(0);
        _startBtn.MinWidth = 0;
        Grid.SetColumn(_startBtn, 2);
        buttons.Children.Add(_addBtn);
        buttons.Children.Add(_startBtn);
        DockTop(dock, Bar(0x0E0E0E, new Thickness(18, 12, 18, 12), buttons));

        DockTop(dock, SectionBar("คิวงาน", null));
        dock.Children.Add(_queueList);
        return dock;
    }

    private UIElement RightColumn()
    {
        var dock = new DockPanel { Background = Ui.Brush(0x0A0A0A), LastChildFill = true };
        DockTop(dock, SectionBar("ขั้นตอนการทำงาน", _flowTitle, HeaderHeight));

        var footer = new Border
        {
            Background = Ui.Brush(0x0D0D0D),
            BorderBrush = Ui.Brush(0x1E1E1E),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(22, 12, 22, 12),
            Child = _statusBar,
        };
        DockPanel.SetDock(footer, Dock.Bottom);
        dock.Children.Add(footer);

        dock.Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _flowPanel,
        });
        return dock;
    }

    private static void DockTop(DockPanel dock, UIElement element)
    {
        DockPanel.SetDock(element, Dock.Top);
        dock.Children.Add(element);
    }

    private static Border SectionBar(string label, UIElement? sub, double height = 0)
    {
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(Text(label, 0x8A8A8A, 12, FontWeights.SemiBold));
        if (sub is not null) stack.Children.Add(sub);
        var vpad = height > 0 ? 0 : 14;
        var bar = Bar(0x0D0D0D, new Thickness(22, vpad, 22, vpad), stack);
        if (height > 0) bar.Height = height;
        return bar;
    }

    private static Border Bar(uint bg, Thickness pad, UIElement child) => new()
    {
        Background = Ui.Brush(bg),
        BorderBrush = Ui.Brush(0x1E1E1E),
        BorderThickness = new Thickness(0, 0, 0, 1),
        Padding = pad,
        Child = child,
    };

    private static TextBlock Text(string text, uint color, double size, FontWeight? weight = null, Thickness margin = default) => new()
    {
        Text = text,
        Foreground = Ui.Brush(color),
        FontSize = size,
        FontWeight = weight ?? FontWeights.Normal,
        Margin = margin,
        TextWrapping = TextWrapping.Wrap,
    };

    private DataTemplate QueueTemplate()
    {
        var grid = new FrameworkElementFactory(typeof(Grid));
        grid.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 7, 0, 7));
        var c0 = new FrameworkElementFactory(typeof(ColumnDefinition));
        c0.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
        var c1 = new FrameworkElementFactory(typeof(ColumnDefinition));
        var c2 = new FrameworkElementFactory(typeof(ColumnDefinition));
        c2.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
        var c3 = new FrameworkElementFactory(typeof(ColumnDefinition));
        c3.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
        grid.AppendChild(c0);
        grid.AppendChild(c1);
        grid.AppendChild(c2);
        grid.AppendChild(c3);

        var number = new FrameworkElementFactory(typeof(TextBlock));
        number.SetValue(Grid.ColumnProperty, 0);
        number.SetBinding(TextBlock.TextProperty, new Binding("(ItemsControl.AlternationIndex)")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor) { AncestorType = typeof(ListBoxItem) },
            Converter = IndexConverter,
        });
        number.SetValue(TextBlock.ForegroundProperty, Ui.Brush(0x666666));
        number.SetValue(TextBlock.FontSizeProperty, 13d);
        number.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0));
        number.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        grid.AppendChild(number);

        const uint DetailGray = 0x7A7A7A;

        var mid = new FrameworkElementFactory(typeof(StackPanel));
        mid.SetValue(Grid.ColumnProperty, 1);
        mid.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        mid.SetValue(StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);

        var brand = Bound(nameof(GenQueueItem.Brand), 0xE5E5E5, 14, FontWeights.Normal, default);
        brand.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        brand.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        mid.AppendChild(brand);

        var sep = new FrameworkElementFactory(typeof(TextBlock));
        sep.SetValue(TextBlock.TextProperty, "|");
        sep.SetValue(TextBlock.ForegroundProperty, Ui.Brush(0x404040));
        sep.SetValue(TextBlock.FontSizeProperty, 13d);
        sep.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        sep.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        mid.AppendChild(sep);

        var timeLabel = new FrameworkElementFactory(typeof(TextBlock));
        timeLabel.SetValue(TextBlock.TextProperty, "ใช้เวลา ");
        timeLabel.SetValue(TextBlock.ForegroundProperty, Ui.Brush(DetailGray));
        timeLabel.SetValue(TextBlock.FontSizeProperty, 12d);
        timeLabel.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        mid.AppendChild(timeLabel);

        var timeVal = new FrameworkElementFactory(typeof(TextBlock));
        timeVal.SetBinding(TextBlock.TextProperty, new Binding(nameof(GenQueueItem.DurationText)));
        timeVal.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(GenQueueItem.HasDetail))
        {
            Converter = DetailBrush,
            ConverterParameter = "F5A623",
        });
        timeVal.SetValue(TextBlock.FontSizeProperty, 12d);
        timeVal.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        timeVal.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        mid.AppendChild(timeVal);

        var costLabel = new FrameworkElementFactory(typeof(TextBlock));
        costLabel.SetValue(TextBlock.TextProperty, "  •  ต้นทุน ");
        costLabel.SetValue(TextBlock.ForegroundProperty, Ui.Brush(DetailGray));
        costLabel.SetValue(TextBlock.FontSizeProperty, 12d);
        costLabel.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        mid.AppendChild(costLabel);

        var costVal = new FrameworkElementFactory(typeof(TextBlock));
        costVal.SetBinding(TextBlock.TextProperty, new Binding(nameof(GenQueueItem.CostText)));
        costVal.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(GenQueueItem.HasDetail))
        {
            Converter = DetailBrush,
            ConverterParameter = "4FD784",
        });
        costVal.SetValue(TextBlock.FontSizeProperty, 12d);
        costVal.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        costVal.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        mid.AppendChild(costVal);

        grid.AppendChild(mid);

        var status = new FrameworkElementFactory(typeof(TextBlock));
        status.SetValue(Grid.ColumnProperty, 2);
        status.SetBinding(TextBlock.TextProperty, new Binding(nameof(GenQueueItem.Status)));
        status.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(GenQueueItem.Status)) { Converter = StatusConverter });
        status.SetValue(TextBlock.FontSizeProperty, 12d);
        status.SetValue(FrameworkElement.MarginProperty, new Thickness(10, 0, 0, 0));
        status.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        grid.AppendChild(status);

        var delete = new FrameworkElementFactory(typeof(Border));
        delete.SetValue(Grid.ColumnProperty, 3);
        delete.SetValue(Border.BackgroundProperty, Ui.Brush(0xC92424));
        delete.SetValue(Border.BorderBrushProperty, Ui.Brush(0xA81E1E));
        delete.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        delete.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        delete.SetValue(Border.PaddingProperty, new Thickness(7, 2, 7, 2));
        delete.SetValue(FrameworkElement.MarginProperty, new Thickness(10, 0, 0, 0));
        delete.SetValue(FrameworkElement.CursorProperty, Cursors.Hand);
        delete.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        delete.SetValue(FrameworkElement.ToolTipProperty, "ลบออกจากคิว");
        delete.SetBinding(UIElement.VisibilityProperty,
            new Binding(nameof(GenQueueItem.Status)) { Converter = PendingVisibility });
        delete.AddHandler(UIElement.MouseLeftButtonUpEvent, new System.Windows.Input.MouseButtonEventHandler(OnDeleteQueueItem));

        var deleteText = new FrameworkElementFactory(typeof(TextBlock));
        deleteText.SetValue(TextBlock.TextProperty, "ลบ");
        deleteText.SetValue(TextBlock.ForegroundProperty, Ui.Brush(0xFFFFFF));
        deleteText.SetValue(TextBlock.FontSizeProperty, 11d);
        deleteText.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        deleteText.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
        deleteText.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        delete.AppendChild(deleteText);
        grid.AppendChild(delete);

        return new DataTemplate { VisualTree = grid };
    }

    private void OnDeleteQueueItem(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: GenQueueItem item })
        {
            e.Handled = true;
            RemoveQueueItem(item);
        }
    }

    private void RemoveQueueItem(GenQueueItem item)
    {
        if (item.Status != "รอคิว") return;
        _queue.Remove(item);
        _queueList.Items.Remove(item);
        UpdateButtons();
        RenderSelected();
    }

    private static Style QueueItemStyle()
    {
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        var template = new ControlTemplate(typeof(ListBoxItem)) { VisualTree = presenter };

        var style = new Style(typeof(ListBoxItem));
        style.Setters.Add(new Setter(System.Windows.Controls.Control.TemplateProperty, template));
        style.Setters.Add(new Setter(System.Windows.Controls.Control.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(System.Windows.Controls.Control.PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(UIElement.OpacityProperty, 0.6));

        var selected = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        selected.Setters.Add(new Setter(UIElement.OpacityProperty, 1.0));
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(UIElement.OpacityProperty, 1.0));
        style.Triggers.Add(selected);
        style.Triggers.Add(hover);

        return style;
    }

    private static FrameworkElementFactory Bound(string path, uint color, double size, FontWeight weight, Thickness margin)
    {
        var block = new FrameworkElementFactory(typeof(TextBlock));
        block.SetBinding(TextBlock.TextProperty, new Binding(path));
        block.SetValue(TextBlock.ForegroundProperty, Ui.Brush(color));
        block.SetValue(TextBlock.FontSizeProperty, size);
        block.SetValue(TextBlock.FontWeightProperty, weight);
        block.SetValue(FrameworkElement.MarginProperty, margin);
        block.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        return block;
    }

    private void AddQueue()
    {
        var dialog = new GenDialog { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Options is not { } options) return;

        var item = new GenQueueItem(options);
        _queue.Add(item);
        _queueList.Items.Add(item);
        _queueList.SelectedItem = item;
        UpdateButtons();
    }

    private async Task StartQueueAsync()
    {
        if (_running || _queue.Count == 0) return;

        var apiKey = SettingsStore.LoadApiKey() ?? "";
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _statusBar.Text = "กรุณาตั้งค่า OpenAI API Key ใน TEMO.AI ก่อน";
            return;
        }

        _running = true;
        UpdateButtons();
        try
        {
            while (_queue.FirstOrDefault(x => x.Status == "รอคิว") is { } item)
            {
                _queueList.SelectedItem = item;
                item.Status = "กำลังทำงาน";
                item.StageIndex = 0;
                item.Duration = null;
                item.CostThb = null;
                Refresh();

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = await SiteGenerator.GenerateAsync(item.Options, apiKey, message =>
                    Dispatcher.Invoke(() =>
                    {
                        item.Message = message;
                        item.StageIndex = StageFromMessage(message);
                        Refresh();
                    }));
                sw.Stop();

                item.ProjectPath = result.ProjectPath;
                item.Duration = sw.Elapsed;
                item.CostThb = result.CostThb;

                if (!result.Ok && OpenAiClient.IsBillingLimitReached(result.Message))
                {
                    item.Status = "ผิดพลาด";
                    item.Message = AiModels.BillingLimitMessage;
                    Refresh();
                    _statusBar.Text = AiModels.BillingLimitMessage;
                    CreditDialog.Show(this, "กรุณาเติมเครดิตก่อนใช้งานต่อ — ระบบได้หยุดคิวทั้งหมดแล้ว");
                    break;
                }

                item.Status = result.Ok ? "เสร็จแล้ว" : "ผิดพลาด";
                item.Message = result.Message;
                item.StageIndex = result.Ok ? Stages.Length - 1 : item.StageIndex;
                Refresh();

                if (result.Ok && result.ProjectPath is { } path)
                    ProjectCreated?.Invoke(path);
                else if (!result.Ok)
                    ShowErrorLog(item, result.Message);
            }
        }
        finally
        {
            _running = false;
            UpdateButtons();
        }
    }

    private void ShowErrorLog(GenQueueItem item, string message)
    {
        var stage = item.StageIndex >= 0 && item.StageIndex < Stages.Length ? Stages[item.StageIndex] : "-";

        var log = new System.Windows.Controls.TextBox
        {
            Text = string.IsNullOrWhiteSpace(message) ? "ไม่ทราบสาเหตุ" : message,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Ui.Brush(0x111111),
            Foreground = Ui.Brush(0xE0E0E0),
            BorderBrush = Ui.Brush(0x2A2A2A),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
        };

        var close = Ui.DialogButton("ปิด", accent: true);
        close.MinWidth = 96;
        close.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
        close.Margin = new Thickness(0, 14, 0, 0);

        var panel = new DockPanel { Margin = new Thickness(20), LastChildFill = true };
        var header = Text($"เกิดข้อผิดพลาด — {item.Brand} (ขั้นตอน: {stage})", 0xE0533B, 15, FontWeights.SemiBold);
        DockPanel.SetDock(header, Dock.Top);
        panel.Children.Add(header);
        DockPanel.SetDock(close, Dock.Bottom);
        panel.Children.Add(close);
        panel.Children.Add(log);

        var dialog = new Window
        {
            Title = "บันทึกข้อผิดพลาด",
            Width = 620,
            Height = 460,
            Owner = this,
            Content = panel,
        };
        Ui.StyleDialog(dialog);
        close.Click += (_, _) => dialog.Close();
        dialog.ShowDialog();
    }

    private static int StageFromMessage(string message)
    {
        if (message.Contains("CSS")) return 4;
        if (message.Contains("รูป")) return 3;
        if (message.Contains("เนื้อหา")) return 2;
        if (message.Contains("Component") || message.Contains("ธีมสี") || message.Contains("Layout")) return 1;
        return 0;
    }

    private void RenderSelected()
    {
        var item = _queueList.SelectedItem as GenQueueItem;
        RenderFlow(item);
        _flowTitle.Text = item?.Summary ?? "เลือกรายการในคิวเพื่อดูความคืบหน้า";
        _statusBar.Text = item is null
            ? "ยังไม่มีคิว"
            : $"สถานะ: {item.Status}" + (string.IsNullOrWhiteSpace(item.Message) ? "" : $"  —  {item.Message}");
    }

    private void RenderFlow(GenQueueItem? item)
    {
        _flowPanel.Children.Clear();
        var active = item?.StageIndex ?? -1;
        var finished = item?.Status == "เสร็จแล้ว";
        var failed = item?.Status == "ผิดพลาด";

        for (var i = 0; i < Stages.Length; i++)
        {
            var done = finished || i < active;
            var current = !done && i == active && item?.Status == "กำลังทำงาน";
            var error = failed && i == active;
            _flowPanel.Children.Add(MakeStageRow(i, done, current, error, item));
            if (i < Stages.Length - 1)
                _flowPanel.Children.Add(MakeConnector(done));
        }
    }

    private static UIElement MakeStageRow(int index, bool done, bool current, bool error, GenQueueItem? item)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NodeSize) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        var node = MakeNode(index, done, current, error);
        node.VerticalAlignment = VerticalAlignment.Center;
        grid.Children.Add(node);

        var color = error ? ErrorColor : done ? DoneColor : current ? CurrentColor : PendingColor;
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(stack, 2);
        stack.Children.Add(Text(Stages[index], color, 16, done || current ? FontWeights.SemiBold : FontWeights.Normal));

        if (SubText(index, current, item) is { } sub)
            stack.Children.Add(Text(sub, 0x8A8A8A, 12, margin: new Thickness(0, 3, 0, 0)));

        grid.Children.Add(stack);
        return grid;
    }

    private static string? SubText(int index, bool current, GenQueueItem? item)
    {
        if (!current || item?.Message is not { Length: > 0 } message) return null;
        if (index == 3 && message.Contains("รูป")) return message.Replace("กำลัง", "").Trim();
        if (index == 3) return "กำลังเตรียมรูปภาพ…";
        return message.Length > 60 ? message[..60] + "…" : message;
    }

    private static FrameworkElement MakeNode(int index, bool done, bool current, bool error)
    {
        var grid = new Grid { Width = NodeSize, Height = NodeSize };

        if (error)
        {
            grid.Children.Add(new System.Windows.Shapes.Ellipse { Fill = Ui.Brush(ErrorColor) });
            grid.Children.Add(Glyph("M11 11 L19 19 M19 11 L11 19", 0x0A0A0A, 2.4));
            return grid;
        }

        if (done)
        {
            grid.Children.Add(new System.Windows.Shapes.Ellipse { Fill = Ui.Brush(DoneColor) });
            grid.Children.Add(Glyph("M9 15.5 L13 19.5 L21 10.5", 0x0A0A0A, 2.4));
            return grid;
        }

        if (current)
        {
            grid.Children.Add(new System.Windows.Shapes.Ellipse
            {
                Fill = Ui.Brush(0x1A1407),
                Stroke = Ui.Brush(0x3A2C12),
                StrokeThickness = 3,
            });
            grid.Children.Add(MakeSpinner());
            return grid;
        }

        grid.Children.Add(new System.Windows.Shapes.Ellipse
        {
            Fill = Ui.Brush(0x161616),
            Stroke = Ui.Brush(0x303030),
            StrokeThickness = 1.5,
        });
        grid.Children.Add(new TextBlock
        {
            Text = (index + 1).ToString(),
            Foreground = Ui.Brush(PendingColor),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        return grid;
    }

    private static System.Windows.Shapes.Path Glyph(string data, uint color, double thickness) => new()
    {
        Stroke = Ui.Brush(color),
        StrokeThickness = thickness,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round,
        Data = Geometry.Parse(data),
    };

    private static UIElement MakeSpinner()
    {
        var arc = Glyph("M15 4 A 11 11 0 1 1 4 15", CurrentColor, 3);
        var rotate = new RotateTransform(0, NodeSize / 2, NodeSize / 2);
        arc.RenderTransform = rotate;
        rotate.BeginAnimation(RotateTransform.AngleProperty,
            new System.Windows.Media.Animation.DoubleAnimation(0, 360, TimeSpan.FromSeconds(0.85))
            {
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever,
            });
        return arc;
    }

    private static UIElement MakeConnector(bool done) => new System.Windows.Shapes.Rectangle
    {
        Width = 2,
        Height = 24,
        Fill = Ui.Brush(done ? DoneColor : 0x2A2A2A),
        HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
        Margin = new Thickness(NodeSize / 2 - 1, 4, 0, 4),
        RadiusX = 1,
        RadiusY = 1,
    };

    private void Refresh()
    {
        _queueList.Items.Refresh();
        RenderSelected();
    }

    private void UpdateButtons()
    {
        _addBtn.IsEnabled = true;
        _startBtn.IsEnabled = !_running && _queue.Count > 0;
        _startBtn.Content = _running ? "กำลังทำงาน…" : "เริ่มทำงาน";
    }
}
