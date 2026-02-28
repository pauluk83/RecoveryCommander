using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;

namespace RecoveryCommander.Core
{
    /// <summary>
    /// Core utility functions for application
    /// </summary>
    public static class CoreUtilities
    {
        #region Application Information
        public static string GetApplicationVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        }

        public static string GetApplicationName()
        {
            return Assembly.GetExecutingAssembly().GetName().Name ?? "RecoveryCommander";
        }

        public static string GetBuildDate()
        {
            try
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    return File.GetLastWriteTime(exePath).ToString("yyyy-MM-dd HH:mm");
                }
            }
            catch { }
            return "Unknown";
        }
        #endregion

        #region File Operations
        /// <summary>
        /// Safely download and execute a file with security validation
        /// </summary>
        public static async Task DownloadAndExecuteAsync(
            string url,
            string? fileName = null,
            string[]? allowedExtensions = null,
            IProgress<ProgressReport>? progress = null,
            Action<string>? reportOutput = null,
            CancellationToken cancellationToken = default)
        {
            await AsyncHelpers.DownloadAndExecuteAsync(url, fileName ?? "download.exe", progress, reportOutput, cancellationToken, allowedExtensions);
        }
        #endregion

        #region Global Error Handling
        /// <summary>
        /// Handle global exceptions with logging
        /// </summary>
        public static void HandleGlobalException(Exception ex, string context = "")
        {
            var logger = ServiceContainer.GetService<ILogger>();
            var message = string.IsNullOrEmpty(context) ? ex.Message : $"{context}: {ex.Message}";
            
            logger?.LogError(ex, "Global exception handled: {Message}", message);
            
            // Also write to debug output
            System.Diagnostics.Debug.WriteLine($"GLOBAL ERROR: {message}");
        }

        /// <summary>
        /// Setup global exception handling
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static void SetupGlobalExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    HandleGlobalException(ex, "UnhandledException");
                }
            };

            if (OperatingSystem.IsWindows())
            {
                Application.ThreadException += (sender, e) =>
                {
                    HandleGlobalException(e.Exception, "ThreadException");
                };
            }
        }
        #endregion

        #region Safe File Operations
        /// <summary>
        /// Safely delete a file with error handling
        /// </summary>
        public static void SafeDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // Remove read-only attribute if present
                    var attributes = File.GetAttributes(filePath);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
                    }

                    File.Delete(filePath);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // File is in use or access denied - ignore for cleanup operations
            }
            catch (IOException)
            {
                // File is in use - ignore for cleanup operations
            }
            catch (Exception)
            {
                // Other errors - ignore for cleanup operations
            }
        }

        /// <summary>
        /// Safely delete a directory with error handling
        /// </summary>
        public static void SafeDeleteDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    // Remove read-only attribute if present
                    var attributes = File.GetAttributes(directoryPath);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(directoryPath, attributes & ~FileAttributes.ReadOnly);
                    }

                    Directory.Delete(directoryPath, true);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Directory is in use or access denied - ignore for cleanup operations
            }
            catch (IOException)
            {
                // Directory is in use - ignore for cleanup operations
            }
            catch (Exception)
            {
                // Other errors - ignore for cleanup operations
            }
        }
        #endregion

        #region Process Management
        public static bool RunProcess(string fileName, string arguments = "", bool runAsAdmin = false)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = runAsAdmin ? "runas" : ""
                };

                using var process = Process.Start(startInfo);
                return process != null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to run process {fileName}: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> RunProcessAsync(string fileName, string arguments = "", bool runAsAdmin = false)
        {
            try
            {
                return await Task.Run(() => RunProcess(fileName, arguments, runAsAdmin));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to run process {fileName}: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region System Information
        [SupportedOSPlatform("windows")]
        public static bool IsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsWindows10OrLater()
        {
            var version = Environment.OSVersion.Version;
            return version.Major >= 10;
        }
        #endregion



        #region System Core Operations

        /// <summary>
        /// Execute a command asynchronously and return the result
        /// </summary>
        public static async Task<CommandResult> ExecuteCommandAsync(string command, string arguments = "", int timeoutSeconds = 60, CancellationToken cancellationToken = default)
        {
            var result = new CommandResult();
            
            try
            {
                // Validate command path
                if (string.IsNullOrWhiteSpace(command))
                {
                    result.Success = false;
                    result.Error = "Command cannot be empty";
                    return result;
                }
                
                // Validate command path
                string validatedCommand;
                if (!Path.IsPathRooted(command) && !command.Contains(Path.DirectorySeparatorChar) && !command.Contains(Path.AltDirectorySeparatorChar))
                {
                     // Simple command name - just validate chars
                     if (!SecurityHelpers.IsValidFileName(command, out validatedCommand))
                     {
                        result.Success = false;
                        result.Error = "Invalid command name";
                        return result;
                     }
                }
                else if (!SecurityHelpers.IsValidFilePath(command, out validatedCommand))
                {
                    result.Success = false;
                    result.Error = "Invalid command path";
                    return result;
                }
                
                // Use arguments as-is safely with Process.Start (CreateProcess)
                // Sanitization is not applied automatically to avoid breaking complex arguments (URLs, etc.)
                // Callers constructing shell commands (cmd /c) must handle their own sanitization.
                var sanitizedArguments = arguments;
                
                using var process = new Process();
                process.StartInfo = CreateProcessInfo(validatedCommand, sanitizedArguments);
                
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();
                
                process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

                process.EnableRaisingEvents = true;
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                process.Exited += (s, e) => tcs.TrySetResult(true);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                try
                {
                    // Register cancellation to kill process
                    using var registration = timeoutCts.Token.Register(() =>
                    {
                        try { if (!process.HasExited) process.Kill(true); } catch { }
                    });

                    await tcs.Task.WaitAsync(timeoutCts.Token);

                    result.Success = process.ExitCode == 0;
                    result.ExitCode = process.ExitCode;
                    result.Output = outputBuilder.ToString();
                    result.Error = errorBuilder.ToString();
                }
                catch (OperationCanceledException)
                {
                    result.Success = false;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.Error = "Command cancelled by user";
                    }
                    else
                    {
                        result.Error = "Command timed out";
                    }
                    result.Output = outputBuilder.ToString();
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Create process info for command execution
        /// </summary>
        public static System.Diagnostics.ProcessStartInfo CreateProcessInfo(string fileName, string arguments, bool useShellExecute = false)
        {
            return new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = useShellExecute,
                RedirectStandardOutput = !useShellExecute,
                RedirectStandardError = !useShellExecute,
                CreateNoWindow = true
            };
        }
        #endregion
    }
}
