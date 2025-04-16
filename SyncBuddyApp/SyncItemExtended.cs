using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Newtonsoft.Json;
using SyncBuddy.ViewModels;
using SyncBuddy.Views;
using SyncBuddyLib;

namespace SyncBuddy;

public class SyncItemExtended : SyncItem, INotifyPropertyChanged
{
    public static SyncItem CastToBase(SyncItemExtended syncItemExtended)
    {
        return new SyncItem(syncItemExtended as SyncItem);
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    
    [JsonIgnore]
    public ICommand Command_SyncNow { get; set; }
    [JsonIgnore]
    public ICommand Command_CopySourceDir { get; set; }
    [JsonIgnore]
    public ICommand Command_CopyTargetDir { get; set; }
    [JsonIgnore]
    
    public ICommand Command_OpenSourceDir { get; set; }
    [JsonIgnore]
    public ICommand Command_OpenTargetDir { get; set; }
    
    public SyncItemExtended(int id, string sourceDir, string targetDir, DateTime lastChecked, bool enabled, int periodMinutes) : base(id, sourceDir, targetDir, lastChecked, enabled, periodMinutes)
    {
        Command_SyncNow = new RelayCommand(SyncNow);
        Command_CopySourceDir = new RelayCommand(() => { CopyToClipboardAndNotify(SourceDir); });
        Command_CopyTargetDir = new RelayCommand(() => { CopyToClipboardAndNotify(TargetDir); });
        Command_OpenSourceDir = new RelayCommand(() =>
        {
            Process.Start(new ProcessStartInfo("explorer.exe", Path.GetFullPath(SourceDir)) { UseShellExecute = true });
        });
        Command_OpenTargetDir = new RelayCommand(() =>
        {
            Process.Start(new ProcessStartInfo("explorer.exe", Path.GetFullPath(TargetDir)) { UseShellExecute = true });
        });
        
        OnSyncStatusChanged += (sender, args) =>
        {
            OnPropertyChanged(nameof(SyncStatus));
            UpdateStatus();
            UpdateTimes();
        };

        OnAnyPropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(LastChecked))
                UpdateTimes();
            if (args.PropertyName is nameof(SourceDir) or nameof(TargetDir))
            {
                UpdateDirNames();
                Validate();
            }

            if (args.PropertyName is nameof(Enabled))
            {
                OnPropertyChanged(nameof(SyncStatus));
                UpdateStatus();
            }

            if (args.PropertyName is nameof(CurrentLog))
            {
                if (CurrentLog.Count > 0 && SyncStatus is SyncStatus.Synced or SyncStatus.Error)
                    SyncedAndLogExists = false;
                else
                    SyncedAndLogExists = true;

                LogExists = CurrentLog.Any();
            }
            
            // make sure we're not saving when editing temporary items :)
            if (SyncApp.Items.Contains(this))
                SyncApp.Save();
        };

        UpdateStatus();
        UpdateTimes();
        UpdateDirNames();
        SyncedAndLogExists = false;
        
        Validate();
    }

    protected SyncItemExtended() : base()
    {
        
    }

    public SyncItemExtended(SyncItemExtended source) : this(source.Id, source.SourceDir, source.TargetDir, source.LastChecked, source.Enabled, source.PeriodMinutes)
    {
    }

    public SyncItemExtended(SyncItem source) : this(source.Id, source.SourceDir, source.TargetDir, source.LastChecked, source.Enabled, source.PeriodMinutes)
    {
    }

    public async void Command_Edit()
    {
        await App.MainWindow.OpenEditDialog(this);
    }
    
    #region LastChecked

    [JsonIgnore]
    private string _lastCheckedAgo = "";
    
    public string LastCheckedAgo
    {
        get => _lastCheckedAgo;
        set
        {
            if (_lastCheckedAgo != value)
            {
                _lastCheckedAgo = value;
                OnPropertyChanged();
            }
        }
    }


    [JsonIgnore]
    private string _nextCheck = "";
    public string NextCheck
    {
        get => _nextCheck;
        set
        {
            if (_nextCheck != value)
            {
                _nextCheck = value;
                OnPropertyChanged();
            }
        }
    }
        
    public void UpdateTimes()
    {
        var diff = DateTime.Now - LastChecked;
        LastCheckedAgo = diff.TotalMinutes < 1 ? "less than a minute" :
            diff.TotalHours < 1 ? $"{(int)diff.TotalMinutes} minute{((int)diff.TotalMinutes == 1 ? "" : "s")}" :
            diff.TotalDays < 1 ? $"{(int)diff.TotalHours} hour{((int)diff.TotalMinutes == 1 ? "" : "s")}" :
            "more than a day";
        
        NextCheck = (LastChecked + TimeSpan.FromMinutes(PeriodMinutes)).ToString("HH:mm:ss");
    }
    
    #endregion
    
    #region SmallText    
    
    private bool _showSmallText = true;
    public bool ShowSmallText
    {
        get => _showSmallText;
        set
        {
            if (_showSmallText != value)
            {
                _showSmallText = value;
                OnPropertyChanged();
            }
        }
    }




    #endregion

    #region Status

    private void UpdateStatus()
    {
        SyncingIcon = SyncStatus switch
        {
            SyncStatus.Idle => MaterialIconKind.QuestionMark,
            SyncStatus.Syncing => MaterialIconKind.Sync,
            SyncStatus.Synced => MaterialIconKind.Check,
            SyncStatus.Error => MaterialIconKind.Error,
            SyncStatus.Stopped => MaterialIconKind.Stop,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (!Enabled)
            SyncingIcon = MaterialIconKind.Stop;

        StatusText = SyncStatus switch
        {
            SyncStatus.Idle => $"Not synced yet",
            SyncStatus.Syncing => $"Synchronizing...",
            SyncStatus.Synced => $"In sync",
            SyncStatus.Error => $"Error",
            SyncStatus.Stopped => $"Disabled",
            _ => throw new ArgumentOutOfRangeException()
        };
        
        if (!Enabled)
            StatusText = "Disabled";
        
        ShowSmallText = SyncStatus is SyncStatus.Synced or SyncStatus.Idle;

        SyncingStatusIconColor = SyncStatus switch
        {
            SyncStatus.Idle => new SolidColorBrush(Colors.White),
            SyncStatus.Syncing => new SolidColorBrush(Colors.Orange),
            SyncStatus.Synced => new SolidColorBrush(Colors.LimeGreen),
            SyncStatus.Error => new SolidColorBrush(Colors.Red),
            SyncStatus.Stopped => new SolidColorBrush(Colors.Gray),
            _ => throw new ArgumentOutOfRangeException()
        };

        if (!Enabled)
            SyncingStatusIconColor = new SolidColorBrush(Colors.Gray);
    }

    [JsonIgnore]
    private MaterialIconKind _syncingIcon = MaterialIconKind.Loading;
    
    public MaterialIconKind SyncingIcon
    {
        get => _syncingIcon;
        set
        {
            if (_syncingIcon != value)
            {
                _syncingIcon = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonIgnore]
    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonIgnore]
    private SolidColorBrush _syncingStatusIconColor = new(Colors.White);

    public SolidColorBrush SyncingStatusIconColor
    {
        get => _syncingStatusIconColor;
        set
        {
            if (_syncingStatusIconColor != value)
            {
                _syncingStatusIconColor = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion

    #region DirNames

    public void UpdateDirNames()
    {
        SourceDirDisplay = Beautify(SourceDir);
        TargetDirDisplay = Beautify(TargetDir);
        return;

        string Beautify(string input)
        {
            if (input.Length <= 28)
                return input;

            var parts = input.Split('/');
            var folderName = parts.Last();
            if (folderName.Length > 25)
            {
                folderName = "*" + '/' + folderName[^19..];
            }
            return parts[0] + '/' + "..." + '/' + folderName;
        }
    }
    
    [JsonIgnore]
    private string _sourceDirDisplay = "";

    public string SourceDirDisplay
    {
        get => _sourceDirDisplay;
        set
        {
            if (_sourceDirDisplay != value)
            {
                _sourceDirDisplay = value;
                OnPropertyChanged();
            }
        }
    }

    [JsonIgnore]
    private string _targetDirDisplay = "";

    public string TargetDirDisplay
    {
        get => _targetDirDisplay;
        set
        {
            if (_targetDirDisplay != value)
            {
                _targetDirDisplay = value;
                OnPropertyChanged();
            }
        }
    }

    public async void CopyToClipboardAndNotify(string toCopy)
    {
        await App.MainWindow.Clipboard.SetTextAsync(toCopy);
        App.NotificationManager?.Show(new Notification("Copied to clipboard", toCopy, NotificationType.Success));
    }

    #endregion

    #region Logs

    private bool _syncedAndLogExists = false;

    public bool SyncedAndLogExists
    {
        get => _syncedAndLogExists;
        set
        {
            if (_syncedAndLogExists != value)
            {
                _syncedAndLogExists = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _logExists = false;

    public bool LogExists
    {
        get => _logExists;
        set
        {
            if (_logExists != value)
            {
                _logExists = value;
                OnPropertyChanged();
            }
        }
    }

    public async void CopyLog()
    {
        App.MainWindow.Clipboard.SetTextAsync(string.Join(Environment.NewLine, CurrentLog));
        App.NotificationManager?.Show(new Notification("Copied to clipboard", "Log copied", NotificationType.Success));
    }

    #endregion

    #region Editing

    private void Validate()
    {
        CanSave = AreDirectoriesIndependent(SourceDir, TargetDir);
    }

    [JsonIgnore]
    private bool _canSave = false;
    public bool CanSave
    {
        get => _canSave;
        set
        {
            if (_canSave != value)
            {
                _canSave = value;
                OnPropertyChanged();
            }
        }
    }

    public async Task BrowseSourceDir()
    {
        if (App.MainWindow.EditWindow is null)
            return;
        var folder =
            await App.MainWindow.EditWindow?.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
                { AllowMultiple = false })!;
        if (folder.Any())
        {
            SourceDir = folder.First().Path.AbsolutePath;
        }
    }

    public async Task BrowseTargetDir()
    {
        if (App.MainWindow.EditWindow is null)
            return;
        var folder =
            await App.MainWindow.EditWindow?.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
                { AllowMultiple = false })!;
        if (folder.Any())
        {
            TargetDir = folder.First().Path.AbsolutePath;
        }
    }

    #endregion

    protected override bool IsMasterEnabled => SyncApp.IsActive;
}