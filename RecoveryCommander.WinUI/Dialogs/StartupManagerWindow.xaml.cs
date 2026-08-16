using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RecoveryCommander.Features;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI;

namespace RecoveryCommanderWinUI.Dialogs;

/// <summary>
/// Startup Manager window - view, enable, disable and remove Windows startup programs.
/// Window-based implementation to overcome ContentDialog width limitations.
/// </summary>
public sealed partial class StartupManagerWindow : BaseWindowDialog
{
    private ObservableCollection<StartupItemVM> _items = new();

    public StartupManagerWindow()
    {
        InitializeComponent();
        this.Activated += async (_, _) => await LoadItemsAsync();
    }

    private async Task LoadItemsAsync()
    {
        ShowLoading(true);
        SetStatus("Loading startup items...");
        _items.Clear();

        try
        {
            var items = await StartupManager.GetStartupItemsAsync();

            foreach (var item in items)
            {
                _items.Add(new StartupItemVM(item));
            }

            if (_items.Count == 0)
            {
                EmptyPanel.Visibility = Visibility.Visible;
                StartupListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                StartupListView.ItemsSource = _items;
                EmptyPanel.Visibility = Visibility.Collapsed;
                StartupListView.Visibility = Visibility.Visible;
            }

            SetStatus($"{_items.Count} startup item(s) loaded. Last updated: {DateTime.Now:HH:mm:ss}");
            SubtitleText.Text = $"{_items.Count} startup programs across all registry hives and startup folders";
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.ComponentModel.Win32Exception)
        {
            SetStatus($"Error loading startup items: {ex.Message}");
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
            StartupListView.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        => await LoadItemsAsync();

    private void ShowAllToggle_Click(object sender, RoutedEventArgs e)
    {
        // Future: filter by scope
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private async void ItemToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggle || toggle.Tag is not StartupItemVM vm) return;

        // Prevent feedback loop
        toggle.Toggled -= ItemToggled;
        try
        {
            bool success;
            if (vm.IsEnabled)
            {
                SetStatus($"Enabling \"{vm.Name}\"...");
                success = await StartupManager.EnableStartupItemAsync(vm.Raw);
                SetStatus(success ? $"✔ \"{vm.Name}\" enabled." : $"✘ Failed to enable \"{vm.Name}\".");
            }
            else
            {
                SetStatus($"Disabling \"{vm.Name}\"...");
                success = await StartupManager.DisableStartupItemAsync(vm.Raw);
                SetStatus(success ? $"✔ \"{vm.Name}\" disabled." : $"✘ Failed to disable \"{vm.Name}\".");
            }

            if (!success)
            {
                // Revert UI
                vm.IsEnabled = !vm.IsEnabled;
                toggle.IsOn = vm.IsEnabled;
            }
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
        {
            SetStatus($"Error: {ex.Message}");
        }
        finally
        {
            toggle.Toggled += ItemToggled;
        }
    }

    private async void DeleteItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not StartupItemVM vm) return;

        var confirm = new ContentDialog
        {
            Title = "Remove Startup Item",
            Content = $"Remove \"{vm.Name}\" from startup?\n\nLocation: {vm.Location}\n\nThis will prevent it from launching at Windows startup.",
            PrimaryButtonText = "Remove",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = App.MainWindow?.Content?.XamlRoot
        };

        var result = await confirm.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        SetStatus($"Removing \"{vm.Name}\"...");
        try
        {
            var success = await StartupManager.DeleteStartupItemAsync(vm.Raw);
            if (success)
            {
                _items.Remove(vm);
                SetStatus($"✔ \"{vm.Name}\" removed from startup.");
                SubtitleText.Text = $"{_items.Count} startup programs";
            }
            else
            {
                SetStatus($"✘ Failed to remove \"{vm.Name}\". Try running as administrator.");
            }
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
        {
            SetStatus($"Error: {ex.Message}");
        }
    }

    /// <summary>View model wrapper around StartupItem with INotifyPropertyChanged support.</summary>
    private sealed class StartupItemVM : INotifyPropertyChanged
    {
        public StartupItem Raw { get; }

        public string Name => Raw.Name;
        public string Command => Raw.Command;
        public string Location => Raw.Location;
        public string ScopeLabel => Raw.Scope == StartupScope.AllUsers ? "All Users" : "Current User";

        public SolidColorBrush ScopeBrush => Raw.Scope == StartupScope.AllUsers
            ? new SolidColorBrush(Color.FromArgb(0x40, 0x18, 0xCF, 0xFF))
            : new SolidColorBrush(Color.FromArgb(0x40, 0x86, 0xA8, 0xFF));

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public StartupItemVM(StartupItem raw)
        {
            Raw = raw;
            _isEnabled = raw.IsEnabled;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
