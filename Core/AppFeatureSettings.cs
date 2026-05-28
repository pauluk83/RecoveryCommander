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

        public bool AllowUnverifiedDownloads { get; set; }

        public static string SettingsPath
        {
            get
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
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

        public static void SetAllowUnverifiedDownloads(bool value)
        {
            var settings = Load();
            settings.AllowUnverifiedDownloads = value;
            Save(settings);
        }
    }
}
