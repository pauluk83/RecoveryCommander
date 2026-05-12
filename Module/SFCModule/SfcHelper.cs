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
        int currentPercent = 0;

        reportOutput($"Starting: sfc {arguments}");
        progress.Report(new ProgressReport(0, "Starting SFC..."));

        // Drive the process manually so we can read the real integer exit code.
        // AsyncHelpers.RunProcessAsync throws on any non-zero exit, which makes all
        // normal SFC outcomes (codes 1-4) look like errors to the user.
        var psi = new ProcessStartInfo
        {
            FileName               = sfcPath,
            Arguments              = arguments,
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
            // sfc.exe writes UTF-16LE to redirected streams.
            StandardOutputEncoding = System.Text.Encoding.Unicode,
            StandardErrorEncoding  = System.Text.Encoding.Unicode,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                ProcessSfcOutput(e.Data, reportOutput, progress, ref currentPercent);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                reportOutput("STDERR: " + e.Data);
        };
        process.Exited += (_, _) => tcs.TrySetResult(process.ExitCode);

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Register cancellation: kill the process tree, then cancel the TCS.
            await using var reg = cancellationToken.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch { /* already gone */ }
                tcs.TrySetCanceled(cancellationToken);
            });

            int exitCode = await tcs.Task.ConfigureAwait(false);
            InterpretExitCode(exitCode, reportOutput, progress);
        }
        catch (OperationCanceledException)
        {
            reportOutput("SFC process terminated by user.");
            progress.Report(new ProgressReport(100, "Cancelled"));
            throw;
        }
        catch (Exception ex)
        {
            reportOutput($"Failed to run SFC: {ex.Message}");
            progress.Report(new ProgressReport(100, "Error"));
            throw;
        }
    }

    private static void ProcessSfcOutput(string line, Action<string> reportOutput, IProgress<ProgressReport> progress, ref int progressPercent)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        // Strip null bytes / wide-char padding that sfc.exe sometimes emits to redirected streams.
        var cleanLine = line.Replace("\0", "").Trim();
        if (string.IsNullOrWhiteSpace(cleanLine)) return;

        reportOutput(cleanLine);

        var lowerLine = cleanLine.ToLowerInvariant();

        // Collapse space-padded digit groups before matching (e.g. "3 4 %" -> "34%").
        var compacted = Regex.Replace(cleanLine, @"(\d)\s+(\d)", "$1$2");
        var match = Regex.Match(compacted, @"(\d+)(?:\.\d+)?%", RegexOptions.RightToLeft);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var percent) && percent is >= 0 and <= 100)
        {
            if (percent > progressPercent)
            {
                progressPercent = percent;
                progress.Report(new ProgressReport(progressPercent, "Scanning system files...", $"Verification: {percent}%"));
            }
            return;
        }

        // Phase keyword detection
        if (lowerLine.Contains("beginning system scan"))
        {
            progressPercent = Math.Max(progressPercent, 5);
            progress.Report(new ProgressReport(progressPercent, "System scan started", "Initializing..."));
        }
        else if (lowerLine.Contains("beginning verification phase"))
        {
            progressPercent = Math.Max(progressPercent, 10);
            progress.Report(new ProgressReport(progressPercent, "Verification phase", "Checking file integrity..."));
        }
        else if (lowerLine.Contains("verification") && lowerLine.Contains("complete"))
        {
            progressPercent = Math.Max(progressPercent, 95);
            progress.Report(new ProgressReport(progressPercent, "Verification complete", "Finalizing..."));
        }
    }

    /// <summary>
    /// Maps documented sfc.exe exit codes to clear user-facing messages.
    ///   0 — no integrity violations found.
    ///   1 — could not perform the requested operation (usually needs reboot or admin).
    ///   2 — found corrupt files and repaired them successfully.
    ///   3 — found corrupt files but could not repair all of them.
    ///   4 — repairs queued; reboot required to complete.
    /// </summary>
    private static void InterpretExitCode(int exitCode, Action<string> reportOutput, IProgress<ProgressReport> progress)
    {
        switch (exitCode)
        {
            case 0:
                reportOutput("SFC: Windows Resource Protection did not find any integrity violations.");
                progress.Report(new ProgressReport(100, "No violations found"));
                break;

            case 1:
                reportOutput("SFC: Windows Resource Protection could not perform the requested operation.");
                reportOutput("Tip: Ensure the application is running as Administrator and that no pending Windows operations are blocking SFC.");
                progress.Report(new ProgressReport(100, "Operation could not be performed"));
                break;

            case 2:
                reportOutput("SFC: Windows Resource Protection found corrupt files and successfully repaired them.");
                reportOutput("Details: %WinDir%\\Logs\\CBS\\CBS.log contains the full repair log.");
                progress.Report(new ProgressReport(100, "Violations repaired successfully"));
                break;

            case 3:
                reportOutput("SFC: Windows Resource Protection found corrupt files but could not fix some of them.");
                reportOutput("Details: %WinDir%\\Logs\\CBS\\CBS.log contains the full repair log.");
                reportOutput("Tip: Run DISM /Online /Cleanup-Image /RestoreHealth first, then re-run SFC /scannow.");
                progress.Report(new ProgressReport(100, "Some violations could not be repaired"));
                break;

            case 4:
                reportOutput("SFC: Repairs were queued — a restart is required to complete the operation.");
                progress.Report(new ProgressReport(100, "Reboot required to complete repairs"));
                break;

            default:
                reportOutput($"SFC: Exited with unexpected code {exitCode}.");
                reportOutput("Check %WinDir%\\Logs\\CBS\\CBS.log for details.");
                progress.Report(new ProgressReport(100, $"Finished (exit code {exitCode})"));
                break;
        }
    }

    private static string GetSfcPath()
    {
        var windir = Environment.GetEnvironmentVariable("windir") ?? @"C:\Windows";
        // When the host is a 32-bit process on a 64-bit OS, System32 is silently
        // redirected to SysWOW64. SysNative bypasses that redirector so we always
        // call the real 64-bit sfc.exe.
        if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
        {
            var sysnative = Path.Combine(windir, "SysNative", "sfc.exe");
            if (File.Exists(sysnative)) return sysnative;
        }
        return Path.Combine(windir, "System32", "sfc.exe");
    }
}
