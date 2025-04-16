using Newtonsoft.Json;

namespace SyncBuddy;

[JsonObject]
public class AppSettings
{
    [JsonProperty("app_active")] public bool AppActive { get; set; } = false;
}