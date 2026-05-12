using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using Microsoft.Win32;

namespace RecoveryCommander.UI
{
    public static partial class Theme
    {
        #region Responsive Design System
        
        public static class ResponsiveDesign
        {
            private static readonly Dictionary<Control, ResponsiveSettings> ResponsiveControls = [];
            private static float baseDpiScale = 1.0f;
            private static Size baseScreenSize = new(1920, 1080);
            
            public static void Initialize(Form? mainForm)
            {
                if (mainForm == null) return;
                baseDpiScale = GetDpiScale(mainForm);
                baseScreenSize = Screen.PrimaryScreen?.WorkingArea.Size ?? new Size(1920, 1080);
                
                if (Environment.OSVersion.Version.Major >= 10)
                    mainForm.DpiChanged += OnDpiChanged;
                SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            }
            
            public static void RegisterControl(Control control, ResponsiveSettings settings)
            {
                if (control == null) return;
                ResponsiveControls[control] = settings;
                ApplyResponsiveSettings(control, settings);
            }
            
            private static void ApplyResponsiveSettings(Control control, ResponsiveSettings settings, 
                float? dpiScale = null, Size? screenSize = null)
            {
                if (control == null || settings == null) return;
                var currentDpiScale = dpiScale ?? GetDpiScale(control);
                var currentScreenSize = screenSize ?? Screen.PrimaryScreen?.WorkingArea.Size ?? baseScreenSize;
                
                var dpiScaleFactor = currentDpiScale / baseDpiScale;
                var screenScaleFactor = Math.Min(
                    (float)currentScreenSize.Width / baseScreenSize.Width,
                    (float)currentScreenSize.Height / baseScreenSize.Height);
                var combinedScale = Math.Max(dpiScaleFactor, screenScaleFactor);
                
                if (settings.ScaleFont && control.Font != null)
                {
                    var newFontSize = Math.Max(settings.MinFontSize, 
                        Math.Min(settings.MaxFontSize, settings.BaseFontSize * combinedScale));
                    if (Math.Abs(control.Font.Size - newFontSize) > 0.1f)
                        control.Font = new Font(control.Font.FontFamily, newFontSize, control.Font.Style);
                }
                
                if (settings.ScaleSize)
                {
                    var newWidth = (int)(settings.BaseSize.Width * combinedScale);
                    var newHeight = (int)(settings.BaseSize.Height * combinedScale);
                    newWidth = Math.Max(settings.MinSize.Width, Math.Min(settings.MaxSize.Width, newWidth));
                    newHeight = Math.Max(settings.MinSize.Height, Math.Min(settings.MaxSize.Height, newHeight));
                    if (control.Size != new Size(newWidth, newHeight))
                        control.Size = new Size(newWidth, newHeight);
                }
            }

            private static float GetDpiScale(Control control)
            {
                if (control == null) return 1.0f;
                try
                {
                    using var g = control.CreateGraphics();
                    return g.DpiX / 96.0f;
                }
                catch { return 1.0f; }
            }

            private static void OnDpiChanged(object? sender, DpiChangedEventArgs e)
            {
                if (sender is Form form)
                    UpdateAllControls(form);
            }

            private static void OnDisplaySettingsChanged(object? sender, EventArgs e)
            {
                foreach (var control in ResponsiveControls.Keys.OfType<Form>())
                {
                    if (!control.IsDisposed)
                        control.BeginInvoke(new Action(() => UpdateAllControls(control)));
                }
            }

            private static void UpdateAllControls(Form form)
            {
                var currentDpiScale = GetDpiScale(form);
                var currentScreenSize = Screen.PrimaryScreen?.WorkingArea.Size ?? baseScreenSize;
                
                foreach (var kvp in ResponsiveControls.ToList())
                {
                    var control = kvp.Key;
                    var settings = kvp.Value;
                    
                    if (control.IsDisposed)
                    {
                        ResponsiveControls.Remove(control);
                        continue;
                    }
                    
                    ApplyResponsiveSettings(control, settings, currentDpiScale, currentScreenSize);
                }
            }

            public static class Presets
            {
                public static ResponsiveSettings ModuleButton => new()
                {
                    ScaleFont = true,
                    BaseFontSize = 10f,
                    MinFontSize = 8f,
                    MaxFontSize = 14f,
                    ScaleSize = true,
                    BaseSize = new Size(200, 72),
                    MinSize = new Size(150, 50),
                    MaxSize = new Size(300, 100)
                };
                
                public static ResponsiveSettings ActionTile => new()
                {
                    ScaleFont = true,
                    BaseFontSize = 9f,
                    MinFontSize = 8f,
                    MaxFontSize = 12f,
                    ScaleSize = true,
                    BaseSize = new Size(300, 72),
                    MinSize = new Size(200, 50),
                    MaxSize = new Size(400, 100)
                };
            }
        }

        public class ResponsiveSettings
        {
            public bool ScaleFont { get; set; } = false;
            public float BaseFontSize { get; set; } = 9f;
            public float MinFontSize { get; set; } = 6f;
            public float MaxFontSize { get; set; } = 20f;
            public bool ScaleSize { get; set; } = false;
            public Size BaseSize { get; set; } = Size.Empty;
            public Size MinSize { get; set; } = Size.Empty;
            public Size MaxSize { get; set; } = new Size(int.MaxValue, int.MaxValue);
            public List<BreakpointSettings> Breakpoints { get; set; } = [];
        }

        public class BreakpointSettings
        {
            public int MinWidth { get; set; }
            public bool? Visible { get; set; }
            public DockStyle? Dock { get; set; }
            public AnchorStyles? Anchor { get; set; }
            public ResponsiveLayout Layout { get; set; } = ResponsiveLayout.Default;
            public int GridColumns { get; set; } = 1;
        }

        public enum ResponsiveLayout
        {
            Default,
            Stack,
            Grid,
            Flex
        }

        #endregion
    }
}
