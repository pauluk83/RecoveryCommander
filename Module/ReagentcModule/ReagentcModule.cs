// AUDIT MARKER: ReagentcModule.cs | Created: 2025-09-10
// CHANGELOG:
// - New module implementing reagentc operations as a standalone module.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Versioning;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;

namespace RecoveryCommander.Module
{
    [SupportedOSPlatform("windows")]
    [RecoveryModuleAttribute("ReagentcModule", "1.0.0")]
    public class ReagentcModule : IRecoveryModule
    {
        public string Name => "REAgentc";
        public string Description => "Manages Windows Recovery Environment (REAgentc) operations.";
        public string BuildInfo => "REAgentc Module v1.0.0 - Windows Recovery Environment Manager";

        public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
        {
            new("Info", "Query REAgentc status (reagentc /info)") { ExecuteAction = ExecuteInfoAsync },
            new("Disable", "Disable Windows Recovery Environment (reagentc /disable)") { ExecuteAction = ExecuteDisableAsync },
            new("Enable", "Enable Windows Recovery Environment (reagentc /enable)") { ExecuteAction = ExecuteEnableAsync },
            new("CreateCustomRecoveryImage", "Create custom recovery image (pick output file)") { ExecuteAction = ExecuteCreateCustomRecoveryImageAsync },
            new("CreateAndSetCustomRecoveryImage", "Create custom recovery image and set it as the recovery image in reagentc (pick location)") { ExecuteAction = ExecuteCreateAndSetCustomRecoveryImageAsync },
            new("SetRecoveryImageFromHiddenPartition", "Temporarily mount hidden Recovery partition, pick WIM, set, unmount") { ExecuteAction = ExecuteSetRecoveryImageFromHiddenPartitionAsync }
        };

        public string Version => "1.0.0";
        public string HealthStatus => "Healthy";
        public bool SupportsAsync => true;


        private void RunReagentc(string args, Action<string> reportOutput, Func<bool> isCancelled)
        {
            var psi = RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("reagentc.exe", args);
            RunProcessAndReport(psi, reportOutput, isCancelled);
        }

        private void RunDism(string args, Action<string> reportOutput, Func<bool> isCancelled)
        {
            var psi = RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("dism.exe", args);
            RunProcessAndReport(psi, reportOutput, isCancelled);
        }

        // Use consolidated RunProcessAndReport from Core/AsyncHelpers
        private static void RunProcessAndReport(ProcessStartInfo psi, Action<string> reportOutput, Func<bool> isCancelled)
        {
            RecoveryCommander.Core.AsyncHelpers.RunProcessAndReport(psi, reportOutput, isCancelled);
        }

        private async Task ExecuteDisableAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(0, "Queued Disable - starting...", "Disable"));
            try
            {
                await RunReagentcAsync("/disable", reportOutput, cancellationToken);
                progress?.Report(new ProgressReport(100, "Completed", "Disable"));
            }
            catch (Exception ex)
            {
                reportOutput($"Exception: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Completed", "Disable"));
            }
        }

        private async Task ExecuteEnableAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(0, "Queued Enable - starting...", "Enable"));
            try
            {
                await RunReagentcAsync("/enable", reportOutput, cancellationToken);
                progress?.Report(new ProgressReport(100, "Completed", "Enable"));
            }
            catch (Exception ex)
            {
                reportOutput($"Exception: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Completed", "Enable"));
            }
        }

        private async Task ExecuteInfoAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(0, "Queued Info - starting...", "Info"));
            try
            {
                var dialog = CreateThemedTextWindow("REAgentc Info");
                var append = dialog.append;
                append("Running: reagentc /info");
                dialog.form.Show();
                void capture(string line) { append(line); reportOutput(line); }
                await RunReagentcAsync("/info", capture, cancellationToken);
                append("\r\nCompleted.");
                progress?.Report(new ProgressReport(100, "Completed", "Info"));
            }
            catch (Exception ex)
            {
                reportOutput($"Exception: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Completed", "Info"));
            }
        }

        private async Task ExecuteCreateCustomRecoveryImageAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(0, "Queued CreateCustomRecoveryImage - starting...", "CreateCustomRecoveryImage"));
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "WIM Image|*.wim|All files|*.*",
                    Title = "Choose output recovery image file",
                    FileName = "custom_recovery.wim",
                    OverwritePrompt = true
                };

                var dr = sfd.ShowDialog();
                if (dr == DialogResult.OK && !string.IsNullOrWhiteSpace(sfd.FileName))
                {
                    var dest = sfd.FileName;
                    reportOutput($"Creating recovery image to: {dest}");

                    var captureArgs = $"/Capture-Image /ImageFile:\"{dest}\" /CaptureDir:C:\\ /Name:\"CustomRecovery\"";
                    await RunDismAsync(captureArgs, reportOutput, cancellationToken);

                    reportOutput($"Recovery image created: {dest}");
                }
                else
                {
                    reportOutput("Recovery image creation cancelled by user.");
                }
                progress?.Report(new ProgressReport(100, "Completed", "CreateCustomRecoveryImage"));
            }
            catch (Exception ex)
            {
                reportOutput($"Exception launching save dialog or creating image: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Completed", "CreateCustomRecoveryImage"));
            }
        }

        private async Task ExecuteCreateAndSetCustomRecoveryImageAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(0, "Queued CreateAndSetCustomRecoveryImage - starting...", "CreateAndSetCustomRecoveryImage"));
            try
            {
                using var sfd2 = new SaveFileDialog
                {
                    Filter = "WIM Image|*.wim|All files|*.*",
                    Title = "Choose output recovery image file",
                    FileName = "custom_recovery.wim",
                    OverwritePrompt = true
                };

                var dr2 = sfd2.ShowDialog();
                if (dr2 == DialogResult.OK && !string.IsNullOrWhiteSpace(sfd2.FileName))
                {
                    var dest = sfd2.FileName;
                    reportOutput($"Creating recovery image to: {dest}");

                    var captureArgs = $"/Capture-Image /ImageFile:\"{dest}\" /CaptureDir:C:\\ /Name:\"CustomRecovery\"";
                    await RunDismAsync(captureArgs, reportOutput, cancellationToken);

                    reportOutput("Custom OS recovery image registration is not supported on Windows 11.");
                    ShowTextDialog("Factory Restore on Windows 11", $"Use WinRE to apply your image.\r\n\r\nCommands:\r\n1) dism /apply-image /imagefile:\"{dest}\" /index:1 /applydir:C:\\\\r\n2) bcdboot C:\\Windows /f UEFI\r\n\r\nTip: Run these from WinRE Command Prompt after selecting Advanced options.");
                }
                else
                {
                    reportOutput("Recovery image creation cancelled by user.");
                }
                progress?.Report(new ProgressReport(100, "Completed", "CreateAndSetCustomRecoveryImage"));
            }
            catch (Exception ex)
            {
                reportOutput($"Exception launching save dialog or creating image: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Completed", "CreateAndSetCustomRecoveryImage"));
            }
        }

        private async Task ExecuteSetRecoveryImageFromHiddenPartitionAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(0, "Queued SetRecoveryImageFromHiddenPartition - starting...", "SetRecoveryImageFromHiddenPartition"));
            try
            {
                var letter = FindAvailableDriveLetter();
                int? imageVol = await FindVolumeNumberByLabelAsync("Image", reportOutput, cancellationToken);
                bool usedVolume = false;
                int usedVolNum = -1;
                int usedDisk = -1, usedPart = -1;
                if (imageVol.HasValue)
                {
                    usedVolume = true;
                    usedVolNum = imageVol.Value;
                    reportOutput($"Assigning drive letter {letter}: to Volume {usedVolNum} (label=Image)...");
                    var assignVolOk = await RunDiskpartScriptAsync($"select volume {usedVolNum}\r\nassign letter={letter}\r\n", reportOutput, cancellationToken);
                    if (!assignVolOk)
                    {
                        reportOutput("Failed to assign drive letter to volume.");
                        progress?.Report(new ProgressReport(100, "Completed", "SetRecoveryImageFromHiddenPartition"));
                        return;
                    }
                }
                else
                {
                    reportOutput("Reading Windows RE location from reagentc /info...");
                    var info = await RunReagentcInfoCaptureAsync(cancellationToken);
                    var dp = ParseDiskPartitionFromInfo(info);
                    if (dp == null)
                    {
                        ShowTextDialog("Location Not Found", "Could not find a volume labeled 'Image' or parse disk/partition from reagentc /info.");
                        progress?.Report(new ProgressReport(100, "Completed", "SetRecoveryImageFromHiddenPartition"));
                        return;
                    }
                    var (disk, part) = dp.Value;
                    usedDisk = disk; usedPart = part;
                    reportOutput($"Assigning drive letter {letter}: to Disk {disk}, Partition {part}...");
                    var assignOk = await RunDiskpartScriptAsync($"select disk {disk}\r\nselect partition {part}\r\nassign letter={letter}\r\n", reportOutput, cancellationToken);
                    if (!assignOk)
                    {
                        reportOutput("Failed to assign drive letter.");
                        progress?.Report(new ProgressReport(100, "Completed", "SetRecoveryImageFromHiddenPartition"));
                        return;
                    }
                }

                try
                {
                    using var ofd = new OpenFileDialog
                    {
                        Filter = "WIM Image|*.wim|All files|*.*",
                        Title = "Select recovery WIM",
                        InitialDirectory = $"{letter}:\\"
                    };
                    var dr = ofd.ShowDialog();
                    if (dr == DialogResult.OK && !string.IsNullOrWhiteSpace(ofd.FileName))
                    {
                        var selected = ofd.FileName;
                        var dir = Path.GetDirectoryName(selected) ?? $"{letter}:\\";
                        var fileName = Path.GetFileName(selected);
                        if (string.Equals(fileName, "winre.wim", StringComparison.OrdinalIgnoreCase) || dir.IndexOf("WindowsRE", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            reportOutput($"Setting WinRE image directory: {dir}");
                            await RunReagentcAsync($"/setreimage /path:\"{dir}\"", reportOutput, cancellationToken);
                        }
                        else
                        {
                            reportOutput("Custom OS recovery image registration is not supported on Windows 11.");
                            ShowTextDialog("Factory Restore on Windows 11", $"Use WinRE to apply your image.\r\n\r\nCommands:\r\n1) dism /apply-image /imagefile:\"{selected}\" /index:1 /applydir:C:\\\\r\n2) bcdboot C:\\Windows /f UEFI\r\n\r\nTip: Run these from WinRE Command Prompt after selecting Advanced options.");
                        }
                        await RunReagentcAsync("/enable", reportOutput, cancellationToken);
                    }
                    else
                    {
                        reportOutput("Selection cancelled by user.");
                    }
                }
                finally
                {
                    if (usedVolume)
                    {
                        reportOutput($"Removing drive letter {letter}: from Volume {usedVolNum}...");
                        await RunDiskpartScriptAsync($"select volume {usedVolNum}\r\nremove letter={letter}\r\n", reportOutput, cancellationToken);
                    }
                    else
                    {
                        reportOutput($"Removing drive letter {letter}: from Disk {usedDisk}, Partition {usedPart}...");
                        await RunDiskpartScriptAsync($"select disk {usedDisk}\r\nselect partition {usedPart}\r\nremove letter={letter}\r\n", reportOutput, cancellationToken);
                    }
                }
                progress?.Report(new ProgressReport(100, "Completed", "SetRecoveryImageFromHiddenPartition"));
            }
            catch (Exception ex)
            {
                reportOutput($"Exception: {ex.Message}");
                progress?.Report(new ProgressReport(100, "Completed", "SetRecoveryImageFromHiddenPartition"));
            }
        }

        private async Task RunReagentcAsync(string args, Action<string> reportOutput, System.Threading.CancellationToken cancellationToken)
        {
            var psi = RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("reagentc.exe", args);
            await RunProcessAndReportAsync(psi, reportOutput, cancellationToken);
        }

        private static string RunReagentcInfoCapture()
        {
            var psi = RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("reagentc.exe", "/info");
            var sb = new StringBuilder();
            try
            {
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    sb.Append(proc.StandardOutput.ReadToEnd());
                    sb.Append(proc.StandardError.ReadToEnd());
                    proc.WaitForExit(5000);
                }
            }
            catch { }
            return sb.ToString();
        }

        private static int? FindVolumeNumberByLabel(string label, Action<string> reportOutput)
        {
            string tmp = Path.Combine(Path.GetTempPath(), $"rc_diskpart_{Guid.NewGuid():N}.txt");
            try
            {
                File.WriteAllText(tmp, "list volume\r\n");
                var psi = RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("diskpart.exe", $"/s \"{tmp}\"");
                var output = new StringBuilder();
                RunProcessAndReport(psi, (line) => { output.AppendLine(line); }, () => false);
                foreach (var raw in output.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var line = raw.Trim();
                    if (line.IndexOf(label, StringComparison.OrdinalIgnoreCase) >= 0 && line.StartsWith("Volume", StringComparison.OrdinalIgnoreCase))
                    {
                        var m = Regex.Match(line, @"Volume\s+(\d+)", RegexOptions.IgnoreCase);
                        if (m.Success && int.TryParse(m.Groups[1].Value, out var vol))
                        {
                            return vol;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                reportOutput($"Failed to list volumes: {ex.Message}");
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
            return null;
        }

        private static (int Disk, int Partition)? ParseDiskPartitionFromInfo(string infoText)
        {
            if (string.IsNullOrWhiteSpace(infoText)) return null;
            var m = Regex.Match(infoText, @"harddisk(?<disk>\d+)\\partition(?<part>\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups["disk"].Value, out var d) && int.TryParse(m.Groups["part"].Value, out var p))
            {
                return (d, p);
            }
            return null;
        }

        private static bool RunDiskpartScript(string script, Action<string> reportOutput, Func<bool> isCancelled)
        {
            string tmp = Path.Combine(Path.GetTempPath(), $"rc_diskpart_{Guid.NewGuid():N}.txt");
            try
            {
                File.WriteAllText(tmp, script);
                var psi = RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("diskpart.exe", $"/s \"{tmp}\"");
                bool success = false;
                void capture(string line)
                {
                    reportOutput(line);
                    if (line.IndexOf("DiskPart successfully", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("DiskPart marked", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("successfully assigned", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("DiskPart successfully removed", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        success = true;
                    }
                }
                RunProcessAndReport(psi, capture, isCancelled);
                return success;
            }
            catch (Exception ex)
            {
                reportOutput($"Diskpart error: {ex.Message}");
                return false;
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }

        private static async Task<string> RunReagentcInfoCaptureAsync(CancellationToken cancellationToken)
        {
            var psi = RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("reagentc.exe", "/info");
            var sb = new StringBuilder();
            try
            {
                using var proc = new Process { StartInfo = psi };
                proc.Start();
                while (!proc.HasExited)
                {
                    var line = await proc.StandardOutput.ReadLineAsync();
                    if (line == null) break;
                    sb.AppendLine(line);
                    if (cancellationToken.IsCancellationRequested) break;
                }
                string err = await proc.StandardError.ReadToEndAsync();
                if (!string.IsNullOrEmpty(err)) sb.AppendLine(err);
            }
            catch { }
            return sb.ToString();
        }

        private static async Task<int?> FindVolumeNumberByLabelAsync(string label, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            string tmp = Path.Combine(Path.GetTempPath(), $"rc_diskpart_{Guid.NewGuid():N}.txt");
            try
            {
                File.WriteAllText(tmp, "list volume\r\n");
                var psi = RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("diskpart.exe", $"/s \"{tmp}\"");
                var sb = new StringBuilder();
                await RunProcessAndReportAsync(psi, (line) => { sb.AppendLine(line); }, cancellationToken);
                foreach (var raw in sb.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var line = raw.Trim();
                    if (line.IndexOf(label, StringComparison.OrdinalIgnoreCase) >= 0 && line.StartsWith("Volume", StringComparison.OrdinalIgnoreCase))
                    {
                        var m = Regex.Match(line, @"Volume\s+(\d+)", RegexOptions.IgnoreCase);
                        if (m.Success && int.TryParse(m.Groups[1].Value, out var vol))
                        {
                            return vol;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                reportOutput($"Failed to list volumes: {ex.Message}");
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
            return null;
        }

        private static async Task<bool> RunDiskpartScriptAsync(string script, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            string tmp = Path.Combine(Path.GetTempPath(), $"rc_diskpart_{Guid.NewGuid():N}.txt");
            try
            {
                File.WriteAllText(tmp, script);
                var psi = RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("diskpart.exe", $"/s \"{tmp}\"");
                bool success = false;
                await RunProcessAndReportAsync(psi, (line) =>
                {
                    reportOutput(line);
                    if (line.IndexOf("DiskPart successfully", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("successfully assigned", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("successfully removed", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        success = true;
                    }
                }, cancellationToken);
                return success;
            }
            catch (Exception ex)
            {
                reportOutput($"Diskpart error: {ex.Message}");
                return false;
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }

        private static char FindAvailableDriveLetter()
        {
            var used = new HashSet<char>();
            try
            {
                foreach (var di in DriveInfo.GetDrives())
                {
                    if (!string.IsNullOrEmpty(di.Name) && di.Name.Length >= 2)
                    {
                        var c = char.ToUpperInvariant(di.Name[0]);
                        used.Add(c);
                    }
                }
            }
            catch { }
            var prefs = new[] { 'R', 'Q', 'P', 'H', 'K' };
            foreach (var c in prefs)
            {
                if (!used.Contains(c)) return c;
            }
            for (char c = 'Z'; c >= 'D'; c--)
            {
                if (!used.Contains(c)) return c;
            }
            return 'R';
        }

        private async Task RunDismAsync(string args, Action<string> reportOutput, System.Threading.CancellationToken cancellationToken)
        {
            var psi = RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("dism.exe", args);
            await RunProcessAndReportAsync(psi, reportOutput, cancellationToken);
        }

        private static async Task RunProcessAndReportAsync(ProcessStartInfo psi, Action<string> reportOutput, System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                // Delegate to robust implementation in CoreUtilities
                await AsyncHelpers.RunProcessAsync(psi, 
                    output => reportOutput(output), 
                    error => reportOutput("ERROR: " + error), 
                    cancellationToken);
            }
            catch (Exception ex) { reportOutput($"Failed to run process {psi.FileName} {psi.Arguments}: {ex.Message}"); }
        }

        private static void ShowTextDialog(string title, string text)
        {
            var (form, box, _) = CreateThemedTextWindow(title);
            box.Text = text;
            form.ShowDialog();
        }

        private static (Form form, RichTextBox box, Action<string> append) CreateThemedTextWindow(string title)
        {
            var form = new Form
            {
                Text = title,
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = ResolveThemeColors().Surface,
                ForeColor = ResolveThemeColors().Text
            };

            var box = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                DetectUrls = true,
                BorderStyle = BorderStyle.None,
                BackColor = ResolveThemeColors().SurfaceVariant,
                ForeColor = ResolveThemeColors().Text,
                Font = new Font("Consolas", 10f)
            };

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                BackColor = ResolveThemeColors().Surface
            };
            panel.Controls.Add(box);
            form.Controls.Add(panel);

            Action<string> append = (line) =>
            {
                if (box.IsHandleCreated)
                {
                    box.BeginInvoke(new Action(() =>
                    {
                        box.AppendText(line + Environment.NewLine);
                        box.SelectionStart = box.TextLength;
                        box.ScrollToCaret();
                    }));
                }
            };

            return (form, box, append);
        }

        private static (Color Surface, Color SurfaceVariant, Color Text) ResolveThemeColors()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var themeType = asm.GetType("RecoveryCommander.UI.Theme");
                    if (themeType != null)
                    {
                        var colorsType = themeType.GetNestedType("Colors", BindingFlags.Public | BindingFlags.Static);
                        if (colorsType != null)
                        {
                            var surface = (Color)(colorsType.GetProperty("Surface")?.GetValue(null) ?? Color.FromArgb(30, 30, 45));
                            var surfaceVariant = (Color)(colorsType.GetProperty("SurfaceVariant")?.GetValue(null) ?? Color.FromArgb(25, 25, 40));
                            var text = (Color)(colorsType.GetProperty("Text")?.GetValue(null) ?? Color.FromArgb(230, 240, 255));
                            return (surface, surfaceVariant, text);
                        }
                    }
                }
            }
            catch { }
            return (Color.FromArgb(30, 30, 45), Color.FromArgb(25, 25, 40), Color.FromArgb(230, 240, 255));
        }

        private static void AutoCloseWithGrace(Form form, int seconds, Action<string>? append)
        {
            var remaining = seconds;
            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (s, e) =>
            {
                remaining--;
                if (remaining <= 0)
                {
                    timer.Stop();
                    timer.Dispose();
                    if (!form.IsDisposed) form.Close();
                }
            };
            void cancel()
            {
                if (timer.Enabled)
                {
                    timer.Stop();
                    timer.Dispose();
                    append?.Invoke("Auto-close cancelled. You can close the window when ready.");
                }
            }
            form.MouseMove += (_, __) => cancel();
            form.KeyDown += (_, __) => cancel();
            form.FormClosed += (_, __) => { if (timer.Enabled) { timer.Stop(); timer.Dispose(); } };
            timer.Start();
        }
    }
}
