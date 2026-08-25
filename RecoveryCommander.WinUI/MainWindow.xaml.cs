using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RecoveryCommanderWinUI;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    private Border appTitleBar;
    private Frame rootFrame;

    public MainWindow()
    {
        try
        {
            // Build UI in code to avoid ms-appx resource resolution in unpackaged Release builds
            appTitleBar = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 3, 9, 20)),
                Height = 40
            };

            var titleText = new TextBlock
            {
                Text = "RecoveryCommander",
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Microsoft.UI.Xaml.Thickness(12, 0, 0, 0)
            };
            appTitleBar.Child = titleText;

            rootFrame = new Frame();

            var rootGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 3, 9, 20))
            };
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            rootGrid.Children.Add(appTitleBar);
            Grid.SetRow(appTitleBar, 0);

            rootGrid.Children.Add(rootFrame);
            Grid.SetRow(rootFrame, 1);

            Content = rootGrid;
        }
    #pragma warning disable CA1031 // Startup logging must capture any construction failure before rethrowing.
        catch (Exception ex)
        {
            try
            {
                var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RecoveryCommander_Crash.log");
                var hr = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MainWindow.InitializeComponent Exception:\nHResult: 0x{hr:X8}\n{ex.ToString()}\n\n");
            }
            catch
            {
                // Ignore logging failures
            }
            throw;
        }
#pragma warning restore CA1031

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(appTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.TitleBar.BackgroundColor = Color.FromArgb(255, 3, 9, 20);
        AppWindow.TitleBar.ForegroundColor = Colors.White;
        AppWindow.TitleBar.InactiveBackgroundColor = Color.FromArgb(255, 3, 9, 20);
        AppWindow.TitleBar.InactiveForegroundColor = Color.FromArgb(255, 168, 185, 202);
        AppWindow.TitleBar.ButtonBackgroundColor = Color.FromArgb(255, 3, 9, 20);
        AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Color.FromArgb(255, 3, 9, 20);
        AppWindow.TitleBar.ButtonInactiveForegroundColor = Color.FromArgb(255, 112, 131, 150);
        AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 7, 24, 40);
        AppWindow.TitleBar.ButtonHoverForegroundColor = Colors.White;

        // Launch the window maximized (full screen with title bar accessible)
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }

        // Navigate the root frame to the main page on startup.
        // Broad catch is intentional: navigation errors must not crash the app; log and continue.
#pragma warning disable CA1031 // Navigation failures should never throw — log and continue
        try
        {
            rootFrame.Navigate(typeof(MainPage));
        }
        catch (Exception ex)
        {
            // Log navigation error
            try
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RecoveryCommander_Navigation.log"),
                    $"Navigation Error: {ex.Message}\n{ex.StackTrace}\n");
            }
            catch
            {
                // Swallow any logging failures — nothing more we can do at startup.
            }
        }
#pragma warning restore CA1031
    }
}


