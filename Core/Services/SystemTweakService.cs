/*
 * AUDIT HEADER
 * File: SystemTweakService.cs
 * Module: Core / Services
 * Created: 2026-04-22
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-04-22 - 1.1.0 - Initial telemetry/web-search registry tweaks.
 * 2026-05-02 - 1.3.0 - Cancellation now observed between each registry write so partial
 *                       cancel works mid-operation. Failed writes surface to reportOutput
 *                       (was previously silent on the user-visible side).
 */

using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Core.Services
{
    [SupportedOSPlatform("windows")]
    public class SystemTweakService
    {
        public Task DisableTelemetryAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);

            progress.Report(new ProgressReport(10, "Disabling telemetry..."));
            var ok1 = SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord, reportOutput);

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(new ProgressReport(60, "Applying secondary telemetry policy..."));
            var ok2 = SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord, reportOutput);

            if (ok1 && ok2)
            {
                reportOutput("Telemetry disabled in registry.");
            }
            else
            {
                reportOutput("Telemetry tweak completed with one or more registry write failures (see log).");
            }
            progress.Report(new ProgressReport(100, "Done"));
            return Task.CompletedTask;
        }

        public Task DisableWebSearchAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);

            progress.Report(new ProgressReport(50, "Disabling Bing search in Start..."));
            var ok = SetRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord, reportOutput);

            reportOutput(ok
                ? "Web search disabled in Explorer policies."
                : "Web search tweak failed (see log).");

            progress.Report(new ProgressReport(100, "Done"));
            return Task.CompletedTask;
        }

        private bool SetRegistryValue(RegistryKey root, string keyPath, string valueName, object value, RegistryValueKind kind, Action<string>? reportOutput = null)
        {
            try
            {
                using var key = root.CreateSubKey(keyPath, writable: true);
                key?.SetValue(valueName, value, kind);
                return true;
            }
            catch (Exception ex)
            {
                var logger = ServiceContainer.GetOptionalService<ILogger>();
                logger?.LogError(ex, "Failed to set registry value: {KeyPath}\\{ValueName} = {Value}", keyPath, valueName, value);
                reportOutput?.Invoke($"[WARN] Could not set {root.Name}\\{keyPath}!{valueName}: {ex.Message}");
                return false;
            }
        }
    }
}
