using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;

namespace TEMO.AI;

public partial class MainWindow : Window
{
    private const string SiteConfig = @"config\site.ts";
    private const string ServerUrl = "http://localhost:4321";

    private readonly MainViewModel _vm = new();
    private ProjectSession _session => _vm.Session;

    private List<FieldDef> _fields => _session.Fields;
    private Dictionary<string, TextBox> _boxes => _session.Boxes;
    private Dictionary<string, TextBox> _cssBoxes => _session.CssBoxes;
    private List<LayoutComponent> _layoutComponents => _session.LayoutComponents;

    private readonly Dictionary<string, Border> _cssColorPreviews = [];

    private Process? _devProcess;
    private string _projectPath { get => _session.ProjectPath; set => _session.ProjectPath = value; }
    private string? _loadedProjectPath;
    private bool _waitingForPreview;
    private bool _navScheduled;
    private readonly List<string> _serverLog = [];
    private int _selectedIndexForSwap = -1;
    private DispatcherTimer? _msgTimer;

    public MainWindow()
    {
        InitializeComponent();

        ApplyDevLayoutMode();

        Title = AppInfo.AppName;
        BrandTitleText.Text = AppInfo.TitleWithVersion;
        LoadingBrandText.Text = AppInfo.AppName;
        _projectPath = "";
        FolderPathBox.Text = "ยังไม่ได้เลือกโปรเจค";
        LoadingText.Text = "New Project or Load Project";
        GenBtn.Visibility = Visibility.Visible;
        UpdateDeployUi();
        TakeSavedSnapshot();
        Loaded += (_, _) => LoadApiKey();
        Loaded += (_, _) => { ProjectPaths.MigrateExisting(); RefreshNewBadge(); };
        Loaded += async (_, _) => await InitWebViewAsync();
        Closing += OnWindowClosing;
        Closed += (_, _) => StopServer();
        KeyDown += OnGlobalKeyDown;
    }

    private void OnGlobalKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.S
            && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control
            && SaveAllBtn.IsEnabled)
        {
            SaveAll_Click(sender, e);
            e.Handled = true;
        }
    }

    private async Task InitWebViewAsync()
    {
        await WebView.EnsureCoreWebView2Async();
        WebView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 10, 10, 10);
        WebView.Visibility = Visibility.Collapsed;
        WebView.CoreWebView2.NavigationCompleted += OnPreviewNavigationCompleted;
    }

    private void OnPreviewNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!_waitingForPreview || !e.IsSuccess || !IsServerUrl(WebView.CoreWebView2.Source)) return;
        _waitingForPreview = false;
        LoadingOverlay.Visibility = Visibility.Collapsed;
        WebView.Visibility = Visibility.Visible;
        UnlockContentPanel();
    }

    private (ScrollViewer Scroll, System.Windows.Controls.Panel Panel)[] EditorPanels =>
    [
        (ContentScroll,  ContentPanel),
        (LayoutScroll,   LayoutPanel),
        (CssScroll,      CssPanel),
        (ImagesScroll,   ImagesPanel),
        (SiteScroll,     SitePanel),
        (KeywordsScroll, KeywordsPanel),
    ];

    private void UnlockContentPanel()
    {
        ActionBar.Opacity = 1;
        UpdateSaveAllUi();
        UpdateDeployUi();
        ContentTabToolbar.IsEnabled = true;
        CssTabToolbar.IsEnabled = true;
        ImgTabToolbar.IsEnabled = true;

        var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400));
        foreach (var (scroll, panel) in EditorPanels)
        {
            scroll.IsEnabled = true;
            panel.Visibility = Visibility.Visible;
            panel.BeginAnimation(OpacityProperty, anim);
        }
    }

    private void LockContentPanel()
    {
        SaveAllBtn.IsEnabled = false;
        UpdateDeployUi();
        ContentTabToolbar.IsEnabled = false;
        CssTabToolbar.IsEnabled = false;
        ImgTabToolbar.IsEnabled = false;

        foreach (var (scroll, panel) in EditorPanels)
        {
            scroll.IsEnabled = false;
            panel.BeginAnimation(OpacityProperty, null);
            panel.Opacity = 0;
            panel.Visibility = Visibility.Hidden;
        }

        CollapseAllFlyouts();
    }

    private bool HasOpenProject() => ProjectPaths.IsProject(_projectPath);

    private void RefreshNewBadge() =>
        LoadProjectNewBadge.Visibility =
            ProjectPaths.HasAnyNew() ? Visibility.Visible : Visibility.Collapsed;

    private void UpdateDeployUi()
    {
        var hasProject = HasOpenProject();
        DeployBtn.IsEnabled = hasProject;
        ActionBar.Opacity = hasProject ? 1 : 0.35;
        UpdateServerUi();
    }

    private static bool IsServerUrl(string url) =>
        url.StartsWith(ServerUrl, StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("http://127.0.0.1:4321", StringComparison.OrdinalIgnoreCase);

    private void LoadProject()
    {
        if (string.IsNullOrWhiteSpace(_projectPath) || !Directory.Exists(_projectPath)) return;

        _suppressSaveTracking = true;
        using var session = Io.Session();
        try
        {
            ComponentStore.EnsureSeeded();
            SectionCatalog.Reload();
            LoadLayoutComponents();
            ProjectComponentSync.Sync(_projectPath, _layoutComponents);
            ProjectMigrations.Run(_projectPath);
            BuildFields();
            BuildContentPanel();
            PullAllToBoxes();
            LoadCssVariables();
            BuildSitePanel();
            PullSiteSettings();
            BuildKeywordsPanel();
            PullKeywords();
            BuildContentKeywordRow();
            ImagesStore.NormalizeStandardDimensions(_projectPath);
            BuildImagesPanel();
            PullImages();
            _undoHistory.Clear();
            TakeSavedSnapshot();
            _loadedProjectPath = _projectPath;
        }
        finally
        {
            _suppressSaveTracking = false;
            UpdateUndoButtons();
        }
    }

    private string SrcPath(string relFile) => ProjectPaths.Src(_projectPath, relFile);

    private string PublicPath(string src) => ProjectPaths.Public(_projectPath, src);

    private void ShowMsg(string msg)
    {
        StatusLabel.Text = msg;
        StatusLabel.Foreground = Ui.Brush(0xE0E0E0);
        _msgTimer?.Stop();
        _msgTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _msgTimer.Tick += (_, _) =>
        {
            StatusLabel.Text = "Ready";
            StatusLabel.Foreground = Ui.Brush(0x454545);
            _msgTimer?.Stop();
        };
        _msgTimer.Start();
    }
}
