/*
 * AUDIT HEADER
 * File: RollingFileLogger.cs
 * Module: Core / Logging
 * Created: 2026-05-02
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-05-02 - 1.3.0 - Initial rolling-file logger backed by Microsoft.Extensions.Logging.
 *                       Daily rolling under %LOCALAPPDATA%\RecoveryCommander\logs, 14-day
 *                       retention, single background writer thread, lock-free MPSC queue.
 *                       No external dependencies (avoids pulling in Serilog).
 */

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RecoveryCommander.Core.Logging
{
    /// <summary>
    /// Configuration knobs for the rolling-file logger.
    /// </summary>
    public sealed class RollingFileLoggerOptions
    {
        public string Directory { get; init; } = AppPaths.LogsDirectory;
        public string FileNamePrefix { get; init; } = "rc-";
        public int RetentionDays { get; init; } = 14;
        public LogLevel MinimumLevel { get; init; } = LogLevel.Information;
    }

    /// <summary>
    /// Resolves stable per-user paths used by the app for logs and similar persistent state.
    /// </summary>
    public static class AppPaths
    {
        public static string RootDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RecoveryCommander");

        public static string LogsDirectory =>
            Path.Combine(RootDirectory, "logs");

        /// <summary>
        /// Path to the log file for the current local date.
        /// </summary>
        public static string CurrentLogFile(string prefix = "rc-")
            => Path.Combine(LogsDirectory, $"{prefix}{DateTime.Now:yyyyMMdd}.log");
    }

    /// <summary>
    /// Logger provider that emits one daily-rolling text log under the user's LocalAppData.
    /// </summary>
    public sealed class RollingFileLoggerProvider : ILoggerProvider
    {
        private readonly RollingFileLoggerOptions _options;
        private readonly BlockingCollection<string> _queue = new(boundedCapacity: 4096);
        private readonly Task _writerTask;
        private readonly CancellationTokenSource _cts = new();
        private bool _disposed;

        public RollingFileLoggerProvider(RollingFileLoggerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            try
            {
                System.IO.Directory.CreateDirectory(_options.Directory);
            }
            catch
            {
                // Failure to create the log dir is not fatal; we degrade to console-only.
            }
            _writerTask = Task.Run(WriteLoopAsync);
        }

        public ILogger CreateLogger(string categoryName)
            => new RollingFileLogger(categoryName, _options.MinimumLevel, Enqueue);

        private void Enqueue(string line)
        {
            if (_disposed) return;
            // Non-blocking add; if the queue is saturated we drop the line rather than
            // back-pressuring the calling thread (which is often the UI thread).
            try { _queue.TryAdd(line, millisecondsTimeout: 0); }
            catch (InvalidOperationException) { /* queue completed */ }
        }

        private async Task WriteLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    string? line = null;
                    try
                    {
                        line = _queue.Take(_cts.Token);
                    }
                    catch (OperationCanceledException) { break; }

                    var path = AppPaths.CurrentLogFile(_options.FileNamePrefix);
                    try
                    {
                        await File.AppendAllTextAsync(path, line + Environment.NewLine, Encoding.UTF8, _cts.Token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch
                    {
                        // Swallow IO errors so logging never crashes the app.
                    }

                    // Opportunistically prune old logs when we cross a day boundary.
                    if (line.Contains("[INF] App startup", StringComparison.Ordinal))
                    {
                        TryPrune();
                    }
                }
            }
            finally
            {
                // Drain any remaining lines on shutdown.
                while (_queue.TryTake(out var leftover))
                {
                    try
                    {
                        var path = AppPaths.CurrentLogFile(_options.FileNamePrefix);
                        File.AppendAllText(path, leftover + Environment.NewLine, Encoding.UTF8);
                    }
                    catch { /* best-effort */ }
                }
            }
        }

        private void TryPrune()
        {
            try
            {
                if (_options.RetentionDays <= 0) return;
                var dir = new DirectoryInfo(_options.Directory);
                if (!dir.Exists) return;

                var cutoff = DateTime.Now.AddDays(-_options.RetentionDays);
                foreach (var f in dir.EnumerateFiles($"{_options.FileNamePrefix}*.log"))
                {
                    if (f.LastWriteTime < cutoff)
                    {
                        try { f.Delete(); } catch { /* best-effort */ }
                    }
                }
            }
            catch { /* best-effort */ }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _queue.CompleteAdding(); } catch { }
            _cts.Cancel();
            try { _writerTask.Wait(TimeSpan.FromSeconds(2)); } catch { }
            _cts.Dispose();
            _queue.Dispose();
        }
    }

    internal sealed class RollingFileLogger : ILogger
    {
        private readonly string _category;
        private readonly LogLevel _minimum;
        private readonly Action<string> _emit;

        public RollingFileLogger(string category, LogLevel minimum, Action<string> emit)
        {
            _category = category;
            _minimum = minimum;
            _emit = emit;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minimum && logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var msg = formatter(state, exception);
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Abbrev(logLevel)}] {_category}: {msg}";
            if (exception != null) line += Environment.NewLine + exception;
            _emit(line);
        }

        private static string Abbrev(LogLevel l) => l switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "INF"
        };

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    /// <summary>
    /// Convenience extensions for wiring the rolling-file logger into the standard
    /// Microsoft.Extensions.Logging builder.
    /// </summary>
    public static class RollingFileLoggerExtensions
    {
        public static ILoggingBuilder AddRollingFile(this ILoggingBuilder builder, RollingFileLoggerOptions? options = null)
        {
            options ??= new RollingFileLoggerOptions();
            builder.AddProvider(new RollingFileLoggerProvider(options));
            return builder;
        }
    }
}
