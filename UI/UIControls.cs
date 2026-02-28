using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.UI
{
    #region Modern Controls (from ModernControls.cs)
    /// <summary>
    /// Modern button with Windows 11 Fluent Design styling
    /// </summary>
    public class ModernButton : Button
    {
        private bool _isHovered = false;
        private bool _isPressed = false;
        private Theme.ButtonStyle _buttonStyle = Theme.ButtonStyle.Standard;
        private int _cornerRadius = 4;
        private float _hoverProgress = 0f;
        private Color _glowColor = Color.Transparent;

        [Category("Appearance")]
        [Description("The glow color for background process indicators")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color GlowColor
        {
            get => _glowColor;
            set
            {
                _glowColor = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [Description("The visual style of the button")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Theme.ButtonStyle ButtonStyle
        {
            get => _buttonStyle;
            set
            {
                _buttonStyle = value;
                ApplyTheme();
                Invalidate();
            }
        }

        [Category("Appearance")]
        [Description("The corner radius of the button")]
        [DefaultValue(4)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set 
            { 
                _cornerRadius = Math.Max(0, value);
                Invalidate(); 
            }
        }

        public ModernButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            switch (_buttonStyle)
            {
                case Theme.ButtonStyle.Primary:
                    BackColor = Theme.Primary;
                    ForeColor = Theme.Text;
                    break;
                case Theme.ButtonStyle.Secondary:
                    BackColor = Theme.Surface;
                    ForeColor = Theme.Text;
                    break;
                case Theme.ButtonStyle.FuturisticPrimary:
                    BackColor = Theme.Colors.AIPrimary;
                    ForeColor = Theme.Colors.AIText;
                    break;
                case Theme.ButtonStyle.FuturisticGhost:
                    BackColor = Color.Transparent;
                    ForeColor = Theme.Colors.AIPrimary;
                    break;
                default:
                    BackColor = Theme.Surface;
                    ForeColor = Theme.Text;
                    break;
            }
            
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            // Region is no longer used to allow for anti-aliasing
            Region = null;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Manual painting to ensure high quality rendering
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var bounds = new Rectangle(0, 0, Width, Height);
            
            // 1. Clear with Parent background to handle corners and transparency
            // We walk up the parent chain to find a non-transparent color if needed
            Color parentColor = Parent?.BackColor ?? Theme.Background;
            if (parentColor.A < 255) parentColor = Theme.Background;
            
            using (var clearBrush = new SolidBrush(parentColor))
            {
                g.FillRectangle(clearBrush, bounds);
            }
            
            // 2. Determine background color for the button
            Color bgColor = BackColor;
            // Hover effect removed per user request: button stays same color until clicked
            // if (_isHovered && _buttonStyle != Theme.ButtonStyle.FuturisticGhost)
            // {
            //    bgColor = Theme.Colors.Lighten(bgColor, 10);
            // }

            if (_isPressed)
            {
                bgColor = Theme.Colors.Darken(bgColor, 10);
            }

            // 3. Draw Button Background
            // For Ghost buttons, we might only want a border or subtle fill
            if (_buttonStyle == Theme.ButtonStyle.FuturisticGhost)
            {
                if (_isHovered)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(20, Theme.Colors.AIPrimary)))
                    {
                         if (_cornerRadius > 0)
                        {
                            using var path = Theme.GetRoundedRectPath(bounds, _cornerRadius);
                            g.FillPath(brush, path);
                        }
                        else
                        {
                            g.FillRectangle(brush, bounds);
                        }
                    }
                }
            }
            else
            {
                // Solid fill for other styles
                using (var brush = new SolidBrush(bgColor))
                {
                    if (_cornerRadius > 0)
                    {
                        using var path = Theme.GetRoundedRectPath(bounds, _cornerRadius);
                        g.FillPath(brush, path);
                    }
                    else
                    {
                        g.FillRectangle(brush, bounds);
                    }
                }
            }

            // 4. Draw Border
            if (_buttonStyle == Theme.ButtonStyle.FuturisticGhost)
            {
                using (var pen = new Pen(Theme.Colors.AIPrimary, 1))
                {
                    // Adjust bounds for border to be inside
                    var borderBounds = new Rectangle(0, 0, Width - 1, Height - 1);
                    
                    if (_cornerRadius > 0)
                    {
                        using var path = Theme.GetRoundedRectPath(borderBounds, _cornerRadius);
                        g.DrawPath(pen, path);
                    }
                    else
                    {
                        g.DrawRectangle(pen, borderBounds);
                    }
                }
            }
            else
            {
                // Draw border for other styles (Primary/Secondary) using theme colors
                Color borderColor = _buttonStyle == Theme.ButtonStyle.Primary 
                    ? Theme.Primary 
                    : Theme.Border;

                // Use GlowColor if active
                if (_glowColor != Color.Transparent)
                {
                    borderColor = _glowColor;
                }

                using (var pen = new Pen(borderColor, _glowColor != Color.Transparent ? 2 : 1))
                {
                    var borderBounds = new Rectangle(0, 0, Width - 1, Height - 1);
                    
                    if (_cornerRadius > 0)
                    {
                        using var path = Theme.GetRoundedRectPath(borderBounds, _cornerRadius);
                        g.DrawPath(pen, path);
                        
                        // Add glow effect
                        if (_glowColor != Color.Transparent)
                        {
                            using (var glowPen = new Pen(Color.FromArgb(100, _glowColor), 3))
                            {
                                var glowBounds = new Rectangle(1, 1, Width - 3, Height - 3);
                                using var glowPath = Theme.GetRoundedRectPath(glowBounds, _cornerRadius);
                                g.DrawPath(glowPen, glowPath);
                            }
                        }
                    }
                    else
                    {
                        g.DrawRectangle(pen, borderBounds);
                    }
                }
            }

            // 5. Draw Text
            if (!string.IsNullOrEmpty(Text))
            {
                var lines = Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                using (var textBrush = new SolidBrush(ForeColor))
                using (var format = new StringFormat())
                {
                    if (lines.Length > 1)
                    {
                        // Multi-line text (e.g., module name + version)
                        var padding = 15;
                        var lineHeight = (int)(Font.GetHeight(g) * 1.2f);
                        var totalHeight = lines.Length * lineHeight;
                        var startY = (bounds.Height - totalHeight) / 2;
                        
                        format.LineAlignment = StringAlignment.Center;
                        format.Trimming = StringTrimming.EllipsisCharacter;
                        format.FormatFlags = StringFormatFlags.NoWrap;
                        
                        // Set horizontal alignment based on TextAlign
                        if (TextAlign == ContentAlignment.MiddleCenter || TextAlign == ContentAlignment.TopCenter || TextAlign == ContentAlignment.BottomCenter)
                        {
                            format.Alignment = StringAlignment.Center;
                        }
                        else if (TextAlign == ContentAlignment.MiddleRight || TextAlign == ContentAlignment.TopRight || TextAlign == ContentAlignment.BottomRight)
                        {
                            format.Alignment = StringAlignment.Far;
                        }
                        else
                        {
                            format.Alignment = StringAlignment.Near;
                        }
                        
                        for (int i = 0; i < lines.Length; i++)
                        {
                            var line = lines[i].Trim();
                            var y = startY + (i * lineHeight);
                            var textRect = new RectangleF(bounds.X + padding, y, bounds.Width - (padding * 2), lineHeight);
                            g.DrawString(line, Font, textBrush, textRect, format);
                        }
                    }
                    else
                    {
                        // Single line text
                        format.Alignment = TextAlign == ContentAlignment.MiddleCenter || TextAlign == ContentAlignment.TopCenter || TextAlign == ContentAlignment.BottomCenter ? StringAlignment.Center :
                                          TextAlign == ContentAlignment.MiddleRight || TextAlign == ContentAlignment.TopRight || TextAlign == ContentAlignment.BottomRight ? StringAlignment.Far :
                                          StringAlignment.Near;
                        format.LineAlignment = StringAlignment.Center;
                        format.Trimming = StringTrimming.EllipsisCharacter;
                        
                        var textRect = new RectangleF(bounds.X + 10, bounds.Y, bounds.Width - 20, bounds.Height);
                        g.DrawString(Text, Font, textBrush, textRect, format);
                    }
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Theme.Animator.Animate(this, "HoverProgress", 1f, 200);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Theme.Animator.Animate(this, "HoverProgress", 0f, 250);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            _isPressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            _isPressed = false;
            Invalidate();
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float HoverProgress
        {
            get => _hoverProgress;
            set
            {
                _hoverProgress = value;
                Invalidate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Modern card panel with elevated shadow effect
    /// </summary>
    public class ModernCard : Panel
    {
        private int _cornerRadius = 8;
        private int _elevation = 2;

        [Category("Appearance")]
        [Description("The corner radius of the card")]
        [DefaultValue(8)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set
            {
                _cornerRadius = Math.Max(0, value);
                UpdateRegion();
                Invalidate();
            }
        }

        [Category("Appearance")]
        [Description("The elevation level of the card")]
        [DefaultValue(2)]
        public int Elevation
        {
            get => _elevation;
            set
            {
                _elevation = Math.Max(0, value);
                Invalidate();
            }
        }

        public ModernCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            
            BackColor = Theme.Surface;
            Padding = new Padding(16);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (_cornerRadius > 0)
            {
                using var path = Theme.GetRoundedRectPath(ClientRectangle, _cornerRadius);
                Region = new Region(path);
            }
            else
            {
                Region = null;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw shadow
            var shadowBounds = new Rectangle(2, 2, Width - 4, Height - 4);
            // Theme.DesignSystem.DrawElevatedShadow(g, shadowBounds, _elevation);
            
            // Simple shadow fallback
            using var shadowBrush = new SolidBrush(Color.FromArgb(50, Color.Black));
            g.FillRectangle(shadowBrush, shadowBounds);

            // Draw card background
            var bounds = new Rectangle(0, 0, Width, Height);
            using (var brush = new SolidBrush(BackColor))
            {
                if (_cornerRadius > 0)
                {
                    using var path = Theme.GetRoundedRectPath(bounds, _cornerRadius);
                    g.FillPath(brush, path);
                }
                else
                {
                    g.FillRectangle(brush, bounds);
                }
            }

            // Draw border
            using (var pen = new Pen(Theme.Border, 1))
            {
                if (_cornerRadius > 0)
                {
                    using var path = Theme.GetRoundedRectPath(bounds, _cornerRadius);
                    g.DrawPath(pen, path);
                }
                else
                {
                    g.DrawRectangle(pen, bounds);
                }
            }
        }
    }

    /// <summary>
    /// Modern text box with rounded corners and focus effects
    /// </summary>
    public class ModernTextBox : TextBox
    {
        private int _cornerRadius = 4;
        private bool _isFocused = false;

        [Category("Appearance")]
        [Description("The corner radius of the text box")]
        [DefaultValue(4)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set
            {
                _cornerRadius = Math.Max(0, value);
                UpdateRegion();
                Invalidate();
            }
        }

        public ModernTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            
            BackColor = Theme.Surface;
            ForeColor = Theme.Text;
            BorderStyle = BorderStyle.None;
            Padding = new Padding(8, 6, 8, 6);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (_cornerRadius > 0)
            {
                using var path = Theme.GetRoundedRectPath(ClientRectangle, _cornerRadius);
                Region = new Region(path);
            }
            else
            {
                Region = null;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, Width, Height);

            // Draw background
            using (var brush = new SolidBrush(BackColor))
            {
                if (_cornerRadius > 0)
                {
                    using var path = Theme.GetRoundedRectPath(bounds, _cornerRadius);
                    g.FillPath(brush, path);
                }
                else
                {
                    g.FillRectangle(brush, bounds);
                }
            }

            // Draw border
            Color borderColor = _isFocused ? Theme.Primary : Theme.Border;
            using (var pen = new Pen(borderColor, 1))
            {
                if (_cornerRadius > 0)
                {
                    using var path = Theme.GetRoundedRectPath(bounds, _cornerRadius);
                    g.DrawPath(pen, path);
                }
                else
                {
                    g.DrawRectangle(pen, bounds);
                }
            }

            // Draw text
            var textBounds = new Rectangle(Padding.Left, Padding.Top, 
                Width - Padding.Horizontal, Height - Padding.Vertical);
            TextRenderer.DrawText(g, Text, Font, textBounds, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            _isFocused = true;
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            _isFocused = false;
            Invalidate();
        }
    }
    #endregion

    #region Enhanced Progress System (from EnhancedProgressSystem.cs)
    /// <summary>
    /// Enhanced progress system with notifications and operation management
    /// </summary>
    public class EnhancedProgressSystem : IDisposable
    {
        private readonly Form parentForm;
        private readonly List<ProgressOperation> activeOperations = new();
        private readonly Queue<ToastNotification> notificationQueue = new();
        private readonly System.Windows.Forms.Timer notificationTimer;
        private ToastNotificationForm? currentToast = null;
        public bool NotificationsEnabled { get; set; } = false;
        
        public EnhancedProgressSystem(Form parent)
        {
            parentForm = parent;
            notificationTimer = new System.Windows.Forms.Timer { Interval = 100 };
            notificationTimer.Tick += ProcessNotificationQueue;
            notificationTimer.Start();
        }
        
        public ProgressOperation CreateOperation(string name, string description = "")
        {
            var operation = new ProgressOperation(name, description, this);
            activeOperations.Add(operation);
            return operation;
        }
        
        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info, int duration = 5000)
        {
            if (!NotificationsEnabled) return;
            var notification = new ToastNotification
            {
                Title = title,
                Message = message,
                Type = type,
                Duration = duration,
                Timestamp = DateTime.Now
            };
            
            notificationQueue.Enqueue(notification);
        }

        private void ProcessNotificationQueue(object? sender, EventArgs e)
        {
            if (currentToast != null && !currentToast.IsDisposed) return;
            
            if (notificationQueue.Count > 0)
            {
                var notification = notificationQueue.Dequeue();
                currentToast = new ToastNotificationForm(notification);
                currentToast.Show();
            }
        }

        internal void RemoveOperation(ProgressOperation operation)
        {
            activeOperations.Remove(operation);
        }

        public void Dispose()
        {
            notificationTimer?.Dispose();
            currentToast?.Dispose();
        }
    }

    /// <summary>
    /// Progress operation with detailed tracking
    /// </summary>
    public class ProgressOperation : IDisposable
    {
        private readonly EnhancedProgressSystem system;
        private bool _isCompleted = false;
        private int _progress = 0;
        private string _status = "";

        public string Name { get; }
        public string Description { get; }
        public DateTime StartTime { get; } = DateTime.Now;
        
        public int Progress
        {
            get => _progress;
            set
            {
                _progress = Math.Max(0, Math.Min(100, value));
                ProgressChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            private set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    if (value)
                    {
                        Completed?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        public event EventHandler? ProgressChanged;
        public event EventHandler? StatusChanged;
        public event EventHandler? Completed;

        public ProgressOperation(string name, string description, EnhancedProgressSystem system)
        {
            Name = name;
            Description = description;
            this.system = system;
        }

        public void UpdateProgress(int progress, string status = "")
        {
            Progress = progress;
            if (!string.IsNullOrEmpty(status))
                Status = status;
        }

        public void Complete()
        {
            Progress = 100;
            Status = "Completed";
            IsCompleted = true;
        }

        public void Dispose()
        {
            if (!IsCompleted)
            {
                Complete();
            }
            system.RemoveOperation(this);
        }
    }

    /// <summary>
    /// Toast notification data
    /// </summary>
    public class ToastNotification
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public NotificationType Type { get; set; } = NotificationType.Info;
        public int Duration { get; set; } = 5000;
        public DateTime Timestamp { get; set; }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Toast notification form
    /// </summary>
    public class ToastNotificationForm : Form
    {
        private readonly System.Windows.Forms.Timer closeTimer;
        
        public ToastNotificationForm(ToastNotification notification)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            Size = new Size(300, 80);
            
            // Position in top-right corner
            var screenBounds = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            Location = new Point(screenBounds.Right - 320, screenBounds.Top + 20);
            
            ApplyTheme(notification.Type);
            
            var titleLabel = new Label
            {
                Text = notification.Title,
                Font = Theme.Typography.BodyStrong,
                ForeColor = Theme.Text,
                Location = new Point(16, 12),
                Size = new Size(268, 20),
                AutoSize = false
            };
            
            var messageLabel = new Label
            {
                Text = notification.Message,
                Font = Theme.Typography.Body,
                ForeColor = Theme.SubtleText,
                Location = new Point(16, 32),
                Size = new Size(268, 36),
                AutoSize = false
            };
            
            Controls.AddRange(new Control[] { titleLabel, messageLabel });
            
            closeTimer = new System.Windows.Forms.Timer { Interval = notification.Duration };
            closeTimer.Tick += (s, e) => Close();
            closeTimer.Start();
            
            // Add click to close
            Click += (s, e) => Close();
            titleLabel.Click += (s, e) => Close();
            messageLabel.Click += (s, e) => Close();
        }

        private void ApplyTheme(NotificationType type)
        {
            Color bgColor = Theme.Surface;
            Color borderColor = Theme.Border;
            
            switch (type)
            {
                case NotificationType.Success:
                    borderColor = Theme.Success;
                    break;
                case NotificationType.Warning:
                    borderColor = Theme.Warning;
                    break;
                case NotificationType.Error:
                    borderColor = Theme.Error;
                    break;
            }
            
            BackColor = bgColor;
            
            Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                using var pen = new Pen(borderColor, 2);
                using var path = Theme.GetRoundedRectPath(ClientRectangle, 8);
                g.DrawPath(pen, path);
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                closeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
    #endregion

    #region Enhanced Progress Dialog (Consolidated from EnhancedProgressDialog.cs)
    /// <summary>
    /// Enhanced progress dialog with modern UI
    /// </summary>
    public partial class EnhancedProgressDialog : Form
    {
        private readonly Theme.RoundedProgressBar _progressBar;
        private readonly Label _statusLabel;
        private readonly Button _cancelButton;

        public EnhancedProgressDialog()
        {
            _progressBar = new Theme.RoundedProgressBar();
            _statusLabel = new Label();
            _cancelButton = new Button();
            
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Progress";
            this.Size = new Size(400, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Apply theme
            Theme.ApplyFormStyle(this);

            // Progress bar
            _progressBar.Location = new Point(20, 20);
            _progressBar.Size = new Size(340, 30);
            _progressBar.Value = 0;
            _progressBar.Maximum = 100;

            // Status label
            _statusLabel.Location = new Point(20, 60);
            _statusLabel.Size = new Size(340, 20);
            _statusLabel.Text = "Initializing...";
            _statusLabel.ForeColor = Theme.Text;

            // Cancel button
            _cancelButton.Location = new Point(160, 90);
            _cancelButton.Size = new Size(80, 25);
            _cancelButton.Text = "Cancel";
            _cancelButton.UseVisualStyleBackColor = false;
            Theme.ApplyButtonStyle(_cancelButton, Theme.ButtonStyle.Primary);
            _cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            // Add controls
            this.Controls.AddRange(new Control[] { _progressBar, _statusLabel, _cancelButton });

            this.ResumeLayout(false);
        }

        /// <summary>
        /// Show error with retry dialog
        /// </summary>
        public static bool ShowErrorWithRetry(string message, string title = "Error")
        {
            return MessageBox.Show(message, title, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry;
        }
        
        public static bool ShowErrorWithRetry(string context, string title, Exception ex)
        {
            var message = $"{context}: {ex.Message}";
            return MessageBox.Show(message, title, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry;
        }

        /// <summary>
        /// Update progress
        /// </summary>
        public void UpdateProgress(int value, string status)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int, string>(UpdateProgress), value, status);
                return;
            }

            _progressBar.Value = value;
            _statusLabel.Text = status;
        }

        /// <summary>
        /// Show progress dialog for async operation
        /// </summary>
        public static async Task<bool> ShowAsync(IProgress<ProgressReport> progress, string title = "Progress")
        {
            using var dialog = new EnhancedProgressDialog();
            dialog.Text = title;

            // Create a simple progress handler
            var progressHandler = new Progress<ProgressReport>(report =>
            {
                dialog.UpdateProgress(report.PercentComplete, report.StatusMessage);
            });

            // Subscribe to progress updates
            if (progress is IProgress<ProgressReport> progressReporter)
            {
                // Simulate progress updates - in real implementation this would be wired up differently
                await Task.Run(() => dialog.ShowDialog());
            }

            return dialog.DialogResult == DialogResult.OK;
        }
    }
    #endregion
}
