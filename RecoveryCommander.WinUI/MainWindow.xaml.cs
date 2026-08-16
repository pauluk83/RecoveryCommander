using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.UI;

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
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

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
            RootFrame.Navigate(typeof(MainPage));
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


