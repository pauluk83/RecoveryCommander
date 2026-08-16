using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RecoveryCommanderWinUI.Dialogs;

public sealed partial class WinREWizardDialog : ContentDialog, IDisposable
{
    private readonly IDialogService _dialogService;
    private readonly CancellationToken _cancellationToken;
    private readonly IProgress<ProgressReport> _externalProgress;
    private readonly Action<string> _externalReportOutput;
    private readonly WinREWizards _wizard;
    private readonly DispatcherQueue _dispatcher;

    private bool _scanStateCompleted;
    private bool _oemRegistrationCompleted;

    public bool CompletedSuccessfully { get; private set; }

    public WinREWizardDialog(IDialogService dialogService, IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken cancellationToken)
    {
        InitializeComponent();
        _dialogService = dialogService;
        _cancellationToken = cancellationToken;
        _externalProgress = progress;
        _externalReportOutput = reportOutput;
        _wizard = new WinREWizards(ReportOutput);
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        Closed += (_, _) => _wizard.Dispose();

        ActionButton.Content = _scanStateCompleted ? "Re-run ScanState Capture" : "Start ScanState Capture";
        SelectImageButton.Visibility = _scanStateCompleted ? Visibility.Visible : Visibility.Collapsed;
        WizardProgressBar.Value = 0;
    }

    public void Dispose()
    {
        _wizard.Dispose();
    }

    private void SetProgress(int percent, string message)
    {
        _dispatcher.TryEnqueue(() =>
        {
            WizardProgressBar.IsIndeterminate = percent < 0;
            WizardProgressBar.Value = Math.Clamp(percent, 0, 100);
            if (!string.IsNullOrWhiteSpace(message))
            {
                OutputTextBox.Text += $"[{percent}%] {message}\r\n";
            }
        });
    }

    private void ReportOutput(string message)
    {
        _dispatcher.TryEnqueue(() =>
        {
            OutputTextBox.Text += $">>> {message}\r\n";
        });

        _externalReportOutput?.Invoke(message);
    }

    private async void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        ActionButton.IsEnabled = false;
        SelectImageButton.IsEnabled = false;

        try
        {
            await _wizard.PerformScanStateCaptureAsync(new Progress<(int, string)>(tuple =>
            {
                SetProgress(tuple.Item1, tuple.Item2);
                _externalProgress?.Report(new ProgressReport(tuple.Item1, tuple.Item2));
            }), _cancellationToken);

            _scanStateCompleted = _wizard.ScanStateCompleted;
            if (_scanStateCompleted)
            {
                ReportOutput("ScanState capture finished. Proceed to OEM image registration.");
                _dispatcher.TryEnqueue(() =>
                {
                    SelectImageButton.Visibility = Visibility.Visible;
                    SelectImageButton.IsEnabled = true;
                    ActionButton.Content = "Capture Again";
                    ActionButton.IsEnabled = true;
                    StepTitle.Text = "OEM Image Registration";
                    StepDescription.Text = "Choose a recovery image (winre.wim) to register for Push-Button Reset.";
                });
            }
        }
        catch (OperationCanceledException)
        {
            ReportOutput("ScanState capture cancelled.");
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is IOException || ex is UnauthorizedAccessException || ex is System.ComponentModel.Win32Exception)
        {
            ReportOutput($"ScanState capture failed: {ex.Message}");
        }
    }

    private async void SelectImageButton_Click(object sender, RoutedEventArgs e)
    {
        SelectImageButton.IsEnabled = false;
        ActionButton.IsEnabled = false;

        try
        {
            var selected = _dialogService.ShowOpenFileDialog("Recovery WIM (winre.wim)|winre.wim|WIM Files (*.wim)|*.wim|All Files (*.*)|*.*", "Locate the OEM recovery WIM image", null);
            if (string.IsNullOrWhiteSpace(selected))
            {
                ReportOutput("OEM image selection was cancelled.");
                return;
            }

            ReportOutput($"Selected OEM image: {selected}");
            await _wizard.PerformOemRegistrationAsync(selected, new Progress<(int, string)>(tuple =>
            {
                SetProgress(tuple.Item1, tuple.Item2);
                _externalProgress?.Report(new ProgressReport(tuple.Item1, tuple.Item2));
            }), _cancellationToken);

            _oemRegistrationCompleted = _wizard.OemRegistrationCompleted;
            if (_oemRegistrationCompleted)
            {
                ReportOutput("OEM image registration completed successfully.");
                _dispatcher.TryEnqueue(() =>
                {
                    StepTitle.Text = "Complete";
                    StepDescription.Text = "Push-Button Reset setup is complete. Close this dialog to finish.";
                    SelectImageButton.Visibility = Visibility.Collapsed;
                    ActionButton.IsEnabled = true;
                    ActionButton.Content = "Start Again";
                });
                CompletedSuccessfully = true;
            }
        }
        catch (OperationCanceledException)
        {
            ReportOutput("OEM registration cancelled.");
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is IOException || ex is UnauthorizedAccessException || ex is System.ComponentModel.Win32Exception)
        {
            ReportOutput($"OEM registration failed: {ex.Message}");
        }
        finally
        {
            if (!_oemRegistrationCompleted)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    SelectImageButton.IsEnabled = true;
                    ActionButton.IsEnabled = true;
                });
            }
        }
    }
}


