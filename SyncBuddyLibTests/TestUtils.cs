using System.Security.Cryptography;

namespace SyncBuddyLibTests;

public static class TestUtils
{
    public static bool AreFilesEqual(string path1, string path2)
    {
        if (!File.Exists(path1) || !File.Exists(path2))
            return false;
        
        using var hash = SHA256.Create();
        using var fs1 = File.OpenRead(path1);
        using var fs2 = File.OpenRead(path2);
        var hash1 = hash.ComputeHash(fs1);
        var hash2 = hash.ComputeHash(fs2);
        return hash1.SequenceEqual(hash2);
    }
}