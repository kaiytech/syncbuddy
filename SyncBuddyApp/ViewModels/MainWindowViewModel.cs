using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SyncBuddyLib;

namespace SyncBuddy.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
    public MainWindowViewModel()
    {
        AppActive = true;
    }
    
    public string AppName => "SyncBuddy";

    public ObservableCollection<SyncItemExtended> SyncItems => SyncApp.Items;
    public void CallChanged()
    {
        OnPropertyChanged(nameof(SyncItems));
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool _appActive;

    public bool AppActive
    {
        get => _appActive;
        set
        {
            if (_appActive != value)
            {
                _appActive = value;
                OnPropertyChanged();
            }
        }
    }

}
