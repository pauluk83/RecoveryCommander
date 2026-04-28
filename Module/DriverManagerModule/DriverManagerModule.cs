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
    [RecoveryModule("DriverManagerModule")]
    [SupportedOSPlatform("windows")]
    public class DriverManagerModule : IRecoveryModule
    {
        private readonly DriverService _driverService = new();

        private static class DownloadUrls
        {
            public const string IObitDriverBooster = "https://www.dropbox.com/scl/fi/2paq4t1yevyprkw5jrp0a/DriverBoosterPortable.txt?rlkey=p6j6ofauo26tp5xnunqhc3pxb&st=bvyegici&raw=1";
        }

        public string Name => "Driver Manager";
        public string Description => "Comprehensive driver backup, restoration, and optimization tools.";
        public string BuildInfo => "Driver Manager v1.0.0 - Focused driver management.";
        public string Version => GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0";
        public string HealthStatus => "Healthy";
        public bool SupportsAsync => true;

        public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
        {
            new ModuleAction("Backup Drivers", "Exports all third-party drivers to a selected folder", ExecuteBackupDriversAsync) { Highlight = true },
            new ModuleAction("Restore Drivers", "Installs drivers from a folder (pnputil)", ExecuteRestoreDriversAsync),
            new ModuleAction("List Drivers", "Enumerate all installed third-party drivers", _driverService.OptimizeDriverStoreAsync),
            new ModuleAction("Cleanup Driver Store", "Removes redundant and old driver versions", ExecuteOptimizeDriversAsync) { IsDestructive = true },
            new ModuleAction("Driver Booster PRO 13.4.0.234", "Driver Booster PRO Portable", (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.IObitDriverBooster, "Driver Booster PRO 13.4.0.234.exe", p, o, c))
        };

        private async Task ExecuteBackupDriversAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            using var fbd = new FolderBrowserDialog 
            { 
                Description = "Select destination folder for driver backup",
                UseDescriptionForTitle = true
            };

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                await _driverService.BackupDriversAsync(fbd.SelectedPath, progress, reportOutput, cancellationToken);
            }
        }

        private async Task ExecuteRestoreDriversAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            using var fbd = new FolderBrowserDialog 
            { 
                Description = "Select folder containing drivers to restore (*.inf)",
                UseDescriptionForTitle = true
            };

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                await _driverService.RestoreDriversAsync(fbd.SelectedPath, progress, reportOutput, cancellationToken);
            }
        }

        private async Task ExecuteOptimizeDriversAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            var result = MessageBox.Show("This action will scan and potentially remove older versions of drivers. It is recommended to perform a backup first. Proceed?", 
                "Driver Optimization", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                await _driverService.OptimizeDriverStoreAsync(progress, reportOutput, cancellationToken);
            }
        }
    }
}
