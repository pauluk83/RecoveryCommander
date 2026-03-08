using System;
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
        public async Task ClearBrowserCachesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            var apps = new[] { "chrome", "msedge", "brave", "firefox" };
            int i = 0;
            foreach (var app in apps)
            {
                if (cancellationToken.IsCancellationRequested) break;
                float p = 10 + (i++ * 20);
                progress.Report(new ProgressReport((int)p, $"Cleaning {app} cache..."));
                // logic here
            }
        }

        public async Task ClearTempFilesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
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

        public async Task RunDiskCleanupAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress.Report(new ProgressReport(50, "Running Disk Cleanup (SageSet 65535)..."));
            var psi = CoreUtilities.CreateProcessInfo("cleanmgr.exe", "/sagerun:65535");
            await AsyncHelpers.RunProcessAsync(psi, reportOutput, null, cancellationToken);
        }

        public async Task DeepCleanWinSxSAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
             await DismHelper.RunDismAsync("/online /cleanup-image /startcomponentcleanup /resetbase", progress, reportOutput, cancellationToken);
        }
    }
}
