using System;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace RecoveryCommander.Core
{
    /// <summary>
    /// Global exception handler for the application
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class GlobalExceptionHandler
    {
        /// <summary>
        /// Handles unhandled exceptions
        /// </summary>
        /// <param name="exception">The exception to handle</param>
        public static void HandleException(Exception exception)
        {
            var logger = ServiceContainer.GetService<ILogger>();
            logger?.LogError(exception, "Unhandled exception occurred");
            
            // Log to Windows Event Log as well
#if WINDOWS
            try
            {
                System.Diagnostics.EventLog.WriteEntry(
                    "RecoveryCommander",
                    $"Unhandled exception: {exception}\n\nStack trace: {exception.StackTrace}",
                    System.Diagnostics.EventLogEntryType.Error
                );
            }
            catch
            {
                // Ignore Event Log errors
            }
#endif
        }

        /// <summary>
        /// Static initialization method for global exception handling
        /// </summary>
        public static void Initialize()
        {
            // Set up global exception handling directly
            SetupGlobalExceptionHandling();
        }

        /// <summary>
        /// Sets up global exception handling for application
        /// </summary>
        public static void SetupGlobalExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is Exception exception)
                {
                    HandleException(exception);
                }
            };

#if WINDOWS
            System.Windows.Forms.Application.ThreadException += (sender, args) =>
            {
                HandleException(args.Exception);
            };
#endif
        }
    }
}
