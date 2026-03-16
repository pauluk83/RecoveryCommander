using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Versioning;
using RecoveryCommander.Core;

namespace RecoveryCommander.Core
{
    /// <summary>
    /// Static theme provider for components that can't reference UI assembly
    /// </summary>
    public static class ThemeProvider
    {
        public static Color BackgroundColor { get; set; } = SystemColors.Control;
        public static Color ForegroundColor { get; set; } = SystemColors.ControlText;
    }

    [SupportedOSPlatform("windows")]
    public partial class WinREWizards : Form
    {
        private enum WizardStep
        {
            Welcome,
            ScanStateCapture,
            OemImageRegistration,
            Completion
        }

        private WizardStep currentStep = WizardStep.Welcome;
        private readonly Action<string> reportOutput;
        private readonly CancellationTokenSource cts = new();

        // Step data
        private string capturedPpkgPath = "";
        private string registeredOemImagePath = "";
        private bool scanStateCompleted = false;
        private bool oemRegistrationCompleted = false;

        public WinREWizards(Action<string> outputCallback)
        {
            InitializeComponent();
            reportOutput = outputCallback;

            // Setup wizard
            SetupWizard();
            UpdateWizardDisplay();
        }

        private void SetupWizard()
        {
            // Configure form
            Text = "Push-Button Reset Setup Wizard";
            Size = new Size(600, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Apply theme colors
            BackColor = ThemeProvider.BackgroundColor;
            ForeColor = ThemeProvider.ForegroundColor;

            // Create controls
            CreateWizardControls();
        }

        private void CreateWizardControls()
        {
            // Title label
            lblTitle = new Label
            {
                Text = "Push-Button Reset Setup Wizard",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(550, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ThemeProvider.ForegroundColor
            };
            Controls.Add(lblTitle);

            // Step indicator
            lblStepIndicator = new Label
            {
                Text = "Step 1 of 3",
                Font = new Font("Segoe UI", 10),
                Location = new Point(20, 60),
                Size = new Size(550, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ThemeProvider.ForegroundColor
            };
            Controls.Add(lblStepIndicator);

            // Content panel
            pnlContent = new Panel
            {
                Location = new Point(20, 90),
                Size = new Size(550, 300),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(pnlContent);

            // Navigation buttons
            btnBack = new Button
            {
                Text = "< Back",
                Location = new Point(20, 410),
                Size = new Size(80, 35),
                Enabled = false,
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeProvider.BackgroundColor,
                ForeColor = ThemeProvider.BackgroundColor == Color.FromArgb(32, 32, 32) ? Color.White : Color.Black
            };
            btnBack.Click += BtnBack_Click;
            Controls.Add(btnBack);

            btnNext = new Button
            {
                Text = "Next >",
                Location = new Point(420, 410),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeProvider.BackgroundColor,
                ForeColor = ThemeProvider.BackgroundColor == Color.FromArgb(32, 32, 32) ? Color.White : Color.Black
            };
            btnNext.Click += BtnNext_Click;
            Controls.Add(btnNext);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(510, 410),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeProvider.BackgroundColor,
                ForeColor = ThemeProvider.BackgroundColor == Color.FromArgb(32, 32, 32) ? Color.White : Color.Black
            };
            btnCancel.Click += BtnCancel_Click;
            Controls.Add(btnCancel);

            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(110, 420),
                Size = new Size(300, 20),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            Controls.Add(progressBar);
        }

        private void UpdateWizardDisplay()
        {
            // Clear content panel
            pnlContent!.Controls.Clear();

            // Update step indicator and navigation
            switch (currentStep)
            {
                case WizardStep.Welcome:
                    lblStepIndicator!.Text = "Step 1 of 3: Introduction";
                    btnBack!.Enabled = false;
                    btnNext!.Text = "Next >";
                    ShowWelcomeStep();
                    break;

                case WizardStep.ScanStateCapture:
                    lblStepIndicator!.Text = "Step 2 of 3: Capture Customizations";
                    btnBack!.Enabled = true;
                    btnNext!.Text = scanStateCompleted ? "Next >" : "Capture Now";
                    ShowScanStateStep();
                    break;

                case WizardStep.OemImageRegistration:
                    lblStepIndicator!.Text = "Step 3 of 3: Register OEM Image";
                    btnBack!.Enabled = true;
                    btnNext!.Text = oemRegistrationCompleted ? "Finish" : "Register Now";
                    ShowOemImageStep();
                    break;

                case WizardStep.Completion:
                    lblStepIndicator!.Text = "Complete!";
                    btnBack!.Enabled = false;
                    btnNext!.Text = "Close";
                    ShowCompletionStep();
                    break;
            }

            // Update progress
            progressBar!.Value = ((int)currentStep * 100) / 3;
        }

        private void ShowWelcomeStep()
        {
            if (pnlContent == null) return;

            var lblWelcome = new Label
            {
                Text = "Push-Button Reset Setup Guide",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(500, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ThemeProvider.ForegroundColor
            };
            pnlContent.Controls.Add(lblWelcome);

            var lblDescription = new Label
            {
                Text = "This wizard will guide you through setting up Windows Push-Button Reset (PBR) " +
                      "by capturing your current system customizations and registering an OEM recovery image.\n\n" +
                      "What this wizard does:\n" +
                      "• Step 1: Capture installed applications and settings using ScanState\n" +
                      "• Step 2: Register a custom Windows image for factory-style reset\n\n" +
                      "Requirements:\n" +
                      "• Windows ADK (Assessment and Deployment Kit) installed\n" +
                      "• Administrator privileges\n" +
                      "• Custom Windows installation image (install.wim)",
                Location = new Point(20, 60),
                Size = new Size(500, 200),
                AutoSize = false,
                ForeColor = ThemeProvider.ForegroundColor
            };
            lblDescription.Font = new Font("Segoe UI", 9);
            pnlContent.Controls.Add(lblDescription);
        }

        private void ShowScanStateStep()
        {
            if (pnlContent == null) return;

            var lblTitle = new Label
            {
                Text = "Step 1: Capture System Customizations",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(500, 30),
                ForeColor = ThemeProvider.ForegroundColor
            };
            pnlContent.Controls.Add(lblTitle);

            var lblDescription = new Label
            {
                Text = "This step captures all installed desktop applications and settings into a provisioning package.\n" +
                      "When users perform a Push-Button Reset, these customizations will be preserved.\n\n" +
                      "The process may take several minutes depending on the number of installed applications.",
                Location = new Point(20, 60),
                Size = new Size(500, 80),
                AutoSize = false,
                ForeColor = ThemeProvider.ForegroundColor
            };
            lblDescription.Font = new Font("Segoe UI", 9);
            pnlContent.Controls.Add(lblDescription);

            // Status display
            lblStatus = new Label
            {
                Text = scanStateCompleted ? "✓ Capture completed successfully!" : "Ready to capture customizations...",
                Location = new Point(20, 150),
                Size = new Size(500, 30),
                ForeColor = scanStateCompleted ? Color.Green : ThemeProvider.ForegroundColor
            };
            pnlContent.Controls.Add(lblStatus);

            if (scanStateCompleted && !string.IsNullOrEmpty(capturedPpkgPath))
            {
                var lblPath = new Label
                {
                    Text = $"Package saved to: {capturedPpkgPath}",
                    Location = new Point(20, 190),
                    Size = new Size(500, 40),
                    Font = new Font("Segoe UI", 8)
                };
                pnlContent.Controls.Add(lblPath);
            }
        }

        private void ShowOemImageStep()
        {
            if (pnlContent == null) return;

            var lblTitle = new Label
            {
                Text = "Step 2: Register OEM Recovery Image",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(500, 30),
                ForeColor = ThemeProvider.ForegroundColor
            };
            pnlContent.Controls.Add(lblTitle);

            var lblDescription = new Label
            {
                Text = "This step registers a custom Windows installation image for factory-style reset.\n" +
                      "Users will be able to perform a complete system recovery that reinstalls Windows from your custom image.\n\n" +
                      "Select the install.wim file containing your customized Windows image.",
                Location = new Point(20, 60),
                Size = new Size(500, 80),
                AutoSize = false,
                ForeColor = ThemeProvider.ForegroundColor
            };
            lblDescription.Font = new Font("Segoe UI", 9);
            pnlContent.Controls.Add(lblDescription);

            // Status display
            lblOemStatus = new Label
            {
                Text = oemRegistrationCompleted ? "✓ OEM image registered successfully!" : "Select Windows image file...",
                Location = new Point(20, 150),
                Size = new Size(500, 30),
                ForeColor = oemRegistrationCompleted ? Color.Green : ThemeProvider.ForegroundColor
            };
            pnlContent.Controls.Add(lblOemStatus);

            if (oemRegistrationCompleted && !string.IsNullOrEmpty(registeredOemImagePath))
            {
                var lblPath = new Label
                {
                    Text = $"Image registered: {registeredOemImagePath}",
                    Location = new Point(20, 190),
                    Size = new Size(500, 40),
                    Font = new Font("Segoe UI", 8)
                };
                pnlContent.Controls.Add(lblPath);
            }
        }

        private void ShowCompletionStep()
        {
            if (pnlContent == null) return;

            var lblTitle = new Label
            {
                Text = "Setup Complete!",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(500, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Green
            };
            pnlContent.Controls.Add(lblTitle);

            var lblSummary = new Label
            {
                Text = "Push-Button Reset has been successfully configured!\n\n" +
                      "Completed steps:\n" +
                      (scanStateCompleted ? "✓ System customizations captured\n" : "") +
                      (oemRegistrationCompleted ? "✓ OEM recovery image registered\n" : "") +
                      "\nUsers can now perform factory-style resets that preserve their applications and settings.",
                Location = new Point(20, 80),
                Size = new Size(500, 120),
                AutoSize = false,
                ForeColor = ThemeProvider.ForegroundColor
            };
            lblSummary.Font = new Font("Segoe UI", 9);
            pnlContent.Controls.Add(lblSummary);

            var lblNextSteps = new Label
            {
                Text = "Next steps:\n" +
                      "• Test the reset functionality in Settings > Update & Security > Recovery\n" +
                      "• Ensure recovery partition has sufficient space\n" +
                      "• Backup the provisioning package and recovery image regularly",
                Location = new Point(20, 220),
                Size = new Size(500, 60),
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeProvider.ForegroundColor
            };
            pnlContent.Controls.Add(lblNextSteps);
        }

        private void BtnBack_Click(object? sender, EventArgs e)
        {
            if (btnBack == null) return;

            if (currentStep > WizardStep.Welcome)
            {
                currentStep--;
                UpdateWizardDisplay();
            }
        }

        private async void BtnNext_Click(object? sender, EventArgs e)
        {
            if (btnNext == null) return;

            if (currentStep == WizardStep.Completion)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            if (currentStep == WizardStep.ScanStateCapture && !scanStateCompleted)
            {
                await ExecuteScanStateCaptureAsync();
                return;
            }

            if (currentStep == WizardStep.OemImageRegistration && !oemRegistrationCompleted)
            {
                await ExecuteOemImageRegistrationAsync();
                return;
            }

            // Move to next step
            if (currentStep < WizardStep.Completion)
            {
                currentStep++;
                UpdateWizardDisplay();
            }
        }

        private async Task DownloadWindowsAdkAsync()
        {
            try
            {
                reportOutput("Preparing Windows ADK download...");
                
                // Show download options dialog
                var adkForm = new Form
                {
                    Text = "Download Windows ADK",
                    Size = new Size(600, 400),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = ThemeProvider.BackgroundColor
                };

                var lblInfo = new Label
                {
                    Text = "Windows ADK (Assessment and Deployment Kit) Download Options:\n\n" +
                          "Recommended: Windows ADK for Windows 11, version 22H2 or later\n" +
                          "Alternative: Windows ADK for Windows 10, version 2004 or later\n\n" +
                          "The ADK includes the User State Migration Tool (USMT) needed for ScanState.",
                    Location = new Point(20, 20),
                    Size = new Size(540, 100),
                    ForeColor = ThemeProvider.ForegroundColor
                };
                adkForm.Controls.Add(lblInfo);

                var btnAdk11 = new Button
                {
                    Text = "Download Windows ADK 11",
                    Location = new Point(20, 140),
                    Size = new Size(250, 40),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = ThemeProvider.BackgroundColor,
                    ForeColor = ThemeProvider.BackgroundColor == Color.FromArgb(32, 32, 32) ? Color.White : Color.Black
                };
                adkForm.Controls.Add(btnAdk11);

                var btnAdk10 = new Button
                {
                    Text = "Download Windows ADK 10",
                    Location = new Point(290, 140),
                    Size = new Size(250, 40),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = ThemeProvider.BackgroundColor,
                    ForeColor = ThemeProvider.BackgroundColor == Color.FromArgb(32, 32, 32) ? Color.White : Color.Black
                };
                adkForm.Controls.Add(btnAdk10);

                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(240, 320),
                    Size = new Size(120, 40),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = ThemeProvider.BackgroundColor,
                    ForeColor = ThemeProvider.BackgroundColor == Color.FromArgb(32, 32, 32) ? Color.White : Color.Black
                };
                adkForm.Controls.Add(btnCancel);

                btnAdk11.Click += (s, e) => {
                    System.Diagnostics.Process.Start(new ProcessStartInfo 
                    { 
                        FileName = "https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install",
                        UseShellExecute = true 
                    });
                    adkForm.DialogResult = DialogResult.OK;
                    adkForm.Close();
                };

                btnAdk10.Click += (s, e) => {
                    System.Diagnostics.Process.Start(new ProcessStartInfo 
                    { 
                        FileName = "https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install",
                        UseShellExecute = true 
                    });
                    adkForm.DialogResult = DialogResult.OK;
                    adkForm.Close();
                };

                btnCancel.Click += (s, e) => {
                    adkForm.DialogResult = DialogResult.Cancel;
                    adkForm.Close();
                };

                var result = adkForm.ShowDialog();
                
                if (result == DialogResult.OK)
                {
                    reportOutput("Windows ADK download page opened in browser.");
                    MessageBox.Show(
                        "Download and install Windows ADK with these components:\n\n" +
                        "✓ User State Migration Tool (USMT)\n" +
                        "✓ Deployment Tools (optional but recommended)\n\n" +
                        "After installation, restart this wizard to continue.",
                        "Installation Instructions", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                }
                else
                {
                    reportOutput("ADK download cancelled by user.");
                }
            }
            catch (Exception ex)
            {
                reportOutput($"ADK download failed: {ex.Message}");
                MessageBox.Show($"Failed to open ADK download: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            if (btnCancel == null) return;

            if (MessageBox.Show("Are you sure you want to cancel the setup wizard?", "Cancel Setup",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                cts.Cancel();
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private async Task ExecuteScanStateCaptureAsync()
        {
            try
            {
                if (btnNext != null) btnNext.Enabled = false;
                if (btnBack != null) btnBack.Enabled = false;
                if (progressBar != null) progressBar.Style = ProgressBarStyle.Marquee;

                reportOutput("Checking for Windows ADK requirements...");

                // Define common ScanState paths
                string[] searchPaths = {
                    @"C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\User State Migration Tool\amd64\scanstate.exe",
                    @"C:\Program Files (x86)\Windows Kits\11\Assessment and Deployment Kit\User State Migration Tool\amd64\scanstate.exe",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "ScanState", "scanstate.exe")
                };

                string? scanstatePath = searchPaths.FirstOrDefault(File.Exists);
                if (string.IsNullOrEmpty(scanstatePath))
                {
                    reportOutput("Windows ADK not found. Checking requirements...");
                    
                    var result = MessageBox.Show(
                        "Windows ADK (Assessment and Deployment Kit) with User State Migration Tool is required for this step.\n\n" +
                        "Would you like to:\n" +
                        "• Yes - Download Windows ADK (recommended)\n" +
                        "• No - Continue without ADK (manual setup required)\n" +
                        "• Cancel - Exit wizard",
                        "Windows ADK Required", 
                        MessageBoxButtons.YesNoCancel, 
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        await DownloadWindowsAdkAsync();
                        return;
                    }
                    else if (result == DialogResult.No)
                    {
                        reportOutput("WARNING: Continuing without ADK. Manual setup required.");
                        MessageBox.Show(
                            "To complete this step manually:\n" +
                            "1. Download Windows ADK from Microsoft's website\n" +
                            "2. Install the User State Migration Tool component\n" +
                            "3. Place scanstate.exe in one of these locations:\n" +
                            "   • C:\\Program Files (x86)\\Windows Kits\\10\\Assessment and Deployment Kit\\User State Migration Tool\\amd64\\\n" +
                            "   • C:\\Program Files (x86)\\Windows Kits\\11\\Assessment and Deployment Kit\\User State Migration Tool\\amd64\\\n" +
                            "   • [AppFolder]\\Tools\\ScanState\\",
                            "Manual Setup Required", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Information);
                        return;
                    }
                    else // Cancel
                    {
                        reportOutput("Setup cancelled by user.");
                        return;
                    }
                }

                string destDir = @"C:\Recovery\Customizations";
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                string ppkgPath = Path.Combine(destDir, "usmt.ppkg");
                string logPath = Path.Combine(destDir, "ScanState.log");

                // Command: ScanState.exe /apps /ppkg C:\Recovery\Customizations\usmt.ppkg /o /v:13 /l:C:\Recovery\Customizations\ScanState.log
                string args = $"/apps /ppkg \"{ppkgPath}\" /o /v:13 /l:\"{logPath}\"";

                reportOutput($"Running: {scanstatePath} {args}");
                reportOutput("This process may take several minutes...");

                var psi = CoreUtilities.CreateProcessInfo(scanstatePath, args);
                await AsyncHelpers.RunProcessAsync(psi, reportOutput, err => reportOutput("SCANSTATE: " + err), cts.Token);

                capturedPpkgPath = ppkgPath;
                scanStateCompleted = true;

                reportOutput("SUCCESS: Customizations captured successfully!");
                if (lblStatus != null)
                {
                    lblStatus.Text = "✓ Capture completed successfully!";
                    lblStatus.ForeColor = Color.Green;
                }

                // Add path display
                var lblPath = new Label
                {
                    Text = $"Package saved to: {capturedPpkgPath}",
                    Location = new Point(20, 190),
                    Size = new Size(500, 40),
                    Font = new Font("Segoe UI", 8),
                    ForeColor = ThemeProvider.ForegroundColor
                };
                pnlContent?.Controls.Add(lblPath);

                MessageBox.Show("ScanState capture completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                reportOutput("ScanState capture was cancelled.");
            }
            catch (Exception ex)
            {
                reportOutput($"ScanState capture failed: {ex.Message}");
                MessageBox.Show($"ScanState capture failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (btnNext != null) btnNext.Enabled = true;
                if (btnBack != null) btnBack.Enabled = true;
                if (progressBar != null) progressBar.Style = ProgressBarStyle.Continuous;
                if (btnNext != null) btnNext.Text = "Next >";
                UpdateWizardDisplay();
            }
        }

        private async Task ExecuteOemImageRegistrationAsync()
        {
            try
            {
                using var ofd = new OpenFileDialog
                {
                    Filter = "Windows Image (install.wim)|install.wim|WIM Files (*.wim)|*.wim|All Files (*.*)|*.*",
                    Title = "Select the Custom Windows Image (WIM) to use for Factory Reset",
                    CheckFileExists = true
                };

                if (ofd.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(ofd.FileName))
                {
                    return; // User cancelled
                }

                if (btnNext != null) btnNext.Enabled = false;
                if (btnBack != null) btnBack.Enabled = false;
                if (progressBar != null) progressBar.Style = ProgressBarStyle.Marquee;

                string wimPath = ofd.FileName;
                string oemDir = @"C:\Recovery\OEM";
                if (!Directory.Exists(oemDir)) Directory.CreateDirectory(oemDir);

                string xmlPath = Path.Combine(oemDir, "ResetConfig.xml");

                string xmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Reset>
  <Run>
    <Phase>FactoryReset_AfterDiskFormat</Phase>
    <Path>scripts\PreparePartitions.cmd</Path>
    <Duration>2</Duration>
  </Run>
  <SystemDisk>
    <MinSize>60000</MinSize>
  </SystemDisk>
</Reset>";

                await File.WriteAllTextAsync(xmlPath, xmlContent, Encoding.UTF8, cts.Token);

                registeredOemImagePath = wimPath;
                oemRegistrationCompleted = true;

                reportOutput($"SUCCESS: Created ResetConfig.xml at {xmlPath}");
                reportOutput("Note: You must also place your custom WIM and any necessary scripts in the OEM directory.");
                if (lblOemStatus != null)
                {
                    lblOemStatus.Text = "✓ OEM image registered successfully!";
                    lblOemStatus.ForeColor = Color.Green;
                }

                // Add path display
                var lblPath = new Label
                {
                    Text = $"Image registered: {registeredOemImagePath}",
                    Location = new Point(20, 190),
                    Size = new Size(500, 40),
                    Font = new Font("Segoe UI", 8),
                    ForeColor = ThemeProvider.ForegroundColor
                };
                pnlContent?.Controls.Add(lblPath);

                MessageBox.Show("OEM image registration completed successfully!\n\n" +
                    "Note: Ensure your custom WIM file and any required scripts are placed in C:\\Recovery\\OEM\\",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                reportOutput("OEM image registration was cancelled.");
            }
            catch (Exception ex)
            {
                reportOutput($"OEM image registration failed: {ex.Message}");
                MessageBox.Show($"OEM image registration failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (btnNext != null) btnNext.Enabled = true;
                if (btnBack != null) btnBack.Enabled = true;
                if (progressBar != null) progressBar.Style = ProgressBarStyle.Continuous;
                if (btnNext != null) btnNext.Text = "Finish";
                UpdateWizardDisplay();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            cts.Cancel();
            base.OnFormClosing(e);
        }

        #region Designer Generated Code
        private void InitializeComponent()
        {
            SuspendLayout();
            // Form properties will be set in SetupWizard()
            ResumeLayout(false);
        }
        #endregion

        #region Control Declarations
        private Label? lblTitle;
        private Label? lblStepIndicator;
        private Panel? pnlContent;
        private Button? btnBack;
        private Button? btnNext;
        private Button? btnCancel;
        private ProgressBar? progressBar;

        // Step-specific controls
        private Label? lblStatus;
        private Label? lblOemStatus;
        #endregion
    }
}
