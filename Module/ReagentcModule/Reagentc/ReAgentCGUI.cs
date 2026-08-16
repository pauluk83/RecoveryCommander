using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ReAgentCGUI
{
    public class MainForm : Form
    {
        // UI Controls
        private Panel headerPanel;
        private Label titleLabel;
        private Label subtitleLabel;
        private Label adminStatusBadge;
        private Button btnElevate;
        private Button btnRefreshStatus;

        private GroupBox dashboardGroup;
        private Label lblStateVal;
        private Label lblLocationVal;
        private Label lblBcdVal;
        private Label lblCustomImgVal;

        private TabControl tabControl;
        private TabPage tabInfo;
        private TabPage tabEnable;
        private TabPage tabDisable;
        private TabPage tabBoottore;
        private TabPage tabSetReImage;
        private TabPage tabSetOsImage;
        private TabPage tabBootRank;
        private TabPage tabMigrate;
        private TabPage tabCustom;

        // Form Labels & Inputs
        private Label lblTarget;
        private TextBox txtTarget;
        private CheckBox chkAuditmode;
        private Label lblOsGuid;
        private TextBox txtOsGuid;
        private Label lblReason;
        private TextBox txtReason;
        private Label lblPath;
        private TextBox txtPath;
        private Button btnBrowsePath;
        private Label lblIndex;
        private NumericUpDown numIndex;
        private Label lblBootRank;
        private NumericUpDown numBootRank;
        private Label lblCustom;
        private TextBox txtCustomArgs;

        private Label lblPreviewText;
        private Button btnExecute;

        private RichTextBox txtConsole;
        private Label lblExitCode;
        private Button btnCopyLog;
        private Button btnClearLog;
        private Button btnCopyCmd;

        private Panel quickActionsPanel;
        private Label lblTabDescription;
        private ToolTip toolTip;

        // State
        private bool isAdmin = false;

        [STAThread]
        public static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error launching ReAgentC Manager:\n\n" + ex.ToString(), "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public MainForm()
        {
            toolTip = new ToolTip { AutoPopDelay = 8000, InitialDelay = 400, ReshowDelay = 200, ShowAlways = true };
            InitializeComponent();
            SetupToolTips();
            CheckAdminStatus();
            FetchWinReStatus();
        }

        private string GetReagentcExecutable()
        {
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            
            // Check Sysnative first for 32-bit process running on 64-bit OS
            string sysnative = Path.Combine(winDir, "Sysnative\\reagentc.exe");
            if (File.Exists(sysnative)) return sysnative;

            // Check System32
            string system32 = Path.Combine(winDir, "System32\\reagentc.exe");
            if (File.Exists(system32)) return system32;

            return "reagentc.exe";
        }

        private void InitializeComponent()
        {
            this.Text = "ReAgentC Manager — Windows RE Control Suite";
            this.Size = new Size(1180, 780);
            this.MinimumSize = new Size(980, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.ForeColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            // --- Header ---
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(16, 12, 16, 12)
            };

            titleLabel = new Label
            {
                Text = "ReAgentC Manager",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249),
                AutoSize = true,
                Location = new Point(16, 12)
            };

            subtitleLabel = new Label
            {
                Text = "Windows Recovery Environment (WinRE) Native Control Center",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Location = new Point(17, 38)
            };

            adminStatusBadge = new Label
            {
                Text = "Checking Admin...",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Size = new Size(230, 32),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(this.ClientSize.Width - 430, 18),
                BackColor = Color.FromArgb(245, 158, 11),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            btnElevate = new Button
            {
                Text = "🛡 Run as Admin",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Size = new Size(130, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(this.ClientSize.Width - 190, 18),
                BackColor = Color.FromArgb(99, 102, 241),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnElevate.FlatAppearance.BorderSize = 0;
            btnElevate.Click += (s, e) => ElevateApp();

            btnRefreshStatus = new Button
            {
                Text = "🔄 Refresh",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Size = new Size(80, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(this.ClientSize.Width - 55, 18),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRefreshStatus.FlatAppearance.BorderSize = 0;
            btnRefreshStatus.Click += (s, e) => FetchWinReStatus();

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subtitleLabel);
            headerPanel.Controls.Add(adminStatusBadge);
            headerPanel.Controls.Add(btnElevate);
            headerPanel.Controls.Add(btnRefreshStatus);

            // --- Dashboard GroupBox ---
            dashboardGroup = new GroupBox
            {
                Text = " WinRE System Overview ",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(99, 102, 241),
                Location = new Point(16, 80),
                Size = new Size(this.ClientSize.Width - 32, 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(30, 41, 59)
            };

            TableLayoutPanel dashTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                Padding = new Padding(8)
            };
            dashTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            dashTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            dashTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            dashTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

            Label l1 = new Label { Text = "STATE:", ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            Label l2 = new Label { Text = "WINRE LOCATION:", ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            Label l3 = new Label { Text = "BCD GUID:", ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            Label l4 = new Label { Text = "CUSTOM IMAGE:", ForeColor = Color.FromArgb(148, 163, 184), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };

            lblStateVal = new Label { Text = "Loading...", ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), AutoSize = true };
            lblLocationVal = new Label { Text = "--", ForeColor = Color.FromArgb(203, 213, 225), AutoSize = true };
            lblBcdVal = new Label { Text = "--", ForeColor = Color.FromArgb(203, 213, 225), AutoSize = true };
            lblCustomImgVal = new Label { Text = "--", ForeColor = Color.FromArgb(203, 213, 225), AutoSize = true };

            dashTable.Controls.Add(l1, 0, 0);
            dashTable.Controls.Add(l2, 1, 0);
            dashTable.Controls.Add(l3, 2, 0);
            dashTable.Controls.Add(l4, 3, 0);

            dashTable.Controls.Add(lblStateVal, 0, 1);
            dashTable.Controls.Add(lblLocationVal, 1, 1);
            dashTable.Controls.Add(lblBcdVal, 2, 1);
            dashTable.Controls.Add(lblCustomImgVal, 3, 1);

            dashboardGroup.Controls.Add(dashTable);

            // --- Quick Actions Panel ---
            quickActionsPanel = new Panel
            {
                Location = new Point(16, 188),
                Size = new Size(this.ClientSize.Width - 32, 44),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent
            };

            Label quickLabel = new Label
            {
                Text = "QUICK ACTIONS:",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(0, 14)
            };

            string[] quickNames = { "Query Info", "Enable WinRE", "Boot to RE", "Disable WinRE" };
            int[] quickTabs = { 0, 1, 3, 2 };
            Color[] quickColors = {
                Color.FromArgb(51, 65, 85),
                Color.FromArgb(99, 102, 241),
                Color.FromArgb(6, 182, 212),
                Color.FromArgb(190, 18, 60)
            };

            for (int i = 0; i < quickNames.Length; i++)
            {
                int tabIdx = quickTabs[i];
                Button qb = new Button
                {
                    Text = quickNames[i],
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Size = new Size(120, 30),
                    Location = new Point(110 + i * 128, 8),
                    BackColor = quickColors[i],
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                qb.FlatAppearance.BorderSize = 0;
                qb.Click += (s, e) => { tabControl.SelectedIndex = tabIdx; ExecuteCurrentCommand(); };
                quickActionsPanel.Controls.Add(qb);
            }
            quickActionsPanel.Controls.Add(quickLabel);

            // --- Split Container ---
            SplitContainer splitContainer = new SplitContainer
            {
                Location = new Point(16, 240),
                Size = new Size(this.ClientSize.Width - 32, this.ClientSize.Height - 252),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Orientation = Orientation.Vertical,
                SplitterDistance = 560,
                BackColor = Color.FromArgb(15, 23, 42)
            };

            // Left Side: TabControl & Form Controls
            tabControl = new TabControl
            {
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Padding = new Point(8, 4)
            };

            tabInfo = new TabPage("Info");
            tabEnable = new TabPage("Enable");
            tabDisable = new TabPage("Disable");
            tabBoottore = new TabPage("Boot to RE");
            tabSetReImage = new TabPage("Set RE Image");
            tabSetOsImage = new TabPage("Set OS Image");
            tabBootRank = new TabPage("Boot Rank");
            tabMigrate = new TabPage("Migrate");
            tabCustom = new TabPage("Custom");

            tabControl.TabPages.Add(tabInfo);
            tabControl.TabPages.Add(tabEnable);
            tabControl.TabPages.Add(tabDisable);
            tabControl.TabPages.Add(tabBoottore);
            tabControl.TabPages.Add(tabSetReImage);
            tabControl.TabPages.Add(tabSetOsImage);
            tabControl.TabPages.Add(tabBootRank);
            tabControl.TabPages.Add(tabMigrate);
            tabControl.TabPages.Add(tabCustom);

            tabControl.SelectedIndexChanged += (s, e) => OnTabChanged();

            // Tab description label
            lblTabDescription = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(12, 8, 12, 0),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.FromArgb(30, 41, 59),
                Text = "Displays Windows RE status and configuration parameters."
            };

            // Form Inputs Container Panel (scrollable)
            Panel formPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(12),
                AutoScroll = true
            };

            // Fields instantiation
            lblTarget = new Label { Text = "Target OS Path (/target):", Location = new Point(12, 12), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184) };
            txtTarget = new TextBox { Location = new Point(12, 32), Width = 400, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            txtTarget.TextChanged += (s, e) => UpdateCommandPreview();

            chkAuditmode = new CheckBox { Text = "Audit Mode (/auditmode)", Location = new Point(12, 65), AutoSize = true, ForeColor = Color.White, Visible = false };
            chkAuditmode.CheckedChanged += (s, e) => UpdateCommandPreview();

            lblOsGuid = new Label { Text = "OS GUID (/osguid):", Location = new Point(12, 95), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184), Visible = false };
            txtOsGuid = new TextBox { Location = new Point(12, 115), Width = 380, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Visible = false };
            txtOsGuid.TextChanged += (s, e) => UpdateCommandPreview();

            lblReason = new Label { Text = "Boot Reason String (/reason):", Location = new Point(12, 65), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184), Visible = false };
            txtReason = new TextBox { Location = new Point(12, 85), Width = 380, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Visible = false };
            txtReason.TextChanged += (s, e) => UpdateCommandPreview();

            lblPath = new Label { Text = "Folder / Image Path (/path):", Location = new Point(12, 65), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184), Visible = false };
            txtPath = new TextBox { Location = new Point(12, 85), Width = 300, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Visible = false };
            txtPath.TextChanged += (s, e) => UpdateCommandPreview();

            btnBrowsePath = new Button { Text = "Browse...", Location = new Point(318, 84), Size = new Size(74, 25), BackColor = Color.FromArgb(51, 65, 85), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Visible = false };
            btnBrowsePath.Click += (s, e) => BrowseFolder();

            lblIndex = new Label { Text = "Image Index (/index):", Location = new Point(12, 118), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184), Visible = false };
            numIndex = new NumericUpDown { Location = new Point(12, 138), Width = 100, Minimum = 1, Maximum = 99, Value = 1, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, Visible = false };
            numIndex.ValueChanged += (s, e) => UpdateCommandPreview();

            lblBootRank = new Label { Text = "Boot Rank Value (/bootrank):", Location = new Point(12, 65), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184), Visible = false };
            numBootRank = new NumericUpDown { Location = new Point(12, 85), Width = 100, Minimum = 0, Maximum = 99, Value = 1, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, Visible = false };
            numBootRank.ValueChanged += (s, e) => UpdateCommandPreview();

            lblCustom = new Label { Text = "Raw Arguments:", Location = new Point(12, 12), AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184), Visible = false };
            txtCustomArgs = new TextBox { Location = new Point(12, 32), Width = 380, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Visible = false };
            txtCustomArgs.TextChanged += (s, e) => UpdateCommandPreview();

            // Command Preview & Run Button Panel
            Panel previewPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(10)
            };

            Label lblPreviewHeader = new Label
            {
                Text = "COMMAND PREVIEW",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(99, 102, 241),
                Location = new Point(10, 6),
                AutoSize = true
            };

            lblPreviewText = new Label
            {
                Text = "reagentc.exe /info",
                Font = new Font("Consolas", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(10, 22),
                AutoSize = true,
                MaximumSize = new Size(380, 0)
            };

            btnCopyCmd = new Button
            {
                Text = "Copy",
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                Size = new Size(54, 26),
                Location = new Point(400, 20),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCopyCmd.FlatAppearance.BorderSize = 0;
            btnCopyCmd.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(lblPreviewText.Text))
                {
                    Clipboard.SetText(lblPreviewText.Text);
                    LogConsole("Command copied to clipboard.", Color.FromArgb(148, 163, 184));
                }
            };

            btnExecute = new Button
            {
                Text = "▶  Run Command  (Ctrl+Enter)",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Size = new Size(220, 36),
                Location = new Point(10, 48),
                BackColor = Color.FromArgb(99, 102, 241),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExecute.FlatAppearance.BorderSize = 0;
            btnExecute.Click += (s, e) => ExecuteCurrentCommand();

            previewPanel.Controls.Add(lblPreviewHeader);
            previewPanel.Controls.Add(lblPreviewText);
            previewPanel.Controls.Add(btnCopyCmd);
            previewPanel.Controls.Add(btnExecute);

            formPanel.Controls.Add(lblTarget);
            formPanel.Controls.Add(txtTarget);
            formPanel.Controls.Add(chkAuditmode);
            formPanel.Controls.Add(lblOsGuid);
            formPanel.Controls.Add(txtOsGuid);
            formPanel.Controls.Add(lblReason);
            formPanel.Controls.Add(txtReason);
            formPanel.Controls.Add(lblPath);
            formPanel.Controls.Add(txtPath);
            formPanel.Controls.Add(btnBrowsePath);
            formPanel.Controls.Add(lblIndex);
            formPanel.Controls.Add(numIndex);
            formPanel.Controls.Add(lblBootRank);
            formPanel.Controls.Add(numBootRank);
            formPanel.Controls.Add(lblCustom);
            formPanel.Controls.Add(txtCustomArgs);

            splitContainer.Panel1.Controls.Add(formPanel);
            splitContainer.Panel1.Controls.Add(previewPanel);
            splitContainer.Panel1.Controls.Add(lblTabDescription);
            splitContainer.Panel1.Controls.Add(tabControl);

            // Right Side: Terminal Console
            Panel consoleHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(17, 23, 38)
            };

            Label consoleTitle = new Label
            {
                Text = "CONSOLE OUTPUT",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(10, 8),
                AutoSize = true
            };

            lblExitCode = new Label
            {
                Text = "Exit Code: --",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(203, 213, 225),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(splitContainer.Panel2.Width - 220, 8),
                AutoSize = true
            };

            btnCopyLog = new Button
            {
                Text = "Copy",
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                Size = new Size(50, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(splitContainer.Panel2.Width - 110, 5),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCopyLog.Click += (s, e) => { if (!string.IsNullOrEmpty(txtConsole.Text)) Clipboard.SetText(txtConsole.Text); };

            btnClearLog = new Button
            {
                Text = "Clear",
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                Size = new Size(50, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(splitContainer.Panel2.Width - 55, 5),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClearLog.Click += (s, e) => txtConsole.Clear();

            consoleHeader.Controls.Add(consoleTitle);
            consoleHeader.Controls.Add(lblExitCode);
            consoleHeader.Controls.Add(btnCopyLog);
            consoleHeader.Controls.Add(btnClearLog);

            txtConsole = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(9, 13, 22),
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Consolas", 9.5f, FontStyle.Regular),
                ReadOnly = true,
                BorderStyle = BorderStyle.None
            };

            splitContainer.Panel2.Controls.Add(txtConsole);
            splitContainer.Panel2.Controls.Add(consoleHeader);

            this.Controls.Add(splitContainer);
            this.Controls.Add(quickActionsPanel);
            this.Controls.Add(dashboardGroup);
            this.Controls.Add(headerPanel);

            OnTabChanged();
        }

        private void SetupToolTips()
        {
            toolTip.SetToolTip(txtTarget, "Path to an offline Windows installation (e.g. D:\\Windows). Leave blank for the current OS.");
            toolTip.SetToolTip(chkAuditmode, "Enable WinRE in audit mode for deployment scenarios.");
            toolTip.SetToolTip(txtOsGuid, "Optional OS GUID when enabling WinRE on a specific installation.");
            toolTip.SetToolTip(txtReason, "Reason string recorded in the event log when booting to WinRE.");
            toolTip.SetToolTip(txtPath, "Path to Winre.wim or recovery folder.");
            toolTip.SetToolTip(numIndex, "WIM image index (usually 1).");
            toolTip.SetToolTip(numBootRank, "Boot priority rank — higher values appear first in the boot menu.");
            toolTip.SetToolTip(txtCustomArgs, "Enter reagentc switches directly, e.g. /info /target C:\\Windows");
            toolTip.SetToolTip(btnExecute, "Run the command shown in the preview (Ctrl+Enter)");
            toolTip.SetToolTip(btnRefreshStatus, "Refresh WinRE status from reagentc /info");
            toolTip.SetToolTip(btnElevate, "Relaunch this application with Administrator privileges");
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                ExecuteCurrentCommand();
            }
        }

        private static readonly string[] TabDescriptions = {
            "Displays Windows RE status and configuration parameters.",
            "Enables Windows Recovery Environment and updates the boot configuration.",
            "Disables Windows Recovery Environment and unmounts the recovery image.",
            "Configures the system to boot into WinRE on the next restart.",
            "Points WinRE to a custom Winre.wim boot image at the specified path.",
            "Configures the OS recovery image used for push-button reset.",
            "Sets the boot priority rank for Windows RE in the boot menu.",
            "Migrates the Windows RE configuration to a new target folder.",
            "Execute arbitrary reagentc switches and flags directly."
        };

        private void CheckAdminStatus()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            if (isAdmin)
            {
                adminStatusBadge.Text = "✓ Administrator Privileges";
                adminStatusBadge.BackColor = Color.FromArgb(16, 185, 129);
                adminStatusBadge.ForeColor = Color.White;
                btnElevate.Visible = false;
            }
            else
            {
                adminStatusBadge.Text = "⚠ Standard User (Requires Admin)";
                adminStatusBadge.BackColor = Color.FromArgb(245, 158, 11);
                adminStatusBadge.ForeColor = Color.FromArgb(15, 23, 42);
                btnElevate.Visible = true;
                LogConsole("NOTICE: ReAgentC operations require Administrator rights. Click 'Run as Admin' above.", Color.Yellow);
            }
        }

        private void ElevateApp()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Elevation request cancelled or failed: " + ex.Message, "Elevation Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnTabChanged()
        {
            int idx = tabControl.SelectedIndex;

            if (idx >= 0 && idx < TabDescriptions.Length)
                lblTabDescription.Text = TabDescriptions[idx];

            lblTarget.Visible = (idx != 8);
            txtTarget.Visible = (idx != 8);

            bool showEnable = (idx == 1);
            chkAuditmode.Visible = showEnable;
            lblOsGuid.Visible = showEnable;
            txtOsGuid.Visible = showEnable;

            bool showBootToRe = (idx == 3);
            lblReason.Visible = showBootToRe;
            txtReason.Visible = showBootToRe;

            bool showPath = (idx == 4 || idx == 5 || idx == 7);
            lblPath.Visible = showPath;
            txtPath.Visible = showPath;
            btnBrowsePath.Visible = showPath;

            bool showIndex = (idx == 4 || idx == 5);
            lblIndex.Visible = showIndex;
            numIndex.Visible = showIndex;

            bool showBootRank = (idx == 6);
            lblBootRank.Visible = showBootRank;
            numBootRank.Visible = showBootRank;

            bool showCustom = (idx == 8);
            lblCustom.Visible = showCustom;
            txtCustomArgs.Visible = showCustom;

            UpdateCommandPreview();
        }

        private void UpdateCommandPreview()
        {
            string args = GetCommandArgs();
            lblPreviewText.Text = "reagentc.exe " + args;
        }

        private string GetCommandArgs()
        {
            int idx = tabControl.SelectedIndex;
            string target = txtTarget.Text.Trim();
            string targetArg = !string.IsNullOrEmpty(target) ? " /target \"" + target + "\"" : "";

            switch (idx)
            {
                case 0: // Info
                    return "/info" + targetArg;
                case 1: // Enable
                    string enableCmd = "/enable";
                    if (chkAuditmode.Checked) enableCmd += " /auditmode";
                    if (!string.IsNullOrEmpty(txtOsGuid.Text.Trim())) enableCmd += " /osguid " + txtOsGuid.Text.Trim();
                    return enableCmd + targetArg;
                case 2: // Disable
                    return "/disable";
                case 3: // Boot to RE
                    string bootCmd = "/boottore";
                    if (!string.IsNullOrEmpty(txtReason.Text.Trim())) bootCmd += " /reason \"" + txtReason.Text.Trim() + "\"";
                    return bootCmd;
                case 4: // Set RE Image
                    string setReCmd = "/setreimage";
                    if (!string.IsNullOrEmpty(txtPath.Text.Trim())) setReCmd += " /path \"" + txtPath.Text.Trim() + "\"";
                    setReCmd += " /index " + numIndex.Value;
                    return setReCmd + targetArg;
                case 5: // Set OS Image
                    string setOsCmd = "/setosimage";
                    if (!string.IsNullOrEmpty(txtPath.Text.Trim())) setOsCmd += " /path \"" + txtPath.Text.Trim() + "\"";
                    setOsCmd += " /index " + numIndex.Value;
                    return setOsCmd + targetArg;
                case 6: // Boot Rank
                    return "/setbootrank /bootrank " + numBootRank.Value + targetArg;
                case 7: // Migrate
                    string migCmd = "/migrateto";
                    if (!string.IsNullOrEmpty(txtPath.Text.Trim())) migCmd += " /path \"" + txtPath.Text.Trim() + "\"";
                    return migCmd + targetArg;
                case 8: // Custom
                    return txtCustomArgs.Text.Trim();
                default:
                    return "/info";
            }
        }

        private void BrowseFolder()
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void FetchWinReStatus()
        {
            if (!isAdmin)
            {
                lblStateVal.Text = "Requires Administrator";
                lblStateVal.ForeColor = Color.FromArgb(245, 158, 11);
                return;
            }

            lblStateVal.Text = "Refreshing...";
            string exe = GetReagentcExecutable();
            RunProcess(exe, "/info", (code, stdout, stderr) =>
            {
                string combined = !string.IsNullOrEmpty(stdout) ? stdout : stderr;
                if (!string.IsNullOrEmpty(combined))
                {
                    Match mStatus = Regex.Match(combined, @"Windows RE status:\s*(.+)", RegexOptions.IgnoreCase);
                    Match mLocation = Regex.Match(combined, @"Windows RE location:\s*(.+)", RegexOptions.IgnoreCase);
                    Match mBcd = Regex.Match(combined, @"Boot Configuration Data \(BCD\) identifier:\s*(.+)", RegexOptions.IgnoreCase);
                    Match mCustom = Regex.Match(combined, @"Custom image location:\s*(.+)", RegexOptions.IgnoreCase);

                    string status = mStatus.Success ? mStatus.Groups[1].Value.Trim() : (code == 0 ? "Enabled" : "Disabled / Error");
                    lblStateVal.Text = status;
                    lblStateVal.ForeColor = status.Equals("Enabled", StringComparison.OrdinalIgnoreCase) ? Color.FromArgb(16, 185, 129) : Color.FromArgb(244, 63, 94);

                    lblLocationVal.Text = mLocation.Success ? mLocation.Groups[1].Value.Trim() : "N/A";
                    lblBcdVal.Text = mBcd.Success ? mBcd.Groups[1].Value.Trim() : "N/A";
                    lblCustomImgVal.Text = mCustom.Success ? mCustom.Groups[1].Value.Trim() : "N/A";
                }
            });
        }

        private void ExecuteCurrentCommand()
        {
            if (!isAdmin)
            {
                DialogResult elevateChoice = MessageBox.Show(
                    "REAGENTC.EXE commands require Administrator privileges.\n\nWould you like to relaunch the application as Administrator now?",
                    "Administrator Privileges Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (elevateChoice == DialogResult.Yes)
                {
                    ElevateApp();
                }
                return;
            }

            int idx = tabControl.SelectedIndex;
            if (idx == 2 || idx == 3) // Disable or BootToRE
            {
                DialogResult res = MessageBox.Show(
                    "You are about to execute a system recovery command:\n\n" + lblPreviewText.Text + "\n\nDo you want to proceed?",
                    "Confirm ReAgentC Operation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (res != DialogResult.Yes) return;
            }

            string exe = GetReagentcExecutable();
            string args = GetCommandArgs();
            LogConsole("\n> " + exe + " " + args, Color.FromArgb(56, 189, 248));

            RunProcess(exe, args, (code, stdout, stderr) =>
            {
                lblExitCode.Text = "Exit Code: " + code;
                lblExitCode.ForeColor = (code == 0) ? Color.FromArgb(16, 185, 129) : Color.FromArgb(244, 63, 94);

                if (!string.IsNullOrEmpty(stdout)) LogConsole(stdout, Color.White);
                if (!string.IsNullOrEmpty(stderr)) LogConsole(stderr, Color.FromArgb(248, 113, 113));

                if (code == 0)
                {
                    LogConsole("✓ Command completed successfully.", Color.FromArgb(74, 222, 128));
                    FetchWinReStatus();
                }
                else if (code == 5)
                {
                    LogConsole("✖ Error 5: Access Denied. Please run application as Administrator.", Color.FromArgb(251, 113, 133));
                }
                else
                {
                    LogConsole("✖ Operation failed with exit code " + code, Color.FromArgb(251, 113, 133));
                }
            });
        }

        private void RunProcess(string exe, string args, Action<int, string, string> callback)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.Default,
                    StandardErrorEncoding = Encoding.Default
                };

                using (Process proc = Process.Start(psi))
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();
                    callback(proc.ExitCode, stdout, stderr);
                }
            }
            catch (Exception ex)
            {
                callback(-1, "", ex.Message);
            }
        }

        private void LogConsole(string text, Color color)
        {
            txtConsole.SelectionStart = txtConsole.TextLength;
            txtConsole.SelectionLength = 0;
            txtConsole.SelectionColor = color;
            txtConsole.AppendText(text + "\n");
            txtConsole.ScrollToCaret();
        }
    }
}
