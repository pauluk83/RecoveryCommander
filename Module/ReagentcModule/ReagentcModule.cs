using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Versioning;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;

namespace RecoveryCommander.Module;

[SupportedOSPlatform("windows")]
[RecoveryModuleAttribute("ReagentcModule", "1.1.0")]
public sealed class ReagentcModule : IRecoveryModule
{
    public string Name => "REAgentc";
    public string Description => "Manages the Windows Recovery Environment (WinRE) including status and linkage repair.";
    public string Version => "1.1.0";
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
        new("Repair WinRE Path", "Advanced Repair (Mount/Pick/Set)", ExecuteSetRecoveryImageFromHiddenPartitionAsync)
        {
            Description = "Allows picking a recovery WIM from a hidden partition or directory and manually re-linking it.",
            RequiresAdmin = true,
            IconName = "Settings"
        },
        new("Complete PBR Setup Wizard", "Guided Push-Button Reset Setup (ScanState + OEM Image)", ExecutePbrSetupWizardAsync)
        {
            Description = "Step-by-step wizard that guides you through capturing system customizations with ScanState and registering an OEM recovery image for complete Push-Button Reset functionality.",
            RequiresAdmin = true,
            Highlight = true,
            IconName = "Wizard"
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

    private async Task ExecuteSetRecoveryImageFromHiddenPartitionAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
    {
        progress.Report(new ProgressReport(0, "Starting recovery environment path repair..."));
        char letter = ' ';
        int? usedVolNum = null;
        int? usedDisk = null, usedPart = null;

        try
        {
            letter = DiskUtility.FindAvailableDriveLetter();
            usedVolNum = await DiskUtility.FindVolumeNumberByLabelAsync("Image", reportOutput, cancellationToken);
            
            if (usedVolNum.HasValue)
            {
                reportOutput($"Assigning drive letter {letter}: to Volume {usedVolNum.Value}...");
                var ok = await DiskUtility.RunDiskpartScriptAsync($"select volume {usedVolNum.Value}\r\nassign letter={letter}\r\n", reportOutput, cancellationToken);
                if (!ok) throw new Exception("Failed to assign drive letter to volume.");
            }
            else
            {
                reportOutput("Determining current WinRE partition location...");
                var info = await ReagentcHelper.RunReagentcAsync("/info", progress, output => { }, cancellationToken);
                var dp = ReagentcHelper.ParseDiskPartitionFromInfo(info);
                if (dp == null) throw new Exception("Could not find current or legacy recovery partition information.");
                
                (usedDisk, usedPart) = dp.Value;
                reportOutput($"Attempting to mount Disk {usedDisk}, Partition {usedPart} as {letter}:...");
                var ok = await DiskUtility.RunDiskpartScriptAsync($"select disk {usedDisk}\r\nselect partition {usedPart}\r\nassign letter={letter}\r\n", reportOutput, cancellationToken);
                if (!ok) throw new Exception("Failed to assign drive letter using diskpart.");
            }

            using var ofd = new OpenFileDialog
            {
                Filter = "Recovery WIM (winre.wim)|winre.wim|WIM Files (*.wim)|*.wim|All Files (*.*)|*.*",
                Title = "Locate winre.wim on the mounted partition",
                InitialDirectory = $"{letter}:\\",
                CheckPathExists = true,
                CheckFileExists = true
            };

            if (ofd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(ofd.FileName))
            {
                var selected = ofd.FileName;
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
            if (letter != ' ')
            {
                reportOutput($"Unmounting temporary drive letter {letter}:...");
                string script = usedVolNum.HasValue 
                    ? $"select volume {usedVolNum.Value}\r\nremove letter={letter}\r\n"
                    : $"select disk {usedDisk}\r\nselect partition {usedPart}\r\nremove letter={letter}\r\n";
                await DiskUtility.RunDiskpartScriptAsync(script, reportOutput, cancellationToken);
            }
            progress.Report(new ProgressReport(100, "Finished."));
        }
    }

    private async Task ExecutePbrSetupWizardAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, IDialogService dialogService, CancellationToken cancellationToken)
    {
        progress.Report(new ProgressReport(0, "Configuring OEM Restore Image..."));

        using var ofd = new OpenFileDialog
        {
            Filter = "Windows Image (install.wim)|install.wim|WIM Files (*.wim)|*.wim|All Files (*.*)|*.*",
            Title = "Select the Custom Windows Image (WIM) to use for Factory Reset",
            CheckFileExists = true
        };

        if (ofd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(ofd.FileName))
        {
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
            
            try
            {
                await File.WriteAllTextAsync(xmlPath, xmlContent, System.Text.Encoding.UTF8, cancellationToken);
                reportOutput($"SUCCESS: Created {xmlPath}. Note: You must also place your custom WIM and any necessary scripts in {oemDir}.");
                progress.Report(new ProgressReport(100, "OEM Image Registration (Config) Complete."));
            }
            catch (Exception ex)
            {
                reportOutput($"Failed to create ResetConfig.xml: {ex.Message}");
                progress.Report(new ProgressReport(100, "Registration failed."));
            }
        }
        else
        {
            reportOutput("Registration cancelled.");
            progress.Report(new ProgressReport(100, "Cancelled."));
        }
    }

    private async Task ExecutePbrSetupWizardAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
    {
        progress.Report(new ProgressReport(0, "Launching Push-Button Reset Setup Wizard..."));

        try
        {
            // Run the wizard on a background thread to avoid blocking the UI
            var result = await Task.Run(() =>
            {
                using var wizard = new RecoveryCommander.Core.WinREWizards(reportOutput);
                return wizard.ShowDialog();
            }, cancellationToken);

            if (result == DialogResult.OK)
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
}
