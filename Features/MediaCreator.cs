using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using RecoveryCommander.UI;
using RecoveryCommander.Core;

namespace RecoveryCommander.Features
{
    public static class MediaCreator
    {
        // Official Microsoft download pages (recommended)
        private const string Windows10Page = "https://www.microsoft.com/en-us/software-download/windows10";
        private const string Windows11Page = "https://www.microsoft.com/software-download/windows11";

        // Known redirect links for Media Creation Tools (updated 2024)
        private const string Windows10Mct = "https://go.microsoft.com/fwlink/?LinkId=2265055"; // Updated Windows 10 MCT link
        // Windows 11 Media Creation Tool direct fwlink (updated 2024) - use redirect
        private const string Windows11Mct = "https://go.microsoft.com/fwlink/?linkid=2171764"; // Updated Windows 11 MCT link

        public static void ShowMediaCreatorDialog(Form? parent)
        {
            using var form = new Form()
            {
                Text = "Media Creation Tools",
                Size = new Size(600, 320),
                StartPosition = FormStartPosition.CenterParent
            };

            Theme.ApplyFormStyle(form);
            Theme.ApplyTheme(form);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(18)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var infoPanel = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(16) };
            infoPanel.Controls.Add(new Label
            {
                Text = "Download official Microsoft Media Creation Tools for Windows 10 and Windows 11. These tools allow you to create installation media (USB or DVD) or ISO files.",
                AutoSize = false,
                Width = 520,
                Height = 60,
                ForeColor = Color.White,
                Font = Theme.Typography.Subtitle
            });

            var actionPanel = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(16) };
            var columns = new TableLayoutPanel { ColumnCount = 2, AutoSize = true };
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            var leftCol = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Margin = new Padding(0, 0, 16, 0) };
            var rightCol = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };

            var btnWin10Download = new Button { Text = "Download Windows 10 Media Creation Tool", Width = 280, Height = 38 };
            var btnWin11Download = new Button { Text = "Download Windows 11 Media Creation Tool", Width = 280, Height = 38 };

            Theme.ApplyButtonStyle(btnWin10Download, Theme.ButtonStyle.FuturisticPrimary);
            Theme.ApplyButtonStyle(btnWin11Download, Theme.ButtonStyle.FuturisticPrimary);

            btnWin10Download.Click += async (s, e) => await DownloadAndOfferAsync(Windows10Mct, "MediaCreationTool.exe", parent);
            btnWin11Download.Click += async (s, e) => await DownloadAndOfferAsync(Windows11Mct, "MediaCreationToolWindows11.exe", parent);

            leftCol.Controls.Add(btnWin10Download);
            rightCol.Controls.Add(btnWin11Download);

            columns.Controls.Add(leftCol, 0, 0);
            columns.Controls.Add(rightCol, 1, 0);
            actionPanel.Controls.Add(columns);

            var footerPanel = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(16) };
            var btnClose = new Button { Text = "Close", Width = 140, Height = 38 };
            btnClose.Click += (s, e) => form.Close();
            Theme.ApplyButtonStyle(btnClose, Theme.ButtonStyle.FuturisticGhost);
            footerPanel.Controls.Add(btnClose);

            layout.Controls.Add(infoPanel, 0, 0);
            layout.Controls.Add(actionPanel, 0, 1);
            layout.Controls.Add(footerPanel, 0, 2);

            form.Controls.Add(layout);

            if (parent != null) form.ShowDialog(parent); else form.ShowDialog();
        }

        private static void OpenUrl(string url)
        {
            if (!SecurityHelpers.IsValidDownloadUrl(url, out _)) return;
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static async Task DownloadAndOfferAsync(string url, string defaultFileName, Form? owner)
        {
            try
            {
                await CoreUtilities.DownloadAndExecuteAsync(
                    url: url, 
                    fileName: defaultFileName, 
                    allowedExtensions: null, 
                    progress: null, 
                    reportOutput: null, 
                    cancellationToken: CancellationToken.None);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, $"Download/Execute failed: {ex.Message}\nOpening official download page instead.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                OpenUrl(url);
            }
        }
    }
}
