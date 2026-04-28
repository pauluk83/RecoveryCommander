using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Versioning;
using System.Diagnostics;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;

namespace RecoveryCommander.Module
{
    [RecoveryModule("CloudRecoveryModule")]
    [SupportedOSPlatform("windows")]
    public class CloudRecoveryModule : IRecoveryModule
    {
        public string Name => "Cloud Recovery";
        public string Description => "Cloud-based system restoration and configuration backup.";
        public string BuildInfo => "Cloud Recovery v1.0.0 - Windows Cloud Reset and Backup.";
        public string Version => GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0";
        public string HealthStatus => "Healthy";
        public bool SupportsAsync => true;

        public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
        {
            new ModuleAction("Trigger Cloud Reset", "Initiate Windows Cloud Download and Reset", ExecuteCloudResetAsync) { Highlight = true, IsDestructive = true },
            new ModuleAction("Backup Profile to Cloud", "Sync user profile settings to cloud storage") { ExecuteActionExtended = ExecuteCloudBackupAsync },
            new ModuleAction("Restore Profile from Cloud", "Download and apply profile settings from cloud") { ExecuteActionExtended = ExecuteCloudRestoreAsync },
            new ModuleAction("Configure Cloud Account", "Set up OneDrive/GitHub integration for backups") { ExecuteActionExtended = ExecuteConfigureCloudAsync }
        };

        private Task ExecuteCloudResetAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            var result = MessageBox.Show("This will restart your computer and begin the Windows Cloud Reset process. This is a DESTRUCTIVE action. Continue?",
                "Cloud Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                progress.Report(new ProgressReport(50, "Launching systemreset..."));
                reportOutput?.Invoke("> systemreset -cleanpc");
                // Note: -cleanpc triggers the "Fresh Start" / Cloud Reset flow in modern Windows
                Process.Start(new ProcessStartInfo("systemreset.exe", "-cleanpc") { UseShellExecute = true });
                progress.Report(new ProgressReport(100, "Windows Reset Wizard launched."));
            }

            return Task.CompletedTask;
        }

        private async Task ExecuteCloudBackupAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
        {
            var service = new CloudProfileSyncService(progress, reportOutput);
            var providers = service.DetectAvailableProviders();

            if (!providers.Any())
            {
                MessageBox.Show("No supported cloud sync clients (OneDrive or Google Drive) detected.", "Cloud Sync Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string provider = providers.First();
            if (providers.Count > 1)
            {
                // Simple choice since we can't easily build a full picker in this flow yet
                var dialogResult = MessageBox.Show($"Multiple cloud providers detected ({string.Join(", ", providers)}).\n\nUse {provider} for backup?",
                    "Select Provider", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (dialogResult == DialogResult.No)
                {
                    provider = providers[1];
                }
            }

            var confirm = MessageBox.Show($"This will back up your Desktop, Documents, and Pictures to your {provider} folder. Continue?",
                "Start Backup", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (confirm == DialogResult.Yes)
            {
                await service.BackupProfileAsync(provider, cancellationToken);
            }
        }

        private async Task ExecuteCloudRestoreAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
        {
            var service = new CloudProfileSyncService(progress, reportOutput);
            var providers = service.DetectAvailableProviders();

            if (!providers.Any())
            {
                MessageBox.Show("No supported cloud sync clients detected.", "Cloud Sync Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string provider = providers.First();
            if (providers.Count > 1)
            {
                var dialogResult = MessageBox.Show($"Detecting backups on {provider}. Use this provider?",
                    "Select Provider", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (dialogResult == DialogResult.No)
                {
                    provider = providers[1];
                }
            }

            var confirm = MessageBox.Show($"This will restore the latest backup found in your {provider} folder. Existing files may be updated. Continue?",
                "Start Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                await service.RestoreProfileAsync(provider, cancellationToken);
            }
        }

        private async Task ExecuteConfigureCloudAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
        {
            var service = new CloudProfileSyncService(progress, reportOutput);
            var providers = service.DetectAvailableProviders();

            string status = providers.Any() ? $"Detected: {string.Join(", ", providers)}" : "No providers detected.";

            MessageBox.Show($"Cloud Configuration Status:\n\n{status}\n\nRecoveryCommander automatically leverages your installed OneDrive and Google Drive clients for zero-setup profile protection.",
                "Cloud Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await Task.CompletedTask;
        }
    }
}
