using CommunityToolkit.Mvvm.ComponentModel;
using RecoveryCommander.Contracts;
using System.Collections.ObjectModel;
using System.Linq;

namespace RecoveryCommanderWinUI.ViewModels;

public partial class ModuleViewModel : ObservableObject
{
    private readonly IRecoveryModule _module;

    public string Name => _module.Name;
    public string Description => _module.Description;

    public ObservableCollection<ModuleAction> Actions { get; }

    public IRecoveryModule Module => _module;

    public ModuleViewModel(IRecoveryModule module)
    {
        _module = module;
        Actions = new ObservableCollection<ModuleAction>(module.Actions.Where(a => !a.IsHeader));
    }
}


