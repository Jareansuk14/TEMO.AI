namespace TEMO.AI;

public partial class MainWindow
{
    private Dictionary<string, TextBox> _siteBoxes => _session.SiteBoxes;

    private ImageEntry? _siteQrEntry;
    private System.Windows.Controls.Image? _siteQrThumb;

    private void BuildSitePanel()
    {
        _siteBoxes.Clear();
        SitePanel.Children.Clear();

        SitePanel.Children.Add(Ui.FieldLabel("Site URL"));
        SitePanel.Children.Add(MakeSiteBox("site-url"));

        SitePanel.Children.Add(Ui.Divider());
        SitePanel.Children.Add(Ui.FieldLabel("External Link"));
        SitePanel.Children.Add(MakeSiteBox("ext-link"));

        SitePanel.Children.Add(Ui.Divider());
        SitePanel.Children.Add(Ui.SectionHeader("LINE"));
        SitePanel.Children.Add(Ui.FieldLabel("LINE ID"));
        SitePanel.Children.Add(MakeSiteBox("line-id"));

        SitePanel.Children.Add(Ui.Divider());
        SitePanel.Children.Add(Ui.FieldLabel("LINE QR"));

        _siteQrEntry = new ImageEntry { Id = "line-qr", Label = "QR LINE", Group = "", HasAlt = false };
        _siteQrThumb = new System.Windows.Controls.Image
        {
            Width = 108,
            Height = 108,
            Stretch = System.Windows.Media.Stretch.Uniform,
            ClipToBounds = true,
        };

        var qrLabel = new TextBlock
        {
            Text = "LINE QR",
            FontSize = 10,
            Foreground = Ui.Brush(0x888888),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0),
        };

        var qrCard = new Border
        {
            Background = Ui.Brush(0x1A1A1A),
            BorderBrush = Ui.Brush(0x282828),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6),
            Margin = new Thickness(0, 0, 0, 12),
            Width = 122,
            Cursor = Cursors.Hand,
            Tag = _siteQrEntry,
            Child = new StackPanel { Children = { _siteQrThumb, qrLabel } },
        };
        qrCard.MouseDown += (_, _) => { if (_siteQrEntry != null) OpenImageEditDialog(_siteQrEntry); };
        if (_siteQrEntry != null) WireImageDrop(qrCard, _siteQrEntry);
        Ui.WireCardHover(qrCard);
        SitePanel.Children.Add(qrCard);
    }

    private TextBox MakeSiteBox(string id)
    {
        var box = Ui.MakeInput(this);
        box.Margin = new Thickness(0, 0, 0, 12);
        _siteBoxes[id] = box;
        WireEditorTracking(box);
        return box;
    }

    private void PullSiteSettings()
    {
        if (Io.ReadOrNull(SrcPath(SiteConfig)) is not { } content) return;

        void Set(string id, string pattern)
        {
            var m = Regex.Match(content, pattern, RegexOptions.Singleline);
            if (m.Success && _siteBoxes.TryGetValue(id, out var box))
                box.Text = m.Groups[1].Value.Trim();
        }

        Set("site-url", @"export\s+const\s+SITE\s*=\s*\{[\s\S]*?\burl:\s*""([^""]*)""");
        Set("ext-link", @"export\s+const\s+EXTERNAL_LINK\s*=\s*""([^""]*)""");
        Set("line-id", @"export\s+const\s+LINE\s*=\s*\{[\s\S]*?\bid:\s*""([^""]*)""");

        PullSiteQr();
    }

    private void PullSiteQr()
    {
        if (_siteQrEntry == null || _siteQrThumb == null) return;
        var imgContent = ImagesStore.ReadConfig(_projectPath);
        if (imgContent.Length == 0) return;

        var (src, alt) = ImagesStore.ReadValues(imgContent, "line-qr");
        _siteQrEntry.SrcValue = src;
        _siteQrEntry.AltValue = alt;
        _siteQrEntry.OriginalSrc = src;
        LoadThumbnail(_siteQrThumb, src);
    }

    private void RefreshSiteQrThumb()
    {
        if (_siteQrEntry == null || _siteQrThumb == null) return;
        LoadThumbnail(_siteQrThumb, _siteQrEntry.SrcValue);
    }

    private bool TryGetValidatedSiteUrl(out string siteUrl)
    {
        siteUrl = "";
        if (!_siteBoxes.TryGetValue("site-url", out var urlBox)) return true;

        var value = urlBox.Text.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(value))
        {
            ShowInvalidSiteUrl(urlBox, "กรุณากรอก Site URL เช่น https://www.yourdomain.com");
            return false;
        }

        if (!value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            ShowInvalidSiteUrl(urlBox, "Site URL ต้องขึ้นต้นด้วย https:// เช่น https://www.yourdomain.com");
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !uri.Host.Contains('.') ||
            uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            ShowInvalidSiteUrl(urlBox, "รูปแบบ Site URL ไม่ถูกต้อง ให้กรอกเฉพาะโดเมน เช่น https://www.yourdomain.com");
            return false;
        }

        siteUrl = value;
        urlBox.Text = siteUrl;
        return true;
    }

    private static void ShowInvalidSiteUrl(TextBox urlBox, string message)
    {
        System.Windows.MessageBox.Show(
            message,
            "Site URL ไม่ถูกต้อง",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        urlBox.Focus();
        urlBox.SelectAll();
    }

    private void SaveSiteSettings(string siteUrl)
    {
        SaveSiteTs(siteUrl);
        SaveAstroConfig(siteUrl);
        SaveRobotsTxt(siteUrl);
    }

    private void SaveSiteTs(string siteUrl)
    {
        if (Io.ReadOrNull(SrcPath(SiteConfig)) is not { } text) return;

        text = Rx.Wrap(text,
            @"(export\s+const\s+SITE\s*=\s*\{[\s\S]*?\burl:\s*"")[^""]*(""\s*,)",
            siteUrl, RegexOptions.Singleline);

        if (_siteBoxes.TryGetValue("ext-link", out var extBox))
            text = Rx.Wrap(text,
                @"(export\s+const\s+EXTERNAL_LINK\s*=\s*"")[^""]*("")",
                extBox.Text.Trim());

        if (_siteBoxes.TryGetValue("line-id", out var lineIdBox))
            text = Rx.Wrap(text,
                @"(export\s+const\s+LINE\s*=\s*\{[\s\S]*?\bid:\s*"")[^""]*("")",
                lineIdBox.Text.Trim(), RegexOptions.Singleline);

        Io.Write(SrcPath(SiteConfig), text);
    }

    private void SaveAstroConfig(string siteUrl)
    {
        var path = Path.Combine(_projectPath, "astro.config.mjs");
        if (Io.ReadOrNull(path) is not { } text) return;

        var updated = Rx.Wrap(text, @"(site:\s*"")[^""]*("")", siteUrl);
        if (updated != text) Io.Write(path, updated);
    }

    private void SaveRobotsTxt(string siteUrl)
    {
        var path = Path.Combine(_projectPath, "public", "robots.txt");
        if (Io.ReadOrNull(path) is not { } text) return;

        var updated = Regex.Replace(text, @"(Sitemap:\s*).*", $"${{1}}{siteUrl}/sitemap-index.xml");
        if (updated != text) Io.Write(path, updated);
    }
}
