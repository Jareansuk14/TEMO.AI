using System.Globalization;
using System.Windows.Media.Animation;

namespace TEMO.AI;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Title = AppInfo.AppName;
        BrandTitleText.Text = AppInfo.AppName;
        var (savedUser, savedPassword, remember) = LoginRememberService.Load();
        if (!string.IsNullOrEmpty(savedUser))
            TxtUsername.Text = savedUser;
        if (!string.IsNullOrEmpty(savedPassword))
            TxtPassword.Password = savedPassword;
        ChkRememberUsername.IsChecked = remember;
        TxtUsername.KeyDown += (_, e) => { if (e.Key == Key.Return) TxtPassword.Focus(); };
        TxtPassword.KeyDown += async (_, e) => { if (e.Key == Key.Return) await DoLogin(); };
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        try { await DoLogin(); }
        catch (Exception ex) { ShowException(ex); }
    }

    private void ShowException(Exception ex)
    {
        SetLoading(false);

        var sb = new StringBuilder();
        sb.AppendLine(ex.Message);

        var inner = ex.InnerException;
        var depth = 0;
        while (inner != null && depth < 5)
        {
            sb.AppendLine();
            sb.Append("→ ").AppendLine(inner.Message);
            inner = inner.InnerException;
            depth++;
        }

        System.Windows.MessageBox.Show(sb.ToString(), "เกิดข้อผิดพลาด", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async Task DoLogin()
    {
        SetLoading(true);
        var (ok, err, locked) = await AuthApiService.LoginAsync(TxtUsername.Text.Trim(), TxtPassword.Password);
        SetLoading(false);
        if (ok)
        {
            LoginRememberService.Save(
                TxtUsername.Text,
                TxtPassword.Password,
                ChkRememberUsername.IsChecked == true);
            var main = new MainWindow();
            System.Windows.Application.Current.MainWindow = main;
            main.Show();
            Close();
        }
        else
        {
            ShowError(err ?? "เกิดข้อผิดพลาด", locked);
        }
    }

    private void SetLoading(bool loading)
    {
        BtnLogin.IsEnabled = !loading;
        Spinner.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        BtnLabel.Text = loading ? "กำลังเข้าสู่ระบบ..." : "เข้าสู่ระบบ";
        if (loading)
        {
            StopErrorMarquee();
            ErrorBorder.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowError(string message, bool locked = false)
    {
        StopErrorMarquee();
        TxtError.Text = message;
        ErrorBorder.Background = locked
            ? new SolidColorBrush(Color.FromRgb(0x2A, 0x20, 0x15))
            : new SolidColorBrush(Color.FromRgb(0x2A, 0x15, 0x15));
        ErrorBorder.Visibility = Visibility.Visible;
        BtnLogin.IsEnabled = !locked;
        ScheduleErrorMarquee();
    }

    private void ScheduleErrorMarquee()
    {
        Dispatcher.BeginInvoke(() =>
        {
            ErrorBorder.UpdateLayout();
            Dispatcher.BeginInvoke(StartErrorMarqueeIfNeeded, DispatcherPriority.Render);
        }, DispatcherPriority.Loaded);
    }

    private void StartErrorMarqueeIfNeeded()
    {
        StopErrorMarquee();

        var textWidth = MeasureErrorTextWidth(TxtError.Text);
        var clipWidth = ErrorTextClip.ActualWidth;
        if (textWidth <= clipWidth || clipWidth <= 0)
            return;

        var overflow = textWidth - clipWidth;
        var duration = TimeSpan.FromSeconds(Math.Max(4, overflow / 30));

        var anim = new DoubleAnimation(0, -overflow, duration)
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        TxtErrorTransform.BeginAnimation(TranslateTransform.XProperty, anim);
    }

    private double MeasureErrorTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var dpi = VisualTreeHelper.GetDpi(TxtError);
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            TxtError.FlowDirection,
            new Typeface(TxtError.FontFamily, TxtError.FontStyle, TxtError.FontWeight, TxtError.FontStretch),
            TxtError.FontSize,
            TxtError.Foreground,
            dpi.PixelsPerDip);
        return formatted.WidthIncludingTrailingWhitespace;
    }

    private void StopErrorMarquee()
    {
        TxtErrorTransform.BeginAnimation(TranslateTransform.XProperty, null);
        TxtErrorTransform.X = 0;
    }
}
