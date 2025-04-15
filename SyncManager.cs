using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SyncBuddyLib;

namespace SyncBuddy;

public static class SyncManager
{
    public static string AppSettingsDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SyncBuddy");

    public static string AppSettingsFile => Path.Combine(AppSettingsDir, "syncs.json");

    private static JsonSerializerSettings SerializerSettings
    {
        get
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    DefaultMembersSearchFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    IgnoreSerializableAttribute = true
                },
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };
            settings.ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
                IgnoreShouldSerializeMembers = true
            };
            settings.DefaultValueHandling = DefaultValueHandling.Ignore;
            settings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;

            settings.ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
                IgnoreSerializableInterface = true,
                IgnoreIsSpecifiedMembers = true,
                IgnoreShouldSerializeMembers = true
            };
            return settings;
        }
    }

    public static ObservableCollection<SyncItemExtended> Items { get; } = new()
    {
    };

    private static SemaphoreSlim _lock = new(1, 1);

    public static void InitConfig()
    {
        if (!Directory.Exists(AppSettingsDir))
            Directory.CreateDirectory(AppSettingsDir);

        if (!File.Exists(AppSettingsFile))
            File.WriteAllText(AppSettingsFile, "[]");
    }

    public static async void Save()
    {
        await _lock.WaitAsync();
        
        InitConfig();

        try
        {
            var items = Items.ToList().Select(SyncItemExtended.CastToBase);
            var json = JsonConvert.SerializeObject(items, SerializerSettings);
            await File.WriteAllTextAsync(AppSettingsFile, json);
        }
        finally
        {
            _lock.Release();
        }
    }

    public static async void Load()
    {
        await _lock.WaitAsync();
        
        InitConfig();

        try
        {
            var json = await File.ReadAllTextAsync(AppSettingsFile);
            var items = JsonConvert.DeserializeObject<List<SyncItem>>(json);
            Items.Clear();
            foreach (var item in items)
                Items.Add(new SyncItemExtended(item));
            // do not assign new list to the observable list.
            // you WILL LOSE event bindings if you do so!!!
        }
        finally
        {
            _lock.Release();
        }
    }
}