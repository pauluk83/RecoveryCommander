using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Module
{
    public class CloudProfileSyncService
    {
        private readonly IProgress<ProgressReport> _progress;
        private readonly Action<string> _reportOutput;

        public CloudProfileSyncService(IProgress<ProgressReport> progress, Action<string> reportOutput)
        {
            _progress = progress;
            _reportOutput = reportOutput;
        }

        public async Task BackupProfileAsync(string provider, CancellationToken cancellationToken)
        {
            try
            {
                string? cloudPath = GetCloudPath(provider);
                if (string.IsNullOrEmpty(cloudPath))
                {
                    _reportOutput($"> Error: Could not find home folder for provider: {provider}");
                    return;
                }

                // Check if the sync process is running
                if (!IsCloudProcessRunning(provider))
                {
                    _reportOutput($"> Warning: The {provider} sync process does not appear to be running. Files may not upload until it is started.");
                }

                _reportOutput($"> Target Cloud Folder: {cloudPath}");
                string backupDir = Path.Combine(cloudPath, "RecoveryCommanderBackups");
                if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFile = Path.Combine(backupDir, $"ProfileBackup_{timestamp}.zip");
                string stagingDir = Path.Combine(Path.GetTempPath(), $"RC_Backup_Staging_{timestamp}");

                _progress.Report(new ProgressReport(10, "Detecting profile settings..."));
                var sourceFolders = GetProfileFolders();

                if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);
                Directory.CreateDirectory(stagingDir);

                // Add Registry Export for Settings
                _progress.Report(new ProgressReport(15, "Exporting registry settings..."));
                try
                {
                    string regFile = Path.Combine(stagingDir, "RegistrySettings.reg");
                    // Export desktop and basic shell settings
                    var psi = new ProcessStartInfo("reg.exe", $"export \"HKCU\\Control Panel\\Desktop\" \"{regFile}\" /y") 
                    { 
                        CreateNoWindow = true, 
                        UseShellExecute = false 
                    };
                    Process.Start(psi)?.WaitForExit();
                }
                catch (Exception ex)
                {
                    _reportOutput($"> Warning: Could not export registry settings: {ex.Message}");
                }

                int i = 0;
                foreach (var folder in sourceFolders)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    
                    string folderName = new DirectoryInfo(folder).Name;
                    // If it's AppData, use a unique name
                    if (folder.Contains("AppData", StringComparison.OrdinalIgnoreCase))
                    {
                        folderName = "AppData_Config";
                    }

                    _progress.Report(new ProgressReport(20 + (i * 10), $"Staging {folderName}..."));
                    _reportOutput($"> Staging: {folder}");
                    
                    await CopyDirectoryResilientAsync(folder, Path.Combine(stagingDir, folderName), cancellationToken);
                    i++;
                }

                _progress.Report(new ProgressReport(80, "Compressing archive..."));
                _reportOutput($"> Creating backup archive: {Path.GetFileName(backupFile)}");
                
                await Task.Run(() => 
                {
                    if (File.Exists(backupFile)) File.Delete(backupFile);
                    ZipFile.CreateFromDirectory(stagingDir, backupFile);
                }, cancellationToken);

                _progress.Report(new ProgressReport(95, "Cleaning up staging area..."));
                if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);

                _progress.Report(new ProgressReport(100, "Done."));
                _reportOutput($"> Backup finalized locally. {provider} will now sync it to the cloud.");
            }
            catch (Exception ex)
            {
                _reportOutput($"> CRITICAL Error: {ex.Message}");
            }
        }

        public async Task RestoreProfileAsync(string provider, CancellationToken cancellationToken)
        {
            try 
            {
                string? cloudPath = GetCloudPath(provider);
                if (string.IsNullOrEmpty(cloudPath)) return;

                string backupDir = Path.Combine(cloudPath, "RecoveryCommanderBackups");
                if (!Directory.Exists(backupDir))
                {
                    _reportOutput("> No RecoveryCommander backups folder found on cloud.");
                    return;
                }

                var files = Directory.GetFiles(backupDir, "ProfileBackup_*.zip")
                                    .OrderByDescending(f => f)
                                    .ToList();

                if (!files.Any())
                {
                    _reportOutput("> No backup archives found in cloud folder.");
                    return;
                }

                string latestBackup = files.First();
                _reportOutput($"> Restoring latest: {Path.GetFileName(latestBackup)}");
                
                string tempExtract = Path.Combine(Path.GetTempPath(), $"RC_Restore_{DateTime.Now.Ticks}");
                _progress.Report(new ProgressReport(20, "Extracting..."));
                
                await Task.Run(() => ZipFile.ExtractToDirectory(latestBackup, tempExtract), cancellationToken);

                // Restore Registry Settings
                _progress.Report(new ProgressReport(40, "Applying registry settings..."));
                string regFile = Path.Combine(tempExtract, "RegistrySettings.reg");
                if (File.Exists(regFile))
                {
                    try
                    {
                        var psi = new ProcessStartInfo("reg.exe", $"import \"{regFile}\"") { CreateNoWindow = true, UseShellExecute = false };
                        Process.Start(psi)?.WaitForExit();
                    }
                    catch { /* Best effort */ }
                }

                _progress.Report(new ProgressReport(60, "Merging files..."));
                await MergeFoldersAsync(tempExtract, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), cancellationToken);

                _progress.Report(new ProgressReport(95, "Cleanup..."));
                if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true);

                _progress.Report(new ProgressReport(100, "Done."));
                _reportOutput("> Restore completed. Some settings may require a logoff to apply.");
            }
            catch (Exception ex)
            {
                _reportOutput($"> Restore Error: {ex.Message}");
            }
        }

        private string? GetCloudPath(string provider)
        {
            if (provider.StartsWith("OneDrive", StringComparison.OrdinalIgnoreCase))
            {
                // Try Personal
                string? personal = (string?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\OneDrive", "UserFolder", null);
                if (provider.Equals("OneDrive", StringComparison.OrdinalIgnoreCase) && personal != null) return personal;

                // Try Business Accounts
                try
                {
                    using var accountsKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\OneDrive\Accounts");
                    if (accountsKey != null)
                    {
                        foreach (var subKeyName in accountsKey.GetSubKeyNames())
                        {
                            if (subKeyName.StartsWith("Business"))
                            {
                                using var account = accountsKey.OpenSubKey(subKeyName);
                                string? path = (string?)account?.GetValue("UserFolder");
                                if (path != null)
                                {
                                    // If we're looking for a specific tagged one or just any business one
                                    if (provider.Contains("Business") || provider == "OneDrive (Work/School)") return path;
                                    return path; // Default to first found
                                }
                            }
                        }
                    }
                }
                catch { }

                // Fallback to default path
                string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive");
                if (Directory.Exists(defaultPath)) return defaultPath;
            }

            if (provider.Equals("Google Drive", StringComparison.OrdinalIgnoreCase))
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] commonPaths = {
                    Path.Combine(userProfile, "Google Drive"),
                    Path.Combine(userProfile, "My Drive"),
                    Path.Combine(userProfile, "Google Drive\\My Drive") // Newer GDrive Desktop versions
                };
                return commonPaths.FirstOrDefault(Directory.Exists);
            }
            return null;
        }

        private bool IsCloudProcessRunning(string provider)
        {
            try
            {
                string processName = "";
                if (provider.Contains("OneDrive")) processName = "OneDrive";
                else if (provider.Contains("Google Drive")) processName = "GoogleDriveFS"; // Google Drive Desktop

                if (string.IsNullOrEmpty(processName)) return true;
                return Process.GetProcessesByName(processName).Any();
            }
            catch { return true; }
        }

        private List<string> GetProfileFolders()
        {
            var folders = new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            // Add specific app configs (Settings)
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string startMenu = Path.Combine(appData, @"Microsoft\Windows\Start Menu");
            if (Directory.Exists(startMenu)) folders.Add(startMenu);

            return folders.Where(Directory.Exists).Distinct().ToList();
        }

        private async Task CopyDirectoryResilientAsync(string source, string dest, CancellationToken ct)
        {
            try 
            {
                Directory.CreateDirectory(dest);
                
                foreach (var file in Directory.GetFiles(source))
                {
                    if (ct.IsCancellationRequested) return;
                    try
                    {
                        string destFile = Path.Combine(dest, Path.GetFileName(file));
                        await Task.Run(() => File.Copy(file, destFile, true), ct);
                    }
                    catch (IOException) { /* File in use, skip */ }
                    catch (UnauthorizedAccessException) { /* No access, skip */ }
                }

                foreach (var dir in Directory.GetDirectories(source))
                {
                    if (ct.IsCancellationRequested) return;
                    // Skip hidden/system folders to avoid circular/junk refs
                    var info = new DirectoryInfo(dir);
                    if ((info.Attributes & FileAttributes.System) != 0) continue;

                    await CopyDirectoryResilientAsync(dir, Path.Combine(dest, Path.GetFileName(dir)), ct);
                }
            }
            catch { /* Skip folder if inaccessible */ }
        }

        private async Task MergeFoldersAsync(string source, string targetBase, CancellationToken ct)
        {
            foreach (var dir in Directory.GetDirectories(source))
            {
                if (ct.IsCancellationRequested) return;
                string folderName = new DirectoryInfo(dir).Name;
                string targetPath = Path.Combine(targetBase, folderName);
                
                if (Directory.Exists(targetPath))
                {
                    _reportOutput($"Merging: {folderName}");
                    await CopyDirectoryResilientAsync(dir, targetPath, ct);
                }
            }
        }

        public List<string> DetectAvailableProviders()
        {
            var providers = new List<string>();
            if (!string.IsNullOrEmpty(GetCloudPath("OneDrive"))) providers.Add("OneDrive");
            if (!string.IsNullOrEmpty(GetCloudPath("Google Drive"))) providers.Add("Google Drive");
            return providers;
        }
    }
}
