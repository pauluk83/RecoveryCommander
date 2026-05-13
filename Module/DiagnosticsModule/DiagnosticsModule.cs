/*
 * AUDIT HEADER
 * File: DiagnosticsModule.cs
 * Module: Diagnostics
 * Created: 2026-04-20
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-04-20 - 1.0.0 - Initial diagnostics module with system info, CPU, RAM, disk, network checks.
 * 2026-05-02 - 1.2.6 - Unified command metadata: per-action (file, args) declared once in
 *                       _diagnosticCommands and consumed by both Actions and RunFullDiagnostic.
 *                       Removes the duplicate commandMap dictionary.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;

namespace RecoveryCommander.Modules
{
    [RecoveryModule("DiagnosticsModule")]
    [SupportedOSPlatform("windows")]
    public class DiagnosticsModule : IRecoveryModule
    {
        public string Name => "Diagnostics";
        public string Description => "Comprehensive system diagnostic toolkit for identifying hardware and software issues.";
        public string Version => GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0";
        public string HealthStatus => "Healthy";
        public string BuildInfo => "DiagnosticsModule (Technician Toolkit)";
        public bool SupportsAsync => true;

        // Single source of truth for diagnostic commands. Used by both Actions and RunFullDiagnostic.
        private sealed record DiagnosticCommand(
            string Name,
            string DisplayName,
            string Description,
            string FileName,
            string Arguments,
            string StatusMessage);

        private static readonly DiagnosticCommand[] _diagnosticCommands =
        {
            new("System Info",         "System Hardware Info",      "Displays detailed system hardware and operating system information.", "systeminfo", "",                                                          "Gathering system information..."),
            new("CPU Details",         "CPU & Processor Details",   "Shows CPU model, cores, and logical processor count.",                  "wmic",       "cpu get name,NumberOfCores,NumberOfLogicalProcessors",      "Retrieving CPU details..."),
            new("RAM Details",         "Memory (RAM) Modules",      "Displays installed RAM capacity and speed.",                            "wmic",       "memorychip get capacity,speed",                             "Checking RAM modules..."),
            new("Disk Health",         "Disk Health (SMART)",       "Checks the health status and SMART attributes of physical drives.",      "wmic",       "diskdrive get model,status,size",                           "Analyzing disk health..."),
            new("Storage Space",       "Free Storage Space",        "Shows free space and file system type for all logical drives.",         "wmic",       "logicaldisk get caption,freespace,size,filesystem",         "Calculating storage space..."),
            new("Network Config",      "Network Configuration",     "Displays all network adapter settings and IP configurations.",          "ipconfig",   "/all",                                                      "Retrieving network configuration..."),
            new("Active Connections",  "Active Connections",        "Lists all active network connections and listening ports.",             "netstat",    "-ano",                                                      "Scanning active connections..."),
            new("Startup Programs",    "Startup Programs",          "Lists applications configured to run at system startup.",               "wmic",       "startup get caption,command",                               "Checking startup programs..."),
            new("Running Processes",   "Running Processes",         "Lists all currently running processes and services.",                   "tasklist",   "",                                                          "Listing running processes..."),
            new("File Integrity",      "System File Integrity",     "Verifies the integrity of system files (verify only).",                 "sfc",        "/verifyonly",                                               "Checking system file integrity..."),
        };

        public IEnumerable<ModuleAction> Actions
        {
            get
            {
                yield return new ModuleAction("Full Diagnostic", "Run Full Diagnostic")
                {
                    Description = "Executes all diagnostic checks and generates a comprehensive report.",
                    ExecuteActionExtended = RunFullDiagnostic,
                    Highlight = true,
                    IconName = "Activity"
                };

                foreach (var cmd in _diagnosticCommands)
                {
                    var local = cmd; // capture for closure
                    yield return new ModuleAction(local.Name, local.DisplayName)
                    {
                        Description = local.Description,
                        ExecuteActionExtended = (p, o, d, c) =>
                            RunDiagnosticCommand(local.FileName, local.Arguments, local.StatusMessage, p, o, d, c, showIndividualReport: true)
                    };
                }
            }
        }

        private static async Task<string> RunDiagnosticCommand(
            string fileName,
            string arguments,
            string statusMessage,
            IProgress<ProgressReport> progress,
            Action<string> reportOutput,
            IDialogService dialogService,
            CancellationToken cancellationToken,
            bool showIndividualReport = false)
        {
            progress.Report(new ProgressReport(0, statusMessage));
            reportOutput($"> Running: {fileName} {arguments}");
            var reportBuilder = new StringBuilder();

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

                Action<string> combinedOutput = s =>
                {
                    reportOutput(s);
                    reportBuilder.AppendLine(s);
                };

                await AsyncHelpers.RunProcessAsync(psi, combinedOutput, combinedOutput, cancellationToken);
                progress.Report(new ProgressReport(100, "Completed"));

                if (showIndividualReport)
                {
                    try
                    {
                        dialogService.ShowContentDialog(reportBuilder.ToString(), $"{fileName} Diagnostic Report");
                    }
                    catch (Exception dex)
                    {
                        reportOutput($"[WARN] Themed dialog unavailable ({dex.Message}); falling back to MessageBox.");
                        System.Windows.Forms.MessageBox.Show(reportBuilder.ToString(), $"{fileName} Diagnostic Report (Fallback)");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                reportOutput("Operation cancelled.");
                progress.Report(new ProgressReport(100, "Cancelled"));
            }
            catch (Exception ex)
            {
                reportOutput($"Error: {ex.Message}");
                progress.Report(new ProgressReport(100, "Failed"));
            }

            return reportBuilder.ToString();
        }

        private async Task RunFullDiagnostic(
            IProgress<ProgressReport> progress,
            Action<string> reportOutput,
            IDialogService dialogService,
            CancellationToken cancellationToken)
        {
            var commands = _diagnosticCommands;
            int total = commands.Length;
            int current = 0;
            var fullReport = new StringBuilder();

            reportOutput("=== STARTING FULL TECHNICIAN DIAGNOSTIC ===");
            fullReport.AppendLine("=== RECOVERY COMMANDER TECHNICIAN DIAGNOSTIC REPORT ===");
            fullReport.AppendFormat(CultureInfo.InvariantCulture, "Generated: {0}", DateTime.Now).AppendLine();
            fullReport.AppendLine("-------------------------------------------------------");

            foreach (var cmd in commands)
            {
                if (cancellationToken.IsCancellationRequested) break;

                current++;
                int percent = (int)((double)current / total * 100);
                progress.Report(new ProgressReport(percent, string.Format(CultureInfo.InvariantCulture, "Running {0} ({1}/{2})...", cmd.DisplayName, current, total)));
                fullReport.AppendFormat(CultureInfo.InvariantCulture, "\n[{0}]", cmd.DisplayName).AppendLine();

                try
                {
                    var result = await RunDiagnosticCommand(
                        cmd.FileName, cmd.Arguments, $"{cmd.DisplayName}...",
                        progress, reportOutput, dialogService, cancellationToken,
                        showIndividualReport: false);
                    fullReport.AppendLine(result);
                }
                catch (Exception ex)
                {
                    reportOutput($"[ERROR] {cmd.DisplayName}: {ex.Message}");
                    fullReport.AppendFormat(CultureInfo.InvariantCulture, "ERROR: {0}", ex.Message).AppendLine();
                }

                reportOutput("\n" + new string('-', 40) + "\n");
                fullReport.AppendLine(new string('-', 55));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                reportOutput("!!! DIAGNOSTIC CANCELLED BY USER !!!");
                progress.Report(new ProgressReport(100, "Cancelled"));
                return;
            }

            reportOutput("=== DIAGNOSTIC COMPLETE ===");
            progress.Report(new ProgressReport(100, "Finished All Checks"));

            try
            {
                dialogService.ShowContentDialog(fullReport.ToString(), "Full System Diagnostic Report");
            }
            catch (Exception dex)
            {
                reportOutput($"[WARN] Full report dialog failed ({dex.Message}); falling back to MessageBox.");
                System.Windows.Forms.MessageBox.Show(fullReport.ToString(), "Full System Diagnostic Report (Fallback)");
            }
        }
    }
}
