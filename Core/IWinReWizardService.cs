using System;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Core
{
    /// <summary>
    /// Abstraction for host-specific WinRE wizard UI integration.
    /// </summary>
    public interface IWinReWizardService
    {
        Task<bool> RunPbrSetupWizardAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken);
    }
}
