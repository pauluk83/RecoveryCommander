using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace RecoveryCommander.UI
{
    public static partial class Theme
    {
        #region Theme Preferences
        public sealed class ThemePreferences
        {
            private const string PreferencesFileName = "theme_preferences.json";
            private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

            public ThemeMode PreferredTheme { get; set; } = ThemeMode.System;
            public BackdropType PreferredBackdrop { get; set; } = BackdropType.Mica;
            public bool UseSystemAccent { get; set; } = true;
            public bool ReduceMotion { get; set; } = false;
            public bool HighContrast { get; set; } = false;
            public bool EnableFuturisticAnimations { get; set; } = true;

            private static string GetPreferencesPath()
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var folder = Path.Combine(appData, "RecoveryCommander");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                return Path.Combine(folder, PreferencesFileName);
            }

            public static ThemePreferences Load()
            {
                try
                {
                    var path = GetPreferencesPath();
                    if (File.Exists(path))
                    {
                        var json = File.ReadAllText(path);
                        return JsonSerializer.Deserialize<ThemePreferences>(json) ?? new ThemePreferences();
                    }
                }
                catch { }
                return new ThemePreferences();
            }

            public static void Save(ThemePreferences preferences)
            {
                try
                {
                    var path = GetPreferencesPath();
                    var json = JsonSerializer.Serialize(preferences, _jsonOptions);
                    File.WriteAllText(path, json);
                }
                catch { }
            }

            public ThemePreferences Clone() => (ThemePreferences)this.MemberwiseClone();
        }
        #endregion

        #region UI Utils
        public static class UIUtils
        {
            public static void SetVisibleAnimated(Control control, bool visible)
            {
                if (visible) control.Show(); else control.Hide();
            }

            public static void FadeIn(Control control, int duration = 300) => control.Show();
            public static void FadeOut(Control control, int duration = 300) => control.Hide();

            public static void CenterOnScreen(Form form)
            {
                var screen = Screen.PrimaryScreen;
                if (screen != null)
                {
                    form.Left = (screen.WorkingArea.Width - form.Width) / 2;
                    form.Top = (screen.WorkingArea.Height - form.Height) / 2;
                }
            }

            public static void ApplyDarkTheme(Control control) => Theme.ApplyTheme(control);

            public static GraphicsPath GetRoundedRect(Rectangle rect, int radius)
            {
                var path = new GraphicsPath();
                if (radius == 0) { path.AddRectangle(rect); return path; }

                int diameter = radius * 2;
                var arc = new Rectangle(rect.Location, new Size(diameter, diameter));
                path.AddArc(arc, 180, 90);
                arc.X = rect.Right - diameter;
                path.AddArc(arc, 270, 90);
                arc.Y = rect.Bottom - diameter;
                path.AddArc(arc, 0, 90);
                arc.X = rect.Left;
                path.AddArc(arc, 90, 90);
                path.CloseFigure();
                return path;
            }
        }
        #endregion
    }

    public static class ControlExtensions
    {
        public static void SetVisibleAnimated(this Control control, bool visible) => Theme.UIUtils.SetVisibleAnimated(control, visible);
        public static void FadeIn(this Control control, int duration = 300) => Theme.UIUtils.FadeIn(control, duration);
        public static void FadeOut(this Control control, int duration = 300) => Theme.UIUtils.FadeOut(control, duration);
        public static void CenterOnScreen(this Form form) => Theme.UIUtils.CenterOnScreen(form);
        public static void ApplyDarkTheme(this Control control) => Theme.UIUtils.ApplyDarkTheme(control);
        public static GraphicsPath GetRoundedRect(this Rectangle rect, int radius) => Theme.UIUtils.GetRoundedRect(rect, radius);
    }
}
