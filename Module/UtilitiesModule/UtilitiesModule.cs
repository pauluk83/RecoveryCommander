using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using System.IO.Compression;

namespace UtilitiesModule
{
    [RecoveryModule("UtilitiesModule")]
    [SupportedOSPlatform("windows")]
    public class UtilitiesModule : IRecoveryModule
    {
        // ✓ Use shared HttpClient from ServiceContainer to prevent socket exhaustion
        private static HttpClient GetHttpClient() => RecoveryCommander.Core.ServiceContainer.GetHttpClient();

        // ✓ Extracted download URLs to static constants to reduce string allocations
        private static class DownloadUrls
        {
            public const string CompactGuiLatest = "https://github.com/IridiumIO/CompactGUI/releases/latest/download/CompactGUI.exe";
            public const string CCleaner = "https://recoverycommander.free.nf/files/CCleaner%206.40.115.62.txt";
            public const string Defragger = "https://drive.google.com/uc?export=download&id=1y-kGi-voJGMaT0KP8nzJ6Y5nuM9rIj4l";
            public const string Ninite = "https://drive.google.com/uc?export=download&id=1qIF8HXRBi7fdxI-ryOxJwG5UioROfMgx";
            public const string Rufus = "https://api.github.com/repos/pbatard/rufus/releases/latest";
            public const string MacriumPortable = "https://recoverycommander.free.nf/files/Macrium.txt";
            public const string Win11DebloatZip = "https://github.com/Raphire/Win11Debloat/archive/refs/heads/master.zip";
            public const string VCRedistApi = "https://api.github.com/repos/abbodi1406/vcredist/releases/latest";
            public const string PCRepairSuite = "https://recoverycommander.free.nf/files/PCRepairSuite.txt";
            public const string IObitDriverBooster = "https://recoverycommander.free.nf/files/DriverBoosterPortable.txt";
            public const string DellOSRecoveryTool = "https://recoverycommander.free.nf/files/Dell%20OS%20Recovery%20Toolv2.3.4.3569.txt";

            // User-hosted Office 2024 (Wix .txt file)
            public const string Office2024Wix = "https://99ed684e-f8f7-418e-a378-a43f97c53364.usrfiles.com/ugd/99ed68_c4c183f52c6442d1930577246c5ae215.txt";
            
            // Original GitHub-hosted Public Scripts
            public const string ActivationPublic = "https://get.activated.win";
            public const string OfficeC2RPublic = "https://c2rsetup.officeapps.live.com/c2r/download.aspx?ProductreleaseID=O365ProPlusRetail&platform=x64&language=en-us&version=O16GA";
            public const string BackupRestorePublic = "https://99ed684e-f8f7-418e-a378-a43f97c53364.usrfiles.com/ugd/99ed68_acaf0031477449598de3ef438aee35be.txt";
            public const string CleanMyPc = "https://recoverycommander.free.nf/files/CleanMyPC.txt";
            public const string ChrisTitusUtility = "https://christitus.com/win";
        }

        public string Name => "Utilities";
        public string Description => "Collection of utility tools for system maintenance and software installation";
        public string Version => GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0";
        public string HealthStatus => "Healthy";
        public string BuildInfo => "UtilitiesModule (Activation button)";
        public bool SupportsAsync => true;

        public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
        {
            new("Activation", "Activation") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.ActivationPublic, "Activate.ps1", p, o, c) },
            new("Install Office 2024", "Install Office 2024") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.Office2024Wix, "Office2024.ps1", p, o, c) },
            new("Office-C2R-Install", "Install Office Click-to-Run") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.OfficeC2RPublic, "OfficeSetup.exe", p, o, c) },
            new("Backup and Restore Activation State", "Backup and Restore Activation State") { ExecuteAction = RunBackupActivation },
            new("Christitus Utility", "Chris Titus Tech Windows Utility") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.ChrisTitusUtility, "Christitus.ps1", p, o, c) },
            new("CCleaner 6.40.115.62", "CCleaner portable") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.CCleaner, "CCleaner 6.40.115.62.exe", p, o, c) },
            new("Macrium Reflect X 10.0.8843", "Macrium Reflect X Portable") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.MacriumPortable, "Macrium Reflect X 10.0.8843.exe", p, o, c) },
            new("CompactGUI", "CompactGUI") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.CompactGuiLatest, "CompactGUI.exe", p, o, c) },
            new("Defragger", "Defragger") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.Defragger, "Defragger.exe", p, o, c) },
            new("Ninite Installer", "Ninite Installer") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.Ninite, "Ninite.exe", p, o, c) },
            new("Rufus", "Rufus") { ExecuteAction = DownloadRufus },
            new("Visual C++ AIO", "Visual C++ AIO Redistributable") { ExecuteAction = DownloadVCRedist },
            new("PC Repair Suite 2.0.0", "PC Repair Suite Portable") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.PCRepairSuite, "PC Repair Suite 2.0.0.exe", p, o, c) },
            new("Driver Booster PRO 13.4.0.234", "Driver Booster PRO Portable") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.IObitDriverBooster, "Driver Booster PRO 13.4.0.234.exe", p, o, c) },
            new("Dell OS Recovery Tool 2.3.4.3569", "Dell OS Recovery Tool Portable") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.DellOSRecoveryTool, "Dell OS Recovery Tool 2.3.4.3569.exe", p, o, c) },
            new("MacPaw CleanMyPC 1.11.1.2079", "MacPaw CleanMyPC Portable") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.CleanMyPc, "MacPaw CleanMyPC 1.11.1.2079.exe", p, o, c) }
        };


        private async Task DownloadRufus(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(0, "Getting latest Rufus release..."));
            try
            {
                // Get latest release info from GitHub API
                var response = await GetHttpClient().GetAsync(DownloadUrls.Rufus, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var releaseInfo = JsonDocument.Parse(json);
                var downloadUrl = "";
                foreach (var asset in releaseInfo.RootElement.GetProperty("assets").EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString();
                    if (name != null && name.EndsWith(".exe"))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                        break;
                    }
                }

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    progress.Report(new ProgressReport(100, "Failed"));
                    reportOutput("Could not find Rufus download URL.");
                    return;
                }

                await RecoveryCommander.Core.AsyncHelpers.DownloadAndExecuteAsync(downloadUrl, "Rufus.exe", progress, reportOutput, cancellationToken);
            }
            catch (Exception ex)
            {
                reportOutput($"Error: {ex.Message}");
                progress.Report(new ProgressReport(100, "Failed"));
            }
        }

        private async Task RunBackupActivation(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            // Use the standard security-validated download-and-execute path with .bat in allowed extensions
            await AsyncHelpers.DownloadAndExecuteAsync(
                DownloadUrls.BackupRestorePublic,
                "Backup-Activation.bat",
                progress,
                reportOutput,
                cancellationToken,
                allowedExtensions: new[] { "exe", "msi", "bat", "cmd", "ps1" });
        }

        private async Task DownloadVCRedist(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(0, "Getting latest Visual C++ AIO release..."));
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, DownloadUrls.VCRedistApi);
                request.Headers.Add("User-Agent", "RecoveryCommander");
                var response = await GetHttpClient().SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var releaseInfo = JsonDocument.Parse(json);
                var downloadUrl = "";
                foreach (var asset in releaseInfo.RootElement.GetProperty("assets").EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString();
                    if (name != null && name.StartsWith("VisualCppRedist_AIO_x86_x64") && name.EndsWith(".exe"))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                        break;
                    }
                }

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    progress.Report(new ProgressReport(100, "Failed"));
                    reportOutput("Could not find Visual C++ AIO download URL.");
                    return;
                }

                reportOutput($"Found latest release: {releaseInfo.RootElement.GetProperty("tag_name").GetString()}");
                await RecoveryCommander.Core.AsyncHelpers.DownloadAndExecuteAsync(downloadUrl, "VisualCppRedist_AIO_x86_x64.exe", progress, reportOutput, cancellationToken);
            }
            catch (Exception ex)
            {
                reportOutput($"Error: {ex.Message}");
                progress.Report(new ProgressReport(100, "Failed"));
            }
        }

    }
}
