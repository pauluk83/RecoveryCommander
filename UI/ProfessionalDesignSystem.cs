using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RecoveryCommander.UI
{
    /// <summary>
    /// Professional design system with consistent spacing, typography, and visual effects
    /// </summary>
    public static class ProfessionalDesignSystem
    {
        #region Spacing System (8px base unit)
        public static class Spacing
        {
            public const int XS = 4;   // 0.5x
            public const int SM = 8;   // 1x
            public const int MD = 16;  // 2x
            public const int LG = 24;  // 3x
            public const int XL = 32;  // 4x
            public const int XXL = 48; // 6x
            
            public static Padding Standard => new Padding(MD);
            public static Padding Compact => new Padding(SM);
            public static Padding Comfortable => new Padding(LG);
            public static Padding Spacious => new Padding(XL);
        }
        #endregion

        #region Professional Shadows
        public static void DrawElevatedShadow(Graphics g, Rectangle bounds, int elevation = 1)
        {
            if (g == null || bounds.Width <= 0 || bounds.Height <= 0) return;
            
            var shadowColor = Color.FromArgb(Math.Min(40 * elevation, 120), 0, 0, 0);
            var blurRadius = elevation * 4;
            var offsetY = elevation * 2;
            
            // Draw multiple shadow layers for soft effect
            for (int i = 0; i < blurRadius; i++)
            {
                var alpha = shadowColor.A / (blurRadius + 1);
                var layerColor = Color.FromArgb(alpha, shadowColor);
                var layerBounds = new Rectangle(
                    bounds.X + i / 2,
                    bounds.Y + offsetY + i / 2,
                    bounds.Width - i,
                    bounds.Height - i
                );
                
                using (var brush = new SolidBrush(layerColor))
                {
                    g.FillRectangle(brush, layerBounds);
                }
            }
        }
        
        public static void DrawSubtleShadow(Graphics g, Rectangle bounds)
        {
            DrawElevatedShadow(g, bounds, 1);
        }
        
        public static void DrawCardShadow(Graphics g, Rectangle bounds)
        {
            DrawElevatedShadow(g, bounds, 2);
        }
        #endregion

        #region Professional Dividers
        public static void DrawDivider(Graphics g, Rectangle bounds, bool isVertical = false)
        {
            if (g == null) return;
            
            using (var pen = new Pen(Color.FromArgb(30, Theme.Colors.Border), 1))
            {
                if (isVertical)
                {
                    g.DrawLine(pen, bounds.X, bounds.Y, bounds.X, bounds.Height);
                }
                else
                {
                    g.DrawLine(pen, bounds.X, bounds.Y, bounds.Width, bounds.Y);
                }
            }
        }
        
        public static void DrawSubtleDivider(Graphics g, Rectangle bounds, bool isVertical = false)
        {
            if (g == null) return;
            
            // Draw gradient divider for subtle effect
            if (isVertical)
            {
                using (var brush = new LinearGradientBrush(
                    new Rectangle(bounds.X, bounds.Y, 1, bounds.Height),
                    Color.Transparent,
                    Color.FromArgb(20, Theme.Colors.Border),
                    LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(brush, bounds.X, bounds.Y, 1, bounds.Height);
                }
            }
            else
            {
                using (var brush = new LinearGradientBrush(
                    new Rectangle(bounds.X, bounds.Y, bounds.Width, 1),
                    Color.Transparent,
                    Color.FromArgb(20, Theme.Colors.Border),
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brush, bounds.X, bounds.Y, bounds.Width, 1);
                }
            }
        }
        #endregion

        #region Professional Empty States
        public static Panel CreateEmptyState(string title, string description, string? icon = null)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = Spacing.Spacious
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
            
            // Icon (optional)
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
                    ForeColor = Color.FromArgb(100, Theme.Colors.Text),
                    BackColor = Color.Transparent
                };
                container.Controls.Add(iconLabel, 0, 0);
            }
            
            // Title
            var titleLabel = new Label
            {
                Text = title,
                Font = Theme.Typography.Header,
                ForeColor = Theme.Colors.Text,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 28,
                Padding = new Padding(0, Spacing.MD, 0, Spacing.SM),
                BackColor = Color.Transparent
            };
            container.Controls.Add(titleLabel, 0, icon != null ? 1 : 0);
            
            // Description
            var descLabel = new Label
            {
                Text = description,
                Font = Theme.Typography.Body,
                ForeColor = Color.FromArgb(180, Theme.Colors.Text),
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
        #endregion

        #region Professional Button Enhancements
        public static void ApplyProfessionalButtonStyle(Button button, bool isPrimary = false)
        {
            if (button == null) return;
            
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Padding = new Padding(Spacing.LG, Spacing.SM, Spacing.LG, Spacing.SM);
            button.Height = 40;
            button.Font = Theme.Typography.BodyStrong;
            button.Cursor = Cursors.Hand;
            
            if (isPrimary)
            {
                button.BackColor = Theme.Colors.Primary;
                button.ForeColor = Color.White;
            }
            else
            {
                button.BackColor = Theme.Colors.Surface;
                button.ForeColor = Theme.Colors.Text;
            }
            
            // Add hover effect
            button.MouseEnter += (s, e) =>
            {
                if (isPrimary)
                {
                    button.BackColor = Theme.Colors.Lighten(Theme.Colors.Primary, 15);
                }
                else
                {
                    button.BackColor = Theme.Colors.Lighten(Theme.Colors.Surface, 10);
                }
            };
            
            button.MouseLeave += (s, e) =>
            {
                if (isPrimary)
                {
                    button.BackColor = Theme.Colors.Primary;
                }
                else
                {
                    button.BackColor = Theme.Colors.Surface;
                }
            };
        }
        #endregion

        #region Professional Status Strip
        public static void ApplyProfessionalStatusStripStyle(StatusStrip statusStrip)
        {
            if (statusStrip == null) return;
            
            statusStrip.Height = 28;
            statusStrip.Padding = new Padding(Spacing.MD, 0, Spacing.MD, 0);
            statusStrip.BackColor = Theme.Colors.Surface;
            statusStrip.ForeColor = Theme.Colors.Text;
            statusStrip.Font = Theme.Typography.Caption;
            
            // Add subtle top border
            statusStrip.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using (var pen = new Pen(Color.FromArgb(30, Theme.Colors.Border), 1))
                {
                    g.DrawLine(pen, 0, 0, statusStrip.Width, 0);
                }
            };
        }
        #endregion

        #region Professional Card Style
        public static void DrawProfessionalCard(Graphics g, Rectangle bounds, int cornerRadius = 12)
        {
            if (g == null || bounds.Width <= 0 || bounds.Height <= 0) return;
            
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Draw shadow first
            var shadowBounds = new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width, bounds.Height);
            DrawSubtleShadow(g, shadowBounds);
            
            // Draw card background
            using (var path = Theme.GetRoundedRectPath(bounds, cornerRadius))
            {
                using (var brush = new SolidBrush(Theme.Colors.Surface))
                {
                    g.FillPath(brush, path);
                }
                
                // Draw subtle border
                using (var pen = new Pen(Color.FromArgb(40, Theme.Colors.Border), 1))
                {
                    g.DrawPath(pen, path);
                }
            }
        }
        #endregion

        #region Professional Loading Indicator
        public static Panel CreateLoadingIndicator(string message = "Loading...")
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            
            var container = new Panel
            {
                AutoSize = true,
                BackColor = Color.Transparent
            };
            
            var spinner = new Panel
            {
                Width = 40,
                Height = 40,
                BackColor = Color.Transparent
            };
            
            var angle = 0f;
            var timer = new System.Windows.Forms.Timer { Interval = 16 };
            
            spinner.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                var center = new PointF(spinner.Width / 2f, spinner.Height / 2f);
                var radius = 12f;
                
                for (int i = 0; i < 8; i++)
                {
                    var dotAngle = (angle + i * 45) * Math.PI / 180;
                    var x = center.X + (float)(radius * Math.Cos(dotAngle));
                    var y = center.Y + (float)(radius * Math.Sin(dotAngle));
                    
                    var alpha = 255 - (i * 30);
                    alpha = Math.Max(50, Math.Min(255, alpha));
                    
                    using (var brush = new SolidBrush(Color.FromArgb(alpha, Theme.Colors.Primary)))
                    {
                        g.FillEllipse(brush, x - 3, y - 3, 6, 6);
                    }
                }
            };
            
            timer.Tick += (s, e) =>
            {
                angle = (angle + 5) % 360;
                spinner.Invalidate();
            };
            timer.Start();
            
            var label = new Label
            {
                Text = message,
                Font = Theme.Typography.Body,
                ForeColor = Color.FromArgb(180, Theme.Colors.Text),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, Spacing.MD, 0, 0)
            };
            
            container.Controls.Add(spinner);
            container.Controls.Add(label);
            
            // Center the container
            container.Location = new Point(
                (panel.Width - container.Width) / 2,
                (panel.Height - container.Height) / 2
            );
            
            panel.Resize += (s, e) =>
            {
                container.Location = new Point(
                    (panel.Width - container.Width) / 2,
                    (panel.Height - container.Height) / 2
                );
            };
            
            panel.Controls.Add(container);
            return panel;
        }
        #endregion
    }
}


