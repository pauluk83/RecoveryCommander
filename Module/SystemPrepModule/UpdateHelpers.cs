using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Core;

namespace SystemPrepModule
{
    public static class UpdateHelpers
    {
        private static readonly Regex TableHeaderRegex = new Regex("^-+\\s+-+", RegexOptions.Compiled);
        private static readonly Regex SeparatorLineRegex = new Regex("^-+$", RegexOptions.Compiled);
        private static readonly Regex ColumnSplitRegex = new Regex("\\s{2,}", RegexOptions.Compiled);
        private static readonly object[] ItemIndexArgument = new object[] { 0 };
        private static readonly object[] SearchCriteriaArgument = new object[] { "IsInstalled=0 and Type='Software'" };
        private static readonly object[] CategoryIndexArgument = new object[] { 0 };
        public sealed class WingetUpgradeItem
        {
            public string Name { get; set; } = string.Empty;
            public string Id { get; set; } = string.Empty;
            public string InstalledVersion { get; set; } = string.Empty;
            public string AvailableVersion { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
        }

        public static async Task<List<WingetUpgradeItem>> GetWingetUpgradesAsync(Action<string> reportOutput, CancellationToken cancellationToken)
        {
            var exe = await FindWingetExecutableAsync(reportOutput, cancellationToken) ?? "winget";
            var result = await CoreUtilities.ExecuteCommandAsync(exe, "upgrade --accept-source-agreements --accept-package-agreements", 120, cancellationToken);
            var items = new List<WingetUpgradeItem>();
            if (!result.Success)
            {
                if (!string.IsNullOrWhiteSpace(result.Error))
                {
                    reportOutput(result.Error);
                }
                return items;
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return items;
            }
            var message = result.Output ?? string.Empty;
            var lines = message.Split(["\r", "\n"], StringSplitOptions.RemoveEmptyEntries);
            var tableStarted = false;
            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                if (!tableStarted)
                {
                    if (TableHeaderRegex.IsMatch(line))
                    {
                        tableStarted = true;
                    }
                    continue;
                }
                if (SeparatorLineRegex.IsMatch(line))
                {
                    continue;
                }
                var cols = ColumnSplitRegex.Split(line.Trim());
                if (cols.Length < 4)
                {
                    continue;
                }
                var source = cols.Length >= 5 ? cols[^1] : string.Empty;
                var available = cols[^2];
                var version = cols[^3];
                var id = cols[^4];
                var nameParts = cols[..^4];
                var name = nameParts.Length == 0 ? id : string.Join(" ", nameParts);
                items.Add(new WingetUpgradeItem
                {
                    Name = name,
                    Id = id,
                    InstalledVersion = version,
                    AvailableVersion = available,
                    Source = source
                });
            }
            reportOutput($"Found {items.Count} winget package(s) with available upgrades.");
            return items;
        }

        public sealed class WindowsUpdateItem
        {
            public string Title { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string KBArticle { get; set; } = string.Empty;
            public string UpdateId { get; set; } = string.Empty;
        }

        public static async Task<List<WindowsUpdateItem>> GetWindowsUpdatesAsync(Action<string> reportOutput, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var items = new List<WindowsUpdateItem>();
                try
                {
                    var sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
                    if (sessionType == null)
                    {
                        reportOutput("Windows Update COM API (Microsoft.Update.Session) is not available on this system.");
                        return items;
                    }

                    var session = Activator.CreateInstance(sessionType);
                    if (session == null)
                    {
                        reportOutput("Failed to create Windows Update session.");
                        return items;
                    }

                    var searcher = sessionType.InvokeMember("CreateUpdateSearcher", BindingFlags.InvokeMethod, null, session, null);
                    if (searcher == null)
                    {
                        reportOutput("Failed to create Windows Update searcher.");
                        return items;
                    }

                    var searcherType = searcher.GetType();
                    var result = searcherType.InvokeMember("Search", BindingFlags.InvokeMethod, null, searcher, SearchCriteriaArgument);
                    if (result == null)
                    {
                        reportOutput("Windows Update search returned no result.");
                        return items;
                    }

                    var resultType = result.GetType();
                    var updatesObj = resultType.InvokeMember("Updates", BindingFlags.GetProperty, null, result, null);
                    if (updatesObj == null)
                    {
                        reportOutput("Windows Update search returned no updates collection.");
                        return items;
                    }

                    var updatesType = updatesObj.GetType();
                    var countObj = updatesType.InvokeMember("Count", BindingFlags.GetProperty, null, updatesObj, null);
                    var count = countObj is int c ? c : 0;

                    for (var i = 0; i < count; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        var update = updatesType.InvokeMember("Item", BindingFlags.GetProperty, null, updatesObj, new object[] { i });
                        if (update == null)
                        {
                            continue;
                        }

                        var uType = update.GetType();
                        var titleObj = uType.InvokeMember("Title", BindingFlags.GetProperty, null, update, null);
                        var title = titleObj as string ?? string.Empty;

                        string categoryName = string.Empty;
                        var categoriesObj = uType.InvokeMember("Categories", BindingFlags.GetProperty, null, update, null);
                        if (categoriesObj != null)
                        {
                            var categoriesType = categoriesObj.GetType();
                            var catCountObj = categoriesType.InvokeMember("Count", BindingFlags.GetProperty, null, categoriesObj, null);
                            var catCount = catCountObj is int cc ? cc : 0;
                            if (catCount > 0)
                            {
                                var cat0 = categoriesType.InvokeMember("Item", BindingFlags.GetProperty, null, categoriesObj, CategoryIndexArgument);
                                if (cat0 != null)
                                {
                                    var catTitleObj = cat0.GetType().InvokeMember("Name", BindingFlags.GetProperty, null, cat0, null);
                                    if (catTitleObj is string catTitle && !string.IsNullOrWhiteSpace(catTitle))
                                    {
                                        categoryName = catTitle;
                                    }
                                }
                            }
                        }

                        string kb = string.Empty;
                        var kbObj = uType.InvokeMember("KBArticleIDs", BindingFlags.GetProperty, null, update, null);
                        if (kbObj is Array kbArray && kbArray.Length > 0)
                        {
                            var parts = new List<string>();
                            foreach (var k in kbArray)
                            {
                                if (k != null)
                                {
                                    var s = k.ToString();
                                    if (!string.IsNullOrWhiteSpace(s))
                                    {
                                        parts.Add(s);
                                    }
                                }
                            }

                            kb = string.Join(", ", parts);
                        }

                        string updateId = string.Empty;
                        var identityObj = uType.InvokeMember("Identity", BindingFlags.GetProperty, null, update, null);
                        if (identityObj != null)
                        {
                            var idObj = identityObj.GetType().InvokeMember("UpdateID", BindingFlags.GetProperty, null, identityObj, null);
                            if (idObj is string id)
                            {
                                updateId = id;
                            }
                        }

                        items.Add(new WindowsUpdateItem
                        {
                            Title = title,
                            Category = categoryName,
                            KBArticle = kb,
                            UpdateId = updateId
                        });
                    }

                    reportOutput($"Found {items.Count} Windows Update(s) available.");
                }
                catch (Exception ex)
                {
                    reportOutput($"Failed to query Windows Updates via COM: {ex.Message}");
                }

                return items;
            }, cancellationToken);
        }

        public static async Task InstallWindowsUpdatesAsync(IEnumerable<WindowsUpdateItem> updates, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            var thread = new Thread(() =>
            {
                try
                {
                    InstallWindowsUpdatesInternal(updates, reportOutput, cancellationToken);
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    reportOutput($"Error in Windows Update thread: {ex.Message}");
                    tcs.SetResult(false);
                }
            });
            
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            
            await tcs.Task;
        }

        private static void InstallWindowsUpdatesInternal(IEnumerable<WindowsUpdateItem> updates, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            try
            {
                var updateList = updates?.ToList() ?? new List<WindowsUpdateItem>();
                if (updateList.Count == 0) return;

                var selectedIds = new HashSet<string>(updateList.Select(u => u.UpdateId).Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);
                if (selectedIds.Count == 0) return;

                var sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
                if (sessionType == null)
                {
                    reportOutput("Windows Update COM API (Microsoft.Update.Session) is not available on this system.");
                    return;
                }

                var session = Activator.CreateInstance(sessionType);
                if (session == null)
                {
                    reportOutput("Failed to create Windows Update session.");
                    return;
                }

                var searcher = sessionType.InvokeMember("CreateUpdateSearcher", BindingFlags.InvokeMethod, null, session, null);
                if (searcher == null)
                {
                    reportOutput("Failed to create Windows Update searcher.");
                    return;
                }

                var searcherType = searcher.GetType();
                var result = searcherType.InvokeMember("Search", BindingFlags.InvokeMethod, null, searcher, SearchCriteriaArgument);
                if (result == null)
                {
                    reportOutput("Windows Update search returned no result.");
                    return;
                }

                var resultType = result.GetType();
                var updatesObj = resultType.InvokeMember("Updates", BindingFlags.GetProperty, null, result, null);
                if (updatesObj == null)
                {
                    reportOutput("Windows Update search returned no updates collection.");
                    return;
                }

                var updatesType = updatesObj.GetType();
                var countObj = updatesType.InvokeMember("Count", BindingFlags.GetProperty, null, updatesObj, null);
                var count = countObj is int c ? c : 0;

                var collType = Type.GetTypeFromProgID("Microsoft.Update.UpdateColl");
                if (collType == null)
                {
                    reportOutput("Windows Update collection type (Microsoft.Update.UpdateColl) is not available.");
                    return;
                }

                var coll = Activator.CreateInstance(collType);
                if (coll == null)
                {
                    reportOutput("Failed to create Windows Update collection.");
                    return;
                }

                int added = 0;
                for (var i = 0; i < count; i++)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var update = updatesType.InvokeMember("Item", BindingFlags.GetProperty, null, updatesObj, new object[] { i });
                    if (update == null) continue;

                    var uType = update.GetType();
                    var identityObj = uType.InvokeMember("Identity", BindingFlags.GetProperty, null, update, null);
                    if (identityObj == null) continue;

                    var idObj = identityObj.GetType().InvokeMember("UpdateID", BindingFlags.GetProperty, null, identityObj, null);
                    if (idObj is not string id || !selectedIds.Contains(id)) continue;

                    collType.InvokeMember("Add", BindingFlags.InvokeMethod, null, coll, new object[] { update });
                    added++;
                }

                if (added == 0)
                {
                    reportOutput("Selected Windows Updates are no longer available.");
                    return;
                }

                var installer = sessionType.InvokeMember("CreateUpdateInstaller", BindingFlags.InvokeMethod, null, session, null);
                if (installer == null)
                {
                    reportOutput("Failed to create Windows Update installer.");
                    return;
                }

                var installerType = installer.GetType();
                installerType.InvokeMember("Updates", BindingFlags.SetProperty, null, installer, new object[] { coll });

                reportOutput("Starting installation of selected Windows Updates...");
                var installResult = installerType.InvokeMember("Install", BindingFlags.InvokeMethod, null, installer, null);
                if (installResult != null)
                {
                    var resType = installResult.GetType();
                    var resultCodeObj = resType.InvokeMember("ResultCode", BindingFlags.GetProperty, null, installResult, null);
                    if (resultCodeObj != null)
                    {
                        reportOutput($"Windows Update installer result: {resultCodeObj}");
                    }
                }
            }
            catch (Exception ex)
            {
                reportOutput($"Failed to install Windows Updates via COM: {ex.Message}");
            }
        }

        private static async Task<string?> FindWingetExecutableAsync(Action<string> reportOutput, CancellationToken cancellationToken = default)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", "/c where winget")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    var output = await p.StandardOutput.ReadToEndAsync();
                    await p.WaitForExitAsync();
                    var first = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(first))
                    {
                        reportOutput($"Found winget at: {first}");
                        return first;
                    }
                }
                var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var candidate = Path.Combine(localApp, "Microsoft", "WindowsApps", "winget.exe");
                if (File.Exists(candidate))
                {
                    reportOutput($"Found winget at: {candidate}");
                    return candidate;
                }
            }
            catch (Exception ex)
            {
                reportOutput($"FindWingetExecutable error: {ex.Message}");
            }
            return null;
        }
    }
}
