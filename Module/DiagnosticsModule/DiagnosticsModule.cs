using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;

namespace DiagnosticsModule
{
    [RecoveryModule("DiagnosticsModule", "1.0.0")]
    [SupportedOSPlatform("windows")]
    public class DiagnosticsModule : IRecoveryModule
    {
        public string Name => "Diagnostics";
        public string Description => "Comprehensive system diagnostic toolkit for identifying hardware and software issues.";
        public string Version => "1.0.0";
        public string HealthStatus => "Healthy";
        public string BuildInfo => "DiagnosticsModule (Technician Toolkit)";
        public bool SupportsAsync => true;

        public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
        {
            new("Full Diagnostic", "Run Full Diagnostic") { 
                Description = "Executes all diagnostic checks and generates a comprehensive report.",
                ExecuteActionExtended = RunFullDiagnostic,
                Highlight = true,
                IconName = "Activity"
            },
            new("System Info", "System Hardware Info") { 
                Description = "Displays detailed system hardware and operating system information.",
                ExecuteActionExtended = (p, o, d, c) => RunDiagnosticCommand("systeminfo", "", "Gathering system information...", p, o, d, c, true) 
            },
            new("CPU Details", "CPU & Processor Details") { 
                Description = "Shows CPU model, cores, and logical processor count.",
                ExecuteActionExtended = (p, o, d, c) => RunDiagnosticCommand("wmic", "cpu get name,NumberOfCores,NumberOfLogicalProcessors", "Retrieving CPU details...", p, o, d, c, true) 
            },
            new("RAM Details", "Memory (RAM) Modules") { 
                Description = "Displays installed RAM capacity and speed.",
                ExecuteActionExtended = (p, o, d, c) => RunDiagnosticCommand("wmic", "memorychip get capacity,speed", "Checking RAM modules...", p, o, d, c, true) 
            },
            new("Disk Health", "Disk Health (SMART)") { 
                Description = "Checks the health status and SMART attributes of physical drives.",
                ExecuteActionExtended = (p, o, d, c) => RunDiagnosticCommand("wmic", "diskdrive get model,status,size", "Analyzing disk health...", p, o, d, c, true) 
            },
            new("Storage Space", "Free Storage Space") { 
                Description = "Shows free space and file system type for all logical drives.",
                ExecuteActionExtended = (p, o, d, c) => RunDiagnosticCommand("wmic", "logicaldisk get caption,freespace,size,filesystem", "Calculating storage space...", p, o, d, c, true) 
            },
            new("Network Config", "Network Configuration") { 
                Description = "Displays all network adapter settings and IP configurations.",
                ExecuteActionExtended = (p, o, d, c) => RunDiagnosticCommand("ipconfig", "/all", "Retrieving network configuration...", p, o, d, c, true) 
            },
            new("Active Connections", "Active Connections") { 
                Description = "Lists all active network connections and listening ports.",
                ExecuteActionExtended = (p, o, d, c) => RunDiagnosticCommand("netstat", "-ano", "Scanning active connections...", p, o, d, c, true) 
            },
            new("Startup Programs", "Startup Programs") { 
                Description = "Lists applications configured to run at system startup.",
                ExecuteActionExtended = (p, o, d, c) => RunDiagnosticCommand("wmic", "startup get caption,command", "Checking startup programs...", p, o, d, c, true) 
            },
            new("Running Processes", "Running Processes") { 
                Description = "Lists all currently running processes and services.",
                ExecuteActionExtended = (p, o, d, c) => RunDiagnosticCommand("tasklist", "", "Listing running processes...", p, o, d, c, true) 
            },
            new("File Integrity", "System File Integrity") { 
                Description = "Verifies the integrity of system files (verify only).",
                ExecuteActionExtended = (p, o, d, c) => RunDiagnosticCommand("sfc", "/verifyonly", "Checking system file integrity...", p, o, d, c, true) 
            }
        };

        private async Task<string> RunDiagnosticCommand(string fileName, string arguments, string statusMessage, IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken, bool showIndividualReport = false)
        {
            progress.Report(new ProgressReport(0, statusMessage));
            reportOutput($"> Running: {fileName} {arguments}");
            var reportBuilder = new System.Text.StringBuilder();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                Action<string> combinedOutput = s => {
                    reportOutput(s);
                    reportBuilder.AppendLine(s);
                };

                await AsyncHelpers.RunProcessAsync(psi, combinedOutput, combinedOutput, cancellationToken);
                progress.Report(new ProgressReport(100, "Completed"));
                
                if (showIndividualReport)
                {
                    reportOutput($"> [POPUP] Requesting themed report dialog for: {fileName}");
                    try 
                    {
                        dialogService.ShowContentDialog(reportBuilder.ToString(), $"{fileName} Diagnostic Report");
                        reportOutput($"> [POPUP] Dialog displayed successfully.");
                    }
                    catch (Exception dex)
                    {
                        reportOutput($"> [ERROR] Themed dialog failed: {dex.Message}. Falling back to standard MessageBox.");
                        System.Windows.Forms.MessageBox.Show(reportBuilder.ToString(), $"{fileName} Diagnostic Report (Fallback)");
                    }
                }
            }
            catch (Exception ex)
            {
                reportOutput($"Error: {ex.Message}");
                progress.Report(new ProgressReport(100, "Failed"));
            }

            return reportBuilder.ToString();
        }

        private async Task RunFullDiagnostic(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
        {
            var diagnosticActions = Actions.Where(a => a.Name != "Full Diagnostic").ToList();
            int total = diagnosticActions.Count;
            int current = 0;
            var fullReport = new System.Text.StringBuilder();

            reportOutput("=== STARTING FULL TECHNICIAN DIAGNOSTIC ===");
            fullReport.AppendLine("=== RECOVERY COMMANDER TECHNICIAN DIAGNOSTIC REPORT ===");
            fullReport.AppendLine($"Generated: {DateTime.Now}");
            fullReport.AppendLine("-------------------------------------------------------");

            foreach (var action in diagnosticActions)
            {
                if (cancellationToken.IsCancellationRequested) break;

                current++;
                int percent = (int)((double)current / total * 100);
                progress.Report(new ProgressReport(percent, $"Running {action.DisplayName} ({current}/{total})..."));
                fullReport.AppendLine($"\n[{action.DisplayName}]");

                try
                {
                    if (action.ExecuteActionExtended != null)
                    {
                        // We override the reporter to collect the result for the full report
                        var result = await RunDiagnosticCommandFromAction(action, progress, reportOutput, dialogService, cancellationToken);
                        fullReport.AppendLine(result);
                    }
                }
                catch (Exception ex)
                {
                    reportOutput($"[ERROR] {action.DisplayName}: {ex.Message}");
                    fullReport.AppendLine($"ERROR: {ex.Message}");
                }

                reportOutput("\n" + new string('-', 40) + "\n");
                fullReport.AppendLine(new string('-', 55));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                reportOutput("!!! DIAGNOSTIC CANCELLED BY USER !!!");
                progress.Report(new ProgressReport(100, "Cancelled"));
            }
            else
            {
                reportOutput("=== DIAGNOSTIC COMPLETE ===");
                progress.Report(new ProgressReport(100, "Finished All Checks"));
                
                reportOutput("> [POPUP] Opening final diagnostic summary report...");
                try 
                {
                    dialogService.ShowContentDialog(fullReport.ToString(), "Full System Diagnostic Report");
                    reportOutput("> [POPUP] Full report displayed.");
                }
                catch (Exception dex)
                {
                    reportOutput($"> [ERROR] Full report dialog failed: {dex.Message}. Falling back to standard MessageBox.");
                    System.Windows.Forms.MessageBox.Show(fullReport.ToString(), "Full System Diagnostic Report (Fallback)");
                }
            }
        }

        private async Task<string> RunDiagnosticCommandFromAction(ModuleAction action, IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
        {
            // This is a helper to run individual commands without showing their popups during full diagnostic
            if (action.DisplayName == "System Hardware Info") return await RunDiagnosticCommand("systeminfo", "", "Gathering system info...", progress, reportOutput, dialogService, cancellationToken, false);
            if (action.DisplayName == "CPU & Processor Details") return await RunDiagnosticCommand("wmic", "cpu get name,NumberOfCores,NumberOfLogicalProcessors", "CPU info...", progress, reportOutput, dialogService, cancellationToken, false);
            if (action.DisplayName == "Memory (RAM) Modules") return await RunDiagnosticCommand("wmic", "memorychip get capacity,speed", "RAM info...", progress, reportOutput, dialogService, cancellationToken, false);
            if (action.DisplayName == "Disk Health (SMART)") return await RunDiagnosticCommand("wmic", "diskdrive get model,status,size", "Disk health...", progress, reportOutput, dialogService, cancellationToken, false);
            if (action.DisplayName == "Free Storage Space") return await RunDiagnosticCommand("wmic", "logicaldisk get caption,freespace,size,filesystem", "Storage space...", progress, reportOutput, dialogService, cancellationToken, false);
            if (action.DisplayName == "Network Configuration") return await RunDiagnosticCommand("ipconfig", "/all", "Network config...", progress, reportOutput, dialogService, cancellationToken, false);
            if (action.DisplayName == "Active Connections") return await RunDiagnosticCommand("netstat", "-ano", "Network connections...", progress, reportOutput, dialogService, cancellationToken, false);
            if (action.DisplayName == "Startup Programs") return await RunDiagnosticCommand("wmic", "startup get caption,command", "Startup items...", progress, reportOutput, dialogService, cancellationToken, false);
            if (action.DisplayName == "Running Processes") return await RunDiagnosticCommand("tasklist", "", "Processes...", progress, reportOutput, dialogService, cancellationToken, false);
            if (action.DisplayName == "System File Integrity") return await RunDiagnosticCommand("sfc", "/verifyonly", "File integrity...", progress, reportOutput, dialogService, cancellationToken, false);
            
            return "Command execution failed.";
        }
    }
}
