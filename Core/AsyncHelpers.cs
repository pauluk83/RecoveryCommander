/*
 * AUDIT HEADER
 * File: AsyncHelpers.cs
 * Module: Core
 * Created: 2026-04-20
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-04-20 - 1.0.0 - Initial async helper utilities.
 * 2026-05-22 - 1.2.7 - Added missing audit header and removed unused RunProcessAndReport.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Core
{
    /// <summary>
    /// Async helper utilities
    /// </summary>
    public static class AsyncHelpers
    {
        private static readonly System.Text.RegularExpressions.Regex PercentagePattern = new(@"(\d+)(?:\.\d+)?%", System.Text.RegularExpressions.RegexOptions.RightToLeft, TimeSpan.FromMilliseconds(100));

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
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);
            ArgumentNullException.ThrowIfNull(progress);

            var buffer = new byte[262144]; // 256KB buffer
            long totalRead = 0;
            int bytesRead;
            var stopwatch = Stopwatch.StartNew();
            long lastReportTime = 0;
            int lastPercentReported = -1;

            while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
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
        public static Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(File.Exists(path));
        }

        /// <summary>
        /// Delete file asynchronously
        /// </summary>
        public static async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Run process asynchronously with output reporting
        /// </summary>
        public static async Task RunProcessAsync(ProcessStartInfo psi, Action<string> reportOutput, Action<string>? reportError = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(psi);
            ArgumentNullException.ThrowIfNull(reportOutput);

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
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
            });

            try
            {
                await Task.WhenAll(tcs.Task, outputTask, errorTask).ConfigureAwait(false);

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
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(reportOutput);

            var buffer = new System.Text.StringBuilder();
            var charBuffer = new char[1024];
            int bytesRead;

            try
            {
                while ((bytesRead = await reader.ReadAsync(charBuffer, 0, charBuffer.Length).ConfigureAwait(false)) > 0)
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
                                try
                                {
                                    var match = PercentagePattern.Match(currentContent);
                                    if (match.Success)
                                    {
                                        reportOutput(currentContent.Trim());
                                    }
                                }
                                catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
                                {
                                    Debug.WriteLine("Regex timeout in percentage pattern matching");
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
            catch (IOException ex)
            {
                Debug.WriteLine($"IO Error reading process stream: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"Process Error reading process stream: {ex.Message}");
            }
        }

        private static string SanitizeUrlForReport(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "N/A";
            
            // Mask sensitive cloud storage links
            if (url.Contains("dropbox.com", StringComparison.OrdinalIgnoreCase)) return "[Secure Storage]";
            if (url.Contains("drive.google.com", StringComparison.OrdinalIgnoreCase)) return "[Secure Content Delivery (Google Drive)]";
            if (url.Contains("usrfiles.com", StringComparison.OrdinalIgnoreCase)) return "[Secure Content Delivery (Wix)]";
            
            return url;
        }

        /// <summary>
        /// Resolves the actual download URL if the provided URL points to a .txt file
        /// </summary>
        public static async Task<string> ResolveDownloadUrlAsync(string url, Action<string>? reportOutput, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(url);

            if (string.IsNullOrWhiteSpace(url)
                || !Uri.TryCreate(url, UriKind.Absolute, out var sourceUri)
                || !sourceUri.AbsolutePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            try
            {
                var http = ServiceContainer.GetHttpClient();
                // Use HeadersRead to check size first
                using var response = await http.GetAsync(sourceUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                // If the file is large, it's likely the actual installer (renamed to .txt)
                // We only treat small files as URL pointers/redirects.
                if (response.Content.Headers.ContentLength > 10240) // > 10KB
                {
                    return url;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                
                // Detection for InfinityFree/free.nf security challenge
                if (content.Contains("aes.js") && content.Contains("__test") && content.Contains("slowAES"))
                {
                    throw new InvalidOperationException("The download host (InfinityFree) is blocking the application. Please host your renamed .txt files on a service like Google Drive or Discord.");
                }

                var resolvedUrl = content.Trim();
                if (Uri.TryCreate(resolvedUrl, UriKind.Absolute, out _))
                {
                    if (!SecurityHelpers.IsValidDownloadUrl(resolvedUrl, out _))
                    {
                        throw new SecurityException("Resolved download URL is not HTTPS or resolves to a blocked/private host.");
                    }

                    reportOutput?.Invoke($"Resolved redirect URL: {SanitizeUrlForReport(resolvedUrl)}");
                    return resolvedUrl;
                }

                // If it's not a URL, it might be a script content (handled by the caller)
                return url;
            }
            catch (HttpRequestException ex)
            {
                reportOutput?.Invoke($"Warning: Failed to resolve URL from {SanitizeUrlForReport(url)}: {ex.Message}");
                return url;
            }
            catch (InvalidOperationException ex)
            {
                reportOutput?.Invoke($"Warning: Host blocked application for {SanitizeUrlForReport(url)}: {ex.Message}");
                return url;
            }
        }

        /// <summary>
        /// Download a file to a specific path with progress reporting and optional hash verification
        /// </summary>
        public static async Task DownloadFileAsync(string url, string destinationPath, IProgress<ProgressReport>? progress, CancellationToken cancellationToken, string? expectedHash = null)
        {
            ArgumentNullException.ThrowIfNull(url);
            ArgumentNullException.ThrowIfNull(destinationPath);

            if (!SecurityHelpers.IsValidDownloadUrl(url, out _))
            {
                throw new SecurityException("Unsafe download URL.");
            }

            var http = ServiceContainer.GetHttpClient();
            try
            {
                using var resp = await http.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();

                // Double check for InfinityFree challenge even in direct download
                if (resp.Content.Headers.ContentType?.MediaType == "text/html")
                {
                    // Peek at the content if it's suspiciously small or HTML
                    var peek = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    if (peek.Contains("aes.js") || peek.Contains("JavaScript to work"))
                    {
                        throw new InvalidOperationException("The download was blocked by the host's security challenge (InfinityFree).");
                    }
                }

                // Ensure parent directory exists
                var dir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var contentLength = resp.Content.Headers.ContentLength;
                    using (var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                    {
                        if (progress != null)
                        {
                            await CopyToAsyncWithProgress(stream, fs, contentLength, progress, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await stream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }

                // Verify hash if provided
                if (!string.IsNullOrWhiteSpace(expectedHash))
                {
                    progress?.Report(new ProgressReport(95, "Verifying integrity..."));
                    using var sha256 = System.Security.Cryptography.SHA256.Create();
                    using var stream = File.OpenRead(destinationPath);
                    byte[] hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
                    string actualHash = Convert.ToHexString(hashBytes); // Use uppercase for consistency
                    string cleanedExpectedHash = expectedHash.Trim();

                    if (!actualHash.Equals(cleanedExpectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        if (AppFeatureSettings.ShouldBypassDownloadVerification())
                        {
                            progress?.Report(new ProgressReport(95, "Hash verification mismatch ignored due to download override."));
                            return;
                        }

                        try { File.Delete(destinationPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                        throw new System.Security.SecurityException($"Hash mismatch! Expected: {cleanedExpectedHash}, Actual: {actualHash}");
                    }
                }
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (SecurityException)
            {
                throw;
            }
        }

        /// <summary>
        /// Download a file and execute it (with validation, unique temp paths, and extension safety)
        /// </summary>
        public static async Task DownloadAndExecuteAsync(
            string url,
            string fileName,
            IProgress<ProgressReport>? progress,
            Action<string>? reportOutput,
            CancellationToken cancellationToken,
            string[]? allowedExtensions = null,
            string? expectedHash = null)
        {
            reportOutput?.Invoke($"Starting download from: {SanitizeUrlForReport(url)}");
            progress?.Report(new ProgressReport(0, $"Downloading {fileName}...", "Connecting..."));
            try
            {
                // Validate URL
                if (!SecurityHelpers.IsValidDownloadUrl(url, out var validUri))
                {
                    reportOutput?.Invoke("Invalid or unsafe download URL.");
                    progress?.Report(new ProgressReport(100, "Failed"));
                    throw new SecurityException("Invalid or unsafe download URL.");
                }
                // Validate/sanitize filename
                if (!SecurityHelpers.IsValidFileName(fileName, out var sanitizedFileName))
                {
                    reportOutput?.Invoke("Invalid filename.");
                    progress?.Report(new ProgressReport(100, "Failed"));
                    throw new ArgumentException("Invalid filename.", nameof(fileName));
                }
                // Only allow whitelisted extensions (default: exe, msi, bat, cmd, ps1)
                allowedExtensions ??= new[] { "exe", "msi", "bat", "cmd", "ps1" };
                if (!SecurityHelpers.IsAllowedFileExtension(sanitizedFileName, allowedExtensions))
                {
                    reportOutput?.Invoke("File extension not allowed.");
                    progress?.Report(new ProgressReport(100, "Failed"));
                    throw new SecurityException("File extension not allowed.");
                }

                // Use a dedicated, unique subdirectory for this specific download to prevent race conditions
                var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
                var downloadRoot = Path.Combine(Path.GetTempPath(), "RecoveryCommander_Downloads", uniqueId);
                var tempPath = Path.Combine(downloadRoot, sanitizedFileName);
                
                if (!SecurityHelpers.IsValidFilePath(tempPath, out var validatedPath))
                {
                    reportOutput?.Invoke("Invalid temp file path.");
                    progress?.Report(new ProgressReport(100, "Failed"));
                    throw new SecurityException("Invalid temp file path.");
                }

                // Resolve the actual URL if it's a .txt redirect
                var resolvedUrl = await ResolveDownloadUrlAsync(url, reportOutput, cancellationToken).ConfigureAwait(false);

                // Download the file (includes hash verification)
                await DownloadFileAsync(resolvedUrl, validatedPath!, progress, cancellationToken, expectedHash).ConfigureAwait(false);
                
                reportOutput?.Invoke($"Downloaded to: {validatedPath}");
                progress?.Report(new ProgressReport(85, "Download complete"));

                ProcessStartInfo psi;
                string extension = Path.GetExtension(validatedPath!).ToUpperInvariant();

                if (extension == ".EXE" && !IsValidPortableExecutable(validatedPath!))
                {
                    reportOutput?.Invoke("Downloaded file does not appear to be a valid executable.");
                    progress?.Report(new ProgressReport(100, "Failed"));
                    throw new InvalidDataException("Downloaded file does not appear to be a valid executable.");
                }

                reportOutput?.Invoke("Preparing launch...");
                if (extension == ".PS1")
                {
                    // PowerShell script
                    var escapedPath = SecurityHelpers.EscapePowerShellArgument(validatedPath!);
                    psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File {escapedPath}",
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetDirectoryName(validatedPath!) ?? Path.GetTempPath()
                    };
                }
                else if (extension == ".MSI")
                {
                    // MSI Installer
                    psi = new ProcessStartInfo
                    {
                        FileName = "msiexec.exe",
                        Arguments = $"/i \"{validatedPath!}\"",
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetDirectoryName(validatedPath!) ?? Path.GetTempPath()
                    };
                }
                else if (extension is ".BAT" or ".CMD")
                {
                    // Batch scripts
                    psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{validatedPath!}\"",
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetDirectoryName(validatedPath!) ?? Path.GetTempPath()
                    };
                }
                else
                {
                    // EXE
                    psi = new ProcessStartInfo
                    {
                        FileName = validatedPath!,
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetDirectoryName(validatedPath!) ?? Path.GetTempPath()
                    };
                }

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    reportOutput?.Invoke($"{sanitizedFileName} launched successfully (PID: {proc.Id}).");
                }
                else
                {
                    reportOutput?.Invoke($"{sanitizedFileName} launched successfully.");
                }
                progress?.Report(new ProgressReport(100, "Launched"));
            }
            catch (OperationCanceledException)
            {
                reportOutput?.Invoke("Download cancelled by user.");
                progress?.Report(new ProgressReport(100, "Cancelled"));
                throw;
            }
            catch (IOException ex)
            {
                reportOutput?.Invoke($"File error: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Failed"));
                throw;
            }
            catch (InvalidDataException ex)
            {
                reportOutput?.Invoke($"Download validation failed: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Failed"));
                throw;
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                reportOutput?.Invoke($"Network error: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Failed"));
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                reportOutput?.Invoke($"Access denied: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Failed"));
                throw;
            }
            catch (SecurityException ex)
            {
                reportOutput?.Invoke($"Security validation failed: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Failed"));
                throw;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Outer catch for Win32 errors during download/validation (not launch — launch is caught above)
                reportOutput?.Invoke($"Execution blocked by OS or UAC: {ex.Message} (Code: {ex.NativeErrorCode})");
                progress?.Report(new ProgressReport(100, "Failed"));
                throw;
            }
            catch (Exception ex)
            {
                reportOutput?.Invoke($"Unexpected error: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    reportOutput?.Invoke($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
                progress?.Report(new ProgressReport(100, "Failed"));
                throw;
            }
        }

        private static bool IsValidPortableExecutable(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                Span<byte> header = stackalloc byte[2];
                return stream.Read(header) == 2 && header[0] == 0x4D && header[1] == 0x5A;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Executes a PowerShell command and streams the output directly to the provided action.
        /// Non-zero exit codes are reported but do not throw, since many PS commands return
        /// non-zero without indicating a fatal error (e.g. Get-Service for a missing service).
        /// </summary>
        public static async Task ExecutePowerShellCommandAsync(string command, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            var escapedCommand = command.Replace("\"", "\\\"");

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"{escapedCommand}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            try
            {
                await RunProcessAsync(psi, reportOutput, reportOutput, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("exited with code"))
            {
                // Non-zero exit code — already reported via stderr stream; swallow so the terminal stays usable
                reportOutput?.Invoke($"[PS] Command finished with: {ex.Message}");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                reportOutput?.Invoke($"[PS ERROR] Could not start PowerShell: {ex.Message} (Code: {ex.NativeErrorCode})");
            }
        }
    }
}
