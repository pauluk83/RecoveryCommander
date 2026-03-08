using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class UpdateService
    {
        public async Task UpgradeWingetPackagesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(5, "Checking for winget..."));
            if (!IsWingetInstalled())
            {
                reportOutput("winget not found. Attempting to install...");
                await InstallWingetAsync(reportOutput, cancellationToken);
                if (!IsWingetInstalled())
                {
                    reportOutput("Failed to install winget. Skipping package upgrades.");
                    return;
                }
            }

            progress.Report(new ProgressReport(20, "Scanning for updates..."));
            var psi = CoreUtilities.CreateProcessInfo("winget", "upgrade --all --silent --accept-package-agreements --accept-source-agreements");
            await AsyncHelpers.RunProcessAsync(psi, reportOutput, null, cancellationToken);
        }

        public async Task UpdateStoreAppsAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(10, "Triggering Microsoft Store updates..."));
            string script = "Get-CimInstance -Namespace root/Microsoft/Windows/Appx -ClassName MSFT_AppxPackage | Foreach-Object { $_.Update() }";
            var psi = CoreUtilities.CreateProcessInfo("powershell", $"-NoProfile -NonInteractive -Command \"{script}\"");
            await AsyncHelpers.RunProcessAsync(psi, reportOutput, null, cancellationToken);
        }

        public async Task ScanForWindowsUpdatesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(5, "Initializing Windows Update Agent..."));
            await Task.Run(() =>
            {
                try
                {
                    Type type = Type.GetTypeFromProgID("Microsoft.Update.Session") ?? throw new Exception("Could not create WU Session.");
                    dynamic session = Activator.CreateInstance(type)!;
                    dynamic searcher = session.CreateUpdateSearcher();
                    reportOutput("Scanning for updates (this may take a few minutes)...");
                    
                    dynamic result = searcher.Search("IsInstalled=0 and Type='Software'");
                    int count = result.Updates.Count;
                    reportOutput($"Found {count} applicable updates.");
                    
                    for (int i = 0; i < count; i++)
                    {
                        reportOutput($"- {result.Updates.Item(i).Title}");
                    }
                }
                catch (Exception ex)
                {
                    reportOutput($"WU Scan Error: {ex.Message}");
                }
            }, cancellationToken);
        }

        private bool IsWingetInstalled()
        {
            try
            {
                var psi = CoreUtilities.CreateProcessInfo("winget", "--version");
                using var proc = Process.Start(psi);
                return proc != null && proc.WaitForExit(3000) && proc.ExitCode == 0;
            }
            catch { return false; }
        }

        private async Task InstallWingetAsync(Action<string> reportOutput, CancellationToken cancellationToken)
        {
            string url = "https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle";
            string temp = Path.Combine(Path.GetTempPath(), "winget_installer.msixbundle");
            try
            {
                await AsyncHelpers.DownloadFileAsync(url, temp, null, cancellationToken);
                var psi = CoreUtilities.CreateProcessInfo("powershell", $"-NoProfile -NonInteractive -Command \"Add-AppxPackage -Path '{temp}'\"");
                await AsyncHelpers.RunProcessAsync(psi, reportOutput, null, cancellationToken);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            }
        }
    }
}
