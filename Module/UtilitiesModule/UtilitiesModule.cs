/*
 * AUDIT HEADER
 * File: UtilitiesModule.cs
 * Module: Utilities
 * Created: 2026-04-20
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-04-20 - 1.0.0 - Initial utility catalog (CCleaner, Macrium, Office, etc.).
 * 2026-05-02 - 1.2.6 - All download URLs migrated to Core/DownloadCatalog.cs so they can
 *                       carry SHA-256 hashes and version metadata in one place.
 */

using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;

namespace UtilitiesModule
{
    [RecoveryModule("UtilitiesModule")]
    [SupportedOSPlatform("windows")]
    public class UtilitiesModule : IRecoveryModule
    {
        // Use the shared HttpClient from ServiceContainer to prevent socket exhaustion.
        private static HttpClient GetHttpClient() => ServiceContainer.GetHttpClient();

        public string Name => "Utilities";
        public string Description => "Collection of utility tools for system maintenance and software installation";
        public string Version => GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0";
        public string HealthStatus => "Healthy";
        public string BuildInfo => "UtilitiesModule (DownloadCatalog-backed)";
        public bool SupportsAsync => true;

        public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
        {
            new("Activation",                                  "Activation")                                   { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.ActivationPublic", p, o, c) },
            new("Install Office 2024 (Build 2024)",            "Install Office 2024 (Build 2024)")             { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.Office2024", p, o, c) },
            new("Office-C2R-Install",                          "Install Office Click-to-Run")                  { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.OfficeC2RPublic", p, o, c) },
            new("Backup and Restore Activation State 1.0.0",   "Backup and Restore Activation State 1.0.0")    { ExecuteAction = RunBackupActivation },
            new("Christitus Utility",                          "Chris Titus Tech Windows Utility")              { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.ChrisTitusUtility", p, o, c) },
            new("CCleaner 6.40.115.62",                        "CCleaner portable")                             { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.CCleaner", p, o, c) },
            new("Macrium Reflect X 10.0.8843",                 "Macrium Reflect X Portable")                    { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.MacriumPortable", p, o, c) },
            new("CompactGUI",                                  "CompactGUI")                                    { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.CompactGUI", p, o, c) },
            new("Defragger",                                   "Defragger")                                     { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.Defragger", p, o, c) },
            new("Ninite Installer",                            "Ninite Installer")                              { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.Ninite", p, o, c) },
            new("Rufus",                                       "Rufus")                                         { ExecuteAction = DownloadRufus },
            new("Visual C++ AIO",                              "Visual C++ AIO Redistributable")                { ExecuteAction = DownloadVCRedist },
            new("PC Repair Suite 2.0.0",                       "PC Repair Suite Portable")                      { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.PCRepairSuite", p, o, c) },
            new("Driver Booster PRO 13.4.0.234",               "Driver Booster PRO Portable")                   { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.IObitDriverBooster", p, o, c) },
            new("Dell OS Recovery Tool 2.3.4.3569",            "Dell OS Recovery Tool Portable")                { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.DellOSRecoveryTool", p, o, c) },
            new("MacPaw CleanMyPC 1.11.1.2079",                "MacPaw CleanMyPC Portable")                     { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.CleanMyPc", p, o, c) },
            new("EaseUS Partition Master 20.3.0 Build 202604081519", "EaseUS Partition Master Portable")              { ExecuteAction = (p, o, c) => DownloadCatalog.DownloadAndExecuteFromCatalogAsync("Utilities.EaseUSPartitionMaster", p, o, c) },
            new("UniGetUI 2026.1.9",                           "UniGetUI (Package Manager UI)")                 { ExecuteAction = DownloadUniGetUI }
        };

        private async Task DownloadRufus(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(0, "Getting latest Rufus release..."));
            try
            {
                var apiUrl = DownloadCatalog.Get("Utilities.RufusReleaseApi").Url;
                using var response = await GetHttpClient().GetAsync(apiUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var releaseInfo = JsonDocument.Parse(json);
                var downloadUrl = "";
                foreach (var asset in releaseInfo.RootElement.GetProperty("assets").EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString();
                    if (name != null && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
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

                reportOutput("[supply-chain] WARN: Rufus is downloaded by latest-asset URL discovery; SHA-256 unpinned.");
                await AsyncHelpers.DownloadAndExecuteAsync(downloadUrl, "Rufus.exe", progress, reportOutput, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                reportOutput("Operation cancelled.");
                progress.Report(new ProgressReport(100, "Cancelled"));
            }
            catch (Exception ex)
            {
                reportOutput($"Error: {ex.Message}");
                progress.Report(new ProgressReport(100, "Failed"));
            }
        }

        private async Task RunBackupActivation(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            await DownloadCatalog.DownloadAndExecuteFromCatalogAsync(
                "Utilities.BackupRestoreActivation",
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
                var apiUrl = DownloadCatalog.Get("Utilities.VCRedistApi").Url;
                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Add("User-Agent", "RecoveryCommander");
                using var response = await GetHttpClient().SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var releaseInfo = JsonDocument.Parse(json);
                var downloadUrl = "";
                foreach (var asset in releaseInfo.RootElement.GetProperty("assets").EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString();
                    if (name != null && name.StartsWith("VisualCppRedist_AIO_x86_x64", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
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
                reportOutput("[supply-chain] WARN: VC++ Redist AIO is downloaded by latest-asset URL discovery; SHA-256 unpinned.");
                await AsyncHelpers.DownloadAndExecuteAsync(downloadUrl, "VisualCppRedist_AIO_x86_x64.exe", progress, reportOutput, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                reportOutput("Operation cancelled.");
                progress.Report(new ProgressReport(100, "Cancelled"));
            }
            catch (Exception ex)
            {
                reportOutput($"Error: {ex.Message}");
                progress.Report(new ProgressReport(100, "Failed"));
            }
        }

        private async Task DownloadUniGetUI(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(0, "Preparing UniGetUI..."));
            try
            {
                var entry = DownloadCatalog.Get("Utilities.UniGetUI");
                var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
                var downloadRoot = Path.Combine(Path.GetTempPath(), "RecoveryCommander_UniGetUI", uniqueId);
                var zipPath = Path.Combine(downloadRoot, entry.FileName);

                if (!Directory.Exists(downloadRoot)) Directory.CreateDirectory(downloadRoot);

                reportOutput($"Downloading UniGetUI {entry.Version}...");
                await DownloadCatalog.DownloadVerifiedAsync("Utilities.UniGetUI", zipPath, progress, reportOutput, cancellationToken);

                var extractPath = Path.Combine(downloadRoot, "Extracted");
                if (!Directory.Exists(extractPath)) Directory.CreateDirectory(extractPath);

                progress.Report(new ProgressReport(90, "Extracting UniGetUI..."));
                reportOutput($"Extracting to {extractPath}...");
                
                await Task.Run(() => 
                {
                    using (var archive = ZipFile.OpenRead(zipPath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            // ZipSlip protection: Ensure entry doesn't escape extractPath
                            var destination = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
                            if (!destination.StartsWith(Path.GetFullPath(extractPath), StringComparison.OrdinalIgnoreCase))
                            {
                                throw new System.Security.SecurityException($"ZipSlip attempt detected in entry: {entry.FullName}");
                            }

                            if (string.IsNullOrEmpty(entry.Name)) // It's a directory
                            {
                                Directory.CreateDirectory(destination);
                            }
                            else
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                                entry.ExtractToFile(destination, true);
                            }
                        }
                    }
                }, cancellationToken);

                var exePath = Path.Combine(extractPath, "UniGetUI.exe");
                if (!File.Exists(exePath))
                {
                    // Sometimes executables are in a subfolder within the ZIP
                    var files = Directory.GetFiles(extractPath, "UniGetUI.exe", SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        exePath = files[0];
                    }
                    else
                    {
                        throw new FileNotFoundException("Could not find UniGetUI.exe in the extracted files.");
                    }
                }

                progress.Report(new ProgressReport(95, "Launching UniGetUI..."));
                reportOutput($"Launching UniGetUI as admin: {exePath}");

                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = Path.GetDirectoryName(exePath)
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        progress.Report(new ProgressReport(100, "Launched"));
                        reportOutput("UniGetUI launched successfully.");
                    }
                    else
                    {
                        throw new InvalidOperationException("Failed to start UniGetUI process.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                reportOutput("Operation cancelled.");
                progress.Report(new ProgressReport(100, "Cancelled"));
            }
            catch (Exception ex)
            {
                reportOutput($"Error: {ex.Message}");
                progress.Report(new ProgressReport(100, "Failed"));
            }
        }
    }
}
