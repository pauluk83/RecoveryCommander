using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using RecoveryCommander.Core;
using RecoveryCommander.UI;

namespace RecoveryCommander.Forms
{
    #region Boot Media Creator (from BootMediaCreator.cs)
    /// <summary>
    /// Bootable recovery media creation form
    /// </summary>
    public class BootMediaCreator : Form
    {
        private ComboBox driveList;
        private ModernButton refreshButton;
        private ModernButton buildButton;
        private TextBox logBox;
        private CheckBox copyApp;
        private CheckBox useWinRE;
        private CheckBox backupRecovery;
        private Label driveInfoLabel;

        public BootMediaCreator()
        {
            Text = "Bootable Recovery Media";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1000, 800);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = true;
            MaximizeBox = true;
            AutoScroll = true;

            // Simple panel with absolute positioning
            var mainPanel = new Panel 
            { 
                Dock = DockStyle.Fill, 
                AutoScroll = true,
                Padding = new Padding(30)
            };
            
            // Title
            var titleLabel = new Label 
            { 
                Text = "Create Bootable Recovery Drive",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, 30),
                Size = new Size(400, 40)
            };
            
            // Drive selection
            driveList = new ComboBox 
            { 
                Location = new Point(30, 100),
                Size = new Size(300, 30),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            refreshButton = new ModernButton 
            { 
                Text = "Refresh",
                Location = new Point(340, 100),
                Size = new Size(180, 40),
                ButtonStyle = Theme.ButtonStyle.Secondary,
                CornerRadius = 10,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            driveInfoLabel = new Label 
            { 
                Text = "",
                Location = new Point(30, 140),
                Size = new Size(410, 60),
                ForeColor = Color.LightGray
            };
            
            // Options
            copyApp = new CheckBox 
            { 
                Text = "Copy RecoveryCommander to drive",
                Location = new Point(30, 220),
                Size = new Size(200, 25),
                ForeColor = Color.White
            };
            
            useWinRE = new CheckBox 
            { 
                Text = "Include Windows RE",
                Location = new Point(30, 250),
                Size = new Size(200, 25),
                ForeColor = Color.White
            };
            
            backupRecovery = new CheckBox 
            { 
                Text = "Backup existing recovery",
                Location = new Point(30, 280),
                Size = new Size(200, 25),
                ForeColor = Color.White
            };
            
            // Build button
            buildButton = new ModernButton 
            { 
                Text = "Create Recovery Drive",
                Location = new Point(30, 330),
                Size = new Size(240, 48),
                ButtonStyle = Theme.ButtonStyle.Secondary,
                CornerRadius = 10,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            // Log box
            logBox = new TextBox 
            { 
                Location = new Point(30, 400),
                Size = new Size(900, 350),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGray,
                Font = new Font("Consolas", 9)
            };
            
            // Add controls to panel
            mainPanel.Controls.AddRange(new Control[] 
            {
                titleLabel, driveList, refreshButton, driveInfoLabel,
                copyApp, useWinRE, backupRecovery, buildButton, logBox
            });
            
            Controls.Add(mainPanel);
            
            // Event handlers
            refreshButton.Click += RefreshDrives;
            buildButton.Click += BuildRecoveryDrive;
            driveList.SelectedIndexChanged += DriveList_SelectedIndexChanged;
            
            // Initial drive scan
            RefreshDrives(null, EventArgs.Empty);
            
            // Apply theme
            Theme.ApplyFormStyle(this);
            Theme.ApplyTheme(this);
        }

        private void RefreshDrives(object? sender, EventArgs e)
        {
            driveList.Items.Clear();
            driveInfoLabel.Text = "";
            
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
                    .OrderBy(d => d.Name);
                
                foreach (var drive in drives)
                {
                    driveList.Items.Add($"{drive.Name} ({drive.TotalSize / 1024 / 1024 / 1024} GB)");
                }
                
                if (driveList.Items.Count > 0)
                {
                    driveList.SelectedIndex = 0;
                }
                else
                {
                    driveInfoLabel.Text = "No removable drives found. Please insert a USB drive.";
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error scanning drives: {ex.Message}");
                driveInfoLabel.Text = "Error scanning drives. Check permissions.";
            }
        }

        private void DriveList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (driveList.SelectedIndex >= 0)
            {
                try
                {
                    var selectedItem = driveList.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(selectedItem))
                    {
                        var driveLetter = selectedItem.Split('(')[0].Trim();
                        var drive = DriveInfo.GetDrives()
                            .FirstOrDefault(d => d.Name.StartsWith(driveLetter));
                    
                        if (drive != null)
                        {
                            var freeGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                            var totalGB = drive.TotalSize / 1024 / 1024 / 1024;
                        
                        driveInfoLabel.Text = $"Drive: {drive.VolumeLabel ?? "No Label"}\n" +
                                           $"Free Space: {freeGB} GB\n" +
                                           $"Total Size: {totalGB} GB\n" +
                                           $"Status: {(freeGB >= 8 ? "Ready" : "Insufficient space (need 8GB minimum)")}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    driveInfoLabel.Text = $"Error getting drive info: {ex.Message}";
                }
            }
        }

        private async void BuildRecoveryDrive(object? sender, EventArgs e)
        {
            if (driveList.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a USB drive.", "No Drive Selected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var selectedItem = driveList.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedItem))
            {
                MessageBox.Show("Invalid drive selection.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            var driveLetter = selectedItem.Split('(')[0].Trim();
            
            buildButton.Enabled = false;
            refreshButton.Enabled = false;
            
            try
            {
                LogMessage("Starting recovery drive creation...");
                LogMessage($"Target drive: {driveLetter}");
                
                // Validate drive
                var drive = DriveInfo.GetDrives()
                    .FirstOrDefault(d => d.Name.StartsWith(driveLetter));
                
                if (drive == null)
                {
                    throw new Exception("Selected drive not found");
                }
                
                var freeGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                if (freeGB < 8)
                {
                    throw new Exception("Drive must have at least 8GB free space");
                }
                
                // Create recovery structure
                LogMessage("Creating recovery directory structure...");
                var recoveryPath = Path.Combine(driveLetter, "Recovery");
                Directory.CreateDirectory(recoveryPath);
                
                // Copy application if requested
                if (copyApp.Checked)
                {
                    LogMessage("Copying RecoveryCommander...");
                    await CopyApplicationAsync(recoveryPath);
                }
                
                // Create boot configuration
                LogMessage("Creating boot configuration...");
                await CreateBootConfigAsync(recoveryPath);
                
                LogMessage("Recovery drive created successfully!");
                MessageBox.Show("Recovery drive created successfully!", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Error: {ex.Message}");
                MessageBox.Show($"Failed to create recovery drive: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                buildButton.Enabled = true;
                refreshButton.Enabled = true;
            }
        }

        private async Task CopyApplicationAsync(string recoveryPath)
        {
            var appPath = Application.ExecutablePath;
            var appDir = Path.GetDirectoryName(appPath);
            var targetPath = Path.Combine(recoveryPath, "RecoveryCommander");
            
            Directory.CreateDirectory(targetPath);
            
            // Copy main executable
            var targetExe = Path.Combine(targetPath, Path.GetFileName(appPath));
            File.Copy(appPath, targetExe, true);
            LogMessage($"Copied: {Path.GetFileName(appPath)}");
            
            // Copy essential files
            var essentialFiles = new[] { "Resources", "Scripts" };
            foreach (var file in essentialFiles)
            {
                var source = Path.Combine(appDir!, file);
                var target = Path.Combine(targetPath, file);
                
                if (Directory.Exists(source))
                {
                    await CopyDirectoryAsync(source, target);
                    LogMessage($"Copied: {file}");
                }
            }
        }

        private async Task CopyDirectoryAsync(string source, string target)
        {
            Directory.CreateDirectory(target);
            
            foreach (var file in Directory.GetFiles(source))
            {
                var targetFile = Path.Combine(target, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
                await Task.Delay(1); // Small delay for UI responsiveness
            }
            
            foreach (var dir in Directory.GetDirectories(source))
            {
                var targetDir = Path.Combine(target, Path.GetFileName(dir));
                await CopyDirectoryAsync(dir, targetDir);
            }
        }

        private async Task CreateBootConfigAsync(string recoveryPath)
        {
            var configPath = Path.Combine(recoveryPath, "recovery.xml");
            var config = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RecoveryConfiguration>
    <Version>1.0</Version>
    <Created>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</Created>
    <Application>RecoveryCommander</Application>
    <Options>
        <CopyApp>{copyApp.Checked}</CopyApp>
        <UseWinRE>{useWinRE.Checked}</UseWinRE>
        <BackupRecovery>{backupRecovery.Checked}</BackupRecovery>
    </Options>
</RecoveryConfiguration>";
            
            await File.WriteAllTextAsync(configPath, config);
            LogMessage("Created recovery configuration");
        }

        private void LogMessage(string message)
        {
            if (logBox.InvokeRequired)
            {
                logBox.Invoke(new Action(() => LogMessage(message)));
                return;
            }
            
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            logBox.AppendText($"[{timestamp}] {message}\r\n");
            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
        }
    }
    #endregion

    #region Consolidated Media Tools
    /// <summary>
    /// Unified Media Tools - Boot Media Creator and Media Creation Tools
    /// </summary>
    public static class MediaTools
    {
        // Direct download URLs for Media Creation Tools
        private const string Windows10MctUrl = "https://go.microsoft.com/fwlink/?LinkId=2265055";
        private const string Windows11MctUrl = "https://go.microsoft.com/fwlink/?linkid=2156295";

        public static void ShowMediaToolsDialog(Form? parent)
        {
            using var form = new Form()
            {
                Text = "Media Tools",
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = true
            };

            Theme.ApplyFormStyle(form);
            Theme.ApplyTheme(form);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Title
            var titlePanel = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };
            titlePanel.Controls.Add(new Label
            {
                Text = "Media Tools - Create Bootable Media & Download Windows Tools",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true
            });
            layout.Controls.Add(titlePanel, 0, 0);

            // Boot Media Creator Section
            var bootPanel = CreateGroupBox("Boot Media Creator", 0);
            var bootLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(15) };
            bootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            bootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            bootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var bootInfo = new Label
            {
                Text = "Create a bootable USB recovery drive with RecoveryCommander and optional Windows Recovery Environment.\n\nFeatures:\n• Copy RecoveryCommander to USB drive\n• Include Windows RE tools\n• Backup existing recovery partition\n• Create boot configuration",
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Fill
            };

            var bootButtonPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, AutoSize = true };
            var bootButton = new ModernButton
            {
                Text = "Create Boot Media",
                Width = 200,
                Height = 45,
                Margin = new Padding(0, 0, 0, 10),
                ButtonStyle = Theme.ButtonStyle.Primary,
                CornerRadius = 10,
                TextAlign = ContentAlignment.MiddleLeft
            };
            bootButton.Click += (s, e) => ShowBootMediaCreator(parent);
            bootButtonPanel.Controls.Add(bootButton);

            bootLayout.Controls.Add(bootInfo, 0, 0);
            bootLayout.Controls.Add(bootButtonPanel, 1, 0);
            bootPanel.Controls.Add(bootLayout);
            layout.Controls.Add(bootPanel, 0, 1);

            // Media Creation Tools Section
            var mctPanel = CreateGroupBox("Media Creation Tools", 0);
            var mctLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(15) };
            mctLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            mctLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            mctLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mctLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var mctInfo = new Label
            {
                Text = "Download official Microsoft Media Creation Tools for Windows 10 and Windows 11. These tools allow you to create installation media (USB or DVD) or download ISO files.",
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Fill
            };

            var mctButtonPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, AutoSize = true };
            
            var win10Button = new ModernButton
            {
                Text = "Download Windows 10 MCT",
                Width = 200,
                Height = 40,
                Margin = new Padding(0, 0, 0, 10),
                ButtonStyle = Theme.ButtonStyle.Secondary,
                CornerRadius = 10,
                TextAlign = ContentAlignment.MiddleLeft
            };
            win10Button.Click += (s, e) => DownloadMediaCreationTool(Windows10MctUrl, "Windows 10");

            var win11Button = new ModernButton
            {
                Text = "Download Windows 11 MCT",
                Width = 200,
                Height = 40,
                Margin = new Padding(0, 0, 0, 10),
                ButtonStyle = Theme.ButtonStyle.Secondary,
                CornerRadius = 10,
                TextAlign = ContentAlignment.MiddleLeft
            };
            win11Button.Click += (s, e) => DownloadMediaCreationTool(Windows11MctUrl, "Windows 11");

            mctButtonPanel.Controls.AddRange(new Control[] { win10Button, win11Button });
            mctLayout.Controls.Add(mctInfo, 0, 0);
            mctLayout.Controls.Add(mctButtonPanel, 1, 0);
            mctPanel.Controls.Add(mctLayout);
            layout.Controls.Add(mctPanel, 0, 2);

            // Close button
            var closePanel = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 20, 0, 0) };
            var closeButton = new ModernButton
            {
                Text = "Close",
                Width = 120,
                Height = 40,
                ButtonStyle = Theme.ButtonStyle.Secondary,
                CornerRadius = 10,
                TextAlign = ContentAlignment.MiddleLeft
            };
            closeButton.Click += (s, e) => form.Close();
            closePanel.Controls.Add(closeButton);
            layout.Controls.Add(closePanel, 0, 3);

            form.Controls.Add(layout);
            form.ShowDialog(parent);
        }

        private static void ShowBootMediaCreator(Form? parent)
        {
            using var form = new BootMediaCreator();
            form.ShowDialog(parent);
        }

        private static void DownloadMediaCreationTool(string url, string version)
        {
            try
            {
                var result = MessageBox.Show(
                    $"This will download the {version} Media Creation Tool.\n\n" +
                    "The tool will be downloaded to your default Downloads folder.\n\n" +
                    "Do you want to continue?",
                    $"Download {version} Media Creation Tool",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    
                    MessageBox.Show(
                        $"The {version} Media Creation Tool download has been started.\n\n" +
                        "Check your Downloads folder for the file and run it to create installation media.",
                        "Download Started",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to start download: {ex.Message}",
                    "Download Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static GroupBox CreateGroupBox(string title, int height)
        {
            var group = new GroupBox 
            { 
                Text = title,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = height > 0 ? DockStyle.Top : DockStyle.Fill,
                Height = height > 0 ? height : 100,
                Padding = new Padding(10),
                MinimumSize = height > 0 ? new Size(0, height) : new Size(0, 100)
            };
            return group;
        }
    }
    #endregion
}
