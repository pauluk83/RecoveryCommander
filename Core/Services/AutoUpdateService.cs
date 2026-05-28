using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Core;

namespace RecoveryCommander.Core.Services
{
    /// <summary>
    /// Auto-update service that checks GitHub releases for the latest version,
    /// downloads the new executable, and launches a self-replacing updater.
    /// UI-independent core logic — dialog/presentation is handled by the main project.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class AutoUpdateService
    {
        // GitHub API endpoint for latest release
        private const string GitHubApiUrl = "https://api.github.com/repos/pauluk83/RecoveryCommander/releases/latest";
        private const string UserAgent = "RecoveryCommander-AutoUpdater";

        private static HttpClient GetHttpClient() => ServiceContainer.GetHttpClient();

        /// <summary>
        /// Result of an update check
        /// </summary>
        /// <summary>
        /// Result of an update check
        /// </summary>
        public sealed class UpdateCheckResult
        {
            public bool UpdateAvailable { get; init; }
            public string CurrentVersion { get; init; } = "";
            public string LatestVersion { get; init; } = "";
            public string DownloadUrl { get; init; } = "";
            public string ReleaseNotes { get; init; } = "";
            public string ReleaseName { get; init; } = "";
            public long AssetSize { get; init; }
            public DateTime? ReleaseDate { get; init; }
            public string ExpectedSha256 { get; init; } = "";
            public string? ErrorMessage { get; init; }
        }

        /// <summary>
        /// Gets the current application version from the assembly
        /// </summary>
        public static string GetCurrentVersion()
        {
            return CoreUtilities.GetApplicationVersion();
        }

        /// <summary>
        /// Checks GitHub for the latest release and determines if an update is available
        /// </summary>
        public static async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, GitHubApiUrl);
                request.Headers.Add("User-Agent", UserAgent);
                request.Headers.Add("Accept", "application/vnd.github.v3+json");

                var response = await GetHttpClient().SendAsync(request, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                using var release = JsonDocument.Parse(json);
                var root = release.RootElement;

                var tagName = root.GetProperty("tag_name").GetString() ?? "";
                var releaseName = root.GetProperty("name").GetString() ?? tagName;
                var releaseNotes = root.GetProperty("body").GetString() ?? "";
                
                DateTime? releaseDate = null;
                if (root.TryGetProperty("published_at", out var publishedAt) && 
                    DateTime.TryParse(publishedAt.GetString(), out var date))
                {
                    releaseDate = date.ToUniversalTime();
                }

                // Strip leading 'v' from tag (e.g., "v1.2.0" -> "1.2.0")
                var latestVersionStr = tagName.TrimStart('v', 'V');
                var currentVersionStr = GetCurrentVersion();

                // Find the exact RecoveryCommander .exe asset and a matching SHA-256 sidecar.
                string downloadUrl = "";
                string checksumUrl = "";
                long assetSize = 0;
                string assetName = "";

                foreach (var asset in root.GetProperty("assets").EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                        name.StartsWith("RecoveryCommander", StringComparison.OrdinalIgnoreCase))
                    {
                        assetName = name;
                        downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                        assetSize = asset.GetProperty("size").GetInt64();
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(assetName))
                {
                    foreach (var asset in root.GetProperty("assets").EnumerateArray())
                    {
                        var name = asset.GetProperty("name").GetString() ?? "";
                        if (name.Equals(assetName + ".sha256", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals(assetName + ".sha256.txt", StringComparison.OrdinalIgnoreCase) ||
                            (name.Contains(assetName, StringComparison.OrdinalIgnoreCase) &&
                             name.Contains("sha256", StringComparison.OrdinalIgnoreCase)))
                        {
                            checksumUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                            break;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(downloadUrl) && !SecurityHelpers.IsValidDownloadUrl(downloadUrl, out _))
                {
                    return new UpdateCheckResult
                    {
                        CurrentVersion = currentVersionStr,
                        LatestVersion = latestVersionStr,
                        ErrorMessage = "Update asset URL failed security validation."
                    };
                }

                string expectedSha256 = "";
                if (!string.IsNullOrWhiteSpace(checksumUrl))
                {
                    if (!SecurityHelpers.IsValidDownloadUrl(checksumUrl, out _))
                    {
                        return new UpdateCheckResult
                        {
                            CurrentVersion = currentVersionStr,
                            LatestVersion = latestVersionStr,
                            ErrorMessage = "Update checksum URL failed security validation."
                        };
                    }

                    expectedSha256 = await DownloadChecksumAsync(checksumUrl, cancellationToken).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(downloadUrl) && string.IsNullOrWhiteSpace(expectedSha256))
                {
                    return new UpdateCheckResult
                    {
                        CurrentVersion = currentVersionStr,
                        LatestVersion = latestVersionStr,
                        ErrorMessage = "Update found, but no SHA-256 checksum asset was published. Refusing automatic update."
                    };
                }

                bool updateAvailable = false;
                Version? latest = null;
                Version? current = null;

                try
                {
                    if (Version.TryParse(latestVersionStr, out latest) &&
                        Version.TryParse(currentVersionStr, out current))
                    {
                        if (latest > current)
                        {
                            updateAvailable = true;
                        }
                        else if (latest == current && releaseDate.HasValue)
                        {
                            // If versions are equal, fallback to date comparison
                            var localBuildDate = CoreUtilities.GetBuildDateUtc();
                            if (localBuildDate != DateTime.MinValue)
                            {
                                // Buffer of 1 minute to account for small jitter
                                updateAvailable = releaseDate.Value > localBuildDate.AddMinutes(1);
                            }
                        }
                    }
                }
                catch
                {
                    // If version parsing fails, do a simple string comparison
                    updateAvailable = !string.Equals(latestVersionStr, currentVersionStr, StringComparison.OrdinalIgnoreCase);
                }

                return new UpdateCheckResult
                {
                    UpdateAvailable = updateAvailable && !string.IsNullOrEmpty(downloadUrl),
                    CurrentVersion = currentVersionStr,
                    LatestVersion = latestVersionStr,
                    DownloadUrl = downloadUrl,
                    ReleaseNotes = releaseNotes,
                    ReleaseName = releaseName,
                    AssetSize = assetSize,
                    ReleaseDate = releaseDate,
                    ExpectedSha256 = expectedSha256
                };
            }
            catch (HttpRequestException ex)
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = GetCurrentVersion(),
                    ErrorMessage = $"Network error: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = GetCurrentVersion(),
                    ErrorMessage = "Update check timed out."
                };
            }
            catch (JsonException ex)
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = GetCurrentVersion(),
                    ErrorMessage = $"Release data parsing failed: {ex.Message}"
                };
            }
            catch (SecurityException ex)
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = GetCurrentVersion(),
                    ErrorMessage = $"Update security validation failed: {ex.Message}"
                };
            }
            catch (InvalidOperationException ex)
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = GetCurrentVersion(),
                    ErrorMessage = $"Invalid update state: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Downloads the update and launches the self-replacing updater script.
        /// Returns true if the update was applied and the app should exit.
        /// </summary>
        public static async Task<bool> DownloadAndApplyUpdateAsync(
            UpdateCheckResult updateInfo,
            IProgress<(int percent, string status)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!updateInfo.UpdateAvailable || string.IsNullOrEmpty(updateInfo.DownloadUrl))
                return false;
            if (string.IsNullOrWhiteSpace(updateInfo.ExpectedSha256))
            {
                progress?.Report((100, "Update verification metadata is missing."));
                return false;
            }
            if (!SecurityHelpers.IsValidDownloadUrl(updateInfo.DownloadUrl, out _))
            {
                progress?.Report((100, "Update URL failed security validation."));
                return false;
            }

            string? tempDir = null;

            try
            {
                // Create temp directory for the update
                tempDir = Path.Combine(Path.GetTempPath(), $"RecoveryCommander_Update_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                var downloadPath = Path.Combine(tempDir, "RecoveryCommander_New.exe");

                progress?.Report((5, "Downloading update..."));

                // Download the new exe with progress tracking
                using var request = new HttpRequestMessage(HttpMethod.Get, updateInfo.DownloadUrl);
                request.Headers.Add("User-Agent", UserAgent);

                using var response = await GetHttpClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? updateInfo.AssetSize;

                await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        int percent = (int)((double)totalRead / totalBytes * 85) + 5; // 5-90%
                        var sizeMb = totalRead / 1024.0 / 1024.0;
                        var totalMb = totalBytes / 1024.0 / 1024.0;
                        progress?.Report((Math.Min(percent, 90), $"Downloading... {sizeMb:F1} / {totalMb:F1} MB"));
                    }
                }

                progress?.Report((92, "Verifying download..."));

                // Verify the downloaded file exists and is reasonable size
                var downloadedFileInfo = new FileInfo(downloadPath);
                if (!downloadedFileInfo.Exists || downloadedFileInfo.Length < 1024)
                {
                    progress?.Report((100, "Download verification failed."));
                    return false;
                }

                var actualHash = await ComputeSha256Async(downloadPath, cancellationToken).ConfigureAwait(false);
                if (!actualHash.Equals(updateInfo.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    progress?.Report((100, "Update checksum verification failed."));
                    return false;
                }

                progress?.Report((95, "Preparing update script..."));

                // Get the current executable path
                var currentExePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(currentExePath))
                {
                    progress?.Report((100, "Could not determine current executable path."));
                    return false;
                }

                // Escape paths for batch script safety (% → %% to prevent env var expansion)
                static string EscapeBatchPath(string path) => path.Replace("%", "%%");
                // Sanitize version string to prevent injection via malicious release tags
                static string SanitizeVersion(string ver) => new string(ver.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray());

                var safeCurrentExe = EscapeBatchPath(currentExePath);
                var safeDownloadPath = EscapeBatchPath(downloadPath);
                var safeTempDir = EscapeBatchPath(tempDir);
                var safeVersion = SanitizeVersion(updateInfo.LatestVersion);

                // Create a batch script that waits for the current process to exit,
                // replaces the exe, then launches the new version
                var scriptPath = Path.Combine(tempDir, "update.bat");
                var script = $"""
                              @echo off
                              title RecoveryCommander Updater
                              echo.
                              echo =============================================
                              echo   RecoveryCommander Auto-Update
                              echo   Updating to version {safeVersion}
                              echo =============================================
                              echo.
                              echo Waiting for RecoveryCommander to close...
                              
                              :waitloop
                              tasklist /FI "PID eq {Environment.ProcessId}" 2>NUL | find /I "{Environment.ProcessId}" >NUL
                              if not errorlevel 1 (
                                  timeout /t 1 /nobreak >NUL
                                  goto waitloop
                              )
                              
                              echo Application closed. Applying update...
                              timeout /t 1 /nobreak >NUL
                              
                              echo Backing up current version...
                              copy /Y "{safeCurrentExe}" "{safeCurrentExe}.bak" >NUL 2>&1
                              
                              echo Replacing executable...
                              copy /Y "{safeDownloadPath}" "{safeCurrentExe}" >NUL 2>&1
                              if errorlevel 1 (
                                  echo.
                                  echo ERROR: Failed to replace executable.
                                  echo Restoring backup...
                                  copy /Y "{safeCurrentExe}.bak" "{safeCurrentExe}" >NUL 2>&1
                                  echo.
                                  echo Update failed. Press any key to exit.
                                  pause >NUL
                                  exit /b 1
                              )
                              
                              echo Update applied successfully!
                              echo.
                              echo Launching RecoveryCommander v{safeVersion}...
                              start "" "{safeCurrentExe}"
                              
                              echo Cleaning up...
                              timeout /t 3 /nobreak >NUL
                              del /Q "{safeCurrentExe}.bak" >NUL 2>&1
                              rmdir /S /Q "{safeTempDir}" >NUL 2>&1
                              exit /b 0
                              """;

                await File.WriteAllTextAsync(scriptPath, script, cancellationToken).ConfigureAwait(false);

                progress?.Report((98, "Launching updater..."));

                // Launch the updater script in a new process
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{scriptPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                Process.Start(psi);

                progress?.Report((100, "Update ready. Closing application..."));

                return true;
            }
            catch (IOException ex)
            {
                progress?.Report((100, $"Update failed (IO): {ex.Message}"));
                if (tempDir != null)
                {
                    try { Directory.Delete(tempDir, true); } catch (IOException) { }
                }
                return false;
            }
            catch (HttpRequestException ex)
            {
                progress?.Report((100, $"Update failed (Network): {ex.Message}"));
                return false;
            }
            catch (SecurityException ex)
            {
                progress?.Report((100, $"Update failed (Security): {ex.Message}"));
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                progress?.Report((100, $"Update failed (Access): {ex.Message}"));
                return false;
            }
        }

        private static async Task<string> DownloadChecksumAsync(string checksumUrl, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, checksumUrl);
            request.Headers.Add("User-Agent", UserAgent);

            using var response = await GetHttpClient().SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var match = Regex.Match(text, @"\b[a-fA-F0-9]{64}\b", RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                throw new SecurityException("Checksum asset did not contain a SHA-256 hash.");
            }

            return match.Value.ToLowerInvariant();
        }

        private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
        {
            await using var stream = File.OpenRead(path);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            return Convert.ToHexStringLower(hash);
        }
    }
}
