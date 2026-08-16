using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using RecoveryCommanderWinUI.Dialogs;

namespace RecoveryCommanderWinUI.Services;

public sealed class WinReWizardService : IWinReWizardService
{
    private readonly IDialogService _dialogService;

    public WinReWizardService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task<bool> RunPbrSetupWizardAsync(IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
    {
        var dialog = new WinREWizardDialog(_dialogService, progress, reportOutput, cancellationToken);
        dialog.XamlRoot = App.MainWindow?.Content.XamlRoot;
        var result = await dialog.ShowAsync();
        return dialog.CompletedSuccessfully;
    }
}


