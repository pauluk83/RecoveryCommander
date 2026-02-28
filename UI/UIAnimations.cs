using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RecoveryCommander.UI
{
    /// <summary>
    /// Animation and transition effects for UI elements
    /// </summary>
    public static class UIAnimations
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
        public static async Task FadeInAsync(Control control, int duration = ANIMATION_DURATION)
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
                    await Task.Delay(16);
                    current = Math.Min(endOpacity, current + step);
                }
                form.Opacity = endOpacity;
            }
        }
        public static async Task FadeOutAsync(Control control, int duration = ANIMATION_DURATION, Action? onComplete = null)
        {
            if (control is null) return;
            if (control is Form form)
            {
                double startOpacity = form.Opacity;
                double endOpacity = 0.0;
                double current = startOpacity;
                var steps = duration / 16;
                var step = (startOpacity - endOpacity) / steps;
                for (int i = 0; i <= steps; i++)
                {
                    form.Opacity = current;
                    await Task.Delay(16);
                    current = Math.Max(endOpacity, current - step);
                }
                form.Opacity = endOpacity;
                form.Visible = false;
                onComplete?.Invoke();
            }
        }
        public enum SlideDirection { Left, Right, Up, Down }
        public static async Task SlideInAsync(Control control, SlideDirection direction, int duration = ANIMATION_DURATION)
        {
            if (control is null) return;
            var originalLocation = control.Location;
            var startLocation = originalLocation;
            switch (direction)
            {
                case SlideDirection.Left:
                    startLocation.X = -control.Width;
                    break;
                case SlideDirection.Right:
                    startLocation.X = (control.Parent?.Width ?? control.Width);
                    break;
                case SlideDirection.Up:
                    startLocation.Y = -control.Height;
                    break;
                case SlideDirection.Down:
                    startLocation.Y = (control.Parent?.Height ?? control.Height);
                    break;
            }
            control.Location = startLocation;
            control.Visible = true;
            var steps = duration / 16;
            for (int i = 0; i <= steps; i++)
            {
                float progress = i / (float)steps;
                double easeProgress = 1 - Math.Pow(1 - progress, 3); // Ease out cubic
                int nX = (int)(startLocation.X + (originalLocation.X - startLocation.X) * easeProgress);
                int nY = (int)(startLocation.Y + (originalLocation.Y - startLocation.Y) * easeProgress);
                control.Location = new System.Drawing.Point(nX, nY);
                await Task.Delay(16);
            }
            control.Location = originalLocation;
        }

        /// <summary>
        /// Slide a control from right to left
        /// </summary>
        public static async Task SlideInFromRightAsync(Control control, int duration = ANIMATION_DURATION)
        {
            if (control == null || control.IsDisposed) return;

            var originalLocation = control.Location;
            var parentWidth = control.Parent?.Width ?? 0;
            
            control.Location = new Point(parentWidth, originalLocation.Y);
            control.Visible = true;

            var steps = duration / 10;
            var stepDelay = duration / steps;
            var stepIncrement = (parentWidth - originalLocation.X) / steps;

            for (int i = 0; i <= steps; i++)
            {
                if (control.IsDisposed) return;
                
                var newX = Math.Max(originalLocation.X, parentWidth - (i * stepIncrement));
                control.Location = new Point(newX, originalLocation.Y);
                await Task.Delay(stepDelay);
            }

            control.Location = originalLocation;
        }

        /// <summary>
        /// Animate a form with Windows API
        /// </summary>
        public static void AnimateForm(Form form, AnimationType animationType, bool show = true)
        {
            if (form == null) return;

            int flags = 0;
            
            switch (animationType)
            {
                case AnimationType.SlideFromLeft:
                    flags = AW_HOR_POSITIVE | AW_SLIDE;
                    break;
                case AnimationType.SlideFromRight:
                    flags = AW_HOR_NEGATIVE | AW_SLIDE;
                    break;
                case AnimationType.SlideFromTop:
                    flags = AW_VER_POSITIVE | AW_SLIDE;
                    break;
                case AnimationType.SlideFromBottom:
                    flags = AW_VER_NEGATIVE | AW_SLIDE;
                    break;
                case AnimationType.Center:
                    flags = AW_CENTER;
                    break;
                case AnimationType.Blend:
                    flags = AW_BLEND;
                    break;
            }

            if (!show) flags |= AW_HIDE;
            else flags |= AW_ACTIVATE;

            try
            {
                AnimateWindow(form.Handle, ANIMATION_DURATION, (uint)flags);
            }
            catch
            {
                // Fallback to simple show/hide if animation fails
                if (show) form.Show();
                else form.Hide();
            }
        }

        /// <summary>
        /// Smooth color transition for a control
        /// </summary>
        public static async Task TransitionColorAsync(Control control, Color fromColor, Color toColor, int duration = ANIMATION_DURATION)
        {
            if (control == null || control.IsDisposed) return;

            var steps = duration / 10;
            var stepDelay = duration / steps;

            for (int i = 0; i <= steps; i++)
            {
                if (control.IsDisposed) return;

                var ratio = (double)i / steps;
                var r = (int)(fromColor.R + (toColor.R - fromColor.R) * ratio);
                var g = (int)(fromColor.G + (toColor.G - fromColor.G) * ratio);
                var b = (int)(fromColor.B + (toColor.B - fromColor.B) * ratio);

                control.BackColor = Color.FromArgb(r, g, b);
                await Task.Delay(stepDelay);
            }
        }

        /// <summary>
        /// Pulse effect for a control (highlight briefly)
        /// </summary>
        public static async Task PulseAsync(Control control, Color pulseColor, int duration = ANIMATION_DURATION)
        {
            if (control == null || control.IsDisposed) return;

            var originalColor = control.BackColor;
            
            await TransitionColorAsync(control, originalColor, pulseColor, duration / 2);
            await TransitionColorAsync(control, pulseColor, originalColor, duration / 2);
        }

        /// <summary>
        /// Smooth size change animation
        /// </summary>
        public static async Task ResizeAsync(Control control, Size targetSize, int duration = ANIMATION_DURATION)
        {
            if (control == null || control.IsDisposed) return;

            var originalSize = control.Size;
            var steps = duration / 10;
            var stepDelay = duration / steps;
            var widthStep = (targetSize.Width - originalSize.Width) / steps;
            var heightStep = (targetSize.Height - originalSize.Height) / steps;

            for (int i = 0; i <= steps; i++)
            {
                if (control.IsDisposed) return;

                var newWidth = originalSize.Width + (i * widthStep);
                var newHeight = originalSize.Height + (i * heightStep);
                control.Size = new Size(newWidth, newHeight);
                await Task.Delay(stepDelay);
            }
        }
    }

    /// <summary>
    /// Animation types for form animations
    /// </summary>
    public enum AnimationType
    {
        SlideFromLeft,
        SlideFromRight,
        SlideFromTop,
        SlideFromBottom,
        Center,
        Blend
    }

    /// <summary>
    /// Enhanced progress indicator with detailed information and cancellation
    /// </summary>
    public class DetailedProgressIndicator : UserControl
    {
        private ProgressBar? _progressBar;
        private Label? _titleLabel;
        private Label? _messageLabel;
        private Label? _detailsLabel;
        private Button? _cancelButton;
        private Panel? _progressPanel;
        private System.Windows.Forms.Timer? _animationTimer;

        public event EventHandler? CancelRequested;

        public DetailedProgressIndicator()
        {
            InitializeComponents();
            InitializeTimer();
        }

        private void InitializeComponents()
        {
            this.Size = new Size(400, 120);
            this.BackColor = Theme.Colors.Surface;

            _progressPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.Transparent
            };

            _titleLabel = new Label
            {
                Font = Theme.Typography.Subtitle,
                ForeColor = Theme.Colors.Text,
                Location = new Point(0, 0),
                Size = new Size(360, 20),
                Text = "Processing..."
            };

            _messageLabel = new Label
            {
                Font = Theme.Typography.Body,
                ForeColor = Theme.Colors.SubtleText,
                Location = new Point(0, 25),
                Size = new Size(280, 20),
                Text = "Please wait..."
            };

            _detailsLabel = new Label
            {
                Font = Theme.Typography.Caption,
                ForeColor = Theme.Colors.SubtleText,
                Location = new Point(0, 50),
                Size = new Size(280, 15),
                Text = ""
            };

            _progressBar = new ProgressBar
            {
                Location = new Point(0, 70),
                Size = new Size(280, 20),
                Style = ProgressBarStyle.Continuous
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(290, 70),
                Size = new Size(70, 20),
                UseVisualStyleBackColor = true
            };
            _cancelButton!.Click += (s, e) => CancelRequested?.Invoke(this, EventArgs.Empty);

            Theme.ApplyProgressBarStyle(_progressBar!);
            Theme.ApplyButtonStyle(_cancelButton!, Theme.ButtonStyle.Secondary);

_progressPanel!.Controls.Add(_titleLabel!);
_progressPanel!.Controls.Add(_messageLabel!);
_progressPanel!.Controls.Add(_detailsLabel!);
_progressPanel!.Controls.Add(_progressBar!);
_progressPanel!.Controls.Add(_cancelButton!);
            this.Controls.Add(_progressPanel!);
        }

        private void InitializeTimer()
        {
            _animationTimer = new System.Windows.Forms.Timer
            {
                Interval = 100
            };
            _animationTimer!.Tick += AnimationTimer_Tick;
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            // Add subtle animation effect
            if (_progressBar != null && _progressBar.Value < _progressBar.Maximum)
            {
                // Pulsing effect for indeterminate progress
                _progressBar.Value = (_progressBar.Value + 1) % 100;
            }
        }

        public void UpdateProgress(int percent, string title, string message, string details = "")
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, string, string, string>(UpdateProgress), percent, title, message, details);
                return;
            }

            _titleLabel!.Text = title;
            _messageLabel!.Text = message;
            _detailsLabel!.Text = details;
            _progressBar!.Value = Math.Min(100, Math.Max(0, percent));
        }

        public void SetIndeterminate(bool indeterminate)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(SetIndeterminate), indeterminate);
                return;
            }

            if (indeterminate)
            {
                _progressBar!.Style = ProgressBarStyle.Marquee;
                _animationTimer!.Start();
            }
            else
            {
                _progressBar!.Style = ProgressBarStyle.Continuous;
                _animationTimer!.Stop();
            }
        }

        public void ShowCancel(bool show)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(ShowCancel), show);
                return;
            }

            _cancelButton!.Visible = show;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
