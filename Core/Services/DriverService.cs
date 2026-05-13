using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Core.Services
{
    [SupportedOSPlatform("windows")]
    public static class DriverService
    {
        public static async Task BackupDriversAsync(string destinationPath, IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(progress);
            ArgumentNullException.ThrowIfNull(reportOutput);

            if (string.IsNullOrWhiteSpace(destinationPath)) return;
            if (!Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);

            progress.Report(new ProgressReport(10, $"Backing up drivers to {destinationPath}..."));
            await DismHelper.RunDismAsync($"/online /export-driver /destination:\"{destinationPath}\"", progress, reportOutput, cancellationToken).ConfigureAwait(false);
        }

        public static async Task RestoreDriversAsync(string sourcePath, IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(progress);
            ArgumentNullException.ThrowIfNull(reportOutput);

            if (string.IsNullOrWhiteSpace(sourcePath)) return;
            if (!Directory.Exists(sourcePath))
            {
                reportOutput?.Invoke($"Error: Source directory {sourcePath} does not exist.");
                return;
            }

            progress.Report(new ProgressReport(20, $"Restoring drivers from {sourcePath}..."));
            // pnputil /add-driver "C:\Drivers\*.inf" /subdirs /install
            string command = $"pnputil.exe /add-driver \"{Path.Combine(sourcePath, "*.inf")}\" /subdirs /install";
            reportOutput?.Invoke($"> {command}");
            
            var psi = CoreUtilities.CreateProcessInfo("pnputil.exe", $"/add-driver \"{Path.Combine(sourcePath, "*.inf")}\" /subdirs /install");
            await AsyncHelpers.RunProcessAsync(psi, reportOutput!, error => reportOutput!.Invoke($"ERROR: {error}"), cancellationToken).ConfigureAwait(false);
            
            progress.Report(new ProgressReport(100, "Driver restoration complete."));
        }

        public static async Task EnumerateDriversAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(progress);
            ArgumentNullException.ThrowIfNull(reportOutput);

            progress.Report(new ProgressReport(20, "Enumerating installed third-party drivers..."));
            reportOutput?.Invoke("> pnputil.exe /enum-drivers");

            var psi = CoreUtilities.CreateProcessInfo("pnputil.exe", "/enum-drivers");
            await AsyncHelpers.RunProcessAsync(psi, reportOutput!, null, cancellationToken).ConfigureAwait(false);

            progress.Report(new ProgressReport(100, "Driver enumeration complete."));
        }

        public static async Task OptimizeDriverStoreAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(progress);
            ArgumentNullException.ThrowIfNull(reportOutput);

            // Safely automated cleanup via pnputil /delete-driver requires reasoning about
            // currently-bound devices, so for now we surface a scan + a clear notice.
            progress.Report(new ProgressReport(20, "Scanning driver store..."));
            reportOutput?.Invoke("Driver store cleanup is currently informational. Removing redundant drivers safely requires");
            reportOutput?.Invoke("verifying each driver is no longer bound to active hardware. Listing installed drivers below:");
            reportOutput?.Invoke("> pnputil.exe /enum-drivers");

            var psi = CoreUtilities.CreateProcessInfo("pnputil.exe", "/enum-drivers");
            await AsyncHelpers.RunProcessAsync(psi, reportOutput!, null, cancellationToken).ConfigureAwait(false);

            progress.Report(new ProgressReport(100, "Driver store scan complete."));
        }
    }
}
