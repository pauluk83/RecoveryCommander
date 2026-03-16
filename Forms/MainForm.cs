// MainForm.cs - Primary UI form for Recovery Commander
// Implements the main application window with module display and navigation

using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using RecoveryCommander.UI;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using static RecoveryCommander.UI.ProfessionalDesignSystem;

namespace RecoveryCommander.Forms
{
    public partial class MainForm : Form, IDialogService
    {
        // UI Components
        public MenuStrip mainMenu = null!;
        private Panel contentPanel = null!;
        public StatusStrip statusStrip = null!;
        private ToolStripStatusLabel statusLabel = null!;
        public SplitContainer mainSplitContainer = null!;
        public FlowLayoutPanel moduleButtonsPanel = null!;
        private FlowLayoutPanel modulesPanel = null!;
        public Panel moduleDisplayPanel = null!;
        private TableLayoutPanel moduleDisplayLayout = null!;
        private Label welcomeLabel = null!;
        private Panel heroPanel = null!;
        private Panel moduleContentPanel = null!;
        private Label heroDetailLabel = null!;
        private Panel scrollPanel = null!;
        public Theme.RoundedProgressBar progressBar = null!;
        private Label progressReadoutLabel = null!;
        private Panel outputPanel = null!;
        public Theme.RoundedRichTextBox outputBox = null!;
        private ToolStrip? outputToolbar;
        private ToolStripButton? copyOutputButton;
        private ToolStripButton? saveOutputButton;
        private ToolStripButton? clearOutputButton;
        private ToolStripDropDownButton? outputFilterButton;
        private ToolStripButton? autoScrollToggleButton;
        private Label? outputHeaderLabel;
        private bool isAutoScrollEnabled = true;
        private readonly List<OutputEntry> outputHistory = new();
        private readonly Dictionary<OutputFilter, ToolStripMenuItem> outputFilterMenuItems = new();
        private OutputFilter currentOutputFilter = OutputFilter.All;
        private Panel outputScrollPanel = null!;
        private ModernButton cancelButton = null!;
        private Panel progressPanel = null!;
        private Action<Theme.ThemeMode>? themeChangedHandler;
        private Action<Theme.ThemePreferences>? themePreferencesChangedHandler;
        
        // PowerShell shell integration
        private Process? shellProcess;
        private StreamWriter? shellInput;
        private TextBox? shellInputField;
        private bool shellMode = false;
        
        // Module management
        private List<IRecoveryModule> loadedModules = new List<IRecoveryModule>();
        
        // Cancellation support
        private CancellationTokenSource? cancellationTokenSource;
        private bool isOperationRunning = false;
        private Panel? busyOverlay;
        private Label? busyOverlayLabel;
        
        // Multi-selection support for module actions
        private HashSet<ModuleAction> selectedActions = new HashSet<ModuleAction>();
        private Panel? currentModulePanel = null;
        private IRecoveryModule? currentModule = null;
        
        // Background Job Management (Multi-threading support)
        private class BackgroundJob
        {
            public IRecoveryModule Module { get; set; } = null!;
            public ModuleAction Action { get; set; } = null!;
            public CancellationTokenSource Cts { get; set; } = null!;
            public ProgressReport? LastReport { get; set; }
            public List<OutputEntry> OutputHistory { get; set; } = new();
            public Task? ExecutionTask { get; set; }
            public DateTime StartTime { get; set; }
        }
        private readonly Dictionary<IRecoveryModule, BackgroundJob> activeJobs = new();
        
        // Progress reporting
        private DateTime operationStartTime;
        // Modern async loop replaces uiRefreshTimer
        private ProgressReport? lastProgressReport;
        
        // Enhanced systems
        private EnhancedProgressSystem? enhancedProgressSystem;
        
        public MainForm()
        {
            // Set window properties with better initial sizing
            this.Text = "Recovery Commander";
            this.WindowState = FormWindowState.Maximized; // Launch maximized for better experience
            this.MinimumSize = new Size(1024, 768);
            this.Size = new Size(1500, 950);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Shield;

            // Enable DPI-aware scaling for smoother UI on high-DPI displays
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);

            // Initialize components
            InitializeComponent();

            // Apply Fluent Design after handle is created
            this.Load += MainForm_Load;

            // Initialize UI (ensure menu is populated)
            InitializeUI();

            // Load modules
            LoadModules();

            // Async UI loop handles refresh without timers
            
            // Initialize responsive design
            InitializeResponsiveDesign();
            
            // Initialize enhanced systems
            InitializeEnhancedSystems();
            themeChangedHandler = _ => ApplyOutputTheme();
            themePreferencesChangedHandler = _ => ApplyOutputTheme();
            Theme.OnThemeChanged += themeChangedHandler;
            Theme.OnThemePreferencesChanged += themePreferencesChangedHandler;
            
            // Apply theme and set up resize handling
            ApplyTheme();

            // Make sure UI elements are visible
            this.mainMenu.Visible = true;
            this.statusStrip.Visible = true;

            // Resize handlers are set up in InitializeResponsiveDesign
            // Initial layout adjustment
            MainForm_Resize(this, EventArgs.Empty);
        }

        public void MainForm_Load(object? sender, EventArgs e)
        {
            // Form closing - cleanup shell
            this.FormClosing += (s, args) => {
                CleanupShell();
            };
            
            // Apply consolidated Windows 11 theme
            try
            {
                // Load the saved theme preference or default to Dark
                var savedTheme = Theme.LoadThemePreference();
                Theme.SetTheme(savedTheme);
                
                // Apply the theme to the form
                Theme.ApplyFormStyle(this);
                Theme.ApplyTheme(this, true);
                Theme.ApplyMicaEffect(this);
                
                // Set up theme listening for system theme changes
                Theme.ListenForSystemThemeChanges(this);
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error applying consolidated theme: {ex.Message}");
                
                // Fallback to basic appearance
                this.BackColor = Theme.Colors.Background;
                this.ForeColor = Theme.Colors.Text;
            }
        }

                
        private void SetWindowRoundedCorners()
        {
            try
            {
                // This would require additional Windows API calls for true rounded corners
                // For now, we'll rely on the Mica effect to provide the modern appearance
            }
            catch
            {
                // Ignore if not supported
            }
        }
        
        private void InitializeComponent()
        {
            InitializeMainPanels();
            InitializeHeroPanel();
            InitializeProgressAndOutput();
            InitializeMainFormLayout();
        }

        private void InitializeMainPanels()
        {
            this.mainMenu = new MenuStrip();
            this.contentPanel = new Panel();
            this.statusStrip = new StatusStrip();
            this.statusLabel = new ToolStripStatusLabel();
            this.mainSplitContainer = new SplitContainer();
            this.moduleButtonsPanel = new FlowLayoutPanel();
            this.moduleDisplayPanel = new Panel();
            this.modulesPanel = new FlowLayoutPanel();
            this.scrollPanel = new Panel();

            this.MainMenuStrip = mainMenu;
            this.statusStrip.Items.Add(this.statusLabel);
            this.statusLabel.Text = "Ready";
            
            // Apply professional status strip styling
            ProfessionalDesignSystem.ApplyProfessionalStatusStripStyle(this.statusStrip);

            this.mainSplitContainer.Dock = DockStyle.Fill;
            this.mainSplitContainer.FixedPanel = FixedPanel.None;
            this.mainSplitContainer.Panel1MinSize = 220;
            this.mainSplitContainer.SplitterDistance = 250;
            this.mainSplitContainer.BorderStyle = BorderStyle.None;
            this.mainSplitContainer.IsSplitterFixed = false;

            this.moduleButtonsPanel.Dock = DockStyle.Fill;
            this.moduleButtonsPanel.FlowDirection = FlowDirection.TopDown;
            this.moduleButtonsPanel.WrapContents = false;
            this.moduleButtonsPanel.AutoScroll = true;
            this.moduleButtonsPanel.Padding = ProfessionalDesignSystem.Spacing.Spacious;

            this.moduleDisplayPanel.Dock = DockStyle.Fill;
            this.moduleDisplayPanel.Padding = new Padding(0);
            this.moduleDisplayPanel.BackColor = Color.Transparent;

            busyOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(140, 10, 10, 25),
                Visible = false
            };

            busyOverlayLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.Typography.Title,
                ForeColor = Color.White,
                Text = "Working..."
            };

            busyOverlay.Controls.Add(busyOverlayLabel);

            this.moduleDisplayLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                RowStyles = { new RowStyle(SizeType.Absolute, 220f), new RowStyle(SizeType.Percent, 100f) },
                BackColor = Color.Transparent
            };

            this.moduleContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = Color.Transparent,
                AutoScroll = true
            };
        }

        private void InitializeHeroPanel()
        {
            this.heroDetailLabel = new Label
            {
                Text = "Select a module from the navigation list to reveal tools.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Font = Theme.Typography.Subtitle,
                ForeColor = Color.FromArgb(220, Theme.Colors.Text),
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };

            this.heroPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 200,
                Margin = new Padding(0, 0, 0, ProfessionalDesignSystem.Spacing.MD),
                Padding = ProfessionalDesignSystem.Spacing.Spacious,
                BackColor = Color.Transparent
            };

            // Add subtle background gradient for better visual balance
            heroPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                var rect = heroPanel.ClientRectangle;
                using (var brush = new LinearGradientBrush(
                    rect,
                    Color.FromArgb(20, Theme.Colors.Surface),
                    Color.Transparent,
                    90f))
                {
                    g.FillRectangle(brush, rect);
                }
                
                // Add subtle top border
                using (var pen = new Pen(Color.FromArgb(40, Theme.Colors.Border), 1))
                {
                    g.DrawLine(pen, 0, 0, rect.Width, 0);
                }
            };

            this.welcomeLabel = new Label();
            this.welcomeLabel.Text = "Recovery Commander";
            this.welcomeLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.welcomeLabel.Dock = DockStyle.Top;
            this.welcomeLabel.Height = 52;
            this.welcomeLabel.Font = Theme.Typography.Display;
            this.welcomeLabel.ForeColor = Theme.Colors.Text;
            this.welcomeLabel.BackColor = Color.Transparent;

            var heroDivider = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 1, 
                Margin = new Padding(0, ProfessionalDesignSystem.Spacing.MD, 0, ProfessionalDesignSystem.Spacing.MD), 
                BackColor = Color.Transparent
            };
            
            // Draw professional divider
            heroDivider.Paint += (s, e) =>
            {
                ProfessionalDesignSystem.DrawSubtleDivider(e.Graphics, heroDivider.ClientRectangle);
            };

            this.heroPanel.Controls.Add(this.heroDetailLabel);
            this.heroPanel.Controls.Add(heroDivider);
            this.heroPanel.Controls.Add(this.welcomeLabel);
        }

        private Label? progressEtaLabel;
        private Label? progressStepLabel;
        private FlowLayoutPanel? stepIndicatorsPanel;
        private List<Panel> stepIndicatorPanels = new List<Panel>();
        private int currentStep = 0;
        private int totalSteps = 0;
        
        private void InitializeProgressAndOutput()
        {
            this.progressPanel = new Panel();
            this.progressBar = new Theme.RoundedProgressBar();
            this.outputPanel = new Panel();
            this.outputBox = new Theme.RoundedRichTextBox();
            this.cancelButton = new ModernButton();
            this.outputScrollPanel = new Panel(); // Legacy

            this.progressPanel.Height = 100;
            this.progressPanel.Dock = DockStyle.Top;
            this.progressPanel.Visible = true;
            this.progressPanel.Padding = ProfessionalDesignSystem.Spacing.Standard;

            // Create progress info panel
            var progressInfoPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 24,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 6)
            };
            
            this.progressReadoutLabel = new Label
            {
                Text = "Idle",
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = Theme.Typography.BodyStrong,
                ForeColor = Theme.Colors.Text,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 12, 0)
            };
            
            this.progressEtaLabel = new Label
            {
                Text = "",
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = Theme.Typography.Caption,
                ForeColor = Color.FromArgb(180, Theme.Colors.Text),
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 12, 0)
            };
            
            this.progressStepLabel = new Label
            {
                Text = "",
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = Theme.Typography.Caption,
                ForeColor = Color.FromArgb(180, Theme.Colors.Text),
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 0)
            };
            
            progressInfoPanel.Controls.Add(progressStepLabel);
            progressInfoPanel.Controls.Add(progressEtaLabel);
            progressInfoPanel.Controls.Add(progressReadoutLabel);

            // Step indicators panel
            this.stepIndicatorsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 28,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = false,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0),
                BackColor = Color.Transparent
            };

            // Container for progress bar and cancel button
            var progressContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 75,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(0)
            };

            this.cancelButton.Text = "Cancel";
            this.cancelButton.Width = 80;
            this.cancelButton.Dock = DockStyle.Right;
            this.cancelButton.Visible = false;
            this.cancelButton.Click += CancelButton_Click;
            this.cancelButton.ButtonStyle = Theme.ButtonStyle.FuturisticGhost;
            this.cancelButton.CornerRadius = 10;
            this.cancelButton.TextAlign = ContentAlignment.MiddleCenter;

            this.progressBar.Height = 24; // Match container roughly or let it fill
            this.progressBar.Value = 0;
            this.progressBar.Maximum = 100;
            this.progressBar.Dock = DockStyle.Fill;
            this.progressBar.Margin = new Padding(0);

            // Spacer between progress bar and cancel button
            var spacer = new Panel { Width = 10, Dock = DockStyle.Right, BackColor = Color.Transparent };

            progressContainer.Controls.Add(this.progressBar);
            progressContainer.Controls.Add(spacer);
            progressContainer.Controls.Add(this.cancelButton);
            
            this.progressPanel.Controls.Add(this.stepIndicatorsPanel);
            this.progressPanel.Controls.Add(progressContainer);
            this.progressPanel.Controls.Add(progressInfoPanel);

            this.outputPanel.Height = 220;
            this.outputPanel.Dock = DockStyle.Fill;
            this.outputPanel.Padding = new Padding(0);
            this.outputPanel.Visible = false;
            
            // Apply modern rounded corners styling
            Theme.ApplyModernOutputPanelStyle(this.outputPanel);

            this.outputBox.ReadOnly = true;
            this.outputBox.Font = new Font("Consolas", 9.5f);
            // Ensure text visibility with explicit colors
            this.outputBox.BackColor = Theme.Colors.Surface;
            this.outputBox.ForeColor = Color.White;
            
            // Enhance the rounded corners and spacing
            this.outputBox.Margin = new Padding(0);

            var outputShell = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), Margin = new Padding(0) };
            var outputHeaderPanel = CreateOutputHeaderPanel();
            
            // Create shell input field
            shellInputField = new TextBox 
            { 
                Dock = DockStyle.Bottom, 
                Height = 30,
                BackColor = Theme.Colors.Surface, 
                ForeColor = Color.White, 
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9.5f),
                PlaceholderText = "Type commands here (press Enter to execute, 'shell' to toggle shell mode)..."
            };
            
            shellInputField.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    ExecuteShellCommand(shellInputField.Text);
                    shellInputField.Clear();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            
            // The RoundedRichTextBox handles its own rounded corners
            this.outputBox.Dock = DockStyle.Fill;
            
            outputShell.Controls.Add(this.outputBox);
            outputShell.Controls.Add(shellInputField);
            this.outputPanel.Controls.Add(outputShell);
        }

        private void InitializeMainFormLayout()
        {
            var moduleDisplayShell = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(ProfessionalDesignSystem.Spacing.XL, 0, ProfessionalDesignSystem.Spacing.XL, 0),
                BackColor = Color.Transparent
            };
            this.moduleDisplayLayout.Controls.Add(this.heroPanel, 0, 0);
            this.moduleDisplayLayout.Controls.Add(this.moduleContentPanel, 0, 1);
            moduleDisplayShell.Controls.Add(this.moduleDisplayLayout);
            this.moduleDisplayPanel.Controls.Clear();
            this.moduleDisplayPanel.Padding = new Padding(0);
            this.moduleDisplayPanel.Controls.Add(busyOverlay);
            this.moduleDisplayPanel.Controls.Add(moduleDisplayShell);

            Panel bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            var paddingExtra = this.progressPanel.Padding.Top + this.progressPanel.Padding.Bottom;
            bottomPanel.Height = this.progressPanel.Height + paddingExtra + 6;
            bottomPanel.Padding = new Padding(ProfessionalDesignSystem.Spacing.XL, 0, ProfessionalDesignSystem.Spacing.XL, ProfessionalDesignSystem.Spacing.LG);

            // Add outputPanel first so it has the lowest dock priority
            bottomPanel.Controls.Add(this.outputPanel);
            // Add progressPanel last so it has highest dock priority and isn't hidden by Fill
            bottomPanel.Controls.Add(this.progressPanel);

            this.mainSplitContainer.Panel1.Controls.Add(this.moduleButtonsPanel);
            this.mainSplitContainer.Panel2.Controls.Add(this.moduleDisplayPanel);

            this.contentPanel.Controls.Add(this.mainSplitContainer);
            this.contentPanel.Controls.Add(bottomPanel);

            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.mainMenu);
            this.Controls.Add(this.statusStrip);

            this.contentPanel.Dock = DockStyle.Fill;
            this.statusStrip.Dock = DockStyle.Bottom;
            this.mainMenu.Dock = DockStyle.Top;

            modulesPanel = moduleButtonsPanel;
            scrollPanel = moduleDisplayPanel;
            outputScrollPanel = outputPanel;
        }
        private void InitializeUI()
        {
            // Apply modern styling
            this.BackColor = SystemColors.Window;
            this.ForeColor = SystemColors.WindowText;
            
            // Set up menu from MenuManager
            foreach (var item in MenuManager.BuildMenuItems(this))
            {
                this.mainMenu.Items.Add(item);
            }
            
            // Apply professional styling to menu
            mainMenu.BackColor = Theme.Colors.Surface;
            mainMenu.ForeColor = Theme.Colors.Text;
            mainMenu.Height = 32;
            mainMenu.Padding = new Padding(ProfessionalDesignSystem.Spacing.SM, 0, ProfessionalDesignSystem.Spacing.SM, 0);
            
            // Add subtle border to menu
            mainMenu.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using (var pen = new Pen(Color.FromArgb(30, Theme.Colors.Border), 1))
                {
                    g.DrawLine(pen, 0, mainMenu.Height - 1, mainMenu.Width, mainMenu.Height - 1);
                }
            };
            
            statusStrip.BackColor = Theme.Colors.Surface;
            statusStrip.ForeColor = Theme.Colors.Text;

            // Apply theme to rest of controls
            ApplyTheme();
        }
        
        private void ApplyTheme()
        {
            Theme.ApplyTheme(this, true);
            ApplyOutputTheme();

            // Customizations for ModernProfessional theme
            if (Theme.IsModernTheme)
            {
                heroPanel.BackColor = Theme.Colors.Surface;
                welcomeLabel.ForeColor = Theme.Colors.Text;
                heroDetailLabel.ForeColor = Theme.Colors.SubtleText;
            }
        }
        
        // Method to show output panel and add text to output box
        public enum OutputLevel
        {
            Info,
            Success,
            Warning,
            Error
        }

        private enum OutputFilter
        {
            All,
            Info,
            Success,
            Warning,
            Error
        }

        private readonly record struct OutputEntry(DateTime Timestamp, string Text, OutputLevel Level);

        private static string GetOutputPrefix(OutputLevel level)
        {
            return level switch
            {
                OutputLevel.Info => "[INFO] ",
                OutputLevel.Success => "[SUCCESS] ",
                OutputLevel.Warning => "[WARNING] ",
                OutputLevel.Error => "[ERROR] ",
                _ => "[INFO] "
            };
        }

        private Color GetOutputColor(OutputLevel level) => level switch
        {
            OutputLevel.Success => Color.FromArgb(125, 211, 161),
            OutputLevel.Warning => Color.FromArgb(255, 193, 107),
            OutputLevel.Error => Color.FromArgb(255, 120, 120),
            _ => Theme.Colors.Text
        };

        private Panel CreateOutputHeaderPanel()
        {
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(0, 0, 0, 6)
            };

            outputHeaderLabel = new Label
            {
                Text = "",
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 0,
                Visible = false
            };

            outputToolbar = new ToolStrip
            {
                Dock = DockStyle.Right,
                LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow,
                GripStyle = ToolStripGripStyle.Hidden,
                Renderer = new ToolStripProfessionalRenderer(),
                AutoSize = true,
                Height = 38,
                Padding = new Padding(0, 2, 0, 0),
                Stretch = false,
                BackColor = Color.Transparent
            };

            copyOutputButton = new ToolStripButton("Copy") { ToolTipText = "Copy visible output", DisplayStyle = ToolStripItemDisplayStyle.Text };
            copyOutputButton.Click += (s, e) => CopyOutputToClipboard();

            saveOutputButton = new ToolStripButton("Save") { ToolTipText = "Save output to file", DisplayStyle = ToolStripItemDisplayStyle.Text };
            saveOutputButton.Click += (s, e) => SaveOutputToFile();

            clearOutputButton = new ToolStripButton("Clear") { ToolTipText = "Clear command feed", DisplayStyle = ToolStripItemDisplayStyle.Text };
            clearOutputButton.Click += (s, e) => ClearOutputHistory();

            outputFilterButton = new ToolStripDropDownButton("Filter");
            foreach (OutputFilter filter in Enum.GetValues(typeof(OutputFilter)))
            {
                var item = new ToolStripMenuItem(filter.ToString())
                {
                    Checked = filter == OutputFilter.All,
                    Tag = filter
                };
                item.Click += HandleFilterMenuItemClick;
                outputFilterMenuItems[filter] = item;
                outputFilterButton.DropDownItems.Add(item);
            }

            autoScrollToggleButton = new ToolStripButton("Auto Scroll")
            {
                CheckOnClick = true,
                Checked = isAutoScrollEnabled,
                ToolTipText = "Toggle automatic scroll to latest output",
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            autoScrollToggleButton.CheckedChanged += (s, e) => isAutoScrollEnabled = autoScrollToggleButton!.Checked;

            outputToolbar.Items.Add(copyOutputButton);
            outputToolbar.Items.Add(saveOutputButton);
            outputToolbar.Items.Add(clearOutputButton);
            outputToolbar.Items.Add(new ToolStripSeparator());
            outputToolbar.Items.Add(outputFilterButton);
            outputToolbar.Items.Add(new ToolStripSeparator());
            outputToolbar.Items.Add(autoScrollToggleButton);

            headerPanel.Controls.Add(outputHeaderLabel);

            ApplyOutputTheme();

            return headerPanel;
        }

        private void HandleFilterMenuItemClick(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not OutputFilter filter)
            {
                return;
            }

            currentOutputFilter = filter;
            foreach (var kvp in outputFilterMenuItems)
            {
                kvp.Value.Checked = kvp.Key == currentOutputFilter;
            }

            RefreshOutputView();
            UpdateOutputHeaderStats();
        }

        private void ApplyOutputTheme()
        {
            if (outputPanel != null)
            {
                outputPanel.BackColor = Theme.Colors.Background;
            }

            if (outputHeaderLabel != null)
            {
                outputHeaderLabel.ForeColor = Theme.Colors.Text;
            }

            if (outputToolbar != null)
            {
                outputToolbar.BackColor = Color.Transparent;
                foreach (ToolStripItem item in outputToolbar.Items)
                {
                    item.ForeColor = Theme.Colors.Text;
                }
            }
        }

        private void ExecuteShellCommand(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return;
            
            ShowOutput($"> {cmd}", OutputLevel.Info);
            
            if (cmd.ToLower() == "shell")
            {
                ToggleShellMode();
                return;
            }
            
            if (cmd.ToLower() == "cls" || cmd.ToLower() == "clear")
            {
                outputBox.Clear();
                return;
            }
            
            if (!shellMode)
            {
                ShowOutput("Shell mode is disabled. Type 'shell' to enable PowerShell integration.", OutputLevel.Warning);
                return;
            }
            
            try
            {
                shellInput?.WriteLine(cmd);
            }
            catch (Exception ex)
            {
                ShowOutput($"Error executing command: {ex.Message}", OutputLevel.Error);
            }
        }
        
        private void ToggleShellMode()
        {
            shellMode = !shellMode;
            
            if (shellMode)
            {
                InitializeShell();
                ShowOutput("PowerShell shell mode enabled. You can now execute PowerShell commands.", OutputLevel.Success);
                shellInputField!.PlaceholderText = "PowerShell shell active - type commands here...";
            }
            else
            {
                CleanupShell();
                ShowOutput("PowerShell shell mode disabled.", OutputLevel.Info);
                shellInputField!.PlaceholderText = "Type 'shell' to enable PowerShell integration...";
            }
        }
        
        private void InitializeShell()
        {
            try
            {
                if (shellProcess != null && !shellProcess.HasExited) return;
                
                shellProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -NoLogo",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = AppContext.BaseDirectory
                    }
                };

                shellProcess.OutputDataReceived += (s, e) => {
                    if (e.Data != null) ShowOutput(e.Data, OutputLevel.Info);
                };
                shellProcess.ErrorDataReceived += (s, e) => {
                    if (e.Data != null) ShowOutput(e.Data, OutputLevel.Error);
                };

                shellProcess.Start();
                shellProcess.BeginOutputReadLine();
                shellProcess.BeginErrorReadLine();
                shellInput = shellProcess.StandardInput;
                
                ShowOutput("Recovery Commander Admin Shell v1.0", OutputLevel.Info);
                ShowOutput("Type 'help' for PowerShell help.", OutputLevel.Info);
                ShowOutput("", OutputLevel.Info);
            }
            catch (Exception ex)
            {
                ShowOutput($"Failed to start shell: {ex.Message}", OutputLevel.Error);
                shellMode = false;
            }
        }
        
        private void CleanupShell()
        {
            try 
            { 
                shellProcess?.Kill(); 
                shellProcess?.Dispose();
            } 
            catch { }
            
            shellProcess = null;
            shellInput = null;
        }

        public void ShowOutput(string text, OutputLevel level = OutputLevel.Info, bool clear = false, IRecoveryModule? sourceModule = null)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowOutput(text, level, clear, sourceModule)));
                return;
            }

            // Handle background job output correctly
            if (sourceModule != null && sourceModule != currentModule)
            {
                var backgroundEntry = new OutputEntry(DateTime.Now, text, level);
                outputHistory.Add(backgroundEntry);
                if (activeJobs.TryGetValue(sourceModule, out var job))
                {
                    job.OutputHistory.Add(backgroundEntry);
                }
                UpdateOutputHeaderStats();
                return;
            }

            if (!outputPanel.Visible)
            {
                outputPanel.Visible = true;
                // Resize bottom panel to accommodate both progress and output
                Panel? bottomPanel = outputPanel.Parent as Panel;
                if (bottomPanel != null)
                {
                    bottomPanel.Height = 235; // 35 for progress + 200 for output
                }
            }

            // Fail-Safe: Aggressively check for any percentage in the output
            if (isOperationRunning && !string.IsNullOrWhiteSpace(text))
            {
                // Clean the text: remove nulls and normalize spaces for matching
                string cleanText = text.Replace("\0", "").Replace(" ", "");
                
                // Try to match percentage in cleaned text
                var match = System.Text.RegularExpressions.Regex.Match(cleanText, @"(\d+)(?:\.\d+)?%", System.Text.RegularExpressions.RegexOptions.RightToLeft);
                if (match.Success)
                {
                    var percentStr = match.Groups[1].Value;
                    if (double.TryParse(percentStr, out var dPercent))
                    {
                        var percent = (int)Math.Round(dPercent);
                        if (percent > 0 && percent <= 100)
                        {
                            this.BeginInvoke(new Action(() => {
                                if (percent > progressBar.Value || (progressBar.Value <= 2 && percent > 0))
                                {
                                    var report = new ProgressReport(percent, $"Progress: {percent}%", text.Trim());
                                    lastProgressReport = report;
                                    UpdateProgressUI(report);
                                }
                            }));
                        }
                    }
                }
            }

            if (clear)
            {
                outputHistory.Clear();
                outputBox.Clear();
                UpdateOutputHeaderStats();
            }

            var entry = new OutputEntry(DateTime.Now, text, level);
            outputHistory.Add(entry);

            UpdateOutputHeaderStats();

            if (IsEntryVisible(entry))
            {
                AppendOutputEntry(entry);
            }
        }

        private void RefreshOutputView()
        {
            if (outputBox == null) return;

            outputBox.SuspendLayout();
            outputBox.Clear();

            foreach (var entry in outputHistory)
            {
                if (IsEntryVisible(entry))
                {
                    AppendOutputEntry(entry, false);
                }
            }

            if (isAutoScrollEnabled)
            {
                outputBox.ScrollToCaret();
            }

            outputBox.ResumeLayout();
            UpdateOutputHeaderStats();
        }

        private bool IsEntryVisible(OutputEntry entry)
        {
            return currentOutputFilter switch
            {
                OutputFilter.Info => entry.Level == OutputLevel.Info,
                OutputFilter.Success => entry.Level == OutputLevel.Success,
                OutputFilter.Warning => entry.Level == OutputLevel.Warning,
                OutputFilter.Error => entry.Level == OutputLevel.Error,
                _ => true
            };
        }

        private void AppendOutputEntry(OutputEntry entry, bool ensureLayout = true)
        {
            if (outputBox == null) return;

            if (ensureLayout)
            {
                outputBox.SuspendLayout();
            }

            // Get color based on output level
            Color textColor = entry.Level switch
            {
                OutputLevel.Success => Color.FromArgb(100, 255, 100),  // Light green
                OutputLevel.Warning => Color.FromArgb(255, 200, 100),  // Light orange
                OutputLevel.Error => Color.FromArgb(255, 100, 100),    // Light red
                _ => Color.White  // White for Info
            };

            // Format the entry with timestamp and level
            string formattedText = $"[{entry.Timestamp:HH:mm:ss}] {entry.Level.ToString().ToUpperInvariant()}: {entry.Text}";
            
            // Append the text with the appropriate color
            outputBox.AppendText(formattedText + Environment.NewLine, textColor);

            // Auto-scroll to bottom if enabled
            if (isAutoScrollEnabled)
            {
                outputBox.ScrollToCaret();
            }

            if (ensureLayout)
            {
                outputBox.ResumeLayout();
            }
        }

        private void UpdateOutputHeaderStats()
        {
            if (outputHeaderLabel == null) return;

            int total = outputHistory.Count;
            int visible = outputHistory.Count(entry => IsEntryVisible(entry));
            outputHeaderLabel.Text = total == visible
                ? $"Command Feed · {total} line{(total == 1 ? string.Empty : "s")}"
                : $"Command Feed · {visible}/{total} visible";
        }

        private void CopyOutputToClipboard()
        {
            if (outputBox == null || string.IsNullOrEmpty(outputBox.Text)) return;

            try
            {
                Clipboard.SetText(outputBox.Text);
                ShowToast("Output copied", "The visible command feed was copied to the clipboard.", NotificationType.Success);
            }
            catch (Exception ex)
            {
                ShowToast("Copy failed", ex.Message, NotificationType.Error);
            }
        }

        private void SaveOutputToFile()
        {
            if (!outputHistory.Any())
            {
                ShowToast("Nothing to save", "There is no output available to save.", NotificationType.Warning);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = $"recovery-output-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var builder = new StringBuilder();
                    foreach (var entry in outputHistory)
                    {
                        builder.AppendLine($"[{entry.Timestamp:HH:mm:ss}] {entry.Level.ToString().ToUpperInvariant()}: {entry.Text}");
                    }

                    File.WriteAllText(dialog.FileName, builder.ToString());
                    ShowToast("Output saved", $"Saved to {dialog.FileName}", NotificationType.Success);
                }
                catch (Exception ex)
                {
                    ShowToast("Save failed", ex.Message, NotificationType.Error);
                }
            }
        }

        private void ClearOutputHistory()
        {
            outputHistory.Clear();
            outputBox.Clear();
            UpdateOutputHeaderStats();
            ShowToast("Command feed cleared", "All output entries were removed.", NotificationType.Info);
        }

        private void ShowToast(string title, string message, NotificationType type = NotificationType.Info)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => ShowToast(title, message, type)));
                return;
            }

            var useToast = enhancedProgressSystem != null && enhancedProgressSystem.NotificationsEnabled;
            if (useToast)
            {
                enhancedProgressSystem!.ShowNotification(title, message, type);
            }
            else
            {
                var level = type switch
                {
                    NotificationType.Success => OutputLevel.Success,
                    NotificationType.Warning => OutputLevel.Warning,
                    NotificationType.Error => OutputLevel.Error,
                    _ => OutputLevel.Info
                };
                ShowOutput($"{title}: {message}", level);
            }
        }

        private void UpdateModuleButtonRunningState(IRecoveryModule module, bool isRunning)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateModuleButtonRunningState(module, isRunning)));
                return;
            }

            foreach (Control control in moduleButtonsPanel.Controls)
            {
                if (control is ModernButton btn && btn.Tag == module)
                {
                    // Add/Remove a visual indicator or change style
                    if (isRunning)
                    {
                        btn.Text = "⚡ " + module.Name + "\nv" + module.Version;
                        btn.GlowColor = Theme.Colors.Primary;
                    }
                    else
                    {
                        btn.Text = module.Name + "\nv" + module.Version;
                        btn.GlowColor = Color.Transparent;
                    }
                    btn.Invalidate();
                    break;
                }
            }
        }

        
        // Method to show/hide and update progress bar
        public void UpdateProgress(int value, string? statusText = null, bool visible = true)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateProgress(value, statusText, visible)));
                return;
            }

            progressBar.Visible = visible;
            progressPanel.Visible = visible;

            if (visible)
            {
                progressBar.Value = Math.Max(progressBar.Minimum, Math.Min(value, progressBar.Maximum));

                if (statusText != null)
                    statusLabel.Text = statusText;

                var status = statusText ?? $"Progress: {value}%";
                UpdateStatus(status);
                progressBar.StatusText = $"{value}% · {(statusText ?? "Working")}";
            }
            else
            {
                progressBar.StatusText = "Idle";
            }
        }

        // Updates the status strip text in a thread-safe manner
        private void UpdateStatus(string? text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatus(text)));
                return;
            }

            if (this.statusLabel != null && text != null)
            {
                this.statusLabel.Text = text;
            }
        }

        private Panel CreateInfoChip(string label, out Label valueLabel)
        {
            var chip = new Panel
            {
                Width = 140,
                Height = 56,
                Margin = new Padding(0, 0, ProfessionalDesignSystem.Spacing.SM, ProfessionalDesignSystem.Spacing.SM),
                Padding = new Padding(ProfessionalDesignSystem.Spacing.MD, ProfessionalDesignSystem.Spacing.SM + 2, ProfessionalDesignSystem.Spacing.MD, ProfessionalDesignSystem.Spacing.SM),
                BackColor = Theme.IsModernTheme ? Theme.Colors.SurfaceVariant : Color.FromArgb(25, Theme.Colors.Surface)
            };

            // Add professional card style with shadow
            chip.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var rect = chip.ClientRectangle;
                rect.Width--;
                rect.Height--;
                
                ProfessionalDesignSystem.DrawProfessionalCard(g, rect, 8);
            };

            var labelControl = new Label
            {
                Text = label.ToUpperInvariant(),
                Dock = DockStyle.Top,
                Height = 16,
                Font = Theme.Typography.Caption,
                ForeColor = Color.FromArgb(160, Theme.Colors.Text),
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 2)
            };

            valueLabel = new Label
            {
                Text = "-",
                Dock = DockStyle.Fill,
                Font = Theme.Typography.BodyStrong,
                ForeColor = Theme.Colors.Text,
                BackColor = Color.Transparent
            };

            chip.Controls.Add(valueLabel);
            chip.Controls.Add(labelControl);
            return chip;
        }

        private Control CreateInfoChip(string label, string value)
        {
            var chip = new Panel
            {
                Width = 180,
                Height = 48,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(10, 6, 10, 6),
                BackColor = Theme.IsModernTheme ? Theme.Colors.Surface : Color.FromArgb(40, 255, 255, 255)
            };

            var labelControl = new Label
            {
                Text = label.ToUpperInvariant(),
                Dock = DockStyle.Top,
                Height = 16,
                Font = Theme.Typography.Caption,
                ForeColor = Color.FromArgb(170, Theme.Colors.Text),
                BackColor = Color.Transparent
            };

            var valueControl = new Label
            {
                Text = value,
                Dock = DockStyle.Fill,
                Font = Theme.Typography.BodyStrong,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            chip.Controls.Add(valueControl);
            chip.Controls.Add(labelControl);
            chip.Paint += (s, e) => Theme.DrawFuturisticEdge(e.Graphics, chip.ClientRectangle, 1f);
            return chip;
        }

        private void UpdateHeroMetrics(string status)
        {
            // Hero metrics removed - keeping method for compatibility
            _ = status;
        }
        
        // Color table for Windows 11 style ToolStrip
        private class Win11ColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => Theme.Colors.Background;
        public override Color ToolStripGradientMiddle => Theme.Colors.Background;
        public override Color ToolStripGradientEnd => Theme.Colors.Background;
        public override Color ToolStripBorder => Theme.Colors.Border;
        public override Color ButtonSelectedBorder => Theme.Colors.Primary;
        public override Color ButtonSelectedHighlight => Theme.Colors.Primary;
        public override Color ButtonSelectedGradientBegin => Color.FromArgb(30, Theme.Colors.Primary);
        public override Color ButtonSelectedGradientMiddle => Color.FromArgb(30, Theme.Colors.Primary);
        public override Color ButtonSelectedGradientEnd => Color.FromArgb(30, Theme.Colors.Primary);
        }
        
        private void LoadModules()
        {
            // This would load recovery modules
            UpdateStatus("Loading modules...");

            try
            {
                // Clear existing controls and data
                moduleButtonsPanel.Controls.Clear();
                loadedModules.Clear();

                // Use the ModuleLoader to load modules
                var modules = ModuleLoader.LoadModules(logger: message => ShowOutput(message));
                
                // Sort modules: Diagnostics ALWAYS at top
                loadedModules = modules.OrderBy(m => {
                    var name = m.Name.Trim();
                    if (string.Equals(name, "Diagnostics", StringComparison.OrdinalIgnoreCase)) return 0;
                    return 1;
                }).ThenBy(m => m.Name).ToList();

                // Clear and add in correct order (FlowLayoutPanel index 0 is TOP)
                moduleButtonsPanel.Controls.Clear();
                ShowOutput($"[DEBUG] Building module buttons for {loadedModules.Count} modules (Diagnostics should be @ index 0)...");
                
                for (int i = 0; i < loadedModules.Count; i++)
                {
                    var module = loadedModules[i];
                    var moduleButton = CreateModuleButton(module);
                    moduleButtonsPanel.Controls.Add(moduleButton);
                    moduleButtonsPanel.Controls.SetChildIndex(moduleButton, i);
                    ShowOutput($"[DEBUG] Added button {i}: '{module.Name}'");
                }

                // Ensure panels and sidebar are visible
                mainSplitContainer.Panel1Collapsed = false;
                mainSplitContainer.Panel2Collapsed = false;

                // Select Diagnostics by default
                if (loadedModules.Count > 0)
                {
                    var diagModule = loadedModules.FirstOrDefault(m => string.Equals(m.Name.Trim(), "Diagnostics", StringComparison.OrdinalIgnoreCase));
                    if (diagModule != null)
                    {
                        DisplayModule(diagModule);
                        foreach (Control ctrl in moduleButtonsPanel.Controls)
                        {
                            if (ctrl is ModernButton btn && btn.Tag == diagModule)
                            {
                                UpdateModuleButtonStyles(moduleButtonsPanel, btn);
                                break;
                            }
                        }
                    }
                    else
                    {
                        DisplayModule(loadedModules[0]);
                    }
                }
                UpdateStatus($"Loaded {loadedModules.Count} modules");
                ShowOutput($"Loaded {loadedModules.Count} module(s).");
                ShowToast("Modules loaded", $"{loadedModules.Count} module(s) ready.", NotificationType.Success);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading modules: {ex.Message}");
                ShowOutput($"Module loading error: {ex.Message}");
                ShowOutput($"Stack trace: {ex.StackTrace}");
                ShowToast("Module load failed", ex.Message, NotificationType.Error);
            }
        }
#pragma warning restore CS8600
        
        private ModernButton CreateModuleButton(IRecoveryModule module)
        {
            var moduleButton = new ModernButton
            {
                Text = $"{module.Name}\nv{module.Version}",
                Height = 72,
                Margin = new Padding(0, 0, 0, 10),
                Tag = module,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.Typography.BodyStrong,
                ButtonStyle = Theme.ButtonStyle.Secondary,
                CornerRadius = 10,
                AutoSize = false
            };

            // Calculate optimal width
            var textSize = TextRenderer.MeasureText(moduleButton.Text, moduleButton.Font, Size.Empty, TextFormatFlags.WordBreak);
            var optimalWidth = Math.Max(textSize.Width + 60, 200);
            moduleButton.Width = optimalWidth;

            moduleButton.Click += (s, e) => {
                foreach (ModernButton btn in moduleButtonsPanel.Controls)
                {
                    btn.Width = moduleButtonsPanel.ClientSize.Width - 20;
                }
                if (s is ModernButton button && button.Tag is IRecoveryModule selectedModule)
                {
                    UpdateModuleButtonStyles(moduleButtonsPanel, button);
                    DisplayModule(selectedModule);
                }
            };

            return moduleButton;
        }

        private void UpdateModuleButtonStyles(FlowLayoutPanel moduleButtonsPanel, ModernButton selectedButton)
        {
            foreach (ModernButton btn in moduleButtonsPanel.Controls)
            {
                btn.ButtonStyle = btn == selectedButton ? Theme.ButtonStyle.Primary : Theme.ButtonStyle.Secondary;
            }
        }
        
        // TreeView removed; selection handled via ListBox
        
        private string GetModuleIcon(string moduleName)
        {
            return moduleName switch
            {
                "Diagnostics" => "🛠️",
                "SFC" => "🛡️",
                "DISM" => "📦",
                "Reagentc" => "🔄",
                "Malware Removal" => "🧹",
                "System Prep" => "⚙️",
                "Utilities" => "🔧",
                _ => "🧩"
            };
        }

        public void DisplayMainDashboard()
        {
            moduleContentPanel.Controls.Clear();
            welcomeLabel.Text = "Recovery Dashboard";
            heroDetailLabel.Text = "Welcome to Recovery Commander. Select a module below to begin system maintenance or use the diagnostics suite for a full check.";
            currentModule = null;
            selectedActions.Clear();

            var dashboardPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20),
                BackColor = Color.Transparent
            };

            foreach (var module in loadedModules)
            {
                var moduleCard = CreateModuleDashboardCard(module);
                dashboardPanel.Controls.Add(moduleCard);
            }

            moduleContentPanel.Controls.Add(dashboardPanel);
            UpdateStatus("Ready - Dashboard View");
        }

        private Panel CreateModuleDashboardCard(IRecoveryModule module)
        {
            var card = new Panel
            {
                Width = 280,
                Height = 160,
                Margin = new Padding(15),
                BackColor = Theme.Colors.SurfaceVariant,
                Cursor = Cursors.Hand
            };

            card.Paint += (s, e) =>
            {
                ProfessionalDesignSystem.DrawProfessionalCard(e.Graphics, card.ClientRectangle, 12);
            };

            var iconLabel = new Label
            {
                Text = GetModuleIcon(module.Name),
                Font = new Font("Segoe UI Emoji", 24f),
                ForeColor = Theme.Colors.Primary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Left,
                Width = 60,
                BackColor = Color.Transparent
            };

            var nameLabel = new Label
            {
                Text = module.Name,
                Font = Theme.Typography.Header,
                ForeColor = Theme.Colors.Primary,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Height = 30,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.Transparent
            };

            var headerPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.Transparent };
            headerPanel.Controls.Add(nameLabel);
            headerPanel.Controls.Add(iconLabel);

            var descLabel = new Label
            {
                Text = module.Description,
                Font = Theme.Typography.Body,
                ForeColor = Theme.Colors.Text,
                TextAlign = ContentAlignment.TopLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 5, 10, 5),
                BackColor = Color.Transparent
            };

            var actionCount = module.Actions?.Count() ?? 0;
            var footerLabel = new Label
            {
                Text = $"{actionCount} Tools Available",
                Font = Theme.Typography.Caption,
                ForeColor = Theme.Colors.SubtleText,
                TextAlign = ContentAlignment.BottomRight,
                Dock = DockStyle.Bottom,
                Height = 25,
                Padding = new Padding(0, 0, 10, 5),
                BackColor = Color.Transparent
            };

            card.Controls.Add(descLabel);
            card.Controls.Add(headerPanel);
            card.Controls.Add(footerLabel);

            void LaunchModule()
            {
                // Find the corresponding button on the left to keep UI in sync
                foreach (Control ctrl in moduleButtonsPanel.Controls)
                {
                    if (ctrl is ModernButton btn && btn.Tag == module)
                    {
                        btn.PerformClick();
                        break;
                    }
                }
            }

            card.Click += (s, e) => LaunchModule();
            nameLabel.Click += (s, e) => LaunchModule();
            descLabel.Click += (s, e) => LaunchModule();

            return card;
        }

        private void DisplayModule(IRecoveryModule module)
        {
            moduleContentPanel.Controls.Clear();
            
            // Special handling for Dashboard/Diagnostics module
            if (module.Name == "Diagnostics")
            {
                welcomeLabel.Text = "Diagnostic Dashboard";
                welcomeLabel.Height = 35; // Tighter header
                heroDetailLabel.Text = "Technician toolkit for comprehensive system health analysis and troubleshooting.";
                heroDetailLabel.Height = 25; // Tighter subheader
            }
            else
            {
                welcomeLabel.Text = module.Name;
                heroDetailLabel.Text = string.IsNullOrWhiteSpace(module.Description) ? "Select a module to see its tools." : module.Description;
            }

            currentModule = module;
            selectedActions.Clear();
            UpdateStatus($"Selected: {module.Name}");

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0), BackColor = Color.Transparent };
            currentModulePanel = mainPanel;

            // Set initial opacity for fade-in
            mainPanel.Visible = false;

            var actionContainer = BuildActionContainer(module);
            mainPanel.Controls.Add(actionContainer);

            moduleContentPanel.Controls.Add(mainPanel);
            
            // ... (rest of the existing job restoration logic)
            if (activeJobs.TryGetValue(module, out var job))
            {
                // Re-attach UI to existing job
                operationStartTime = job.StartTime;
                cancellationTokenSource = job.Cts;
                isOperationRunning = true;
                
                progressPanel.Visible = true;
                outputPanel.Visible = true;
                cancelButton.Visible = true;
                cancelButton.Enabled = true;
                cancelButton.Text = "Cancel";
                
                // Restore output
                outputBox.Clear();
                foreach (var entry in job.OutputHistory)
                {
                    var color = GetOutputColor(entry.Level);
                    outputBox.AppendText($"{entry.Timestamp:HH:mm:ss} {GetOutputPrefix(entry.Level)}{entry.Text}\r\n", color);
                }
                
                if (job.LastReport != null)
                {
                    UpdateProgressUI(job.LastReport);
                }
                else
                {
                    progressBar.IsIndeterminate = true;
                    progressBar.StatusText = "Working in background...";
                }
            }
            else
            {
                progressPanel.Visible = false;
                outputPanel.Visible = false;
                progressBar.Value = 0;
                progressBar.IsIndeterminate = false;
                isOperationRunning = false;
                cancellationTokenSource = null;
            }

            mainPanel.FadeIn(300);
        }

        private Panel BuildActionContainer(IRecoveryModule module)
        {
            var actionContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, AutoSize = false, Padding = new Padding(0) };
            
            // Special Dashboard Layout for Diagnostics
            if (module.Name == "Diagnostics")
            {
                var mainLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    BackColor = Color.Transparent,
                    Padding = new Padding(15)
                };
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f)); // Actions
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f)); // Info pane

                // Left side: Actions Grid (Optimized for 3 columns)
                var actionsPanelFlow = new FlowLayoutPanel 
                { 
                    Dock = DockStyle.Fill,
                    Padding = new Padding(0),
                    Margin = new Padding(0),
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true,
                    BackColor = Color.Transparent,
                    AutoScroll = false
                };
                
                // Use a standard panel instead of DarkFlowLayoutPanel for more predictable layout in Diagnostics
                foreach (var action in module.Actions)
                {
                    if (action.IsHeader) continue;
                    var tile = CreateActionTile(module, action, null);
                    tile.Width = 280; // Reliable 3-column width for 70% of 1500px
                    tile.Height = 48; // Shorter to save vertical space
                    actionsPanelFlow.Controls.Add(tile);
                }
                
                mainLayout.Controls.Add(actionsPanelFlow, 0, 0);

                // Right side: "What this toolkit checks" info pane
                var infoPane = new Panel 
                { 
                    Dock = DockStyle.Fill, 
                    Padding = new Padding(15), 
                    BackColor = Color.FromArgb(20, Theme.Colors.Primary), 
                    Margin = new Padding(10, 0, 0, 0) 
                };
                infoPane.Paint += (s, e) => {
                    ProfessionalDesignSystem.DrawProfessionalCard(e.Graphics, infoPane.ClientRectangle, 12);
                };

                var infoTitle = new Label {
                    Text = "WHAT THIS TOOLKIT CHECKS",
                    Dock = DockStyle.Top,
                    Height = 35,
                    Font = Theme.Typography.Header,
                    ForeColor = Theme.Colors.Primary,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(0, 0, 0, 10)
                };

                var checklist = new FlowLayoutPanel 
                { 
                    Dock = DockStyle.Fill, 
                    FlowDirection = FlowDirection.TopDown, 
                    AutoScroll = false,
                    WrapContents = false,
                    BackColor = Color.Transparent
                };
                
                string[] checks = { 
                    "✓ System hardware info", 
                    "✓ CPU & Processor details", 
                    "✓ RAM modules & speed", 
                    "✓ Disk health (SMART)", 
                    "✓ Free storage space", 
                    "✓ Network configuration", 
                    "✓ Active connections", 
                    "✓ Startup programs", 
                    "✓ Running processes", 
                    "✓ System integrity" 
                };

                foreach (var check in checks)
                {
                    var checkLabel = new Label { 
                        Text = check, 
                        AutoSize = true, 
                        Font = new Font(Theme.Typography.DefaultFontFamily, 10f), 
                        ForeColor = Theme.Colors.Text,
                        Margin = new Padding(0, 3, 0, 3)
                    };
                    checklist.Controls.Add(checkLabel);
                }

                infoPane.Controls.Add(checklist);
                infoPane.Controls.Add(infoTitle);
                mainLayout.Controls.Add(infoPane, 1, 0);

                actionContainer.Controls.Add(mainLayout);
                return actionContainer;
            }

            var actionsPanelFlowDefault = new Theme.DarkFlowLayoutPanel(enableScroll: false) { Dock = DockStyle.Fill };
            actionsPanelFlowDefault.FlowDirection = FlowDirection.LeftToRight;
            actionsPanelFlowDefault.WrapContents = true;
            actionsPanelFlowDefault.Padding = new Padding(8);
            actionsPanelFlowDefault.Margin = new Padding(0);
            Theme.ApplyPanelStyle(actionsPanelFlowDefault);

            ModernButton? runSelectedButton = null;
            if (module.Name == "System Prep")
            {
                runSelectedButton = new ModernButton
                {
                    Text = "Run Selected Actions",
                    Dock = DockStyle.Bottom,
                    Height = 40,
                    Margin = new Padding(10),
                    Visible = false,
                    ButtonStyle = Theme.ButtonStyle.Primary,
                    Font = Theme.Typography.BodyStrong,
                    CornerRadius = 8
                };
                runSelectedButton.Click += (s, e) => RunSelectedActions();
            }

            BuildAndAddActionTiles(module, actionsPanelFlowDefault, runSelectedButton);

            if (runSelectedButton != null)
            {
                actionContainer.Controls.Add(runSelectedButton);
            }
            actionContainer.Controls.Add(actionsPanelFlowDefault);

            return actionContainer;
        }

        private void BuildAndAddActionTiles(IRecoveryModule module, FlowLayoutPanel panel, ModernButton? runSelectedButton)
        {
            var actions = module.Actions?.ToList() ?? new List<ModuleAction>();
            if (actions.Count == 0)
            {
                var emptyState = ProfessionalDesignSystem.CreateEmptyState(
                    "No Actions Available",
                    "This module doesn't have any actions configured.",
                    "⚙️"
                );
                panel.Controls.Add(emptyState);
                return;
            }

            foreach (ModuleAction action in actions)
            {
                if (action.IsHeader) continue;
                var tile = CreateActionTile(module, action, runSelectedButton);
                panel.Controls.Add(tile);
            }
        }

        private Panel CreateActionTile(IRecoveryModule module, ModuleAction action, ModernButton? runSelectedButton)
        {
            bool isSystemPrepModule = module.Name == "System Prep";
            
            var tile = new Panel
            {
                Width = 340,
                Height = 64,
                Margin = new Padding(ProfessionalDesignSystem.Spacing.SM),
                Padding = ProfessionalDesignSystem.Spacing.Standard,
                BackColor = Theme.IsModernTheme ? Theme.Colors.SurfaceVariant : Color.FromArgb(30, 30, 40),
                Tag = action
            };
            
            // Add professional card shadow effect
            tile.Paint += (s, e) =>
            {
                var rect = tile.ClientRectangle;
                rect.Width--;
                rect.Height--;
                ProfessionalDesignSystem.DrawProfessionalCard(e.Graphics, rect, 10);
            };

            var iconLabel = new Label
            {
                Text = module.Name == "Diagnostics" ? "✓" : "⚡",
                Dock = DockStyle.Left,
                Width = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Emoji", 14f),
                ForeColor = action.Highlight ? Theme.Colors.Success : Theme.Colors.Accent,
                BackColor = Color.Transparent
            };

            var title = new Label
            {
                Text = action.DisplayName ?? action.Name,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                ForeColor = Theme.Colors.Text,
                Font = Theme.Typography.BodyStrong,
                AutoSize = false,
                Padding = new Padding(8, 0, 0, 0)
            };

            tile.Controls.Add(title);
            tile.Controls.Add(iconLabel);

            // Paint is now handled by the professional card style above

            Color Lighten(Color c, int amount)
            {
                int r = Math.Min(255, c.R + amount);
                int g = Math.Min(255, c.G + amount);
                int b = Math.Min(255, c.B + amount);
                return Color.FromArgb(c.A, r, g, b);
            }

            var normalColor = Theme.IsModernTheme ? Theme.Colors.SurfaceVariant : Color.FromArgb(30, 30, 40);
            var hoverColor = Theme.IsModernTheme ? Theme.Colors.Surface : Color.FromArgb(40, 40, 50);
            var selectedColor = Theme.IsModernTheme ? Lighten(Theme.Colors.Surface, 10) : Color.FromArgb(50, 50, 60);
            var selectedHoverColor = Theme.IsModernTheme ? Lighten(Theme.Colors.Surface, 16) : Color.FromArgb(60, 60, 70);

            tile.MouseEnter += (s, e) =>
            {
                bool isSelected = selectedActions.Contains(action);
                tile.BackColor = isSelected ? selectedHoverColor : hoverColor;
                tile.Cursor = Cursors.Hand;
                
                // Subtle lift animation on hover
                Theme.Animator.Animate(tile, "Top", tile.Top - 2, 150, Theme.Animator.EasingFunction.CubicOut);
            };

            tile.MouseLeave += (s, e) =>
            {
                bool isSelected = selectedActions.Contains(action);
                tile.BackColor = isSelected ? selectedColor : normalColor;
                tile.Cursor = Cursors.Default;
                
                // Return to original position
                Theme.Animator.Animate(tile, "Top", tile.Top + 2, 200, Theme.Animator.EasingFunction.CubicOut);
            };

            void HandleTileInteraction()
            {
                if (isSystemPrepModule)
                {
                    if (runSelectedButton != null) ToggleActionSelection(action, tile, runSelectedButton);
                }
                else if (!isOperationRunning)
                {
                    _ = ExecuteActionSafelyAsync(module, action);
                }
            }

            tile.Click += (s, e) => HandleTileInteraction();
            title.Click += (s, e) => HandleTileInteraction();

            return tile;
        }
        
        private string GetActionDescription(string actionName)
        {
            // Add meaningful descriptions based on action names
            return actionName.ToLower() switch
            {
                "scan" => "Performs comprehensive system scan and analysis",
                "repair" => "Repairs detected issues and system corruption",
                "restore" => "Restores system to previous working state",
                "backup" => "Creates system backup and restore points",
                "diagnose" => "Runs advanced diagnostic procedures",
                "clean" => "Cleans temporary files and system cache",
                "optimize" => "Optimizes system performance and settings",
                "verify" => "Verifies system integrity and file consistency",
                "checkhealth" => "Quickly checks Windows image for corruption and health issues",
                "scanhealth" => "Thoroughly scans Windows image for component store corruption",
                "restorehealth" => "Automatically repairs corrupted Windows system files",
                "startcomponentcleanup" => "Cleans up superseded components to free disk space",
                _ => "Performs system maintenance and recovery operations"
            };
        }

        private Control BuildModuleOverviewPanel(IRecoveryModule module)
        {
            var overviewShell = new Panel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(24),
                Margin = new Padding(0, 0, 0, 10),
                Height = 160,
                BackColor = Color.Transparent
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Clear();
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));

            var left = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var right = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                WrapContents = false
            };

            var title = new Label
            {
                Text = module.Name,
                Dock = DockStyle.Top,
                Height = 32,
                Font = Theme.Typography.Title,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            var description = new Label
            {
                Text = string.IsNullOrWhiteSpace(module.Description) ? "Neon-tuned recovery utility" : module.Description,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(215, 230, 255),
                Font = Theme.Typography.Subtitle,
                BackColor = Color.Transparent
            };

            left.Controls.Add(description);
            left.Controls.Add(title);

            right.Controls.Add(CreateInfoChip("Version", module.Version));
            right.Controls.Add(CreateInfoChip("Build", string.IsNullOrWhiteSpace(module.BuildInfo) ? "Internal" : module.BuildInfo));
            right.Controls.Add(CreateInfoChip("Actions", (module.Actions?.Count() ?? 0).ToString()));

            layout.Controls.Add(left, 0, 0);
            layout.Controls.Add(right, 1, 0);
            overviewShell.Controls.Add(layout);
            return overviewShell;
        }
        
        private void DisplayAction(ModuleAction action)
        {
            try
            {
                UpdateStatus($"Executing {action.Name}...");

                // Show progress bar and output panel
                progressPanel.Visible = true;
                progressBar.Value = 0;
                statusLabel.Text = $"Starting {action.Name}...";

                outputPanel.Visible = true;
                outputBox.Clear();
                outputBox.AppendText($"Executing action: {action.DisplayName ?? action.Name}\r\n", Color.White);
                outputBox.AppendText($"Description: {GetActionDescription(action.Name)}\r\n", Color.White);
                outputBox.AppendText($"Started at: {DateTime.Now}\r\n\r\n", Color.White);

                // Resize bottom panel to show both progress and output
                Panel? bottomPanel = outputPanel.Parent as Panel;
                if (bottomPanel != null)
                {
                    bottomPanel.Height = 280; // 45 for progress + 235 for output
                }

                // Simulate action execution
                SimulateActionExecution(action.Name);

                UpdateStatus($"Executed {action.Name} successfully");
                outputBox.AppendText($"\r\nAction completed successfully at {DateTime.Now}\r\n", Color.LimeGreen);
                statusLabel.Text = "Completed";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing action: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Error executing action");
                outputBox.AppendText($"Error: {ex.Message}\r\n", Color.Red);
            }
        }

        private void ToggleActionSelection(ModuleAction action, Panel tile, Button runSelectedButton)
        {
            if (selectedActions.Contains(action))
            {
                selectedActions.Remove(action);
                tile.BackColor = Theme.IsModernTheme ? Theme.Colors.SurfaceVariant : Color.FromArgb(30, 30, 40);
            }
            else
            {
                selectedActions.Add(action);
                var baseColor = Theme.IsModernTheme ? Theme.Colors.Surface : Color.FromArgb(40, 40, 50);
                int r = Math.Min(255, baseColor.R + 10);
                int g = Math.Min(255, baseColor.G + 10);
                int b = Math.Min(255, baseColor.B + 10);
                tile.BackColor = Color.FromArgb(baseColor.A, r, g, b);
            }
            
            // Show/hide run selected button (only if it exists)
            if (runSelectedButton != null)
            {
                runSelectedButton.Visible = selectedActions.Count > 0;
                runSelectedButton.Text = $"Run Selected ({selectedActions.Count})";
            }
        }

        private void RunSelectedActions()
        {
            if (currentModule == null || selectedActions.Count == 0) return;
            
            // Use the safe async execution method
            _ = RunSelectedActionsSafelyAsync();
        }

        private async Task RunSelectedActionsSafelyAsync()
        {
            if (currentModule == null || selectedActions.Count == 0) return;
            
            // Prevent multiple simultaneous operations
            if (isOperationRunning)
            {
                MessageBox.Show("Another operation is already running. Please wait for it to complete.", 
                              "Operation in Progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            // Disable the run button during execution
            var runSelectedButton = currentModulePanel?.Controls.OfType<Button>().FirstOrDefault(b => b.Text.Contains("Run Selected"));
            if (runSelectedButton != null)
            {
                runSelectedButton.Enabled = false;
            }
            
            try
            {
                // Set visual feedback
                this.Cursor = Cursors.WaitCursor;

                SetBusyState(true, $"Running {selectedActions.Count} action(s)...");

                // Show progress indication
                UpdateStatus($"Running {selectedActions.Count} selected actions...");
                ShowToast("Batch running", $"Executing {selectedActions.Count} action(s)...", NotificationType.Info);
                
                // Run each selected action sequentially
                foreach (var action in selectedActions.ToList())
                {
                    await RunModuleActionAsync(currentModule, action);
                }
                
                // Clear selections after successful execution
                selectedActions.Clear();
                if (runSelectedButton != null)
                {
                    runSelectedButton.Visible = false;
                }
                
                // Reset tile colors
                var actionsPanelFlow = currentModulePanel?.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
                if (actionsPanelFlow != null)
                {
                    foreach (Control control in actionsPanelFlow.Controls)
                    {
                        if (control is Panel tile)
                        {
                            tile.BackColor = Theme.IsDarkMode ? Theme.Colors.SurfaceVariant : Theme.Colors.Surface;
                        }
                    }
                }
                
                UpdateStatus($"Completed {selectedActions.Count} actions successfully");
                ShowToast("Actions completed", "All selected actions finished.", NotificationType.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running selected actions: {ex.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"Error: {ex.Message}");
                ShowToast("Batch failed", ex.Message, NotificationType.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                if (runSelectedButton != null)
                {
                    runSelectedButton.Enabled = true;
                }
                SetBusyState(false, string.Empty);
            }
        }

        private async Task RunModuleActionAsync(IRecoveryModule module, ModuleAction action)
        {
            // Create new cancellation token source for this operation
            cancellationTokenSource = new CancellationTokenSource();
            operationStartTime = DateTime.Now;
            lastProgressReport = null; // Reset for new operation
            isOperationRunning = true;
            
            SetBusyState(true, action.DisplayName ?? action.Name);

            // Custom DirectUIProgress implementation to guarantee UI updates
            // Bypasses System.Progress<T> context capturing which can be unreliable in mixed async contexts
            var lastUpdateTime = DateTime.MinValue;
            var isFirstUpdate = true;
            
            var progressReporter = new DirectUIProgress<ProgressReport>(this, report =>
            {
                // Store in job state for multi-threaded UI persistence
                if (activeJobs.TryGetValue(module, out var job))
                {
                    job.LastReport = report;
                }

                // Only update the actual UI if the module is currently displayed
                if (currentModule != module) return;

                // Only update the cached report if it's forward progress or meaningful status change
                var currentCachedPercent = lastProgressReport?.PercentComplete ?? 0;
                if (report.PercentComplete >= currentCachedPercent || report.PercentComplete < 0)
                {
                    lastProgressReport = report;
                }
                
                var now = DateTime.Now;
                bool isCompletion = report.PercentComplete >= 100;
                bool isStarting = isFirstUpdate;
                
                // Allow update if starting, finishing, OR enough time has passed (approx 30fps)
                if (isStarting || isCompletion || (now - lastUpdateTime).TotalMilliseconds > 30)
                {
                    UpdateProgressUI(report);
                    if (isStarting) isFirstUpdate = false;
                    lastUpdateTime = now;
                }
            });

            // Start the UI refresh loop
            StartUIRefreshLoop();
            
            // Update UI to show action is running
            progressPanel.Visible = true;
            outputPanel.Visible = true;
            
            // Expand the bottom panel to fit both progress and output
            var btmPanel = progressPanel.Parent as Panel;
            if (btmPanel != null)
                btmPanel.Height = 300;
            
            progressBar.Value = 0;
            progressBar.Maximum = 100;
            statusLabel.Text = $"Running: {action.DisplayName ?? action.Name}";
            progressBar.Visible = true;
            progressBar.IsIndeterminate = true;
            progressReadoutLabel.Visible = false;
            
            // Show cancel button
            cancelButton.Visible = true;
            cancelButton.Enabled = true;
            cancelButton.Text = "Cancel";
            
            outputBox.Clear();
            outputBox.Visible = true;
            
            // Register job in tracker BEFORE starting task
            var backgroundJob = new BackgroundJob
            {
                Module = module,
                Action = action,
                Cts = cancellationTokenSource,
                StartTime = operationStartTime
            };
            activeJobs[module] = backgroundJob;
            UpdateModuleButtonRunningState(module, true);

            // Add initial timestamp to output
            ShowOutput(string.Empty);
            
            try
            {
                // All modules now support async interface - passing 'this' as IDialogService
                await module.ExecuteActionAsync(action.Name, progressReporter, (output) => ShowOutput(output, sourceModule: module), this, cancellationTokenSource.Token);
                
                if (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    ShowOutput($"Completed: {action.DisplayName ?? action.Name}", OutputLevel.Success);
                    ShowOutput("Action completed successfully.", OutputLevel.Success);
                    
                    progressBar.Value = 100;
                    statusLabel.Text = "Completed";
                    progressBar.IsIndeterminate = false;
                    if (!string.Equals(module.Name, "REAgentc", StringComparison.OrdinalIgnoreCase) || !string.Equals(action.Name, "Info", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowToast("Action complete", $"{action.DisplayName ?? action.Name} finished successfully.", NotificationType.Success);
                    }
                }
                else
                {
                    ShowOutput($"Cancelled: {action.DisplayName ?? action.Name}", OutputLevel.Warning);
                    ShowOutput("Action was cancelled by user.", OutputLevel.Warning);
                    
                    statusLabel.Text = "Cancelled";
                    progressBar.IsIndeterminate = false;
                    ShowToast("Action cancelled", $"{action.DisplayName ?? action.Name} was cancelled.", NotificationType.Warning);
                }
            }
            catch (OperationCanceledException)
            {
                ShowOutput($"Cancelled: {action.DisplayName ?? action.Name}", OutputLevel.Warning);
                ShowOutput("Action was cancelled by user.", OutputLevel.Warning);
                
                statusLabel.Text = "Cancelled";
                progressBar.IsIndeterminate = false;
                ShowToast("Action cancelled", $"{action.DisplayName ?? action.Name} was cancelled.", NotificationType.Warning);
            }
            catch (Exception ex)
            {
                ShowOutput($"Error: {ex.Message}", OutputLevel.Error);
                if (ex.InnerException != null)
                {
                    ShowOutput($"Inner exception: {ex.InnerException.Message}", OutputLevel.Error);
                }
                ShowOutput($"Failed: {action.DisplayName ?? action.Name}", OutputLevel.Error);
                
                statusLabel.Text = "Error occurred";
                progressBar.IsIndeterminate = false;
                ShowToast("Action failed", ex.Message, NotificationType.Error);
            }
            finally
            {
                // Clean up job tracker
                activeJobs.Remove(module);
                UpdateModuleButtonRunningState(module, false);

                isOperationRunning = false;
                cancelButton.Visible = false;
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                
                // UI loop auto terminates when isOperationRunning is false
                
                var elapsed = DateTime.Now - operationStartTime;
                ShowOutput($"Total execution time: {FormatTimeSpan(elapsed)}", OutputLevel.Info);

                // Reset operation start time
                operationStartTime = DateTime.MinValue;
                
                // Clear progress info
                if (progressEtaLabel != null)
                    progressEtaLabel.Visible = false;
                if (progressStepLabel != null)
                    progressStepLabel.Visible = false;
                if (stepIndicatorsPanel != null)
                    stepIndicatorsPanel.Visible = false;
                
                // Clear operation info
                
                SetBusyState(false, string.Empty);
            }
        }
        
        private void UpdateProgressUI(ProgressReport report)
        {
            try
            {
                string statusText = "";
                
                // Ensure we don't jump backwards, but be permissive if we're near the start
                bool isForwardProgress = report.PercentComplete > progressBar.Value;
                bool isSameProgress = report.PercentComplete == progressBar.Value && progressBar.Value > 0;
                bool isInitialProgress = progressBar.Value <= 2 && report.PercentComplete > 0;
                
                if (report.PercentComplete >= 0 && report.PercentComplete <= 100)
                {
                    if (progressBar.IsIndeterminate) progressBar.IsIndeterminate = false;

                    if (isForwardProgress || isSameProgress || isInitialProgress || progressBar.Value == 0)
                    {
                        progressBar.Value = report.PercentComplete;
                        lastProgressReport = report;

                        var elapsed = operationStartTime == DateTime.MinValue ? TimeSpan.Zero : DateTime.Now - operationStartTime;
                        
                        // Priority 1: Details with units
                        if (!string.IsNullOrEmpty(report.Details) && 
                            (report.Details.Contains("/s") || report.Details.Contains("MB") || report.Details.Contains("KB")))
                        {
                            statusText = $"{report.PercentComplete}% - {report.Details}";
                        }
                        else
                        {
                            // Priority 2: Formal percentage and ETA
                            statusText = $"{report.PercentComplete}%";
                            if (report.PercentComplete > 0 && report.PercentComplete < 100 && elapsed.TotalSeconds > 3)
                            {
                                var rate = (double)report.PercentComplete / elapsed.TotalSeconds;
                                if (rate > 0)
                                {
                                    var remaining = (100 - report.PercentComplete) / rate;
                                    statusText += $" · ETA: {FormatTimeSpan(TimeSpan.FromSeconds(remaining))}";
                                }
                            }
                            statusText += $" · {FormatTimeSpan(elapsed)}";
                        }
                    }
                    else
                    {
                        // Stale report - check if we should still update the text/status if same
                        if (report.PercentComplete < progressBar.Value) return;
                        statusText = progressBar.StatusText; // Keep current text
                    }
                }
                else if (report.PercentComplete < 0)
                {
                    progressBar.IsIndeterminate = true;
                    statusText = !string.IsNullOrEmpty(report.Details) ? report.Details : "Working...";
                    lastProgressReport = report;
                }

                // Force a refresh of the bar's status text
                if (!string.IsNullOrEmpty(statusText))
                {
                    progressBar.StatusText = statusText;
                    
                    // Also update the external readout label if it exists
                    if (progressReadoutLabel != null)
                    {
                        progressReadoutLabel.Text = statusText;
                        progressReadoutLabel.Refresh();
                    }
                }

                // Force immediate visual update to overcome any message queue lag
                progressBar.Value = progressBar.Value; // Trigger internal setter logic
                progressBar.Invalidate();
                progressBar.Update();
                
                // If we're at a high percentage, ensure busy overlay is updated
                if (progressBar.Value > 5)
                {
                    SetBusyState(true, statusText);
                }

                // Set the text inside the progress bar
                progressBar.StatusText = statusText;
                
                // Hide external labels as requested
                if (progressReadoutLabel != null) progressReadoutLabel.Visible = false;
                if (progressEtaLabel != null) progressEtaLabel.Visible = false;

                if (!string.IsNullOrEmpty(report.StatusMessage))
                {
                    statusLabel.Text = report.StatusMessage;
                }
                
                // Update step information if available in Details
                if (!string.IsNullOrEmpty(report.Details))
                {
                    UpdateStepFromDetails(report.Details);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the UI
                System.Diagnostics.Debug.WriteLine($"Error updating progress UI: {ex.Message}");
            }
        }
        
        private string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            else if (ts.TotalMinutes >= 1)
                return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
            else
                return $"{ts.Seconds}s";
        }
        
        private void UpdateStepFromDetails(string details)
        {
            // Look for step patterns like "Step 2/5" or "Step 2 of 5"
            var stepMatch = System.Text.RegularExpressions.Regex.Match(
                details, 
                @"[Ss]tep\s+(\d+)\s*(?:of|/)\s*(\d+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (stepMatch.Success && stepMatch.Groups.Count >= 3)
            {
                if (int.TryParse(stepMatch.Groups[1].Value, out int step) &&
                    int.TryParse(stepMatch.Groups[2].Value, out int total))
                {
                    SetStepIndicators(step, total);
                }
            }
        }
        
        private void SetStepIndicators(int current, int total)
        {
            if (stepIndicatorsPanel == null) return;
            
            currentStep = current;
            totalSteps = total;
            
            if (progressStepLabel != null)
            {
                progressStepLabel.Text = $"Step {current}/{total}";
                progressStepLabel.Visible = true;
            }
            
            // Create or update step indicator dots
            stepIndicatorsPanel.Controls.Clear();
            stepIndicatorPanels.Clear();
            
            for (int i = 1; i <= total; i++)
            {
                var stepDot = new Panel
                {
                    Width = 24,
                    Height = 24,
                    Margin = new Padding(4, 0, 4, 0),
                    BackColor = i < current 
                        ? Theme.Colors.Success 
                        : i == current 
                            ? Theme.Colors.Primary 
                            : Color.FromArgb(60, Theme.Colors.Border)
                };
                
                stepDot.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    
                    var rect = stepDot.ClientRectangle;
                    rect.Width--;
                    rect.Height--;
                    
                    using (var path = Theme.GetRoundedRectPath(rect, 12))
                    {
                        using (var brush = new SolidBrush(stepDot.BackColor))
                        {
                            g.FillPath(brush, path);
                        }
                        
                        if (i == current)
                        {
                            using (var pen = new Pen(Theme.Colors.Primary, 2))
                            {
                                g.DrawPath(pen, path);
                            }
                        }
                    }
                    
                    // Draw step number
                    using (var brush = new SolidBrush(i <= current ? Color.White : Color.FromArgb(120, Theme.Colors.Text)))
                    {
                        var font = new Font(Theme.Typography.Caption.FontFamily, 8, FontStyle.Bold);
                        var text = i.ToString();
                        var textSize = g.MeasureString(text, font);
                        var textPos = new PointF(
                            (rect.Width - textSize.Width) / 2,
                            (rect.Height - textSize.Height) / 2
                        );
                        g.DrawString(text, font, brush, textPos);
                    }
                };
                
                stepIndicatorsPanel.Controls.Add(stepDot);
                stepIndicatorPanels.Add(stepDot);
            }
            
            stepIndicatorsPanel.Visible = total > 1;
        }

        private void ExecuteActionSafely(IRecoveryModule module, ModuleAction action)
        {
            // Use the async version but run it synchronously for backward compatibility
            _ = ExecuteActionSafelyAsync(module, action);
        }

        private async Task ExecuteActionSafelyAsync(IRecoveryModule module, ModuleAction action)
        {
            // Check if THIS module already has an operation running
            if (activeJobs.ContainsKey(module))
            {
                MessageBox.Show("This module is already running an operation. Please wait for it to complete or use another module.", 
                              "Module Busy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Update operation info in hero panel
                
                // Set visual feedback without disabling the entire form
                this.Cursor = Cursors.WaitCursor;

                SetBusyState(true, action.DisplayName ?? action.Name);

                // Show progress indication
                UpdateStatus($"Starting {action.DisplayName ?? action.Name}...");

                // Safety First Logic - Removed as snapshot feature is currently disabled
                // and the popup was considered redundant for modern modules.

                if (!string.Equals(module.Name, "REAgentc", StringComparison.OrdinalIgnoreCase) || !string.Equals(action.Name, "Info", StringComparison.OrdinalIgnoreCase))
                {
                    ShowToast("Action starting", action.DisplayName ?? action.Name, NotificationType.Info);
                }
                
                // Reset step indicators
                if (stepIndicatorsPanel != null)
                {
                    stepIndicatorsPanel.Visible = false;
                    stepIndicatorsPanel.Controls.Clear();
                    stepIndicatorPanels.Clear();
                }
                if (progressEtaLabel != null)
                    progressEtaLabel.Visible = false;
                if (progressStepLabel != null)
                    progressStepLabel.Visible = false;
                
                // Execute the action
                await RunModuleActionAsync(module, action);
            }
            catch (Exception ex)
            {
                // Use enhanced error handling with retry capability
                var shouldRetry = EnhancedProgressDialog.ShowErrorWithRetry(
                    $"Executing {action.DisplayName ?? action.Name}", "Error", ex);
                
                if (shouldRetry)
                {
                    // Retry the operation
                    await ExecuteActionSafelyAsync(module, action);
                }
                else
                {
                    UpdateStatus($"Error: {ex.Message}");
                    
                    // Show notification for the error
                    enhancedProgressSystem?.ShowNotification("Operation Failed", 
                        $"{action.DisplayName ?? action.Name} failed: {ex.Message}", 
                        NotificationType.Error);
                }
            }
            finally
            {
                // Re-enable UI and restore cursor
                this.Cursor = Cursors.Default;
                SetBusyState(false, string.Empty);
                if (!isOperationRunning)
                {
                    UpdateStatus("Ready");
                }
            }
        }

        private void SetBusyState(bool isBusy, string statusText)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetBusyState(isBusy, statusText)));
                return;
            }

            // moduleButtonsPanel.Enabled = !isBusy; // Keep enabled for multi-threading
            busyOverlay?.BringToFront();

            if (busyOverlay != null)
            {
                busyOverlay.Visible = isBusy;
                if (busyOverlayLabel != null)
                {
                    busyOverlayLabel.Text = string.IsNullOrWhiteSpace(statusText)
                        ? "Working..."
                        : statusText;
                }
            }
        }

        private void SetControlsEnabled(Control parent, bool enabled)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is Button || control is CheckBox || control is Panel panel && panel.Tag != null)
                {
                    control.Enabled = enabled;
                }
                
                // Recursively enable/disable child controls
                if (control.HasChildren)
                {
                    SetControlsEnabled(control, enabled);
                }
            }
        }
        public void ShowContentDialog(string content, string title)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowContentDialog(content, title)));
                return;
            }
            DialogFactory.ShowContentDialog(this, content, title);
        }

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
        
        // Cancel button and its click handler have been removed
        
        private void CancelButton_Click(object? sender, EventArgs e)
        {
            // Cancel the current operation
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                cancelButton.Enabled = false;
                cancelButton.Text = "Cancelling...";
                ShowOutput("Cancellation requested...");
            }
        }

        private void SimulateActionExecution(string actionName)
        {
            // Simulate progress updates with cancellation support
            for (int i = 0; i <= 100; i += 10)
            {
                // Check for cancellation
                if (cancellationTokenSource != null && cancellationTokenSource.Token.IsCancellationRequested)
                {
                    outputBox.AppendText("Operation cancelled by user.\r\n", Color.Yellow);
                    return;
                }
                
                progressBar.Value = i;
                statusLabel.Text = $"Executing {actionName}... {i}%";
                outputBox.AppendText($"Progress: {i}%\r\n", Color.White);
                System.Threading.Thread.Sleep(100); // Simulate work
                Application.DoEvents(); // Keep UI responsive
            }
        }
        
        private void InitializeResponsiveDesign()
        {
            // Set minimum form size
            this.MinimumSize = new Size(1024, 768);
            
            // Set up resize event handlers
            this.Resize += MainForm_Resize;
            this.ResizeEnd += MainForm_ResizeEnd;
            
            // Initialize layout containers
            InitializeLayoutContainers();
            
            // Apply initial responsive layout
            ApplyResponsiveLayout();
        }
        
        private void InitializeLayoutContainers()
        {
            // Configure split container for responsive behavior
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.FixedPanel = FixedPanel.None;
            mainSplitContainer.IsSplitterFixed = false;
            
                // Configure module buttons panel for dynamic sizing
                if (mainSplitContainer.Panel1.Controls.Count > 0 && 
                    mainSplitContainer.Panel1.Controls[0] is FlowLayoutPanel moduleButtonsPanel)
                {
                    moduleButtonsPanel.Resize += (s, e) => {
                        foreach (Control control in moduleButtonsPanel.Controls)
                        {
                            if (control is ModernButton btn)
                            {
                                btn.Width = moduleButtonsPanel.ClientSize.Width - 20;
                            }
                        }
                    };
                }
            
            // Configure action panel for responsive behavior
            moduleDisplayPanel.Dock = DockStyle.Fill;
            
            // Configure output panel
            outputPanel.Dock = DockStyle.Fill;
        }
        
        private void ApplyResponsiveLayout()
        {
            var screenSize = Screen.PrimaryScreen?.WorkingArea.Size ?? new Size(1920, 1080);
            var currentSize = this.Size;
            
            // Adjust split container ratio based on screen size - original proportions
            if (screenSize.Width >= 1920) // Large screens (1080p+)
            {
                mainSplitContainer.SplitterDistance = 300;
                mainSplitContainer.SplitterWidth = 6;
            }
            else if (screenSize.Width >= 1366) // Medium screens
            {
                mainSplitContainer.SplitterDistance = 280;
                mainSplitContainer.SplitterWidth = 5;
            }
            else // Small screens
            {
                mainSplitContainer.SplitterDistance = 250;
                mainSplitContainer.SplitterWidth = 4;
            }
            
            // Adjust module tile sizes based on available space
            UpdateModuleTileSizes();
            
            // Adjust font sizes for better readability
            UpdateFontSizes();
            
            // Adjust button sizes and spacing
            UpdateControlSizes();
        }
        
        private void UpdateModuleTileSizes()
        {
            // No longer needed since we're using a simple ListBox
            // This method is kept for compatibility but does nothing
        }
        
        private void AdjustTileControls(Panel tile)
        {
            // Find and adjust title label
            var titleLabel = tile.Controls.OfType<Label>().FirstOrDefault(l => l.Font.Size >= 10);
            if (titleLabel != null)
            {
                titleLabel.Width = tile.Width - 20;
                titleLabel.Font = new Font(titleLabel.Font.FontFamily, 
                    Math.Max(10, tile.Height / 10));
            }
            
            // Find and adjust description label
            var descLabel = tile.Controls.OfType<Label>().FirstOrDefault(l => l.Font.Size < 10);
            if (descLabel != null)
            {
                descLabel.Width = tile.Width - 20;
                descLabel.Font = new Font(descLabel.Font.FontFamily, 
                    Math.Max(8, tile.Height / 15));
            }
            
            // Find and adjust buttons
            var buttons = tile.Controls.OfType<Button>();
            foreach (var button in buttons)
            {
                button.Width = Math.Max(80, tile.Width / 3);
                button.Height = Math.Max(30, tile.Height / 6);
            }
        }
        
        private void UpdateFontSizes()
        {
            var baseFontSize = 9f;
            var screenWidth = Screen.PrimaryScreen?.WorkingArea.Width ?? 1366;
            
            if (screenWidth >= 2560) // 4K displays
                baseFontSize = 12f;
            else if (screenWidth >= 1920) // 1080p displays
                baseFontSize = 10f;
            else if (screenWidth >= 1366) // Standard displays
                baseFontSize = 9f;
            else // Small displays
                baseFontSize = 8f;
            
            // Update main form font
            this.Font = new Font(this.Font.FontFamily, baseFontSize);
            
            // Update menu font
            if (mainMenu != null)
                mainMenu.Font = new Font(mainMenu.Font.FontFamily, baseFontSize);
            
            // Update status strip font
            if (statusStrip != null)
                statusStrip.Font = new Font(statusStrip.Font.FontFamily, baseFontSize - 1);
        }
        
        private void UpdateControlSizes()
        {
            var screenScale = (Screen.PrimaryScreen?.WorkingArea.Width ?? 1920) / 1920.0; // Base on 1080p
            
            // Update button sizes
            var buttonHeight = (int)(35 * screenScale);
            var buttonWidth = (int)(120 * screenScale);
            
            // Update progress bar height
            progressBar.Height = (int)(25 * screenScale);
            
            // Update cancel button size
            cancelButton.Height = buttonHeight;
            cancelButton.Width = buttonWidth;
            
            // Update status strip height
            statusStrip.Height = (int)(25 * screenScale);
        }
        
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                return;

            if (this.Width < this.MinimumSize.Width || this.Height < this.MinimumSize.Height)
            {
                return;
            }

            // Apply responsive layout on resize
            ApplyResponsiveLayout();
            RefreshLayout();
            
            // Update module button sizes
            UpdateModuleButtonSizes();
        }
        
        private void UpdateModuleButtonSizes()
        {
            if (moduleButtonsPanel?.Controls == null) return;
            
            foreach (Control control in moduleButtonsPanel.Controls)
            {
                if (control is ModernButton moduleButton && moduleButton.Tag is IRecoveryModule)
                {
                    // Recalculate optimal width based on text content with better measurement
                    var textSize = TextRenderer.MeasureText(moduleButton.Text, moduleButton.Font, Size.Empty, TextFormatFlags.WordBreak);
                    var optimalWidth = Math.Max(textSize.Width + 60, 200);
                    
                    moduleButton.Width = optimalWidth;
                }
            }
        }
        
        private void MainForm_ResizeEnd(object? sender, EventArgs e)
        {
            ApplyResponsiveLayout();
            RefreshLayout();
            UpdateStatus($"Window resized to {this.Width}x{this.Height}");
        }

        private void RefreshLayout()
        {
            // Refresh the layout to ensure proper proportions
            this.Refresh();

            // Ensure split container maintains good proportions - smaller left pane
            if (mainSplitContainer.Width > 0)
            {
                int minPanelWidth = Math.Max(140, this.Width / 8);
                int maxPanelWidth = Math.Max(200, this.Width / 4);

                if (mainSplitContainer.SplitterDistance < minPanelWidth)
                {
                    mainSplitContainer.SplitterDistance = minPanelWidth;
                }
                else if (mainSplitContainer.SplitterDistance > maxPanelWidth)
                {
                    mainSplitContainer.SplitterDistance = maxPanelWidth;
                }
            }
        }
        
        private async void StartUIRefreshLoop()
        {
            while (isOperationRunning)
            {
                await Task.Delay(250);
                if (!isOperationRunning) break;
                
                // Update elapsed time display and refresh ETA calculation
                if (operationStartTime != DateTime.MinValue)
                {
                    var elapsed = DateTime.Now - operationStartTime;
                    var statusText = $"Running... ({elapsed.TotalSeconds:F0}s elapsed)";
                    if (statusLabel.Text != statusText)
                    {
                        UpdateStatus(statusText);
                    }

                    // Also refresh the progress bar text (which contains the ETA)
                    if (lastProgressReport != null)
                    {
                        UpdateProgressUI(lastProgressReport);
                    }
                }
            }
        }
        
        private void InitializeEnhancedSystems()
        {
            try
            {
                // Initialize enhanced progress system
                enhancedProgressSystem = new EnhancedProgressSystem(this)
                {
                    NotificationsEnabled = false
                };
                
                // Initialize responsive design
                Theme.ResponsiveDesign.Initialize(this);
                
                // Register main form controls for responsive behavior
                SetupResponsiveControls();
                
                ShowOutput("Enhanced systems initialized successfully.");
            }
            catch (Exception ex)
            {
                ShowOutput($"Warning: Failed to initialize enhanced systems: {ex.Message}");
                // Continue without enhanced systems if initialization fails
            }
        }
        
        private void SetupResponsiveControls()
        {
            // Register module buttons panel for responsive behavior
            if (moduleButtonsPanel != null)
            {
                Theme.ResponsiveDesign.RegisterControl(moduleButtonsPanel, new Theme.ResponsiveSettings
                {
                    ScaleSize = true,
                    BaseSize = moduleButtonsPanel.Size,
                    MinSize = new Size(200, 100),
                    MaxSize = new Size(400, 800),
                    Breakpoints = new List<Theme.BreakpointSettings>
                    {
                        new Theme.BreakpointSettings { MinWidth = 1200, Layout = Theme.ResponsiveLayout.Stack },
                        new Theme.BreakpointSettings { MinWidth = 800, Layout = Theme.ResponsiveLayout.Stack },
                        new Theme.BreakpointSettings { MinWidth = 0, Layout = Theme.ResponsiveLayout.Stack }
                    }
                });
            }
            
            // Register progress bar for responsive behavior
            if (progressBar != null)
            {
                Theme.ResponsiveDesign.RegisterControl(progressBar, new Theme.ResponsiveSettings
                {
                    ScaleSize = true,
                    BaseSize = new Size(progressBar.Width, progressBar.Height),
                    MinSize = new Size(150, 16),
                    MaxSize = new Size(600, 32)
                });
            }
        }
        
        // Public methods to access enhanced systems
        public EnhancedProgressSystem? GetEnhancedProgressSystem() => enhancedProgressSystem;
        public List<IRecoveryModule> GetLoadedModules() => loadedModules;
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe from events to prevent memory leaks
                this.Resize -= MainForm_Resize;
                this.ResizeEnd -= MainForm_ResizeEnd;
                
                // Dispose enhanced systems
                enhancedProgressSystem?.Dispose();
                if (themeChangedHandler != null)
                {
                    Theme.OnThemeChanged -= themeChangedHandler;
                    themeChangedHandler = null;
                }
                if (themePreferencesChangedHandler != null)
                {
                    Theme.OnThemePreferencesChanged -= themePreferencesChangedHandler;
                    themePreferencesChangedHandler = null;
                }
                
                // Dispose other resources
                // Clean up loops
                isOperationRunning = false;
                
                // Clear collections to help GC
                outputHistory?.Clear();
                selectedActions?.Clear();
                stepIndicatorPanels?.Clear();
            }
            base.Dispose(disposing);
        }
        
        private async Task RunActionWithUiAsync(IRecoveryModule module, string actionName)
        {
            if (isOperationRunning)
            {
                MessageBox.Show("Another operation is currently running. Please wait for it to complete.",
                    "Operation in Progress", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isOperationRunning = true;
            operationStartTime = DateTime.Now;
            cancellationTokenSource = new CancellationTokenSource();

            cancelButton.Visible = true;
            cancelButton.Enabled = true;

            outputBox.Clear();
            outputBox.Visible = true;

            progressBar.Value = 0;
            statusLabel.Text = $"Running: {actionName}";
            progressBar.Visible = true;
            progressBar.IsIndeterminate = true;

            ShowOutput($"[{DateTime.Now:HH:mm:ss}] Starting: {actionName}");
            ShowOutput("");

            var progressReporter = new Progress<ProgressReport>(report =>
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => UpdateProgressUI(report)));
                }
                else
                {
                    UpdateProgressUI(report);
                }
            });

            try
            {
                await module.ExecuteActionAsync(actionName, progressReporter, output => ShowOutput(output), this, cancellationTokenSource.Token);

                if (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    progressBar.Value = 100;
                    statusLabel.Text = "Completed";
                    ShowOutput($"[{DateTime.Now:HH:mm:ss}] Completed: {actionName}");
                    ShowOutput("Operation completed successfully.");
                }
                else
                {
                    statusLabel.Text = "Cancelled";
                    ShowOutput($"[{DateTime.Now:HH:mm:ss}] Operation cancelled by user.");
                }
            }
            catch (OperationCanceledException)
            {
                statusLabel.Text = "Cancelled";
                ShowOutput($"[{DateTime.Now:HH:mm:ss}] Operation cancelled by user.");
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Error occurred";
                ShowOutput($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    ShowOutput($"Inner exception: {ex.InnerException.Message}");
                }
            }
            finally
            {
                cancelButton.Visible = false;
                progressBar.IsIndeterminate = false;
                isOperationRunning = false;

                var elapsed = DateTime.Now - operationStartTime;
                ShowOutput(string.Empty);
                ShowOutput($"Total execution time: {elapsed.TotalSeconds:F1} seconds");

                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }
    } // End of MainForm class

    // MenuRenderer for Windows 11 style
    internal class Windows11MenuRenderer : ToolStripProfessionalRenderer
    {
        public Windows11MenuRenderer() : base(new Windows11ColorTable()) { }
        
        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                Rectangle rect = new Rectangle(0, 0, e.Item.Width - 1, e.Item.Height - 1);
                using (GraphicsPath path = GetRoundedRect(rect, 4))
                {
                    using (SolidBrush brush = new SolidBrush(e.Item.Pressed ? Theme.Colors.Primary : Color.FromArgb(30, Theme.Colors.Primary)))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
            }
            else
            {
                base.OnRenderButtonBackground(e);
            }
        }

        private static GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(bounds.Right - radius * 2, bounds.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Color table for Windows 11 style menu
    internal class Windows11ColorTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => Theme.Colors.Background;
        public override Color ToolStripGradientMiddle => Theme.Colors.Background;
        public override Color ToolStripGradientEnd => Theme.Colors.Background;
        public override Color ToolStripBorder => Theme.Colors.Border;
        public override Color ButtonSelectedBorder => Theme.Colors.Primary;
        public override Color ButtonSelectedHighlight => Theme.Colors.Primary;
        public override Color ButtonSelectedGradientBegin => Color.FromArgb(30, Theme.Colors.Primary);
        public override Color ButtonSelectedGradientMiddle => Color.FromArgb(30, Theme.Colors.Primary);
        public override Color ButtonSelectedGradientEnd => Color.FromArgb(30, Theme.Colors.Primary);
    }

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
            catch { /* Ignore context-specific failures during shutdown */ }
        }
    }
}
