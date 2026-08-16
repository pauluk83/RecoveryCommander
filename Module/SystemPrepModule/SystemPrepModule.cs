using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Linq;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using RecoveryCommander.Core.Services;
using SystemPrepModule;

namespace RecoveryCommander.Modules
{
    [RecoveryModule("SystemPrepModule")]
    public class SystemPrepModule : IRecoveryModule
    {
        public string Name => "System Prep";
        public string Description => "Performs various system preparation and cleanup tasks using modular services.";
        public string BuildInfo => "System Prep Module v1.2.0 - Selective updates with popup selection.";

        private readonly List<ModuleAction> _actions;
        public IEnumerable<ModuleAction> Actions => _actions;

        public SystemPrepModule()
        {
            _actions = new List<ModuleAction>
            {
                new ModuleAction("Full System Prep", "Run all maintenance tasks sequentially", ExecuteFullPrepAsync) { Highlight = true },
                new ModuleAction("Upgrade Winget Packages", "Updates programs via Winget (Selective)")
                {
                    ExecuteActionExtended = ExecuteWingetUpdatesSelectiveAsync
                },
                new ModuleAction("Update Store Apps", "Updates Microsoft Store packages (Selective)")
                {
                    ExecuteActionExtended = ExecuteStoreUpdatesSelectiveAsync
                },
                new ModuleAction("Update PS Modules", "Updates PowerShell Modules (Selective)")
                {
                    ExecuteActionExtended = ExecutePSUpdatesSelectiveAsync
                },
                new ModuleAction("Scan for Windows Updates", "Check and install OS updates (Selective)")
                {
                    ExecuteActionExtended = ExecuteWindowsUpdatesSelectiveAsync
                },
                new ModuleAction("Clear All Caches", "Removes browser caches and temp files", ExecuteClearCachesAsync) { IsDestructive = true },
                new ModuleAction("Deep Clean WinSxS", "Component store cleanup (resetbase)", CleanupService.DeepCleanWinSxSAsync) { IsDestructive = true },
                new ModuleAction("Apply Privacy Tweaks", "Disable telemetry and web search in Start", ExecuteApplyTweaksAsync) { IsDestructive = true },
                new ModuleAction("Run Disk Cleanup", "Standard cleanmgr /sagerun:65535", CleanupService.RunDiskCleanupAsync) { IsDestructive = true }
            };
        }

        public string Version => GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0";
        public string HealthStatus => "Healthy";
        public bool SupportsAsync => true;

        private async Task ExecuteWingetUpdatesSelectiveAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(10, "Scanning for Winget updates..."));
            var updates = await UpdateHelpers.GetWingetUpgradesAsync(reportOutput, cancellationToken);

            if (updates.Count == 0)
            {
                reportOutput("No Winget updates found.");
                progress.Report(new ProgressReport(100, "Scan complete - No updates."));
                return;
            }

            var selected = PromptUser(updates, "Select Winget Updates", u => new object[] { u.Name, u.InstalledVersion, u.AvailableVersion, u.Size }, u => u.Size, dialogService);
            if (selected == null || !selected.Any()) return;

            int count = selected.Count();
            int i = 0;
            foreach (var item in selected)
            {
                i++;
                var pct = (int)((double)i / count * 100);
                progress.Report(new ProgressReport(pct, $"Updating {item.Name} ({i}/{count})..."));
                reportOutput($">>> Installing {item.Name}...");
                await RecoveryCommander.Core.Services.UpdateService.UpgradeWingetPackageAsync(item.Id, reportOutput, cancellationToken);
            }
            progress.Report(new ProgressReport(100, "Winget updates completed."));
        }

        private async Task ExecuteStoreUpdatesSelectiveAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(10, "Scanning for MS Store updates..."));
            var updates = await UpdateHelpers.GetStoreUpdatesAsync(reportOutput, cancellationToken);

            if (updates.Count == 0)
            {
                reportOutput("No MS Store updates found.");
                progress.Report(new ProgressReport(100, "Scan complete - No updates."));
                return;
            }

            var selected = PromptUser(updates, "Select Microsoft Store Updates", u => new object[] { u.Name, u.InstalledVersion, u.AvailableVersion, u.Size }, u => u.Size, dialogService);
            if (selected == null || !selected.Any()) return;

            int count = selected.Count();
            int i = 0;
            foreach (var item in selected)
            {
                i++;
                var pct = (int)((double)i / count * 100);
                progress.Report(new ProgressReport(pct, $"Updating {item.Name} ({i}/{count})..."));
                await RecoveryCommander.Core.Services.UpdateService.UpdateStoreAppAsync(item.Id, reportOutput, cancellationToken);
            }
            progress.Report(new ProgressReport(100, "Store updates completed."));
        }

        private async Task ExecutePSUpdatesSelectiveAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(10, "Scanning for PowerShell updates..."));
            var updates = await UpdateHelpers.GetPSModuleUpdatesAsync(reportOutput, cancellationToken);

            if (updates.Count == 0)
            {
                reportOutput("No PowerShell module updates found.");
                return;
            }

            var selected = PromptUser(updates, "Select PS Module Updates", u => new object[] { u.Name, u.InstalledVersion, u.AvailableVersion, u.Size }, u => u.Size, dialogService);
            if (selected == null || !selected.Any()) return;

            int count = selected.Count();
            int i = 0;
            foreach (var item in selected)
            {
                i++;
                var pct = (int)((double)i / count * 100);
                progress.Report(new ProgressReport(pct, $"Updating {item.Name} ({i}/{count})..."));
                await RecoveryCommander.Core.Services.UpdateService.UpdatePSModuleAsync(item.Name, reportOutput, cancellationToken);
            }
            progress.Report(new ProgressReport(100, "PS updates completed."));
        }

        private async Task ExecuteWindowsUpdatesSelectiveAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(10, "Scanning for Windows updates..."));
            var updates = await UpdateHelpers.GetWindowsUpdatesAsync(reportOutput, cancellationToken);

            if (updates.Count == 0)
            {
                dialogService.ShowContentDialog("Your Windows OS is up to date!", "Windows Update");
                reportOutput("No Windows updates found.");
                return;
            }

            var selected = PromptUser(updates, "Select Windows Updates", u => new object[] { u.Title, u.Category, u.KBArticle, u.Size }, u => u.Size, dialogService);
            if (selected == null || !selected.Any()) return;

            progress.Report(new ProgressReport(50, "Installing selected Windows updates..."));
            await UpdateHelpers.InstallWindowsUpdatesAsync(selected, reportOutput, cancellationToken);
            progress.Report(new ProgressReport(100, "Windows updates completed."));
        }

        private static IEnumerable<T> PromptUser<T>(List<T> items, string title, Func<T, object[]> rowData, Func<T, string> sizeFetch, IDialogService dialogService) where T : class
        {
            // Use the dialog service to show item selection dialog
            if (dialogService.ShowItemSelectionDialog(items, title, rowData, sizeFetch, out var selectedItems))
            {
                return selectedItems;
            }
            return Enumerable.Empty<T>();
        }

        private async Task ExecuteFullPrepAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(0, "Full Prep starting..."));
            // Full prep remains unattended to avoid blocking
            await RecoveryCommander.Core.Services.UpdateService.UpgradeWingetPackagesAsync(progress, reportOutput, cancellationToken);
            await RecoveryCommander.Core.Services.UpdateService.UpdateStoreAppsAsync(progress, reportOutput, cancellationToken);
            await RecoveryCommander.Core.Services.CleanupService.ClearTempFilesAsync(progress, reportOutput, cancellationToken);
            await RecoveryCommander.Core.Services.CleanupService.ClearBrowserCachesAsync(progress, reportOutput, cancellationToken);
            progress.Report(new ProgressReport(100, "Full Prep completed successfully."));
        }

        private async Task ExecuteClearCachesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            await RecoveryCommander.Core.Services.CleanupService.ClearTempFilesAsync(progress, reportOutput, cancellationToken);
            await RecoveryCommander.Core.Services.CleanupService.ClearBrowserCachesAsync(progress, reportOutput, cancellationToken);
        }

        private async Task ExecuteApplyTweaksAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            await RecoveryCommander.Core.Services.SystemTweakService.DisableTelemetryAsync(progress, reportOutput, cancellationToken);
            await RecoveryCommander.Core.Services.SystemTweakService.DisableWebSearchAsync(progress, reportOutput, cancellationToken);
            progress.Report(new ProgressReport(100, "Privacy tweaks applied."));
        }
    }
}
