using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RecoveryCommander.Features;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace RecoveryCommanderWinUI.Dialogs;

/// <summary>
/// Restore Point Manager window - view, create, restore and delete system restore points.
/// Window-based implementation to overcome ContentDialog width limitations.
/// </summary>
public sealed partial class RestorePointManagerWindow : BaseWindowDialog
{
    public RestorePointManagerWindow()
    {
        InitializeComponent();
        this.Activated += async (_, _) => await LoadRestorePointsAsync();
    }

    private async Task LoadRestorePointsAsync()
    {
        ShowLoading(true);
        SetStatus("Loading restore points...");

        try
        {
            var points = await RestorePointManager.GetRestorePointsAsync();

            RestorePointsListView.ItemsSource = null;

            if (points.Count == 0)
            {
                EmptyPanel.Visibility = Visibility.Visible;
                RestorePointsListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                var items = new List<RestorePointItem>();
                foreach (var rp in points)
                {
                    items.Add(new RestorePointItem
                    {
                        Id = rp.Id,
                        Description = string.IsNullOrWhiteSpace(rp.Description) ? "(No description)" : rp.Description,
                        CreationTime = rp.CreationTime.ToString("yyyy-MM-dd  HH:mm:ss", CultureInfo.CurrentCulture),
                        TypeLabel = rp.Type.ToString().Replace("Application", "App ").Replace("Device", "Dev "),
                        Raw = rp
                    });
                }

                RestorePointsListView.ItemsSource = items;
                EmptyPanel.Visibility = Visibility.Collapsed;
                RestorePointsListView.Visibility = Visibility.Visible;
            }

            SetStatus($"Found {points.Count} restore point(s). Last updated: {DateTime.Now:HH:mm:ss}");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.ComponentModel.Win32Exception)
        {
            SetStatus($"Error: {ex.Message}");
            EmptyPanel.Visibility = Visibility.Visible;
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private void ShowLoading(bool loading)
    {
        LoadingPanel.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        if (loading)
        {
            RestorePointsListView.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        => await LoadRestorePointsAsync();

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        CreatePanel.Visibility = Visibility.Visible;
        DescriptionBox.Focus(FocusState.Programmatic);
    }

    private void CancelCreateButton_Click(object sender, RoutedEventArgs e)
    {
        CreatePanel.Visibility = Visibility.Collapsed;
        DescriptionBox.Text = string.Empty;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private async void ConfirmCreateButton_Click(object sender, RoutedEventArgs e)
    {
        var description = DescriptionBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(description))
            description = $"RecoveryCommander – {DateTime.Now:yyyy-MM-dd HH:mm}";

        CreatePanel.Visibility = Visibility.Collapsed;
        DescriptionBox.Text = string.Empty;
        SetStatus("Creating restore point...");
        ShowLoading(true);

        try
        {
            var result = await RestorePointManager.CreateRestorePointAsync(description);
            SetStatus(result.Success ? $"✔ {result.Message}" : $"✘ {result.Message}");

            if (result.Success)
                await LoadRestorePointsAsync();
            else
                ShowLoading(false);
        }
        catch (InvalidOperationException ex)
        {
            SetStatus($"Error: {ex.Message}");
            ShowLoading(false);
        }
        catch (IOException ex)
        {
            SetStatus($"Error: {ex.Message}");
            ShowLoading(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            SetStatus($"Error: {ex.Message}");
            ShowLoading(false);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
            ShowLoading(false);
        }
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not RestorePointItem item) return;

        var confirm = new ContentDialog
        {
            Title = "Restore System",
            Content = $"Restore Windows to:\n\n\"{item.Description}\"\nCreated: {item.CreationTime}\n\nThe computer will restart. Unsaved work will be lost.\nContinue?",
            PrimaryButtonText = "Yes, Restore",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        var result = await confirm.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        SetStatus($"Initiating restore to point #{item.Id}...");
        var restoreResult = await RestorePointManager.RestoreToPointAsync(item.Id);
        SetStatus(restoreResult.Success ? $"✔ {restoreResult.Message}" : $"✘ {restoreResult.Message}");
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not RestorePointItem item) return;

        var confirm = new ContentDialog
        {
            Title = "Delete Restore Point",
            Content = $"Delete restore point:\n\n\"{item.Description}\"\nCreated: {item.CreationTime}\n\nThis action cannot be undone. Continue?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        var result = await confirm.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        SetStatus($"Deleting restore point #{item.Id}...");
        ShowLoading(true);

        try
        {
            // Windows vssadmin to remove a specific restore point by sequence number
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "vssadmin.exe",
                Arguments = $"delete shadows /Shadow={item.Id} /Quiet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync();
                SetStatus(process.ExitCode == 0
                    ? $"✔ Restore point deleted."
                    : $"✘ Could not delete via vssadmin (code {process.ExitCode}). Try running as administrator.");
            }

            await LoadRestorePointsAsync();
        }
        catch (InvalidOperationException ex)
        {
            SetStatus($"Error: {ex.Message}");
            ShowLoading(false);
        }
        catch (IOException ex)
        {
            SetStatus($"Error: {ex.Message}");
            ShowLoading(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            SetStatus($"Error: {ex.Message}");
            ShowLoading(false);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
            ShowLoading(false);
        }
    }

    /// <summary>View model for a single restore point row.</summary>
    private sealed class RestorePointItem
    {
        public int Id { get; set; }
        public string Description { get; set; } = "";
        public string CreationTime { get; set; } = "";
        public string TypeLabel { get; set; } = "";
        public RestorePoint Raw { get; set; } = null!;
    }
}
