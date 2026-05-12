using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace RecoveryCommander.UI
{
    public static partial class Theme
    {
        #region Custom Controls
        
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
                if (disposing)
                {
                    _cachedFont?.Dispose();
                    _animationTimer?.Dispose();
                }
                base.Dispose(disposing);
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public int Value
            {
                get => _value;
                set
                {
                    _value = Math.Max(_minimum, Math.Min(_maximum, value));
                    Invalidate();
                }
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
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

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
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

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
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

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
            public string StatusText
            {
                get => _statusText;
                set
                {
                    _statusText = value;
                    Invalidate();
                }
            }

            private Font? _cachedFont;

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                var clientRect = new Rectangle(0, 0, Width, Height);

                var surfaceColor = Theme.Surface;
                var primaryColor = Theme.Primary;
                var borderColor = Theme.Border;
                var textColor = Theme.Text;

                float radius = 10f;

                using (var path = GetRoundedRectPath(clientRect, radius))
                {
                    g.SetClip(path);

                    using (var bgBrush = new SolidBrush(surfaceColor))
                        g.FillPath(bgBrush, path);

                    if (_isIndeterminate)
                    {
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

                    using (var borderPath = GetRoundedRectPath(rect, radius))
                    using (var pen = new Pen(Color.FromArgb(50, borderColor), 1))
                        g.DrawPath(pen, borderPath);
                }

                if (!string.IsNullOrEmpty(_statusText))
                {
                    if (_cachedFont == null || !_cachedFont.FontFamily.Equals(Typography.DefaultFontFamily))
                    {
                        _cachedFont?.Dispose();
                        _cachedFont = new Font(Typography.DefaultFontFamily, 12f, FontStyle.Bold);
                    }

                    var font = _cachedFont;
                    var textSize = g.MeasureString(_statusText, font);
                    var textPos = new PointF(
                        (Width - textSize.Width) / 2,
                        (Height - textSize.Height) / 2);

                    using (var shadowBrush = new SolidBrush(Color.FromArgb(100, Color.Black)))
                    {
                        g.DrawString(_statusText, font, shadowBrush, new PointF(textPos.X + 1, textPos.Y + 1));
                    }
                    
                    using (var brush = new SolidBrush(Color.White))
                    {
                        g.DrawString(_statusText, font, brush, textPos);
                    }
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

            [DefaultValue(0)]
            public int Minimum
            {
                get => _minimum;
                set { _minimum = value; if (_value < _minimum) _value = _minimum; Invalidate(); }
            }

            [DefaultValue(100)]
            public int Maximum
            {
                get => _maximum;
                set { _maximum = value; if (_value > _maximum) _value = _maximum; Invalidate(); }
            }

            [DefaultValue(0)]
            public int Value
            {
                get => _value;
                set
                {
                    var newValue = Math.Max(_minimum, Math.Min(_maximum - _largeChange + 1, value));
                    if (newValue != _value)
                    {
                        _value = newValue;
                        Invalidate();
                        Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.ThumbPosition, _value));
                    }
                }
            }

            [DefaultValue(10)]
            public int LargeChange
            {
                get => _largeChange;
                set => _largeChange = Math.Max(1, value);
            }

            [DefaultValue(1)]
            public int SmallChange
            {
                get => _smallChange;
                set => _smallChange = Math.Max(1, value);
            }

            public CustomScrollBar()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
                Width = 17;
                BackColor = Colors.Surface;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var trackBrush = new SolidBrush(Color.FromArgb(45, 45, 55))) g.FillRectangle(trackBrush, ClientRectangle);
                CalculateThumbRect();
                var thumbColor = _thumbPressed ? Color.FromArgb(120, 120, 130) : _thumbHovered ? Color.FromArgb(100, 100, 110) : Color.FromArgb(80, 80, 90);
                using (var thumbBrush = new SolidBrush(thumbColor))
                {
                    var thumbRect = new Rectangle(_thumbRect.X + 3, _thumbRect.Y, _thumbRect.Width - 6, _thumbRect.Height);
                    if (thumbRect.Height > 0 && thumbRect.Width > 0)
                    {
                        using (var thumbPath = GetRoundedRectPath(thumbRect, 4)) g.FillPath(thumbBrush, thumbPath);
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
                    if (_thumbRect.Contains(e.Location)) { _isDragging = true; _thumbPressed = true; _lastMousePos = e.Location; Capture = true; Invalidate(); }
                    else Value = e.Y < _thumbRect.Y ? _value - _largeChange : _value + _largeChange;
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
                else { var wasHovered = _thumbHovered; _thumbHovered = _thumbRect.Contains(e.Location); if (wasHovered != _thumbHovered) Invalidate(); }
            }

            protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _isDragging = false; _thumbPressed = false; Capture = false; Invalidate(); }
            protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); Value -= (e.Delta / 120) * _smallChange; }
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
            
            public static void ApplyDarkThemeToRichTextBox(RichTextBox richTextBox)
            {
                if (richTextBox?.IsHandleCreated != true) return;
                try {
                    SetPreferredAppMode(2);
                    FlushMenuThemes();
                    var themeNames = new[] { "DarkMode_Explorer", "Explorer", "DarkMode_CFD", "DarkMode" };
                    foreach (var theme in themeNames) if (SetWindowTheme(richTextBox.Handle, theme, string.Empty) == 0) break;
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
                    foreach (var theme in themeNames) if (SetWindowTheme(textBox.Handle, theme, string.Empty) == 0) break;
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
                foreach (Control child in container.Controls) ApplyDarkThemeRecursive(child);
            }
        }
        #endregion

        public class RoundedRichTextBox : RichTextBox
        {
            private const int CornerRadius = 12;
            private readonly ContextMenuStrip _contextMenu;

            public RoundedRichTextBox()
            {
                this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, true);
                this.BackColor = Colors.Surface;
                this.ForeColor = Colors.Text;
                this.BorderStyle = BorderStyle.None;
                this.Font = new Font("Consolas", 9.5f);
                this.ReadOnly = true;
                _contextMenu = new ContextMenuStrip();
                var copyItem = new ToolStripMenuItem("Copy", null, (s, e) => { if (!string.IsNullOrEmpty(SelectedText)) Clipboard.SetText(SelectedText); });
                var copyAllItem = new ToolStripMenuItem("Copy All", null, (s, e) => { if (!string.IsNullOrEmpty(Text)) Clipboard.SetText(Text); });
                var clearItem = new ToolStripMenuItem("Clear", null, (s, e) => Clear());
                var selectAllItem = new ToolStripMenuItem("Select All", null, (s, e) => SelectAll());
                _contextMenu.Items.Add(copyItem);
                _contextMenu.Items.Add(selectAllItem);
                _contextMenu.Items.Add(new ToolStripSeparator());
                _contextMenu.Items.Add(copyAllItem);
                _contextMenu.Items.Add(new ToolStripSeparator());
                _contextMenu.Items.Add(clearItem);
                this.ContextMenuStrip = _contextMenu;
            }

            public void AppendText(string text, Color color)
            {
                if (this.InvokeRequired) { this.Invoke(new Action<string, Color>(AppendText), text, color); return; }
                int start = this.TextLength;
                base.AppendText(text);
                int end = this.TextLength;
                this.Select(start, end - start);
                this.SelectionColor = color;
                this.SelectionStart = this.TextLength;
                this.SelectionLength = 0;
                this.SelectionColor = this.ForeColor;
                this.ScrollToCaret();
            }

            public void BeginUpdate() { SendMessage(this.Handle, 0x000B, (IntPtr)0, IntPtr.Zero); }
            public void EndUpdate() { SendMessage(this.Handle, 0x000B, (IntPtr)1, IntPtr.Zero); this.Invalidate(); }

            [DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        }

        public class RoundedPanel : Panel
        {
            private const int CornerRadius = 12;
            public RoundedPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
                BackColor = Colors.Surface;
                BorderStyle = BorderStyle.None;
                Padding = new Padding(8);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                if (Width <= 0 || Height <= 0) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = GetRoundedRectPath(bounds, CornerRadius))
                {
                    using (var brush = new SolidBrush(Colors.Surface)) g.FillPath(brush, path);
                    using (var pen = new Pen(Colors.Border, 1.5f)) g.DrawPath(pen, path);
                }
            }
            protected override void OnPaintBackground(PaintEventArgs e) { }
        }

        public class DarkRichTextBox : RichTextBox
        {
            private CustomScrollBar? _customScrollBar;
            private const int CornerRadius = 12;
            public DarkRichTextBox()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
                BackColor = Colors.Surface;
                ForeColor = Colors.Text;
                BorderStyle = BorderStyle.None;
                ScrollBars = RichTextBoxScrollBars.None;
                _customScrollBar = new CustomScrollBar { Dock = DockStyle.Right, Width = 17 };
                _customScrollBar.Scroll += OnCustomScroll;
                Controls.Add(_customScrollBar);
                TextChanged += UpdateScrollBar;
                Resize += UpdateScrollBar;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = GetRoundedRectPath(bounds, CornerRadius))
                {
                    using (var brush = new SolidBrush(Colors.Surface)) g.FillPath(brush, path);
                    using (var pen = new Pen(Colors.Border, 1.5f)) g.DrawPath(pen, path);
                }
                var textBounds = new Rectangle(8, 8, Width - 16 - (_customScrollBar?.Visible == true ? 17 : 0), Height - 16);
                using (var clipPath = GetRoundedRectPath(textBounds, CornerRadius - 4)) g.SetClip(clipPath);
                base.OnPaint(e);
            }
            protected override void OnPaintBackground(PaintEventArgs e) { }
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
                else _customScrollBar.Visible = false;
            }
            [DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
            protected override void Dispose(bool disposing)
            {
                if (disposing && _customScrollBar != null)
                {
                    _customScrollBar.Scroll -= OnCustomScroll;
                    _customScrollBar.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
