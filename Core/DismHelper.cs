using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Core
{
    public static class DismHelper
    {
        public static async Task RunDismAsync(string args, IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            int lastPercent = 0;
            var psi = CoreUtilities.CreateProcessInfo("dism.exe", args);
            
            await AsyncHelpers.RunProcessAsync(psi, 
                output => {
                    reportOutput(output);
                    if (progress != null && !string.IsNullOrWhiteSpace(output))
                    {
                        ParseDismProgress(output, progress, p => lastPercent = p, () => lastPercent);
                    }
                }, 
                error => reportOutput("ERROR: " + error), 
                cancellationToken).ConfigureAwait(false);
        }

        private static void ParseDismProgress(string output, IProgress<ProgressReport> progress, Action<int> setPercent, Func<int> getPercent)
        {
            var upperOutput = output.ToUpperInvariant();
            int currentPercent = getPercent();
            
            string cleanOutput = output.Replace("\0", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
            var match = System.Text.RegularExpressions.Regex.Match(cleanOutput, @"(\d+)(?:\.\d+)?%", System.Text.RegularExpressions.RegexOptions.RightToLeft);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var percent))
            {
                if (percent >= currentPercent)
                {
                    currentPercent = percent;
                    setPercent(currentPercent);
                    var detail = upperOutput.Contains("COMPLETE", StringComparison.Ordinal) ? "complete" : "processing";
                    progress.Report(new ProgressReport(currentPercent, $"DISM: {currentPercent}% {detail}"));
                }
                return;
            }

            if (upperOutput.Contains("MOUNTING", StringComparison.Ordinal) && currentPercent < 5)
            {
                currentPercent = 5;
                setPercent(currentPercent);
                progress.Report(new ProgressReport(currentPercent, "Mounting image..."));
            }
            else if (upperOutput.Contains("CLEANING", StringComparison.Ordinal) && currentPercent < 50)
            {
                currentPercent = currentPercent > 50 ? currentPercent : 50;
                setPercent(currentPercent);
                progress.Report(new ProgressReport(currentPercent, "Cleaning component store..."));
            }
            else if (upperOutput.Contains("RESTORING", StringComparison.Ordinal) && currentPercent < 40)
            {
                currentPercent = 40;
                setPercent(currentPercent);
                progress.Report(new ProgressReport(currentPercent, "Restoring health..."));
            }
            else if (upperOutput.Contains("SCANNING", StringComparison.Ordinal) && currentPercent < 10)
            {
                currentPercent = 10;
                setPercent(currentPercent);
                progress.Report(new ProgressReport(currentPercent, "Scanning health..."));
            }
            else if (upperOutput.Contains("OPERATION COMPLETED SUCCESSFULLY", StringComparison.Ordinal) || upperOutput.Contains("COMPLETED SUCCESSFULLY", StringComparison.Ordinal))
            {
                setPercent(0);
                progress.Report(new ProgressReport(100, "Operation completed successfully"));
            }
        }
    }
}
