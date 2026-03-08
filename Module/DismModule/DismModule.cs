// AUDIT MARKER: DismModule.cs | Created: 2025-09-10
// CHANGELOG:
// - New module implementing DISM operations as a standalone module.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace RecoveryCommander.Module
{
    [SupportedOSPlatform("windows")]
    [RecoveryModuleAttribute("DismModule", "1.0.0")]
    public class DismModule : IRecoveryModule
    {
        public string Name => "DISM";
        public string Description => "Deployment Image Servicing and Management (DISM) operations.";
        public string BuildInfo => "DISM Module v1.0.0 - Deployment Image Servicing and Management";

        public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
        {
            new("CheckHealth", "Check image health (/Online /Cleanup-Image /CheckHealth)")
            {
                ExecuteAction = (p, o, c) => ExecuteActionSafeAsync("CheckHealth", "/Online /Cleanup-Image /CheckHealth", p, o, c)
            },
            new("ScanHealth", "Scan image health (/Online /Cleanup-Image /ScanHealth)")
            {
                ExecuteAction = (p, o, c) => ExecuteActionSafeAsync("ScanHealth", "/Online /Cleanup-Image /ScanHealth", p, o, c)
            },
            new("RestoreHealth", "Restore image health (/Online /Cleanup-Image /RestoreHealth)")
            {
                ExecuteAction = (p, o, c) => ExecuteActionSafeAsync("RestoreHealth", "/Online /Cleanup-Image /RestoreHealth", p, o, c)
            },
            new("StartComponentCleanup", "Component cleanup (/Online /Cleanup-Image /StartComponentCleanup)")
            {
                ExecuteAction = (p, o, c) => ExecuteActionSafeAsync("StartComponentCleanup", "/Online /Cleanup-Image /StartComponentCleanup", p, o, c)
            },
         };

        public string Version => "1.0.0";
        public string HealthStatus => "Healthy";
        public bool SupportsAsync => true;


        private void RunDism(string args, Action<string> reportOutput, Func<bool> isCancelled)
        {
            var psi = RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("dism.exe", args);
            RunProcessAndReport(psi, reportOutput, isCancelled);
        }

        // Use consolidated RunProcessAndReport from Core/AsyncHelpers
        private static void RunProcessAndReport(ProcessStartInfo psi, Action<string> reportOutput, Func<bool> isCancelled)
        {
            RecoveryCommander.Core.AsyncHelpers.RunProcessAndReport(psi, reportOutput, isCancelled);
        }

        private async Task ExecuteActionSafeAsync(string actionName, string args, IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(1, $"Starting DISM: {actionName}..."));
            try
            {
                await DismHelper.RunDismAsync(args, progress ?? new Progress<ProgressReport>(), reportOutput, cancellationToken);
                progress?.Report(new ProgressReport(100, "Completed"));
            }
            catch (Exception ex)
            {
                reportOutput($"Exception: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Error occurred"));
            }
        }
    }
}
