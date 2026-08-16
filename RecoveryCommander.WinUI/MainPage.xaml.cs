using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RecoveryCommanderWinUI;

/// <summary>
/// The main content page displayed inside the application window.
/// Add your UI logic, event handlers, and data binding here.
/// </summary>
public sealed partial class MainPage : Page
{
    public ViewModels.MainViewModel ViewModel { get; } = new ViewModels.MainViewModel();

    public MainPage()
    {
        InitializeComponent();
        this.DataContext = ViewModel;
    }

    private void TerminalOutputBox_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        TerminalScrollViewer.ChangeView(null, TerminalScrollViewer.ScrollableHeight, null);
    }

    private async void TerminalInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            var command = TerminalInputBox.Text?.Trim();
            if (!string.IsNullOrEmpty(command))
            {
                TerminalInputBox.Text = string.Empty;
                await ViewModel.ExecuteTerminalCommandAsync(command);
            }
        }
    }
}


