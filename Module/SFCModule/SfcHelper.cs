using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;

namespace RecoveryCommander.Module;

internal static class SfcHelper
{
    public static async Task RunSfcAsync(string arguments, IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
    {
        var sfcPath = GetSfcPath();
        var psi = CoreUtilities.CreateProcessInfo(sfcPath, arguments);
        int currentPercent = 0;

        reportOutput($"Starting: sfc {arguments}");

        try
        {
            await AsyncHelpers.RunProcessAsync(psi, 
                output => {
                    ProcessSfcOutput(output, reportOutput, progress, ref currentPercent);
                }, 
                error => reportOutput("ERROR: " + error), 
                cancellationToken);

            // SFC Exit codes handling (based on common sfc.exe returns)
            // Note: AsyncHelpers.RunProcessAsync throws if exit code != 0, so we need to handle it in the caller or here.
            progress.Report(new ProgressReport(100, "SFC completed successfully"));
            reportOutput("SFC scan finished.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("exited with code"))
        {
            // Parse exit code from message if possible, or use process.ExitCode if we had access to it.
            // Since AsyncHelpers doesnt return the process, we'd need to modify it or handle it based on output.
            reportOutput($"SFC finished with specialized result: {ex.Message}");
            InterpretExitCode(ex.Message, reportOutput, progress);
        }
        catch (OperationCanceledException)
        {
            reportOutput("SFC process terminated by user.");
            throw;
        }
        catch (Exception ex)
        {
            reportOutput($"Failed to run SFC: {ex.Message}");
            throw;
        }
    }

    private static void ProcessSfcOutput(string line, Action<string> reportOutput, IProgress<ProgressReport> progress, ref int progressPercent)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        reportOutput(line);

        var cleanLine = line.Replace("\0", "").Trim();
        var lowerLine = cleanLine.ToLower();

        // Extract percentage
        var match = Regex.Match(cleanLine, @"(\d+)(?:\.\d+)?%", RegexOptions.RightToLeft);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var percent))
        {
            if (percent > progressPercent)
            {
                progressPercent = percent;
                progress.Report(new ProgressReport(progressPercent, "Scanning system files...", $"Phase: Verification ({percent}%)"));
            }
            return;
        }

        // Detect phases
        if (lowerLine.Contains("beginning system scan"))
        {
            progressPercent = Math.Max(progressPercent, 5);
            progress.Report(new ProgressReport(progressPercent, "System scan started", "Initializing..."));
        }
        else if (lowerLine.Contains("beginning verification phase"))
        {
            progressPercent = Math.Max(progressPercent, 10);
            progress.Report(new ProgressReport(progressPercent, "Verification phase started", "Checking file integrity..."));
        }
        else if (lowerLine.Contains("verification") && lowerLine.Contains("complete"))
        {
            progressPercent = 100;
            progress.Report(new ProgressReport(100, "Verification complete", "Finished scanning system files."));
        }
    }

    private static void InterpretExitCode(string message, Action<string> reportOutput, IProgress<ProgressReport> progress)
    {
        if (message.Contains("code 0"))
        {
            progress.Report(new ProgressReport(100, "SFC: No integrity violations found."));
        }
        else if (message.Contains("code 1"))
        {
            reportOutput("SFC: Could not perform the requested operation.");
            progress.Report(new ProgressReport(100, "Operation failed"));
        }
        else if (message.Contains("code 2"))
        {
            reportOutput("SFC: Found integrity violations but could not fix them. See CBS.log for details.");
            progress.Report(new ProgressReport(100, "Violations found (unrepaired)"));
        }
        else if (message.Contains("code 3"))
        {
            reportOutput("SFC: Found integrity violations and repaired them successfully.");
            progress.Report(new ProgressReport(100, "Violations repaired"));
        }
    }

    private static string GetSfcPath()
    {
        var windir = Environment.GetEnvironmentVariable("windir") ?? @"C:\Windows";
        var sfcPath = Path.Combine(windir, "System32", "sfc.exe");
        
        if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
        {
            var sysnative = Path.Combine(windir, "SysNative", "sfc.exe");
            if (File.Exists(sysnative)) return sysnative;
        }
        return sfcPath;
    }
}
