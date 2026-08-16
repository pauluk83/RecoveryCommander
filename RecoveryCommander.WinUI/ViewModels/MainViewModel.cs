using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using RecoveryCommander.Core.Services;
using RecoveryCommanderWinUI.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;

namespace RecoveryCommanderWinUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    public ObservableCollection<ModuleViewModel> Modules { get; } = new();
    private readonly DispatcherQueueTimer _statusTimer;
    private CpuSnapshot? _lastCpuSnapshot;
    private CancellationTokenSource? _operationCancellation;

    [ObservableProperty]
    public partial ModuleViewModel? SelectedModule { get; set; }

    [ObservableProperty]
    public partial string TerminalOutput { get; set; } = "System Ready...\n";

    [ObservableProperty]
    public partial double OperationProgress { get; set; }

    [ObservableProperty]
    public partial bool IsProgressIndeterminate { get; set; }

    [ObservableProperty]
    public partial bool IsOperationRunning { get; set; }

    [ObservableProperty]
    public partial string OperationStatus { get; set; } = "Idle";

    [ObservableProperty]
    public partial string CpuStatus { get; set; } = "CPU: Reading...";

    [ObservableProperty]
    public partial string RamStatus { get; set; } = "RAM: Reading...";

    [ObservableProperty]
    public partial string NetworkStatus { get; set; } = "Network: Reading...";

    [ObservableProperty]
    public partial string OsStatus { get; set; } = $"OS: {Environment.OSVersion.VersionString}";

    [ObservableProperty]
    public partial string AdminStatus { get; set; } = "Admin: Checking...";

    public MainViewModel()
    {
        LoadModules();
        RefreshSystemStatus();

        _statusTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _statusTimer.Interval = TimeSpan.FromSeconds(2);
        _statusTimer.Tick += (_, _) => RefreshSystemStatus();
        _statusTimer.Start();
    }

    private void LoadModules()
    {
        string basePath = System.AppContext.BaseDirectory;
        var dlls = System.IO.Directory.GetFiles(basePath, "*Module.dll");
        
        foreach (var dll in dlls)
        {
            try
            {
                Assembly.LoadFrom(dll);
            }
            catch (System.BadImageFormatException)
            {
                TerminalOutput += $"[ERROR] Failed to load assembly {System.IO.Path.GetFileName(dll)}: Invalid image format\n";
            }
            catch (System.IO.FileLoadException ex)
            {
                TerminalOutput += $"[ERROR] Failed to load assembly {System.IO.Path.GetFileName(dll)}: {ex.Message}\n";
            }
            catch (System.IO.FileNotFoundException)
            {
                TerminalOutput += $"[ERROR] Failed to load assembly {System.IO.Path.GetFileName(dll)}: File not found\n";
            }
        }

        var moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => {
                try { return a.GetTypes(); } catch (ReflectionTypeLoadException) { return Array.Empty<Type>(); }
            })
            .Where(t => typeof(IRecoveryModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in moduleTypes)
        {
            try 
            {
                if (Activator.CreateInstance(type) is IRecoveryModule module)
                {
                    Modules.Add(new ModuleViewModel(module));
                }
            }
            catch (System.MissingMethodException)
            {
                TerminalOutput += $"[ERROR] Failed to instantiate {type.Name}: Missing constructor\n";
            }
            catch (System.TypeLoadException ex)
            {
                TerminalOutput += $"[ERROR] Failed to instantiate {type.Name}: {ex.Message}\n";
            }
            catch (System.InvalidOperationException ex)
            {
                TerminalOutput += $"[ERROR] Failed to instantiate {type.Name}: {ex.Message}\n";
            }
        }

        if (Modules.Count > 0)
            SelectedModule = Modules.First();
    }

    [RelayCommand]
    private async Task RunActionAsync(ModuleAction action)
    {
        if (SelectedModule == null || action == null || IsOperationRunning) return;

        TerminalOutput += $"\n[INFO] Starting {action.Name}...\n";
        OperationProgress = 0;
        OperationStatus = $"Starting {action.DisplayName ?? action.Name}...";
        IsProgressIndeterminate = true;
        IsOperationRunning = true;
        CancelActionCommand.NotifyCanExecuteChanged();
        _operationCancellation = new CancellationTokenSource();
        
        var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        var progress = new Progress<ProgressReport>(report => 
        {
            dispatcher?.TryEnqueue(() =>
            {
                IsProgressIndeterminate = report.IsIndeterminate;
                OperationProgress = Math.Clamp(report.PercentComplete, 0, 100);
                OperationStatus = string.IsNullOrWhiteSpace(report.StatusMessage) ? $"{OperationProgress:0}%" : report.StatusMessage;
                TerminalOutput += $"[{report.PercentComplete}%] {report.StatusMessage}\n";
            });
        });
        
        Action<string> reportOutput = msg => 
        {
            dispatcher?.TryEnqueue(() =>
            {
                TerminalOutput += $"{msg}\n";
            });
        };

        try
        {
            var dialogService = ServiceContainer.GetService<IDialogService>();
            await SelectedModule.Module.ExecuteActionAsync(action.Name, progress, reportOutput, dialogService, _operationCancellation.Token);
            OperationProgress = 100;
            OperationStatus = $"Completed {action.DisplayName ?? action.Name}.";
            TerminalOutput += $"[OK] Completed {action.Name}.\n";
        }
        catch (OperationCanceledException)
        {
            OperationStatus = $"Cancelled {action.DisplayName ?? action.Name}.";
            TerminalOutput += $"[CANCELLED] {action.Name}.\n";
        }
        catch (System.UnauthorizedAccessException ex)
        {
            OperationStatus = $"Failed {action.DisplayName ?? action.Name}.";
            TerminalOutput += $"[ERROR] Failed {action.Name}: Access denied - {ex.Message}\n";
        }
        catch (System.IO.IOException ex)
        {
            OperationStatus = $"Failed {action.DisplayName ?? action.Name}.";
            TerminalOutput += $"[ERROR] Failed {action.Name}: I/O error - {ex.Message}\n";
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            OperationStatus = $"Failed {action.DisplayName ?? action.Name}.";
            TerminalOutput += $"[ERROR] Failed {action.Name}: Win32 error - {ex.Message}\n";
        }
        catch (System.InvalidOperationException ex)
        {
            OperationStatus = $"Failed {action.DisplayName ?? action.Name}.";
            TerminalOutput += $"[ERROR] Failed {action.Name}: {ex.Message}\n";
        }
        catch (System.Security.SecurityException ex)
        {
            // Supply-chain block: unverified catalog download denied.
            OperationStatus = $"Blocked — {action.DisplayName ?? action.Name}.";
            TerminalOutput += $"[SECURITY] {action.Name} was blocked: {ex.Message}\n";
        }
#pragma warning disable CA1031 // Action runner must catch all to prevent UI hang — failures are surfaced to the terminal
        catch (Exception ex)
        {
            OperationStatus = $"Failed {action.DisplayName ?? action.Name}.";
            TerminalOutput += $"[ERROR] Failed {action.Name}: {ex.GetType().Name} \u2014 {ex.Message}\n";
            if (ex.InnerException != null)
                TerminalOutput += $"       Inner: {ex.InnerException.GetType().Name} \u2014 {ex.InnerException.Message}\n";
        }
#pragma warning restore CA1031
        finally
        {
            _operationCancellation?.Dispose();
            _operationCancellation = null;
            IsProgressIndeterminate = false;
            IsOperationRunning = false;
            CancelActionCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(IsOperationRunning))]
    private void CancelAction()
    {
        if (!IsOperationRunning || _operationCancellation == null)
        {
            return;
        }

        OperationStatus = "Cancelling...";
        TerminalOutput += "[INFO] Cancellation requested.\n";
        _operationCancellation.Cancel();
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        TerminalOutput += "\n[INFO] Checking for updates...\n";
        try
        {
            var result = await AutoUpdateService.CheckForUpdateAsync();
            if (result.ErrorMessage != null)
            {
                TerminalOutput += $"[ERROR] {result.ErrorMessage}\n";
                await ShowContentDialogAsync("Update Check Failed", result.ErrorMessage);
            }
            else if (result.UpdateAvailable)
            {
                TerminalOutput += $"[INFO] Update available: {result.LatestVersion}\n";
                TerminalOutput += $"[INFO] Release notes: {result.ReleaseNotes}\n";
                await ShowContentDialogAsync("Update Available", 
                    $"A new version ({result.LatestVersion}) is available!\n\nCurrent: {result.CurrentVersion}\nLatest: {result.LatestVersion}\n\nRelease Notes:\n{result.ReleaseNotes}");
            }
            else
            {
                TerminalOutput += "[INFO] You are running the latest version.\n";
                await ShowContentDialogAsync("Up to Date", "You are running the latest version of RecoveryCommander.");
            }
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            TerminalOutput += $"[ERROR] Network error checking for updates: {ex.Message}\n";
        }
        catch (System.Threading.Tasks.TaskCanceledException ex)
        {
            TerminalOutput += $"[ERROR] Update check timed out: {ex.Message}\n";
        }
        catch (InvalidOperationException ex)
        {
            TerminalOutput += $"[ERROR] Update check failed: {ex.Message}\n";
        }
    }

    [RelayCommand]
    private static async Task AboutAsync()
    {
        var version = AutoUpdateService.GetCurrentVersion();
        var buildDate = CoreUtilities.GetBuildDateUtc();
        var buildDateStr = buildDate != DateTime.MinValue ? buildDate.ToString("yyyy-MM-dd HH:mm:ss UTC", System.Globalization.CultureInfo.InvariantCulture) : "Unknown";
        
        var contentDialog = new ContentDialog
        {
            Title = "About Recovery Commander",
            XamlRoot = App.MainWindow?.Content?.XamlRoot,
            PrimaryButtonText = "Close"
        };

        var rootGrid = new Grid();
        rootGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Microsoft.UI.ColorHelper.FromArgb(3, 3, 9, 20));
        
        var gradientBorder = new Border();
        var gradientBrush = new Microsoft.UI.Xaml.Media.RadialGradientBrush
        {
            Center = new Windows.Foundation.Point(0.5, 0.46),
            GradientOrigin = new Windows.Foundation.Point(0.5, 0.46),
            RadiusX = 0.72,
            RadiusY = 0.72
        };
        gradientBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop
        {
            Color = Microsoft.UI.ColorHelper.FromArgb(53, 24, 207, 255),
            Offset = 0
        });
        gradientBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop
        {
            Color = Microsoft.UI.ColorHelper.FromArgb(16, 0, 229, 255),
            Offset = 0.38
        });
        gradientBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop
        {
            Color = Microsoft.UI.ColorHelper.FromArgb(208, 3, 9, 20),
            Offset = 1
        });
        gradientBorder.Background = gradientBrush;
        rootGrid.Children.Add(gradientBorder);
        
        var contentPanel = new StackPanel
        {
            Spacing = 12,
            Padding = new Thickness(20)
        };
        contentPanel.Children.Add(new TextBlock { Text = $"Version: {version}", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) });
        contentPanel.Children.Add(new TextBlock { Text = $"Build Date: {buildDateStr}", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) });
        contentPanel.Children.Add(new TextBlock { Text = "Author: Zane Stanton", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) });
        contentPanel.Children.Add(new TextBlock { Text = "© 2026 Recovery Commander™", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) });
        contentPanel.Children.Add(new TextBlock { Text = "All rights reserved.", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) });
        contentPanel.Children.Add(new TextBlock { Text = "\nThis project is developed for system recovery and maintenance purposes. Please ensure compliance with Windows licensing terms when using system modification.", TextWrapping = TextWrapping.Wrap, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) });
        contentPanel.Children.Add(new TextBlock { Text = "\nOpen Source & Credits", FontWeight = FontWeights.SemiBold, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) });
        contentPanel.Children.Add(new TextBlock { Text = "Proudly open source. We express our gratitude to the third-party developers whose software is integrated here; all respective copyrights belong to their original owners.", TextWrapping = TextWrapping.Wrap, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) });
        contentPanel.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 20,
            Children =
            {
                new HyperlinkButton
                {
                    NavigateUri = new Uri("https://github.com/pauluk83/RecoveryCommander"),
                    Content = new Image
                    {
                        Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/github.png")),
                        Width = 32,
                        Height = 32,
                        Stretch = Stretch.Uniform
                    }
                },
                new HyperlinkButton
                {
                    NavigateUri = new Uri("https://recoverycommander.free.nf/?i=2"),
                    Content = new Image
                    {
                        Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/website.jpg")),
                        Width = 32,
                        Height = 32,
                        Stretch = Stretch.Uniform
                    }
                }
            }
        });
        
        rootGrid.Children.Add(contentPanel);
        contentDialog.Content = rootGrid;

        await contentDialog.ShowAsync();
    }

    [RelayCommand]
    private static async Task SettingsAsync()
    {
        var window = new SettingsWindow();
        await window.ShowAsync();
    }

    [RelayCommand]
    private static async Task RestorePointManagerAsync()
    {
        var dialog = new RestorePointManagerWindow();
        await dialog.ShowAsync();
    }

    [RelayCommand]
    private static async Task StartupManagerAsync()
    {
        var dialog = new StartupManagerWindow();
        await dialog.ShowAsync();
    }

    [RelayCommand]
    private static async Task NetworkRepairAsync()
    {
        var dialog = new NetworkRepairWindow();
        await dialog.ShowAsync();
    }

    [RelayCommand]
    private static async Task MediaToolsAsync()
    {
        var contentDialog = new ContentDialog
        {
            Title = "Media Tools",
            XamlRoot = App.MainWindow?.Content?.XamlRoot,
            PrimaryButtonText = "Close"
        };

        var rootGrid = new Grid();
        rootGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Microsoft.UI.ColorHelper.FromArgb(3, 3, 9, 20));
        
        var gradientBorder = new Border();
        var gradientBrush = new Microsoft.UI.Xaml.Media.RadialGradientBrush
        {
            Center = new Windows.Foundation.Point(0.5, 0.46),
            GradientOrigin = new Windows.Foundation.Point(0.5, 0.46),
            RadiusX = 0.72,
            RadiusY = 0.72
        };
        gradientBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop
        {
            Color = Microsoft.UI.ColorHelper.FromArgb(53, 24, 207, 255),
            Offset = 0
        });
        gradientBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop
        {
            Color = Microsoft.UI.ColorHelper.FromArgb(16, 0, 229, 255),
            Offset = 0.38
        });
        gradientBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop
        {
            Color = Microsoft.UI.ColorHelper.FromArgb(208, 3, 9, 20),
            Offset = 1
        });
        gradientBorder.Background = gradientBrush;
        rootGrid.Children.Add(gradientBorder);
        
        var stackPanel = new StackPanel { Spacing = 12, Padding = new Thickness(20) };

        // Boot Media Creator Section
        var bootMediaPanel = new StackPanel { Spacing = 8 };
        bootMediaPanel.Children.Add(new TextBlock 
        { 
            Text = "Boot Media Creator", 
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
        });
        bootMediaPanel.Children.Add(new TextBlock 
        { 
            Text = "Create a bootable USB recovery drive with RecoveryCommander and optional Windows Recovery Environment.", 
            TextWrapping = TextWrapping.Wrap,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) 
        });

        var bootMediaButton = new Button 
        { 
            Content = "Create Boot Media",
            HorizontalAlignment = HorizontalAlignment.Stretch 
        };
        bootMediaButton.Click += async (s, e) =>
        {
            await ShowBootMediaCreatorDialogAsync();
            contentDialog.Hide();
        };
        bootMediaPanel.Children.Add(bootMediaButton);

        // Media Creation Tools Section
        var mctPanel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 16, 0, 0) };
        mctPanel.Children.Add(new TextBlock 
        { 
            Text = "Media Creation Tools", 
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
        });
        mctPanel.Children.Add(new TextBlock 
        { 
            Text = "Download official Microsoft Media Creation Tools for Windows 10 and Windows 11.", 
            TextWrapping = TextWrapping.Wrap,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) 
        });

        var win10Button = new Button 
        { 
            Content = "Download Windows 10 MCT",
            HorizontalAlignment = HorizontalAlignment.Stretch 
        };
        win10Button.Click += (s, e) =>
        {
            RecoveryCommander.Features.MediaTools.DownloadWindows10Mct();
        };
        mctPanel.Children.Add(win10Button);

        var win11Button = new Button 
        { 
            Content = "Download Windows 11 MCT",
            HorizontalAlignment = HorizontalAlignment.Stretch 
        };
        win11Button.Click += (s, e) =>
        {
            RecoveryCommander.Features.MediaTools.DownloadWindows11Mct();
        };
        mctPanel.Children.Add(win11Button);

        stackPanel.Children.Add(bootMediaPanel);
        stackPanel.Children.Add(mctPanel);
        
        rootGrid.Children.Add(stackPanel);
        contentDialog.Content = rootGrid;

        await contentDialog.ShowAsync();
    }

    private static async Task ShowBootMediaCreatorDialogAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Boot Media Creator",
            XamlRoot = App.MainWindow?.Content?.XamlRoot,
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel"
        };

        var stackPanel = new StackPanel { Spacing = 12 };

        // Drive selection
        var drivePanel = new StackPanel { Spacing = 4 };
        drivePanel.Children.Add(new TextBlock { Text = "Select USB Drive:" });
        
        var drives = await RecoveryCommander.Features.BootMediaCreator.GetRemovableDrivesAsync();
        var driveComboBox = new ComboBox { PlaceholderText = "No removable drives found" };
        
        foreach (var drive in drives)
        {
            var freeGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
            var totalGB = drive.TotalSize / 1024 / 1024 / 1024;
            driveComboBox.Items.Add($"{drive.Name} ({freeGB} GB free / {totalGB} GB total)");
        }
        
        if (drives.Count > 0)
        {
            driveComboBox.SelectedIndex = 0;
        }
        
        drivePanel.Children.Add(driveComboBox);
        stackPanel.Children.Add(drivePanel);

        // Options
        var copyAppCheckBox = new CheckBox { Content = "Copy RecoveryCommander to drive", IsChecked = true };
        var includeDriversCheckBox = new CheckBox { Content = "Export & Include Drivers (Recommended)", IsChecked = true };
        
        stackPanel.Children.Add(copyAppCheckBox);
        stackPanel.Children.Add(includeDriversCheckBox);

        dialog.Content = stackPanel;

        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary && drives.Count > 0 && driveComboBox.SelectedIndex >= 0)
        {
            var selectedDrive = drives[driveComboBox.SelectedIndex];
            var progress = new Progress<RecoveryCommander.Contracts.ProgressReport>();
            var output = new Action<string>(msg => { /* Log output */ });
            using var cts = new CancellationTokenSource();
            
            var bootResult = await RecoveryCommander.Features.BootMediaCreator.CreateRecoveryDriveAsync(
                selectedDrive.Name,
                copyAppCheckBox.IsChecked == true,
                false, // useWinRE
                false, // backupRecovery
                includeDriversCheckBox.IsChecked == true,
                progress,
                output,
                cts.Token);
            
            if (bootResult.Success)
            {
                await ShowContentDialogAsync("Success", bootResult.Message);
            }
            else
            {
                await ShowContentDialogAsync("Error", bootResult.Message);
            }
        }
    }

    [RelayCommand]
    private static async Task ModuleBuilderAsync()
    {
        var dialog = new ModuleBuilderDialog
        {
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };
        await dialog.ShowAsync();
    }

    [RelayCommand]
    private async Task ViewReadmeAsync()
    {
        try
        {
            var readmePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "README.md");
            TerminalOutput += $"[INFO] Looking for README at: {readmePath}\n";
            
            if (System.IO.File.Exists(readmePath))
            {
                var readmeContent = await System.IO.File.ReadAllTextAsync(readmePath);
                TerminalOutput += $"[INFO] README loaded, {readmeContent.Length} characters\n";
                
                var window = new DocumentViewerWindow("README", readmeContent);
                if (window.AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.Maximize();
                }
                window.Activate();
                TerminalOutput += "[INFO] Document viewer window opened\n";
            }
            else
            {
                TerminalOutput += "[ERROR] README.md file not found\n";
                await ShowContentDialogAsync("README", "README.md file not found in application directory.");
            }
        }
        catch (System.IO.IOException ex)
        {
            TerminalOutput += $"[ERROR] Failed to view README: {ex.Message}\n";
            await ShowContentDialogAsync("Error", $"Failed to view README: {ex.Message}");
        }
        catch (System.UnauthorizedAccessException ex)
        {
            TerminalOutput += $"[ERROR] Failed to view README: {ex.Message}\n";
            await ShowContentDialogAsync("Error", $"Failed to view README: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ViewChangelogAsync()
    {
        try
        {
            var changelogPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "CHANGELOG.md");
            TerminalOutput += $"[INFO] Looking for CHANGELOG at: {changelogPath}\n";
            
            if (System.IO.File.Exists(changelogPath))
            {
                var changelogContent = await System.IO.File.ReadAllTextAsync(changelogPath);
                TerminalOutput += $"[INFO] CHANGELOG loaded, {changelogContent.Length} characters\n";
                
                var window = new DocumentViewerWindow("Changelog", changelogContent);
                if (window.AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.Maximize();
                }
                window.Activate();
                TerminalOutput += "[INFO] Document viewer window opened\n";
            }
            else
            {
                TerminalOutput += "[ERROR] CHANGELOG.md file not found\n";
                await ShowContentDialogAsync("Changelog", "CHANGELOG.md file not found in application directory. This feature is coming soon.");
            }
        }
        catch (System.IO.IOException ex)
        {
            TerminalOutput += $"[ERROR] Failed to view CHANGELOG: {ex.Message}\n";
            await ShowContentDialogAsync("Error", $"Failed to view CHANGELOG: {ex.Message}");
        }
        catch (System.UnauthorizedAccessException ex)
        {
            TerminalOutput += $"[ERROR] Failed to view CHANGELOG: {ex.Message}\n";
            await ShowContentDialogAsync("Error", $"Failed to view CHANGELOG: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ViewArchitecturalNotesAsync()
    {
        try
        {
            var notesPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "Resources", "ARCHITECTURAL_NOTES.md");
            TerminalOutput += $"[INFO] Looking for ARCHITECTURAL_NOTES at: {notesPath}\n";
            
            if (System.IO.File.Exists(notesPath))
            {
                var notesContent = await System.IO.File.ReadAllTextAsync(notesPath);
                TerminalOutput += $"[INFO] ARCHITECTURAL_NOTES loaded, {notesContent.Length} characters\n";
                
                var window = new DocumentViewerWindow("Architectural & Design Notes", notesContent);
                if (window.AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.Maximize();
                }
                window.Activate();
                TerminalOutput += "[INFO] Document viewer window opened\n";
            }
            else
            {
                TerminalOutput += "[ERROR] ARCHITECTURAL_NOTES.md file not found\n";
                await ShowContentDialogAsync("Architectural & Design Notes", "ARCHITECTURAL_NOTES.md file not found in Resources folder.");
            }
        }
        catch (System.IO.IOException ex)
        {
            TerminalOutput += $"[ERROR] Failed to view ARCHITECTURAL_NOTES: {ex.Message}\n";
            await ShowContentDialogAsync("Error", $"Failed to view ARCHITECTURAL_NOTES: {ex.Message}");
        }
        catch (System.UnauthorizedAccessException ex)
        {
            TerminalOutput += $"[ERROR] Failed to view ARCHITECTURAL_NOTES: {ex.Message}\n";
            await ShowContentDialogAsync("Error", $"Failed to view ARCHITECTURAL_NOTES: {ex.Message}");
        }
    }

    private static async Task ShowContentDialogAsync(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            XamlRoot = App.MainWindow?.Content?.XamlRoot,
            CloseButtonText = "OK"
        };

        var rootGrid = new Grid();
        rootGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Microsoft.UI.ColorHelper.FromArgb(3, 3, 9, 20));
        
        var gradientBorder = new Border();
        var gradientBrush = new Microsoft.UI.Xaml.Media.RadialGradientBrush
        {
            Center = new Windows.Foundation.Point(0.5, 0.46),
            GradientOrigin = new Windows.Foundation.Point(0.5, 0.46),
            RadiusX = 0.72,
            RadiusY = 0.72
        };
        gradientBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop
        {
            Color = Microsoft.UI.ColorHelper.FromArgb(53, 24, 207, 255),
            Offset = 0
        });
        gradientBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop
        {
            Color = Microsoft.UI.ColorHelper.FromArgb(16, 0, 229, 255),
            Offset = 0.38
        });
        gradientBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop
        {
            Color = Microsoft.UI.ColorHelper.FromArgb(208, 3, 9, 20),
            Offset = 1
        });
        gradientBorder.Background = gradientBrush;
        
        var contentPanel = new StackPanel
        {
            Spacing = 12,
            Padding = new Thickness(20)
        };
        contentPanel.Children.Add(new TextBlock 
        { 
            Text = content, 
            TextWrapping = TextWrapping.Wrap,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) 
        });
        
        rootGrid.Children.Add(contentPanel);
        dialog.Content = rootGrid;

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Executes a user-typed PowerShell command from the interactive terminal input.
    /// Streams the output back to TerminalOutput in real-time.
    /// </summary>
    public async Task ExecuteTerminalCommandAsync(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;

        TerminalOutput += $"\nPS> {command}\n";

        var dispatcher = DispatcherQueue.GetForCurrentThread();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        try
        {
            await Task.Run(async () =>
            {
                await RecoveryCommander.Core.AsyncHelpers.ExecutePowerShellCommandAsync(
                    command,
                    line =>
                    {
                        dispatcher?.TryEnqueue(() =>
                        {
                            TerminalOutput += $"{line}\n";
                        });
                    },
                    cts.Token
                ).ConfigureAwait(false);
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            TerminalOutput += "[PS] Command timed out after 5 minutes.\n";
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            TerminalOutput += $"[PS ERROR] Win32 error: {ex.Message}\n";
        }
        catch (System.UnauthorizedAccessException ex)
        {
            TerminalOutput += $"[PS ERROR] Access denied: {ex.Message}\n";
        }
        catch (System.IO.IOException ex)
        {
            TerminalOutput += $"[PS ERROR] I/O error: {ex.Message}\n";
        }
    }

    private void RefreshSystemStatus()
    {
        CpuStatus = $"CPU: {GetCpuUsage():0}% ({Environment.ProcessorCount} logical processors)";
        RamStatus = GetMemoryStatus();
        NetworkStatus = GetNetworkStatus();
        AdminStatus = IsRunningAsAdministrator() ? "Admin: Elevated" : "Admin: Not elevated";
    }

    private double GetCpuUsage()
    {
        var current = CpuSnapshot.Read();
        if (_lastCpuSnapshot == null)
        {
            _lastCpuSnapshot = current;
            return 0;
        }

        var previous = _lastCpuSnapshot.Value;
        _lastCpuSnapshot = current;

        var idle = current.Idle - previous.Idle;
        var total = current.Total - previous.Total;
        return total <= 0 ? 0 : Math.Clamp((1.0 - idle / (double)total) * 100.0, 0, 100);
    }

    private static string GetMemoryStatus()
    {
        var status = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(status))
        {
            return "RAM: Unavailable";
        }

        var used = status.TotalPhys - status.AvailPhys;
        return $"RAM: {FormatBytes(used)} / {FormatBytes(status.TotalPhys)} ({status.MemoryLoad}%)";
    }

    private static string GetNetworkStatus()
    {
        var active = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .OrderByDescending(n => n.Speed)
            .FirstOrDefault();

        return active == null
            ? "Network: Disconnected"
            : $"Network: Connected ({active.Name})";
    }

    private static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static string FormatBytes(ulong bytes)
    {
        return $"{bytes / 1024d / 1024d / 1024d:0.0} GB";
    }

    public void Dispose()
    {
        _statusTimer.Stop();
        _operationCancellation?.Dispose();
    }

    private readonly record struct CpuSnapshot(ulong Idle, ulong Total)
    {
        public static CpuSnapshot Read()
        {
            if (!GetSystemTimes(out var idle, out var kernel, out var user))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return new CpuSnapshot(FileTimeToUInt64(idle), FileTimeToUInt64(kernel) + FileTimeToUInt64(user));
        }

        private static ulong FileTimeToUInt64(FILETIME fileTime)
        {
            return ((ulong)fileTime.dwHighDateTime << 32) | fileTime.dwLowDateTime;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private sealed class MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }
}


