namespace TEMO.AI;

public partial class MainWindow
{
    private string? _lastPhase;

    private void ToggleServer_Click(object sender, RoutedEventArgs e)
    {
        if (_devProcess is { HasExited: false })
        {
            if (!TryStopServer()) return;
        }
        else
        {
            StartServer();
        }
    }

    private bool TryStopServer()
    {
        if (_devProcess is not { HasExited: false }) return true;
        if (!ConfirmUnsavedChanges()) return false;
        StopServer();
        return true;
    }

    private void StartServer()
    {
        if (!Directory.Exists(_projectPath))
        {
            ShowMsg("ยังไม่ได้เลือกโปรเจค กด \"New Project\" หรือ \"Load Project\"");
            return;
        }

        if (!NodeRuntime.IsAvailable())
        {
            NodeRuntime.OpenDownloadPage();
            ShowMsg("⚠️  ไม่พบ Node.js — ต้องติดตั้งก่อนจึงจะรันโปรเจคได้ (เปิดหน้าดาวน์โหลดให้แล้ว)");
            return;
        }

        LoadProject();
        UpdateSaveAllUi();

        _waitingForPreview = true;
        _navScheduled = false;
        _lastPhase = null;
        LoadingOverlay.Visibility = Visibility.Visible;
        WebView.Visibility = Visibility.Collapsed;

        bool firstRun = !Directory.Exists(Path.Combine(_projectPath, "node_modules"));
        var command = firstRun
            ? "npm install && npm run build && npm run dev"
            : "npm run dev";
        LoadingText.Text = firstRun ? "กำลังติดตั้งโปรเจค..." : "Starting server…";

        AstroProjectSettings.DisableDevToolbar(_projectPath);

        _devProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                WorkingDirectory = _projectPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
            EnableRaisingEvents = true
        };
        _devProcess.OutputDataReceived += OnServerOut;
        _devProcess.ErrorDataReceived += OnServerOut;
        _devProcess.Exited += (_, _) => Ui.RunOnUi(this, UpdateServerUi);
        _devProcess.Start();
        _devProcess.BeginOutputReadLine();
        _devProcess.BeginErrorReadLine();
        UpdateServerUi();
    }

    private void OnServerOut(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is null || _navScheduled) return;

        UpdatePhaseText(e.Data);

        if (!e.Data.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            && !e.Data.Contains("Local:", StringComparison.OrdinalIgnoreCase)) return;
        _navScheduled = true;
        Ui.RunOnUi(this, async () =>
        {
            await Task.Delay(800);
            WebView.CoreWebView2?.Navigate(ServerUrl);
        });
    }

    private void UpdatePhaseText(string line)
    {
        string? phase = null;
        if (line.Contains("npm warn", StringComparison.OrdinalIgnoreCase)
            || line.Contains("added ", StringComparison.OrdinalIgnoreCase)
            || line.Contains("packages in", StringComparison.OrdinalIgnoreCase)
            || line.Contains("idealTree", StringComparison.OrdinalIgnoreCase))
            phase = "กำลังติดตั้ง dependencies…";
        else if (line.Contains("building", StringComparison.OrdinalIgnoreCase)
                 || line.Contains("astro build", StringComparison.OrdinalIgnoreCase)
                 || line.Contains("Completed in", StringComparison.OrdinalIgnoreCase)
                 || line.Contains("pages built", StringComparison.OrdinalIgnoreCase))
        {
            phase = "กำลัง build โปรเจค…";
            if (line.Contains("Completed in", StringComparison.OrdinalIgnoreCase)
                || line.Contains("pages built", StringComparison.OrdinalIgnoreCase))
                AstroProjectSettings.DisableDevToolbar(_projectPath);
        }

        if (phase is null || phase == _lastPhase) return;
        _lastPhase = phase;
        Ui.RunOnUi(this, () => LoadingText.Text = phase);
    }

    private void StopServer()
    {
        var proc = _devProcess;
        _devProcess = null;
        _waitingForPreview = false;
        _navScheduled = false;

        UpdateServerUi();
        LoadingOverlay.Visibility = Visibility.Visible;
        WebView.Visibility = Visibility.Collapsed;
        LoadingText.Text = "Start server to preview";
        LockContentPanel();

        if (proc is null) return;

        Task.Run(() =>
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            try { proc.WaitForExit(3000); } catch { }
            proc.Dispose();
        });
    }

    private void UpdateServerUi()
    {
        bool on = _devProcess is { HasExited: false };
        bool hasProject = HasOpenProject();

        StatusDot.Fill = Ui.Brush(on ? 0x22C55Eu : 0x3A3A3Au);
        StatusText.Text = on ? "Running" : "Offline";
        ServerBtn.Content = on ? "■  STOP" : "▶  START";
        SaveTemplateBtn.IsEnabled = on;

        if (on)
        {
            ServerBtn.IsEnabled = true;
            ServerBtn.Style = (Style)FindResource("BtnDanger");
        }
        else
        {
            ServerBtn.IsEnabled = hasProject;
            ServerBtn.Style = (Style)FindResource("BtnAccent");
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) =>
        WebView.CoreWebView2?.Navigate(ServerUrl);

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2 is { CanGoBack: true } core) core.GoBack();
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2 is { CanGoForward: true } core) core.GoForward();
    }
}
