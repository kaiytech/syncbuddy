using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SyncBuddyLib;

namespace SyncBuddy.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
    public string AppName => "SyncBuddy";

    public ObservableCollection<SyncItemExtended> SyncItems => SyncManager.Items;
    public void CallChanged()
    {
        OnPropertyChanged(nameof(SyncItems));
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
