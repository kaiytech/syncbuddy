using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
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
        var stopwatch = Stopwatch.StartNew();
        CurrentLog.Clear();
        CurrentLog.Add("Syncing started...");
        SyncStatus = SyncStatus.Syncing;

        var removedCounter_files = 0;
        var addedCounter_files = 0;
        var updatedCounter_files = 0;
        
        var removedCounter_dirs = 0;
        var addedCounter_dirs = 0;
        
        var sourceEntries = Directory.EnumerateFileSystemEntries(SourceDir, "*", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(SourceDir, p))
            .ToList();
        
        var targetEntries = Directory.EnumerateFileSystemEntries(TargetDir, "*", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(TargetDir, p))
            .ToList();
        
        // remove entries that do not exist in SOURCE but exist in TARGET:
        var toRemove = targetEntries.Except(sourceEntries).ToList();
        //   first all files, then directories:
        toRemove = toRemove.OrderBy(_ => Directory.Exists(Path.Join(TargetDir, _))).ThenBy(_ => _).ToList();
        foreach (var item in toRemove)
        {
            var path = Path.Join(TargetDir, item);
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path);
                    CurrentLog.Add($"- Removed directory '{item}' from the Target directory");
                    removedCounter_dirs++;
                } 
                catch (Exception e)
                {
                    CurrentLog.Add($"! Failed to remove directory '{item}' to the Target directory => {e.Message}");
                }
            } 
            else
            {
                try
                {
                    var fileSize = Utils.GetReadableFileSize(new FileInfo(path).Length);
                    File.Delete(path);
                    CurrentLog.Add($"- Removed file '{item}' from the Target directory ({fileSize})");
                    removedCounter_files++;
                }
                catch (Exception e)
                {
                    CurrentLog.Add($"! Failed to remove file '{item}' from the Target directory => {e.Message}");
                }
            }
        }

        // add entries that do not exist in TARGET but exist in SOURCE
        var toAdd = sourceEntries.Except(targetEntries).ToList();
        //   first all directories, then files:
        toAdd = toAdd.OrderBy(_ => File.Exists(Path.Join(SourceDir, _))).ThenBy(_ => _).ToList();
        foreach (var item in toAdd)
        {
            var sourcePath = Path.Join(SourceDir, item);
            var targetPath = Path.Join(TargetDir, item);
            if (Directory.Exists(sourcePath))
            {
                try
                {
                    Directory.CreateDirectory(targetPath);
                    CurrentLog.Add($"+ Added directory '{item}' to the Target directory");
                    addedCounter_dirs++;
                }
                catch (Exception e)
                {
                    CurrentLog.Add($"! Failed to add directory '{item}' to the Target directory => {e.Message}");
                }
            }
            else
            {
                try
                {
                    var fileSize = Utils.GetReadableFileSize(new FileInfo(sourcePath).Length);
                    await using var source = File.OpenRead(sourcePath);
                    await using var destination = File.Create(targetPath);
                    await source.CopyToAsync(destination);
                    CurrentLog.Add($"+ Added file '{item}' to the Target directory ({fileSize})");
                    addedCounter_files++;
                }                 
                catch (Exception e)
                {
                    CurrentLog.Add($"! Failed to add '{item}' to the Target directory => {e.Message}");
                }
            }
        }

        // verify files with the same name and directory
        var toVerify = sourceEntries.Intersect(targetEntries)
            .Where(_ => File.Exists(Path.Join(SourceDir, _)) && File.Exists(Path.Join(TargetDir, _))).ToList();
        foreach (var item in toVerify)
        {
            var sourcePath = Path.Join(SourceDir, item);
            var targetPath = Path.Join(TargetDir, item);
            var sourceInfo = new FileInfo(sourcePath);
            var targetInfo = new FileInfo(targetPath);

            if (sourceInfo.Length != targetInfo.Length)
            {
                try
                {
                    await using var source = File.OpenRead(sourcePath);
                    await using var destination = File.Create(targetPath);
                    await source.CopyToAsync(destination);
                }
                catch (Exception e)
                {
                    CurrentLog.Add($"! Failed to replace '{item}' at the Target directory => {e.Message}");
                }

                CurrentLog.Add($"* Replaced '{item}' at Target directory (file size mismatch)");
                updatedCounter_files++;
                continue;
            }

            using var sha256 = SHA256.Create();
            var sourceStream = File.OpenRead(sourcePath);
            var targetStream = File.OpenRead(targetPath);
            
            var sourceHash = await sha256.ComputeHashAsync(sourceStream);
            var targetHash = await sha256.ComputeHashAsync(targetStream);
            
            sourceStream.Close();
            targetStream.Close();

            if (!sourceHash.SequenceEqual(targetHash))
            {
                try
                {
                    await using var source = File.OpenRead(sourcePath);
                    await using var destination = File.Create(targetPath);
                    await source.CopyToAsync(destination);
                    CurrentLog.Add($"* Replaced '{item}' at Target directory (SHA256 mismatch)");
                    updatedCounter_files++;
                }
                catch (Exception e)
                {
                    CurrentLog.Add($"! Failed to replace '{item}' at the Target directory => {e.Message}");
                }
            }
        }

        if ((removedCounter_files + removedCounter_dirs + addedCounter_files + updatedCounter_files) > 0)
        {
            CurrentLog.Add($"Sync completed! Summary:");
            if (removedCounter_files > 0)
                CurrentLog.Add($"  Removed {removedCounter_files} file{(removedCounter_files > 1 ? "s" : "")}");
            if (removedCounter_dirs > 0)
                CurrentLog.Add($"  Removed {removedCounter_dirs} director{(removedCounter_dirs > 1 ? "ies" : "y")}");
            if (addedCounter_files > 0)
                CurrentLog.Add($"  Added {addedCounter_files} file{(addedCounter_files > 1 ? "s" : "")}");
            if (addedCounter_dirs > 0)
                CurrentLog.Add($"  Added {addedCounter_dirs} director{(addedCounter_dirs > 1 ? "ies" : "y")}");
            if (updatedCounter_files > 0)
                CurrentLog.Add($"  Updated {updatedCounter_files} file{(updatedCounter_files > 1 ? "s" : "")}");
        }
        else
        {
            CurrentLog.Add($"Sync completed! Nothing to update.");
        }
        
        var elapsed = stopwatch.Elapsed;
        CurrentLog.Add(
            $"Took: {(elapsed.TotalMilliseconds > 1000 ? $"{elapsed.TotalSeconds:0.##} seconds" : $"{elapsed.TotalMilliseconds:0} ms")}");

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