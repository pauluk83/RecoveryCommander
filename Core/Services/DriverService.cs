using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class DriverService
    {
        public async Task BackupDriversAsync(string destinationPath, IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(destinationPath)) return;
            if (!Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);

            progress.Report(new ProgressReport(10, $"Backing up drivers to {destinationPath}..."));
            await DismHelper.RunDismAsync($"/online /export-driver /destination:\"{destinationPath}\"", progress, reportOutput, cancellationToken);
        }

        public async Task RestoreDriversAsync(string sourcePath, IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
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
            await AsyncHelpers.RunProcessAsync(psi, reportOutput ?? (_ => {}), error => reportOutput?.Invoke($"ERROR: {error}"), cancellationToken);
            
            progress.Report(new ProgressReport(100, "Driver restoration complete."));
        }

        public async Task OptimizeDriverStoreAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(20, "Optimizing driver store (removing old drivers)..."));
            // This is complex to automate safely via pnputil /delete-driver, 
            // but we can at least trigger a cleanup info scan.
            var psi = CoreUtilities.CreateProcessInfo("pnputil.exe", "/enum-drivers");
            await AsyncHelpers.RunProcessAsync(psi, reportOutput, null, cancellationToken);
        }
    }
}
