namespace TEMO.AI;

internal sealed class VercelDomainDialog : Window
{
    private readonly VercelProjectOption _project;
    private readonly VercelScopeOption _scope;
    private readonly string _token;
    private readonly Func<Task>? _onDomainsChanged;

    private Button _addBtn = null!;
    private Button _refreshBtn = null!;
    private TextBlock _statusText = null!;
    private StackPanel _domainList = null!;
    private bool _busy;

    public VercelDomainDialog(
        VercelProjectOption project,
        VercelScopeOption scope,
        string token,
        Func<Task>? onDomainsChanged = null)
    {
        _project = project;
        _scope = scope;
        _token = token;
        _onDomainsChanged = onDomainsChanged;

        Title = $"จัดการโดเมน — {project.Name}";
        Width = 760;
        Height = 620;
        MinWidth = 760;
        MaxWidth = 760;
        ResizeMode = ResizeMode.NoResize;
        Ui.StyleDialog(this);
        Background = Ui.Brush(0x0B0B0B);

        Content = BuildContent();
        Loaded += async (_, _) => await LoadDomainsAsync();
    }

    private Grid BuildContent()
    {
        var root = new DockPanel();
        var header = BuildHeader();
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        root.Children.Add(BuildFooter());
        root.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = BuildBody(),
        });

        var outer = new Grid();
        outer.Children.Add(root);
        return outer;
    }

    private Border BuildHeader() => new()
    {
        Background = Ui.Brush(0x0D0D0D),
        BorderBrush = Ui.Brush(0x1E1E1E),
        BorderThickness = new Thickness(0, 0, 0, 1),
        Padding = new Thickness(24, 18, 24, 18),
        Child = new TextBlock
        {
            Text = $"จัดการโดเมน — {_project.Name}",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0xEDEDED),
        },
    };

    private StackPanel BuildBody()
    {
        var body = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };

        var topRow = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        topRow.Children.Add(new TextBlock
        {
            Text = "โดเมนทั้งหมดของโปรเจคนี้",
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ui.Brush(0xEDEDED),
            VerticalAlignment = VerticalAlignment.Center,
        });

        _refreshBtn = Ui.DialogButton("Refresh", accent: false);
        _refreshBtn.Height = 38;
        _refreshBtn.FontSize = 13;
        _refreshBtn.Click += async (_, _) => await LoadDomainsAsync();
        Grid.SetColumn(_refreshBtn, 1);
        topRow.Children.Add(_refreshBtn);

        _addBtn = Ui.DialogButton("+ เพิ่มโดเมน", accent: true);
        _addBtn.Height = 38;
        _addBtn.FontSize = 13;
        _addBtn.Click += async (_, _) => await AddDomainFlowAsync();
        Grid.SetColumn(_addBtn, 3);
        topRow.Children.Add(_addBtn);

        body.Children.Add(topRow);

        body.Children.Add(new Border
        {
            Background = Ui.Brush(0x121212),
            BorderBrush = Ui.Brush(0x282828),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 10, 16, 10),
            Child = BuildListHost(),
        });

        return body;
    }

    private StackPanel BuildListHost()
    {
        _domainList = new StackPanel();
        _domainList.Children.Add(MakeHint("กำลังโหลดโดเมน..."));
        return _domainList;
    }

    private Border BuildFooter()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
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
        grid.Children.Add(_statusText);

        var closeBtn = Ui.DialogButton("ปิด", accent: false);
        closeBtn.Height = 38;
        closeBtn.FontSize = 14;
        closeBtn.Padding = new Thickness(24, 0, 24, 0);
        closeBtn.Click += (_, _) => Close();
        Grid.SetColumn(closeBtn, 1);
        grid.Children.Add(closeBtn);

        var border = new Border
        {
            Background = Ui.Brush(0x0D0D0D),
            BorderBrush = Ui.Brush(0x1E1E1E),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(24, 14, 24, 14),
            Child = grid,
        };
        DockPanel.SetDock(border, Dock.Bottom);
        return border;
    }

    private async Task LoadDomainsAsync()
    {
        if (_busy) return;
        SetBusy(true);
        SetStatus("กำลังโหลดโดเมน...");
        _domainList.Children.Clear();
        _domainList.Children.Add(MakeHint("กำลังโหลดโดเมน..."));

        try
        {
            var domains = await VercelApiClient.TryGetProjectDomainsAsync(
                _token, _project.Id, _project.Url, _scope.TeamId);

            _domainList.Children.Clear();
            if (domains is null)
            {
                _domainList.Children.Add(MakeHint("โหลดโดเมนไม่สำเร็จ"));
                SetStatus("โหลดโดเมนไม่สำเร็จ");
                return;
            }

            if (domains.Count == 0)
            {
                _domainList.Children.Add(MakeHint("ยังไม่มีโดเมนในโปรเจคนี้"));
                SetStatus("ยังไม่มีโดเมน");
                return;
            }

            foreach (var domain in domains)
                _domainList.Children.Add(BuildDomainRow(domain, domains.Count));

            SetStatus($"พบโดเมน {domains.Count} รายการ");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private Border BuildDomainRow(VercelProjectDomainOption domain, int domainCount)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });

        var nameText = new TextBlock
        {
            Text = domain.Name,
            FontSize = 14,
            Foreground = Ui.Brush(0xEDEDED),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(nameText, 0);
        grid.Children.Add(nameText);

        var statusText = new TextBlock
        {
            Text = $"สถานะ: {domain.ConfigurationStatus}",
            FontSize = 13,
            Foreground = domain.IsValidConfiguration ? Ui.Brush(0x4CAF50) : Ui.Brush(0xE0A64B),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(statusText, 1);
        grid.Children.Add(statusText);

        if (domain.CanShowDns)
        {
            var dnsBtn = Ui.DialogButton("ดู DNS", accent: false);
            dnsBtn.Height = 32;
            dnsBtn.FontSize = 12;
            dnsBtn.Padding = new Thickness(12, 0, 12, 0);
            dnsBtn.Click += async (_, _) => await ShowDnsAsync(domain.Name);
            Grid.SetColumn(dnsBtn, 2);
            grid.Children.Add(dnsBtn);
        }

        var deleteBtn = Ui.DialogDangerButton("ลบ");
        deleteBtn.Height = 32;
        deleteBtn.FontSize = 12;
        deleteBtn.Padding = new Thickness(14, 0, 14, 0);
        deleteBtn.IsEnabled = CanDeleteDomain(domain, domainCount);
        deleteBtn.ToolTip = deleteBtn.IsEnabled
            ? "ลบโดเมนนี้ออกจากโปรเจค"
            : "ไม่สามารถลบได้เมื่อเหลือโดเมนเดียว หรือเป็นโดเมนเริ่มต้นของ Vercel";
        deleteBtn.Click += async (_, _) => await DeleteDomainAsync(domain.Name, domainCount);
        Grid.SetColumn(deleteBtn, 4);
        grid.Children.Add(deleteBtn);

        return new Border
        {
            Background = Ui.Brush(0x161616),
            BorderBrush = Ui.Brush(0x252525),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 8),
            Child = grid,
        };
    }

    private static bool CanDeleteDomain(VercelProjectDomainOption domain, int domainCount) =>
        domainCount > 1 && !domain.Name.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase);

    private async Task DeleteDomainAsync(string domain, int domainCount)
    {
        if (_busy) return;
        if (domainCount <= 1)
        {
            SetStatus("ลบไม่ได้ — ต้องมีโดเมนอย่างน้อย 1 รายการ");
            return;
        }

        if (domain.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
        {
            SetStatus("ลบโดเมนเริ่มต้นของ Vercel ไม่ได้");
            return;
        }

        if (System.Windows.MessageBox.Show(
                this,
                $"ต้องการลบโดเมน {domain} ออกจากโปรเจคนี้ใช่ไหม?",
                "ลบโดเมน",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        SetBusy(true);
        SetStatus($"กำลังลบ {domain}...");
        try
        {
            var result = await VercelApiClient.DeleteProjectDomainAsync(_token, _project.Id, domain, _scope.TeamId);
            if (!result.Success)
            {
                SetStatus($"ลบไม่สำเร็จ: {result.Message}");
                if (result.Message.Contains("not assigned", StringComparison.OrdinalIgnoreCase))
                {
                    SetBusy(false);
                    await RefreshAllAsync();
                }
                return;
            }

            SetStatus($"ลบ {domain} แล้ว");
            SetBusy(false);
            await RefreshAllAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task AddDomainFlowAsync()
    {
        if (_busy) return;

        var prompt = new PromptDialog(
            "เพิ่มโดเมน",
            "ชื่อโดเมนหลัก",
            validate: value => IsValidDomain(NormalizeApex(value)),
            invalidMessage: "ใส่โดเมนหลัก เช่น temo888.fun หรือ www.temo888.fun")
        { Owner = this };

        if (prompt.ShowDialog() != true) return;

        var apex = NormalizeApex(prompt.Value);
        var www = $"www.{apex}";
        SetBusy(true);
        SetStatus("กำลังเพิ่มโดเมน...");

        try
        {
            var apexAdd = await VercelApiClient.AddProjectDomainAsync(_token, _project.Id, apex, _scope.TeamId);
            var wwwAdd = await VercelApiClient.AddProjectDomainAsync(_token, _project.Id, www, _scope.TeamId);

            var configs = await GetDnsConfigsAsync(apex, www);
            new VercelDomainDnsDialog(_project.Name, configs, [apexAdd, wwwAdd]) { Owner = this }.ShowDialog();

            SetBusy(false);
            await RefreshAllAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ShowDnsAsync(string domain)
    {
        SetStatus($"กำลังดึง DNS ของ {domain}...");
        var cfg = await VercelApiClient.GetDomainConfigAsync(_token, domain, IsApexDomain(domain), _scope.TeamId);
        new VercelDomainDnsDialog(_project.Name, [cfg], []) { Owner = this }.ShowDialog();
        SetStatus("ดูค่า DNS แล้ว");
    }

    private async Task<List<VercelDomainDnsConfig>> GetDnsConfigsAsync(string apex, string www) =>
    [
        await VercelApiClient.GetDomainConfigAsync(_token, apex, isApex: true, _scope.TeamId),
        await VercelApiClient.GetDomainConfigAsync(_token, www, isApex: false, _scope.TeamId),
    ];

    private async Task RefreshAllAsync()
    {
        await LoadDomainsAsync();
        if (_onDomainsChanged is not null)
            await _onDomainsChanged();
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _addBtn.IsEnabled = !busy;
        _refreshBtn.IsEnabled = !busy;
    }

    private void SetStatus(string message) => _statusText.Text = message;

    private static TextBlock MakeHint(string text) => new()
    {
        Text = text,
        FontSize = 12,
        Foreground = Ui.Brush(0x777777),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(2, 2, 0, 2),
    };

    private static string NormalizeApex(string input)
    {
        var d = (input ?? "").Trim().ToLowerInvariant();
        d = d.Replace("https://", "").Replace("http://", "");
        var slash = d.IndexOf('/');
        if (slash >= 0) d = d[..slash];
        if (d.StartsWith("www.")) d = d[4..];
        return d.Trim('.');
    }

    private static bool IsApexDomain(string domain) =>
        !domain.StartsWith("www.", StringComparison.OrdinalIgnoreCase);

    private static bool IsValidDomain(string apex) =>
        apex.Length >= 3
        && apex.Contains('.')
        && !apex.Contains(' ')
        && !apex.StartsWith('.')
        && !apex.EndsWith('.')
        && apex.All(c => char.IsLetterOrDigit(c) || c is '.' or '-');
}

internal sealed class VercelDomainDnsDialog : Window
{
    private readonly string _projectName;
    private readonly List<VercelDomainDnsConfig> _configs;
    private readonly List<VercelDomainAddResult> _results;
    private TextBlock _statusText = null!;

    public VercelDomainDnsDialog(
        string projectName,
        IEnumerable<VercelDomainDnsConfig> configs,
        IEnumerable<VercelDomainAddResult> results)
    {
        _projectName = projectName;
        _configs = [.. configs];
        _results = [.. results];

        Title = $"DNS — {projectName}";
        Width = 680;
        Height = 480;
        MinWidth = 680;
        MaxWidth = 680;
        ResizeMode = ResizeMode.NoResize;
        Ui.StyleDialog(this);
        Background = Ui.Brush(0x0B0B0B);
        Content = BuildContent();
    }

    private DockPanel BuildContent()
    {
        var root = new DockPanel();
        var header = BuildHeader();
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        root.Children.Add(BuildFooter());
        root.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = BuildBody(),
        });
        return root;
    }

    private Border BuildHeader() => new()
    {
        Background = Ui.Brush(0x0D0D0D),
        BorderBrush = Ui.Brush(0x1E1E1E),
        BorderThickness = new Thickness(0, 0, 0, 1),
        Padding = new Thickness(24, 18, 24, 18),
        Child = new TextBlock
        {
            Text = $"DNS สำหรับ Cloudflare",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0xEDEDED),
        },
    };

    private StackPanel BuildBody()
    {
        var body = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };
        body.Children.Add(new TextBlock
        {
            Text = "นำค่า DNS ด้านล่างไปตั้งใน Cloudflare",
            FontSize = 12,
            Foreground = Ui.Brush(0x999999),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16),
        });

        foreach (var cfg in _configs)
        {
            var result = _results.FirstOrDefault(r => r.Domain.Equals(cfg.Domain, StringComparison.OrdinalIgnoreCase));
            body.Children.Add(BuildDnsCard(cfg, result));
        }

        return body;
    }

    private Border BuildDnsCard(VercelDomainDnsConfig cfg, VercelDomainAddResult? result)
    {
        var stack = new StackPanel();
        var title = new TextBlock
        {
            Text = result is null ? cfg.Domain : $"{cfg.Domain}  •  {result.Message}",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = result is { Success: false } ? Ui.Brush(0xCC6666) : Ui.Brush(0xEDEDED),
            Margin = new Thickness(0, 0, 0, 10),
        };
        stack.Children.Add(title);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        AddField(grid, 0, "Type", cfg.RecordType, cfg.RecordType.Equals("A", StringComparison.OrdinalIgnoreCase)
            ? Ui.Brush(0xE57373)
            : Ui.Brush(0xF2B56B));
        AddField(grid, 1, "Name", cfg.RecordName, cfg.RecordName == "@"
            ? Ui.Brush(0x73A7FF)
            : Ui.Brush(0x7EDC8A));
        AddField(grid, 2, "Value", cfg.RecordValue);

        var copyBtn = Ui.DialogButton("Copy", accent: false);
        copyBtn.Padding = new Thickness(14, 0, 14, 0);
        copyBtn.VerticalAlignment = VerticalAlignment.Bottom;
        copyBtn.Click += (_, _) =>
        {
            try { Clipboard.SetText(cfg.RecordValue); SetStatus($"คัดลอกแล้ว: {cfg.RecordValue}"); }
            catch { }
        };
        Grid.SetColumn(copyBtn, 3);
        grid.Children.Add(copyBtn);
        stack.Children.Add(grid);

        return new Border
        {
            Background = Ui.Brush(0x141414),
            BorderBrush = Ui.Brush(0x282828),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 14, 16, 14),
            Margin = new Thickness(0, 0, 0, 12),
            Child = stack,
        };
    }

    private Border BuildFooter()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _statusText = new TextBlock
        {
            FontSize = 13,
            Foreground = Ui.Brush(0x8A8A8A),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(_statusText, 0);
        grid.Children.Add(_statusText);

        var closeBtn = Ui.DialogButton("ปิด", accent: false);
        closeBtn.Height = 38;
        closeBtn.FontSize = 14;
        closeBtn.Padding = new Thickness(24, 0, 24, 0);
        closeBtn.Click += (_, _) => Close();
        Grid.SetColumn(closeBtn, 1);
        grid.Children.Add(closeBtn);

        var border = new Border
        {
            Background = Ui.Brush(0x0D0D0D),
            BorderBrush = Ui.Brush(0x1E1E1E),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(24, 14, 24, 14),
            Child = grid,
        };
        DockPanel.SetDock(border, Dock.Bottom);
        return border;
    }

    private static void AddField(Grid grid, int column, string label, string value, Brush? valueBrush = null)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = Ui.Brush(0x808080),
            Margin = new Thickness(0, 0, 0, 4),
        });
        stack.Children.Add(new TextBox
        {
            Text = value,
            IsReadOnly = true,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Foreground = valueBrush ?? Ui.Brush(0xEDEDED),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            TextWrapping = TextWrapping.NoWrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            CaretBrush = Brushes.White,
            SelectionBrush = Ui.Brush(0x505050),
        });
        Grid.SetColumn(stack, column);
        grid.Children.Add(stack);
    }

    private void SetStatus(string message) => _statusText.Text = message;
}
