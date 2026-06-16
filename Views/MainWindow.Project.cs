using Microsoft.Web.WebView2.Core;

namespace TEMO.AI;

public partial class MainWindow
{
    private async void NewProject_Click(object sender, RoutedEventArgs e)
    {
        if (_devProcess is { HasExited: false })
        {
            if (!ConfirmUnsavedChanges()) return;
            StopServer();
        }

        var gallery = new TemplateGalleryDialog { Owner = this };
        if (gallery.ShowDialog() != true || gallery.SelectedTemplate is not { } template) return;

        if (!Directory.Exists(template))
        {
            ShowMsg("⚠️  ไม่พบโฟลเดอร์ต้นแบบ");
            return;
        }

        string parentDir;
        using (var dlg = new WinForms.FolderBrowserDialog
        {
            Description = "เลือกที่จะเก็บโปรเจคใหม่",
        })
        {
            if (dlg.ShowDialog() != WinForms.DialogResult.OK) return;
            parentDir = dlg.SelectedPath;
        }

        var templateName = new DirectoryInfo(template.TrimEnd('\\', '/')).Name;
        var prompt = new PromptDialog("ตั้งชื่อโปรเจคใหม่", "ชื่อโฟลเดอร์โปรเจค",
            SuggestName(parentDir, templateName)) { Owner = this };
        if (prompt.ShowDialog() != true) return;

        var dest = Path.Combine(parentDir, SanitizeName(prompt.Value));
        if (Directory.Exists(dest) && Directory.EnumerateFileSystemEntries(dest).Any())
        {
            ShowMsg("⚠️  มีโฟลเดอร์ชื่อนี้อยู่แล้วและไม่ว่าง — เปลี่ยนชื่อใหม่");
            return;
        }

        await CreateProjectAsync(template, dest);
    }

    private void LoadOldProject_Click(object sender, RoutedEventArgs e)
    {
        if (_devProcess is { HasExited: false })
        {
            if (!ConfirmUnsavedChanges()) return;
            StopServer();
        }

        using var dlg = new WinForms.FolderBrowserDialog
        {
            Description = "เลือกโฟลเดอร์ Project",
            SelectedPath = Directory.Exists(_projectPath) ? _projectPath : "",
        };
        if (dlg.ShowDialog() != WinForms.DialogResult.OK) return;

        if (!File.Exists(Path.Combine(dlg.SelectedPath, "package.json")))
        {
            ShowMsg("⚠️  โฟลเดอร์นี้ไม่ใช่โปรเจค (ไม่พบ package.json)");
            return;
        }

        SwitchProject(dlg.SelectedPath);
        ShowMsg("โหลดโปรเจคแล้ว — กด START เพื่อเริ่มโปรเจค");
    }

    private async void SaveTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (!HasOpenProject()) { ShowMsg("ยังไม่ได้เลือกโปรเจค"); return; }

        var baseName = new DirectoryInfo(_projectPath.TrimEnd('\\', '/')).Name;
        var prompt = new PromptDialog("บันทึก Template", "ชื่อ Template",
            SuggestTemplateName(baseName)) { Owner = this };
        if (prompt.ShowDialog() != true) return;

        var dest = Path.Combine(TemplateStore.Root, SanitizeName(prompt.Value));
        if (Directory.Exists(dest) && Directory.EnumerateFileSystemEntries(dest).Any()
            && System.Windows.MessageBox.Show("มี Template ชื่อนี้อยู่แล้ว เขียนทับหรือไม่?",
                "TEMO.AI", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            if (_devProcess is { HasExited: false })
            {
                ShowMsg("กำลังบันทึก Template…");
                await CapturePreviewAsync(Path.Combine(_projectPath, "public", "Preview.png"));
            }
            TemplateStore.SaveAsTemplate(_projectPath, dest);
            ShowMsg($"✅  บันทึก Template แล้ว: {Path.GetFileName(dest)}");
        }
        catch (Exception ex)
        {
            ShowMsg($"⚠️  บันทึก Template ไม่สำเร็จ: {ex.Message}");
        }
    }

    private const string HideScrollbarScript =
        "(function(){var s=document.createElement('style');s.textContent='html::-webkit-scrollbar,body::-webkit-scrollbar{display:none!important}html,body{scrollbar-width:none!important;-ms-overflow-style:none!important}';document.head.appendChild(s);})();";

    private const string WaitForMediaScript = @"(async () => {
  const sleep = ms => new Promise(r => setTimeout(r, ms));
  const max = (p, ms) => Promise.race([p, sleep(ms)]);
  try {
    const h = Math.max(document.body.scrollHeight, document.documentElement.scrollHeight);
    for (let y = 0; y < h; y += Math.max(200, window.innerHeight)) { window.scrollTo(0, y); await sleep(80); }
    window.scrollTo(0, 0);
    document.querySelectorAll('img[loading=""lazy""]').forEach(i => { i.loading = 'eager'; });
    await sleep(120);
    const imgs = Array.from(document.images);
    await max(Promise.all(imgs.map(img => {
      if (img.complete && img.naturalWidth > 0) return img.decode().catch(() => {});
      return new Promise(res => {
        img.addEventListener('load', () => img.decode().then(res).catch(() => res()), { once: true });
        img.addEventListener('error', () => res(), { once: true });
      });
    })), 12000);
    if (document.fonts && document.fonts.ready) { try { await max(document.fonts.ready, 4000); } catch (e) {} }
    await sleep(150);
  } catch (e) {}
  return true;
})();";

    private static async Task WaitForMediaAsync(CoreWebView2 core)
    {
        var param = new JsonObject
        {
            ["expression"] = WaitForMediaScript,
            ["awaitPromise"] = true,
            ["returnByValue"] = true,
        };
        try { await core.CallDevToolsProtocolMethodAsync("Runtime.evaluate", param.ToJsonString()); }
        catch { }
    }

    private async Task CapturePreviewAsync(string destPath)
    {
        var web = new Microsoft.Web.WebView2.Wpf.WebView2();
        var host = new Window
        {
            Width = 1600,
            Height = 950,
            Left = -20000,
            Top = -20000,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
            Content = web,
        };
        web.DefaultBackgroundColor = System.Drawing.Color.White;
        host.Show();

        try
        {
            await web.EnsureCoreWebView2Async();
            var core = web.CoreWebView2;

            await NavigateAndWaitAsync(core, ServerUrl);
            await core.ExecuteScriptAsync(HideScrollbarScript);
            await WaitForMediaAsync(core);

            var size = ReadContentSize(await core.CallDevToolsProtocolMethodAsync("Page.getLayoutMetrics", "{}"));
            var metricsOverride = new JsonObject
            {
                ["width"] = (int)Math.Ceiling(size.W),
                ["height"] = (int)Math.Ceiling(size.H),
                ["deviceScaleFactor"] = 2,
                ["mobile"] = false,
            };
            await core.CallDevToolsProtocolMethodAsync("Emulation.setDeviceMetricsOverride", metricsOverride.ToJsonString());
            await WaitForMediaAsync(core);

            var full = ReadContentSize(await core.CallDevToolsProtocolMethodAsync("Page.getLayoutMetrics", "{}"));
            var param = new JsonObject
            {
                ["format"] = "png",
                ["captureBeyondViewport"] = true,
                ["clip"] = new JsonObject
                {
                    ["x"] = 0,
                    ["y"] = 0,
                    ["width"] = full.W,
                    ["height"] = full.H,
                    ["scale"] = 1,
                },
            };
            var shot = await core.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", param.ToJsonString());
            var data = JsonDocument.Parse(shot).RootElement.GetProperty("data").GetString();
            if (string.IsNullOrEmpty(data)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.WriteAllBytes(destPath, Convert.FromBase64String(data));
        }
        finally
        {
            host.Close();
            web.Dispose();
        }
    }

    private static (double W, double H) ReadContentSize(string layoutMetricsJson)
    {
        var root = JsonDocument.Parse(layoutMetricsJson).RootElement;
        var size = root.TryGetProperty("cssContentSize", out var css) ? css : root.GetProperty("contentSize");
        return (size.GetProperty("width").GetDouble(), size.GetProperty("height").GetDouble());
    }

    private static async Task NavigateAndWaitAsync(CoreWebView2 core, string url)
    {
        var tcs = new TaskCompletionSource();
        void Handler(object? s, CoreWebView2NavigationCompletedEventArgs e)
        {
            core.NavigationCompleted -= Handler;
            tcs.TrySetResult();
        }
        core.NavigationCompleted += Handler;
        core.Navigate(url);
        await Task.WhenAny(tcs.Task, Task.Delay(8000));
    }

    private static string SuggestTemplateName(string baseName)
    {
        var candidate = baseName;
        int i = 2;
        while (Directory.Exists(Path.Combine(TemplateStore.Root, candidate)))
            candidate = $"{baseName}-{i++}";
        return candidate;
    }

    private async Task CreateProjectAsync(string template, string dest)
    {
        StopServer();
        LoadingOverlay.Visibility = Visibility.Visible;
        WebView.Visibility = Visibility.Collapsed;
        LoadingText.Text = "กำลังสร้างโปรเจค (คัดลอกไฟล์ต้นแบบ)…";

        try
        {
            await Task.Run(() => TemplateStore.Copy(template, dest));
            ComponentStore.EnsureSeeded();
            AstroProjectSettings.DisableDevToolbar(dest);
        }
        catch (Exception ex)
        {
            LoadingText.Text = "Start server to preview";
            ShowMsg($"⚠️  สร้างโปรเจคไม่สำเร็จ: {ex.Message}");
            return;
        }

        SwitchProject(dest);
        LoadingText.Text = "กด START เพื่อเริ่มโปรเจค";
        ShowMsg($"✅  สร้างโปรเจคแล้ว: {Path.GetFileName(dest)}");
    }

    private void SwitchProject(string path)
    {
        _projectPath = path;
        FolderPathBox.Text = path;
        SettingsStore.SaveLastProject(path);
        LoadProject();
        UpdateDeployUi();
    }

    private static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(name.Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(clean) ? "project" : clean;
    }

    private static string SuggestName(string parentDir, string baseName)
    {
        var candidate = baseName;
        int i = 2;
        while (Directory.Exists(Path.Combine(parentDir, candidate)))
            candidate = $"{baseName}-{i++}";
        return candidate;
    }
}
