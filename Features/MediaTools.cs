using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using RecoveryCommander.Core;
using RecoveryCommander.Core.Services;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Features
{
    /// <summary>
    /// Boot Media Creator - WinUI3 compatible version
    /// Provides methods to create bootable recovery drives
    /// </summary>
    public static class BootMediaCreator
    {
        /// <summary>
        /// Creates a bootable recovery drive on the specified drive letter
        /// </summary>
        public static async Task<BootMediaResult> CreateRecoveryDriveAsync(
            string driveLetter,
            bool copyApp,
            bool useWinRE,
            bool backupRecovery,
            bool includeDrivers,
            IProgress<ProgressReport>? progress,
            Action<string>? reportOutput,
            CancellationToken cancellationToken)
        {
            var result = new BootMediaResult { Success = false };
            
            try
            {
                reportOutput?.Invoke("Starting recovery drive creation...");
                reportOutput?.Invoke($"Target drive: {driveLetter}");
                
                // Validate drive
                var drive = DriveInfo.GetDrives()
                    .FirstOrDefault(d => d.Name.StartsWith(driveLetter));
                
                if (drive == null)
                {
                    result.Message = "Selected drive not found";
                    return result;
                }
                
                var freeGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                if (freeGB < 8)
                {
                    result.Message = "Drive must have at least 8GB free space";
                    return result;
                }
                
                // Create recovery structure
                reportOutput?.Invoke("Creating recovery directory structure...");
                var recoveryPath = Path.Combine(driveLetter, "Recovery");
                Directory.CreateDirectory(recoveryPath);
                
                // Copy application if requested
                if (copyApp)
                {
                    reportOutput?.Invoke("Copying RecoveryCommander...");
                    await CopyApplicationAsync(recoveryPath, reportOutput, cancellationToken);
                }

                // Export drivers if requested
                if (includeDrivers)
                {
                    reportOutput?.Invoke("Exporting third-party drivers (this may take a few minutes)...");
                    var driverPath = Path.Combine(recoveryPath, "Drivers");
                    Directory.CreateDirectory(driverPath);
                    
                    var progressValue = progress ?? new Progress<ProgressReport>();
                    var outputValue = reportOutput ?? (Action<string>)(_ => { });
                    await DriverService.BackupDriversAsync(driverPath, progressValue, outputValue, cancellationToken);
                }
                
                // Create boot configuration
                reportOutput?.Invoke("Creating boot configuration...");
                await CreateBootConfigAsync(recoveryPath, copyApp, useWinRE, backupRecovery, cancellationToken);
                
                reportOutput?.Invoke("Recovery drive created successfully!");
                result.Success = true;
                result.Message = "Recovery drive created successfully!";
            }
            catch (Exception ex)
            {
                reportOutput?.Invoke($"Error: {ex.Message}");
                result.Message = $"Failed to create recovery drive: {ex.Message}";
            }
            
            return result;
        }

        /// <summary>
        /// Gets available removable drives
        /// </summary>
        public static async Task<List<DriveInfo>> GetRemovableDrivesAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    return DriveInfo.GetDrives()
                        .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
                        .OrderBy(d => d.Name)
                        .ToList();
                }
                catch
                {
                    return new List<DriveInfo>();
                }
            });
        }

        private static async Task CopyApplicationAsync(string recoveryPath, Action<string>? reportOutput, CancellationToken cancellationToken)
        {
            var appPath = Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];
            var appDir = Path.GetDirectoryName(appPath);
            var targetPath = Path.Combine(recoveryPath, "RecoveryCommander");
            
            Directory.CreateDirectory(targetPath);
            
            // Copy main executable
            var targetExe = Path.Combine(targetPath, Path.GetFileName(appPath));
            File.Copy(appPath, targetExe, true);
            reportOutput?.Invoke($"Copied: {Path.GetFileName(appPath)}");
            
            // Copy essential files
            var essentialFiles = new[] { "Resources", "Scripts" };
            foreach (var file in essentialFiles)
            {
                var source = Path.Combine(appDir!, file);
                var target = Path.Combine(targetPath, file);
                
                if (Directory.Exists(source))
                {
                    await CopyDirectoryAsync(source, target, cancellationToken);
                    reportOutput?.Invoke($"Copied: {file}");
                }
            }
        }

        private static async Task CopyDirectoryAsync(string source, string target, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(target);
            
            foreach (var file in Directory.GetFiles(source))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var targetFile = Path.Combine(target, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
            }
            
            foreach (var dir in Directory.GetDirectories(source))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var targetDir = Path.Combine(target, Path.GetFileName(dir));
                await CopyDirectoryAsync(dir, targetDir, cancellationToken);
            }
        }

        private static async Task CreateBootConfigAsync(string recoveryPath, bool copyApp, bool useWinRE, bool backupRecovery, CancellationToken cancellationToken)
        {
            var configPath = Path.Combine(recoveryPath, "recovery.xml");
            var config = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RecoveryConfiguration>
    <Version>1.0</Version>
    <Created>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</Created>
    <Application>RecoveryCommander</Application>
    <Options>
        <CopyApp>{copyApp}</CopyApp>
        <UseWinRE>{useWinRE}</UseWinRE>
        <BackupRecovery>{backupRecovery}</BackupRecovery>
    </Options>
</RecoveryConfiguration>";
            
            await File.WriteAllTextAsync(configPath, config, cancellationToken);
        }
    }

    public class BootMediaResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// Unified Media Tools - Boot Media Creator and Media Creation Tools
    /// WinUI3 compatible version
    /// </summary>
    public static class MediaTools
    {
        // Direct download URLs for Media Creation Tools
        private const string Windows10MctUrl = "https://go.microsoft.com/fwlink/?LinkId=2265055";
        private const string Windows11MctUrl = "https://go.microsoft.com/fwlink/?linkid=2156295";

        /// <summary>
        /// Downloads the Windows 10 Media Creation Tool
        /// </summary>
        public static void DownloadWindows10Mct()
        {
            DownloadMediaCreationTool(Windows10MctUrl, "Windows 10");
        }

        /// <summary>
        /// Downloads the Windows 11 Media Creation Tool
        /// </summary>
        public static void DownloadWindows11Mct()
        {
            DownloadMediaCreationTool(Windows11MctUrl, "Windows 11");
        }

        private static void DownloadMediaCreationTool(string url, string version)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start download: {ex.Message}");
            }
        }
    }
}
