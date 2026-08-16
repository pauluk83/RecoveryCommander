/*
 * AUDIT HEADER
 * File: SettingsWindow.xaml.cs
 * Module: RecoveryCommander.WinUI
 * Created: 2026-05-21
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-05-21 - 1.2.8 - Initial settings window with Allow Unverified Downloads toggle.
 * 2026-07-15 - 1.3.0 - Wired AppFeatureSettings persistence (load on open, save on Save button).
 *                       Added Download Safety Settings help dialog explaining the policy.
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using RecoveryCommander.Core;
using RecoveryCommanderWinUI.Dialogs;
using System;
using System.Threading.Tasks;

namespace RecoveryCommanderWinUI;

public sealed partial class SettingsWindow : BaseWindowDialog
{
    public SettingsWindow()
    {
        this.InitializeComponent();
        LoadSettings();
    }

    public new async Task ShowAsync()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
        await base.ShowAsync();
    }

    private void LoadSettings()
    {
        var settings = AppFeatureSettings.Load();
        AllowUnverifiedDownloadsCheck.IsChecked = settings.AllowUnverifiedDownloads;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = AppFeatureSettings.Load();
        settings.AllowUnverifiedDownloads = AllowUnverifiedDownloadsCheck.IsChecked == true;
        AppFeatureSettings.Save(settings);
        Close();
    }

    private async void UnverifiedDownloadsHelpButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowDownloadSafetyDialogAsync();
    }

    private async Task ShowDownloadSafetyDialogAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Download Safety Settings",
            XamlRoot = this.Content?.XamlRoot,
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close
        };

        var panel = new StackPanel { Spacing = 14, Padding = new Thickness(0, 8, 0, 0) };

        panel.Children.Add(new TextBlock
        {
            Text = "What does \"Allow download verification bypass\" mean?",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            TextWrapping = TextWrapping.Wrap
        });

        panel.Children.Add(new TextBlock
        {
            Text = "RecoveryCommander's supply-chain policy requires all third-party downloads in the catalog " +
                   "to carry a pinned SHA-256 hash. Before running any tool, the downloaded file is " +
                   "compared against that hash. If the file has been tampered with or corrupted, the " +
                   "download is rejected and deleted automatically. This setting lets you override that " +
                   "behavior for missing, unavailable, or mismatched SHA-256 data.",
            FontSize = 12,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 180, 200, 210)),
            TextWrapping = TextWrapping.Wrap
        });

        panel.Children.Add(new TextBlock
        {
            Text = "When would this be used?",
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            TextWrapping = TextWrapping.Wrap
        });

        panel.Children.Add(new TextBlock
        {
            Text = "This is useful when a catalog entry has no pinned SHA-256 hash, when the checksum asset is unavailable, " +
                   "or when a downloaded file fails SHA-256 validation. By default these cases are blocked to protect you, " +
                   "but the override allows a technician to continue for testing or emergency recovery.",
            FontSize = 12,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 180, 200, 210)),
            TextWrapping = TextWrapping.Wrap
        });

        panel.Children.Add(new TextBlock
        {
            Text = "When should I enable this?",
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            TextWrapping = TextWrapping.Wrap
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Only enable this if you trust all catalog sources and understand the risk. " +
                   "This setting is intended for advanced technicians or for testing. " +
                   "It can also be overridden on a per-launch basis by setting the environment variable " +
                   "RC_ALLOW_UNVERIFIED_DOWNLOAD=1 before launching RecoveryCommander.",
            FontSize = 12,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 180, 200, 210)),
            TextWrapping = TextWrapping.Wrap
        });

        // Warning banner
        var warningBorder = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 255, 160, 0)),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(180, 255, 160, 0)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8, 12, 8)
        };
        warningBorder.Child = new TextBlock
        {
            Text = "\u26a0\ufe0f  Recommendation: Keep this disabled unless you are an advanced user who has reviewed the catalog sources. Unverified downloads skip SHA-256 integrity checking.",
            FontSize = 11,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 200, 100)),
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(warningBorder);

        dialog.Content = new ScrollViewer
        {
            Content = panel,
            MaxHeight = 420,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        await dialog.ShowAsync();
    }
}
