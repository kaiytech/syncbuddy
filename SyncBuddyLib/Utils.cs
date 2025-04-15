namespace SyncBuddyLib;

public static class Utils
{
    public static string GetReadableFileSize(long length)
    {
        string[] sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        var order = 0;
        double len = length;

        while (len >= 1024 && order < sizeSuffixes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizeSuffixes[order]}";
    }
}