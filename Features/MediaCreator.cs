using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Core;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Features
{
    /// <summary>
    /// Media Creation Tools - WinUI3 compatible version
    /// Provides methods to download Microsoft Media Creation Tools
    /// </summary>
    public static class MediaCreator
    {
        // Official Microsoft download pages (recommended)
        private const string Windows10Page = "https://www.microsoft.com/en-us/software-download/windows10";
        private const string Windows11Page = "https://www.microsoft.com/software-download/windows11";

        // Known redirect links for Media Creation Tools (updated 2024)
        private const string Windows10Mct = "https://go.microsoft.com/fwlink/?LinkId=2265055"; // Updated Windows 10 MCT link
        // Windows 11 Media Creation Tool direct fwlink (updated 2024) - use redirect
        private const string Windows11Mct = "https://go.microsoft.com/fwlink/?linkid=2171764"; // Updated Windows 11 MCT link

        /// <summary>
        /// Downloads and executes the Windows 10 Media Creation Tool
        /// </summary>
        public static async Task DownloadWindows10MctAsync(IProgress<ProgressReport>? progress, Action<string>? reportOutput, CancellationToken cancellationToken)
        {
            await DownloadAndOfferAsync(Windows10Mct, "MediaCreationTool.exe", progress, reportOutput, cancellationToken);
        }

        /// <summary>
        /// Downloads and executes the Windows 11 Media Creation Tool
        /// </summary>
        public static async Task DownloadWindows11MctAsync(IProgress<ProgressReport>? progress, Action<string>? reportOutput, CancellationToken cancellationToken)
        {
            await DownloadAndOfferAsync(Windows11Mct, "MediaCreationToolWindows11.exe", progress, reportOutput, cancellationToken);
        }

        private static void OpenUrl(string url)
        {
            if (!SecurityHelpers.IsValidDownloadUrl(url, out _)) return;
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                reportOutput?.Invoke($"Failed to open browser: {ex.Message}");
            }
        }

        private static Action<string>? reportOutput;

        // Cancelled when the host closes so MCT downloads don't outlive the dialog.
        private static CancellationTokenSource? _activeDownloadCts;

        private static async Task DownloadAndOfferAsync(string url, string defaultFileName, IProgress<ProgressReport>? progress, Action<string>? output, CancellationToken cancellationToken)
        {
            reportOutput = output;
            
            // Replace any prior in-flight token (close/cancel previous if user re-clicks).
            try { _activeDownloadCts?.Cancel(); } catch { /* ignore */ }
            _activeDownloadCts?.Dispose();
            _activeDownloadCts = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _activeDownloadCts.Token).Token;

            try
            {
                await CoreUtilities.DownloadAndExecuteAsync(
                    url: url,
                    fileName: defaultFileName,
                    allowedExtensions: null,
                    progress: progress,
                    reportOutput: output,
                    cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
                output?.Invoke("Download cancelled.");
            }
            catch (Exception ex)
            {
                output?.Invoke($"Download/Execute failed: {ex.Message}\nOpening official download page instead.");
                OpenUrl(url);
            }
        }
    }
}
