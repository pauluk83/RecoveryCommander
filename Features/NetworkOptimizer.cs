using System;
using System.Windows.Forms;
using System.Drawing;
using System.Management;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using RecoveryCommander.UI;
using RecoveryCommander.Core;

namespace RecoveryCommander.Features
{
    public class NetworkOptimizer : Form
    {
        private ComboBox dnsCombo = null!;
        private ComboBox adapterCombo = null!;
        private Button applyDnsBtn = null!;
        private Button resetNetworkBtn = null!;
        private Button diagnoseBtn = null!;
        private Button flushDnsBtn = null!;
        private TextBox logBox = null!;
        private TextBox pingHostTxt = null!;
        private Button pingBtn = null!;
        private RichTextBox pingLogBox = null!;
        private ProgressBar progressBar = null!;
        private Label statusLabel = null!;
        private System.Windows.Forms.Timer networkMonitorTimer = null!;
        private Label networkStatusLbl = null!;

        private readonly Dictionary<string, string> dnsServers = new()
        {
            { "Google DNS", "8.8.8.8,8.8.4.4" },
            { "Cloudflare DNS", "1.1.1.1,1.0.0.1" },
            { "OpenDNS", "208.67.222.222,208.67.220.220" },
            { "Quad9 DNS", "9.9.9.9,149.112.112.112" },
            { "AdGuard DNS", "94.140.14.14,94.140.15.15" },
            { "DHCP (Auto)", "" }
        };

        public NetworkOptimizer()
        {
            Text = "Network Repair & Optimization";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(850, 650);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = true;
            MaximizeBox = true;
            Icon = SystemIcons.Application;

            InitializeComponents();
            LoadNetworkAdapters();
            StartNetworkMonitoring();
        }

        private void InitializeComponents()
        {
            // Simple clean layout
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            // Status at top
            var statusPanel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(45, 45, 48), Margin = new Padding(0, 0, 0, 15) };
            networkStatusLbl = new Label { Text = "Checking...", Font = new Font("Segoe UI", 9f), ForeColor = Color.LightGreen, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            statusPanel.Controls.Add(networkStatusLbl);

            // DNS Section
            var dnsGroup = CreateGroupBox("DNS Settings", 140);
            
            // Simple absolute positioning approach
            var adapterLabel = new Label { Text = "Adapter:", ForeColor = Color.White, Location = new Point(10, 25), AutoSize = true };
            adapterCombo = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(80, 22) };
            
            var dnsLabel = new Label { Text = "DNS Server:", ForeColor = Color.White, Location = new Point(10, 65), AutoSize = true };
            dnsCombo = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(80, 62) };
            dnsCombo.Items.AddRange(dnsServers.Keys.ToArray());
            dnsCombo.SelectedIndex = 0;
            
            // Buttons positioned to the right
            applyDnsBtn = new RecoveryCommander.UI.ModernButton { Text = "Apply DNS", Width = 120, Height = 35, Location = new Point(320, 22), ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 8, TextAlign = ContentAlignment.MiddleCenter };
            applyDnsBtn.Click += ApplyDns_Click;
            
            flushDnsBtn = new RecoveryCommander.UI.ModernButton { Text = "Flush DNS", Width = 120, Height = 35, Location = new Point(320, 62), ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 8, TextAlign = ContentAlignment.MiddleCenter };
            flushDnsBtn.Click += FlushDns_Click;
            
            dnsGroup.Controls.AddRange(new Control[] { adapterLabel, adapterCombo, dnsLabel, dnsCombo, applyDnsBtn, flushDnsBtn });

            // Network Repair Section
            var repairGroup = CreateGroupBox("Network Repair", 80);
            var repairLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, Height = 50, Padding = new Padding(5) };
            resetNetworkBtn = new RecoveryCommander.UI.ModernButton { Text = "Reset Network", Width = 200, Height = 40, Margin = new Padding(0, 0, 10, 0), ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 10, TextAlign = ContentAlignment.MiddleLeft };
            resetNetworkBtn.Click += ResetNetwork_Click;
            diagnoseBtn = new RecoveryCommander.UI.ModernButton { Text = "Diagnose", Width = 160, Height = 40, ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 10, TextAlign = ContentAlignment.MiddleLeft };
            diagnoseBtn.Click += DiagnoseNetwork_Click;
            repairLayout.Controls.Add(resetNetworkBtn);
            repairLayout.Controls.Add(diagnoseBtn);
            repairGroup.Controls.Add(repairLayout);

            // Ping Test Section
            var pingGroup = CreateGroupBox("Connectivity Test", 180);
            var pingLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(5) };
            pingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pingLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            pingLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            pingLayout.Controls.Add(new Label { Text = "Host:", ForeColor = Color.White }, 0, 0);
            var pingInputPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            pingHostTxt = new TextBox { Width = 150, Text = "google.com" };
            pingBtn = new RecoveryCommander.UI.ModernButton { Text = "Ping", Width = 140, Height = 36, Margin = new Padding(5, 0, 0, 0), ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 10, TextAlign = ContentAlignment.MiddleLeft };
            pingBtn.Click += PingTest_Click;
            pingInputPanel.Controls.Add(pingHostTxt);
            pingInputPanel.Controls.Add(pingBtn);
            pingLayout.Controls.Add(pingInputPanel, 1, 0);

            pingLogBox = new RichTextBox { Dock = DockStyle.Fill, Height = 100, ReadOnly = true, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.LightGreen, Font = new Font("Consolas", 8f) };
            pingLayout.Controls.Add(pingLogBox, 0, 1);
            pingLayout.SetColumnSpan(pingLogBox, 2);

            pingGroup.Controls.Add(pingLayout);

            // Activity Log
            var logGroup = CreateGroupBox("Activity Log", 0); // 0 means fill remaining space
            logBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, Font = new Font("Consolas", 8f) };
            logGroup.Controls.Add(logBox);

            // Arrange sections
            var sectionsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(10) };
            sectionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); // Status
            sectionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140)); // DNS
            sectionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // Repair
            sectionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180)); // Ping
            sectionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Log

            sectionsLayout.Controls.Add(statusPanel, 0, 0);
            sectionsLayout.Controls.Add(dnsGroup, 0, 1);
            sectionsLayout.Controls.Add(repairGroup, 0, 2);
            sectionsLayout.Controls.Add(pingGroup, 0, 3);
            sectionsLayout.Controls.Add(logGroup, 0, 4);

            mainPanel.Controls.Add(sectionsLayout);

            // Progress bar at bottom
            progressBar = new ProgressBar { Dock = DockStyle.Bottom, Height = 20, Visible = false };
            statusLabel = new Label { Dock = DockStyle.Bottom, Height = 20, Text = "Ready", ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter };

            Controls.Add(mainPanel);
            Controls.Add(progressBar);
            Controls.Add(statusLabel);

            Theme.ApplyFormStyle(this);
            Theme.ApplyTheme(this);
        }

        private GroupBox CreateGroupBox(string title, int height)
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

        private void StartNetworkMonitoring()
        {
            networkMonitorTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            networkMonitorTimer.Tick += (s, e) => UpdateNetworkStatus();
            networkMonitorTimer.Start();
            UpdateNetworkStatus();
        }

        private void UpdateNetworkStatus()
        {
            try
            {
                var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback && 
                                       ni.OperationalStatus == OperationalStatus.Up);

                if (networkInterface != null)
                {
                    networkStatusLbl.Text = $"Connected - {networkInterface.Name}";
                    networkStatusLbl.ForeColor = Color.LightGreen;
                }
                else
                {
                    networkStatusLbl.Text = "Disconnected";
                    networkStatusLbl.ForeColor = Color.Red;
                }
            }
            catch
            {
                networkStatusLbl.Text = "Status Unknown";
                networkStatusLbl.ForeColor = Color.Yellow;
            }
        }

        private void LoadNetworkAdapters()
        {
            try
            {
                var adapters = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Select(ni => ni.Name)
                    .ToArray();

                adapterCombo.Items.AddRange(adapters);
                if (adapterCombo.Items.Count > 0)
                    adapterCombo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to load network adapters: {ex.Message}");
            }
        }

        private async void ApplyDns_Click(object? sender, EventArgs e)
        {
            if (adapterCombo.SelectedItem == null || dnsCombo.SelectedItem == null) return;

            var adapter = adapterCombo.SelectedItem.ToString();
            if (dnsCombo.SelectedItem is string dnsKey && dnsServers.TryGetValue(dnsKey, out var dnsValue))
            {
                await RunOperationAsync("Applying DNS Settings", async () =>
                {
                    if (string.IsNullOrEmpty(dnsValue))
                    {
                        await ExecuteCommandAsync($"netsh interface ip set dns \"{adapter}\" dhcp");
                        LogMessage($"DNS set to DHCP for {adapter}");
                    }
                    else
                    {
                        var primary = dnsValue.Split(',')[0];
                        await ExecuteCommandAsync($"netsh interface ip set dns \"{adapter}\" static {primary}");
                        LogMessage($"DNS set to {dnsKey} for {adapter}");
                    }
                });
            }
        }

        private async void FlushDns_Click(object? sender, EventArgs e)
        {
            await RunOperationAsync("Flushing DNS Cache", async () =>
            {
                await ExecuteCommandAsync("ipconfig /flushdns");
                LogMessage("DNS cache flushed successfully");
            });
        }

        private async void ResetNetwork_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("This will reset all network settings. Continue?", 
                "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            
            if (result != DialogResult.Yes) return;

            await RunOperationAsync("Resetting Network", async () =>
            {
                await ExecuteCommandAsync("netsh winsock reset");
                await ExecuteCommandAsync("netsh int ip reset");
                await ExecuteCommandAsync("ipconfig /flushdns");
                LogMessage("Network reset completed");
            });
        }

        private async void DiagnoseNetwork_Click(object? sender, EventArgs e)
        {
            await RunOperationAsync("Diagnosing Network", async () =>
            {
                LogMessage("Starting network diagnosis...");
                await ExecuteCommandAsync("ping -n 2 8.8.8.8");
                await ExecuteCommandAsync("nslookup google.com");
                LogMessage("Network diagnosis completed");
            });
        }

        private async void PingTest_Click(object? sender, EventArgs e)
        {
            var host = pingHostTxt.Text.Trim();
            host = RecoveryCommander.Core.SecurityHelpers.SanitizeCommandArguments(host);
            if (string.IsNullOrEmpty(host)) return;

            pingLogBox.Clear();
            await RunOperationAsync($"Pinging {host}", async () =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ping",
                    Arguments = $"-n 4 {host}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();
                    
                    var result = output + error;
                    pingLogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {result}\n");
                    pingLogBox.SelectionStart = pingLogBox.TextLength;
                    pingLogBox.ScrollToCaret();
                }
            });
        }

        private async Task RunOperationAsync(string operation, Func<Task> action)
        {
            try
            {
                statusLabel.Text = operation;
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                
                LogMessage($"[{DateTime.Now:HH:mm:ss}] {operation}...");
                await action();
                LogMessage($"[{DateTime.Now:HH:mm:ss}] {operation} completed");
            }
            catch (Exception ex)
            {
                LogMessage($"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}");
                MessageBox.Show($"Operation failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                statusLabel.Text = "Ready";
                progressBar.Visible = false;
            }
        }

        private async Task ExecuteCommandAsync(string command)
        {
            try
            {
                var sanitizedCommand = RecoveryCommander.Core.SecurityHelpers.SanitizeCommandArguments(command);
                if (string.IsNullOrWhiteSpace(sanitizedCommand))
                {
                    LogMessage("ERROR: Invalid or empty command after sanitization.");
                    return;
                }
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {sanitizedCommand}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                    // Note: Verb="runas" has no effect when UseShellExecute=false
                    // The app must already be running as admin for network commands to work
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();
                    
                    if (!string.IsNullOrEmpty(output))
                        LogMessage(output.Trim());
                    
                    if (!string.IsNullOrEmpty(error))
                        LogMessage($"ERROR: {error.Trim()}");
                }
                else
                {
                    LogMessage("ERROR: Failed to start process.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: Command execution failed: {ex.Message}");
            }
        }

        private void LogMessage(string message)
        {
            if (logBox.InvokeRequired)
            {
                logBox.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                networkMonitorTimer?.Stop();
                networkMonitorTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
