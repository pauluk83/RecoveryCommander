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
    public static class Theme
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

        #region Consolidated System Utilities (from SystemUtilities.cs)
        /// <summary>
        /// Unified system utilities - Consolidates error handling, animation, and async operations
        /// Consolidated from: SystemUtilities.cs
        /// </summary>
        public static class SystemUtilities
        {
            #region Error Handling
            public static class ErrorHandler
            {
                public static void HandleError(Exception ex, string context = "")
                {
                    var message = string.IsNullOrEmpty(context) 
                        ? ex.Message 
                        : $"{context}: {ex.Message}";
                    
                    Console.WriteLine($"ERROR: {message}");
                    
                    if (Application.OpenForms.Count > 0)
                    {
                        MessageBox.Show(
                            Application.OpenForms[0], 
                            message, 
                            "Error", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Error);
                    }
                }

                public static void HandleWarning(string message, string context = "")
                {
                    var fullMessage = string.IsNullOrEmpty(context) 
                        ? message 
                        : $"{context}: {message}";
                    
                    Console.WriteLine($"WARNING: {fullMessage}");
                }
            }
            #endregion
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

        // Dynamic color properties that change based on current theme
        public static Color Background => _isDarkMode ? Colors.Dark.Background : Colors.Light.Background;
        public static Color Surface => _isDarkMode ? Colors.Dark.Surface : Colors.Light.Surface;
        public static Color SurfaceVariant => _isDarkMode ? Colors.Dark.SurfaceVariant : Colors.Light.SurfaceVariant;
        public static Color Primary => _isDarkMode ? Colors.Dark.Primary : Colors.Light.Primary;
        public static Color PrimaryVariant => _isDarkMode ? Colors.Dark.PrimaryVariant : Colors.Light.PrimaryVariant;
        public static Color Text => _isDarkMode ? Colors.Dark.Text : Colors.Light.Text;
        public static Color TextSecondary => _isDarkMode ? Colors.Dark.TextSecondary : Colors.Light.TextSecondary;
        public static Color SubtleText => _isDarkMode ? Colors.Dark.SubtleText : Colors.Light.SubtleText;
        public static Color Border => _isDarkMode ? Colors.Dark.Border : Colors.Light.Border;
        public static Color Accent => _isDarkMode ? Colors.Dark.Accent : Colors.Light.Accent;
        public static Color Success => _isDarkMode ? Colors.Dark.Success : Colors.Light.Success;
        public static Color Warning => _isDarkMode ? Colors.Dark.Warning : Colors.Light.Warning;
        public static Color Error => _isDarkMode ? Colors.Dark.Error : Colors.Light.Error;

        public static event Action<ThemeMode>? OnThemeChanged;
        public static event Action<ThemePreferences>? OnThemePreferencesChanged;
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
            ApplyMicaEffect(form, true);
        }

        public static void ApplyMicaEffect(Form form, bool enabled)
        {
            if (form == null || !form.IsHandleCreated) return;

            try
            {
                int darkMode = _isDarkMode ? 1 : 0;
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                if (Environment.OSVersion.Version.Build >= 22000) // Windows 11
                {
                    int backdropType = enabled ? 2 : 0; // 2 = Mica
                    DwmSetWindowAttribute(form.Handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
                }
                else if (Environment.OSVersion.Version.Major >= 10 && enabled) // Windows 10
                {
                    int trueValue = 1;
                    DwmSetWindowAttribute(form.Handle, DWMWA_MICA_EFFECT, ref trueValue, sizeof(int));
                }
                
                form.BackColor = Color.Black; // Required for Mica to show through
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

        public class RoundedProgressBar : Control
        {
            private int _value = 0;
            private int _minimum = 0;
            private int _maximum = 100;
            private bool _isIndeterminate = false;
            private string _statusText = "";
            private System.Windows.Forms.Timer? _animationTimer;
            private float _animationOffset = 0;

            public RoundedProgressBar()
            {
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
                DoubleBuffered = true;
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                _animationTimer = new System.Windows.Forms.Timer { Interval = 20 };
                _animationTimer.Tick += (s, ev) =>
                {
                    if (_isIndeterminate)
                    {
                        _animationOffset += 0.05f;
                        if (_animationOffset > 1.0f) _animationOffset = 0;
                        Invalidate();
                    }
                };
                if (_isIndeterminate) _animationTimer.Start();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _animationTimer?.Dispose();
                base.Dispose(disposing);
            }

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Visible)]
            public int Value
            {
                get => _value;
                set
                {
                    _value = Math.Max(_minimum, Math.Min(_maximum, value));
                    Invalidate();
                }
            }

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Visible)]
            public int Minimum
            {
                get => _minimum;
                set
                {
                    _minimum = value;
                    if (_value < _minimum) _value = _minimum;
                    Invalidate();
                }
            }

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Visible)]
            public int Maximum
            {
                get => _maximum;
                set
                {
                    _maximum = value;
                    if (_value > _maximum) _value = _maximum;
                    Invalidate();
                }
            }

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Visible)]
            public bool IsIndeterminate
            {
                get => _isIndeterminate;
                set
                {
                    if (_isIndeterminate == value) return;
                    _isIndeterminate = value;
                    if (_isIndeterminate)
                        _animationTimer?.Start();
                    else
                        _animationTimer?.Stop();
                    Invalidate();
                }
            }

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Visible)]
            public string StatusText
            {
                get => _statusText;
                set
                {
                    _statusText = value;
                    Invalidate();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                var clientRect = new Rectangle(0, 0, Width, Height);

                // Get current theme colors
                var surfaceColor = Theme.Surface;
                var primaryColor = Theme.Primary;
                var borderColor = Theme.Border;
                var textColor = Theme.Text;

                float radius = 10f; // Rounded corner radius for modern look

                using (var path = GetRoundedRectPath(clientRect, radius))
                {
                    // Clip all drawing to the rounded rectangle
                    g.SetClip(path);

                    // Background with rounded corners
                    using (var bgBrush = new SolidBrush(surfaceColor))
                        g.FillPath(bgBrush, path);

                    // Draw Progress
                    if (_isIndeterminate)
                    {
                        // Draw a modern marquee effect
                        int marqueeWidth = Width / 3;
                        int x = (int)((Width + marqueeWidth) * _animationOffset) - marqueeWidth;
                        var marqueeRect = new Rectangle(x, 0, marqueeWidth, Height);
                        
                        using (var lgb = new LinearGradientBrush(marqueeRect, Color.Transparent, primaryColor, 0f))
                        {
                            ColorBlend cb = new ColorBlend(3);
                            cb.Colors = new[] { Color.Transparent, primaryColor, Color.Transparent };
                            cb.Positions = new[] { 0f, 0.5f, 1.0f };
                            lgb.InterpolationColors = cb;
                            g.FillRectangle(lgb, marqueeRect);
                        }
                    }
                    else if (_value > _minimum)
                    {
                        float ratio = (float)(_value - _minimum) / (_maximum - _minimum);
                        var progressRect = new Rectangle(0, 0, (int)(Width * ratio), Height);
                        if (progressRect.Width > 0)
                        {
                            using (var progressBrush = new SolidBrush(primaryColor))
                                g.FillRectangle(progressBrush, progressRect);
                        }
                    }

                    g.ResetClip();

                    // Subtle Border with rounded corners
                    using (var borderPath = GetRoundedRectPath(rect, radius))
                    using (var pen = new Pen(Color.FromArgb(50, borderColor), 1))
                        g.DrawPath(pen, borderPath);
                }

                // Text centered and bold
                if (!string.IsNullOrEmpty(_statusText))
                {
                    using (var font = new Font(Typography.DefaultFontFamily, 12f, FontStyle.Bold))
                    {
                        var textSize = g.MeasureString(_statusText, font);
                        var textPos = new PointF(
                            (Width - textSize.Width) / 2,
                            (Height - textSize.Height) / 2);

                        // Draw shadow for readability if it's over the progress chunk
                        using (var shadowBrush = new SolidBrush(Color.FromArgb(100, Color.Black)))
                        {
                            g.DrawString(_statusText, font, shadowBrush, new PointF(textPos.X + 1, textPos.Y + 1));
                        }
                        
                        using (var brush = new SolidBrush(Color.White)) // Always white for best contrast on primary colors
                        {
                            g.DrawString(_statusText, font, brush, textPos);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// FlowLayoutPanel with dark scrollbars
        /// </summary>
        public class DarkFlowLayoutPanel : FlowLayoutPanel
        {
            private CustomScrollBar? _customScrollBar;

            public DarkFlowLayoutPanel(bool enableScroll = true)
            {
                BackColor = Theme.Surface;
                ForeColor = Theme.Text;
                AutoScroll = enableScroll;

                if (enableScroll)
                {
                    Resize += (s, e) => UpdateCustomScrollbar();
                    ControlAdded += (s, e) => UpdateCustomScrollbar();
                    ControlRemoved += (s, e) => UpdateCustomScrollbar();
                }
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                Theme.DarkScrollBar.ApplyDarkTheme(this);
                EnsureCustomScrollbar();
                UpdateCustomScrollbar();
            }

            private void EnsureCustomScrollbar()
            {
                if (_customScrollBar == null)
                {
                    _customScrollBar = new CustomScrollBar
                    {
                        Dock = DockStyle.Right,
                        Visible = false
                    };
                    _customScrollBar.Scroll += OnCustomScroll;
                    Controls.Add(_customScrollBar);
                }
            }

            private void OnCustomScroll(object? sender, ScrollEventArgs e)
            {
                AutoScrollPosition = new Point(-AutoScrollPosition.X, -e.NewValue);
            }

            private void UpdateCustomScrollbar()
            {
                if (!IsHandleCreated)
                    return;

                EnsureCustomScrollbar();

                // Hide the native scrollbar to avoid light styling
                DarkScrollBar.HideScrollBar(Handle, 1); // SB_VERT

                var contentHeight = DisplayRectangle.Height;
                var viewportHeight = ClientSize.Height;

                if (contentHeight > viewportHeight)
                {
                    _customScrollBar!.Visible = true;
                    _customScrollBar.Maximum = Math.Max(0, contentHeight - 1);
                    _customScrollBar.LargeChange = Math.Max(1, viewportHeight);
                    _customScrollBar.SmallChange = 20;
                    _customScrollBar.Value = Math.Max(0, -AutoScrollPosition.Y);
                }
                else
                {
                    _customScrollBar!.Visible = false;
                    AutoScrollPosition = new Point(0, 0);
                }
            }
        }


        #endregion



        public static void DrawElevatedShadow(Graphics g, Rectangle bounds, int elevation)
        {
             using (var path = GetRoundedRectPath(bounds, 8))
             {
                 // Draw a subtle border/shadow effect
                 using (var pen = new Pen(Colors.Border, 1)) // Use a defined color
                 {
                     g.DrawPath(pen, path);
                 }
             }
        }
            public static void DrawCardShadow(Graphics g, Rectangle bounds) => DrawElevatedShadow(g, bounds, 2);

            public static void DrawDivider(Graphics g, Rectangle bounds, bool isVertical = false)
            {
                if (g == null) return;
                using (var pen = new Pen(Color.FromArgb(30, Colors.Border), 1))
                {
                    if (isVertical)
                        g.DrawLine(pen, bounds.X, bounds.Y, bounds.X, bounds.Height);
                    else
                        g.DrawLine(pen, bounds.X, bounds.Y, bounds.Width, bounds.Y);
                }
            }

            public static Panel CreateEmptyState(string title, string description, string? icon = null)
            {
                var panel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent,
                    Padding = new Padding(Spacing.Spacious)
                };
                
                var container = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 3,
                    ColumnCount = 1,
                    BackColor = Color.Transparent,
                    AutoSize = true
                };
                
                container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                
                if (!string.IsNullOrEmpty(icon))
                {
                    var iconLabel = new Label
                    {
                        Text = icon,
                        Font = new Font("Segoe UI Emoji", 48f),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Dock = DockStyle.Fill,
                        AutoSize = false,
                        Height = 80,
                        ForeColor = Color.FromArgb(100, Colors.Text),
                        BackColor = Color.Transparent
                    };
                    container.Controls.Add(iconLabel, 0, 0);
                }
                
                var titleLabel = new Label
                {
                    Text = title,
                    Font = Typography.Header,
                    ForeColor = Colors.Text,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    AutoSize = false,
                    Height = 28,
                    Padding = new Padding(0, Spacing.MD, 0, Spacing.SM),
                    BackColor = Color.Transparent
                };
                container.Controls.Add(titleLabel, 0, icon != null ? 1 : 0);
                
                var descLabel = new Label
                {
                    Text = description,
                    Font = Typography.Body,
                    ForeColor = Color.FromArgb(180, Colors.Text),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    AutoSize = false,
                    Height = 40,
                    Padding = new Padding(Spacing.XL, 0, Spacing.XL, 0),
                    BackColor = Color.Transparent
                };
                container.Controls.Add(descLabel, 0, icon != null ? 2 : 1);
                
                panel.Controls.Add(container);
                return panel;
            }

            public static void ApplyProfessionalButtonStyle(Button button, bool isPrimary = false)
            {
                if (button == null) return;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                button.Padding = new Padding(Spacing.LG, Spacing.SM, Spacing.LG, Spacing.SM);
                button.Height = 40;
                button.Font = Typography.BodyStrong;
                button.Cursor = Cursors.Hand;
                
                if (isPrimary)
                {
                    button.BackColor = Colors.Primary;
                    button.ForeColor = Color.White;
                }
                else
                {
                    button.BackColor = Colors.Surface;
                    button.ForeColor = Colors.Text;
                }
                
                button.MouseEnter += (s, e) =>
                {
                    if (isPrimary)
                        button.BackColor = Colors.Lighten(Colors.Primary, 15);
                    else
                        button.BackColor = Colors.Lighten(Colors.Surface, 10);
                };
                
                button.MouseLeave += (s, e) =>
                {
                    if (isPrimary)
                        button.BackColor = Colors.Primary;
                    else
                        button.BackColor = Colors.Surface;
                };
            }




        #region Responsive Design System (from ResponsiveDesignManager.cs)
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

        #region ScrollBar Support
        public static class DarkScrollBar
        {
            public static void ApplyTo(Control control) => ApplyDarkTheme(control);
            
            [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
            private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

            [DllImport("uxtheme.dll", EntryPoint = "#135", CharSet = CharSet.Unicode)]
            private static extern int SetPreferredAppMode(int preferredAppMode);

            [DllImport("uxtheme.dll", EntryPoint = "#136", CharSet = CharSet.Unicode)]
            private static extern void FlushMenuThemes();

            [DllImport("user32.dll")]
            private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

            public static void HideScrollBar(IntPtr handle, int sbCode) => ShowScrollBar(handle, sbCode, false);
            
            // Public method for RichTextBox dark scrollbars
            public static void ApplyDarkThemeToRichTextBox(RichTextBox richTextBox)
            {
                if (richTextBox?.IsHandleCreated != true) return;
                
                try {
                    SetPreferredAppMode(2);
                    FlushMenuThemes();
                    
                    var themeNames = new[] { "DarkMode_Explorer", "Explorer", "DarkMode_CFD", "DarkMode" };
                    foreach (var theme in themeNames) {
                        if (SetWindowTheme(richTextBox.Handle, theme, string.Empty) == 0) break;
                    }
                    
                    richTextBox.Invalidate();
                    richTextBox.Update();
                } catch { }
            }

            public static void ApplyDarkTheme(Control control)
            {
                if (control?.IsHandleCreated != true) return;

                try
                {
                    SetPreferredAppMode(2);
                    FlushMenuThemes();
                    SetWindowTheme(control.Handle, "DarkMode_Explorer", string.Empty);

                    if (control is TextBoxBase textBox) ApplyDarkThemeToTextBox(textBox);
                    else if (control is ListBox listBox) ApplyDarkThemeToControl(listBox);
                    else if (control is TreeView treeView) ApplyDarkThemeToControl(treeView);
                    else if (control is ListView listView) ApplyDarkThemeToControl(listView);
                }
                catch { }
            }

            private static void ApplyDarkThemeToTextBox(TextBoxBase textBox)
            {
                try
                {
                    var themeNames = new[] { "DarkMode_Explorer", "Explorer", "DarkMode_CFD", "DarkMode" };
                    foreach (var theme in themeNames)
                    {
                        if (SetWindowTheme(textBox.Handle, theme, string.Empty) == 0) break;
                    }

                    textBox.Invalidate();
                    textBox.Update();
                }
                catch { }
            }

            private static void ApplyDarkThemeToControl(Control control)
            {
                try
                {
                    SetWindowTheme(control.Handle, "DarkMode_Explorer", string.Empty);
                    control.BackColor = Colors.Surface;
                    control.ForeColor = Colors.Text;
                }
                catch { }
            }

            public static void ApplyDarkThemeRecursive(Control container)
            {
                if (container == null) return;
                ApplyDarkTheme(container);
                foreach (Control child in container.Controls)
                {
                    ApplyDarkThemeRecursive(child);
                }
            }
        }

        public class CustomScrollBar : Control
        {
            private int _minimum = 0;
            private int _maximum = 100;
            private int _value = 0;
            private int _largeChange = 10;
            private int _smallChange = 1;
            private bool _isDragging;
            private Point _lastMousePos;
            private Rectangle _thumbRect;
            private bool _thumbHovered;
            private bool _thumbPressed;

            public event EventHandler<ScrollEventArgs>? Scroll;

            [System.ComponentModel.DefaultValue(0)]
            public int Minimum
            {
                get => _minimum;
                set
                {
                    _minimum = value;
                    if (_value < _minimum) _value = _minimum;
                    Invalidate();
                }
            }

            [System.ComponentModel.DefaultValue(100)]
            public int Maximum
            {
                get => _maximum;
                set
                {
                    _maximum = value;
                    if (_value > _maximum) _value = _maximum;
                    Invalidate();
                }
            }

            [System.ComponentModel.DefaultValue(0)]
            public int Value
            {
                get => _value;
                set
                {
                    var newValue = Math.Max(_minimum, Math.Min(_maximum, value));
                    if (newValue != _value)
                    {
                        _value = newValue;
                        Invalidate();
                        Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.ThumbPosition, _value));
                    }
                }
            }

            [System.ComponentModel.DefaultValue(10)]
            public int LargeChange
            {
                get => _largeChange;
                set => _largeChange = Math.Max(1, value);
            }

            [System.ComponentModel.DefaultValue(1)]
            public int SmallChange
            {
                get => _smallChange;
                set => _smallChange = Math.Max(1, value);
            }

            public CustomScrollBar()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                         ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
                Width = 17;
                BackColor = Colors.Surface;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Draw track background
                using (var trackBrush = new SolidBrush(Color.FromArgb(45, 45, 55)))
                {
                    g.FillRectangle(trackBrush, ClientRectangle);
                }

                CalculateThumbRect();

                // Modern scrollbar thumb with rounded corners
                var thumbColor = _thumbPressed ? Color.FromArgb(120, 120, 130) :
                                 _thumbHovered ? Color.FromArgb(100, 100, 110) :
                                 Color.FromArgb(80, 80, 90);

                using (var thumbBrush = new SolidBrush(thumbColor))
                {
                    // Create rounded rectangle for thumb
                    var thumbRect = new Rectangle(_thumbRect.X + 3, _thumbRect.Y, _thumbRect.Width - 6, _thumbRect.Height);
                    if (thumbRect.Height > 0 && thumbRect.Width > 0)
                    {
                        using (var thumbPath = GetRoundedRectPath(thumbRect, 4))
                        {
                            g.FillPath(thumbBrush, thumbPath);
                        }
                    }
                }
            }

            private void CalculateThumbRect()
            {
                if (_maximum <= _minimum) return;

                var trackHeight = Height;
                var thumbHeight = Math.Max(20, (int)(trackHeight * 0.1));
                var trackRange = trackHeight - thumbHeight;
                var valueRange = _maximum - _minimum;
                var thumbTop = (int)(trackRange * (_value - _minimum) / (double)valueRange);

                _thumbRect = new Rectangle(0, thumbTop, Width, thumbHeight);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button == MouseButtons.Left)
                {
                    if (_thumbRect.Contains(e.Location))
                    {
                        _isDragging = true;
                        _thumbPressed = true;
                        _lastMousePos = e.Location;
                        Capture = true;
                        Invalidate();
                    }
                    else
                    {
                        Value = e.Y < _thumbRect.Y ? _value - _largeChange : _value + _largeChange;
                    }
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (_isDragging)
                {
                    var deltaY = e.Y - _lastMousePos.Y;
                    var trackRange = Height - _thumbRect.Height;
                    var valueRange = _maximum - _minimum;
                    var valueChange = (int)(deltaY * valueRange / (double)trackRange);
                    Value = _value + valueChange;
                    _lastMousePos = e.Location;
                }
                else
                {
                    var wasHovered = _thumbHovered;
                    _thumbHovered = _thumbRect.Contains(e.Location);
                    if (wasHovered != _thumbHovered) Invalidate();
                }
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (_isDragging)
                {
                    _isDragging = false;
                    _thumbPressed = false;
                    Capture = false;
                    Invalidate();
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _thumbHovered = false;
                Invalidate();
            }
        }

        public class RoundedRichTextBox : Control
        {
            private const int CornerRadius = 12;
            private const int ScrollBarWidth = 17;
            private readonly List<string> _originalLines = new List<string>();
            private readonly List<string> _displayLines = new List<string>();
            private readonly List<Color> _lineColors = new List<Color>();
            private readonly CustomScrollBar _scrollBar;
            private int _scrollPosition = 0;
            private int _visibleLines = 0;
            private Font _font = new Font("Consolas", 9.5f);
            private bool _wordWrap = true;

            [Category("Appearance")]
            [DefaultValue(true)]
            public bool WordWrap
            {
                get => _wordWrap;
                set
                {
                    if (_wordWrap != value)
                    {
                        _wordWrap = value;
                        ReWrapAll();
                    }
                }
            }

            #pragma warning disable CS8764, CS8765 // Nullability mismatch warnings for overridden properties
            public override Font Font
            {
                get => _font;
                set
                {
                    _font = value ?? new Font("Consolas", 9.5f);
                    ReWrapAll();
                }
            }
            
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public bool ReadOnly { get; set; } = true;
            
            public override Color ForeColor { get; set; } = Colors.Text;
            public override Color BackColor { get; set; } = Colors.Surface;
#pragma warning restore CS8764, CS8765

            public RoundedRichTextBox()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.DoubleBuffer |
                         ControlStyles.ResizeRedraw, true);

                BackColor = Colors.Surface;
                ForeColor = Colors.Text;

                _scrollBar = new CustomScrollBar
                {
                    Dock = DockStyle.Right,
                    Width = ScrollBarWidth,
                    Visible = false,
                    Minimum = 0
                };
                
                _scrollBar.Scroll += OnScrollBarScroll;
                Controls.Add(_scrollBar);
                
                Resize += OnResize;
                
                // Add mouse wheel support for scrolling
                MouseWheel += OnMouseWheel;
                
                // Enable double buffering for smoother rendering
                SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            }

            public void Clear()
            {
                _originalLines.Clear();
                _displayLines.Clear();
                _lineColors.Clear();
                _scrollPosition = 0;
                UpdateScrollBar();
                Invalidate();
            }

            private void ReWrapAll()
            {
                _displayLines.Clear();
                _lineColors.Clear();

                if (_originalLines.Count == 0)
                {
                    UpdateScrollBar();
                    Invalidate();
                    return;
                }

                int maxWidth = Width - 32 - (_scrollBar.Visible ? ScrollBarWidth : 0);
                if (maxWidth <= 0) maxWidth = 100;

                using var g = IsHandleCreated ? CreateGraphics() : null;
                
                foreach (var line in _originalLines)
                {
                    var highlightedColor = ApplySyntaxHighlighting(line);
                    if (!_wordWrap || g == null)
                    {
                        _displayLines.Add(line);
                        _lineColors.Add(highlightedColor);
                    }
                    else
                    {
                        var wrapped = WrapLine(g, line, maxWidth);
                        foreach (var w in wrapped)
                        {
                            _displayLines.Add(w);
                            _lineColors.Add(highlightedColor);
                        }
                    }
                }
                UpdateScrollBar();
                Invalidate();
            }

            private List<string> WrapLine(Graphics g, string line, int maxWidth)
            {
                var result = new List<string>();
                if (string.IsNullOrEmpty(line))
                {
                    result.Add("");
                    return result;
                }

                // Simple word wrap
                var words = line.Split(' ');
                var currentLine = "";

                foreach (var word in words)
                {
                    var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                    var size = g.MeasureString(testLine, _font);

                    if (size.Width > maxWidth && !string.IsNullOrEmpty(currentLine))
                    {
                        result.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = testLine;
                    }
                }

                if (!string.IsNullOrEmpty(currentLine))
                    result.Add(currentLine);

                return result;
            }

            public void AppendText(string text, Color color)
            {
                var lines = text.Split('\n');
                using var g = IsHandleCreated ? CreateGraphics() : null;
                int maxWidth = Width - 32 - (_scrollBar.Visible ? ScrollBarWidth : 0);
                if (maxWidth <= 0) maxWidth = 100;

                foreach (var line in lines)
                {
                    var cleanLine = line.TrimEnd('\r');
                    _originalLines.Add(cleanLine);
                    
                    var highlightedColor = color == ForeColor ? ApplySyntaxHighlighting(cleanLine) : color;

                    if (!_wordWrap || g == null)
                    {
                        _displayLines.Add(cleanLine);
                        _lineColors.Add(highlightedColor);
                    }
                    else
                    {
                        var wrapped = WrapLine(g, cleanLine, maxWidth);
                        foreach (var w in wrapped)
                        {
                            _displayLines.Add(w);
                            _lineColors.Add(highlightedColor);
                        }
                    }
                }
                UpdateScrollBar();
                Invalidate();
            }
            
            private Color ApplySyntaxHighlighting(string line)
            {
                var lowerLine = line.ToLowerInvariant();
                
                // Error patterns
                if (lowerLine.Contains("[error]") || lowerLine.Contains("error:") || 
                    lowerLine.Contains("failed") || lowerLine.Contains("exception") ||
                    lowerLine.Contains("✗") || lowerLine.StartsWith("error"))
                {
                    return Color.FromArgb(255, 100, 100); // Red
                }
                
                // Success patterns
                if (lowerLine.Contains("[success]") || lowerLine.Contains("success:") ||
                    lowerLine.Contains("completed") || lowerLine.Contains("succeeded") ||
                    lowerLine.Contains("✓") || lowerLine.Contains("done"))
                {
                    return Color.FromArgb(100, 255, 100); // Green
                }
                
                // Warning patterns
                if (lowerLine.Contains("[warning]") || lowerLine.Contains("warning:") ||
                    lowerLine.Contains("⚠") || lowerLine.Contains("caution"))
                {
                    return Color.FromArgb(255, 200, 100); // Orange
                }
                
                // Info patterns
                if (lowerLine.Contains("[info]") || lowerLine.Contains("info:"))
                {
                    return Color.FromArgb(150, 200, 255); // Light blue
                }
                
                // Timestamp patterns (make them more subtle)
                if (System.Text.RegularExpressions.Regex.IsMatch(line, @"\[\d{2}:\d{2}:\d{2}\]"))
                {
                    return Color.FromArgb(140, 140, 150); // Gray
                }
                
                // Command patterns
                if (line.StartsWith(">") || line.StartsWith("$") || line.StartsWith("PS "))
                {
                    return Color.FromArgb(200, 200, 255); // Light purple
                }
                
                // Default color
                return ForeColor;
            }

            public void AppendText(string text)
            {
                AppendText(text, ForeColor);
            }

            public void BeginUpdate() { }
            public void EndUpdate() { Invalidate(); }
            public new void SuspendLayout() { }
            public new void ResumeLayout() { }

            public void ScrollToCaret()
            {
                if (_displayLines.Count > _visibleLines)
                {
                    _scrollPosition = Math.Max(0, _displayLines.Count - _visibleLines);
                    _scrollBar.Value = _scrollPosition;
                    Invalidate();
                }
                else if (_displayLines.Count > 0)
                {
                    _scrollPosition = 0;
                    _scrollBar.Value = 0;
                    Invalidate();
                }
            }

            #pragma warning disable CS8764, CS8765 // Nullability mismatch warnings for overridden properties
            public override string Text
            {
                get => string.Join(Environment.NewLine, _originalLines);
                set
                {
                    _originalLines.Clear();
                    if (!string.IsNullOrEmpty(value))
                    {
                        var lines = value!.Split('\n');
                        foreach (var line in lines)
                        {
                            _originalLines.Add(line.TrimEnd('\r'));
                        }
                    }
                    ReWrapAll();
                }
            }
#pragma warning restore CS8764, CS8765

            private void OnScrollBarScroll(object? sender, ScrollEventArgs e)
            {
                _scrollPosition = e.NewValue;
                Invalidate();
            }

            private void OnResize(object? sender, EventArgs e)
            {
                ReWrapAll();
            }

            private void UpdateScrollBar()
            {
                _visibleLines = Math.Max(1, Height / _font.Height);
                
                if (_displayLines.Count > _visibleLines)
                {
                    _scrollBar.Visible = true;
                    _scrollBar.Maximum = Math.Max(0, _displayLines.Count - 1);
                    _scrollBar.LargeChange = Math.Max(1, _visibleLines);
                    _scrollBar.SmallChange = 1;
                    
                    if (_scrollPosition > _scrollBar.Maximum)
                    {
                        _scrollPosition = _scrollBar.Maximum;
                    }
                    _scrollBar.Value = _scrollPosition;
                }
                else
                {
                    _scrollBar.Visible = false;
                    _scrollPosition = 0;
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = GetRoundedRectPath(bounds, CornerRadius))
                {
                    using (var brush = new SolidBrush(BackColor))
                        g.FillPath(brush, path);
                    
                    using (var borderPen = new Pen(Color.FromArgb(60, 60, 70), 1.5f))
                        g.DrawPath(borderPen, path);
                }

                var textBounds = new Rectangle(8, 8, Width - 16 - (_scrollBar.Visible ? ScrollBarWidth : 0), Height - 16);
                using (var clipPath = GetRoundedRectPath(textBounds, CornerRadius - 4))
                {
                    g.SetClip(clipPath);
                }

                var y = 8;
                var maxLineIndex = Math.Max(0, _displayLines.Count - _visibleLines);
                var actualScrollPosition = Math.Min(_scrollPosition, maxLineIndex);
                
                if (actualScrollPosition >= _displayLines.Count)
                    actualScrollPosition = Math.Max(0, _displayLines.Count - _visibleLines);
                
                var linesToDraw = Math.Min(_visibleLines, _displayLines.Count - actualScrollPosition);

                for (int i = 0; i < linesToDraw && actualScrollPosition + i < _displayLines.Count; i++)
                {
                    var line = _displayLines[actualScrollPosition + i];
                    var baseColor = actualScrollPosition + i < _lineColors.Count ? _lineColors[actualScrollPosition + i] : ForeColor;
                    
                    if (y + _font.Height <= Height - 8)
                    {
                        using (var brush = new SolidBrush(baseColor))
                            g.DrawString(line, _font, brush, 8, y);
                    }
                    y += _font.Height;
                }
            }

            private void OnMouseWheel(object? sender, MouseEventArgs e)
            {
                if (!_scrollBar.Visible) return;
                
                var delta = e.Delta / 120;
                var newValue = _scrollPosition - (delta * _scrollBar.SmallChange);
                newValue = Math.Max(0, Math.Min(_scrollBar.Maximum - _visibleLines + 1, newValue));
                
                if (newValue != _scrollPosition)
                {
                    _scrollPosition = newValue;
                    _scrollBar.Value = _scrollPosition;
                    Invalidate();
                }
            }

            protected override void OnPaintBackground(PaintEventArgs e) { }

        }

        public class RoundedPanel : Panel
        {
            private const int CornerRadius = 12;

            public RoundedPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.DoubleBuffer, true);

                BackColor = Colors.Surface;
                BorderStyle = BorderStyle.None;
                Padding = new Padding(8);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (Width <= 0 || Height <= 0) return;

                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Draw rounded rectangle background
                var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = GetRoundedRectPath(bounds, CornerRadius))
                {
                    using (var brush = new SolidBrush(Colors.Surface))
                    {
                        g.FillPath(brush, path);
                    }
                    
                    using (var pen = new Pen(Colors.Border, 1.5f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                // Don't call base - we handle all painting in OnPaint
            }
        }

        public class DarkRichTextBox : RichTextBox
        {
            private CustomScrollBar? _customScrollBar;
            private const int CornerRadius = 12;

            public DarkRichTextBox()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.DoubleBuffer, true);

                BackColor = Colors.Surface;
                ForeColor = Colors.Text;
                BorderStyle = BorderStyle.None;
                ScrollBars = RichTextBoxScrollBars.None;

                _customScrollBar = new CustomScrollBar
                {
                    Dock = DockStyle.Right,
                    Width = 17
                };

                _customScrollBar.Scroll += OnCustomScroll;
                Controls.Add(_customScrollBar);

                TextChanged += UpdateScrollBar;
                Resize += UpdateScrollBar;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                // Draw rounded background first
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = GetRoundedRectPath(bounds, CornerRadius))
                {
                    using (var brush = new SolidBrush(Colors.Surface))
                    {
                        g.FillPath(brush, path);
                    }
                    
                    using (var pen = new Pen(Colors.Border, 1.5f))
                    {
                        g.DrawPath(pen, path);
                    }
                }

                // Set clipping region for text content
                var textBounds = new Rectangle(8, 8, Width - 16 - (_customScrollBar?.Visible == true ? 17 : 0), Height - 16);
                using (var clipPath = GetRoundedRectPath(textBounds, CornerRadius - 4))
                {
                    g.SetClip(clipPath);
                }

                // Call base to draw native text content
                base.OnPaint(e);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                // Don't call base - we handle all background painting
            }

            private void OnCustomScroll(object? sender, ScrollEventArgs e)
            {
                var lines = Lines.Length;
                var visibleLines = Height / Font.Height;
                var maxScroll = Math.Max(0, lines - visibleLines);
                var scrollPosition = (int)(maxScroll * (e.NewValue / 100.0));
                SendMessage(Handle, 0x115, scrollPosition, 0);
            }

            private void UpdateScrollBar(object? sender, EventArgs e)
            {
                if (_customScrollBar == null) return;
                var lines = Lines.Length;
                var visibleLines = Height / Font.Height;

                if (lines > visibleLines)
                {
                    _customScrollBar.Visible = true;
                    _customScrollBar.Maximum = 100;
                    _customScrollBar.LargeChange = Math.Max(1, (int)(100.0 * visibleLines / lines));
                }
                else
                {
                    _customScrollBar.Visible = false;
                }
            }

            [DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (_customScrollBar != null)
                    {
                        _customScrollBar.Scroll -= OnCustomScroll;
                        _customScrollBar.Dispose();
                    }
                }
                base.Dispose(disposing);
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
            ApplyMicaEffect(form, IsDarkMode);
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

        #region Error Handling
        /// <summary>
        /// Centralized error handling for the application
        /// </summary>
        public static class ErrorHandler
        {
            public static void HandleError(Exception ex, string context = "")
            {
                var message = string.IsNullOrEmpty(context) 
                    ? ex.Message 
                    : $"{context}: {ex.Message}";
                
                Console.WriteLine($"ERROR: {message}");
                
                if (System.Windows.Forms.Application.OpenForms.Count > 0)
                {
                    System.Windows.Forms.MessageBox.Show(
                        System.Windows.Forms.Application.OpenForms[0], 
                        message, 
                        "Error", 
                        System.Windows.Forms.MessageBoxButtons.OK, 
                        System.Windows.Forms.MessageBoxIcon.Error);
                }
            }

            public static void HandleWarning(string message, string context = "")
            {
                var fullMessage = string.IsNullOrEmpty(context) 
                    ? message 
                    : $"{context}: {message}";
                
                Console.WriteLine($"WARNING: {fullMessage}");
            }
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

        #region Theme Preferences
        /// <summary>
        /// Theme preferences management
        /// </summary>
        public sealed class ThemePreferences
        {
            private const string PreferencesFileName = "theme_preferences.json";
            private static readonly JsonSerializerOptions _jsonOptions = new()
            {
                WriteIndented = true
            };

            public ThemeMode PreferredTheme { get; set; } = ThemeMode.System;
            public bool UseSystemAccent { get; set; } = true;
            public bool ReduceMotion { get; set; } = false;
            public bool HighContrast { get; set; } = false;
            public bool EnableFuturisticAnimations { get; set; } = true;

            private static string GetPreferencesPath()
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var folder = Path.Combine(appData, "RecoveryCommander");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

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
                        var prefs = JsonSerializer.Deserialize<ThemePreferences>(json);
                        if (prefs != null)
                        {
                            return prefs;
                        }
                    }
                }
                catch
                {
                    // Ignore and fall back to defaults
                }

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
                catch
                {
                    // Non-fatal: ignore persistence errors
                }
            }

            public ThemePreferences Clone()
            {
                return new ThemePreferences
                {
                    PreferredTheme = PreferredTheme,
                    UseSystemAccent = UseSystemAccent,
                    ReduceMotion = ReduceMotion,
                    HighContrast = HighContrast,
                    EnableFuturisticAnimations = EnableFuturisticAnimations
                };
            }
        }
        #endregion

        #region UI Utilities
        /// <summary>
        /// UI utility methods (non-extension methods)
        /// Consolidated from: UIExtensions.cs
        /// </summary>
        public static class UIUtils
        {
            /// <summary>
            /// Set control visibility with animation
            /// </summary>
            public static void SetVisibleAnimated(Control control, bool visible)
            {
                if (visible)
                    control.Show();
                else
                    control.Hide();
            }

            /// <summary>
            /// Fade in control
            /// </summary>
            public static void FadeIn(Control control, int duration = 300)
            {
                control.Show();
                // Placeholder for fade animation
            }

            /// <summary>
            /// Fade out control
            /// </summary>
            public static void FadeOut(Control control, int duration = 300)
            {
                control.Hide();
                // Placeholder for fade animation
            }

            /// <summary>
            /// Center form on screen
            /// </summary>
            public static void CenterOnScreen(Form form)
            {
                var screen = System.Windows.Forms.Screen.PrimaryScreen;
                if (screen != null)
                {
                    form.Left = (screen.WorkingArea.Width - form.Width) / 2;
                    form.Top = (screen.WorkingArea.Height - form.Height) / 2;
                }
            }

            /// <summary>
            /// Apply dark theme to control
            /// </summary>
            public static void ApplyDarkTheme(Control control)
            {
                Theme.ApplyTheme(control);
            }

            /// <summary>
            /// Get rounded rectangle path
            /// </summary>
            public static System.Drawing.Drawing2D.GraphicsPath GetRoundedRect(System.Drawing.Rectangle rect, int radius)
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                
                if (radius == 0)
                {
                    path.AddRectangle(rect);
                    return path;
                }

                int diameter = radius * 2;
                var arc = new System.Drawing.Rectangle(rect.Location, new System.Drawing.Size(diameter, diameter));

                // Top-left arc
                path.AddArc(arc, 180, 90);
                // Top-right arc
                arc.X = rect.Right - diameter;
                path.AddArc(arc, 270, 90);
                // Bottom-right arc
                arc.Y = rect.Bottom - diameter;
                path.AddArc(arc, 0, 90);
                // Bottom-left arc
                arc.X = rect.Left;
                path.AddArc(arc, 90, 90);

                path.CloseFigure();
                return path;
            }
        }
        #endregion

        #region UI Animations
        /// <summary>
        /// Animation and transition effects for UI elements
        /// Consolidated from: UIAnimations.cs
        /// </summary>
        public static class Animations
        {
            private const int ANIMATION_DURATION = 300; // milliseconds

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool AnimateWindow(IntPtr hwnd, uint dwTime, uint dwFlags);

            private const int AW_HOR_POSITIVE = 0x00000001;
            private const int AW_HOR_NEGATIVE = 0x00000002;
            private const int AW_VER_POSITIVE = 0x00000004;
            private const int AW_VER_NEGATIVE = 0x00000008;
            private const int AW_CENTER = 0x00000010;
            private const int AW_HIDE = 0x00010000;
            private const int AW_ACTIVATE = 0x00020000;
            private const int AW_SLIDE = 0x00040000;
            private const int AW_BLEND = 0x00080000;

            /// <summary>
            /// Fade in a control smoothly
            /// </summary>
            public static async System.Threading.Tasks.Task FadeInAsync(Control control, int duration = ANIMATION_DURATION)
            {
                if (control is null) return;
                if (control is Form form)
                {
                    double startOpacity = 0.0;
                    double endOpacity = 1.0;
                    double current = startOpacity;
                    form.Opacity = startOpacity;
                    form.Visible = true;
                    var steps = duration / 16;
                    var step = (endOpacity - startOpacity) / steps;
                    for (int i = 0; i <= steps; i++)
                    {
                        form.Opacity = current;
                        await System.Threading.Tasks.Task.Delay(16);
                        current = Math.Min(endOpacity, current + step);
                    }
                    form.Opacity = endOpacity;
                }
                else
                {
                    control.Visible = true;
                }
            }

            /// <summary>
            /// Fade out a control smoothly
            /// </summary>
            public static async System.Threading.Tasks.Task FadeOutAsync(Control control, int duration = ANIMATION_DURATION)
            {
                if (control is null) return;
                if (control is Form form)
                {
                    double startOpacity = form.Opacity;
                    double endOpacity = 0.0;
                    double current = startOpacity;
                    var steps = duration / 16;
                    var step = (endOpacity - startOpacity) / steps;
                    for (int i = 0; i <= steps; i++)
                    {
                        form.Opacity = current;
                        await System.Threading.Tasks.Task.Delay(16);
                        current = Math.Max(endOpacity, current + step);
                    }
                    form.Opacity = endOpacity;
                    form.Visible = false;
                }
                else
                {
                    control.Visible = false;
                }
            }

            /// <summary>
            /// Slide in a form from the left
            /// </summary>
            public static void SlideInFromLeft(Form form)
            {
                AnimateWindow(form.Handle, ANIMATION_DURATION, AW_SLIDE | AW_HOR_POSITIVE | AW_ACTIVATE);
            }

            /// <summary>
            /// Slide out a form to the right
            /// </summary>
            public static void SlideOutToRight(Form form)
            {
                AnimateWindow(form.Handle, ANIMATION_DURATION, AW_SLIDE | AW_HOR_NEGATIVE | AW_HIDE);
            }

            /// <summary>
            /// Center a form with animation
            /// </summary>
            public static void CenterShow(Form form)
            {
                AnimateWindow(form.Handle, ANIMATION_DURATION, AW_CENTER | AW_ACTIVATE);
            }
        }
        #endregion

        #region UI Animation System
        /// <summary>
        /// Provides high-performance fluid animations for UI elements
        /// </summary>
        public static class Animator
        {
            private static readonly List<AnimationInstance> ActiveAnimations = new();
            private static readonly System.Windows.Forms.Timer AnimationTimer;

            static Animator()
            {
                AnimationTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
                AnimationTimer.Tick += (s, e) => ProcessAnimations();
                AnimationTimer.Start();
            }

            public static void Animate(Control control, string property, float targetValue, int durationMs = 250, EasingFunction easing = EasingFunction.CubicOut)
            {
                if (control == null) return;
                
                var anim = new AnimationInstance
                {
                    Target = control,
                    PropertyName = property,
                    StartValue = GetPropertyValue(control, property),
                    TargetValue = targetValue,
                    StartTime = DateTime.Now,
                    Duration = durationMs,
                    Easing = easing
                };

                lock (ActiveAnimations)
                {
                    // Remove existing animations for the same control/property
                    ActiveAnimations.RemoveAll(a => a.Target == control && a.PropertyName == property);
                    ActiveAnimations.Add(anim);
                }
            }

            private static float GetPropertyValue(Control control, string property)
            {
                return property switch
                {
                    "Opacity" => (control is Form f) ? (float)f.Opacity : 1f,
                    "Left" => control.Left,
                    "Top" => control.Top,
                    "Width" => control.Width,
                    "Height" => control.Height,
                    "HoverProgress" => (control is ModernButton mb) ? mb.HoverProgress : 0f,
                    _ => 0f
                };
            }

            private static void SetPropertyValue(Control control, string property, float value)
            {
                switch (property)
                {
                    case "Opacity": if (control is Form f) f.Opacity = value; break;
                    case "Left": control.Left = (int)value; break;
                    case "Top": control.Top = (int)value; break;
                    case "Width": control.Width = (int)value; break;
                    case "Height": control.Height = (int)value; break;
                    case "HoverProgress": if (control is ModernButton mb) mb.HoverProgress = value; break;
                }
            }

            private static void ProcessAnimations()
            {
                lock (ActiveAnimations)
                {
                    var now = DateTime.Now;
                    for (int i = ActiveAnimations.Count - 1; i >= 0; i--)
                    {
                        var anim = ActiveAnimations[i];
                        float elapsed = (float)(now - anim.StartTime).TotalMilliseconds;
                        float progress = Math.Min(1f, elapsed / anim.Duration);
                        
                        float easedProgress = ApplyEasing(progress, anim.Easing);
                        float currentValue = anim.StartValue + (anim.TargetValue - anim.StartValue) * easedProgress;

                        if (anim.Target.IsDisposed)
                        {
                            ActiveAnimations.RemoveAt(i);
                            continue;
                        }

                        if (anim.Target.InvokeRequired)
                            anim.Target.BeginInvoke(new Action(() => SetPropertyValue(anim.Target, anim.PropertyName, currentValue)));
                        else
                            SetPropertyValue(anim.Target, anim.PropertyName, currentValue);

                        if (progress >= 1f)
                            ActiveAnimations.RemoveAt(i);
                    }
                }
            }

            private static float ApplyEasing(float t, EasingFunction easing)
            {
                return easing switch
                {
                    EasingFunction.Linear => t,
                    EasingFunction.CubicOut => 1f - (float)Math.Pow(1f - t, 3),
                    EasingFunction.QuickOut => 1f - (float)Math.Pow(1f - t, 4),
                    _ => t
                };
            }

            public enum EasingFunction { Linear, CubicOut, QuickOut }

            private class AnimationInstance
            {
                public Control Target = null!;
                public string PropertyName = "";
                public float StartValue;
                public float TargetValue;
                public DateTime StartTime;
                public int Duration;
                public EasingFunction Easing;
            }
        }
        #endregion
    }
}

namespace RecoveryCommander.UI
{
    /// <summary>
    /// Extension methods for UI controls
    /// Consolidated from: UIExtensions.cs
    /// </summary>
    public static class ControlExtensions
    {
        /// <summary>
        /// Set control visibility with animation
        /// </summary>
        public static void SetVisibleAnimated(this Control control, bool visible)
        {
            Theme.UIUtils.SetVisibleAnimated(control, visible);
        }

        /// <summary>
        /// Fade in control
        /// </summary>
        public static void FadeIn(this Control control, int duration = 300)
        {
            Theme.UIUtils.FadeIn(control, duration);
        }

        /// <summary>
        /// Fade out control
        /// </summary>
        public static void FadeOut(this Control control, int duration = 300)
        {
            Theme.UIUtils.FadeOut(control, duration);
        }

        /// <summary>
        /// Center form on screen
        /// </summary>
        public static void CenterOnScreen(this Form form)
        {
            Theme.UIUtils.CenterOnScreen(form);
        }

        /// <summary>
        /// Apply dark theme to control
        /// </summary>
        public static void ApplyDarkTheme(this Control control)
        {
            Theme.UIUtils.ApplyDarkTheme(control);
        }

        /// <summary>
        /// Get rounded rectangle path
        /// </summary>
        public static System.Drawing.Drawing2D.GraphicsPath GetRoundedRect(this System.Drawing.Rectangle rect, int radius)
        {
            return Theme.UIUtils.GetRoundedRect(rect, radius);
        }
    }
}
