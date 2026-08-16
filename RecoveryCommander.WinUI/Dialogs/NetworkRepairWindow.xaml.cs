using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Windows.UI;

namespace RecoveryCommanderWinUI.Dialogs;

/// <summary>
/// Network Repair &amp; Optimization window — native WinUI3 implementation.
/// Window-based implementation to overcome ContentDialog width limitations.
/// Replaces the legacy WinForms NetworkOptimizer which is now deprecated.
/// </summary>
public sealed partial class NetworkRepairWindow : BaseWindowDialog
{
    public NetworkRepairWindow()
    {
        InitializeComponent();
        this.Activated += async (_, _) =>
        {
            LoadAdapters();
            await RefreshConnectionStatusAsync();
        };
    }

    // ─── Adapters ──────────────────────────────────────────────────────────────

    private void LoadAdapters()
    {
        AdapterCombo.Items.Clear();
        try
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderByDescending(ni => ni.OperationalStatus == OperationalStatus.Up)
                .ToList();

            foreach (var ni in adapters)
                AdapterCombo.Items.Add(ni.Name);

            if (AdapterCombo.Items.Count > 0)
                AdapterCombo.SelectedIndex = 0;
        }
        catch (Exception ex) when (ex is System.Net.NetworkInformation.NetworkInformationException || ex is System.ComponentModel.Win32Exception || ex is UnauthorizedAccessException)
        {
            AppendLog($"[WARN] Could not enumerate adapters: {ex.Message}");
        }
    }

    private void RefreshStatusButton_Click(object sender, RoutedEventArgs e)
        => _ = RefreshConnectionStatusAsync();

    private async Task RefreshConnectionStatusAsync()
    {
        await Task.Run(() =>
        {
            var active = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderByDescending(ni => ni.Speed)
                .FirstOrDefault();

            DispatcherQueue.TryEnqueue(() =>
            {
                if (active == null)
                {
                    ConnectionStatusText.Text = "Disconnected";
                    AdapterDetailsText.Text = "No active network interface found";
                    StatusDot.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x6B, 0x6B));
                    StatusBanner.Background = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0x6B, 0x6B));
                }
                else
                {
                    var speedMb = active.Speed > 0 ? $"  •  {active.Speed / 1_000_000} Mbps" : "";
                    ConnectionStatusText.Text = $"Connected — {active.Name}";
                    AdapterDetailsText.Text = $"{active.NetworkInterfaceType}{speedMb}  •  {active.Description}";
                    StatusDot.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x64, 0xFF, 0x8A));
                    StatusBanner.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x18, 0xCF, 0xFF));
                }
            });
        });
    }

    // ─── DNS ───────────────────────────────────────────────────────────────────

    private async void ApplyDnsButton_Click(object sender, RoutedEventArgs e)
    {
        var adapter = AdapterCombo.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(adapter)) { AppendLog("[WARN] Select an adapter first."); return; }

        var dnsTag = (DnsCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "dhcp";
        var dnsName = (DnsCombo.SelectedItem as ComboBoxItem)?.Content as string ?? dnsTag;

        if (dnsTag == "dhcp")
        {
            await RunNetworkCommandAsync("netsh.exe", $"interface ip set dns \"{adapter}\" dhcp",
                $"Setting DNS to DHCP for {adapter}...", $"✔ DNS set to automatic (DHCP) for {adapter}.");
        }
        else
        {
            await RunNetworkCommandAsync("netsh.exe", $"interface ip set dns \"{adapter}\" static {dnsTag}",
                $"Applying {dnsName} to {adapter}...", $"✔ DNS set to {dnsTag} for {adapter}.");
        }
    }

    private async void FlushDnsButton_Click(object sender, RoutedEventArgs e)
        => await RunNetworkCommandAsync("ipconfig.exe", "/flushdns",
            "Flushing DNS cache...", "✔ DNS cache flushed successfully.");

    // ─── Repair ────────────────────────────────────────────────────────────────

    private async void ResetWinsockButton_Click(object sender, RoutedEventArgs e)
    {
        AppendLog("─── Winsock Reset ───");
        await RunNetworkCommandAsync("netsh.exe", "winsock reset",
            "Resetting Winsock...", "✔ Winsock reset complete. Restart recommended.");
    }

    private async void ResetTcpButton_Click(object sender, RoutedEventArgs e)
    {
        AppendLog("─── TCP/IP Stack Reset ───");
        await RunNetworkCommandAsync("netsh.exe", "int ip reset",
            "Resetting TCP/IP stack...", "✔ TCP/IP stack reset complete. Restart recommended.");
    }

    private async void DiagnoseButton_Click(object sender, RoutedEventArgs e)
    {
        AppendLog("─── Network Diagnosis ───");
        SetBusy(true, "Running diagnosis...");
        try
        {
            await RunNetworkCommandAsync("ping.exe", "-n 2 8.8.8.8", "Pinging 8.8.8.8...", null);
            await RunNetworkCommandAsync("ping.exe", "-n 2 1.1.1.1", "Pinging 1.1.1.1...", null);
            await RunNetworkCommandAsync("nslookup.exe", "google.com", "Resolving google.com...", null);
            await RunNetworkCommandAsync("netsh.exe", "winsock show catalog",
                "Checking Winsock catalog...", null);
            AppendLog("✔ Diagnosis complete.");
            SetStatus("✔ Diagnosis complete.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ─── Ping ──────────────────────────────────────────────────────────────────

    private async void PingButton_Click(object sender, RoutedEventArgs e)
    {
        var host = PingHostBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(host)) { AppendLog("[WARN] Enter a host to ping."); return; }

        // Basic sanitize — no shell metacharacters
        host = System.Text.RegularExpressions.Regex.Replace(host, @"[;&|<>`$\\]", "");

        AppendLog($"─── Ping {host} ───");
        await RunNetworkCommandAsync("ping.exe", $"-n 4 {host}",
            $"Pinging {host}...", $"✔ Ping complete.");
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private async Task RunNetworkCommandAsync(string fileName, string arguments,
        string startMsg, string? doneMsg)
    {
        SetBusy(true, startMsg);
        AppendLog($"[{DateTime.Now:HH:mm:ss}] > {fileName} {arguments}");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                AppendLog("[ERROR] Failed to start process.");
                return;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(output))
                AppendLog(output.TrimEnd());

            if (!string.IsNullOrWhiteSpace(error))
                AppendLog($"[STDERR] {error.TrimEnd()}");

            if (doneMsg != null)
            {
                AppendLog(doneMsg);
                SetStatus(doneMsg);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is IOException || ex is UnauthorizedAccessException || ex is System.ComponentModel.Win32Exception || ex is PlatformNotSupportedException)
        {
            AppendLog($"[ERROR] {ex.Message}");
            SetStatus($"✘ Error: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static async Task<bool> ConfirmAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Continue",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private void AppendLog(string message)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ActivityLog.Text += message + "\n";
        });
    }

    private void SetStatus(string message)
        => DispatcherQueue.TryEnqueue(() => StatusText.Text = message);

    private void SetBusy(bool busy, string? status = null)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            BusyRing.IsActive = busy;
            BusyRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            if (status != null) StatusText.Text = status;
            else if (!busy) StatusText.Text = "Ready";
        });
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        => ActivityLog.Text = string.Empty;

    private void ActivityLog_SizeChanged(object sender, SizeChangedEventArgs e)
        => LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
