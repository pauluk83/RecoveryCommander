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
        public Task DisableTelemetryAsync(IProgress<ProgressReport> progress, Action<string> reportOutput)
        {
            progress.Report(new ProgressReport(10, "Disabling telemetry..."));
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord);
            reportOutput("Telemetry disabled in registry.");
            return Task.CompletedTask;
        }

        public Task DisableWebSearchAsync(IProgress<ProgressReport> progress, Action<string> reportOutput)
        {
            progress.Report(new ProgressReport(50, "Disabling Bing search in Start..."));
            SetRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord);
            reportOutput("Web search disabled in Explorer policies.");
            return Task.CompletedTask;
        }

        private void SetRegistryValue(RegistryKey root, string keyPath, string valueName, object value, RegistryValueKind kind)
        {
            try
            {
                using var key = root.CreateSubKey(keyPath, true);
                key?.SetValue(valueName, value, kind);
            }
            catch (Exception ex)
            {
                var logger = ServiceContainer.GetService<ILogger>();
                logger?.LogError(ex, "Failed to set registry value: {KeyPath}\\{ValueName} = {Value}", keyPath, valueName, value);
            }
        }
    }
}
