using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;
using RecoveryCommander.Core;

namespace RecoveryCommander.Features
{
    #region Restore Point Manager (from RestorePointManager.cs)
    /// <summary>
    /// System Restore Point Manager - Create, browse, and restore system restore points
    /// </summary>
    public static class RestorePointManager
    {
        public static async Task<RestorePointResult> CreateRestorePointAsync(string description, RestorePointType type = RestorePointType.ApplicationInstall)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // First check if System Restore is enabled
                    if (!IsSystemRestoreEnabled())
                    {
                        return new RestorePointResult 
                        { 
                            Success = false, 
                            Message = "System Restore is not enabled on this system. Please enable it first." 
                        };
                    }

                    // Check if running with administrator privileges
                    if (!IsRunningAsAdministrator())
                    {
                        return new RestorePointResult 
                        { 
                            Success = false, 
                            Message = "Administrator privileges are required to create restore points." 
                        };
                    }

                    var scope = new ManagementScope("\\\\localhost\\root\\default");
                    scope.Connect();

                    using var mgmtClass = new ManagementClass(scope, new ManagementPath("SystemRestore"), null);
                    var inParams = mgmtClass.GetMethodParameters("CreateRestorePoint");
                    inParams["Description"] = description;
                    inParams["RestorePointType"] = (int)type;
                    inParams["EventType"] = 100; // BEGIN_SYSTEM_CHANGE

                    var outParams = mgmtClass.InvokeMethod("CreateRestorePoint", inParams, null);
                    var result = Convert.ToInt32(outParams["ReturnValue"]);

                    return new RestorePointResult
                    {
                        Success = result == 0,
                        Message = result == 0 ? "Restore point created successfully" : GetErrorMessage(result),
                        RestorePointId = result == 0 ? GetLatestRestorePointId() : null
                    };
                }
                catch (Exception ex)
                {
                    return new RestorePointResult
                    {
                        Success = false,
                        Message = $"Error creating restore point: {ex.Message}"
                    };
                }
            });
        }

        public static async Task<List<RestorePoint>> GetRestorePointsAsync()
        {
            return await Task.Run(() =>
            {
                var restorePoints = new List<RestorePoint>();

                try
                {
                    var scope = new ManagementScope("\\\\localhost\\root\\default");
                    scope.Connect();

                    using var searcher = new ManagementObjectSearcher(scope, new SelectQuery("SystemRestore"));
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var restorePoint = new RestorePoint
                        {
                            Id = Convert.ToInt32(obj["SequenceNumber"]),
                            Description = obj["Description"]?.ToString() ?? "",
                            CreationTime = Convert.ToDateTime(obj["CreationTime"]),
                            Type = (RestorePointType)Convert.ToInt32(obj["RestorePointType"]),
                            EventType = (RestorePointEventType)Convert.ToInt32(obj["EventType"])
                        };
                        restorePoints.Add(restorePoint);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but return empty list
                    System.Diagnostics.Debug.WriteLine($"Error getting restore points: {ex.Message}");
                }

                return restorePoints.OrderByDescending(rp => rp.CreationTime).ToList();
            });
        }

        public static async Task<RestorePointResult> RestoreToPointAsync(int restorePointId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!IsRunningAsAdministrator())
                    {
                        return new RestorePointResult
                        {
                            Success = false,
                            Message = "Administrator privileges are required to restore system restore points."
                        };
                    }

                    var scope = new ManagementScope("\\\\localhost\\root\\default");
                    scope.Connect();

                    using var mgmtClass = new ManagementClass(scope, new ManagementPath("SystemRestore"), null);
                    var inParams = mgmtClass.GetMethodParameters("Restore");
                    inParams["SequenceNumber"] = restorePointId;

                    var outParams = mgmtClass.InvokeMethod("Restore", inParams, null);
                    var result = Convert.ToInt32(outParams["ReturnValue"]);

                    return new RestorePointResult
                    {
                        Success = result == 0,
                        Message = result == 0 ? "System restore initiated successfully. The system will restart." : GetErrorMessage(result)
                    };
                }
                catch (Exception ex)
                {
                    return new RestorePointResult
                    {
                        Success = false,
                        Message = $"Error restoring to point: {ex.Message}"
                    };
                }
            });
        }

        private static bool IsSystemRestoreEnabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore");
                return key?.GetValue("RPSessionInterval") != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static string GetErrorMessage(int errorCode)
        {
            return errorCode switch
            {
                2 => "Access denied. Administrator privileges required.",
                8 => "System Restore is disabled.",
                10 => "System Restore is suspended.",
                11 => "System Restore is paused.",
                12 => "System Restore is in progress.",
                13 => "System Restore is busy.",
                14 => "System Restore is not supported.",
                15 => "System Restore is not configured.",
                16 => "System Restore is not available.",
                17 => "System Restore is not initialized.",
                18 => "System Restore is not running.",
                19 => "System Restore is not enabled.",
                20 => "System Restore is not found.",
                21 => "System Restore is not valid.",
                22 => "System Restore is not accessible.",
                23 => "System Restore is not ready.",
                24 => "System Restore is not completed.",
                25 => "System Restore is not successful.",
                26 => "System Restore is not allowed.",
                27 => "System Restore is not permitted.",
                28 => "System Restore is not possible.",
                29 => "System Restore is not feasible.",
                30 => "System Restore is not appropriate.",
                _ => $"Unknown error code: {errorCode}"
            };
        }

        private static int? GetLatestRestorePointId()
        {
            try
            {
                var scope = new ManagementScope("\\\\localhost\\root\\default");
                scope.Connect();

                using var searcher = new ManagementObjectSearcher(scope, new SelectQuery("SystemRestore"));
                var latest = searcher.Get()
                    .Cast<ManagementObject>()
                    .OrderByDescending(obj => Convert.ToDateTime(obj["CreationTime"]))
                    .FirstOrDefault();

                return latest != null ? Convert.ToInt32(latest["SequenceNumber"]) : null;
            }
            catch
            {
                return null;
            }
        }
    }

    public class RestorePointResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int? RestorePointId { get; set; }
    }

    public class RestorePoint
    {
        public int Id { get; set; }
        public string Description { get; set; } = "";
        public DateTime CreationTime { get; set; }
        public RestorePointType Type { get; set; }
        public RestorePointEventType EventType { get; set; }
    }

    public enum RestorePointType
    {
        ApplicationInstall = 0,
        ApplicationUninstall = 1,
        DeviceDriverInstall = 10,
        ModifySettings = 12,
        CancelledOperation = 13
    }

    public enum RestorePointEventType
    {
        BeginSystemChange = 100,
        EndSystemChange = 101
    }
    #endregion

    #region Startup Manager (from StartupManager.cs)
    /// <summary>
    /// Startup Manager - Manage Windows startup programs with async operations
    /// </summary>
    public static class StartupManager
    {
        private static readonly string[] StartupKeys = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"
        };

        public static async Task<List<StartupItem>> GetStartupItemsAsync()
        {
            return await Task.Run(async () =>
            {
                var items = new List<StartupItem>();

                // HKEY_LOCAL_MACHINE
                foreach (var keyPath in StartupKeys)
                {
                    try
                    {
                        using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                        if (key != null)
                        {
                            foreach (var valueName in key.GetValueNames())
                            {
                                var command = key.GetValue(valueName)?.ToString() ?? "";
                                var item = new StartupItem
                                {
                                    Name = valueName,
                                    Command = command,
                                    Location = $"HKLM\\{keyPath}",
                                    IsEnabled = true,
                                    Scope = StartupScope.AllUsers
                                };
                                PopulateItemDetails(item);
                                items.Add(item);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error reading HKLM startup key {keyPath}: {ex.Message}");
                    }
                }

                // HKEY_CURRENT_USER
                var currentUserKeys = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"
                };

                foreach (var keyPath in currentUserKeys)
                {
                    try
                    {
                        using var key = Registry.CurrentUser.OpenSubKey(keyPath);
                        if (key != null)
                        {
                            foreach (var valueName in key.GetValueNames())
                            {
                                var command = key.GetValue(valueName)?.ToString() ?? "";
                                var item = new StartupItem
                                {
                                    Name = valueName,
                                    Command = command,
                                    Location = $"HKCU\\{keyPath}",
                                    IsEnabled = true,
                                    Scope = StartupScope.CurrentUser
                                };
                                PopulateItemDetails(item);
                                items.Add(item);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error reading HKCU startup key {keyPath}: {ex.Message}");
                    }
                }

                // Startup folder
                await AddStartupFolderItems(items);

                return items.OrderBy(i => i.Name).ToList();
            });
        }

        public static async Task<bool> DisableStartupItemAsync(StartupItem item)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var keyPath = item.Location.Replace("HKLM\\", "").Replace("HKCU\\", "");
                    var hive = item.Location.StartsWith("HKLM") ? Registry.LocalMachine : Registry.CurrentUser;

                    using var key = hive.OpenSubKey(keyPath, true);
                    if (key != null && key.GetValue(item.Name) != null)
                    {
                        key.DeleteValue(item.Name);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disabling startup item {item.Name}: {ex.Message}");
                }
                return false;
            });
        }

        public static async Task<bool> EnableStartupItemAsync(StartupItem item)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var keyPath = item.Location.Replace("HKLM\\", "").Replace("HKCU\\", "");
                    var hive = item.Location.StartsWith("HKLM") ? Registry.LocalMachine : Registry.CurrentUser;

                    using var key = hive.OpenSubKey(keyPath, true);
                    if (key != null)
                    {
                        key.SetValue(item.Name, item.Command);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error enabling startup item {item.Name}: {ex.Message}");
                }
                return false;
            });
        }

        public static async Task<bool> DeleteStartupItemAsync(StartupItem item)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (item.Location.StartsWith("Startup Folder"))
                    {
                        var filePath = item.Command;
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            return true;
                        }
                    }
                    else
                    {
                        return await DisableStartupItemAsync(item);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting startup item {item.Name}: {ex.Message}");
                }
                return false;
            });
        }

        private static void PopulateItemDetails(StartupItem item)
        {
            try
            {
                var parts = item.Command.Split(new[] { '"', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    item.ExecutablePath = parts[0].Trim('"');
                    
                    if (File.Exists(item.ExecutablePath))
                    {
                        var fileInfo = new FileInfo(item.ExecutablePath);
                        item.FileVersion = FileVersionInfo.GetVersionInfo(item.ExecutablePath).FileVersion ?? "";
                        item.FileSize = fileInfo.Length;
                        item.LastModified = fileInfo.LastWriteTime;
                    }
                }
            }
            catch
            {
                // If we can't get file details, that's okay
            }
        }

        private static async Task AddStartupFolderItems(List<StartupItem> items)
        {
            await Task.Run(() =>
            {
                var startupFolders = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
                };

                foreach (var folder in startupFolders)
                {
                    if (!Directory.Exists(folder)) continue;

                    try
                    {
                        foreach (var file in Directory.GetFiles(folder, "*.lnk"))
                        {
                            var item = new StartupItem
                            {
                                Name = Path.GetFileNameWithoutExtension(file),
                                Command = file,
                                Location = $"Startup Folder\\{Path.GetDirectoryName(folder)?.Split('\\').Last()}",
                                IsEnabled = true,
                                Scope = folder.Contains("Common") ? StartupScope.AllUsers : StartupScope.CurrentUser
                            };
                            PopulateItemDetails(item);
                            items.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error reading startup folder {folder}: {ex.Message}");
                    }
                }
            });
        }
    }

    public class StartupItem
    {
        public string Name { get; set; } = "";
        public string Command { get; set; } = "";
        public string Location { get; set; } = "";
        public string ExecutablePath { get; set; } = "";
        public string FileVersion { get; set; } = "";
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsEnabled { get; set; }
        public StartupScope Scope { get; set; }
    }

    public enum StartupScope
    {
        CurrentUser,
        AllUsers
    }
    #endregion
}
