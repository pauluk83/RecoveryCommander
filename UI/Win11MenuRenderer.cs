/*
 * AUDIT HEADER
 * File: Win11MenuRenderer.cs
 * Module: UI / Theme
 * Created: 2026-05-02
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-05-02 - 1.3.0 - Extracted from Forms/MainForm.cs as part of the v1.3.0 refactor.
 *                       Hosts Win11-styled menu renderer + color table + DirectUIProgress.
 */

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RecoveryCommander.Forms
{
    // Windows 11-style menu renderer (rounded selection highlight, theme-aware colors).
    internal class Windows11MenuRenderer : ToolStripProfessionalRenderer
    {
        public Windows11MenuRenderer() : base(new Windows11ColorTable()) { }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                Rectangle rect = new Rectangle(0, 0, e.Item.Width - 1, e.Item.Height - 1);
                using GraphicsPath path = GetRoundedRect(rect, 4);
                using SolidBrush brush = new SolidBrush(
                    e.Item.Pressed ? UI.Theme.Colors.Primary : Color.FromArgb(30, UI.Theme.Colors.Primary));
                e.Graphics.FillPath(brush, path);
            }
            else
            {
                base.OnRenderButtonBackground(e);
            }
        }

        private static GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(bounds.Right - radius * 2, bounds.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Color table for Windows 11-style menu surfaces.
    internal class Windows11ColorTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => UI.Theme.Colors.Background;
        public override Color ToolStripGradientMiddle => UI.Theme.Colors.Background;
        public override Color ToolStripGradientEnd => UI.Theme.Colors.Background;
        public override Color ToolStripBorder => UI.Theme.Colors.Border;
        public override Color ButtonSelectedBorder => UI.Theme.Colors.Primary;
        public override Color ButtonSelectedHighlight => UI.Theme.Colors.Primary;
        public override Color ButtonSelectedGradientBegin => Color.FromArgb(30, UI.Theme.Colors.Primary);
        public override Color ButtonSelectedGradientMiddle => Color.FromArgb(30, UI.Theme.Colors.Primary);
        public override Color ButtonSelectedGradientEnd => Color.FromArgb(30, UI.Theme.Colors.Primary);
    }

    /// <summary>
    /// IProgress&lt;T&gt; implementation that marshals callbacks onto a specific control's UI thread.
    /// Safe to call before the target is fully created and during shutdown - silently no-ops if
    /// the handle has been destroyed.
    /// </summary>
    public class DirectUIProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;
        private readonly Control _syncRoot;

        public DirectUIProgress(Control syncRoot, Action<T> handler)
        {
            _syncRoot = syncRoot;
            _handler = handler;
        }

        public void Report(T value)
        {
            if (_syncRoot.IsDisposed || !_syncRoot.IsHandleCreated) return;

            try
            {
                if (_syncRoot.InvokeRequired)
                {
                    _syncRoot.BeginInvoke(new Action<T>(_handler), value);
                }
                else
                {
                    _handler(value);
                }
            }
            catch (ObjectDisposedException) { /* shutting down */ }
            catch (InvalidOperationException) { /* handle race during shutdown */ }
        }
    }
}
