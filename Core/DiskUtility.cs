using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RecoveryCommander.Core
{
    public static class DiskUtility
    {
        private static readonly string[] _lineSeparators = new[] { "\r\n", "\n" };

        public static async Task<bool> RunDiskpartScriptAsync(string script, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            string tmp = Path.Combine(Path.GetTempPath(), $"rc_diskpart_{Guid.NewGuid():N}.txt");
            try
            {
                await File.WriteAllTextAsync(tmp, script, cancellationToken);
                var psi = CoreUtilities.CreateProcessInfo("diskpart.exe", $"/s \"{tmp}\"");
                bool success = false;
                await AsyncHelpers.RunProcessAsync(psi, (line) =>
                {
                    reportOutput(line);
                    if (line.Contains("DiskPart successfully", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("successfully assigned", StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("successfully removed", StringComparison.OrdinalIgnoreCase))
                    {
                        success = true;
                    }
                }, error => reportOutput("ERROR: " + error), cancellationToken);
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

        public static async Task<int?> FindVolumeNumberByLabelAsync(string label, Action<string> reportOutput, CancellationToken cancellationToken)
        {
            string tmp = Path.Combine(Path.GetTempPath(), $"rc_diskpart_{Guid.NewGuid():N}.txt");
            try
            {
                await File.WriteAllTextAsync(tmp, "list volume\r\n", cancellationToken);
                var psi = CoreUtilities.CreateProcessInfo("diskpart.exe", $"/s \"{tmp}\"");
                var sb = new StringBuilder();
                await AsyncHelpers.RunProcessAsync(psi, (line) => { sb.AppendLine(line); }, null, cancellationToken);
                foreach (var raw in sb.ToString().Split(_lineSeparators, StringSplitOptions.RemoveEmptyEntries))
                {
                    var line = raw.Trim();
                    if (line.Contains(label, StringComparison.OrdinalIgnoreCase) && line.StartsWith("Volume", StringComparison.OrdinalIgnoreCase))
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

        public static char FindAvailableDriveLetter()
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
            throw new InvalidOperationException("No available drive letters found (D-Z all in use).");
        }
    }
}
