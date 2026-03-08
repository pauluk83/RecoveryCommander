using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Versioning;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using RecoveryCommander.Core.Services;

namespace RecoveryCommander.Module
{
    [RecoveryModule("SystemPrepModule", "1.1.0")]
    [SupportedOSPlatform("windows")]
    public class SystemPrepModule : IRecoveryModule
    {
        private readonly UpdateService _updateService = new();
        private readonly CleanupService _cleanupService = new();
        private readonly SystemTweakService _tweakService = new();
        private readonly DriverService _driverService = new();

        public string Name => "System Prep";
        public string Description => "Performs various system preparation and cleanup tasks using modular services.";
        public string BuildInfo => "System Prep Module v1.1.0 - Modernized with individual services.";

        public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
        {
            new ModuleAction("Full System Prep", "Run all maintenance tasks sequentially", ExecuteFullPrepAsync) { Highlight = true },
            new ModuleAction("Upgrade Winget Packages", "Updates programs via Winget", _updateService.UpgradeWingetPackagesAsync),
            new ModuleAction("Update Store Apps", "Updates Microsoft Store packages", _updateService.UpdateStoreAppsAsync),
            new ModuleAction("Scan for Windows Updates", "Check for OS updates", _updateService.ScanForWindowsUpdatesAsync),
            new ModuleAction("Backup Drivers", "Exports third-party drivers to a folder", ExecuteBackupDriversAsync),
            new ModuleAction("Clear All Caches", "Removes browser caches and temp files", ExecuteClearCachesAsync) { IsDestructive = true },
            new ModuleAction("Deep Clean WinSxS", "Component store cleanup (resetbase)", _cleanupService.DeepCleanWinSxSAsync) { IsDestructive = true },
            new ModuleAction("Apply Privacy Tweaks", "Disable telemetry and web search in Start", ExecuteApplyTweaksAsync) { IsDestructive = true },
            new ModuleAction("Run Disk Cleanup", "Standard cleanmgr /sagerun:65535", _cleanupService.RunDiskCleanupAsync) { IsDestructive = true }
        };

        public string Version => "1.1.0";
        public string HealthStatus => "Healthy";
        public bool SupportsAsync => true;

        private async Task ExecuteFullPrepAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(0, "Full Prep starting..."));
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

        private async Task ExecuteBackupDriversAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            using var fbd = new FolderBrowserDialog { Description = "Select driver backup destination" };
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                await _driverService.BackupDriversAsync(fbd.SelectedPath, progress, reportOutput, cancellationToken);
            }
        }

        private async Task ExecuteApplyTweaksAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            await _tweakService.DisableTelemetryAsync(progress, reportOutput);
            await _tweakService.DisableWebSearchAsync(progress, reportOutput);
            progress.Report(new ProgressReport(100, "Privacy tweaks applied."));
        }
    }
}
