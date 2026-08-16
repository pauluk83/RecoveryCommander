using Microsoft.UI.Xaml.Controls;
using RecoveryCommanderWinUI.ViewModels;

namespace RecoveryCommanderWinUI.Dialogs;

/// <summary>
/// Dialog for displaying update availability and release notes.
/// </summary>
public sealed partial class UpdateCheckDialog : ContentDialog
{
    public UpdateCheckViewModel ViewModel { get; set; }

    public UpdateCheckDialog()
    {
        InitializeComponent();
        ViewModel = new UpdateCheckViewModel();
        DataContext = ViewModel;
    }

    public void SetUpdateInfo(string currentVersion, string latestVersion, string releaseNotes, string downloadUrl)
    {
        ViewModel.SetUpdateInfo(currentVersion, latestVersion, releaseNotes, downloadUrl);
        CurrentVersionText.Text = currentVersion;
        LatestVersionText.Text = latestVersion;
        ReleaseNotesText.Text = releaseNotes;
    }

    public void SetError(string errorMessage)
    {
        ViewModel.SetError(errorMessage);
        ErrorMessage.Text = errorMessage;
        ErrorMessage.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
    }

    public string GetDownloadUrl()
    {
        return ViewModel.DownloadUrl;
    }
}


