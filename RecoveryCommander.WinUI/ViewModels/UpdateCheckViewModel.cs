using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RecoveryCommanderWinUI.ViewModels;

/// <summary>
/// ViewModel for the update check dialog.
/// Handles update availability and release notes display.
/// </summary>
#pragma warning disable MVVMTK0045
public partial class UpdateCheckViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "Updates Available";

    [ObservableProperty]
    private string currentVersion = "";

    [ObservableProperty]
    private string latestVersion = "";

    [ObservableProperty]
    private string releaseNotes = "";

    [ObservableProperty]
    private string downloadUrl = "";

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private string? errorMessage = null;

    public UpdateCheckViewModel()
    {
    }

    public void SetUpdateInfo(string current, string latest, string notes, string url)
    {
        CurrentVersion = current;
        LatestVersion = latest;
        ReleaseNotes = notes;
        DownloadUrl = url;
    }

    public void SetError(string error)
    {
        ErrorMessage = error;
    }

    public void SetLoading(bool loading)
    {
        IsLoading = loading;
    }
#pragma warning restore MVVMTK0045
}


