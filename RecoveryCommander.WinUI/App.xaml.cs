using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Extensions.DependencyInjection;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using RecoveryCommanderWinUI.Services;
using RecoveryCommanderWinUI.Dialogs;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RecoveryCommanderWinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    private DialogService? _dialogService;

    public static Window? MainWindow => _instance?._window;
    private static App? _instance;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    // Win32 MessageBox for showing errors before the WinUI window is available
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    private const uint MB_OK = 0x00000000;
    private const uint MB_ICONERROR = 0x00000010;

    public App()
    {
        _instance = this;

        try
        {
            // Start normally. Some recovery actions may still prompt for elevation later.
            // We only auto-elevate when explicitly requested so the app remains usable
            // for non-admin users and when UAC is declined.
            if (!IsRunningAsAdministrator() && ShouldAttemptElevationOnStartup())
            {
                try
                {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        Process.Start(startInfo);
                        Environment.Exit(0);
                        return;
                    }
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // If relaunch fails due to Windows shell error, continue without admin
                }
                catch (InvalidOperationException)
                {
                    // Invalid executable path or process start failed
                }
                catch (PlatformNotSupportedException)
                {
                    // Shell execute is not supported on this platform
                }
            }

            try
            {
                // Load theme resources first so XAML parsing can resolve StaticResource references.
                try
                {
                    LoadThemeResources();
                }
#pragma warning disable CA1031 // Theme loading has a file-based fallback.
                catch (Exception ex)
                {
                    LogError("LoadThemeResources", ex);
                }
#pragma warning restore CA1031

                this.InitializeComponent();
            }
            catch (Exception ex)
            {
                LogError("InitializeComponent", ex);
                ShowCrashDialog(ex);
                throw;
            }

            try
            {
                RecoveryCommander.Core.ServiceContainer.Initialize(services =>
                {
                    // Register DialogService as singleton - will be initialized after window is created
                    services.AddSingleton<IDialogService>(sp =>
                    {
                        if (_instance?._dialogService == null && _instance?._window != null)
                        {
                            _instance._dialogService = new DialogService(_instance._window);
                        }
                        return _instance?._dialogService ?? throw new InvalidOperationException("DialogService not initialized");
                    });

                    // Register the WinRE wizard integration service for the WinUI host.
                    services.AddSingleton<IWinReWizardService, WinReWizardService>();
                });
            }
            catch (Exception ex)
            {
                LogError("ServiceContainer.Initialize", ex);
                ShowCrashDialog(ex);
                throw;
            }

            this.UnhandledException += App_UnhandledException;
        }
        catch (Exception ex)
        {
            // Top-level constructor catch — fires when anything above throws and
            // ensures a visible error is shown even before the window exists.
            LogError("App.Constructor", ex);
            ShowCrashDialog(ex);
            throw;
        }
    }

    private void LoadThemeResources()
    {
        // Try adding the Theme/Styles.xaml as a merged dictionary using a relative Uri first.
        try
        {
            var rd = new Microsoft.UI.Xaml.ResourceDictionary
            {
                Source = new Uri("Theme/Styles.xaml", UriKind.Relative)
            };
            this.Resources.MergedDictionaries.Add(rd);
            return;
        }
    #pragma warning disable CA1031 // Resource URI loading has a file-based fallback.
        catch { /* fall through to file-based load */ }
    #pragma warning restore CA1031

        // Fallback: load XAML from disk and parse it. This helps when ms-appx URI resolution
        // fails for unpackaged Release builds.
        var themePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Theme", "Styles.xaml");
        if (System.IO.File.Exists(themePath))
        {
            var xamlText = System.IO.File.ReadAllText(themePath);
            var obj = Microsoft.UI.Xaml.Markup.XamlReader.Load(xamlText);
            if (obj is Microsoft.UI.Xaml.ResourceDictionary parsedRd)
            {
                this.Resources.MergedDictionaries.Add(parsedRd);
            }
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    private static bool ShouldAttemptElevationOnStartup()
    {
        var envValue = Environment.GetEnvironmentVariable("RC_REQUEST_ELEVATION_ON_STARTUP");
        return string.Equals(envValue, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(envValue, "true", StringComparison.OrdinalIgnoreCase);
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        ShowCrashDialog(e.Exception);
        e.Handled = true; // Attempt to prevent instant crash to show dialog
    }

    private static void LogError(string context, Exception ex)
    {
#pragma warning disable CA1031 // Logging must never throw — broad catch is intentional here
        try
        {
            var logPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "RecoveryCommander_Crash.log");
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}\nException: {ex.GetType().Name}\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}\n\n";
            System.IO.File.AppendAllText(logPath, logMessage);
        }
        catch
        {
            // Logging failed - nothing we can do
        }
#pragma warning restore CA1031
    }

    private void ShowCrashDialog(Exception ex)
    {
        LogError("ShowCrashDialog", ex);

        // Always try the native Win32 MessageBox first — it works even before
        // the XAML window is created and is always visible to the user.
#pragma warning disable CA1031 // Crash dialog must never throw — broad catch is intentional here
        try
        {
            var inner = ex.InnerException != null
                ? $"\n\nInner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}"
                : string.Empty;

            var logPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "RecoveryCommander_Crash.log");

            var text = $"RecoveryCommander encountered an error and could not continue.\n\n" +
                       $"Error Type: {ex.GetType().FullName}\n" +
                       $"Message: {ex.Message}{inner}\n\n" +
                       $"Stack Trace:\n{ex.StackTrace}\n\n" +
                       $"A full log has been saved to:\n{logPath}";

#pragma warning disable CA1806 // Return value intentionally discarded — we only care that the box appeared
            _ = MessageBox(IntPtr.Zero, text, "RecoveryCommander — Crash Report", MB_OK | MB_ICONERROR);
#pragma warning restore CA1806
        }
        catch
        {
            // Absolute last resort — nothing more we can do.
        }
#pragma warning restore CA1031

        // Also try the XAML dialog if the window is available (non-fatal errors)
        if (_window?.Content?.XamlRoot != null)
        {
            try
            {
                var crashDialog = new CrashDialog(ex)
                {
                    XamlRoot = _window.Content.XamlRoot
                };
                _ = crashDialog.ShowAsync();
            }
            catch (InvalidOperationException) { }
            catch (System.Runtime.InteropServices.COMException) { }
        }
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _dialogService = new DialogService(_window);
            _window.Activate();
        }
        catch (Exception ex)
        {
            LogError("OnLaunched", ex);
            ShowCrashDialog(ex);
            throw;
        }
    }
}


