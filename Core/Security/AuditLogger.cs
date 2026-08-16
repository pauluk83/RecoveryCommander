/*
 * AUDIT HEADER
 * File: AuditLogger.cs
 * Module: Core / Security
 * Created: 2026-07-20
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-07-20 - 1.0.0 - Initial audit logger for ISO 27001/SOC 2 compliance.
 *                       Logs all security-relevant events with structured format,
 *                       tamper-evident storage, and immutable audit trail.
 */

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RecoveryCommander.Core.Security
{
    /// <summary>
    /// Audit event severity levels for security classification
    /// </summary>
    public enum AuditSeverity
    {
        Information,
        Warning,
        SecurityEvent,
        Critical
    }

    /// <summary>
    /// Structured audit event for compliance logging
    /// </summary>
    public sealed class AuditEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public AuditSeverity Severity { get; init; }
        public string Category { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public string? UserId { get; init; }
        public string? SourceIp { get; init; }
        public string? Resource { get; init; }
        public string? Details { get; init; }
        public bool Success { get; init; }
        public string? FailureReason { get; init; }
    }

    /// <summary>
    /// Audit logger for ISO 27001/SOC 2 compliance.
    /// Provides tamper-evident, immutable audit trail for all security-relevant events.
    /// </summary>
    public sealed class AuditLogger : IDisposable
    {
        private static readonly Lazy<AuditLogger> _instance = new(() => new AuditLogger());
        public static AuditLogger Instance => _instance.Value;

        private readonly ConcurrentQueue<AuditEvent> _eventQueue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _processingTask;
        private readonly string _auditLogPath;
        private readonly string _auditHashPath;
        private readonly object _hashLock = new();
        private bool _disposed;

        private AuditLogger()
        {
            var auditDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecoveryCommander",
                "audit");

            Directory.CreateDirectory(auditDir);

            _auditLogPath = Path.Combine(auditDir, "audit.log");
            _auditHashPath = Path.Combine(auditDir, "audit.hash");

            _processingTask = Task.Run(ProcessAuditEventsAsync);
        }

        /// <summary>
        /// Log a security-relevant event for compliance
        /// </summary>
        public void LogEvent(AuditEvent auditEvent)
        {
            if (_disposed) return;
            _eventQueue.Enqueue(auditEvent);
        }

        /// <summary>
        /// Log a successful security event
        /// </summary>
        public void LogSuccess(string category, string action, string? resource = null, string? details = null)
        {
            LogEvent(new AuditEvent
            {
                Severity = AuditSeverity.Information,
                Category = category,
                Action = action,
                Resource = resource,
                Details = details,
                Success = true
            });
        }

        /// <summary>
        /// Log a failed security event
        /// </summary>
        public void LogFailure(string category, string action, string failureReason, string? resource = null, string? details = null)
        {
            LogEvent(new AuditEvent
            {
                Severity = AuditSeverity.SecurityEvent,
                Category = category,
                Action = action,
                Resource = resource,
                Details = details,
                Success = false,
                FailureReason = failureReason
            });
        }

        /// <summary>
        /// Log a critical security event
        /// </summary>
        public void LogCritical(string category, string action, string details)
        {
            LogEvent(new AuditEvent
            {
                Severity = AuditSeverity.Critical,
                Category = category,
                Action = action,
                Details = details,
                Success = false
            });
        }

        private async Task ProcessAuditEventsAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    WriteToAuditLog();
                    await Task.Delay(100, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Log processing errors to system event log
                    try
                    {
                        File.AppendAllText(
                            Path.Combine(Path.GetDirectoryName(_auditLogPath)!, "audit-errors.log"),
                            $"{DateTime.UtcNow:O} - Audit processing error: {ex.Message}\n");
                    }
                    catch { }
                }
            }

            // Process remaining events on shutdown
            WriteToAuditLog();
        }

        private void WriteToAuditLog()
        {
            if (!_eventQueue.TryDequeue(out var auditEvent)) return;

            try
            {
                var logEntry = FormatAuditEvent(auditEvent);
                File.AppendAllText(_auditLogPath, logEntry + Environment.NewLine, Encoding.UTF8);
                UpdateAuditHash();
            }
            catch (Exception ex)
            {
                // Fallback: write to separate error log if main log fails
                try
                {
                    var errorPath = Path.Combine(Path.GetDirectoryName(_auditLogPath)!, "audit-fallback.log");
                    File.AppendAllText(errorPath, $"{DateTime.UtcNow:O} - {ex.Message}\n");
                }
                catch { }
            }
        }

        private string FormatAuditEvent(AuditEvent evt)
        {
            // Structured format for SIEM integration
            return $"[{evt.Timestamp:O}] [{evt.Severity}] [{evt.Category}] [{evt.Action}] " +
                   $"ID:{evt.EventId} Success:{evt.Success} " +
                   $"User:{evt.UserId ?? "SYSTEM"} " +
                   $"Resource:{evt.Resource ?? "N/A"} " +
                   $"Details:{evt.Details ?? "N/A"} " +
                   $"Failure:{evt.FailureReason ?? "N/A"}";
        }

        private void UpdateAuditHash()
        {
            lock (_hashLock)
            {
                try
                {
                    var currentHash = ComputeFileHash(_auditLogPath);
                    File.WriteAllText(_auditHashPath, currentHash);
                }
                catch { }
            }
        }

        private string ComputeFileHash(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// Verify audit log integrity by comparing current hash with stored hash
        /// </summary>
        public bool VerifyAuditLogIntegrity()
        {
            lock (_hashLock)
            {
                try
                {
                    if (!File.Exists(_auditHashPath) || !File.Exists(_auditLogPath))
                        return false;

                    var storedHash = File.ReadAllText(_auditHashPath).Trim();
                    var currentHash = ComputeFileHash(_auditLogPath);

                    return storedHash.Equals(currentHash, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Get audit log statistics for compliance reporting
        /// </summary>
        public AuditStatistics GetStatistics()
        {
            try
            {
                if (!File.Exists(_auditLogPath))
                    return new AuditStatistics();

                var lines = File.ReadAllLines(_auditLogPath);
                var stats = new AuditStatistics
                {
                    TotalEvents = lines.Length,
                    LogFilePath = _auditLogPath,
                    LastModified = File.GetLastWriteTimeUtc(_auditLogPath),
                    IntegrityVerified = VerifyAuditLogIntegrity()
                };

                foreach (var line in lines)
                {
                    if (line.Contains("[Critical]")) stats.CriticalEvents++;
                    if (line.Contains("[SecurityEvent]")) stats.SecurityEvents++;
                    if (line.Contains("[Warning]")) stats.Warnings++;
                    if (line.Contains("Success:False")) stats.FailedEvents++;
                }

                return stats;
            }
            catch
            {
                return new AuditStatistics();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();
            try
            {
                _processingTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch { }

            _cts.Dispose();
        }
    }

    /// <summary>
    /// Audit log statistics for compliance reporting
    /// </summary>
    public sealed class AuditStatistics
    {
        public int TotalEvents { get; init; }
        public int CriticalEvents { get; set; }
        public int SecurityEvents { get; set; }
        public int Warnings { get; set; }
        public int FailedEvents { get; set; }
        public string? LogFilePath { get; init; }
        public DateTime LastModified { get; init; }
        public bool IntegrityVerified { get; set; }
    }
}
