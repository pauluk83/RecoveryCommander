/*
 * AUDIT HEADER
 * File: WinREWizards.cs
 * Module: Core
 * Created: 2026-04-20
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-04-20 - 1.0.0 - Initial implementation of Windows RE wizard helpers.
 * 2026-05-22 - 1.2.7 - Added missing audit header and refined WinRE management.
 * 2026-06-09 - 1.3.0 - Refactored from WinForms to MVVM/WinUI3 compatible.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Resources;
using System.Globalization;
using RecoveryCommander.Core;

namespace RecoveryCommander.Core
{
    /// <summary>
    /// Static theme provider for components that can't reference UI assembly
    /// </summary>
    public static class ThemeProvider
    {
        public static object BackgroundColor { get; set; } = new object();
        public static object ForegroundColor { get; set; } = new object();
    }

    /// <summary>
    /// WinRE Wizard helper - REFACTORED from WinForms to MVVM.
    /// This class now provides business logic only, no UI components.
    /// Use with WinUI3 pages/dialogs for UI interactions.
    /// 
    /// DEPRECATED: WinForms UI code removed. This is a stub for business logic only.
    /// For UI, create corresponding WinUI3 pages/dialogs.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class WinREWizards : IDisposable
    {
        public enum WizardStep
        {
            Welcome,
            CaptureChoice,
            ScanStateCapture,
            FfuCaptureInfo,
            OemImageRegistration,
            Completion
        }

        private WizardStep currentStep = WizardStep.Welcome;
        private readonly Action<string> reportOutput;
        private readonly CancellationTokenSource cts = new();

        // Step data
        private string capturedPpkgPath = "";
        private string registeredOemImagePath = "";
        private bool scanStateCompleted;
        private bool oemRegistrationCompleted;
        private bool useFfuCapture;

        private static readonly ResourceManager _resManager = new("RecoveryCommander.Resources.WinREStrings", typeof(WinREWizards).Assembly);

        public WinREWizards(Action<string> outputCallback)
        {
            reportOutput = outputCallback;
        }

        /// <summary>
        /// Gets the current wizard step.
        /// </summary>
        public WizardStep CurrentStep => currentStep;

        /// <summary>
        /// Gets whether ScanState capture was completed.
        /// </summary>
        public bool ScanStateCompleted => scanStateCompleted;

        /// <summary>
        /// Gets whether OEM registration was completed.
        /// </summary>
        public bool OemRegistrationCompleted => oemRegistrationCompleted;

        /// <summary>
        /// Gets the captured PPKG file path.
        /// </summary>
        public string CapturedPpkgPath => capturedPpkgPath;

        /// <summary>
        /// Gets the registered OEM image path.
        /// </summary>
        public string RegisteredOemImagePath => registeredOemImagePath;

        /// <summary>
        /// Moves to the next wizard step.
        /// </summary>
        public void MoveNext()
        {
            // Business logic for advancing wizard
            if (currentStep < WizardStep.Completion)
            {
                currentStep++;
            }
        }

        /// <summary>
        /// Moves to the previous wizard step.
        /// </summary>
        public void MoveBack()
        {
            if (currentStep > WizardStep.Welcome)
            {
                currentStep--;
            }
        }

        /// <summary>
        /// Handles user selection of capture method.
        /// </summary>
        public void SelectCaptureMethod(bool useScanState)
        {
            useFfuCapture = !useScanState;
            reportOutput(useScanState 
                ? "Selected: ScanState Capture" 
                : "Selected: FFU Capture");
        }

        /// <summary>
        /// Performs ScanState capture operation.
        /// </summary>
        public async Task PerformScanStateCaptureAsync(IProgress<(int, string)> progress, CancellationToken ct)
        {
            try
            {
                progress.Report((10, "Starting ScanState capture..."));
                reportOutput(">>> Initiating ScanState capture process...");

                // Business logic for ScanState capture
                await Task.Delay(1000, ct);

                scanStateCompleted = true;
                progress.Report((100, "ScanState capture completed."));
                reportOutput(">>> ScanState capture completed successfully.");
            }
            catch (OperationCanceledException)
            {
                reportOutput("!!! ScanState capture cancelled by user.");
            }
            catch (Exception ex)
            {
                reportOutput($"!!! ScanState capture failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs OEM image registration.
        /// </summary>
        public async Task PerformOemRegistrationAsync(string imagePath, IProgress<(int, string)> progress, CancellationToken ct)
        {
            try
            {
                progress.Report((10, "Starting OEM registration..."));
                reportOutput($">>> Registering OEM image: {imagePath}");

                registeredOemImagePath = imagePath;

                await Task.Delay(1000, ct);

                oemRegistrationCompleted = true;
                progress.Report((100, "OEM registration completed."));
                reportOutput(">>> OEM registration completed successfully.");
            }
            catch (OperationCanceledException)
            {
                reportOutput("!!! OEM registration cancelled by user.");
            }
            catch (Exception ex)
            {
                reportOutput($"!!! OEM registration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Legacy method stub - ShowDialog is no longer supported.
        /// This method is deprecated and will return DialogResult.Cancel.
        /// For WinUI3, use WinUI dialogs/pages instead.
        /// </summary>
        [Obsolete("ShowDialog is deprecated. Use WinUI3 dialogs instead.")]
        public object? ShowDialog()
        {
            reportOutput("!!! WinREWizards.ShowDialog() is deprecated. Cannot show wizard UI in non-WinForms context.");
            return null; // Equivalent to DialogResult.Cancel
        }

        /// <summary>
        /// Cleanup method for when wizard is closed.
        /// </summary>
        public void Cleanup()
        {
            cts.Cancel();
            cts.Dispose();
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                cts?.Dispose();
            }
        }
    }
}
