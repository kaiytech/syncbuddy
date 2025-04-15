using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

namespace SyncBuddyLib;

[JsonObject]
public class SyncItem
{
    public static bool AreDirectoriesIndependent(string sourceDir, string targetDir)
    {
        if (string.IsNullOrEmpty(sourceDir) || string.IsNullOrEmpty(targetDir))
            return false;
        var full1 = Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full2 = Path.GetFullPath(targetDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (full1 == full2)
            return false;
        return !full1.StartsWith(full2, StringComparison.OrdinalIgnoreCase)
               && !full2.StartsWith(full1, StringComparison.OrdinalIgnoreCase);
    }

    protected SyncItem()
    {
        
    }
    
    public SyncItem(int id, string sourceDir, string targetDir)
    {
        Id = id;
        SourceDir = sourceDir;
        TargetDir = targetDir;
        LastChecked = DateTime.Now;
        SyncStatus = SyncStatus.Idle;
        CurrentLog.CollectionChanged += (sender, args) => 
            OnAnyPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLog)));
    }

    public SyncItem(SyncItem source) : this(source.Id, source.SourceDir, source._targetDir)
    {
        LastChecked = source.LastChecked;
        SyncStatus = source.SyncStatus;
        
        CurrentLog.Clear();
        foreach (var se in source.CurrentLog)
            CurrentLog.Add(se);
    }
    
    public event EventHandler<PropertyChangedEventArgs>? OnAnyPropertyChanged;

    [JsonProperty("id")] public int Id { get; }

    [JsonProperty("source_dir")] private string _sourceDir = string.Empty;
    [JsonIgnore]
    public string SourceDir
    {
        get => _sourceDir;
        set
        {
            if (_sourceDir != value)
            {
                _sourceDir = value;
                OnAnyPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SourceDir)));
            }
        }
    }
    
    [JsonProperty("target_dir")] private string _targetDir = string.Empty;
    [JsonIgnore]
    public string TargetDir
    {
        get => _targetDir;
        set
        {
            if (_targetDir != value)
            {
                _targetDir = value;
                OnAnyPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TargetDir)));
            }
        }
    }


    [JsonProperty("is_enabled")]
    private bool _enabled = true;

    [JsonIgnore]
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                OnAnyPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
            }
        }
    }


    [JsonProperty("last_checked")]
    private DateTime _lastChecked;
    

    [JsonIgnore]
    public DateTime LastChecked
    {
        get => _lastChecked;
        set
        {
            if (_lastChecked != value)
            {
                _lastChecked = value;
                OnAnyPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastChecked)));
            }
        }
    }

    [JsonProperty("period_minutes")]
    private int _periodMinutes = 2;

    [JsonIgnore]
    public int PeriodMinutes
    {
        get => _periodMinutes;
        set
        {
            if (_periodMinutes != value)
            {
                _periodMinutes = value;
                OnAnyPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PeriodMinutes)));
            }
        }
    }




    public async void SyncNow()
    {
        CurrentLog.Clear();
        CurrentLog.Add("Syncing started...");
        SyncStatus = SyncStatus.Syncing;
        await Task.Delay(800);
        CurrentLog.Add("Syncing complete. Synced 3 items.");
        CurrentLog.Add("Syncing complete. Synced 3 items.");
        CurrentLog.Add("Syncing complete. Synced 3 items.");
        CurrentLog.Add("Syncing complete. Synced 3 items.");
        CurrentLog.Add("Syncing complete. Synced 3 items.");
        CurrentLog.Add("Syncing complete. Synced 3 items.");
        CurrentLog.Add("Syncing complete. Synced 3 items.");
        CurrentLog.Add("Syncing complete. Synced 3 items.");
        CurrentLog.Add("Syncing complete. Synced 3 items.");
        CurrentLog.Add("Syncing complete. Synced 3 items.");
        SyncStatus = SyncStatus.Synced;
        LastChecked = DateTime.Now;
    }
    
    public event EventHandler<SyncChangedEventArgs>? OnSyncStatusChanged;
    
    [JsonIgnore]
    public SyncStatus SyncStatus
    {
        get
        {
            return _syncStatus;
        }
        set
        {
            if (_syncStatus != value)
            {
                var message = new SyncChangedEventArgs(_syncStatus, value);
                _syncStatus = value;
                OnSyncStatusChanged?.Invoke(this, message);
            }
        }
    }

    [JsonIgnore]
    private SyncStatus _syncStatus = SyncStatus.Idle;

    [JsonIgnore]
    public ObservableCollection<string> CurrentLog { get; set; } = new();
}