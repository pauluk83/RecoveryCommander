using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core.Security;

namespace RecoveryCommander.Core.Services
{
    [SupportedOSPlatform("windows")]
    public static class UpdateService
    {
        private static readonly Regex PackageIdRegex = new(@"^[A-Za-z0-9][A-Za-z0-9._\-+]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly HashSet<string> AllowedUpdateSources = new(StringComparer.OrdinalIgnoreCase)
        {
            "winget",
            "msstore",
            "nuget",
            "pypi",
            "chocolatey"
        };

        public static async Task UpgradeWingetPackagesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(progress);
            ArgumentNullException.ThrowIfNull(reportOutput);

            progress.Report(new ProgressReport(5, "Checking for winget..."));
            if (!IsWingetInstalled())
            {
                reportOutput("winget not found. Attempting to install...");
                await InstallWingetAsync(reportOutput, cancellationToken).ConfigureAwait(false);
                if (!IsWingetInstalled())
                {
                    reportOutput("Failed to install winget. Skipping package upgrades.");
                    return;
                }
            }

            progress.Report(new ProgressReport(20, "Scanning for updates..."));
            var psi = CoreUtilities.CreateProcessInfo("winget", "upgrade --all --silent --accept-package-agreements --accept-source-agreements");
            await AsyncHelpers.RunProcessAsync(psi, reportOutput, null, cancellationToken).ConfigureAwait(false);
        }

        public static async Task UpdateStoreAppsAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(progress);
            ArgumentNullException.ThrowIfNull(reportOutput);

            progress.Report(new ProgressReport(10, "Triggering Microsoft Store updates..."));
            string script = "Get-CimInstance -Namespace root/Microsoft/Windows/Appx -ClassName MSFT_AppxPackage | Foreach-Object { $_.Update() }";
            var psi = CoreUtilities.CreateProcessInfo("powershell", $"-NoProfile -NonInteractive -Command \"{script}\"");
            await AsyncHelpers.RunProcessAsync(psi, reportOutput, null, cancellationToken).ConfigureAwait(false);
        }

        public static async Task UpgradeWingetPackageAsync(string packageId, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            if (!IsSafePackageId(packageId))
            {
                reportOutput($"Skipping unsafe package id: {packageId}");
                return;
            }

            var psi = CoreUtilities.CreateProcessInfo("winget", "");
            psi.ArgumentList.Add("upgrade");
            psi.ArgumentList.Add("--id");
            psi.ArgumentList.Add(packageId);
            psi.ArgumentList.Add("--silent");
            psi.ArgumentList.Add("--accept-package-agreements");
            psi.ArgumentList.Add("--accept-source-agreements");
            await AsyncHelpers.RunProcessAsync(psi, reportOutput, null, cancellationToken).ConfigureAwait(false);
        }

        public static async Task UpdateStoreAppAsync(string packageId, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            if (!IsSafePackageId(packageId))
            {
                reportOutput($"Skipping unsafe Store package id: {packageId}");
                return;
            }

            // Try winget first for Store apps as it is more reliable for specific versions
            var psi = CoreUtilities.CreateProcessInfo("winget", "");
            psi.ArgumentList.Add("upgrade");
            psi.ArgumentList.Add("--id");
            psi.ArgumentList.Add(packageId);
            psi.ArgumentList.Add("--silent");
            psi.ArgumentList.Add("--accept-package-agreements");
            psi.ArgumentList.Add("--accept-source-agreements");
            psi.ArgumentList.Add("--source");
            psi.ArgumentList.Add("msstore");
            await AsyncHelpers.RunProcessAsync(psi, reportOutput, null, cancellationToken).ConfigureAwait(false);
        }

        public static async Task UpdatePSModuleAsync(string moduleName, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            if (!IsSafePackageId(moduleName))
            {
                reportOutput($"Skipping unsafe PowerShell module name: {moduleName}");
                return;
            }

            var psi = CoreUtilities.CreateProcessInfo("powershell", "");
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-NonInteractive");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add($"Update-Module -Name '{moduleName}' -Force -ErrorAction SilentlyContinue");
            await AsyncHelpers.RunProcessAsync(psi, reportOutput, null, cancellationToken).ConfigureAwait(false);
        }

        public static async Task ScanForWindowsUpdatesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(progress);
            ArgumentNullException.ThrowIfNull(reportOutput);

            progress.Report(new ProgressReport(5, "Initializing Windows Update Agent..."));
            await Task.Run(() =>
            {
                try
                {
                    Type type = Type.GetTypeFromProgID("Microsoft.Update.Session") ?? throw new InvalidOperationException("Could not create WU Session.");
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
            }, cancellationToken).ConfigureAwait(false);
        }

        private static bool IsWingetInstalled()
        {
            try
            {
                var psi = CoreUtilities.CreateProcessInfo("winget", "--version");
                using var proc = Process.Start(psi);
                return proc != null && proc.WaitForExit(3000) && proc.ExitCode == 0;
            }
            catch { return false; }
        }

        private static async Task InstallWingetAsync(Action<string> reportOutput, CancellationToken cancellationToken)
        {
            string url = "https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle";
            
            // Validate URL for security
            if (!SecurityHelpers.IsValidDownloadUrl(url, out var validUri))
            {
                AuditLogger.Instance.LogFailure("UpdateService", "InvalidDownloadUrl", "Winget installer URL validation failed");
                reportOutput("Security validation failed for winget installer URL");
                return;
            }

            // Use unique temp directory to prevent pre-population attacks
            string tempDir = Path.Combine(Path.GetTempPath(), $"winget_install_{Guid.NewGuid():N}"[..16]);
            Directory.CreateDirectory(tempDir);
            string temp = Path.Combine(tempDir, "winget_installer.msixbundle");
            
            try
            {
                AuditLogger.Instance.LogSuccess("UpdateService", "WingetInstallStart", url);
                
                await AsyncHelpers.DownloadFileAsync(url, temp, null, cancellationToken).ConfigureAwait(false);
                
                // Verify file integrity
                if (!File.Exists(temp) || new FileInfo(temp).Length == 0)
                {
                    AuditLogger.Instance.LogFailure("UpdateService", "WingetDownloadFailed", "Downloaded file is empty or missing");
                    reportOutput("Winget installer download failed verification");
                    return;
                }

                var psi = CoreUtilities.CreateProcessInfo("powershell", $"-NoProfile -NonInteractive -Command \"Add-AppxPackage -Path '{temp}'\"");
                await AsyncHelpers.RunProcessAsync(psi, reportOutput, null, cancellationToken).ConfigureAwait(false);
                
                AuditLogger.Instance.LogSuccess("UpdateService", "WingetInstallComplete");
            }
            catch (Exception ex)
            {
                AuditLogger.Instance.LogFailure("UpdateService", "WingetInstallError", ex.Message);
                reportOutput($"Winget installation failed: {ex.Message}");
            }
            finally
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        private static bool IsSafePackageId(string value)
            => !string.IsNullOrWhiteSpace(value) && PackageIdRegex.IsMatch(value);
    }
}
