using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RecoveryCommanderWinUI.ViewModels;
using System.Collections.Generic;

namespace RecoveryCommanderWinUI.Dialogs;

/// <summary>
/// Dialog for selecting items from a list (e.g., updates to install).
/// Replaces the WinForms UpdateSelectorForm.
/// </summary>
public sealed partial class ItemSelectorDialog : ContentDialog
{
    private ItemSelectorViewModel<object>? _viewModel;

    public ItemSelectorDialog()
    {
        InitializeComponent();
    }

    public void SetViewModel<T>(ItemSelectorViewModel<T> viewModel) where T : class
    {
        _viewModel = (ItemSelectorViewModel<object>)(object)viewModel;
        DataContext = _viewModel;
    }

    public IEnumerable<T> GetSelectedItems<T>() where T : class
    {
        if (_viewModel == null) return new List<T>();
        return (IEnumerable<T>)_viewModel.GetSelectedItems();
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.SelectAll();
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.ClearAll();
    }
}


