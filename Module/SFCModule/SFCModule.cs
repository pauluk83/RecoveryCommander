// Hidden SFC Module - Captures output directly without external window
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using System.Windows.Forms;
using System.Runtime.Versioning;

namespace RecoveryCommander.Module;

[SupportedOSPlatform("windows")]
[RecoveryModuleAttribute("SfcModule", "1.1.0")]
public sealed class SfcModule : IRecoveryModule
{
    public string Name => "System File Checker";
    public string Description => "Verifies and repairs system file integrity using SFC (System File Checker).";
    public string Version => "1.1.0";
    public string HealthStatus => "Healthy";
    public string BuildInfo => "SFCModule - System File Checker (Modernized)";
    public bool SupportsAsync => true;

    public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
    {
        new("Scan Now", "Full System Scan (/scannow)", ExecuteScanNow)
        {
            Description = "Scans all protected system files and replaces corrupted versions with correct ones.",
            RequiresAdmin = true,
            Highlight = true,
            IconName = "ShieldCheck"
        },
        new("Verify Only", "Verification Only (/verifyonly)", ExecuteVerifyOnly)
        {
            Description = "Scans protected system files for corruption but does not attempt any repairs.",
            RequiresAdmin = true,
            IconName = "Search"
        },
        new("Offline Scan", "Offline System Scan", ExecuteOfflineScan)
        {
            Description = "Performs an SFC scan on an offline Windows installation or directory.",
            RequiresAdmin = true,
            IconName = "HardDrive"
        }
    };

    private Task ExecuteScanNow(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        => ExecuteActionSafeAsync("Scan Now", "/scannow", progress, reportOutput, cancellationToken);

    private Task ExecuteVerifyOnly(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        => ExecuteActionSafeAsync("Verify Only", "/verifyonly", progress, reportOutput, cancellationToken);

    private Task ExecuteOfflineScan(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
    {
        var bootDir = PromptForFolder("Select boot directory (e.g. C:\\)");
        var winDir = PromptForFolder("Select Windows directory (e.g. C:\\Windows)");
        
        if (string.IsNullOrWhiteSpace(bootDir) || string.IsNullOrWhiteSpace(winDir))
        {
            reportOutput("Offline scan cancelled: Required directories not selected.");
            return Task.CompletedTask;
        }

        return ExecuteActionSafeAsync("Offline Scan", $"/scannow /offbootdir={bootDir} /offwindir={winDir}", progress, reportOutput, cancellationToken);
    }

    private async Task ExecuteActionSafeAsync(string actionName, string arguments, IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
    {
        progress.Report(new ProgressReport(0, $"Preparing {actionName}..."));

        if (!CoreUtilities.IsAdministrator())
        {
            reportOutput("CRITICAL: Administrative privileges are required to run SFC.");
            progress.Report(new ProgressReport(100, "Permission Denied"));
            return;
        }

        try
        {
            await SfcHelper.RunSfcAsync(arguments, progress, reportOutput, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            progress.Report(new ProgressReport(100, "Cancelled"));
            reportOutput("Operation cancelled by user.");
            throw;
        }
        catch (Exception ex)
        {
            progress.Report(new ProgressReport(100, "Error occurred"));
            reportOutput($"Error: {ex.Message}");
        }
    }

    private static string PromptForFolder(string prompt)
    {
        using var dialog = new FolderBrowserDialog 
        { 
            Description = prompt, 
            ShowNewFolderButton = false,
            UseDescriptionForTitle = true
        };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : string.Empty;
    }
}
