using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Globalization;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Modules
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

                _reportOutput($"> Target: {cloudPath}");
                string backupDir = Path.Combine(cloudPath, "RecoveryCommanderBackups");
                if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                string backupFile = Path.Combine(backupDir, $"ProfileBackup_{timestamp}.zip");
                string stagingDir = Path.Combine(Path.GetTempPath(), $"RC_Backup_Staging_{timestamp}");

                _progress.Report(new ProgressReport(10, "Scanning files..."));
                var sourceFolders = GetProfileFolders();

                if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);
                Directory.CreateDirectory(stagingDir);

                int i = 0;
                foreach (var folder in sourceFolders)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    string folderName = new DirectoryInfo(folder).Name;
                    _progress.Report(new ProgressReport(20 + (i * 10), $"Staging {folderName}..."));
                    _reportOutput($"Staging: {folder}");

                    await CopyDirectoryAsync(folder, Path.Combine(stagingDir, folderName), cancellationToken);
                    i++;
                }

                _progress.Report(new ProgressReport(60, "Compressing..."));
                _reportOutput($"> Creating: {backupFile}");

                await Task.Run(() => ZipFile.CreateFromDirectory(stagingDir, backupFile), cancellationToken);

                _progress.Report(new ProgressReport(90, "Cleaning up..."));
                if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);

                _progress.Report(new ProgressReport(100, "Done."));
                _reportOutput($"> Backup successful: {backupFile}");
            }
            catch (Exception ex)
            {
                _reportOutput($"> Error during backup: {ex.Message}");
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
                    _reportOutput("> No backups found.");
                    return;
                }

                var files = Directory.GetFiles(backupDir, "ProfileBackup_*.zip")
                                    .OrderByDescending(f => f)
                                    .ToList();

                if (files.Count == 0)
                {
                    _reportOutput("> No backup archives found.");
                    return;
                }

                string latestBackup = files.First();
                _reportOutput($"> Restoring from: {latestBackup}");

                string tempExtract = Path.Combine(Path.GetTempPath(), $"RC_Restore_{DateTime.Now.Ticks}");
                _progress.Report(new ProgressReport(20, "Extracting..."));

                await Task.Run(() => ZipFile.ExtractToDirectory(latestBackup, tempExtract), cancellationToken);

                _progress.Report(new ProgressReport(50, "Merging files..."));
                await MergeFoldersAsync(tempExtract, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), cancellationToken);

                _progress.Report(new ProgressReport(90, "Cleaning temporary files..."));
                if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true);

                _progress.Report(new ProgressReport(100, "Done."));
                _reportOutput("> Restore completed successfully.");
            }
            catch (Exception ex)
            {
                _reportOutput($"> Error during restore: {ex.Message}");
            }
        }

        private static string? GetCloudPath(string provider)
        {
            if (provider.Equals("OneDrive", StringComparison.OrdinalIgnoreCase))
            {
                return (string?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\OneDrive", "UserFolder", null)
                       ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive");
            }
            if (provider.Equals("Google Drive", StringComparison.OrdinalIgnoreCase))
            {
                // Try common paths
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] commonPaths = {
                    Path.Combine(userProfile, "Google Drive"),
                    Path.Combine(userProfile, "My Drive")
                };
                return commonPaths.FirstOrDefault(Directory.Exists);
            }
            return null;
        }

        private static List<string> GetProfileFolders()
        {
            return new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            }.Where(Directory.Exists).ToList();
        }

        private static async Task CopyDirectoryAsync(string source, string dest, CancellationToken ct)
        {
            Directory.CreateDirectory(dest);
            foreach (var file in Directory.GetFiles(source))
            {
                if (ct.IsCancellationRequested) return;
                string destFile = Path.Combine(dest, Path.GetFileName(file));
                await Task.Run(() => File.Copy(file, destFile, true), ct);
            }
            foreach (var dir in Directory.GetDirectories(source))
            {
                if (ct.IsCancellationRequested) return;
                await CopyDirectoryAsync(dir, Path.Combine(dest, Path.GetFileName(dir)), ct);
            }
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
                    await CopyDirectoryAsync(dir, targetPath, ct);
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
