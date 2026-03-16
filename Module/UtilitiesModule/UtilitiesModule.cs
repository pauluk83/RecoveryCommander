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
    [RecoveryModule("UtilitiesModule", "1.0.0")]
    [SupportedOSPlatform("windows")]
    public class UtilitiesModule : IRecoveryModule
    {
        // ✓ Use shared HttpClient from ServiceContainer to prevent socket exhaustion
        private static HttpClient GetHttpClient() => RecoveryCommander.Core.ServiceContainer.GetHttpClient();

        // ✓ Extracted download URLs to static constants to reduce string allocations
        private static class DownloadUrls
        {
            public const string CompactGuiLatest = "https://github.com/IridiumIO/CompactGUI/releases/latest/download/CompactGUI.exe";
            public const string CCleaner = "https://a33b356a-b835-4066-889e-3d5811408855.filesusr.com/ugd/99ed68_1badceab0a0c4c65902f587b36b8d3be.txt?dn=ccleaner.txt";
            public const string Defragger = "https://drive.google.com/uc?export=download&id=1y-kGi-voJGMaT0KP8nzJ6Y5nuM9rIj4l";
            public const string Ninite = "https://drive.google.com/uc?export=download&id=1qIF8HXRBi7fdxI-ryOxJwG5UioROfMgx";
            public const string Rufus = "https://api.github.com/repos/pbatard/rufus/releases/latest";
            public const string MacriumPortable = "https://a33b356a-b835-4066-889e-3d5811408855.filesusr.com/ugd/99ed68_008a1fe1988b4d48afb25371d743c596.txt?dn=Macrium.txt";
            public const string Win11DebloatZip = "https://github.com/Raphire/Win11Debloat/archive/refs/heads/master.zip";
            public const string VCRedistApi = "https://api.github.com/repos/abbodi1406/vcredist/releases/latest";
            public const string PCRepairSuite = "https://a33b356a-b835-4066-889e-3d5811408855.filesusr.com/ugd/99ed68_97245b3f1c08429db4201b1cbd33e992.txt?dn=PCRepairSuite.txt";

            // User-hosted Office 2024 (Wix .txt file)
            public const string Office2024Wix = "https://99ed684e-f8f7-418e-a378-a43f97c53364.usrfiles.com/ugd/99ed68_c4c183f52c6442d1930577246c5ae215.txt";
            
            // Original GitHub-hosted Public Scripts
            public const string ActivationPublic = "https://get.activated.win";
            public const string OfficeC2RPublic = "https://c2rsetup.officeapps.live.com/c2r/download.aspx?ProductreleaseID=O365ProPlusRetail&platform=x64&language=en-us&version=O16GA";
            public const string BackupRestorePublic = "https://raw.githubusercontent.com/massgravel/Microsoft-Office-Activation-Scripts/master/Inof/Backup-Restore.ps1";
        }

        public string Name => "Utilities";
        public string Description => "Collection of utility tools for system maintenance and software installation";
        public string Version => "1.5.0";
        public string HealthStatus => "Healthy";
        public string BuildInfo => "UtilitiesModule (Activation button)";
        public bool SupportsAsync => true;

        public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
        {
            new("Activation", "Activation") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.ActivationPublic, "Activate.ps1", p, o, c) },
            new("Install Office 2024", "Install Office 2024") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.Office2024Wix, "Office2024.ps1", p, o, c) },
            new("Office-C2R-Install", "Install Office Click-to-Run") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.OfficeC2RPublic, "OfficeSetup.exe", p, o, c) },
            new("Backup and Restore Activation State", "Backup and Restore Activation State") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.BackupRestorePublic, "BackupRestore.ps1", p, o, c) },
            new("Christitus Utility", "Chris Titus Tech Windows Utility") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync("https://christitus.com/win", "Christitus.ps1", p, o, c) },
            new("CCleaner Portable", "CCleaner Portable") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.CCleaner, "CCleaner.exe", p, o, c) },
            new("Macrium Reflect Portable", "Macrium Reflect Portable") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.MacriumPortable, "Macrium.exe", p, o, c) },
            new("CompactGUI", "CompactGUI") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.CompactGuiLatest, "CompactGUI.exe", p, o, c) },
            new("Defragger", "Defragger") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.Defragger, "Defragger.exe", p, o, c) },
            new("Ninite Installer", "Ninite Installer") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.Ninite, "Ninite.exe", p, o, c) },
            new("Rufus", "Rufus") { ExecuteAction = DownloadRufus },
            new("Visual C++ AIO", "Visual C++ AIO Redistributable") { ExecuteAction = DownloadVCRedist },
            new("PC Repair Suite", "PC Repair Suite") { ExecuteAction = (p, o, c) => AsyncHelpers.DownloadAndExecuteAsync(DownloadUrls.PCRepairSuite, "PCRepairSuite.exe", p, o, c) }
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
