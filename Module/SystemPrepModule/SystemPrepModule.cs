// AUDIT MARKER: SystemPrepModule.cs | Last updated: 2025-09-05 14:50 BST
// CHANGELOG:
// - Replaced simple SystemPrepModule with full IRecoveryModule implementation
// - Reused process streaming pattern from SfcModule
// - Safe file deletions with error handling
// - P/Invoke to empty Recycle Bin
// - Added detailed comments and structured output reporting

using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using SystemPrepModule;
using System.Diagnostics;
using System.Net.Http;
using System.IO;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;

namespace RecoveryCommander.Module
{
    [RecoveryModuleAttribute("SystemPrepModule", "1.0.0")]
    [SupportedOSPlatform("windows")]
    public class SystemPrepModule : IRecoveryModule
    {
        // Cache directory arrays - moved from CoreUtilities since they were removed
        private static readonly string[] CHROME_CACHE_DIRS = new[]
        {
            @"%LOCALAPPDATA%\Google\Chrome\User Data\Default\Cache",
            @"%LOCALAPPDATA%\Google\Chrome\User Data\Default\Code Cache",
            @"%LOCALAPPDATA%\Google\Chrome\User Data\Default\GPUCache"
        };

        private static readonly string[] EDGE_CACHE_DIRS = new[]
        {
            @"%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Cache",
            @"%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Code Cache",
            @"%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\GPUCache"
        };

        // Ensure only one action from this module runs at a time (prevent concurrent/ticked option execution)
        private static readonly object _actionExecutionLock = new object();

        // Use shared HttpClient from ServiceContainer to avoid socket exhaustion
        private static HttpClient GetHttpClient() => RecoveryCommander.Core.ServiceContainer.GetHttpClient();

        public string Name => "System Prep";
        public string Description => "Performs various system preparation and cleanup tasks.";

        // Defined actions for system preparation module
        public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
        {
            // Header for maintenance group (non-actionable)
            new ModuleAction(string.Empty, "Maintenance") { IsHeader = true, AutoTick = false },

            // Maintenance group
            new ModuleAction("Scan for Windows Updates", "Trigger Windows Update scan/install (usoclient)") { ExecuteAction = ScanForWindowsUpdatesAsync },
            new ModuleAction("Upgrade winget packages", "Run winget upgrade --all to upgrade installed packages") { ExecuteAction = UpgradeWingetPackagesAsync },
            new ModuleAction("Update Store app", "Check Microsoft Store and update all available Store apps") { ExecuteAction = UpdateStoreAppsAsync },
            new ModuleAction("Backup Drivers", "Export all system drivers to D:\\Drivers folder") { ExecuteAction = BackupDriversAsync },

            // Header for optimization/cleanup group (non-actionable)
            new ModuleAction(string.Empty, "Optimization") { IsHeader = true, AutoTick = false },

            // Optimization and cleanup actions
            new ModuleAction("Clear Chrome Cache", "Clear Chrome Cache (preserve cookies, passwords, site settings)") { IsDestructive = true, ExecuteAction = ClearChromeCacheAsync },
            new ModuleAction("Clear Edge Cache", "Clear Edge Cache (preserve cookies, passwords, site settings)") { IsDestructive = true, ExecuteAction = ClearEdgeCacheAsync },
            new ModuleAction("Delete Temp Files", "Delete temporary files") { IsDestructive = true, ExecuteAction = DeleteTempFilesAsync },
            new ModuleAction("Compact OS", "Compact OS (Compact.exe)") { IsDestructive = true, ExecuteAction = RunCompactOSAsync },
            new ModuleAction("Run Disk Cleanup", "Run Disk Cleanup & component cleanup") { IsDestructive = true, ExecuteAction = RunDiskCleanupAsync },
            new ModuleAction("Optimize Drives", "Optimize /defragment drives") { ExecuteAction = OptimizeDrivesAsync },
            new ModuleAction("Clear Prefetch", "Clear Windows Prefetch") { IsDestructive = true, ExecuteAction = ClearPrefetchAsync },
            new ModuleAction("Empty Recycle Bin", "Empty Recycle Bin") { IsDestructive = true, ExecuteAction = EmptyRecycleBinAsync },
            new ModuleAction("Clean Windows Update", "Clean up Windows Update files") { IsDestructive = true, ExecuteAction = CleanWindowsUpdateAsync },
            
            // New: System Tweaks action
            new ModuleAction("ApplySystemTweaks", "Apply System Tweaks (Windows 11)") { IsHeader = false, AutoTick = false, Highlight = true, ExecuteAction = ApplySystemTweaksAsync }
        };

        public string Version => "1.0.0";
        public string HealthStatus => "Healthy";
        public string BuildInfo => "SystemPrep Module v1.0.0 - System Preparation and Cleanup";
        public bool SupportsAsync => true;


        private static void ShowWingetToast(string message)
        {
            try
            {
                if (Application.OpenForms.Count == 0)
                {
                    return;
                }

                var owner = Application.OpenForms[0]!;
                if (owner == null)
                {
                    return;
                }

                void Show()
                {
                    try
                    {
                        var ownerType = owner.GetType();
                        var getEnhanced = ownerType.GetMethod("GetEnhancedProgressSystem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (getEnhanced == null)
                            return;

                        var enhancedInstance = getEnhanced.Invoke(owner, null);
                        if (enhancedInstance == null)
                            return;

                        var epsType = enhancedInstance.GetType();
                        var notifType = epsType.Assembly.GetType("RecoveryCommander.UI.NotificationType");
                        var showNotification = epsType.GetMethod("ShowNotification", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (notifType == null || showNotification == null)
                            return;

                        var infoValue = Enum.Parse(notifType, "Info");
                        showNotification.Invoke(enhancedInstance, new object[] { "Winget Updates", message, infoValue, 5000 });
                    }
                    catch
                    {
                        // Ignore toast failures; they are non-critical.
                    }
                }

                if (owner.IsHandleCreated && owner.InvokeRequired)
                {
                    owner.BeginInvoke(new Action(Show));
                }
                else
                {
                    Show();
                }
            }
            catch
            {
                // Swallow all exceptions from toast logic to avoid impacting module execution.
            }
        }

        private static Task<List<UpdateHelpers.WingetUpgradeItem>> ShowWingetSelectorAsync(List<UpdateHelpers.WingetUpgradeItem> upgrades)
        {
            var tcs = new TaskCompletionSource<List<UpdateHelpers.WingetUpgradeItem>>();

            try
            {
                if (Application.OpenForms.Count == 0)
                {
                    // No main form available; fall back to selecting all upgrades.
                    tcs.SetResult(upgrades);
                    return tcs.Task;
                }

                var owner = Application.OpenForms[0]!;

                void Show()
                {
                    try
                    {
                        var columns = new List<DataGridViewColumn>
                        {
                            new DataGridViewTextBoxColumn { HeaderText = "Name", FillWeight = 35, ReadOnly = true },
                            new DataGridViewTextBoxColumn { HeaderText = "Id", FillWeight = 25, ReadOnly = true },
                            new DataGridViewTextBoxColumn { HeaderText = "Installed", FillWeight = 15, ReadOnly = true },
                            new DataGridViewTextBoxColumn { HeaderText = "Available", FillWeight = 15, ReadOnly = true },
                            new DataGridViewTextBoxColumn { HeaderText = "Source", FillWeight = 10, ReadOnly = true }
                        };

                        using (var selector = new UpdateSelectorForm<UpdateHelpers.WingetUpgradeItem>(
                            "Select winget updates",
                            upgrades,
                            columns,
                            item => new object[] { item.Name, item.Id, item.InstalledVersion, item.AvailableVersion, item.Source }))
                        {
                            var result = selector.ShowDialog(owner);
                            if (result == DialogResult.OK)
                            {
                                tcs.SetResult(selector.SelectedItems.ToList());
                            }
                            else
                            {
                                tcs.SetResult(new List<UpdateHelpers.WingetUpgradeItem>());
                            }
                        }
                    }
                    catch
                    {
                        // If anything goes wrong, default to all upgrades so we at least continue.
                        if (!tcs.Task.IsCompleted)
                        {
                            tcs.SetResult(upgrades);
                        }
                    }
                }

                if (owner.InvokeRequired)
                {
                    owner.BeginInvoke(new Action(Show));
                }
                else
                {
                    Show();
                }
            }
            catch
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.SetResult(upgrades);
                }
            }

            return tcs.Task;
        }

        private static void ShowGenericToast(string title, string message)
        {
            try
            {
                if (Application.OpenForms.Count == 0)
                {
                    return;
                }

                var owner = Application.OpenForms[0]!;
                if (owner == null)
                {
                    return;
                }

                void Show()
                {
                    try
                    {
                        var ownerType = owner.GetType();
                        var getEnhanced = ownerType.GetMethod("GetEnhancedProgressSystem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (getEnhanced == null)
                            return;

                        var enhancedInstance = getEnhanced.Invoke(owner, null);
                        if (enhancedInstance == null)
                            return;

                        var epsType = enhancedInstance.GetType();
                        var notifType = epsType.Assembly.GetType("RecoveryCommander.UI.NotificationType");
                        var showNotification = epsType.GetMethod("ShowNotification", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (notifType == null || showNotification == null)
                            return;

                        var infoValue = Enum.Parse(notifType, "Info");
                        showNotification.Invoke(enhancedInstance, new object[] { title, message, infoValue, 5000 });
                    }
                    catch
                    {
                    }
                }

                if (owner.IsHandleCreated && owner.InvokeRequired)
                {
                    owner.BeginInvoke(new Action(Show));
                }
                else
                {
                    Show();
                }
            }
            catch
            {
            }
        }

        private static Task<List<UpdateHelpers.WindowsUpdateItem>> ShowWindowsUpdateSelectorAsync(List<UpdateHelpers.WindowsUpdateItem> updates)
        {
            var tcs = new TaskCompletionSource<List<UpdateHelpers.WindowsUpdateItem>>();

            try
            {
                if (Application.OpenForms.Count == 0)
                {
                    tcs.SetResult(updates);
                    return tcs.Task;
                }

                var owner = Application.OpenForms[0]!;

                void Show()
                {
                    try
                    {
                        var columns = new List<DataGridViewColumn>
                        {
                            new DataGridViewTextBoxColumn { HeaderText = "Title", FillWeight = 45, ReadOnly = true },
                            new DataGridViewTextBoxColumn { HeaderText = "KB", FillWeight = 20, ReadOnly = true },
                            new DataGridViewTextBoxColumn { HeaderText = "Category", FillWeight = 20, ReadOnly = true }
                        };

                        using (var selector = new UpdateSelectorForm<UpdateHelpers.WindowsUpdateItem>(
                            "Select Windows Updates",
                            updates,
                            columns,
                            item => new object[] { item.Title, item.KBArticle, item.Category }))
                        {
                            var result = selector.ShowDialog(owner);
                            if (result == DialogResult.OK)
                            {
                                tcs.SetResult(selector.SelectedItems.ToList());
                            }
                            else
                            {
                                tcs.SetResult(new List<UpdateHelpers.WindowsUpdateItem>());
                            }
                        }
                    }
                    catch
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            tcs.SetResult(updates);
                        }
                    }
                }

                if (owner.InvokeRequired)
                {
                    owner.BeginInvoke(new Action(Show));
                }
                else
                {
                    Show();
                }
            }
            catch
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.SetResult(updates);
                }
            }

            return tcs.Task;
        }

        private void ClearChromeCache(Action<string> reportOutput, Func<bool> isCancelled)
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var chromeBase = Path.Combine(localApp, "Google", "Chrome", "User Data");
            if (!Directory.Exists(chromeBase))
            {
                reportOutput("Chrome user data folder not found.");
                return;
            }

            foreach (var profile in Directory.EnumerateDirectories(chromeBase))
            {
                if (isCancelled()) return;
                // Skip Local State & system files - target cache folders only
                foreach (var d in CHROME_CACHE_DIRS)
                {
                    var path = Path.Combine(profile, d);
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            DeleteDirectoryContents(path, reportOutput, isCancelled);
                            reportOutput($"Cleared Chrome cache directory: {path}");
                        }
                    }
                    catch (Exception ex)
                    {
                        reportOutput($"Failed to clear {path}: {ex.Message}");
                    }
                }
            }
        }

        private void ClearEdgeCache(Action<string> reportOutput, Func<bool> isCancelled)
        {
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var edgeBase = Path.Combine(localApp, "Microsoft", "Edge", "User Data");
            if (!Directory.Exists(edgeBase))
            {
                reportOutput("Edge user data folder not found.");
                return;
            }

            foreach (var profile in Directory.EnumerateDirectories(edgeBase))
            {
                if (isCancelled()) return;
                foreach (var d in EDGE_CACHE_DIRS)
                {
                    var path = Path.Combine(profile, d);
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            DeleteDirectoryContents(path, reportOutput, isCancelled);
                            reportOutput($"Cleared Edge cache directory: {path}");
                        }
                    }
                    catch (Exception ex)
                    {
                        reportOutput($"Failed to clear {path}: {ex.Message}");
                    }
                }
            }
        }

        private void DeleteTempFiles(Action<string> reportOutput, Func<bool> isCancelled)
        {
            var paths = new List<string>
            {
                Path.GetTempPath(),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
            };

            foreach (var p in paths)
            {
                if (isCancelled()) return;
                if (!Directory.Exists(p)) { reportOutput($"Temp folder not found: {p}"); continue; }
                reportOutput($"Cleaning temp folder: {p}");
                DeleteDirectoryContents(p, reportOutput, isCancelled);
            }
        }

        private void RunDiskCleanup(Action<string> reportOutput, Func<bool> isCancelled)
        {
            // Run DISM component cleanup first (non-interactive)
            RunProcessAndReport(RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("dism", "/Online /Cleanup-Image /StartComponentCleanup"), reportOutput, isCancelled);
            // Attempt to run cleanmgr sagerun; note this may require prior sageset configuration. We'll still invoke it for best-effort.
            RunProcessAndReport(RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("cleanmgr.exe", "/sagerun:1"), reportOutput, isCancelled);
        }

        private void OptimizeDrives(Action<string> reportOutput, Func<bool> isCancelled)
        {
            // Use defrag /O to optimize all volumes; iterate logical drives
            foreach (var drv in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
            {
                if (isCancelled()) return;
                var letter = drv.Name.TrimEnd('\\');
                reportOutput($"Optimizing drive: {letter}");
                RunProcessAndReport(RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("defrag", $"{letter} /O"), reportOutput, isCancelled);
            }
        }

        private void CleanWindowsUpdate(Action<string> reportOutput, Func<bool> isCancelled)
        {
            try
            {
                reportOutput("Stopping Windows Update service...");
                RunProcessAndReport(RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("net", "stop wuauserv"), reportOutput, isCancelled);

                var sd = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");
                if (Directory.Exists(sd))
                {
                    DeleteDirectoryContents(sd, reportOutput, isCancelled);
                    reportOutput($"Cleared Windows Update download cache: {sd}");
                }

                reportOutput("Starting Windows Update service...");
                RunProcessAndReport(RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("net", "start wuauserv"), reportOutput, isCancelled);
            }
            catch (Exception ex)
            {
                reportOutput($"Failed to clean Windows Update: {ex.Message}");
            }
        }

        private void ClearPrefetch(Action<string> reportOutput, Func<bool> isCancelled)
        {
            var prefetch = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
            if (!Directory.Exists(prefetch)) { reportOutput("Prefetch folder not found."); return; }
            DeleteDirectoryContents(prefetch, reportOutput, isCancelled);
            reportOutput("Prefetch cleared.");
        }

        private void EmptyRecycleBin(Action<string> reportOutput)
        {
            try
            {
                // Flags: 0x00000001 SHERB_NOCONFIRMATION, 0x00000002 SHERB_NOPROGRESSUI, 0x00000004 SHERB_NOSOUND
                const uint flags = 0x00000001 | 0x00000002 | 0x00000004;
                var hr = SHEmptyRecycleBin(IntPtr.Zero, null, flags);
                reportOutput(hr == 0 ? "Recycle Bin emptied." : $"Recycle Bin operation returned: 0x{hr:X}");
            }
            catch (Exception ex)
            {
                reportOutput($"Failed to empty Recycle Bin: {ex.Message}");
            }
        }

        private void ScanForWindowsUpdates(Action<string> reportOutput, Func<bool> isCancelled)
        {
            try
            {
                reportOutput("Triggering Windows Update scan (usoclient StartScan)...");

                RunProcessAndReport(RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("usoclient.exe", "StartScan"), reportOutput, isCancelled);

                if (isCancelled()) return;

                reportOutput("Triggering Windows Update install (usoclient StartInstall)...");

                RunProcessAndReport(RecoveryCommander.Core.CoreUtilities.CreateProcessInfo("usoclient.exe", "StartInstall"), reportOutput, isCancelled);

                reportOutput("Windows Update scan/install triggered.");
            }
            catch (Exception ex)
            {
                reportOutput($"Failed to trigger Windows Update scan/install: {ex.Message}");
            }
        }

        private void UpgradeWingetPackages(Action<string> reportOutput, Func<bool> isCancelled)
        {
            try
            {
                reportOutput("Running winget upgrade --all --accept-source-agreements --accept-package-agreements...");

                // Try to locate winget binary first (where/known paths)
                var wingetPath = FindWingetExecutable(reportOutput);
                var exe = string.IsNullOrEmpty(wingetPath) ? "winget" : wingetPath;
                RunProcessAndReport(new ProcessStartInfo(exe, "upgrade --all --accept-source-agreements --accept-package-agreements") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true }, reportOutput, isCancelled);
                reportOutput("winget upgrade completed.");
            }
            catch (Exception ex)
            {
                reportOutput($"Failed to run winget upgrade: {ex.Message}");
                // If winget is missing, attempt to download and install latest Desktop App Installer from GitHub then retry
                if (ex.Message.Contains("The system cannot find the file specified") || ex is System.ComponentModel.Win32Exception)
                {
                    reportOutput("winget not found — attempting to download and install App Installer from GitHub releases...");
                    try
                    {
                        // Prefer using the lightweight installer script if available (community script)
                        InstallWingetUsingScript(reportOutput, isCancelled);
                        // fallback to GitHub release method if script install didn't make winget available
                        if (FindWingetExecutable(reportOutput) == null)
                        {
                            InstallLatestWingetFromGitHub(reportOutput, isCancelled);
                        }
                        if (isCancelled()) return;
                        reportOutput("Retrying winget upgrade after installing App Installer...");
                        var wingetPath2 = FindWingetExecutable(reportOutput) ?? "winget";
                        RunProcessAndReport(new ProcessStartInfo(wingetPath2, "upgrade --all --accept-source-agreements --accept-package-agreements") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true }, reportOutput, isCancelled);
                    }
                    catch (Exception iex)
                    {
                        reportOutput($"Failed to install winget/App Installer or retry upgrade: {iex.Message}");
                    }
                }
            }
        }

        private async Task UpdateStoreApps(Action<string> reportOutput, Func<bool> isCancelled)
        {
            try
            {
                reportOutput("Attempting to update Microsoft Store apps using PowerShell module or Appx re-registration...");

                // Build the PowerShell script from lines to avoid complex escaping issues
                var psLines = new[]
                {
                    "try {",
                    "$ErrorActionPreference = 'Stop'",
                    "# Try common Store PowerShell modules/cmdlets (best-effort)",
                    "$updated = $false",
                    "foreach ($mod in @('Microsoft.Store','MicrosoftStore','Store')) {",
                    "  try { Import-Module $mod -ErrorAction SilentlyContinue } catch {}",
                    "}","if (Get-Command -Name Update-Store* -ErrorAction SilentlyContinue) {",
                    "  Write-Output 'Found Store update cmdlet. Running Update-Store* cmdlets...'",
                    "  Get-Command -Name Update-Store* | ForEach-Object { & $_.Source }",
                    "  $updated = $true",
                    "}","# Fallback: re-register Appx packages for non-inbox packages to trigger repairs/updates",
                    "if (-not $updated) {",
                    "  Write-Output 'Falling back to re-registering Appx packages (may repair/update Store apps)...'","  $pkgs = Get-AppxPackage -AllUsers | Where-Object { $_.InstallLocation -and (Test-Path $_.InstallLocation) }",
                    "  foreach ($p in $pkgs) {",
                    "    Write-Output ('Re-registering: ' + $p.Name + ' (' + $p.PackageFullName + ')')",
                    "    try {",
                    "      $manifest = Join-Path $p.InstallLocation 'AppxManifest.xml'",
                    "      if (Test-Path $manifest) { Add-AppxPackage -Register $manifest -DisableDevelopmentMode -ErrorAction Stop | ForEach-Object { Write-Output $_ } }",
                    "      else { Write-Output ('No manifest at ' + $manifest + '; skipping.') }",
                    "    } catch { Write-Output ('Re-register failed for ' + $p.PackageFullName + ': ' + $_.Exception.Message) }",
                    "  }",
                    "}","} catch {",
                    "  Write-Output ('PowerShell Store update script failed: ' + $_.Exception.Message)",
                    "  exit 1",
                    "}"
                };

                var psScript = string.Join(Environment.NewLine, psLines);

                // Write script to a temporary file to avoid argument escaping issues
                var tempScript = Path.Combine(Path.GetTempPath(), $"rc_store_update_{Guid.NewGuid():N}.ps1");
                try
                {
                    await AsyncHelpers.WriteAllTextAsync(tempScript, psScript, CancellationToken.None);

                    // Validate temp script path
                    if (!RecoveryCommander.Core.SecurityHelpers.IsValidFilePath(tempScript, out var validatedTempScript))
                    {
                        reportOutput("Invalid script path - security validation failed.");
                        return;
                    }
                    
                    // Ensure the file exists
                    if (!File.Exists(validatedTempScript))
                    {
                        reportOutput($"Script file not found: {validatedTempScript}");
                        return;
                    }
                    
                    var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{validatedTempScript}\"")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    await RunProcessAndReportAsync(psi, reportOutput, CancellationToken.None);
                }
                finally
                {
                    try { 
                        if (await AsyncHelpers.FileExistsAsync(tempScript, CancellationToken.None))
                            await AsyncHelpers.DeleteFileAsync(tempScript, CancellationToken.None); 
                    } catch { }
                }

                reportOutput("Store update script completed (see output for detailed results).");
            }
            catch (Exception ex)
            {
                reportOutput($"Failed to run PowerShell Store update: {ex.Message}");
            }
        }

        private string? FindWingetExecutable(Action<string> reportOutput)
        {
            try
            {
                // Try 'where' to locate winget on PATH
                var psi = new ProcessStartInfo("cmd.exe", "/c where winget") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
                using var p = Process.Start(psi);
                if (p == null) return null;
                var outp = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(2000);
                if (!string.IsNullOrWhiteSpace(outp))
                {
                    var first = outp.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    reportOutput($"Found winget at: {first}");
                    return first;
                }

                // Common WindowsApps location (may not be accessible); try WindowsApps alias via PATH locations
                var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var candidate = Path.Combine(localApp, "Microsoft", "WindowsApps", "winget.exe");
                if (File.Exists(candidate)) { reportOutput($"Found winget at: {candidate}"); return candidate; }

                // Try Program Files WindowsApps scan (requires permission) - best-effort, do not throw
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var wapps = Path.Combine(programFiles, "WindowsApps");
                if (Directory.Exists(wapps))
                {
                    var files = Directory.EnumerateFiles(wapps, "winget.exe", SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrEmpty(files)) { reportOutput($"Found winget at: {files}"); return files; }
                }

                reportOutput("winget executable not found on PATH or common locations.");
            }
            catch (Exception ex)
            {
                reportOutput($"FindWingetExecutable error: {ex.Message}");
            }
            return null;
        }

        private void InstallWingetUsingScript(Action<string> reportOutput, Func<bool> isCancelled)
        {
            var tempScript = Path.Combine(Path.GetTempPath(), $"winget-install_{Guid.NewGuid():N}.ps1");
            try
            {
                reportOutput("Downloading winget installer script...");

                // Use synchronous download since the caller expects this to block (it was async void before, causing a race)
                var http = ServiceContainer.GetHttpClient();
                using (var response = http.GetAsync("https://raw.githubusercontent.com/asheroto/winget-installer/master/winget-install.ps1").GetAwaiter().GetResult())
                {
                    response.EnsureSuccessStatusCode();
                    using var fs = new FileStream(tempScript, FileMode.Create, FileAccess.Write, FileShare.None);
                    response.Content.CopyToAsync(fs).GetAwaiter().GetResult();
                }

                reportOutput("Executing installer script...");
                var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\"")
                {
                    UseShellExecute = true,
                    Verb = "runAs"
                };

                using var p = Process.Start(psi);
                if (p != null)
                {
                    p.WaitForExit();
                    reportOutput($"Installer script exit code: {p.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                reportOutput($"Installer script failed: {ex.Message}");
            }
            finally
            {
                try { if (File.Exists(tempScript)) File.Delete(tempScript); } catch { }
            }
        }

        private void InstallLatestWingetFromGitHub(Action<string> reportOutput, Func<bool> isCancelled)
        {
            // Existing GitHub release download/install fallback (kept for completeness)
            try
            {
                // Use the shared HttpClient
                var http = GetHttpClient();
                var apiUrl = "https://api.github.com/repos/microsoft/winget-cli/releases/latest";
                reportOutput($"Querying GitHub for latest winget release: {apiUrl}");
                using var resp = http.GetAsync(apiUrl).GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode)
                {
                    reportOutput($"GitHub query failed: {resp.StatusCode}");
                    return;
                }

                using var s = resp.Content.ReadAsStream();
                using var doc = JsonDocument.Parse(s);
                var root = doc.RootElement;
                if (!root.TryGetProperty("assets", out var assets))
                {
                    reportOutput("No assets in release JSON.");
                    return;
                }

                string? downloadUrl = null;
                string? assetName = null;
                foreach (var a in assets.EnumerateArray())
                {
                    if (!a.TryGetProperty("browser_download_url", out var bd)) continue;
                    if (!a.TryGetProperty("name", out var nm)) continue;
                    var name = nm.GetString() ?? string.Empty;
                    var url = bd.GetString() ?? string.Empty;
                    if (name.Contains("msixbundle", StringComparison.OrdinalIgnoreCase) || name.Contains("msix", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = url;
                        assetName = name;
                        break;
                    }
                    if (name.Contains("appxbundle", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        if (downloadUrl == null) { downloadUrl = url; assetName = name; }
                    }
                }

                if (downloadUrl == null)
                {
                    reportOutput("No suitable App Installer asset found in latest release.");
                    return;
                }

                reportOutput($"Found asset to download: {assetName}");

                var tempFile = Path.Combine(Path.GetTempPath(), assetName!);
                reportOutput($"Downloading to: {tempFile}");
                using (var dl = http.GetAsync(downloadUrl).GetAwaiter().GetResult())
                {
                    dl.EnsureSuccessStatusCode();
                    using var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write);
                    dl.Content.CopyToAsync(fs).GetAwaiter().GetResult();
                }

                if (isCancelled()) return;

                reportOutput("Download complete. Launching installer with elevation (UAC may be required)...");

                var psi = new ProcessStartInfo("powershell.exe")
                {
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Add-AppxPackage -Path '{tempFile}'\"",
                    UseShellExecute = true,
                    Verb = "runAs"
                };
                try
                {
                    using var p = Process.Start(psi);
                    if (p != null) p.WaitForExit();
                    reportOutput($"Installer exit code: {p?.ExitCode}");
                }
                catch (Exception ex)
                {
                    reportOutput($"Failed to start installer elevated: {ex.Message}");
                }
                try { File.Delete(tempFile); } catch { }
            }
            catch (Exception ex)
            {
                reportOutput($"InstallLatestWingetFromGitHub failed: {ex.Message}");
            }
        }

        private static void SafeDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                    File.Delete(filePath);
                }
            }
            catch
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.SetAttributes(filePath, FileAttributes.Normal);
                        File.Delete(filePath);
                    }
                }
                catch { }
            }
        }

        private static async Task SafeDeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await AsyncHelpers.FileExistsAsync(filePath, cancellationToken))
                {
                    await Task.Run(() => File.SetAttributes(filePath, FileAttributes.Normal), cancellationToken);
                    await AsyncHelpers.DeleteFileAsync(filePath, cancellationToken);
                }
            }
            catch
            {
                try
                {
                    if (await AsyncHelpers.FileExistsAsync(filePath, cancellationToken))
                    {
                        await Task.Run(() => File.SetAttributes(filePath, FileAttributes.Normal), cancellationToken);
                        await AsyncHelpers.DeleteFileAsync(filePath, cancellationToken);
                    }
                }
                catch { }
            }
        }

        private static void SafeDeleteDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
            }
            catch
            {
                try
                {
                    if (Directory.Exists(directoryPath))
                    {
                        Directory.Delete(directoryPath, true);
                    }
                }
                catch { }
            }
        }

        // Delete contents of a directory, with cancellation support
        private static void DeleteDirectoryContents(string path, Action<string> reportOutput, Func<bool> isCancelled)
        {
            try
            {
                var dir = new DirectoryInfo(path);
                foreach (var file in dir.GetFiles())
                {
                    if (isCancelled()) return;
                    try
                    {
                        // attempt to recycle first, then fallback to permanent delete
                        SafeDeleteFile(file.FullName);
                    }
                    catch (Exception ex) { reportOutput($"Failed to delete file {file.FullName}: {ex.Message}"); }
                }
                foreach (var sub in dir.GetDirectories())
                {
                    if (isCancelled()) return;
                    try
                    {
                        SafeDeleteDirectory(sub.FullName);
                    }
                    catch (Exception ex) { reportOutput($"Failed to delete folder {sub.FullName}: {ex.Message}"); }
                }
            }
            catch (Exception ex)
            {
                reportOutput($"Error deleting contents of {path}: {ex.Message}");
            }
        }

        // Use consolidated RunProcessAndReport from Core/AsyncHelpers
        private static void RunProcessAndReport(ProcessStartInfo psi, Action<string> reportOutput, Func<bool> isCancelled)
        {
            RecoveryCommander.Core.AsyncHelpers.RunProcessAndReport(psi, reportOutput, isCancelled);
        }

        private static async Task RunProcessAndReportAsync(ProcessStartInfo psi, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            try
            {
                // Use AsyncHelpers for proper async execution
                await AsyncHelpers.RunProcessAsync(psi, reportOutput, null, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                reportOutput("Process cancelled by user.");
                throw;
            }
            catch (Exception ex)
            {
                reportOutput($"Failed to run process {psi.FileName} {psi.Arguments}: {ex.Message}");
                throw;
            }
        }

        // P/Invoke signature for emptying the Recycle Bin
        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        // Runs the Tweaks.ps1 script from Resources
        private void ApplySystemTweaks(Action<string> reportOutput, Func<bool> isCancelled, Action<int, string> reportProgress)
        {
            try
            {
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Tweaks.ps1");
                if (!File.Exists(scriptPath))
                {
                    // Fallback to module directory if not found in Resources
                    scriptPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Tweaks.ps1");
                    if (!File.Exists(scriptPath))
                    {
                        reportOutput($"Script not found: Tweaks.ps1");
                        reportProgress(100, "Failed");
                        return;
                    }
                }

                reportProgress(10, "Starting System Tweaks script...");
                reportOutput($"Executing script: {scriptPath}");

                // Validate script path to prevent path traversal
                if (!RecoveryCommander.Core.SecurityHelpers.IsValidFilePath(scriptPath, out var validatedScriptPath))
                {
                    reportOutput("Invalid script path - security validation failed.");
                    reportProgress(100, "Error");
                    return;
                }
                
                // Ensure the file exists
                if (!File.Exists(validatedScriptPath))
                {
                    reportOutput($"Script file not found: {validatedScriptPath}");
                    reportProgress(100, "Error");
                    return;
                }
                
                // Only allow .ps1 files
                if (!validatedScriptPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                {
                    reportOutput("Only PowerShell (.ps1) scripts are allowed.");
                    reportProgress(100, "Error");
                    return;
                }
                
                var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{validatedScriptPath}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                RunProcessAndReport(psi, reportOutput, isCancelled);
                
                reportProgress(100, "Completed");
            }
            catch (Exception ex)
            {
                reportOutput($"Error applying tweaks: {ex.Message}");
                reportProgress(100, "Error");
            }
        }


        private async Task BackupDriversAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(5, "Initializing driver backup..."));
            
            // Re-use the diagnostic-heavy BackupDrivers logic but wrap it in a Task for async consistency
            await Task.Run(() => BackupDrivers(reportOutput, () => cancellationToken.IsCancellationRequested), cancellationToken);
            
            progress?.Report(new ProgressReport(100, "Driver backup attempt finished."));
        }

        private async Task ClearChromeCacheAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(10, "Clearing Chrome cache..."));
            try
            {
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var chromeBase = Path.Combine(localApp, "Google", "Chrome", "User Data");
                if (!Directory.Exists(chromeBase))
                {
                    reportOutput("Chrome user data folder not found.");
                    return;
                }

                int profileCount = 0;
                foreach (var profile in Directory.EnumerateDirectories(chromeBase))
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    foreach (var d in CHROME_CACHE_DIRS)
                    {
                        var path = Path.Combine(profile, d);
                        try
                        {
                            if (Directory.Exists(path))
                            {
                                await Task.Run(() => DeleteDirectoryContents(path, reportOutput, () => cancellationToken.IsCancellationRequested), cancellationToken);
                                reportOutput($"Cleared Chrome cache directory: {path}");
                                profileCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            reportOutput($"Failed to clear {path}: {ex.Message}");
                        }
                    }
                }
                progress?.Report(new ProgressReport(90, $"Cleared cache for {profileCount} Chrome profiles"));
            }
            catch (Exception ex)
            {
                reportOutput($"Error clearing Chrome cache: {ex.Message}");
                throw;
            }
        }

        private async Task ClearEdgeCacheAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(10, "Clearing Edge cache..."));
            try
            {
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var edgeBase = Path.Combine(localApp, "Microsoft", "Edge", "User Data");
                if (!Directory.Exists(edgeBase))
                {
                    reportOutput("Edge user data folder not found.");
                    return;
                }

                int profileCount = 0;
                foreach (var profile in Directory.EnumerateDirectories(edgeBase))
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    foreach (var d in EDGE_CACHE_DIRS)
                    {
                        var path = Path.Combine(profile, d);
                        try
                        {
                            if (Directory.Exists(path))
                            {
                                await Task.Run(() => DeleteDirectoryContents(path, reportOutput, () => cancellationToken.IsCancellationRequested), cancellationToken);
                                reportOutput($"Cleared Edge cache directory: {path}");
                                profileCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            reportOutput($"Failed to clear {path}: {ex.Message}");
                        }
                    }
                }
                progress?.Report(new ProgressReport(90, $"Cleared cache for {profileCount} Edge profiles"));
            }
            catch (Exception ex)
            {
                reportOutput($"Error clearing Edge cache: {ex.Message}");
                throw;
            }
        }

        private async Task DeleteTempFilesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(10, "Deleting temporary files..."));
            try
            {
                var tempPath = Path.GetTempPath();
                var files = await Task.Run(() => Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories), cancellationToken);
                int deletedCount = 0;
                
                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    try
                    {
                        await Task.Run(() => CoreUtilities.SafeDeleteFile(file), cancellationToken);
                        deletedCount++;
                        if (deletedCount % 10 == 0)
                        {
                            progress?.Report(new ProgressReport(Math.Min(90, 10 + (deletedCount * 80 / files.Length)), $"Deleted {deletedCount}/{files.Length} temp files"));
                        }
                    }
                    catch (Exception ex)
                    {
                        reportOutput($"Failed to delete {file}: {ex.Message}");
                    }
                }
                
                progress?.Report(new ProgressReport(90, $"Deleted {deletedCount} temporary files"));
            }
            catch (Exception ex)
            {
                reportOutput($"Error deleting temp files: {ex.Message}");
                throw;
            }
        }

        private async Task RunCompactOSAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(10, "Running Compact OS..."));
            try
            {
                var psi = new ProcessStartInfo("compact", "/CompactOS:always")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                await RunProcessAndReportAsync(psi, progress!, reportOutput, cancellationToken);
            }
            catch (Exception ex)
            {
                reportOutput($"Error running Compact OS: {ex.Message}");
                throw;
            }
        }

        private async Task RunDiskCleanupAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(10, "Running disk cleanup..."));
            try
            {
                await Task.Run(() => RunDiskCleanup(reportOutput, () => cancellationToken.IsCancellationRequested), cancellationToken);
            }
            catch (Exception ex)
            {
                reportOutput($"Error running disk cleanup: {ex.Message}");
                throw;
            }
        }

        private async Task OptimizeDrivesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(10, "Optimizing drives..."));
            try
            {
                await Task.Run(() => OptimizeDrives(reportOutput, () => cancellationToken.IsCancellationRequested), cancellationToken);
            }
            catch (Exception ex)
            {
                reportOutput($"Error optimizing drives: {ex.Message}");
                throw;
            }
        }

        private async Task CleanWindowsUpdateAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(10, "Cleaning Windows Update files..."));
            try
            {
                await Task.Run(() => CleanWindowsUpdate(reportOutput, () => cancellationToken.IsCancellationRequested), cancellationToken);
            }
            catch (Exception ex)
            {
                reportOutput($"Error cleaning Windows Update: {ex.Message}");
                throw;
            }
        }

        private async Task ClearPrefetchAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(10, "Clearing prefetch files..."));
            try
            {
                var prefetchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                if (!Directory.Exists(prefetchPath))
                {
                    reportOutput("Prefetch directory not found.");
                    return;
                }

                var files = await Task.Run(() => Directory.GetFiles(prefetchPath, "*.pf"), cancellationToken);
                int deletedCount = 0;
                
                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    try
                    {
                        await Task.Run(() => CoreUtilities.SafeDeleteFile(file), cancellationToken);
                        deletedCount++;
                        if (deletedCount % 10 == 0)
                        {
                            progress?.Report(new ProgressReport(Math.Min(90, 10 + (deletedCount * 80 / files.Length)), $"Deleted {deletedCount}/{files.Length} prefetch files"));
                        }
                    }
                    catch (Exception ex)
                    {
                        reportOutput($"Failed to delete {file}: {ex.Message}");
                    }
                }
                
                progress?.Report(new ProgressReport(90, $"Deleted {deletedCount} prefetch files"));
            }
            catch (Exception ex)
            {
                reportOutput($"Error clearing prefetch: {ex.Message}");
                throw;
            }
        }

        private async Task ScanForWindowsUpdatesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(10, "Scanning for Windows Updates..."));
            try
            {
                var updates = await UpdateHelpers.GetWindowsUpdatesAsync(reportOutput, cancellationToken);
                if (updates.Count == 0)
                {
                    reportOutput("No Windows Updates found.");
                    progress?.Report(new ProgressReport(100, "No Windows Updates available"));
                    ShowGenericToast("Windows Update", "No Windows Updates are currently available.");
                    return;
                }

                foreach (var item in updates)
                {
                    reportOutput($"Discovered Windows Update: {item.Title} ({item.KBArticle}) [{item.Category}]");
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var selected = await ShowWindowsUpdateSelectorAsync(updates);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (selected.Count == 0)
                {
                    reportOutput("No Windows Updates selected for installation.");
                    progress?.Report(new ProgressReport(100, "No Windows Updates selected"));
                    ShowGenericToast("Windows Update", "No Windows Updates were selected for installation.");
                    return;
                }

                progress?.Report(new ProgressReport(40, "Installing selected Windows Updates..."));
                await UpdateHelpers.InstallWindowsUpdatesAsync(selected, reportOutput, cancellationToken);
                progress?.Report(new ProgressReport(100, "Completed Windows Updates"));
            }
            catch (Exception ex)
            {
                reportOutput($"Error scanning or installing Windows Updates: {ex.Message}");
                throw;
            }
        }

        private async Task UpgradeWingetPackagesAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(10, "Discovering winget package updates..."));
            try
            {
                var upgrades = await UpdateHelpers.GetWingetUpgradesAsync(reportOutput, cancellationToken);
                if (upgrades.Count == 0)
                {
                    reportOutput("No winget upgrades found.");
                    progress?.Report(new ProgressReport(100, "No winget upgrades available"));
                    ShowWingetToast("No winget package updates are currently available.");
                    return;
                }

                foreach (var item in upgrades)
                {
                    reportOutput($"Discovered: {item.Name} ({item.Id}) {item.InstalledVersion} -> {item.AvailableVersion} [{item.Source}]");
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Show selection dialog on the UI thread so the user can choose which packages to upgrade
                var selected = await ShowWingetSelectorAsync(upgrades);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (selected.Count == 0)
                {
                    reportOutput("No winget packages selected for upgrade.");
                    progress?.Report(new ProgressReport(100, "No winget packages selected"));
                    ShowWingetToast("No winget packages were selected for upgrade.");
                    return;
                }

                var wingetPath = FindWingetExecutable(reportOutput);
                var exe = string.IsNullOrEmpty(wingetPath) ? "winget" : wingetPath;

                int index = 0;
                foreach (var item in selected)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        reportOutput("Winget upgrade cancelled during execution.");
                        break;
                    }

                    index++;
                    var percent = 40 + (int)((index - 1) * 60.0 / Math.Max(1, selected.Count));
                    progress?.Report(new ProgressReport(percent, $"Upgrading {item.Name} ({item.Id}) to {item.AvailableVersion}..."));

                    var args = $"upgrade --id \"{item.Id}\" --accept-source-agreements --accept-package-agreements";
                    var psi = new ProcessStartInfo(exe, args)
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    await RunProcessAndReportAsync(psi, progress!, reportOutput, cancellationToken);
                }

                reportOutput($"Winget upgrade completed for {selected.Count} package(s).");
                progress?.Report(new ProgressReport(100, "Completed winget upgrades"));
            }
            catch (Exception ex)
            {
                reportOutput($"Error upgrading winget packages: {ex.Message}");
                throw;
            }
        }

        private async Task UpdateStoreAppsAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(10, "Checking Microsoft Store app updates..."));
            try
            {
                var upgrades = await UpdateHelpers.GetWingetUpgradesAsync(reportOutput, cancellationToken);
                var storeItems = upgrades.Where(u => string.Equals(u.Source, "msstore", StringComparison.OrdinalIgnoreCase)).ToList();

                if (storeItems.Count == 0)
                {
                    reportOutput("No Microsoft Store app updates found.");
                    progress?.Report(new ProgressReport(100, "No Microsoft Store app updates available"));
                    ShowGenericToast("Microsoft Store", "No Microsoft Store app updates are currently available.");
                    return;
                }

                foreach (var item in storeItems)
                {
                    reportOutput($"Store app update: {item.Name} ({item.Id}) {item.InstalledVersion} -> {item.AvailableVersion} [{item.Source}]");
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var selected = await ShowWingetSelectorAsync(storeItems);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (selected.Count == 0)
                {
                    reportOutput("No Microsoft Store apps selected for update.");
                    progress?.Report(new ProgressReport(100, "No Microsoft Store apps selected"));
                    ShowGenericToast("Microsoft Store", "No Microsoft Store apps were selected for update.");
                    return;
                }

                var wingetPath = FindWingetExecutable(reportOutput);
                var exe = string.IsNullOrEmpty(wingetPath) ? "winget" : wingetPath;

                int index = 0;
                foreach (var item in selected)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        reportOutput("Microsoft Store app update cancelled during execution.");
                        break;
                    }

                    index++;
                    var percent = 40 + (int)((index - 1) * 60.0 / Math.Max(1, selected.Count));
                    progress?.Report(new ProgressReport(percent, $"Updating {item.Name} ({item.Id}) to {item.AvailableVersion}..."));

                    var args = $"upgrade --id \"{item.Id}\" --source msstore --accept-source-agreements --accept-package-agreements";
                    var psi = new ProcessStartInfo(exe, args)
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    await RunProcessAndReportAsync(psi, progress!, reportOutput, cancellationToken);
                }

                reportOutput($"Microsoft Store app update completed for {selected.Count} app(s).");
                progress?.Report(new ProgressReport(100, "Completed Microsoft Store app updates"));
            }
            catch (Exception ex)
            {
                reportOutput($"Error updating Store apps: {ex.Message}");
                throw;
            }
        }

        private async Task EmptyRecycleBinAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(10, "Emptying Recycle Bin..."));
            try
            {
                await Task.Run(() => EmptyRecycleBin(reportOutput), cancellationToken);
            }
            catch (Exception ex)
            {
                reportOutput($"Error emptying Recycle Bin: {ex.Message}");
                throw;
            }
        }

        private async Task ApplySystemTweaksAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            progress?.Report(new ProgressReport(10, "Applying System Tweaks..."));
            try
            {
                await Task.Run(() => ApplySystemTweaks(reportOutput, () => cancellationToken.IsCancellationRequested, (p, m) => progress?.Report(new ProgressReport(p, m))), cancellationToken);
            }
            catch (Exception ex)
            {
                reportOutput($"Error applying System Tweaks: {ex.Message}");
                throw;
            }
        }

        private static void ShowWingetInfoMessage(string message)
        {
            try
            {
                if (Application.OpenForms.Count > 0)
                {
                    var owner = Application.OpenForms[0];
                    if (owner == null)
                    {
                        return;
                    }
                    if (owner.InvokeRequired)
                    {
                        owner.BeginInvoke(new Action(() =>
                        {
                            MessageBox.Show(owner, message, "Winget Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }));
                    }
                    else
                    {
                        MessageBox.Show(owner, message, "Winget Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show(message, "Winget Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch
            {
                try
                {
                    MessageBox.Show(message, "Winget Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch
                {
                    // Swallow any UI errors to avoid crashing the module
                }
            }
        }

        private async Task InstallLatestWingetFromGitHubAsync(Action<string> reportOutput, Func<bool> isCancelled)
        {
            try
            {
                reportOutput("Fetching latest winget release from GitHub...");
                using var http = GetHttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("RecoveryCommander/1.0");
                var json = await http.GetStringAsync("https://api.github.com/repos/microsoft/winget-cli/releases/latest");
                using var doc = JsonDocument.Parse(json);
                var assets = doc.RootElement.GetProperty("assets");

                string? downloadUrl = null;
                string? assetName = null;
                foreach (var a in assets.EnumerateArray())
                {
                    if (!a.TryGetProperty("browser_download_url", out var bd)) continue;
                    if (!a.TryGetProperty("name", out var nm)) continue;
                    var name = nm.GetString() ?? string.Empty;
                    var url = bd.GetString() ?? string.Empty;
                    if (name.Contains("msixbundle", StringComparison.OrdinalIgnoreCase) || name.Contains("msix", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = url;
                        assetName = name;
                        break;
                    }
                    if (name.Contains("appxbundle", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        if (downloadUrl == null) { downloadUrl = url; assetName = name; }
                    }
                }

                if (downloadUrl == null)
                {
                    reportOutput("No suitable App Installer asset found in latest release.");
                    return;
                }

                reportOutput($"Found asset to download: {assetName}");

                var tempFile = Path.Combine(Path.GetTempPath(), assetName!);
                reportOutput($"Downloading to: {tempFile}");
                using (var dl = await http.GetAsync(downloadUrl))
                {
                    dl.EnsureSuccessStatusCode();
                    using var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write);
                    await dl.Content.CopyToAsync(fs);
                }

                if (isCancelled()) return;

                reportOutput("Download complete. Launching installer with elevation (UAC may be required)...");

                var psi = new ProcessStartInfo("powershell.exe")
                {
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Add-AppxPackage -Path '{tempFile}'\"",
                    UseShellExecute = true,
                    Verb = "runAs"
                };
                try
                {
                    using var p = Process.Start(psi);
                    if (p != null) await p.WaitForExitAsync();
                    reportOutput($"Installer exit code: {p?.ExitCode}");
                }
                catch (Exception ex)
                {
                    reportOutput($"Failed to start installer elevated: {ex.Message}");
                }
                try { File.Delete(tempFile); } catch { }
            }
            catch (Exception ex)
            {
                reportOutput($"InstallLatestWingetFromGitHub failed: {ex.Message}");
            }
        }

        private async Task RunProcessAndReportAsync(ProcessStartInfo psi, IProgress<ProgressReport>? progress, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            try
            {
                // Delegate to the robust implementation in CoreUtilities
                await AsyncHelpers.RunProcessAsync(psi, 
                    output => reportOutput(output), 
                    error => reportOutput("ERROR: " + error), 
                    cancellationToken);
            }
            catch (Exception ex)
            {
                reportOutput($"Failed to run process {psi.FileName} {psi.Arguments}: {ex.Message}");
            }
        }

        private void BackupDrivers(Action<string> reportOutput, Func<bool> isCancelled)
        {
            try
            {
                const string destinationPath = @"D:\Drivers";
                
                reportOutput("=== Starting Driver Backup ===");
                
                // Check if the D: drive exists
                var driveRoot = Path.GetPathRoot(destinationPath);
                reportOutput($"Checking drive root: {driveRoot}");
                
                if (string.IsNullOrEmpty(driveRoot))
                {
                    reportOutput("ERROR: Could not determine drive root.");
                    return;
                }
                
                // Check if drive exists using DriveInfo
                try
                {
                    var driveInfo = new DriveInfo(driveRoot);
                    if (!driveInfo.IsReady)
                    {
                        reportOutput($"ERROR: Drive {driveRoot} is not ready.");
                        return;
                    }
                    reportOutput($"Drive {driveRoot} is ready. Type: {driveInfo.DriveType}, Format: {driveInfo.DriveFormat}");
                }
                catch (Exception driveEx)
                {
                    reportOutput($"ERROR: Cannot access drive {driveRoot}: {driveEx.Message}");
                    return;
                }
                
                // Ensure the destination directory exists
                if (!Directory.Exists(destinationPath))
                {
                    reportOutput($"Creating destination directory: {destinationPath}");
                    try
                    {
                        Directory.CreateDirectory(destinationPath);
                        reportOutput($"Directory created successfully: {Directory.Exists(destinationPath)}");
                    }
                    catch (Exception dirEx)
                    {
                        reportOutput($"ERROR: Failed to create directory: {dirEx.Message}");
                        return;
                    }
                }
                else
                {
                    reportOutput($"Destination directory already exists: {destinationPath}");
                }

                reportOutput($"Exporting drivers to {destinationPath}...");
                reportOutput("Running: dism.exe /online /export-driver /destination:\"" + destinationPath + "\"");
                
                // Use cmd.exe /c to run DISM - this often works better for system commands
                var command = $"dism.exe /online /export-driver /destination:\"{destinationPath}\"";
                var psi = new ProcessStartInfo("cmd.exe", $"/c {command}")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = driveRoot
                };
                
                reportOutput("Starting DISM process...");
                
                using (var proc = new Process { StartInfo = psi })
                {
                    proc.Start();
                    
                    // Read output synchronously to ensure we capture everything
                    var output = proc.StandardOutput.ReadToEnd();
                    var error = proc.StandardError.ReadToEnd();
                    
                    proc.WaitForExit();
                    
                    reportOutput($"DISM Exit Code: {proc.ExitCode}");
                    
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            reportOutput(line);
                        }
                    }
                    
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        foreach (var line in error.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            reportOutput($"ERROR: {line}");
                        }
                    }
                    
                    if (proc.ExitCode != 0)
                    {
                        reportOutput($"DISM command failed with exit code {proc.ExitCode}");
                    }
                }
                
                // Check if any drivers were exported
                reportOutput("Checking exported drivers...");
                if (Directory.Exists(destinationPath))
                {
                    var driverFolders = Directory.GetDirectories(destinationPath);
                    var driverFiles = Directory.GetFiles(destinationPath, "*.inf", SearchOption.AllDirectories);
                    reportOutput($"Found {driverFolders.Length} folder(s) and {driverFiles.Length} .inf file(s) in {destinationPath}");
                    
                    if (driverFolders.Length > 0 || driverFiles.Length > 0)
                    {
                        reportOutput($"=== Driver backup completed successfully ===");
                    }
                    else
                    {
                        reportOutput("WARNING: No drivers were exported. The folder is empty.");
                    }
                }
                else
                {
                    reportOutput($"ERROR: Destination folder {destinationPath} does not exist after DISM command.");
                }
            }
            catch (Exception ex)
            {
                reportOutput($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                reportOutput($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
