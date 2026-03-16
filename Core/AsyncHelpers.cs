using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Core
{
    /// <summary>
    /// Async helper utilities
    /// </summary>
    public static class AsyncHelpers
    {
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public static async Task CopyToAsyncWithProgress(Stream source, Stream destination, long? totalBytes, IProgress<ProgressReport> progress, CancellationToken cancellationToken)
        {
            var buffer = new byte[81920]; // 80KB buffer
            long totalRead = 0;
            int bytesRead;
            var stopwatch = Stopwatch.StartNew();
            long lastReportTime = 0;
            int lastPercentReported = -1;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalRead += bytesRead;

                // Calculate percent if possible
                int percent = -1;
                if (totalBytes.HasValue && totalBytes.Value > 0)
                {
                    percent = (int)Math.Clamp((totalRead * 100) / totalBytes.Value, 0, 100);
                }

                var elapsedMs = stopwatch.ElapsedMilliseconds;
                
                // Report if:
                // 1. We haven't reported in > 50ms
                // 2. The percentage has changed (only if determinate)
                // 3. This is the first chunk (to initialize UI state from 0)
                if ((elapsedMs - lastReportTime > 50) || 
                    (percent != -1 && percent != lastPercentReported))
                {
                    var elapsed = stopwatch.Elapsed.TotalSeconds;
                    var speed = elapsed > 0 ? totalRead / elapsed : 0;
                    
                    if (percent != -1)
                    {
                        string details = $"{FormatBytes(totalRead)} / {FormatBytes(totalBytes!.Value)} ({FormatBytes((long)speed)}/s)";
                        progress.Report(new ProgressReport(percent, "Downloading...", details));
                        lastPercentReported = percent;
                    }
                    else
                    {
                         string details = $"{FormatBytes(totalRead)} downloaded ({FormatBytes((long)speed)}/s)";
                         progress.Report(new ProgressReport(-1, "Downloading...", details));
                    }
                    lastReportTime = elapsedMs;
                }
            }
            // Ensure 100% is reported at the end of the stream copy
            if (totalBytes.HasValue)
            {
                 progress.Report(new ProgressReport(100, "Download complete", $"{FormatBytes(totalRead)} / {FormatBytes(totalRead)}"));
            }
        }
        /// <summary>
        /// Run a synchronous operation on a background thread
        /// </summary>
        public static Task<T> RunOnBackgroundThread<T>(Func<T> operation)
        {
            return Task.Run(operation);
        }

        /// <summary>
        /// Run a synchronous operation on a background thread
        /// </summary>
        public static Task RunOnBackgroundThread(Action operation)
        {
            return Task.Run(operation);
        }

        /// <summary>
        /// Execute operation with timeout
        /// </summary>
        public static async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeout)
        {
            using var cts = new System.Threading.CancellationTokenSource(timeout);
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
            
            if (completedTask == task)
            {
                return await task;
            }
            else
            {
                throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
            }
        }

        /// <summary>
        /// Execute operation with timeout
        /// </summary>
        public static async Task WithTimeout(Task task, TimeSpan timeout)
        {
            using var cts = new System.Threading.CancellationTokenSource(timeout);
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
            
            if (completedTask != task)
            {
                throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
            }
        }

        /// <summary>
        /// Write all text to file asynchronously
        /// </summary>
        public static async Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        {
            await File.WriteAllTextAsync(path, contents, cancellationToken);
        }

        /// <summary>
        /// Check if file exists asynchronously
        /// </summary>
        public static async Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => File.Exists(path), cancellationToken);
        }

        /// <summary>
        /// Delete file asynchronously
        /// </summary>
        public static async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => 
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Run process asynchronously with output reporting
        /// </summary>
        public static async Task RunProcessAsync(ProcessStartInfo psi, Action<string> reportOutput, Action<string>? reportError = null, CancellationToken cancellationToken = default)
        {
            using var process = new Process();
            process.StartInfo = psi;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.StandardOutputEncoding = System.Text.Encoding.Default;
            process.StartInfo.StandardErrorEncoding = System.Text.Encoding.Default;
            process.EnableRaisingEvents = true;

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            process.Exited += (s, e) => tcs.TrySetResult(true);

            process.Start();

            // Handle output and error via character-based consumption to support \r progress updates
            var outputTask = ConsumeStreamAsync(process.StandardOutput, reportOutput, cancellationToken);
            var errorTask = reportError != null 
                ? ConsumeStreamAsync(process.StandardError, reportError, cancellationToken)
                : Task.CompletedTask;

            // Register cancellation callback to kill the process
            using var registration = cancellationToken.Register(() => 
            {
                try 
                { 
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        tcs.TrySetCanceled(cancellationToken);
                    }
                } 
                catch { }
            });

            try
            {
                await Task.WhenAll(tcs.Task, outputTask, errorTask);

                if (process.ExitCode != 0 && !cancellationToken.IsCancellationRequested)
                {
                    throw new InvalidOperationException($"Process {psi.FileName} exited with code {process.ExitCode}");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }

        private static async Task ConsumeStreamAsync(StreamReader reader, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            var buffer = new System.Text.StringBuilder();
            var charBuffer = new char[1024];
            int bytesRead;

            try
            {
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
                                    reportOutput(line);
                                }
                                buffer.Clear();
                            }
                        }
                        else
                        {
                            buffer.Append(c);
                            
                            // If we see a percentage sign, it might be an in-place progress update
                            // Check the buffer for the latest percentage pattern
                            if (c == '%' && buffer.Length >= 2)
                            {
                                var currentContent = buffer.ToString();
                                // Look for the latest number preceding a % sign
                                var match = System.Text.RegularExpressions.Regex.Match(currentContent, @"(\d+)(?:\.\d+)?%", System.Text.RegularExpressions.RegexOptions.RightToLeft);
                                if (match.Success)
                                {
                                    // Report the line fragment as a "live" update
                                    reportOutput(currentContent.Trim());
                                }
                            }
                        }
                    }
                }
                
                if (buffer.Length > 0)
                {
                    var line = buffer.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(line)) reportOutput(line);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading process stream: {ex.Message}");
            }
        }

        /// <summary>
        /// Run a process synchronously and report its output, with cancellation support
        /// Consolidated from multiple modules - uses event-driven async reads to avoid deadlocks
        /// </summary>
        public static void RunProcessAndReport(ProcessStartInfo psi, Action<string> reportOutput, Func<bool> isCancelled)
        {
            try
            {
                using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

                // Use event-driven asynchronous reads to avoid potential deadlocks
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                // Store handlers for proper cleanup
                DataReceivedEventHandler? outputHandler = null;
                DataReceivedEventHandler? errorHandler = null;
                
                try
                {
                    outputHandler = (s, e) =>
                    {
                        try { if (e.Data != null) reportOutput(e.Data); } catch { }
                    };
                    errorHandler = (s, e) =>
                    {
                        try { if (e.Data != null) reportOutput("ERROR: " + e.Data); } catch { }
                    };
                    
                    proc.OutputDataReceived += outputHandler;
                    proc.ErrorDataReceived += errorHandler;

                    proc.Exited += (s, e) =>
                    {
                        try { tcs.TrySetResult(true); } catch { }
                    };

                    proc.Start();

                    try
                    {
                        if (proc.StartInfo.RedirectStandardOutput) proc.BeginOutputReadLine();
                        if (proc.StartInfo.RedirectStandardError) proc.BeginErrorReadLine();
                    }
                    catch (Exception ex)
                    {
                        reportOutput($"Failed to begin async read: {ex.Message}");
                    }

                    // Poll for cancellation while waiting for process to exit
                    while (!proc.HasExited)
                    {
                        if (isCancelled())
                        {
                            try { proc.Kill(entireProcessTree: true); } catch { }
                            break;
                        }
                        // Wait a short interval before checking again
                        if (tcs.Task.Wait(200)) break;
                    }

                    // Ensure the process has exited; give some time for async reads to flush
                    proc.WaitForExit(2000);

                    // Try to stop async reads gracefully
                    try { if (proc.StartInfo.RedirectStandardOutput) proc.CancelOutputRead(); } catch { }
                    try { if (proc.StartInfo.RedirectStandardError) proc.CancelErrorRead(); } catch { }
                }
                finally
                {
                    // Unsubscribe from events to prevent memory leaks
                    if (outputHandler != null)
                        proc.OutputDataReceived -= outputHandler;
                    if (errorHandler != null)
                        proc.ErrorDataReceived -= errorHandler;
                }
            }
            catch (Exception ex)
            {
                reportOutput($"Failed to run process {psi.FileName} {psi.Arguments}: {ex.Message}");
            }
        }

        /// <summary>
        /// Download a file to a specific path with progress reporting
        /// </summary>
        public static async Task DownloadFileAsync(string url, string destinationPath, IProgress<ProgressReport>? progress, CancellationToken cancellationToken)
        {
            var http = ServiceContainer.GetHttpClient();
            using (var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                resp.EnsureSuccessStatusCode();
                using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var contentLength = resp.Content.Headers.ContentLength;
                    using (var stream = await resp.Content.ReadAsStreamAsync(cancellationToken))
                    {
                        if (progress != null)
                        {
                            await CopyToAsyncWithProgress(stream, fs, contentLength, progress, cancellationToken);
                        }
                        else
                        {
                            await stream.CopyToAsync(fs, cancellationToken);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Download a file and execute it (with validation, temp file logic, and extension safety)
        /// </summary>
        public static async Task DownloadAndExecuteAsync(
            string url,
            string fileName,
            IProgress<ProgressReport>? progress,
            Action<string>? reportOutput,
            CancellationToken cancellationToken,
            string[]? allowedExtensions = null)
        {
            reportOutput?.Invoke($"Starting download from: {url}");
            progress?.Report(new ProgressReport(0, $"Downloading {fileName}...", "Connecting..."));
            try
            {
                // Validate URL
                if (!SecurityHelpers.IsValidDownloadUrl(url, out var validUri))
                {
                    reportOutput?.Invoke("Invalid or unsafe download URL.");
                    progress?.Report(new ProgressReport(100, "Failed"));
                    return;
                }
                // Validate/sanitize filename
                if (!SecurityHelpers.IsValidFileName(fileName, out var sanitizedFileName))
                {
                    reportOutput?.Invoke("Invalid filename.");
                    progress?.Report(new ProgressReport(100, "Failed"));
                    return;
                }
                // Only allow whitelisted extensions (default: exe, msi, bat, cmd, ps1)
                allowedExtensions ??= new[] { "exe", "msi", "bat", "cmd", "ps1" };
                if (!SecurityHelpers.IsAllowedFileExtension(sanitizedFileName, allowedExtensions))
                {
                    reportOutput?.Invoke("File extension not allowed.");
                    progress?.Report(new ProgressReport(100, "Failed"));
                    return;
                }
                var tempPath = Path.Combine(Path.GetTempPath(), sanitizedFileName);
                if (!SecurityHelpers.IsValidFilePath(tempPath, out var validatedPath))
                {
                    reportOutput?.Invoke("Invalid temp file path.");
                    progress?.Report(new ProgressReport(100, "Failed"));
                    return;
                }

                // Delete existing file if it exists to avoid corruption/lock issues
                try { if (File.Exists(validatedPath!)) File.Delete(validatedPath!); } catch { }

                var http = ServiceContainer.GetHttpClient();
                using (var resp = await http.GetAsync(validUri!, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    resp.EnsureSuccessStatusCode();
                    progress?.Report(new ProgressReport(1, $"Downloading {fileName}...", "Server responded, starting transfer..."));
                    
                    using var fs = new FileStream(validatedPath!, FileMode.Create, FileAccess.Write, FileShare.None);
                    
                    var contentLength = resp.Content.Headers.ContentLength;
                    using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                    
                    if (progress != null)
                    {
                        await CopyToAsyncWithProgress(stream, fs, contentLength, progress, cancellationToken);
                    }
                    else
                    {
                        await stream.CopyToAsync(fs, cancellationToken);
                    }
                }
                reportOutput?.Invoke($"Downloaded to: {validatedPath}");
                progress?.Report(new ProgressReport(80, "Download complete"));
                
                // Small delay to allow antivirus/SmartScreen to release the file handle lock after closing
                await Task.Delay(500, cancellationToken);

                ProcessStartInfo psi;
                string extension = Path.GetExtension(validatedPath!).ToLowerInvariant();

                if (extension == ".ps1")
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{validatedPath}\"",
                        UseShellExecute = true,
                        Verb = "runas",
                        WorkingDirectory = Path.GetDirectoryName(validatedPath!) ?? Path.GetTempPath()
                    };
                }
                else
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = validatedPath!,
                        UseShellExecute = true,
                        Verb = "runas",
                        WorkingDirectory = Path.GetDirectoryName(validatedPath!) ?? Path.GetTempPath()
                    };
                }

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        progress?.Report(new ProgressReport(100, "Launched"));
                        reportOutput?.Invoke($"{sanitizedFileName} launched successfully.");
                    }
                    else
                    {
                        progress?.Report(new ProgressReport(100, "Failed"));
                        reportOutput?.Invoke($"Failed to start {sanitizedFileName}.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                reportOutput?.Invoke("Download cancelled by user.");
                progress?.Report(new ProgressReport(100, "Cancelled"));
            }
            catch (Exception ex)
            {
                reportOutput?.Invoke($"Error: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Failed"));
            }
        }
    }
}
