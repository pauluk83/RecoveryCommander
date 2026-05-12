using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Text.Json;

namespace RecoveryCommander.UI
{
    /// <summary>
    /// Consolidated Windows 11 Theme System - Single source of truth for all theming
    /// Replaces: Windows11FluentTheme, Win11Theme, ThemeManager, and UnifiedTheme
    /// </summary>
    public static partial class Theme
    {
        #region Enums
        public enum ThemeMode
        {
            Light,
            Dark,
            System,
            AIUltraFuturistic,
            ModernProfessional
        }

        public enum ButtonStyle
        {
            Standard,
            Primary,
            Secondary,
            Subtle,
            Modern,
            FuturisticPrimary,
            FuturisticGhost
        }

        public enum ControlType
        {
            Button,
            Panel,
            TextBox,
            Card,
            Menu,
            StatusBar
        }

        public enum BackdropType
        {
            None = 1,
            Mica = 2,
            Acrylic = 3,
            MicaAlt = 4
        }
        #endregion

        #region Consolidated Interfaces (from CommonInterfaces.cs)
        /// <summary>
        /// Unified interface definitions for the RecoveryCommander UI system
        /// Consolidated from: CommonInterfaces.cs
        /// </summary>
        public interface IThemeable
        {
            void UpdateTheme(bool isDarkMode = true);
        }

        public interface IOutputTextProvider
        {
            string GetOutputText();
            string OutputText { get; set; }
        }

        public interface IAnimationProvider
        {
            void StartAnimation();
            void StopAnimation();
        }

        public interface IErrorHandler
        {
            void HandleError(Exception ex, string context);
        }

        public interface IAsyncOperation
        {
            Task StartAsync();
            Task CancelAsync();
            bool IsRunning { get; }
            bool IsCancelled { get; }
        }
        #endregion

        #region Colors
        public static class Colors
        {
            // Current theme helpers
            public static Color Background => Theme.Background;
            public static Color Surface => Theme.Surface;
            public static Color SurfaceVariant => Theme.SurfaceVariant;
            public static Color Primary => Theme.Primary;
            public static Color PrimaryVariant => Theme.PrimaryVariant;
            public static Color Text => Theme.Text;
            public static Color TextSecondary => Theme.TextSecondary;
            public static Color SubtleText => Theme.SubtleText;
            public static Color Border => Theme.Border;
            public static Color Accent => Theme.Accent;
            public static Color Success => Theme.Success;
            public static Color Warning => Theme.Warning;
            public static Color Error => Theme.Error;

            // Futuristic colors (defaults)
            public static Color FuturisticGradientStart => Color.FromArgb(0, 0, 50);
            public static Color FuturisticGradientEnd => Color.FromArgb(0, 50, 100);
            public static Color FuturisticEdge => Color.FromArgb(0, 200, 255);
            public static Color FuturisticGlow => Color.FromArgb(0, 150, 255);
            public static Color AISurface => Color.FromArgb(20, 20, 30);
            public static Color AISurfaceVariant => Color.FromArgb(30, 30, 40);
            public static Color AIPrimaryVariant => Color.FromArgb(0, 200, 100);
            public static Color AIAccent => Color.FromArgb(0, 255, 200);
            // Light mode colors
            public static class Light
            {
                public static readonly Color Background = Color.FromArgb(255, 255, 255);
                public static readonly Color Surface = Color.FromArgb(248, 248, 248);
                public static readonly Color SurfaceVariant = Color.FromArgb(240, 240, 240);
                public static readonly Color Primary = Color.FromArgb(0, 120, 215);
                public static readonly Color PrimaryVariant = Color.FromArgb(0, 90, 170);
                public static readonly Color Text = Color.FromArgb(0, 0, 0);
                public static readonly Color TextSecondary = Color.FromArgb(100, 100, 100);
                public static readonly Color SubtleText = Color.FromArgb(120, 120, 120);
                public static readonly Color Border = Color.FromArgb(200, 200, 200);
                public static readonly Color Accent = Color.FromArgb(0, 120, 215);
                public static readonly Color Success = Color.FromArgb(16, 124, 16);
                public static readonly Color Warning = Color.FromArgb(255, 185, 0);
                public static readonly Color Error = Color.FromArgb(196, 43, 28);
            }

            // Dark mode colors
            public static class Dark
            {
                public static readonly Color Background = Color.FromArgb(32, 32, 32);
                public static readonly Color Surface = Color.FromArgb(45, 45, 45);
                public static readonly Color SurfaceVariant = Color.FromArgb(56, 56, 56);
                public static readonly Color Primary = Color.FromArgb(0, 120, 215);
                public static readonly Color PrimaryVariant = Color.FromArgb(0, 90, 170);
                public static readonly Color Text = Color.FromArgb(255, 255, 255);
                public static readonly Color TextSecondary = Color.FromArgb(200, 200, 200);
                public static readonly Color SubtleText = Color.FromArgb(150, 150, 150);
                public static readonly Color Border = Color.FromArgb(76, 76, 76);
                public static readonly Color Accent = Color.FromArgb(0, 120, 215);
                public static readonly Color Success = Color.FromArgb(16, 124, 16);
                public static readonly Color Warning = Color.FromArgb(255, 185, 0);
                public static readonly Color Error = Color.FromArgb(196, 43, 28);
            }
            
            // AI theme colors
            public static readonly Color AIPrimary = Color.FromArgb(0, 189, 255);
            public static readonly Color AIText = Color.FromArgb(240, 250, 255);
            
            // Helper methods
            public static Color Lighten(Color color, float amount) => 
                Color.FromArgb(
                    Math.Min(255, color.R + (int)(255 * amount)),
                    Math.Min(255, color.G + (int)(255 * amount)),
                    Math.Min(255, color.B + (int)(255 * amount))
                );

            public static Color Lighten(Color color, int amount) => 
                Color.FromArgb(
                    Math.Min(255, color.R + amount),
                    Math.Min(255, color.G + amount),
                    Math.Min(255, color.B + amount)
                );
                
            public static Color Darken(Color color, float amount) => 
                Color.FromArgb(
                    Math.Max(0, color.R - (int)(255 * amount)),
                    Math.Max(0, color.G - (int)(255 * amount)),
                    Math.Max(0, color.B - (int)(255 * amount))
                );

            public static Color Darken(Color color, int amount) => 
                Color.FromArgb(
                    Math.Max(0, color.R - amount),
                    Math.Max(0, color.G - amount),
                    Math.Max(0, color.B - amount)
                );
        }
        #endregion

        #region Spacing
        public static class Spacing
        {
            public const int XS = 4;
            public const int SM = 8;
            public const int MD = 16;
            public const int LG = 24;
            public const int XL = 32;
            public const int XXL = 48;
            public const int Spacious = 24;
        }
        #endregion

        #region Typography
        public static class Typography
        {
            private const string PreferredFont = "Inter";
            private const string SecondaryFont = "Segoe UI Variable";
            private const string FallbackFont = "Segoe UI";

            public static readonly FontFamily DefaultFontFamily = CreateFontFamily(PreferredFont, SecondaryFont, FallbackFont);
            public static readonly FontFamily MonoFontFamily = new FontFamily("Cascadia Code");

            public static readonly Font Display = new Font(DefaultFontFamily, 28f, FontStyle.Bold);
            public static readonly Font Title = new Font(DefaultFontFamily, 20f, FontStyle.Bold);
            public static readonly Font Header = new Font(DefaultFontFamily, 16f, FontStyle.Bold);
            public static readonly Font Subtitle = new Font(DefaultFontFamily, 12f, FontStyle.Regular);
            public static readonly Font Body = new Font(DefaultFontFamily, 10.5f, FontStyle.Regular);
            public static readonly Font BodyStrong = new Font(DefaultFontFamily, 10.5f, FontStyle.Bold);
            public static readonly Font Caption = new Font(DefaultFontFamily, 9.5f, FontStyle.Regular);
            public static readonly Font FuturisticTitle = new Font(DefaultFontFamily, 22f, FontStyle.Bold);
            public static readonly Font FuturisticTagline = new Font(DefaultFontFamily, 10f, FontStyle.Italic);
            public static readonly Font Mono = new Font(MonoFontFamily, 10f, FontStyle.Regular);

            private static FontFamily CreateFontFamily(params string[] fonts)
            {
                foreach (var font in fonts)
                {
                    try
                    {
                        using (var f = new Font(font, 10))
                        {
                            if (f.Name.Equals(font, StringComparison.OrdinalIgnoreCase))
                                return new FontFamily(font);
                        }
                    }
                    catch { }
                }
                return FontFamily.GenericSansSerif;
            }
        }
        #endregion

        #region Theme Application
        private static ThemeMode _currentTheme = ThemeMode.Dark;
        private static bool _isDarkMode = true;

        public static ThemeMode CurrentTheme => _currentTheme;
        public static bool IsDarkMode => _isDarkMode;

        // When the user has enabled a Windows High Contrast theme, every palette getter snaps
        // to SystemColors so the app inherits whatever palette the OS provides. This keeps
        // text-on-background contrast above WCAG AA without us having to maintain a separate
        // "HC" theme.
        private static bool HighContrast => System.Windows.Forms.SystemInformation.HighContrast;

        // Dynamic color properties that change based on current theme
        public static Color Background => HighContrast ? System.Drawing.SystemColors.Window
                                                       : (_isDarkMode ? Colors.Dark.Background : Colors.Light.Background);
        public static Color Surface => HighContrast ? System.Drawing.SystemColors.Control
                                                    : (_isDarkMode ? Colors.Dark.Surface : Colors.Light.Surface);
        public static Color SurfaceVariant => HighContrast ? System.Drawing.SystemColors.ControlLight
                                                           : (_isDarkMode ? Colors.Dark.SurfaceVariant : Colors.Light.SurfaceVariant);
        public static Color Primary => HighContrast ? System.Drawing.SystemColors.Highlight
                                                    : (_isDarkMode ? Colors.Dark.Primary : Colors.Light.Primary);
        public static Color PrimaryVariant => HighContrast ? System.Drawing.SystemColors.HotTrack
                                                           : (_isDarkMode ? Colors.Dark.PrimaryVariant : Colors.Light.PrimaryVariant);
        public static Color Text => HighContrast ? System.Drawing.SystemColors.WindowText
                                                 : (_isDarkMode ? Colors.Dark.Text : Colors.Light.Text);
        public static Color TextSecondary => HighContrast ? System.Drawing.SystemColors.ControlText
                                                          : (_isDarkMode ? Colors.Dark.TextSecondary : Colors.Light.TextSecondary);
        public static Color SubtleText => HighContrast ? System.Drawing.SystemColors.GrayText
                                                       : (_isDarkMode ? Colors.Dark.SubtleText : Colors.Light.SubtleText);
        public static Color Border => HighContrast ? System.Drawing.SystemColors.WindowFrame
                                                   : (_isDarkMode ? Colors.Dark.Border : Colors.Light.Border);
        public static Color Accent => HighContrast ? System.Drawing.SystemColors.Highlight
                                                   : (_isDarkMode ? Colors.Dark.Accent : Colors.Light.Accent);
        public static Color Success => HighContrast ? System.Drawing.SystemColors.WindowText
                                                    : (_isDarkMode ? Colors.Dark.Success : Colors.Light.Success);
        public static Color Warning => HighContrast ? System.Drawing.SystemColors.WindowText
                                                    : (_isDarkMode ? Colors.Dark.Warning : Colors.Light.Warning);
        public static Color Error => HighContrast ? System.Drawing.SystemColors.WindowText
                                                  : (_isDarkMode ? Colors.Dark.Error : Colors.Light.Error);

        public static event Action<ThemeMode>? OnThemeChanged;
        public static event Action<ThemePreferences>? OnThemePreferencesChanged;
        public static event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
        public static bool IsModernTheme => CurrentTheme == ThemeMode.AIUltraFuturistic || CurrentTheme == ThemeMode.ModernProfessional;
        
        public static ThemePreferences CurrentPreferences { get; private set; } = new();

        public static ThemeMode LoadThemePreference()
        {
             CurrentPreferences = ThemePreferences.Load();
             if (CurrentPreferences.PreferredTheme != ThemeMode.System)
             {
                 SetTheme(CurrentPreferences.PreferredTheme);
             }
             return CurrentPreferences.PreferredTheme;
        }

        public static void UpdatePreferences(ThemePreferences preferences)
        {
            CurrentPreferences = preferences;
            ThemePreferences.Save(preferences);
            OnThemePreferencesChanged?.Invoke(preferences);
        }
        
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_MICA_EFFECT = 1029;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static void ApplyMicaEffect(Form form)
        {
            ApplyBackdropEffect(form, CurrentPreferences.PreferredBackdrop);
        }

        public static void ApplyBackdropEffect(Form form, BackdropType type)
        {
            if (form == null || !form.IsHandleCreated) return;

            try
            {
                int darkMode = _isDarkMode ? 1 : 0;
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                if (Environment.OSVersion.Version.Build >= 22000) // Windows 11
                {
                    int backdropType = (int)type;
                    DwmSetWindowAttribute(form.Handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
                    
                    // Required for transparency to function correctly with DWM backdrops
                    if (type != BackdropType.None)
                    {
                        form.BackColor = Color.Black;
                    }
                }
                else if (Environment.OSVersion.Version.Major >= 10 && type == BackdropType.Mica) // Windows 10 Mica Fallback
                {
                    int trueValue = 1;
                    DwmSetWindowAttribute(form.Handle, DWMWA_MICA_EFFECT, ref trueValue, sizeof(int));
                    form.BackColor = Color.Black;
                }
            }
            catch { }
        }

        public static class Preferences
        {
            public static bool FuturisticAnimationsEnabled 
            {
               get => CurrentPreferences.EnableFuturisticAnimations;
               set => CurrentPreferences.EnableFuturisticAnimations = value;
            }
        }
        
        public static void ListenForSystemThemeChanges() { }

        public static void SetTheme(ThemeMode mode)
        {
            // Force dark theme only - ignore all other theme modes
            _currentTheme = ThemeMode.Dark;
            _isDarkMode = true;
            
            TriggerThemeChanged(ThemeMode.Dark);
        }

        private static bool IsSystemDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int value)
                {
                    return value == 0;
                }
            }
            catch { }
            return true; // Default to dark mode
        }

        public static void TriggerThemeChanged(ThemeMode theme)
        {
            OnThemeChanged?.Invoke(theme);
        }

        #endregion

        #region Nested Classes
        public class ThemeChangedEventArgs : EventArgs
        {
            public ThemeMode ThemeMode { get; }
            public bool IsDarkMode { get; }

            public ThemeChangedEventArgs(ThemeMode themeMode, bool isDarkMode)
            {
                ThemeMode = themeMode;
                IsDarkMode = isDarkMode;
            }
        }

        #endregion

        #region Helper Classes
        private readonly struct ThemePalette
        {
            public ThemePalette(
                Color background,
                Color surface,
                Color surfaceVariant,
                Color primary,
                Color primaryVariant,
                Color text,
                Color border,
                Color accent,
                Color success,
                Color warning,
                Color error)
            {
                Background = background;
                Surface = surface;
                SurfaceVariant = surfaceVariant;
                Primary = primary;
                PrimaryVariant = primaryVariant;
                Text = text;
                Border = border;
                Accent = accent;
                Success = success;
                Warning = warning;
                Error = error;
            }

            public Color Background { get; }
            public Color Surface { get; }
            public Color SurfaceVariant { get; }
            public Color Primary { get; }
            public Color PrimaryVariant { get; }
            public Color Text { get; }
            public Color Border { get; }
            public Color Accent { get; }
            public Color Success { get; }
            public Color Warning { get; }
            public Color Error { get; }
        }

        private readonly struct RegisteredControl
        {
            public RegisteredControl(Control control, Action<Control>? customApply)
            {
                Control = control;
                CustomApply = customApply;
            }

            public Control Control { get; }
            public Action<Control>? CustomApply { get; }
        }

        private class ThemedColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => Colors.Surface;
            public override Color ToolStripGradientMiddle => Colors.Surface;
            public override Color ToolStripGradientEnd => Colors.Surface;
            public override Color ToolStripBorder => Colors.Border;
            public override Color MenuItemSelected => Colors.Primary;
            public override Color MenuItemBorder => Colors.Border;
            public override Color MenuBorder => Colors.Border;
            public override Color ToolStripDropDownBackground => Colors.Surface;
            public override Color ImageMarginGradientBegin => Colors.Surface;
            public override Color ImageMarginGradientMiddle => Colors.Surface;
            public override Color ImageMarginGradientEnd => Colors.Surface;
            public override Color MenuStripGradientBegin => Colors.Surface;
            public override Color MenuStripGradientEnd => Colors.Surface;
            public override Color MenuItemPressedGradientBegin => Colors.Primary;
            public override Color MenuItemPressedGradientEnd => Colors.Primary;
        }
        #endregion

        #region Advanced Control Styling
        public static void ApplyTheme(Control root, bool includeChildren = true)
        {
            if (root == null) return;

            switch (root)
            {
                case Form form:
                    ApplyFormStyle(form);
                    break;
                case Panel panel:
                    ApplyPanelStyle(panel);
                    break;
                case Button button:
                    // Don't override ModernButton styles as they manage their own state
                    if (button is not ModernButton)
                    {
                        ApplyButtonStyle(button, ButtonStyle.FuturisticPrimary);
                    }
                    break;
                case TextBoxBase textBoxBase:
                    ApplyTextBoxStyle(textBoxBase);
                    break;
                case MenuStrip menu:
                    ApplyMenuStyle(menu);
                    break;
                case StatusStrip status:
                    ApplyStatusStyle(status);
                    break;
                case ToolStrip toolStrip:
                    ApplyToolStripStyle(toolStrip);
                    break;
                case ListView listView:
                    listView.BackColor = Colors.Background;
                    listView.ForeColor = Colors.Text;
                    listView.BorderStyle = BorderStyle.None;
                    if (listView.IsHandleCreated)
                        DarkScrollBar.ApplyDarkTheme(listView);
                    else
                        listView.HandleCreated += (s, e) => DarkScrollBar.ApplyDarkTheme(listView);
                    break;
            }

            if (!includeChildren) return;

            foreach (Control child in root.Controls)
            {
                ApplyTheme(child, true);
            }
        }

        public static void ApplyPanelStyle(Panel panel, bool isCard = false)
        {
            if (panel == null) return;

            panel.BackColor = isCard ? Colors.Surface : Colors.Background;
            panel.ForeColor = Colors.Text;
            panel.BorderStyle = BorderStyle.None;
            panel.Padding = isCard ? new Padding(16) : new Padding(8);

            if (panel is FlowLayoutPanel || panel.AutoScroll)
            {
                if (panel.IsHandleCreated)
                    DarkScrollBar.ApplyDarkTheme(panel);
                else
                    panel.HandleCreated += (s, e) => DarkScrollBar.ApplyDarkTheme(panel);
            }
        }

        public static LinearGradientBrush CreateFuturisticGradientBrush(Rectangle bounds, float angle = 135f)
        {
            return new LinearGradientBrush(bounds, Colors.FuturisticGradientStart, Colors.FuturisticGradientEnd, angle);
        }

        public static LinearGradientBrush CreateFuturisticAccentBrush(Rectangle bounds, float angle = 120f)
        {
            return new LinearGradientBrush(bounds,
                Colors.FuturisticEdge,
                Color.FromArgb(180, Colors.FuturisticGlow),
                angle);
        }

        public static void RenderFuturisticGradient(Graphics graphics, Rectangle bounds, float angle = 135f)
        {
            if (graphics == null || bounds.Width <= 0 || bounds.Height <= 0) return;

            var previousMode = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = CreateFuturisticGradientBrush(bounds, angle);
            graphics.FillRectangle(brush, bounds);
            graphics.SmoothingMode = previousMode;
        }

        public static void DrawFuturisticEdge(Graphics graphics, Rectangle bounds, Color color, float thickness = 1.5f)
        {
            using var pen = new Pen(color, thickness);
            graphics.DrawRectangle(pen, bounds);
        }

        public static void DrawFuturisticEdge(Graphics graphics, Rectangle bounds, float thickness = 1.5f)
        {
            if (graphics == null || bounds.Width <= 0 || bounds.Height <= 0) return;

            var previousMode = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var outerPen = new Pen(Colors.FuturisticEdge, thickness) { Alignment = PenAlignment.Inset };
            graphics.DrawRectangle(outerPen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            using var innerPen = new Pen(Colors.FuturisticGlow, thickness * 0.6f) { Alignment = PenAlignment.Inset, DashStyle = DashStyle.Dot };
            graphics.DrawRectangle(innerPen, bounds.X + 3, bounds.Y + 3, bounds.Width - 7, bounds.Height - 7);
            graphics.SmoothingMode = previousMode;
        }

        public static void ApplyTextBoxStyle(TextBoxBase textBox)
        {
            if (textBox == null) return;

            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = Colors.Surface;
            textBox.ForeColor = Colors.Text;
            textBox.Font = Typography.Body;
            
            if (textBox.IsHandleCreated)
                DarkScrollBar.ApplyDarkTheme(textBox);
            else
                textBox.HandleCreated += (s, e) => DarkScrollBar.ApplyDarkTheme(textBox);
        }

        public static void ApplyMenuStyle(MenuStrip menu)
        {
            if (menu == null) return;

            menu.BackColor = Colors.Surface;
            menu.ForeColor = Colors.Text;
            menu.Font = Typography.Body;
            menu.Renderer = new ToolStripProfessionalRenderer(new ThemedColorTable());
        }

        public static void ApplyStatusStyle(StatusStrip status)
        {
            if (status == null) return;

            status.BackColor = Colors.Surface;
            status.ForeColor = Colors.Text;
            status.Font = Typography.Caption;
            status.Renderer = new ToolStripProfessionalRenderer(new ThemedColorTable());
        }

        private static void ApplyToolStripStyle(ToolStrip toolStrip)
        {
            if (toolStrip == null) return;

            toolStrip.BackColor = Colors.Surface;
            toolStrip.ForeColor = Colors.Text;
            toolStrip.Font = Typography.Body;
            toolStrip.Renderer = new ToolStripProfessionalRenderer(new ThemedColorTable());
        }

        public static void ApplyFormStyle(Form form)
        {
            if (form == null) return;

            form.BackColor = Colors.Background;
            form.ForeColor = Colors.Text;
            form.Font = Typography.Body;
            ApplyBackdropEffect(form, CurrentPreferences.PreferredBackdrop);
        }

        public static void ApplySplitContainerStyle(SplitContainer splitContainer)
        {
            if (splitContainer == null) return;

            splitContainer.BackColor = Colors.Background;
            splitContainer.Panel1.BackColor = Colors.Background;
            splitContainer.Panel2.BackColor = Colors.Surface;
            splitContainer.BorderStyle = BorderStyle.None;
            splitContainer.SplitterWidth = 1;
        }

        public static void ApplyRichTextBoxStyle(RichTextBox richTextBox)
        {
            if (richTextBox == null) return;

            richTextBox.BackColor = Colors.Surface;
            richTextBox.ForeColor = Colors.Text;
            richTextBox.BorderStyle = BorderStyle.None;
            richTextBox.Font = Typography.Body;
            
            // Apply dark scrollbars
            if (richTextBox.IsHandleCreated)
                DarkScrollBar.ApplyDarkTheme(richTextBox);
            else
                richTextBox.HandleCreated += (s, e) => DarkScrollBar.ApplyDarkTheme(richTextBox);
        }

        /// <summary>
        /// Applies modern styling with rounded corners to an output panel.
        /// </summary>
        public static void ApplyModernOutputPanelStyle(Panel outputPanel)
        {
            if (outputPanel == null) return;

            outputPanel.BackColor = Colors.Surface;
            outputPanel.ForeColor = Colors.Text;
            outputPanel.BorderStyle = BorderStyle.None;
            outputPanel.Padding = new Padding(12);

            // Border drawing removed as per user request
        }

        public static void ApplyButtonStyle(Button button, ButtonStyle style, int cornerRadius)
        {
            ApplyButtonStyle(button, style);
            if (cornerRadius > 0 && button != null)
            {
                 // ApplyRoundedCorners(button, cornerRadius); // If ApplyRoundedCorners existed.
                 // Since ApplyRoundedCorners was legacy/deleted, and new code handles it differently,
                 // we might ignore or re-implement.
                 // For now, ignore to fix build.
            }
        }

        public static void ApplyButtonStyle(Button button, ButtonStyle style = ButtonStyle.Standard)
        {
            if (button == null) return;

            button.Font = Typography.Body;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;

            switch (style)
            {
                case ButtonStyle.Primary:
                    button.BackColor = Colors.Primary;
                    button.ForeColor = Color.White;
                    button.FlatAppearance.BorderColor = Colors.Primary;
                    button.FlatAppearance.MouseOverBackColor = Colors.Lighten(Colors.Primary, 20);
                    button.FlatAppearance.MouseDownBackColor = Colors.Darken(Colors.Primary, 10);
                    break;
                    
                case ButtonStyle.Secondary:
                    button.BackColor = Colors.Surface;
                    button.ForeColor = Colors.Text;
                    button.FlatAppearance.BorderColor = Colors.Border;
                    button.FlatAppearance.MouseOverBackColor = Colors.Lighten(Colors.Surface, 10);
                    button.FlatAppearance.MouseDownBackColor = Colors.Darken(Colors.Surface, 5);
                    break;
                    
                case ButtonStyle.Subtle:
                    button.BackColor = Color.Transparent;
                    button.ForeColor = Colors.SubtleText;
                    button.FlatAppearance.BorderSize = 0;
                    button.FlatAppearance.MouseOverBackColor = Colors.Surface;
                    button.FlatAppearance.MouseDownBackColor = Colors.Lighten(Colors.Surface, 5);
                    break;
                    
                case ButtonStyle.Modern:
                    button.BackColor = Colors.Surface;
                    button.ForeColor = Colors.Text;
                    button.FlatAppearance.BorderColor = Colors.Border;
                    button.FlatAppearance.MouseOverBackColor = Colors.Lighten(Colors.Surface, 10);
                    button.FlatAppearance.MouseDownBackColor = Colors.Darken(Colors.Surface, 5);
                    break;
                    
                case ButtonStyle.FuturisticPrimary:
                    ApplyFuturisticButtonStyle(button, true);
                    break;
                    
                case ButtonStyle.FuturisticGhost:
                    ApplyFuturisticButtonStyle(button, false);
                    break;
                    
                default: // Standard
                    button.BackColor = Colors.Surface;
                    button.ForeColor = Colors.Text;
                    button.FlatAppearance.BorderColor = Colors.Border;
                    button.FlatAppearance.MouseOverBackColor = Colors.Lighten(Colors.Surface, 10);
                    button.FlatAppearance.MouseDownBackColor = Colors.Darken(Colors.Surface, 5);
                    break;
            }
        }
        #endregion

        #region Futuristic Settings
        private static bool _futuristicAnimationsEnabled = true;
        
        public static void SetFuturisticAnimationsEnabled(bool enabled)
        {
            _futuristicAnimationsEnabled = enabled;
        }
        
        public static bool IsFuturisticAnimationsEnabled => _futuristicAnimationsEnabled;
        
        private static void NotifyRegisteredControls()
        {
            // Simple implementation - theme change notification is already handled by events
            // This method is kept for compatibility but doesn't need complex implementation
            // since individual controls should listen to OnThemeChanged event
        }
        #endregion

        #region Theme Listening
        public static void ListenForSystemThemeChanges(Control root, Action<ThemeMode>? callback = null)
        {
            try
            {
                SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
                SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

                void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
                {
                    if (e.Category == UserPreferenceCategory.General || 
                        e.Category == UserPreferenceCategory.VisualStyle)
                    {
                        if (root.IsHandleCreated && !root.IsDisposed)
                        {
                            root.BeginInvoke(new Action(() =>
                            {
                                if (CurrentTheme == ThemeMode.System)
                                {
                                    ApplyTheme(root, true);
                                    callback?.Invoke(CurrentTheme);
                                }
                            }));
                        }
                    }
                }
            }
            catch { /* Ignore theme listening errors */ }
        }
        #endregion

        #region Graphics Helpers
        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
        {
            using var path = GetRoundedRectPath(bounds, radius);
            graphics.DrawPath(pen, path);
        }

        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
        {
            using var path = GetRoundedRectPath(bounds, radius);
            graphics.FillPath(brush, path);
        }

        public static GraphicsPath GetRoundedRectPath(Rectangle bounds, float radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0) { path.AddRectangle(bounds); return path; }
            path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90);
            path.AddArc(bounds.X + bounds.Width - radius, bounds.Y, radius, radius, 270, 90);
            path.AddArc(bounds.X + bounds.Width - radius, bounds.Y + bounds.Height - radius, radius, radius, 0, 90);
            path.AddArc(bounds.X, bounds.Y + bounds.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }
        #endregion

        /// <summary>
        /// Applies theme styling to a ProgressBar control
        /// </summary>
        public static void ApplyProgressBarStyle(ProgressBar progressBar)
        {
            if (progressBar == null) return;

            progressBar.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 55) : Color.FromArgb(240, 240, 245);
            progressBar.ForeColor = IsDarkMode ? Color.FromArgb(0, 120, 215) : Color.FromArgb(0, 99, 204);
        }

        /// <summary>
        /// Applies the current theme to a form and its controls
        /// </summary>
        public static bool TryApplyTheme(Form form, DataGridView grid, Button okButton, Button cancelButton, Button selectAllButton, Button clearButton)
        {
            if (form == null) return false;

            try
            {
                ApplyFormStyle(form);
                ApplyTheme(form);

                if (grid != null)
                {
                    ApplyTheme(grid);
                    grid.EnableHeadersVisualStyles = false;
                    grid.ColumnHeadersDefaultCellStyle.BackColor = IsDarkMode ? Color.FromArgb(40, 40, 50) : Color.FromArgb(240, 240, 245);
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = IsDarkMode ? Color.White : Color.Black;
                    grid.RowHeadersDefaultCellStyle.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 55) : Color.FromArgb(230, 230, 235);
                    grid.RowHeadersDefaultCellStyle.ForeColor = IsDarkMode ? Color.White : Color.Black;
                    grid.BackgroundColor = IsDarkMode ? Color.FromArgb(30, 30, 40) : Color.White;
                    grid.DefaultCellStyle.BackColor = IsDarkMode ? Color.FromArgb(30, 30, 40) : Color.White;
                    grid.DefaultCellStyle.ForeColor = IsDarkMode ? Color.White : Color.Black;
                    grid.GridColor = IsDarkMode ? Color.FromArgb(60, 60, 70) : Color.FromArgb(220, 220, 220);
                }

                if (okButton != null) ApplyButtonStyle(okButton, ButtonStyle.FuturisticPrimary);
                if (cancelButton != null) ApplyButtonStyle(cancelButton, ButtonStyle.FuturisticGhost);
                if (selectAllButton != null) ApplyButtonStyle(selectAllButton, ButtonStyle.FuturisticGhost);
                if (clearButton != null) ApplyButtonStyle(clearButton, ButtonStyle.FuturisticGhost);

                return true;
            }
            catch
            {
                return false;
            }
        }
        
        #region Futuristic Style Methods
        private static void ApplyFuturisticButtonStyle(Button button, bool isPrimary = false)
        {
            if (button == null) return;

            // Basic button styling
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.Transparent;
            button.FlatAppearance.MouseDownBackColor = Color.Transparent;
            // Don't set BorderColor when BorderSize is 0 to avoid NotSupportedException
            button.BackColor = Color.Transparent;
            button.ForeColor = Colors.AIText;
            button.Font = Typography.Body;
            button.Padding = new Padding(24, 8, 24, 8);
            button.Height = 42;
            button.Cursor = Cursors.Hand;
            button.TextAlign = ContentAlignment.MiddleCenter;

            // Add glow effect on hover
            button.MouseEnter += (s, e) => button.Invalidate();
            button.MouseLeave += (s, e) => button.Invalidate();
            
            button.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Draw background
                using (var brush = new LinearGradientBrush(
                    button.ClientRectangle,
                    isPrimary ? Colors.AIPrimary : Colors.AISurfaceVariant,
                    isPrimary ? Colors.AIPrimaryVariant : Colors.AISurface,
                    135f))
                {
                    g.FillRoundedRectangle(brush, button.ClientRectangle, 4);
                }
                
                // Draw glow effect on hover
                if (button.ClientRectangle.Contains(button.PointToClient(Cursor.Position)))
                {
                    using (var glowPen = new Pen(Color.FromArgb(100, Colors.AIAccent), 2))
                    {
                        var glowRect = new Rectangle(1, 1, button.Width - 3, button.Height - 3);
                        g.DrawRoundedRectangle(glowPen, glowRect, 4);
                    }
                }
                
                // Draw text with shadow for better contrast
                TextRenderer.DrawText(
                    g,
                    button.Text,
                    button.Font,
                    button.ClientRectangle,
                    button.Enabled ? Colors.AIText : Color.FromArgb(100, 100, 110),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private static void ApplyFuturisticPanelStyle(Panel panel)
        {
            if (panel == null) return;
            
            panel.BackColor = Colors.AISurface;
            panel.BorderStyle = BorderStyle.None;
            
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Draw subtle border
                using (var borderPen = new Pen(Color.FromArgb(40, 40, 55), 1))
                {
                    var borderRect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                    g.DrawRoundedRectangle(borderPen, borderRect, 4);
                }
            };
        }

        private static void ApplyFuturisticTextBoxStyle(TextBox textBox)
        {
            if (textBox == null) return;
            
            textBox.BorderStyle = BorderStyle.None;
            textBox.BackColor = Colors.AISurfaceVariant;
            textBox.ForeColor = Colors.AIText;
            textBox.Font = Typography.Body;
            textBox.Padding = new Padding(8, 6, 8, 6);
            
            textBox.Enter += (s, e) => textBox.Invalidate();
            textBox.Leave += (s, e) => textBox.Invalidate();
            
            textBox.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Draw background
                using (var brush = new SolidBrush(Colors.AISurfaceVariant))
                {
                    g.FillRoundedRectangle(brush, textBox.ClientRectangle, 4);
                }
                
                // Draw border
                using (var borderPen = new Pen(
                    textBox.Focused ? Colors.AIPrimary : Color.FromArgb(60, 60, 75), 
                    textBox.Focused ? 2 : 1))
                {
                    var borderRect = new Rectangle(
                        textBox.Focused ? 0 : 1,
                        textBox.Focused ? 0 : 1,
                        textBox.Width - (textBox.Focused ? 1 : 2),
                        textBox.Height - (textBox.Focused ? 1 : 2));
                        
                    g.DrawRoundedRectangle(borderPen, borderRect, 4);
                }
                
                // Draw text (manually to handle padding)
                if (!string.IsNullOrEmpty(textBox.Text) && textBox.Text.Length > 0)
                {
                    TextRenderer.DrawText(
                        g,
                        textBox.Text,
                        textBox.Font,
                        new Rectangle(8, 6, textBox.Width - 16, textBox.Height - 12),
                        textBox.Enabled ? Colors.AIText : Color.FromArgb(100, 100, 110),
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }
            };
        }
        #endregion
        
        #region Futuristic Theme Classes
        private class FuturisticMenuRenderer : ToolStripProfessionalRenderer
        {
            private readonly FuturisticColorTable _colorTable;
            
            public FuturisticMenuRenderer() : base(new FuturisticColorTable()) 
            { 
                _colorTable = new FuturisticColorTable();
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item.Selected || e.Item.Pressed)
                {
                    using (var brush = new LinearGradientBrush(
                        e.Item.ContentRectangle,
                        Color.FromArgb(40, 40, 60),
                        Color.FromArgb(30, 30, 50),
                        90f))
                    {
                        e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                    }

                    // Draw highlight border
                    using (var pen = new Pen(Colors.AIPrimary, 1))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, e.Item.Width - 1, e.Item.Height - 1);
                    }
                }
                else
                {
                    base.OnRenderMenuItemBackground(e);
                }
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Selected || e.Item.Pressed ? Colors.AIPrimary : Colors.AIText;
                e.TextFont = e.Item.Font ?? Typography.Body;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                if (e is null) return;
                if (e.Item is null)
                {
                    base.OnRenderArrow(e);
                    return;
                }

                e.ArrowColor = e.Item.Enabled ? Colors.AIText : Color.FromArgb(100, 100, 110);
                base.OnRenderArrow(e);
            }
        }

        private class FuturisticColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => Color.FromArgb(40, 40, 60);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(40, 40, 60);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(30, 30, 50);
            public override Color MenuItemBorder => Color.Transparent;
            public override Color MenuBorder => Color.FromArgb(40, 40, 60);
            public override Color MenuItemPressedGradientBegin => Color.FromArgb(50, 50, 70);
            public override Color MenuItemPressedGradientEnd => Color.FromArgb(40, 40, 60);
            public override Color MenuStripGradientBegin => Color.FromArgb(30, 30, 45);
            public override Color MenuStripGradientEnd => Color.FromArgb(20, 20, 35);
            public override Color ToolStripDropDownBackground => Color.FromArgb(25, 25, 40);
            public override Color ImageMarginGradientBegin => Color.FromArgb(25, 25, 40);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(25, 25, 40);
            public override Color ImageMarginGradientEnd => Color.FromArgb(25, 25, 40);
            public override Color SeparatorDark => Color.FromArgb(60, 60, 80);
            public override Color SeparatorLight => Color.FromArgb(80, 80, 100);
        }
        #endregion





    }
}
