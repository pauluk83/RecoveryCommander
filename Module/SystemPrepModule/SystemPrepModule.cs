using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Versioning;
using System.Linq;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using RecoveryCommander.Core.Services;
using SystemPrepModule;

namespace RecoveryCommander.Module
{
    [RecoveryModule("SystemPrepModule")]
    public class SystemPrepModule : IRecoveryModule
    {
        private readonly UpdateService _updateService = new();
        private readonly CleanupService _cleanupService = new();
        private readonly SystemTweakService _tweakService = new();
        
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
                new ModuleAction("Upgrade Winget Packages", "Updates programs via Winget (Selective)", ExecuteWingetUpdatesSelectiveAsync),
                new ModuleAction("Update Store Apps", "Updates Microsoft Store packages (Selective)", ExecuteStoreUpdatesSelectiveAsync),
                new ModuleAction("Update PS Modules", "Updates PowerShell Modules (Selective)", ExecutePSUpdatesSelectiveAsync),
                new ModuleAction("Scan for Windows Updates", "Check and install OS updates (Selective)", ExecuteWindowsUpdatesSelectiveAsync),
                new ModuleAction("Clear All Caches", "Removes browser caches and temp files", ExecuteClearCachesAsync) { IsDestructive = true },
                new ModuleAction("Deep Clean WinSxS", "Component store cleanup (resetbase)", _cleanupService.DeepCleanWinSxSAsync) { IsDestructive = true },
                new ModuleAction("Apply Privacy Tweaks", "Disable telemetry and web search in Start", ExecuteApplyTweaksAsync) { IsDestructive = true },
                new ModuleAction("Run Disk Cleanup", "Standard cleanmgr /sagerun:65535", _cleanupService.RunDiskCleanupAsync) { IsDestructive = true }
            };
        }

        public string Version => GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0";
        public string HealthStatus => "Healthy";
        public bool SupportsAsync => true;

        private async Task ExecuteWingetUpdatesSelectiveAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(10, "Scanning for Winget updates..."));
            var updates = await UpdateHelpers.GetWingetUpgradesAsync(reportOutput, cancellationToken);
            
            if (updates.Count == 0)
            {
                MessageBox.Show("All packages are up to date!", "Winget Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                reportOutput("No Winget updates found.");
                progress.Report(new ProgressReport(100, "Scan complete - No updates."));
                return;
            }

            var selected = PromptUser(updates, "Select Winget Updates", u => new object[] { u.Name, u.InstalledVersion, u.AvailableVersion, u.Size }, u => u.Size);
            if (selected == null || !selected.Any()) return;

            int count = selected.Count();
            int i = 0;
            foreach (var item in selected)
            {
                i++;
                var pct = (int)((double)i / count * 100);
                progress.Report(new ProgressReport(pct, $"Updating {item.Name} ({i}/{count})..."));
                reportOutput($">>> Installing {item.Name}...");
                await _updateService.UpgradeWingetPackageAsync(item.Id, reportOutput, cancellationToken);
            }
            progress.Report(new ProgressReport(100, "Winget updates completed."));
        }

        private async Task ExecuteStoreUpdatesSelectiveAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(10, "Scanning for MS Store updates..."));
            var updates = await UpdateHelpers.GetStoreUpdatesAsync(reportOutput, cancellationToken);

            if (updates.Count == 0)
            {
                MessageBox.Show("All Microsoft Store apps are up to date!", "Store Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                reportOutput("No MS Store updates found.");
                progress.Report(new ProgressReport(100, "Scan complete - No updates."));
                return;
            }

            var selected = PromptUser(updates, "Select Microsoft Store Updates", u => new object[] { u.Name, u.InstalledVersion, u.AvailableVersion, u.Size }, u => u.Size);
            if (selected == null || !selected.Any()) return;

            int count = selected.Count();
            int i = 0;
            foreach (var item in selected)
            {
                i++;
                var pct = (int)((double)i / count * 100);
                progress.Report(new ProgressReport(pct, $"Updating {item.Name} ({i}/{count})..."));
                await _updateService.UpdateStoreAppAsync(item.Id, reportOutput, cancellationToken);
            }
            progress.Report(new ProgressReport(100, "Store updates completed."));
        }

        private async Task ExecutePSUpdatesSelectiveAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(10, "Scanning for PowerShell updates..."));
            var updates = await UpdateHelpers.GetPSModuleUpdatesAsync(reportOutput, cancellationToken);

            if (updates.Count == 0)
            {
                MessageBox.Show("All PowerShell modules are up to date!", "PowerShell Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                reportOutput("No PowerShell module updates found.");
                return;
            }

            var selected = PromptUser(updates, "Select PS Module Updates", u => new object[] { u.Name, u.InstalledVersion, u.AvailableVersion, u.Size }, u => u.Size);
            if (selected == null || !selected.Any()) return;

            int count = selected.Count();
            int i = 0;
            foreach (var item in selected)
            {
                i++;
                var pct = (int)((double)i / count * 100);
                progress.Report(new ProgressReport(pct, $"Updating {item.Name} ({i}/{count})..."));
                await _updateService.UpdatePSModuleAsync(item.Name, reportOutput, cancellationToken);
            }
            progress.Report(new ProgressReport(100, "PS updates completed."));
        }

        private async Task ExecuteWindowsUpdatesSelectiveAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(10, "Scanning for Windows updates..."));
            var updates = await UpdateHelpers.GetWindowsUpdatesAsync(reportOutput, cancellationToken);

            if (updates.Count == 0)
            {
                MessageBox.Show("Your Windows OS is up to date!", "Windows Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                reportOutput("No Windows updates found.");
                return;
            }

            var selected = PromptUser(updates, "Select Windows Updates", u => new object[] { u.Title, u.Category, u.KBArticle, u.Size }, u => u.Size);
            if (selected == null || !selected.Any()) return;

            progress.Report(new ProgressReport(50, "Installing selected Windows updates..."));
            await UpdateHelpers.InstallWindowsUpdatesAsync(selected, reportOutput, cancellationToken);
            progress.Report(new ProgressReport(100, "Windows updates completed."));
        }

        private IEnumerable<T> PromptUser<T>(List<T> items, string title, Func<T, object[]> rowData, Func<T, string> sizeFetch) where T : class
        {
            IEnumerable<T>? selected = null;
            // Need to run the UI on the UI thread
            if (Application.OpenForms.Count > 0)
            {
                Application.OpenForms[0]?.Invoke(new Action(() =>
                {
                    var cols = new List<DataGridViewColumn>();
                    // Auto-generate columns based on rowData count (simplified)
                    cols.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", FillWeight = 50 });
                    cols.Add(new DataGridViewTextBoxColumn { HeaderText = "Info", FillWeight = 30 });
                    cols.Add(new DataGridViewTextBoxColumn { HeaderText = "Version", FillWeight = 20 });
                    cols.Add(new DataGridViewTextBoxColumn { HeaderText = "Size", FillWeight = 15 });

                    using (var form = new UpdateSelectorForm<T>(title, items, cols, rowData, sizeFetch))
                    {
                        if (form.ShowDialog() == DialogResult.OK)
                        {
                            selected = form.SelectedItems.ToList();
                        }
                    }
                }));
            }
            return selected ?? Enumerable.Empty<T>();
        }

        private async Task ExecuteFullPrepAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(0, "Full Prep starting..."));
            // Full prep remains unattended to avoid blocking
            await _updateService.UpgradeWingetPackagesAsync(progress, reportOutput, cancellationToken);
            await _updateService.UpdateStoreAppsAsync(progress, reportOutput, cancellationToken);
            await _cleanupService.ClearTempFilesAsync(progress, reportOutput, cancellationToken);
            await _cleanupService.ClearBrowserCachesAsync(progress, reportOutput, cancellationToken);
            progress.Report(new ProgressReport(100, "Full Prep completed successfully."));
        }

        private async Task ExecuteClearCachesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            await _cleanupService.ClearTempFilesAsync(progress, reportOutput, cancellationToken);
            await _cleanupService.ClearBrowserCachesAsync(progress, reportOutput, cancellationToken);
        }

        private async Task ExecuteApplyTweaksAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            await _tweakService.DisableTelemetryAsync(progress, reportOutput, cancellationToken);
            await _tweakService.DisableWebSearchAsync(progress, reportOutput, cancellationToken);
            progress.Report(new ProgressReport(100, "Privacy tweaks applied."));
        }
    }
}
