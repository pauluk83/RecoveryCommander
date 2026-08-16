using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace RecoveryCommanderWinUI.Dialogs;

/// <summary>
/// Base class for window-based dialogs with async show capability.
/// This provides an alternative to ContentDialog for dialogs that need custom sizing.
/// </summary>
public class BaseWindowDialog : Window
{
    private TaskCompletionSource<bool>? _showCompletionSource;

    public BaseWindowDialog()
    {
        this.Closed += OnClosed;
    }

    /// <summary>
    /// Shows the window asynchronously and returns when it's closed.
    /// </summary>
    public Task<bool> ShowAsync()
    {
        _showCompletionSource = new TaskCompletionSource<bool>();
        this.Activate();
        return _showCompletionSource.Task;
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _showCompletionSource?.TrySetResult(true);
    }

    /// <summary>
    /// Centers the window on the screen.
    /// </summary>
    public void CenterOnScreen()
    {
        if (App.MainWindow is null) return;

        var mainWindow = App.MainWindow;
        var mainWindowBounds = mainWindow.Bounds;

        this.AppWindow.Move(new Windows.Graphics.PointInt32(
            (int)(mainWindowBounds.X + (mainWindowBounds.Width - this.AppWindow.Size.Width) / 2),
            (int)(mainWindowBounds.Y + (mainWindowBounds.Height - this.AppWindow.Size.Height) / 2)
        ));
    }
}
