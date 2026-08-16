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
        private const int MaxBackupEntries = 100_000;
        private const long MaxBackupUncompressedBytes = 2L * 1024 * 1024 * 1024;
        private static readonly HashSet<string> AllowedRestoreFolders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Desktop",
            "Documents",
            "Pictures"
        };

        private readonly IProgress<ProgressReport> _progress;
        private readonly Action<string> _reportOutput;

        public CloudProfileSyncService(IProgress<ProgressReport> progress, Action<string> reportOutput)
        {
            _progress = progress;
            _reportOutput = reportOutput;
        }

        public async Task BackupProfileAsync(string provider, CancellationToken cancellationToken)
        {
            string? stagingDir = null;
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
                stagingDir = Path.Combine(Path.GetTempPath(), $"RC_Backup_Staging_{timestamp}");

                _progress.Report(new ProgressReport(10, "Scanning files..."));
                var sourceFolders = GetProfileFolders();

                if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);
                Directory.CreateDirectory(stagingDir);

                int i = 0;
                foreach (var folder in sourceFolders)
                {
                    cancellationToken.ThrowIfCancellationRequested();

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

                _progress.Report(new ProgressReport(100, "Done."));
                _reportOutput($"> Backup successful: {backupFile}");
            }
            catch (OperationCanceledException)
            {
                _progress.Report(new ProgressReport(100, "Cancelled."));
                _reportOutput("> Backup cancelled.");
            }
            catch (Exception ex)
            {
                _reportOutput($"> Error during backup: {ex.Message}");
            }
            finally
            {
                TryDeleteDirectory(stagingDir);
            }
        }

        public async Task RestoreProfileAsync(string provider, CancellationToken cancellationToken)
        {
            string? tempExtract = null;
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

                var files = Directory.GetFiles(backupDir, "ProfileBackup_*.zip");
                var latestFile = files.OrderByDescending(f => f).FirstOrDefault();

                if (string.IsNullOrEmpty(latestFile))
                {
                    _reportOutput("> No backup archives found.");
                    return;
                }

                string latestBackup = files.First();
                _reportOutput($"> Restoring from: {latestBackup}");

                tempExtract = Path.Combine(Path.GetTempPath(), $"RC_Restore_{DateTime.Now.Ticks}");
                _progress.Report(new ProgressReport(20, "Extracting..."));

                await Task.Run(() => ExtractBackupSafely(latestBackup, tempExtract), cancellationToken);

                _progress.Report(new ProgressReport(50, "Merging files..."));
                await MergeFoldersAsync(tempExtract, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), cancellationToken);

                _progress.Report(new ProgressReport(90, "Cleaning temporary files..."));

                _progress.Report(new ProgressReport(100, "Done."));
                _reportOutput("> Restore completed successfully.");
            }
            catch (OperationCanceledException)
            {
                _progress.Report(new ProgressReport(100, "Cancelled."));
                _reportOutput("> Restore cancelled.");
            }
            catch (Exception ex)
            {
                _reportOutput($"> Error during restore: {ex.Message}");
            }
            finally
            {
                TryDeleteDirectory(tempExtract);
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
                ct.ThrowIfCancellationRequested();
                string destFile = Path.Combine(dest, Path.GetFileName(file));
                await Task.Run(() => File.Copy(file, destFile, true), ct);
            }
            foreach (var dir in Directory.GetDirectories(source))
            {
                ct.ThrowIfCancellationRequested();
                await CopyDirectoryAsync(dir, Path.Combine(dest, Path.GetFileName(dir)), ct);
            }
        }

        private async Task MergeFoldersAsync(string source, string targetBase, CancellationToken ct)
        {
            foreach (var dir in Directory.GetDirectories(source))
            {
                ct.ThrowIfCancellationRequested();
                string folderName = new DirectoryInfo(dir).Name;
                if (!AllowedRestoreFolders.Contains(folderName))
                {
                    _reportOutput($"Skipping unexpected restore folder: {folderName}");
                    continue;
                }

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

        private static void ExtractBackupSafely(string zipPath, string destinationDirectory)
        {
            Directory.CreateDirectory(destinationDirectory);
            var root = Path.GetFullPath(destinationDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            long totalUncompressedBytes = 0;
            var entryCount = 0;

            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                entryCount++;
                if (entryCount > MaxBackupEntries)
                {
                    throw new InvalidDataException("Backup archive contains too many entries.");
                }

                totalUncompressedBytes += entry.Length;
                if (totalUncompressedBytes > MaxBackupUncompressedBytes)
                {
                    throw new InvalidDataException("Backup archive is too large to restore safely.");
                }

                if (Path.IsPathRooted(entry.FullName))
                {
                    throw new InvalidDataException("Backup archive contains a rooted entry.");
                }

                var destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName));
                if (!destinationPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("Backup archive contains an unsafe path.");
                }

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: false);
                }
            }
        }

        private static void TryDeleteDirectory(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
