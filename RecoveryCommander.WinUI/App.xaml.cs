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
                // InitializeComponent first so this.Resources is available and App.xaml's
                // <ResourceDictionary Source="Theme/Styles.xaml" /> is loaded via XAML.
                this.InitializeComponent();

                // Fallback: if XAML-based theme load failed (e.g. unpackaged build URI issues),
                // patch the merged dictionaries from disk so StaticResource lookups still resolve.
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
        Microsoft.UI.Xaml.ResourceDictionary? appResources;
        try
        {
            appResources = this.Resources;
        }
#pragma warning disable CA1031 // Resource access can transiently fail in unpackaged builds — broad catch is intentional here
        catch (Exception ex)
#pragma warning restore CA1031
        {
            // This can happen if called from constructor immediately after InitializeComponent
            // in Release publish builds (WinUI hydration race). Log it; the OnLaunched
            // hook will call LoadThemeResources() again BEFORE MainWindow construction,
            // by which time this.Resources is always hydrated.
            LogError("LoadThemeResources [dbg] get_Resources COMException — OnLaunched will retry", ex);
            return;
        }

        LoadThemeResourcesCore(appResources);
    }

    private static bool ProbeThemeLoaded(Microsoft.UI.Xaml.ResourceDictionary appResources)
    {
        // Probe for actual registered keys rather than trusting Source strings.
        // This correctly catches the case where App.xaml's <ResourceDictionary Source="Theme/Styles.xaml"/>
        // resolves Source at parse time but fails to actually populate its MergedDictionaries
        // in unpackaged Release builds (ms-appx:/// resolution failure).
        try
        {
            object _ = appResources["PrimaryAccentBrush"];
            object __ = appResources["MutedTextBrush"];
            return true;
        }
#pragma warning disable CA1031 // Resource key probing must return false on any failure — broad catch is intentional here
        catch { return false; }
#pragma warning restore CA1031
    }

    private static bool MergeXamlFileFromDisk(Microsoft.UI.Xaml.ResourceDictionary appResources, string baseDir, string relPath)
    {
        var fullPath = System.IO.Path.Combine(baseDir, relPath);
        if (!System.IO.File.Exists(fullPath))
        {
            LogError("LoadThemeResources [dbg] disk-load MISSING", new InvalidOperationException(fullPath));
            return false;
        }
        try
        {
            var xaml = System.IO.File.ReadAllText(fullPath);

            // Styles.xaml declares <ResourceDictionary Source="Colors.xaml"/> in its own
            // MergedDictionaries. When loading via XamlReader.Load(string) with no BaseUri,
            // XamlReader cannot resolve that relative URI and falls back to ms-resource://,
            // which always fails in UNPACKAGED builds. Since we already load Colors.xaml
            // BEFORE Styles (see LoadThemeResourcesCore), we can safely strip that
            // inner reference from the Styles.xaml text before parsing.
            var fileName = System.IO.Path.GetFileName(fullPath);
            if (string.Equals(fileName, "Styles.xaml", System.StringComparison.OrdinalIgnoreCase))
            {
                xaml = System.Text.RegularExpressions.Regex.Replace(
                    xaml,
                    @"<ResourceDictionary\.MergedDictionaries>[\s\S]*?</ResourceDictionary\.MergedDictionaries>\s*",
                    string.Empty,
                    System.Text.RegularExpressions.RegexOptions.Multiline);
                LogError("LoadThemeResources [dbg] styles-prep",
                    new InvalidOperationException("Stripped Styles.xaml inner MergedDictionaries before XamlReader.Load"));
            }

            var obj = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
            if (obj is Microsoft.UI.Xaml.ResourceDictionary rd)
            {
                appResources.MergedDictionaries.Insert(0, rd);
                LogError("LoadThemeResources [dbg] disk-load OK", new InvalidOperationException($"{relPath} inserted, Keys={rd.Count}"));
                return true;
            }
            LogError("LoadThemeResources [dbg] disk-load NOT-RD", new InvalidOperationException($"{relPath} -> {obj?.GetType().Name}"));
            return false;
        }
#pragma warning disable CA1031 // Disk-based XAML loading is best-effort — broad catch is intentional here
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogError("LoadThemeResources [dbg] disk-load FAILED " + relPath, ex);
            return false;
        }
    }

    private static void LoadThemeResourcesCore(Microsoft.UI.Xaml.ResourceDictionary appResources)
    {
        #region debug-point theme-core-start
        try
        {
            var merged = appResources.MergedDictionaries;
            var state = $"Core BaseDir={AppContext.BaseDirectory}, MergedDictCount={merged.Count}";
            for (var i = 0; i < merged.Count; i++)
            {
                var src = merged[i].Source?.ToString() ?? "(null)";
                var dk = merged[i].Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                state += $" | MD[{i}] Type={merged[i].GetType().Name} Source={src} Keys={dk}";
            }
            LogError("LoadThemeResources [dbg] core-state", new InvalidOperationException(state));
            LogError("LoadThemeResources [dbg] core-keyprobe-PRE",
                new InvalidOperationException($"PrimaryAccentBrush resolves = {ProbeThemeLoaded(appResources)}"));
        }
#pragma warning disable CA1031 // Debug logging must never throw — broad catch is intentional here
        catch (Exception preEx) { LogError("LoadThemeResources [dbg] pre-state probe FAILED", preEx); }
#pragma warning restore CA1031
        #endregion

        // Step 0: Always ensure XamlControlsResources (default WinUI styles for Button,
        // TextBlock, etc.) is present. App.xaml merges it but in Release publish builds,
        // App.xaml's entire MergedDictionaries can be 0 entries (this.Resources hydration
        // failure / ms-appx URI resolution failure). Without this, every standard control
        // has no ControlTemplate -> XamlParseException at any page parse.
        // NOTE: In unpackaged builds, XamlControlsResources may fail to load from ms-appx:///
        // URIs. This is acceptable as long as our custom theme provides the necessary styles.
        var needXamlControls = true;
        var md0 = appResources.MergedDictionaries;
        for (var i = 0; i < md0.Count; i++)
        {
            if (md0[i] is Microsoft.UI.Xaml.Controls.XamlControlsResources)
            {
                needXamlControls = false;
                break;
            }
        }
        if (needXamlControls)
        {
            try
            {
                md0.Insert(0, new Microsoft.UI.Xaml.Controls.XamlControlsResources());
                LogError("LoadThemeResources [dbg] xaml-controls",
                    new InvalidOperationException("Added XamlControlsResources explicitly via code"));
            }
#pragma warning disable CA1031 // XamlControlsResources loading is best-effort — broad catch is intentional here
            catch (Exception xcrEx)
            {
                LogError("LoadThemeResources [dbg] xaml-controls FAILED - will rely on custom theme", xcrEx);
                // Continue anyway - our custom theme should provide necessary styles
            }
#pragma warning restore CA1031
        }

        // KEY-BASED hasTheme detection: only skip if PrimaryAccentBrush + MutedTextBrush resolve.
        if (ProbeThemeLoaded(appResources))
        {
            LogError("LoadThemeResources [dbg] SKIP — key-probe already TRUE", new InvalidOperationException("no-op"));
            return;
        }

        var baseDir = AppContext.BaseDirectory;

        // Always try disk-load of Colors FIRST (dependency order: Styles depends on Colors).
        var colorsOk = MergeXamlFileFromDisk(appResources, baseDir, System.IO.Path.Combine("Theme", "Colors.xaml"));

        // Then Styles.
        var stylesOk = MergeXamlFileFromDisk(appResources, baseDir, System.IO.Path.Combine("Theme", "Styles.xaml"));

        // If Styles loaded successfully, re-merge its Colors dependency once more because
        // compiled merged dictionaries can load out of order.
        if (stylesOk && !colorsOk)
        {
            MergeXamlFileFromDisk(appResources, baseDir, System.IO.Path.Combine("Theme", "Colors.xaml"));
        }

        // Final probe
        var finalProbe = ProbeThemeLoaded(appResources);
        LogError("LoadThemeResources [dbg] core-keyprobe-POST",
            new InvalidOperationException($"PrimaryAccentBrush resolves = {finalProbe}. ColorsOk={colorsOk}, StylesOk={stylesOk}. Final MD Count={appResources.MergedDictionaries.Count}"));
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
            // CRITICAL: Load (or retry loading) theme resources BEFORE MainWindow construction.
            // In Release publish builds, constructor-time this.Resources can throw COMException;
            // by OnLaunched time, it is always hydrated. This MUST run before Frame.Navigate
            // constructs MainPage, because MainPage.InitializeComponent() resolves every
            // StaticResource brush and failing to resolve throws XamlParseException.
            try
            {
                LoadThemeResources();
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                LogError("OnLaunched.LoadThemeResources", ex);
            }
            catch (Microsoft.UI.Xaml.Markup.XamlParseException ex)
            {
                LogError("OnLaunched.LoadThemeResources", ex);
            }
            catch (System.IO.IOException ex)
            {
                LogError("OnLaunched.LoadThemeResources", ex);
            }

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


