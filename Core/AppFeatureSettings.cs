using System;
using System.IO;
using System.Text.Json;

namespace RecoveryCommander.Core
{
    /// <summary>
    /// Persisted feature flags for technician-facing app behavior.
    /// </summary>
    public sealed class AppFeatureSettings
    {
        private static readonly object SyncRoot = new();
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        /// <summary>
        /// Allows downloads to proceed even when a SHA-256 hash is missing, unavailable,
        /// or fails verification. This is intended for advanced technicians only.
        /// </summary>
        public bool AllowUnverifiedDownloads { get; set; } = false;

        public static string SettingsPath
        {
            get
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrWhiteSpace(appData))
                {
                    appData = AppContext.BaseDirectory;
                }

                return Path.Combine(appData, "RecoveryCommander", "settings.json");
            }
        }

        public static AppFeatureSettings Load()
        {
            lock (SyncRoot)
            {
                try
                {
                    if (!File.Exists(SettingsPath))
                    {
                        return new AppFeatureSettings();
                    }

                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppFeatureSettings>(json) ?? new AppFeatureSettings();
                }
                catch (IOException)
                {
                    return new AppFeatureSettings();
                }
                catch (UnauthorizedAccessException)
                {
                    return new AppFeatureSettings();
                }
                catch (JsonException)
                {
                    return new AppFeatureSettings();
                }
            }
        }

        public static void Save(AppFeatureSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            lock (SyncRoot)
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
            }
        }

        public static bool GetAllowUnverifiedDownloads()
            => Load().AllowUnverifiedDownloads;

        public static bool ShouldBypassDownloadVerification()
            => GetAllowUnverifiedDownloads()
            || string.Equals(
                Environment.GetEnvironmentVariable("RC_ALLOW_UNVERIFIED_DOWNLOAD"),
                "1",
                StringComparison.OrdinalIgnoreCase);

        public static void SetAllowUnverifiedDownloads(bool value)
        {
            var settings = Load();
            settings.AllowUnverifiedDownloads = value;
            Save(settings);
        }
    }
}
