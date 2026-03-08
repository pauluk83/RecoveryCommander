using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RecoveryCommander.Contracts
{
    public interface IRecoveryModule
    {
        string Name { get; }
        string Description { get; }
        string Version { get; }
        string HealthStatus { get; }
        string BuildInfo { get; }
        bool SupportsAsync { get; }
        
        IEnumerable<ModuleAction> Actions { get; }
        
        Task ExecuteActionAsync(string actionName, IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
        {
            // Default explicit delegate routing instead of string mapping in each module
            var action = System.Linq.Enumerable.FirstOrDefault(Actions, a => a.Name == actionName);
            if (action != null)
            {
                if (action.ExecuteActionExtended != null)
                {
                    return action.ExecuteActionExtended(progress, reportOutput, dialogService, cancellationToken);
                }
                if (action.ExecuteAction != null)
                {
                    return action.ExecuteAction(progress, reportOutput, cancellationToken);
                }
            }
            
            reportOutput($"Action '{actionName}' not properly configured or missing execution delegate.");
            return Task.CompletedTask;
        }
    }

    public class ModuleAction
    {
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public bool IsAsync { get; set; } = true;
        public bool RequiresAdmin { get; set; } = false;
        public string? IconName { get; set; }
        
        public bool IsHeader { get; set; } = false;
        public bool AutoTick { get; set; } = false;
        public bool IsDestructive { get; set; } = false;
        public bool Highlight { get; set; } = false;
        
        public Func<IProgress<ProgressReport>, Action<string>, CancellationToken, Task>? ExecuteAction { get; set; }
        public Func<IProgress<ProgressReport>, Action<string>, IDialogService, CancellationToken, Task>? ExecuteActionExtended { get; set; }
        
        public ModuleAction(string name, string? displayName = null, Func<IProgress<ProgressReport>, Action<string>, CancellationToken, Task>? executeAction = null)
        {
            Name = name;
            DisplayName = displayName ?? name;
            ExecuteAction = executeAction;
        }
    }

    public class ProgressReport
    {
        public int PercentComplete { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public string? Details { get; set; }
        public bool IsIndeterminate { get; set; } = false;

        public ProgressReport(int percent, string message, string? details = null)
        {
            PercentComplete = percent;
            StatusMessage = message;
            Details = details;
        }
    }

    public class ModuleResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public int ExitCode { get; set; }
        public Exception? Exception { get; set; }
        
        public string? Error 
        { 
            get => Exception?.Message ?? (Success ? null : Message);
            set => Message = value ?? string.Empty;
        }

        public static ModuleResult CreateSuccess(string message) => new() { Success = true, Message = message };
        public static ModuleResult CreateFailure(string message, Exception? ex = null) => new() { Success = false, Message = message, Exception = ex };
    }

    public class CommandResult
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
 
    public interface IProgressReporter
    {
        void ReportProgress(int percent, string message);
        void ReportOutput(string output);
        void ReportError(string error);
        void Report(ProgressReport report);
    }

    public interface IDialogService
    {
        void ShowContentDialog(string content, string title);
    }
}
