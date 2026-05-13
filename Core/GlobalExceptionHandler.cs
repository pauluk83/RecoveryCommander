/*
 * AUDIT HEADER
 * File: GlobalExceptionHandler.cs
 * Module: Core
 * Created: 2026-04-22
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-04-22 - 1.1.0 - Initial app-level handler bridging to ILogger + EventLog.
 * 2026-05-02 - 1.3.0 - ILogger is now the primary sink; EventLog is best-effort and only
 *                       used when the source is registered. Source registration is attempted
 *                       once at Initialize() and caches the result so we don't keep retrying
 *                       a privileged op that can fail silently in non-elevated runs.
 */

using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace RecoveryCommander.Core
{
    [SupportedOSPlatform("windows")]
    public sealed class GlobalExceptionHandler
    {
        private static readonly Action<ILogger, Exception?> _logUnhandledException = 
            LoggerMessage.Define(LogLevel.Error, new EventId(2, "UnhandledException"), "Unhandled exception occurred");

        private const string EventLogSource = "RecoveryCommander";
        private const string EventLogName = "Application";
        private static bool _eventLogReady;
        private static bool _eventLogAttempted;

        public static void HandleException(Exception exception)
        {
            var logger = ServiceContainer.GetOptionalService<ILogger>();
            if (logger != null)
            {
                _logUnhandledException(logger, exception);
            }

            if (!_eventLogReady) return;

            try
            {
                EventLog.WriteEntry(
                    EventLogSource,
                    $"Unhandled exception: {exception}\n\nStack trace: {exception.StackTrace}",
                    EventLogEntryType.Error);
            }
            catch (System.ComponentModel.Win32Exception) { }
            catch (InvalidOperationException) { }
            catch (System.Security.SecurityException) { }
        }

        public static void Initialize()
        {
            CoreUtilities.SetupGlobalExceptionHandling();
            TryRegisterEventLogSource();
        }

        private static void TryRegisterEventLogSource()
        {
            if (_eventLogAttempted) return;
            _eventLogAttempted = true;

            try
            {
                if (EventLog.SourceExists(EventLogSource))
                {
                    _eventLogReady = true;
                    return;
                }

                // CreateEventSource requires admin. The app's manifest already requests admin so
                // this should usually succeed; if it doesn't, we still log via ILogger above.
                EventLog.CreateEventSource(EventLogSource, EventLogName);
                _eventLogReady = true;
            }
            catch (System.Security.SecurityException ex)
            {
                Debug.WriteLine($"[GlobalExceptionHandler] Permission denied to register Event Log source '{EventLogSource}': {ex.Message}");
                _eventLogReady = false;
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"[GlobalExceptionHandler] Invalid operation during Event Log registration: {ex.Message}");
                _eventLogReady = false;
            }
        }
    }
}
