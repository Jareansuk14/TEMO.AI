namespace TEMO.AI;

internal sealed partial class VercelDeployDialog
{
    private const double ActionBtnHeight = 38;
    private const double ActionBtnFontSize = 14;

    private Grid BuildContent()
    {
        var outer = new Grid();
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _logColumn = new ColumnDefinition { Width = new GridLength(0) };
        outer.ColumnDefinitions.Add(_logColumn);

        var main = BuildMainPanel();
        Grid.SetColumn(main, 0);
        outer.Children.Add(main);

        var log = BuildLogPanel();
        Grid.SetColumn(log, 1);
        outer.Children.Add(log);

        return outer;
    }

    private DockPanel BuildMainPanel()
    {
        var root = new DockPanel();

        var header = BuildHeader();
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        var footer = BuildFooter();
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);

        root.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = BuildBody(),
        });

        return root;
    }

    private Border BuildHeader()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new TextBlock
        {
            Text = "Deploy to Vercel",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0xEDEDED),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(title, 0);
        grid.Children.Add(title);

        _logToggleBtn = MakeSecondaryActionButton("LOG");
        _logToggleBtn.Padding = new Thickness(16, 0, 16, 0);
        _logToggleBtn.VerticalAlignment = VerticalAlignment.Center;
        _logToggleBtn.Click += (_, _) => ToggleLog();
        Grid.SetColumn(_logToggleBtn, 1);
        grid.Children.Add(_logToggleBtn);

        return Ui.ChromeHeader(grid, new Thickness(24, 18, 20, 18));
    }

    private StackPanel BuildBody()
    {
        var body = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };

        _accountNameText = new TextBlock
        {
            Text = "ชื่อบัญชี: กำลังดึงบัญชี...",
            FontSize = 14,
            Foreground = Ui.Brush(0x8A8A8A),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        _loginBtn = MakeAccentActionButton("+ เพิ่มบัญชี");
        _loginBtn.Margin = new Thickness(16, 0, 0, 0);
        _loginBtn.Click += (_, _) => { if (_loginInProgress) CancelLogin(); else StartLogin(); };

        _logoutBtn = MakeDangerActionButton("ลบบัญชี");
        _logoutBtn.Padding = new Thickness(18, 0, 18, 0);
        _logoutBtn.Margin = new Thickness(8, 0, 0, 0);
        _logoutBtn.IsEnabled = false;
        _logoutBtn.Click += async (_, _) => await LogoutAsync();

        var accountGrid = new Grid { Margin = new Thickness(0, 0, 0, 20) };
        accountGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        accountGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        accountGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(_accountNameText, 0);
        Grid.SetColumn(_loginBtn, 1);
        Grid.SetColumn(_logoutBtn, 2);
        accountGrid.Children.Add(_accountNameText);
        accountGrid.Children.Add(_loginBtn);
        accountGrid.Children.Add(_logoutBtn);
        body.Children.Add(accountGrid);

        _createProjectBtn = MakeAccentActionButton("+ สร้างโปรเจคใหม่");
        _createProjectBtn.IsEnabled = false;
        _createProjectBtn.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
        _createProjectBtn.Margin = new Thickness(0, 0, 0, 10);
        _createProjectBtn.Click += (_, _) => AddPendingProject();
        body.Children.Add(_createProjectBtn);

        _projectTable = VercelProjectGrid.Create(OpenDomainDialog);
        _projectPanel = new LoadingPanel { MinHeight = 380 };
        body.Children.Add(new Border
        {
            Background = Ui.Brush(0x161616),
            BorderBrush = Ui.Brush(0x282828),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true,
            Child = _projectPanel,
        });

        return body;
    }

    private Button MakeAccentActionButton(string content) => StyleActionButton(Ui.DialogButton(content, accent: true));

    private Button MakeSecondaryActionButton(string content) => StyleActionButton(Ui.DialogButton(content, accent: false));

    private Button MakeDangerActionButton(string content) => StyleActionButton(Ui.DialogDangerButton(content));

    private static Button StyleActionButton(Button btn)
    {
        btn.Height = ActionBtnHeight;
        btn.FontSize = ActionBtnFontSize;
        return btn;
    }

    private Border BuildFooter()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _statusText = new TextBlock
        {
            FontSize = 13,
            Foreground = Ui.Brush(0x8A8A8A),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 16, 0),
        };
        Grid.SetColumn(_statusText, 0);

        var closeBtn = MakeSecondaryActionButton("ปิด");
        closeBtn.Padding = new Thickness(22, 0, 22, 0);
        closeBtn.Click += (_, _) => Close();
        Grid.SetColumn(closeBtn, 1);

        _deployBtn = MakeAccentActionButton("Deploy");
        _deployBtn.Padding = new Thickness(30, 0, 30, 0);
        _deployBtn.Click += async (_, _) => await RunDeployAsync();
        Grid.SetColumn(_deployBtn, 3);

        grid.Children.Add(_statusText);
        grid.Children.Add(closeBtn);
        grid.Children.Add(_deployBtn);

        return Ui.ChromeFooter(grid, new Thickness(24, 16, 24, 16));
    }

    private Border BuildLogPanel()
    {
        var dock = new DockPanel();

        var logHeader = Ui.ChromeHeader(new TextBlock
        {
            Text = "LOG",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0x888888),
        }, new Thickness(12, 8, 12, 8));
        DockPanel.SetDock(logHeader, Dock.Top);
        dock.Children.Add(logHeader);

        _logBox = new TextBox
        {
            Background = Ui.Brush(0x080808),
            Foreground = Ui.Brush(0xC8C8C8),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(12, 10, 12, 10),
        };
        dock.Children.Add(_logBox);

        return new Border
        {
            Background = Ui.Brush(0x080808),
            BorderBrush = Ui.Brush(0x1E1E1E),
            BorderThickness = new Thickness(1, 0, 0, 0),
            Child = dock,
        };
    }

    private void ToggleLog()
    {
        _logVisible = !_logVisible;
        var target = _logVisible ? 380.0 : 0.0;
        _logColumn.Width = new GridLength(target);
        _logToggleBtn.Foreground = _logVisible ? Brushes.White : Ui.Brush(0xB0B0B0);
    }
}
