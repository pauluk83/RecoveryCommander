using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class CleanupService
    {
        public static async Task ClearBrowserCachesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appPaths = new Dictionary<string, string>
            {
                { "Chrome", Path.Combine(appData, @"Google\Chrome\User Data\Default\Cache") },
                { "Edge", Path.Combine(appData, @"Microsoft\Edge\User Data\Default\Cache") },
                { "Brave", Path.Combine(appData, @"BraveSoftware\Brave-Browser\User Data\Default\Cache") },
                { "Firefox", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Mozilla\Firefox\Profiles") }
            };

            int i = 0;
            foreach (var kvp in appPaths)
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                int p = 10 + (i++ * 20);
                progress.Report(new ProgressReport(p, $"Cleaning {kvp.Key} cache..."));
                
                try
                {
                    if (kvp.Key == "Firefox")
                    {
                        // Firefox cache is in Local appdata, profiles are in Roaming
                        var firefoxLocal = Path.Combine(appData, @"Mozilla\Firefox\Profiles");
                        if (Directory.Exists(firefoxLocal))
                        {
                            foreach (var profile in Directory.GetDirectories(firefoxLocal))
                            {
                                var cacheDir = Path.Combine(profile, "cache2");
                                if (Directory.Exists(cacheDir))
                                {
                                    reportOutput($"Cleaning Firefox cache: {cacheDir}");
                                    await Task.Run(() => SafeDeleteDirectoryContents(cacheDir), cancellationToken);
                                }
                            }
                        }
                    }
                    else if (Directory.Exists(kvp.Value))
                    {
                        reportOutput($"Cleaning {kvp.Key} cache: {kvp.Value}");
                        await Task.Run(() => SafeDeleteDirectoryContents(kvp.Value), cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    reportOutput($"Warning: Failed to clean {kvp.Key} cache: {ex.Message}");
                }
            }
        }

        private static void SafeDeleteDirectoryContents(string path)
        {
            try
            {
                if (!Directory.Exists(path)) return;

                // Delete all files in the current directory
                foreach (var file in Directory.GetFiles(path))
                {
                    CoreUtilities.SafeDeleteFile(file);
                }

                // Delete all subdirectories recursively
                foreach (var dir in Directory.GetDirectories(path))
                {
                    CoreUtilities.SafeDeleteDirectory(dir);
                }
            }
            catch { }
        }

        public static async Task ClearTempFilesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(10, "Cleaning Temp files..."));
            string[] dirs = {
                Path.GetTempPath(),
                Environment.ExpandEnvironmentVariables("%windir%\\Temp"),
                Environment.ExpandEnvironmentVariables("%windir%\\Prefetch"),
                Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Temp")
            };

            foreach (var dir in dirs)
            {
                if (Directory.Exists(dir))
                {
                    reportOutput($"Cleaning: {dir}");
                    await Task.Run(() => 
                    {
                        foreach (var file in Directory.GetFiles(dir))
                        {
                            try { File.Delete(file); } catch { }
                        }
                        foreach (var sub in Directory.GetDirectories(dir))
                        {
                            try { Directory.Delete(sub, true); } catch { }
                        }
                    }, cancellationToken);
                }
            }
        }

        public static async Task RunDiskCleanupAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(50, "Running Disk Cleanup (SageSet 65535)..."));
            var psi = CoreUtilities.CreateProcessInfo("cleanmgr.exe", "/sagerun:65535");
            await AsyncHelpers.RunProcessAsync(psi, reportOutput, null, cancellationToken);
        }

        public static async Task DeepCleanWinSxSAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
             await DismHelper.RunDismAsync("/online /cleanup-image /startcomponentcleanup /resetbase", progress, reportOutput, cancellationToken);
        }
    }
}
