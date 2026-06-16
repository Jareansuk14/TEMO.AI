namespace TEMO.AI;

internal sealed partial class VercelDeployDialog
{
    private readonly object _logLock = new();
    private readonly StringBuilder _logPending = new();
    private DispatcherTimer? _logTimer;

    private async Task LoadTeamsSafeAsync()
    {
        try { await LoadTeamsAsync(); }
        catch (Exception ex) { SetStatus($"เปิดหน้าต่างไม่สำเร็จ: {ex.Message}"); }
    }

    private async Task LoadTeamsAsync()
    {
        _loginPollCts?.Cancel();
        VercelCli.ClearProjectLink(_projectPath);
        BeginOp();
        SetStatus("กำลังดึงบัญชี...");
        _suppressProjectLoad = true;
        SetAccountDisplay("กำลังดึงบัญชี...", loggedIn: false, loading: true);
        _currentScope = null;
        _projects = [];
        _projectTable.ItemsSource = null;
        _projectPanel.ShowSpinner();

        try
        {
            var teams = await _service.LoadTeamsAsync();
            if (teams.Count == 0)
            {
                SetLoggedOutState();
                SetStatus("ยังไม่ได้ login Vercel — กด + เพิ่มบัญชี/เปลี่ยนบัญชี");
                return;
            }

            _currentScope = teams.FirstOrDefault(t => t.IsCurrent) ?? teams[0];
            SetAccountDisplay(_currentScope.Name, loggedIn: true);
            UpdateCreateProjectButton();

            _suppressProjectLoad = false;
            await LoadProjectsAsync();

            SetStatus($"บัญชี: {_currentScope.Name}");
        }
        catch (Exception ex)
        {
            SetAccountDisplay("โหลดบัญชีไม่สำเร็จ", loggedIn: false);
            _projectPanel.ShowBlank();
            SetStatus($"โหลดบัญชีไม่สำเร็จ: {ex.Message}");
        }
        finally
        {
            _suppressProjectLoad = false;
            EndOp();
        }
    }

    private async Task LoadProjectsAsync()
    {
        if (_suppressProjectLoad) return;
        if (_currentScope is not { } scope) return;

        var generation = ++_projectLoadGeneration;
        BeginOp();
        SetStatus($"กำลังโหลดโปรเจคใน {scope.Slug}...");
        _projects = [];
        _projectTable.ItemsSource = null;
        _projectPanel.ShowSpinner();

        try
        {
            var projects = await _service.LoadProjectsAsync(scope);
            if (generation != _projectLoadGeneration) return;

            _projects = projects;
            if (_projects.Count == 0)
            {
                _projectPanel.ShowBlank();
            }
            else
            {
                _projectTable.ItemsSource = _projects;
                _projectPanel.ShowContent(_projectTable);
                _projectTable.SelectedIndex = 0;
            }

            SetStatus($"พบโปรเจค {_projects.Count} รายการใน {scope.Slug}");
        }
        catch (Exception ex)
        {
            if (generation != _projectLoadGeneration) return;
            _projectPanel.ShowEmpty("โหลดโปรเจคไม่สำเร็จ");
            SetStatus($"โหลดโปรเจคไม่สำเร็จ: {ex.Message}");
        }
        finally
        {
            EndOp();
        }
    }

    private void AddPendingProject()
    {
        if (_currentScope is null)
        {
            SetStatus("กรุณา login บัญชีก่อน");
            return;
        }

        var defaultName = VercelNames.FromPath(_projectPath);
        var prompt = new PromptDialog(
            "สร้างโปรเจคใหม่",
            "ชื่อโปรเจคบน Vercel",
            defaultName,
            validate: VercelNames.IsValid,
            invalidMessage: VercelNames.ValidNameHint,
            filterInput: true,
            maxLength: VercelNames.MaxLength)
        { Owner = this };
        if (prompt.ShowDialog() != true) return;

        var name = prompt.Value;
        if (!VercelNames.IsValid(name))
        {
            SetStatus("ชื่อโปรเจคไม่ถูกต้อง");
            return;
        }

        var existing = _projects.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            _projectTable.SelectedItem = existing;
            SetStatus($"โปรเจค '{name}' มีอยู่แล้ว");
            return;
        }

        _projects = [new VercelProjectOption(name, "", null), .. _projects.Where(p => !p.IsNew)];
        _projectTable.ItemsSource = _projects;
        _projectPanel.ShowContent(_projectTable);
        _projectTable.SelectedItem = _projects[0];

        SetStatus($"พร้อม Deploy — จะสร้าง '{name}' บน Vercel ตอนกด Deploy");
    }

    private void OpenDomainDialog(VercelProjectOption project)
    {
        if (project.IsNew)
        {
            SetStatus("โปรเจคนี้ยังไม่ได้ Deploy — กรุณา Deploy ก่อนจัดการโดเมน");
            return;
        }

        if (_currentScope is not { } scope)
        {
            SetStatus("กรุณา login บัญชีก่อน");
            return;
        }

        if (VercelAuthStore.TryGetToken() is not { } token)
        {
            SetStatus("ไม่พบ token — การจัดการโดเมนต้อง login ผ่าน Vercel ก่อน");
            return;
        }

        new VercelDomainDialog(project, scope, token, LoadProjectsAsync) { Owner = this }.ShowDialog();
    }

    private async Task LogoutAsync()
    {
        if (System.Windows.MessageBox.Show(this, "ต้องการ logout บัญชี Vercel ที่ login อยู่ตอนนี้ใช่ไหม?",
                "Vercel Logout", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        BeginOp();
        try
        {
            await VercelCli.LogoutAsync(_projectPath);
            VercelCli.ClearProjectLink(_projectPath);
            SetLoggedOutState();
        }
        finally
        {
            EndOp();
        }
    }

    private async Task RunDeployAsync()
    {
        if (_currentScope is not { } scope)
        {
            SetStatus("กรุณา login บัญชีก่อน");
            return;
        }

        if (_projectTable.SelectedItem is not VercelProjectOption project)
        {
            SetStatus("กรุณาเลือกโปรเจคก่อน");
            return;
        }

        BeginOp();
        if (!_logVisible) ToggleLog();
        _logBox.Clear();
        StartLogTimer();
        SetStatus("กำลัง deploy...");

        AppendLog($"Scope:   {scope.Slug}");
        AppendLog($"Project: {project.Name}");
        AppendLog("");

        _deployCts = new CancellationTokenSource();
        var deployedName = project.Name;
        bool success = false;
        try
        {
            var exitCode = await _service.DeployAsync(
                scope.Slug, deployedName, project.IsNew, AppendLog, _deployCts.Token);

            AppendLog("");
            if (exitCode == 0)
            {
                AppendLog("Deploy completed successfully.");
                SetStatus("Deploy สำเร็จ — กำลังโหลดข้อมูลใหม่...");
                success = true;
            }
            else
            {
                AppendLog($"Deploy failed with exit code {exitCode}.");
                SetStatus($"Deploy ไม่สำเร็จ (exit code {exitCode})");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Error: {ex.Message}");
            SetStatus("Deploy ไม่สำเร็จ");
        }
        finally
        {
            StopLogTimer();
            _deployCts?.Dispose();
            _deployCts = null;
            EndOp();
        }

        if (success)
        {
            await LoadProjectsAsync();
            var refreshed = _projects.FirstOrDefault(
                p => p.Name.Equals(deployedName, StringComparison.OrdinalIgnoreCase));
            if (refreshed is not null)
            {
                _projectTable.SelectedItem = refreshed;
                SetStatus($"Deploy สำเร็จ  —  {refreshed.Domain}");
            }
        }
    }

    private void SetLoggedOutState()
    {
        _currentScope = null;
        SetAccountDisplay("ยังไม่ได้ login", loggedIn: false, loading: false);
        _projects = [];
        _projectTable.ItemsSource = null;
        _projectPanel.ShowBlank();
        UpdateCreateProjectButton();
    }

    private void SetAccountDisplay(string name, bool loggedIn, bool loading = false)
    {
        Ui.RunOnUi(this, () =>
        {
            _accountNameText.Text = $"ชื่อบัญชี: {name}";
            _accountNameText.Foreground = loggedIn
                ? Ui.Brush(0xDEDEDE)
                : Ui.Brush(0x8A8A8A);
            if (!_loginInProgress)
            {
                _loginBtn.Content = loggedIn ? "เปลี่ยนบัญชี" : "+ เพิ่มบัญชี";
                _loginBtn.Padding = loggedIn
                    ? new Thickness(18, 0, 18, 0)
                    : new Thickness(20, 0, 20, 0);
            }
            _logoutBtn.IsEnabled = loggedIn && !loading;
        });
    }

    private void StartLogin()
    {
        _loginPollCts?.Cancel();
        _loginPollCts = new CancellationTokenSource();
        _loginInProgress = true;

        VercelCli.ClearProjectLink(_projectPath);
        SetAccountDisplay("กำลังเปิดเบราว์เซอร์เพื่อ login...", loggedIn: false, loading: true);
        _loginBtn.Content = "ยกเลิก";
        _projectPanel.ShowSpinner();
        SetStatus("รอการ login — กด \"ยกเลิก\" เพื่อหยุด");

        _ = RunLoginAndReloadAsync(_loginPollCts.Token);
    }

    private void CancelLogin()
    {
        _loginPollCts?.Cancel();
        _loginInProgress = false;
        _ = LoadTeamsAsync();
    }

    private async Task RunLoginAndReloadAsync(CancellationToken ct)
    {
        try
        {
            await VercelCli.StartLoginAsync(_projectPath, OnLoginCliMessage, ct);
            if (!ct.IsCancellationRequested)
                await LoadTeamsAsync();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { SetStatus($"Login ไม่สำเร็จ: {ex.Message}"); }
        finally
        {
            _loginInProgress = false;
        }
    }

    private void OnLoginCliMessage(string line)
    {
        var code = DeviceCodeRegex().Match(line);
        if (!code.Success) return;

        Ui.RunOnUi(this, () =>
            SetAccountDisplay($"รหัส login: {code.Groups[1].Value}", loggedIn: false, loading: true));
    }

    [GeneratedRegex(@"\b([A-Z0-9]{4}-[A-Z0-9]{4})\b")]
    private static partial Regex DeviceCodeRegex();

    private void SetStatus(string message) => _statusText.Text = message;

    private void AppendLog(string line)
    {
        lock (_logLock) _logPending.AppendLine(line);
    }

    private void StartLogTimer()
    {
        lock (_logLock) _logPending.Clear();
        _logTimer ??= new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(60),
        };
        _logTimer.Tick -= OnLogTick;
        _logTimer.Tick += OnLogTick;
        _logTimer.Start();
    }

    private void StopLogTimer()
    {
        _logTimer?.Stop();
        FlushLog();
    }

    private void OnLogTick(object? sender, EventArgs e) => FlushLog();

    private void FlushLog()
    {
        string text;
        lock (_logLock)
        {
            if (_logPending.Length == 0) return;
            text = _logPending.ToString();
            _logPending.Clear();
        }

        _logBox.AppendText(text);
        _logBox.ScrollToEnd();
    }
}
