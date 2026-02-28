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
            _lastDismPercent = 0;
            progress?.Report(new ProgressReport(1, $"Starting DISM: {actionName}..."));
            try
            {
                await RunDismAsync(args, progress ?? new Progress<ProgressReport>(), reportOutput, cancellationToken);
                progress?.Report(new ProgressReport(100, "Completed"));
            }
            catch (Exception ex)
            {
                reportOutput($"Exception: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Error occurred"));
            }
        }

        private async Task RunDismAsync(string args, IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            var psi = RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("dism.exe", args);
            await RunProcessAndReportAsync(psi, progress, reportOutput, cancellationToken);
        }

        private static async Task RunProcessAndReportAsync(ProcessStartInfo psi, IProgress<ProgressReport>? progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            try
            {
                // Delegate to robust implementation in CoreUtilities with progress parsing
                await AsyncHelpers.RunProcessAsync(psi, 
                    output => {
                        reportOutput(output);
                        
                        // Parse DISM progress from output
                        if (progress != null && !string.IsNullOrWhiteSpace(output))
                        {
                            ParseDismProgress(output, progress);
                        }
                    }, 
                    error => reportOutput("ERROR: " + error), 
                    cancellationToken);
            }
            catch (Exception ex) 
            { 
                reportOutput($"Failed to run process {psi.FileName} {psi.Arguments}: {ex.Message}"); 
            }
        }
        
        private static int _lastDismPercent = 0;
        
        private static void ParseDismProgress(string output, IProgress<ProgressReport> progress)
        {
            var lowerOutput = output.ToLower();
            
            // Extract percentage from any line containing a % sign
            string cleanOutput = output.Replace("\0", "").Replace(" ", "");
            var match = System.Text.RegularExpressions.Regex.Match(cleanOutput, @"(\d+)(?:\.\d+)?%", System.Text.RegularExpressions.RegexOptions.RightToLeft);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var percent))
            {
                if (percent >= _lastDismPercent)
                {
                    _lastDismPercent = percent;
                    var detail = lowerOutput.Contains("complete") ? "complete" : "processing";
                    progress.Report(new ProgressReport(_lastDismPercent, $"DISM: {_lastDismPercent}% {detail}"));
                }
                return;
            }

            // Keyword based reporting - only if it doesn't lower the current percentage significantly
            if (lowerOutput.Contains("mounting") && _lastDismPercent < 5)
            {
                _lastDismPercent = 5;
                progress.Report(new ProgressReport(_lastDismPercent, "Mounting image..."));
            }
            else if (lowerOutput.Contains("cleaning") && _lastDismPercent < 50)
            {
                // Component cleanup is usually late stage
                progress.Report(new ProgressReport(_lastDismPercent > 50 ? _lastDismPercent : 50, "Cleaning component store..."));
            }
            else if (lowerOutput.Contains("restoring") && _lastDismPercent < 40)
            {
                progress.Report(new ProgressReport(40, "Restoring health..."));
            }
            else if (lowerOutput.Contains("scanning") && _lastDismPercent < 10)
            {
                progress.Report(new ProgressReport(10, "Scanning health..."));
            }
            else if (lowerOutput.Contains("operation completed successfully") || lowerOutput.Contains("completed successfully"))
            {
                _lastDismPercent = 0; // Reset for next run
                progress.Report(new ProgressReport(100, "Operation completed successfully"));
            }
        }
    }
}
