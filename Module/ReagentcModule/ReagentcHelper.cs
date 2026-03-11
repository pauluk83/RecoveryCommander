using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using System.Runtime.Versioning;

namespace RecoveryCommander.Module;

[SupportedOSPlatform("windows")]
internal static class ReagentcHelper
{
    public static async Task<string> RunReagentcAsync(string arguments, IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
    {
        var psi = CoreUtilities.CreateProcessInfo("reagentc.exe", arguments);
        var sb = new StringBuilder();

        try
        {
            await AsyncHelpers.RunProcessAsync(psi, 
                output => {
                    sb.AppendLine(output);
                    reportOutput(output);
                }, 
                error => reportOutput("ERROR: " + error), 
                cancellationToken);

            return sb.ToString();
        }
        catch (OperationCanceledException)
        {
            reportOutput("Operation cancelled by user.");
            throw;
        }
        catch (Exception ex)
        {
             reportOutput($"REAgentc error: {ex.Message}");
             throw;
        }
    }

    public static (int Disk, int Partition)? ParseDiskPartitionFromInfo(string infoText)
    {
        if (string.IsNullOrWhiteSpace(infoText)) return null;
        
        // Look for the harddiskX\partitionY pattern
        var match = Regex.Match(infoText, @"harddisk(?<disk>\d+)\\partition(?<part>\d+)", RegexOptions.IgnoreCase);
        if (match.Success && 
            int.TryParse(match.Groups["disk"].Value, out var disk) && 
            int.TryParse(match.Groups["part"].Value, out var partition))
        {
            return (disk, partition);
        }
        return null;
    }

    public static string? GetWinReStatus(string infoText)
    {
        if (string.IsNullOrWhiteSpace(infoText)) return "Unknown";
        
        var match = Regex.Match(infoText, @"Windows RE status:\s*(Enabled|Disabled)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "Unknown";
    }
}
