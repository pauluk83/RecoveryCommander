using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace RecoveryCommanderWinUI.ViewModels;

/// <summary>
/// Represents a selectable item in a list dialog.
/// </summary>
#pragma warning disable MVVMTK0045
public partial class SelectableItem : ObservableObject
{
    [ObservableProperty]
    private bool isSelected = true;

    [ObservableProperty]
    private object? item;

    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private string description = "";

    [ObservableProperty]
    private string size = "";
}

/// <summary>
/// ViewModel for item selection dialogs (e.g., update selection).
/// Manages multiple items with selection state.
/// </summary>
public partial class ItemSelectorViewModel<T> : ObservableObject where T : class
{
    [ObservableProperty]
    private string title = "Select Items";

    [ObservableProperty]
    private string totalSizeText = "";
#pragma warning restore MVVMTK0045

    public ObservableCollection<SelectableItem> Items { get; } = new();

    private readonly System.Func<T, string>? _getSizeString;

    public ItemSelectorViewModel(System.Func<T, string>? getSizeString = null)
    {
        _getSizeString = getSizeString;
    }

    public void AddItem(T item, string name, string description, string size = "")
    {
        var selectableItem = new SelectableItem { Item = item, Name = name, Description = description, Size = size };
        Items.Add(selectableItem);
    }

    public void SelectAll()
    {
        foreach (var item in Items)
        {
            item.IsSelected = true;
        }
        UpdateTotalSize();
    }

    public void ClearAll()
    {
        foreach (var item in Items)
        {
            item.IsSelected = false;
        }
        UpdateTotalSize();
    }

    public System.Collections.Generic.IEnumerable<T> GetSelectedItems()
    {
        return Items.Where(i => i.IsSelected).Select(i => (T)i.Item!);
    }

    private void UpdateTotalSize()
    {
        if (_getSizeString == null) return;

        var total = Items.Where(i => i.IsSelected)
                        .Select(i => _getSizeString((T)i.Item!))
                        .Aggregate("", (acc, s) => acc + s);

        TotalSizeText = total;
    }
}


