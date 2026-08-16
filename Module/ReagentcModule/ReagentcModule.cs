using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Runtime.Versioning;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;

namespace RecoveryCommander.Modules;

[SupportedOSPlatform("windows")]
[RecoveryModuleAttribute("ReagentcModule")]
public sealed class ReagentcModule : IRecoveryModule
{
    public string Name => "REAgentc";
    public string Description => "Manages the Windows Recovery Environment (WinRE) including status and linkage repair.";
    public string Version => GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0";
    public string HealthStatus => "Healthy";
    public string BuildInfo => "REAgentc Module - Windows Recovery Environment Manager (Modernized)";
    public bool SupportsAsync => true;

    public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
    {
        new("Check Status", "Query WinRE Status (/info)")
        {
            ExecuteActionExtended = ExecuteInfoAsync,
            Description = "Displays detailed information about the Windows Recovery Environment and its current location.",
            RequiresAdmin = true,
            Highlight = true,
            IconName = "ShieldSearch"
        },
        new("Reset Recovery", "Reset WinRE Link (Disable/Enable Cycle)", ExecuteResetWinReAsync)
        {
            Description = "Toggles WinRE off and back on. This often fixes corrupted links or 'stuck' recovery environments.",
            RequiresAdmin = true,
            IconName = "Tools"
        },
        new("Enable WinRE", "Enable WinRE (/enable)", ExecuteEnableAsync)
        {
            Description = "Enables the Windows Recovery Environment if it was previously disabled.",
            RequiresAdmin = true,
            IconName = "ShieldCheck"
        },
        new("Disable WinRE", "Disable WinRE (/disable)", ExecuteDisableAsync)
        {
            Description = "Disables the Windows Recovery Environment. Required before some disk partitioning tasks.",
            RequiresAdmin = true,
            IconName = "ShieldAlert"
        },
        new("Repair WinRE Path", "Advanced Repair (Mount/Pick/Set)")
        {
            ExecuteActionExtended = ExecuteSetRecoveryImageFromHiddenPartitionAsync,
            Description = "Allows picking a recovery WIM from a hidden partition or directory and manually re-linking it.",
            RequiresAdmin = true,
            IconName = "Settings"
        },
        new("Complete PBR Setup Wizard", "Guided Push-Button Reset Setup (ScanState + OEM Image)")
        {
            ExecuteActionExtended = ExecutePbrSetupWizardAsync,
            Description = "Step-by-step wizard that guides you through capturing system customizations with ScanState and registering an OEM recovery image for complete Push-Button Reset functionality.",
            RequiresAdmin = true,
            Highlight = true,
            IconName = "Wizard"
        },
        new("Register FFU Restore", "Register Modern FFU Factory Image")
        {
            ExecuteActionExtended = ExecuteFfuRegistrationAsync,
            Description = "Registers a Full Flash Update (FFU) image for high-speed factory restore. FFU is sector-based and significantly faster than WIM restores.",
            RequiresAdmin = true,
            IconName = "Lightning"
        },
        new("Launch ReAgentC GUI", "Run ReAgentC GUI as Administrator")
        {
            ExecuteActionExtended = ExecuteLaunchReagentcGuiAsync,
            Description = "Launches ReAgentC_GUI.exe from the bundled Reagentc folder with elevated permissions.",
            RequiresAdmin = true,
            IconName = "OpenInNewWindow"
        }
    };

    private async Task ExecuteInfoAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
    {
        progress.Report(new ProgressReport(0, "Querying REAgentc status..."));
        string result = await ReagentcHelper.RunReagentcAsync("/info", progress, reportOutput, cancellationToken);
        progress.Report(new ProgressReport(100, "Status query complete."));

        // Show the results in a themed popup as requested
        if (!string.IsNullOrWhiteSpace(result))
        {
            dialogService.ShowContentDialog(result, "Windows RE Status Information");
        }
    }

    private Task ExecuteLaunchReagentcGuiAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
    {
        progress.Report(new ProgressReport(0, "Preparing ReAgentC GUI launch..."));
        cancellationToken.ThrowIfCancellationRequested();

        string exePath = FindReagentcGuiExecutable();
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            string message = $"Could not find ReAgentC_GUI.exe in the bundled Reagentc folder. Expected at: {exePath}";
            reportOutput(message);
            progress.Report(new ProgressReport(100, "ReAgentC GUI launch failed."));
            dialogService.ShowContentDialog(message, "ReAgentC GUI Launch Failed");
            return Task.CompletedTask;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory,
                UseShellExecute = true,
                Verb = "runas"
            };

            reportOutput($"Launching ReAgentC GUI from {exePath}...");
            Process.Start(startInfo);
            progress.Report(new ProgressReport(100, "ReAgentC GUI launch requested."));
        }
        catch (Exception ex)
        {
            string message = $"Unable to start ReAgentC GUI as administrator: {ex.Message}";
            reportOutput(message);
            progress.Report(new ProgressReport(100, "ReAgentC GUI launch failed."));
            dialogService.ShowContentDialog(message, "ReAgentC GUI Launch Failed");
        }

        return Task.CompletedTask;
    }

    private static string FindReagentcGuiExecutable()
    {
        // For single-file published apps Assembly.Location may be empty — prefer AppContext.BaseDirectory.
        string[] candidateRoots = new[]
        {
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Module", "ReagentcModule", "Reagentc"))
        };

        foreach (string root in candidateRoots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string candidate = Path.Combine(root, "Reagentc", "ReAgentC_GUI.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(root, "ReAgentC_GUI.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private async Task ExecuteResetWinReAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
    {
        progress.Report(new ProgressReport(10, "Disabling WinRE..."));
        await ReagentcHelper.RunReagentcAsync("/disable", progress, reportOutput, cancellationToken);
        
        progress.Report(new ProgressReport(50, "Enabling WinRE..."));
        await ReagentcHelper.RunReagentcAsync("/enable", progress, reportOutput, cancellationToken);
        
        progress.Report(new ProgressReport(100, "WinRE has been reset. Checking status..."));
        await ReagentcHelper.RunReagentcAsync("/info", progress, reportOutput, cancellationToken);
    }

    private async Task ExecuteEnableAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
    {
        progress.Report(new ProgressReport(0, "Enabling Windows RE..."));
        await ReagentcHelper.RunReagentcAsync("/enable", progress, reportOutput, cancellationToken);
        progress.Report(new ProgressReport(100, "Enabled successfully."));
    }

    private async Task ExecuteDisableAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
    {
        progress.Report(new ProgressReport(0, "Disabling Windows RE..."));
        await ReagentcHelper.RunReagentcAsync("/disable", progress, reportOutput, cancellationToken);
        progress.Report(new ProgressReport(100, "Disabled successfully."));
    }

    private async Task ExecuteSetRecoveryImageFromHiddenPartitionAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
    {
        progress.Report(new ProgressReport(0, "Starting recovery environment path repair..."));
        char letter = ' ';
        int? usedVolNum = null;
        int? usedDisk = null, usedPart = null;
        bool mountedTemporaryLetter = false;

        try
        {
            letter = DiskUtility.FindAvailableDriveLetter();
            usedVolNum = await DiskUtility.FindVolumeNumberByLabelAsync("Image", reportOutput, cancellationToken);
            
            if (usedVolNum.HasValue)
            {
                reportOutput($"Assigning drive letter {letter}: to Volume {usedVolNum.Value}...");
                var ok = await DiskUtility.RunDiskpartScriptAsync($"select volume {usedVolNum.Value}\r\nassign letter={letter}\r\n", reportOutput, cancellationToken);
                if (!ok) throw new InvalidOperationException("Failed to assign drive letter to volume.");
                mountedTemporaryLetter = true;
            }
            else
            {
                reportOutput("Determining current WinRE partition location...");
                var info = await ReagentcHelper.RunReagentcAsync("/info", progress, output => { }, cancellationToken);
                var dp = ReagentcHelper.ParseDiskPartitionFromInfo(info);
                if (dp == null) throw new InvalidOperationException("Could not find current or legacy recovery partition information.");
                
                (usedDisk, usedPart) = dp.Value;
                reportOutput($"Attempting to mount Disk {usedDisk}, Partition {usedPart} as {letter}:...");
                var ok = await DiskUtility.RunDiskpartScriptAsync($"select disk {usedDisk}\r\nselect partition {usedPart}\r\nassign letter={letter}\r\n", reportOutput, cancellationToken);
                if (!ok) throw new InvalidOperationException("Failed to assign drive letter using diskpart.");
                mountedTemporaryLetter = true;
            }

            var selected = dialogService.ShowOpenFileDialog("Recovery WIM (winre.wim)|winre.wim|WIM Files (*.wim)|*.wim|All Files (*.*)|*.*", "Locate winre.wim on the mounted partition", $"{letter}:\\");

            if (!string.IsNullOrWhiteSpace(selected))
            {
                var dir = Path.GetDirectoryName(selected) ?? $"{letter}:\\";
                var fileName = Path.GetFileName(selected);
                
                reportOutput($"Registering recovery path: {dir}");
                await ReagentcHelper.RunReagentcAsync($"/setreimage /path \"{dir}\"", progress, reportOutput, cancellationToken);
                
                reportOutput("Activating environment...");
                await ReagentcHelper.RunReagentcAsync("/enable", progress, reportOutput, cancellationToken);
                progress.Report(new ProgressReport(95, "Repair complete."));
            }
        }
        catch (OperationCanceledException)
        {
            reportOutput("Operation aborted.");
        }
        catch (Exception ex)
        {
            reportOutput($"Repair failed: {ex.Message}");
            progress.Report(new ProgressReport(100, "Error during repair."));
        }
        finally
        {
            if (mountedTemporaryLetter)
            {
                reportOutput($"Unmounting temporary drive letter {letter}:...");
                string script = usedVolNum.HasValue 
                    ? $"select volume {usedVolNum.Value}\r\nremove letter={letter}\r\n"
                    : $"select disk {usedDisk}\r\nselect partition {usedPart}\r\nremove letter={letter}\r\n";
                await DiskUtility.RunDiskpartScriptAsync(script, reportOutput, CancellationToken.None);
            }
            progress.Report(new ProgressReport(100, "Finished."));
        }
    }

    private async Task ExecutePbrSetupWizardAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
    {
        progress.Report(new ProgressReport(0, "Launching Push-Button Reset Setup Wizard..."));

        var wizardService = ServiceContainer.GetOptionalService<IWinReWizardService>();
        if (wizardService == null)
        {
            reportOutput("The WinRE wizard service is not available in this host.");
            progress.Report(new ProgressReport(100, "Wizard service unavailable."));
            return;
        }

        try
        {
            bool result = await wizardService.RunPbrSetupWizardAsync(progress, reportOutput, cancellationToken);

            if (result)
            {
                reportOutput("Push-Button Reset Setup Wizard completed successfully!");
                progress.Report(new ProgressReport(100, "PBR Setup Wizard completed."));
            }
            else
            {
                reportOutput("Push-Button Reset Setup Wizard was cancelled.");
                progress.Report(new ProgressReport(100, "Wizard cancelled."));
            }
        }
        catch (Exception ex)
        {
            reportOutput($"Failed to launch PBR Setup Wizard: {ex.Message}");
            progress.Report(new ProgressReport(100, "Wizard failed to launch."));
        }
    }

    private async Task ExecuteFfuRegistrationAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
    {
        progress.Report(new ProgressReport(0, "Configuring Modern FFU Factory Reset..."));

        string? ffuPath = dialogService.ShowOpenFileDialog("FFU Image (*.ffu)|*.ffu|All Files (*.*)|*.*", "Select the Full Flash Update (FFU) Image for Factory Reset");

        if (string.IsNullOrWhiteSpace(ffuPath))
        {
            reportOutput("Registration cancelled.");
            progress.Report(new ProgressReport(100, "Cancelled."));
            return;
        }
        string oemDir = @"C:\Recovery\OEM";
        if (!Directory.Exists(oemDir)) Directory.CreateDirectory(oemDir);

        string xmlPath = Path.Combine(oemDir, "ResetConfig.xml");
        
        // FFU utilizes a different restore logic than WIM (sector-based vs file-based).
        // Build XML structurally so special characters in selected paths cannot corrupt it.
        var xmlContent = new XDocument(
            new XElement("Reset",
                new XElement("Run",
                    new XAttribute("Phase", "FactoryReset_AfterDiskFormat"),
                    new XElement("Path", $@"cmd.exe /c dism /Apply-Ffu /ImageFile:""{ffuPath}"" /ApplyDrive:\\.\PhysicalDrive0 /CheckIntegrity")),
                new XElement("Run",
                    new XAttribute("Phase", "FactoryReset_AfterImageApply"),
                    new XElement("Path", @"cmd.exe /c bcdboot C:\Windows"))));

        try
        {
            await using (var stream = File.Create(xmlPath))
            {
                await xmlContent.SaveAsync(stream, SaveOptions.None, cancellationToken);
            }
            reportOutput($"SUCCESS: Modern FFU Restore Configured.");
            reportOutput($"Registered: {ffuPath}");
            reportOutput($"Config: {xmlPath}");
            reportOutput("NOTE: FFU restore is sector-accurate. Ensure the FFU was captured from the same physical drive layout.");
            progress.Report(new ProgressReport(100, "FFU Registration Complete."));

            dialogService.ShowContentDialog("FFU-based Factory Reset has been configured.\n\n" +
                "When you initiate 'Reset this PC', Windows will now use DISM to apply your FFU image directly to the physical drive.",
                "Modern Reset Configured");
        }
        catch (Exception ex)
        {
            reportOutput($"Failed to register FFU: {ex.Message}");
            progress.Report(new ProgressReport(100, "Registration failed."));
        }
    }
}
