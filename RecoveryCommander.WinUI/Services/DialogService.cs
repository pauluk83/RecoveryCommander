using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RecoveryCommander.Contracts;
using RecoveryCommanderWinUI.Dialogs;
using RecoveryCommanderWinUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace RecoveryCommanderWinUI.Services;

/// <summary>
/// Service for managing WinUI3 dialogs in the application.
/// Provides consistent dialog handling across the app.
/// </summary>
public class DialogService : IDialogService
{
    private readonly Window _mainWindow;

    public DialogService(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    /// <summary>
    /// Shows an information dialog with OK button.
    /// </summary>
    public async Task ShowInfoAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "OK",
            XamlRoot = _mainWindow.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows a warning dialog with OK button.
    /// </summary>
    public async Task ShowWarningAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "OK",
            XamlRoot = _mainWindow.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows an error dialog with OK button.
    /// </summary>
    public async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "OK",
            XamlRoot = _mainWindow.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons.
    /// </summary>
    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No",
            XamlRoot = _mainWindow.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Shows a custom dialog content.
    /// </summary>
    public async Task<ContentDialogResult> ShowCustomAsync(ContentDialog dialog)
    {
        dialog.XamlRoot = _mainWindow.Content.XamlRoot;
        return await dialog.ShowAsync();
    }

    /// <summary>
    /// Shows a custom dialog and returns a result.
    /// </summary>
    public async Task<T?> ShowCustomAsync<T>(ContentDialog dialog) where T : class
    {
        dialog.XamlRoot = _mainWindow.Content.XamlRoot;
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.Content is T content)
        {
            return content;
        }

        return null;
    }

    /// <summary>
    /// Shows a content dialog with the specified content and title.
    /// </summary>
    public void ShowContentDialog(string content, string title)
    {
        // Run on UI thread
        _mainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = "OK",
                XamlRoot = _mainWindow.Content.XamlRoot
            };

            // Fire and forget for synchronous interface
            _ = dialog.ShowAsync();
        });
    }

    /// <summary>
    /// Shows an open file dialog with the specified filter and title.
    /// </summary>
    public string? ShowOpenFileDialog(string filter, string title, string? initialDirectory = null)
    {
        // This is a synchronous wrapper around an async operation
        // We need to block since the interface is synchronous
        var task = ShowOpenFileDialogAsync(filter, title, initialDirectory);
        try
        {
            return task.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    private async Task<string?> ShowOpenFileDialogAsync(string filter, string title, string? initialDirectory)
    {
        var hwnd = WindowNative.GetWindowHandle(_mainWindow);
        var filePicker = new FileOpenPicker();

        // Initialize the file picker with the window handle
        InitializeWithWindow.Initialize(filePicker, hwnd);

        filePicker.ViewMode = PickerViewMode.List;
        filePicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        filePicker.FileTypeFilter.Clear();

        // Parse filter string (format: "Description|*.ext|Description2|*.ext2")
        var filterParts = filter.Split('|');
        for (int i = 1; i < filterParts.Length; i += 2)
        {
            if (i < filterParts.Length)
            {
                var extensions = filterParts[i].Split(';');
                foreach (var ext in extensions)
                {
                    filePicker.FileTypeFilter.Add(ext.TrimStart('*'));
                }
            }
        }

        StorageFile? file = await filePicker.PickSingleFileAsync();
        return file?.Path;
    }

    /// <summary>
    /// Shows a folder browser dialog with the specified description.
    /// </summary>
    public string? ShowFolderBrowserDialog(string description)
    {
        var task = ShowFolderBrowserDialogAsync(description);
        try
        {
            return task.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    private async Task<string?> ShowFolderBrowserDialogAsync(string description)
    {
        var hwnd = WindowNative.GetWindowHandle(_mainWindow);
        var folderPicker = new FolderPicker();

        // Initialize the folder picker with the window handle
        InitializeWithWindow.Initialize(folderPicker, hwnd);

        folderPicker.ViewMode = PickerViewMode.List;
        folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        folderPicker.FileTypeFilter.Add("*");

        StorageFolder? folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }

    /// <summary>
    /// Shows an item selection dialog for selecting items from a list.
    /// </summary>
    public bool ShowItemSelectionDialog<T>(List<T> items, string title, Func<T, object[]> rowData, Func<T, string> sizeFetch, out List<T> selectedItems) where T : class
    {
        selectedItems = new List<T>();

        // This is a synchronous wrapper around an async operation
        var task = ShowItemSelectionDialogAsync(items, title, rowData, sizeFetch);
        try
        {
            var result = task.GetAwaiter().GetResult();
            if (result != null)
            {
                selectedItems = result;
                return true;
            }
            return false;
        }
        catch (Exception ex) when (ex is OperationCanceledException || ex is InvalidOperationException || ex is System.Runtime.InteropServices.COMException)
        {
            return false;
        }
    }

    private async Task<List<T>?> ShowItemSelectionDialogAsync<T>(List<T> items, string title, Func<T, object[]> rowData, Func<T, string> sizeFetch) where T : class
    {
        var viewModel = new ItemSelectorViewModel<T>(sizeFetch);
        
        // Add items to the view model
        foreach (var item in items)
        {
            var data = rowData(item);
            var name = data.Length > 0 ? data[0]?.ToString() ?? "" : "";
            var description = data.Length > 1 ? data[1]?.ToString() ?? "" : "";
            var size = sizeFetch(item);
            viewModel.AddItem(item, name, description, size);
        }

        var dialog = new ItemSelectorDialog
        {
            Title = title,
            XamlRoot = _mainWindow.Content.XamlRoot
        };
        dialog.SetViewModel(viewModel);

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            return dialog.GetSelectedItems<T>().ToList();
        }

        return null;
    }
}


