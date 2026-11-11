using System.Windows;
using System.IO;
namespace MidiRouter;

internal sealed class UiChecks
{
    private readonly App app;
    private Window MainWindow => app.MainWindow;

    public UiChecks(App app)
    {
        this.app = app;
    }
    private static void CheckThemeContrast(Window window)
    {
        if (window.Background is not System.Windows.Media.SolidColorBrush background ||
            window.Foreground is not System.Windows.Media.SolidColorBrush foreground)
            throw new InvalidOperationException("Theme colors missing");

        static double Luminance(System.Windows.Media.Color color)
        {
            static double Channel(byte value)
            {
                double component = value / 255d;
                return component <= .04045 ? component / 12.92 : Math.Pow((component + .055) / 1.055, 2.4);
            }
            return .2126 * Channel(color.R) + .7152 * Channel(color.G) + .0722 * Channel(color.B);
        }
        double back = Luminance(background.Color), text = Luminance(foreground.Color);
        if ((Math.Max(back, text) + .05) / (Math.Min(back, text) + .05) < 7)
            throw new InvalidOperationException("Theme text contrast below 7:1");
    }

    private static bool HasText(DependencyObject root, string expected)
    {
        if (root is System.Windows.Controls.TextBlock text && text.Text == expected)
            return true;
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
            if (HasText(System.Windows.Media.VisualTreeHelper.GetChild(root, i), expected))
                return true;
        return false;
    }

    private void CheckSettingsLanguages()
    {
        var main = (MidiRouter.MainWindow)MainWindow;
        var settings = new GeneralSettingsWindow(main.Model) { Owner = main };
        settings.Show();
        var tabs = ((System.Windows.Controls.DockPanel)settings.Content).Children
            .OfType<System.Windows.Controls.TabControl>().Single();
        var titles = new[] { "Ogólne", "Interfejs", "Wygląd" };
        var labels = new[] { "Język", "Główny interfejs MIDI", "Motyw kolorystyczny" };
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        int step = 0;
        timer.Tick += (_, _) =>
        {
            try
            {
                int languageIndex = step / 6;
                int tabIndex = step / 2 % 3;
                if (step % 2 == 0)
                {
                    UiPresentation.Configure(L.LanguageCodes[languageIndex], "Default");
                    tabs.SelectedIndex = tabIndex;
                }
                else
                {
                    foreach (var (item, index) in tabs.Items.Cast<System.Windows.Controls.TabItem>().Select((item, index) => (item, index)))
                        if (item.Header?.ToString() != L.T(titles[index]))
                            throw new InvalidOperationException($"Settings tab not translated: {L.Language}/{titles[index]}");
                    if (!HasText(settings, L.T(labels[tabIndex])) || !HasText(settings, L.T("Anuluj")) || !HasText(settings, L.T("Zapisz")))
                        throw new InvalidOperationException($"Settings content not translated: {L.Language}/{titles[tabIndex]}");
                }
                if (++step == L.LanguageCodes.Length * 6)
                {
                    timer.Stop();
                    settings.Close();
                    UiPresentation.Configure(main.Model.State.Language, main.Model.Graphics.Theme);
                    File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "settings-check-result.txt"),
                        "PASS: all settings tabs in all supported languages");
                    app.Shutdown(0);
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "settings-check-error.txt"), ex.ToString());
                app.Shutdown(1);
            }
        };
        timer.Start();
    }

    public void Run(string[] args)
    {
        var directory = Path.Combine(Path.GetTempPath(), "MidiPatchline-ui-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        app.Exit += (_, _) =>
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        };
        // Reproduce a window existing before its XAML has finished construction.
        var pendingWindow = new Window();
        UiPresentation.Configure("PL", "Light");
        var original = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 27, 32));
        pendingWindow.Background = original;
        if (!ReferenceEquals(pendingWindow.Background, original))
            throw new InvalidOperationException("Theme tracked an uninitialized window");
        pendingWindow.Close();
        UiPresentation.Configure("PL", "Default");
        new MainWindow(new RouterViewModel(Path.Combine(directory, "studio.json"))).Show();
        if (args.Contains("--settings-check"))
        {
            CheckSettingsLanguages();
            return;
        }
        if (args.Contains("--startup-check"))
        {
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            int steps = 0;
            timer.Tick += (_, _) =>
            {
                var languages = new[] { "FR", "ZH", "DE", "ES", "ENG", "PL" };
                var sourceHeadings = new[] { "SOURCES", "输入源", "QUELLEN", "FUENTES", "SOURCES", "ŹRÓDŁA" };
                var receiverHeadings = new[] { "DESTINATAIRES", "接收端", "EMPFÄNGER", "DESTINATARIOS", "RECEIVERS", "ODBIORCY" };
                if (steps < languages.Length * 2)
                {
                    int index = steps / 2;
                    if (steps % 2 == 0)
                        UiPresentation.Configure(languages[index], index % 2 == 0 ? "Midnight" : "Graphite");
                    else if (!HasText(MainWindow, sourceHeadings[index]) || !HasText(MainWindow, receiverHeadings[index]))
                        throw new InvalidOperationException($"Localized headings missing: {languages[index]}");
                }
                if (steps == 12)
                    UiPresentation.Configure("PL", "Light");
                if (steps == 13)
                {
                    CheckThemeContrast(MainWindow);
                    UiPresentation.Configure("PL", "WhiteLight");
                }
                if (steps == 14)
                {
                    CheckThemeContrast(MainWindow);
                    var white = ((System.Windows.Media.SolidColorBrush)MainWindow.Background).Color;
                    if (white.R != 255 || white.G != 255 || white.B != 255)
                        throw new InvalidOperationException("White theme background must be pure white");
                    UiPresentation.Configure("PL", "GrayLight");
                }
                if (steps == 15)
                {
                    CheckThemeContrast(MainWindow);
                    var gray = ((System.Windows.Media.SolidColorBrush)MainWindow.Background).Color;
                    if (gray.R != gray.G || gray.G != gray.B || gray.R == 255)
                        throw new InvalidOperationException("Gray theme background must be neutral gray");
                }
                if (steps == 16 && MainWindow is MidiRouter.MainWindow view)
                    UiPresentation.Configure(view.Model.State.Language, view.Model.Graphics.Theme);
                // Exercise both cursor states while the real input hook is active.
                app.SetDiagnosticPointer(steps % 2 == 0 ? System.Windows.Input.Cursors.None : null);
                System.Windows.Input.Mouse.UpdateCursor();
                if (++steps == 18)
                {
                    if (!NativeTheme.HasNativeCaptionButtons(MainWindow))
                        throw new InvalidOperationException("Native caption buttons missing");
                    timer.Stop();
                    app.SetDiagnosticPointer(null);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "startup-check-result.txt"), "PASS: window startup and repeated cursor updates under active input hook");
                    app.Shutdown(0);
                }
            };
            timer.Start();
        }
    }
}
