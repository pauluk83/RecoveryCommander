using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using RecoveryCommander.Core.Services;
using RecoveryCommander.UI;

namespace RecoveryCommander.Features
{
    /// <summary>
    /// Themed update dialog UI that uses AutoUpdateService for core logic.
    /// Provides both manual "Check for Updates" and silent startup check flows.
    /// </summary>
    public static class AutoUpdateDialog
    {
        /// <summary>
        /// Shows a themed update dialog — either finding updates or telling the user they're current.
        /// </summary>
        public static async Task ShowUpdateDialogAsync(Form? parent, bool silentIfNoUpdate = false)
        {
            AutoUpdateService.UpdateCheckResult? result = null;

            // Show a checking cursor while querying GitHub
            if (!silentIfNoUpdate)
            {
                var checkingCursor = parent?.Cursor;
                if (parent != null) parent.Cursor = Cursors.WaitCursor;

                try
                {
                    result = await AutoUpdateService.CheckForUpdateAsync();
                }
                finally
                {
                    if (parent != null) parent.Cursor = checkingCursor ?? Cursors.Default;
                }
            }
            else
            {
                result = await AutoUpdateService.CheckForUpdateAsync();
            }

            if (result == null) return;

            // Handle errors
            if (result.ErrorMessage != null)
            {
                if (!silentIfNoUpdate)
                {
                    MessageBox.Show(parent,
                        $"Could not check for updates.\n\n{result.ErrorMessage}",
                        "Update Check Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                return;
            }

            // No update available
            if (!result.UpdateAvailable)
            {
                if (!silentIfNoUpdate)
                {
                    MessageBox.Show(parent,
                        $"You're running the latest version.\n\nCurrent version: {result.CurrentVersion}",
                        "No Updates Available",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                return;
            }

            // Show update available dialog
            ShowUpdateAvailableDialog(parent, result);
        }

        /// <summary>
        /// Displays the themed update-available dialog with release notes and install button
        /// </summary>
        private static void ShowUpdateAvailableDialog(Form? parent, AutoUpdateService.UpdateCheckResult result)
        {
            using var form = new Form
            {
                Text = "Update Available",
                Size = new Size(560, 480),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Theme.ApplyFormStyle(form);
            Theme.ApplyTheme(form);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(24)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // Header
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Release notes
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // Progress
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // Buttons

            // ── Header ──────────────────────────────────────────────────
            var headerLabel = new Label
            {
                Text = "A new version is available!",
                Font = Theme.Typography.Title,
                ForeColor = Theme.Colors.Text,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 4)
            };

            var versionLabel = new Label
            {
                Text = $"Current: v{result.CurrentVersion}  →  New: v{result.LatestVersion}",
                Font = Theme.Typography.BodyStrong,
                ForeColor = Theme.Colors.Accent,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 2)
            };

            var dateLabel = new Label
            {
                Text = result.ReleaseDate.HasValue 
                    ? $"Released: {result.ReleaseDate.Value.ToLocalTime():yyyy-MM-dd HH:mm}" 
                    : "",
                Font = Theme.Typography.Caption,
                ForeColor = Theme.Colors.SubtleText,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 8)
            };

            var headerPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Dock = DockStyle.Fill,
                WrapContents = false
            };
            headerPanel.Controls.Add(headerLabel);
            headerPanel.Controls.Add(versionLabel);
            if (result.ReleaseDate.HasValue) headerPanel.Controls.Add(dateLabel);

            layout.Controls.Add(headerPanel, 0, 0);

            // ── Release notes ───────────────────────────────────────────
            var notesBox = new Theme.RoundedRichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Text = string.IsNullOrWhiteSpace(result.ReleaseNotes)
                    ? "No release notes available."
                    : result.ReleaseNotes,
                Font = Theme.Typography.Body,
                BackColor = Theme.Colors.Surface,
                ForeColor = Theme.Colors.Text
            };
            layout.Controls.Add(notesBox, 0, 1);

            // ── Progress section (hidden initially) ─────────────────────
            var progressPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 60,
                Visible = false,
                Padding = new Padding(0, 8, 0, 0)
            };

            var progressLabel = new Label
            {
                Text = "Preparing...",
                Font = Theme.Typography.Caption,
                ForeColor = Theme.Colors.Text,
                Dock = DockStyle.Top,
                Height = 20
            };

            var progressBar = new Theme.RoundedProgressBar
            {
                Dock = DockStyle.Top,
                Height = 22,
                Value = 0,
                Maximum = 100,
                Margin = new Padding(0, 4, 0, 0)
            };

            progressPanel.Controls.Add(progressBar);
            progressPanel.Controls.Add(progressLabel);
            layout.Controls.Add(progressPanel, 0, 2);

            // ── Button bar ──────────────────────────────────────────────
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 12, 0, 0)
            };

            var installButton = new ModernButton
            {
                Text = "Download && Install",
                Width = 180,
                Height = 40,
                ButtonStyle = Theme.ButtonStyle.Primary,
                CornerRadius = 10,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var laterButton = new ModernButton
            {
                Text = "Later",
                Width = 100,
                Height = 40,
                ButtonStyle = Theme.ButtonStyle.Secondary,
                CornerRadius = 10,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 10, 0)
            };
            laterButton.Click += (s, e) => form.Close();

            // Size info
            var sizeLabel = new Label
            {
                Text = result.AssetSize > 0 ? $"Size: {result.AssetSize / 1024.0 / 1024.0:F1} MB" : "",
                Font = Theme.Typography.Caption,
                ForeColor = Theme.Colors.SubtleText,
                AutoSize = true,
                Padding = new Padding(0, 10, 10, 0)
            };

            buttonPanel.Controls.Add(installButton);
            buttonPanel.Controls.Add(laterButton);
            buttonPanel.Controls.Add(sizeLabel);
            layout.Controls.Add(buttonPanel, 0, 3);

            // ── Install click handler ───────────────────────────────────
            installButton.Click += async (s, e) =>
            {
                installButton.Enabled = false;
                laterButton.Enabled = false;
                progressPanel.Visible = true;

                var progress = new Progress<(int percent, string status)>(update =>
                {
                    if (form.IsDisposed) return;
                    try
                    {
                        progressBar.Value = Math.Min(update.percent, 100);
                        progressLabel.Text = update.status;
                    }
                    catch (ObjectDisposedException) { }
                });

                var success = await AutoUpdateService.DownloadAndApplyUpdateAsync(result, progress);

                if (success)
                {
                    // Close the application — the updater batch script takes over
                    form.Close();
                    Application.Exit();
                }
                else
                {
                    installButton.Enabled = true;
                    laterButton.Enabled = true;
                    MessageBox.Show(form,
                        "The update could not be applied.\nPlease try again or download manually from GitHub.",
                        "Update Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            form.Controls.Add(layout);
            form.ShowDialog(parent);
        }

        /// <summary>
        /// Performs a silent background update check and shows a notification only if an update is available.
        /// Call this from MainForm_Load or similar startup hook.
        /// </summary>
        public static async Task CheckForUpdateOnStartupAsync(Form parent)
        {
            try
            {
                // Small delay to let the app finish loading before checking
                await Task.Delay(3000);

                await ShowUpdateDialogAsync(parent, silentIfNoUpdate: true);
            }
            catch
            {
                // Silently fail — update check is non-critical
            }
        }
    }
}
