// Hidden SFC Module - Captures output directly without external window
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using System.Windows.Forms;
using System.Text;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("windows")]

namespace RecoveryCommander.Module
{
    [RecoveryModuleAttribute("SFCModule", "1.0.0")]
    public class SfcModuleHidden : IRecoveryModule
    {
        public string Name => "System File Checker";
        public string Description => "Performs System File Checker (SFC) operations to verify and repair system files.";

        public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
        {
            new("Scan Now", "Scan Now (/scannow)")
            {
                ExecuteAction = (p, o, c) => ExecuteActionSafeAsync("Scan Now", "/scannow", p, o, c)
            },
            new("Verify Only", "Verify Only (/verifyonly)")
            {
                ExecuteAction = (p, o, c) => ExecuteActionSafeAsync("Verify Only", "/verifyonly", p, o, c)
            },
            new("Offline Scan", "Offline Scan (/offbootdir + /offwindir)")
            {
                ExecuteAction = (p, o, c) => 
                {
                    var bootDir = PromptForFolder("Select boot directory (e.g. C:\\)");
                    var winDir = PromptForFolder("Select Windows directory (e.g. C:\\Windows)");
                    if (string.IsNullOrWhiteSpace(bootDir) || string.IsNullOrWhiteSpace(winDir)) return Task.CompletedTask;
                    return ExecuteActionSafeAsync("Offline Scan", $"/scannow /offbootdir={bootDir} /offwindir={winDir}", p, o, c);
                }
            }
        };

        public string Version => "1.0.4";
        public string HealthStatus => "Healthy";
        public string BuildInfo => "SFCModule (System File Checker)";
        public bool SupportsAsync => true;


        private async Task ExecuteActionSafeAsync(string actionName, string arguments, IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(0, $"Preparing {actionName}..."));

            try
            {
                await RunSfcHidden(arguments, progress ?? new Progress<ProgressReport>(), reportOutput, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                progress?.Report(new ProgressReport(100, "Cancelled"));
                reportOutput("Operation cancelled by user.");
                throw;
            }
            catch (Exception ex)
            {
                progress?.Report(new ProgressReport(100, "Error occurred"));
                reportOutput($"Error: {ex.Message}");
                reportOutput("Make sure RecoveryCommander is running as Administrator.");
            }
        }

        private async Task RunSfcHidden(string arguments, IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            reportOutput($"Running: sfc {arguments}");
            reportOutput("Capturing output directly to this panel...");
            reportOutput("");

            try
            {
                progress?.Report(new ProgressReport(1, "Starting SFC process..."));

                var sfcPath = GetSfcPath();
                var psi = new ProcessStartInfo
                {
                    FileName = sfcPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    StandardOutputEncoding = System.Text.Encoding.Default,
                    StandardErrorEncoding = System.Text.Encoding.Default
                };

                using var process = new Process { StartInfo = psi };
                
                reportOutput($"SFC process starting: {sfcPath} {arguments}");
                
                process.Start();
                reportOutput($"SFC process started (PID: {process.Id})");
                progress?.Report(new ProgressReport(2, "SFC process started"));

                var sharedProgress = 0;
                var outputTask = ReadStreamAsync(process.StandardOutput, reportOutput, progress ?? new Progress<ProgressReport>(), cancellationToken, p => sharedProgress = p, () => Math.Max(sharedProgress, 0));
                var errorTask = ReadStreamAsync(process.StandardError, reportOutput, progress ?? new Progress<ProgressReport>(), cancellationToken, p => sharedProgress = p, () => Math.Max(sharedProgress, 0));

                var startTime = DateTime.Now;

                // Monitor process for cancellation
                while (!process.HasExited)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        try 
                        { 
                            process.Kill(true); 
                            reportOutput("SFC process terminated by user.");
                        }
                        catch { }
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    await Task.Delay(1000, cancellationToken);
                }

                // Wait for output tasks to complete
                await Task.WhenAll(outputTask, errorTask);

                var exitCode = process.ExitCode;
                var totalTime = DateTime.Now - startTime;
                
                reportOutput("");
                reportOutput($"SFC process completed in {totalTime.TotalMinutes:F1} minutes");
                reportOutput($"Exit code: {exitCode}");

                if (exitCode == 0)
                {
                    progress?.Report(new ProgressReport(100, "SFC completed successfully"));
                    reportOutput("SFC scan completed successfully!");
                }
                else
                {
                    progress?.Report(new ProgressReport(100, $"SFC completed with exit code {exitCode}"));
                    reportOutput($"SFC completed with exit code {exitCode}");
                    
                    if (exitCode == 1)
                        reportOutput("Note: Exit code 1 may indicate corrupted files were found and repaired.");
                    else if (exitCode == 2)
                        reportOutput("Note: Exit code 2 may indicate corrupted files were found but could not be repaired.");
                    else if (exitCode == 3)
                        reportOutput("Note: Exit code 3 may indicate the scan could not be performed.");
                }
            }
            catch (Exception ex)
            {
                reportOutput($"Failed to run SFC: {ex.Message}");
                reportOutput("Make sure RecoveryCommander is running as Administrator.");
                throw;
            }
            finally
            {
                // Process cleanup handled by using block
            }
        }

        private async Task ReadStreamAsync(StreamReader reader, Action<string> reportOutput, IProgress<ProgressReport> progress, CancellationToken cancellationToken, Action<int> setProgress, Func<int> getProgress)
        {
            try
            {
                var buffer = new StringBuilder();
                var charBuffer = new char[1024];
                int bytesRead;

                while ((bytesRead = await reader.ReadAsync(charBuffer, 0, charBuffer.Length)) > 0)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    for (int i = 0; i < bytesRead; i++)
                    {
                        char c = charBuffer[i];
                        if (c == '\n' || c == '\r')
                        {
                            if (buffer.Length > 0)
                            {
                                var line = buffer.ToString().Trim();
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    var current = getProgress();
                                    ProcessSfcOutputLine(line, reportOutput, progress, ref current);
                                    setProgress(current);
                                }
                                buffer.Clear();
                            }
                        }
                        else
                        {
                            buffer.Append(c);
                            
                            // Check for mid-line percentage updates - look for the latest percentage in the buffer
                            if (c == '%' && buffer.Length >= 2)
                            {
                                var currentContent = buffer.ToString().Replace("\0", "").Replace(" ", "");
                                var match = System.Text.RegularExpressions.Regex.Match(currentContent, @"(\d+)(?:\.\d+)?%", System.Text.RegularExpressions.RegexOptions.RightToLeft);
                                if (match.Success && int.TryParse(match.Groups[1].Value, out var percent))
                                {
                                    var current = getProgress();
                                    if (percent > current) 
                                    {
                                        current = percent;
                                        progress?.Report(new ProgressReport(current, $"Scanning files: {percent}% complete"));
                                        setProgress(current);
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Process any remaining partial line
                if (buffer.Length > 0)
                {
                    var current = getProgress();
                    ProcessSfcOutputLine(buffer.ToString().Trim(), reportOutput, progress, ref current);
                    setProgress(current);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading SFC stream: {ex.Message}");
            }
        }

        private void ProcessSfcOutputLine(string line, Action<string> reportOutput, IProgress<ProgressReport> progress, ref int progressPercent)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            // Report the line to the output panel
            reportOutput(line);
            
            // Extract percentage - any line with a percentage is likely progress
            string cleanLine = line.Replace("\0", "").Replace(" ", "");
            var match = System.Text.RegularExpressions.Regex.Match(cleanLine, @"(\d+)(?:\.\d+)?%", System.Text.RegularExpressions.RegexOptions.RightToLeft);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var percent))
            {
                if (percent > progressPercent)
                {
                    progressPercent = percent;
                    progress?.Report(new ProgressReport(progressPercent, $"Scanning files: {percent}% complete"));
                }
                return;
            }

            // Update progress based on other SFC output patterns
            var lowerLine = line.ToLower();
            if (lowerLine.Contains("beginning system scan") || lowerLine.Contains("starting sfc") || lowerLine.Contains("starting system scan"))
            {
                progressPercent = Math.Max(progressPercent, 30);
                progress?.Report(new ProgressReport(progressPercent, "System scan started"));
            }
            else if (lowerLine.Contains("scan") && (lowerLine.Contains("complete") || lowerLine.Contains("finished") || lowerLine.Contains("completed")))
            {
                progressPercent = Math.Max(progressPercent, 95);
                progress?.Report(new ProgressReport(progressPercent, "Scan completing"));
            }
            else if (lowerLine.Contains("windows resource protection") && (lowerLine.Contains("found") || lowerLine.Contains("repair")))
            {
                progressPercent = Math.Max(progressPercent, 95);
                progress?.Report(new ProgressReport(progressPercent, "Processing scan results"));
            }
            else if (lowerLine.Contains("successfully") && lowerLine.Contains("repair"))
            {
                progressPercent = Math.Max(progressPercent, 98);
                progress?.Report(new ProgressReport(progressPercent, "Applying repairs"));
            }
        }

        private static string GetSfcPath()
        {
            var windir = Environment.GetEnvironmentVariable("windir") ?? @"C:\Windows";
            var sfcPath = Path.Combine(windir, "System32", "sfc.exe");
            
            // Check for 64-bit redirection
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                var sysnative = Path.Combine(windir, "SysNative", "sfc.exe");
                if (File.Exists(sysnative)) return sysnative;
            }
            
            return sfcPath;
        }

        private static string PromptForFile(string prompt)
        {
            using var dialog = new OpenFileDialog
            {
                Title = prompt,
                CheckFileExists = true,
                CheckPathExists = true
            };
            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : string.Empty;
        }

        private static string PromptForFolder(string prompt)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = prompt,
                ShowNewFolderButton = false
            };
            return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : string.Empty;
        }
    }
}
