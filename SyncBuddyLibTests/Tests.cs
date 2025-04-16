using System.Diagnostics;
using System.Reflection.Metadata;
using FluentAssertions;
using SyncBuddyLib;

namespace SyncBuddyLibTests;

public class Tests
{
    private string TestFolder => Path.Combine(Path.GetTempPath(), "SyncBuddyLibTests");
    private string TestFolderA => Path.Combine(TestFolder, "A");
    private string TestFolderB => Path.Combine(TestFolder, "B");
    
    [SetUp]
    public void Setup()
    {
        Directory.Delete(TestFolder, true);
        
        Directory.CreateDirectory(TestFolder);
        Directory.CreateDirectory(TestFolderA);
        Directory.CreateDirectory(TestFolderB);
    }

    [Test]
    public void PathCheck_Independent_Case1()
    {
        SyncItem.AreDirectoriesIndependent(Path.Combine(TestFolder, "folder1"), Path.Combine(TestFolder, "folder1"))
            .Should().BeFalse();
    }
    
    [Test]
    public void PathCheck_Independent_Case2()
    {
        SyncItem.AreDirectoriesIndependent(Path.Combine(TestFolder, "folder2"), Path.Combine(TestFolder, "folder2", "folder3"))
            .Should().BeFalse();
    }
    
    [Test]
    public void PathCheck_Independent_Case3()
    {
        SyncItem.AreDirectoriesIndependent(Path.Combine(TestFolder, "folder1", "folder2"), Path.Combine(TestFolder, "folder1", "folder3"))
            .Should().BeTrue();
    }
    
    [Test]
    public void PathCheck_Independent_Case4()
    {
        SyncItem.AreDirectoriesIndependent(Path.Combine(TestFolder, "folder3", "folder2"), Path.Combine(TestFolder, "folder3"))
            .Should().BeFalse();
    }

    [Test]
    public void PathCheck_Independent_Case5()
    {
        var syncItem = new SyncItem(0, Path.Combine(TestFolder, "folder1"), Path.Combine(TestFolder, "folder1"),
            DateTime.Now, true, 1);
        try
        {
            syncItem.Sync().Wait();
        }
        catch (AggregateException e)
        {
            (e.InnerException is ArgumentException).Should().BeTrue();
            return;
        }
        
        Assert.Fail();
    }

    [Test]
    public void Sync_AddMissingFile()
    {
        var fileA = Path.Combine(TestFolderA, "test.txt");
        var fileB = Path.Combine(TestFolderB, "test.txt");
        File.WriteAllText(fileA, "test");
        
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now, true, 2);
        syncItem.Sync().Wait();

        TestUtils.AreFilesEqual(fileA, fileB).Should().BeTrue();
    }
    
    [Test]
    public void Sync_AddMissingFileInsideDirectory()
    {
        var fileA = Path.Combine(TestFolderA, "directory", "test.txt");
        var fileB = Path.Combine(TestFolderB, "directory", "test.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(fileA));
        File.WriteAllText(fileA, "test");
        
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now, true, 2);
        syncItem.Sync().Wait();

        TestUtils.AreFilesEqual(fileA, fileB).Should().BeTrue();
    }
    
    [Test]
    public void Sync_AddMissingEmptyDirectory()
    {
        var dirA = Path.Combine(TestFolderA, "directory");
        var dirB = Path.Combine(TestFolderB, "directory");
        Directory.CreateDirectory(dirA);
        
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now, true, 2);
        syncItem.Sync().Wait();
        
        Directory.Exists(dirB).Should().BeTrue();
        Directory.EnumerateFiles(dirB).Count().Should().Be(0);
    }
    
    [Test]
    public void Sync_RemoveFile()
    {
        var fileB = Path.Combine(TestFolderB, "test.txt");
        File.WriteAllText(fileB, "test");

        File.Exists(fileB).Should().BeTrue();
        
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now, true, 2);
        syncItem.Sync().Wait();
        
        File.Exists(fileB).Should().BeFalse();
    }

    [Test]
    public void Sync_RemoveFileInsideDirectory()
    {
        var dirA = Path.Combine(TestFolderA, "directory");
        var fileB = Path.Combine(TestFolderB, "directory", "test.txt");
        Directory.CreateDirectory(dirA);
        Directory.CreateDirectory(Path.GetDirectoryName(fileB));
        File.WriteAllText(fileB, "test");

        File.Exists(fileB).Should().BeTrue();
        
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now, true, 2);
        syncItem.Sync().Wait();
        
        File.Exists(fileB).Should().BeFalse();

        Directory.Exists(dirA).Should().BeTrue();
        Directory.Exists(dirA).Should().BeTrue();
    }
    
    [Test]
    public void Sync_RemoveEmptyDirectory()
    {
        var dirB = Path.Combine(TestFolderB, "directory");
        Directory.CreateDirectory(dirB);
        Directory.Exists(dirB).Should().BeTrue();
        
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now, true, 2);
        syncItem.Sync().Wait();
        
        Directory.Exists(dirB).Should().BeFalse();
    }

    [Test]
    public void Sync_CompareWithDifferentSize()
    {
        var fileA = Path.Combine(TestFolderA, "test.txt");
        var fileB = Path.Combine(TestFolderB, "test.txt");
        File.WriteAllText(fileA, "test");
        File.WriteAllText(fileB, "testtest");
        
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now, true, 2);
        syncItem.Sync().Wait();

        TestUtils.AreFilesEqual(fileA, fileB).Should().BeTrue();
    }
    
    [Test]
    public void Sync_CompareWithSameSize()
    {
        var fileA = Path.Combine(TestFolderA, "test.txt");
        var fileB = Path.Combine(TestFolderB, "test.txt");
        File.WriteAllText(fileA, "test");
        File.WriteAllText(fileB, "t3st");
        
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now, true, 2);
        syncItem.Sync().Wait();

        TestUtils.AreFilesEqual(fileA, fileB).Should().BeTrue();
    }
    
    [Test]
    public void Sync_HasLog()
    {
        var fileA = Path.Combine(TestFolderA, "test.txt");
        var fileB = Path.Combine(TestFolderB, "test.txt");
        File.WriteAllText(fileA, "test");
        
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now, true, 2);
        syncItem.Sync().Wait();

        TestUtils.AreFilesEqual(fileA, fileB).Should().BeTrue();
        
        syncItem.CurrentLog.Should().NotBeEmpty();
    }
    
    [Test]
    public void Sync_Periodic()
    {
        var fileA = Path.Combine(TestFolderA, "test.txt");
        var fileB = Path.Combine(TestFolderB, "test.txt");
        File.WriteAllText(fileA, "test");
        
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now - TimeSpan.FromSeconds(120), true, 1);
        syncItem.PeriodicCheck(new CancellationTokenSource());
        var sw = Stopwatch.StartNew();
        while (true)
            if (sw.Elapsed.TotalSeconds > 3 || syncItem.SyncStatus == SyncStatus.Synced)
                break;

        syncItem.SyncStatus.Should().Be(SyncStatus.Synced);
        TestUtils.AreFilesEqual(fileA, fileB).Should().BeTrue();
    }
    
    [Test]
    public void Sync_Periodic_Disabled()
    {
        var fileA = Path.Combine(TestFolderA, "test.txt");
        var fileB = Path.Combine(TestFolderB, "test.txt");
        File.WriteAllText(fileA, "test");
        
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now - TimeSpan.FromSeconds(120), false, 1);
        syncItem.PeriodicCheck(new CancellationTokenSource());
        var sw = Stopwatch.StartNew();
        while (true)
            if (sw.Elapsed.TotalSeconds > 3)
                break;

        syncItem.SyncStatus.Should().Be(SyncStatus.Idle);
        TestUtils.AreFilesEqual(fileA, fileB).Should().BeFalse();
    }
    
    [Test]
    public void Sync_Periodic_LastCheckedCorrect()
    {
        var fileA = Path.Combine(TestFolderA, "test.txt");
        var fileB = Path.Combine(TestFolderB, "test.txt");
        File.WriteAllText(fileA, "test");
        
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now - TimeSpan.FromSeconds(120), true, 1);
        syncItem.PeriodicCheck(new CancellationTokenSource());
        var sw = Stopwatch.StartNew();
        while (true)
            if (sw.Elapsed.TotalSeconds > 3 || syncItem.SyncStatus == SyncStatus.Synced)
                break;

        syncItem.SyncStatus.Should().Be(SyncStatus.Synced);
        TestUtils.AreFilesEqual(fileA, fileB).Should().BeTrue();
        (DateTime.Now - syncItem.LastChecked).Minutes.Should().BeLessThan(5).And.BeGreaterThanOrEqualTo(0);
    }
    
    [Test]
    public void Sync_Periodic_CanCancel()
    {
        var fileA = Path.Combine(TestFolderA, "test.txt");
        var fileB = Path.Combine(TestFolderB, "test.txt");
        File.WriteAllText(fileA, "test");
        
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now - TimeSpan.FromSeconds(59), true, 1);
        var cts = new CancellationTokenSource();
        syncItem.PeriodicCheck(cts);
        var sw = Stopwatch.StartNew();
        while (true)
        {
            cts.Cancel();
            if (sw.Elapsed.TotalSeconds > 3 || syncItem.SyncStatus == SyncStatus.Idle)
                break;
        }

        syncItem.SyncStatus.Should().Be(SyncStatus.Idle);
        TestUtils.AreFilesEqual(fileA, fileB).Should().BeFalse();
    }
    
    [Test]
    public void Sync_TryError()
    {
        var fileA = Path.Combine(TestFolderA, "test.txt");
        var fileB = Path.Combine(TestFolderB, "test.txt");
        File.WriteAllText(fileA, "test");
        File.WriteAllText(fileB, "test2");
        TestUtils.AreFilesEqual(fileA, fileB).Should().BeFalse();

        var handle = new FileStream(fileB, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        try
        {
            var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now - TimeSpan.FromSeconds(120), true,
                1);
            syncItem.PeriodicCheck(new CancellationTokenSource());
            var sw = Stopwatch.StartNew();
            while (true)
                if (sw.Elapsed.TotalSeconds > 3 || syncItem.SyncStatus == SyncStatus.Error)
                    break;

            syncItem.SyncStatus.Should().Be(SyncStatus.Error);
            handle.Close(); //important
            TestUtils.AreFilesEqual(fileA, fileB).Should().BeFalse();
        }
        finally
        {
            handle.Close();
            handle.Dispose();
        }
    }

    [Test]
    public void PropertyChange_Happened()
    {
        var changed = false;
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now, false, 1);
        syncItem.OnAnyPropertyChanged += (sender, args) => changed = true;
        syncItem.SourceDir = TestFolderA + "_Test";
        changed.Should().BeTrue();
    }
    
    [Test]
    public void PropertyChange_DidntHappen()
    {
        var changed = false;
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now, false, 1);
        syncItem.OnAnyPropertyChanged += (sender, args) => changed = true;
        syncItem.SourceDir = TestFolderA;
        changed.Should().BeFalse();
    }
    
    
    [Test]
    public void SyncItem_StatusCorrectWhenNothingHappened()
    {
        var syncItem = new SyncItem(0, TestFolderA, TestFolderB, DateTime.Now - TimeSpan.FromSeconds(120), true, 1);
        syncItem.SyncStatus.Should().Be(SyncStatus.Idle);
    }

    [Test]
    public void SyncItem_PropertiesAreCorrect()
    {
        var id = 0;
        var source = TestFolderA;
        var target = TestFolderB;
        var lastChecked = DateTime.Now;
        var enabled = true;
        var period = 1;
        
        var syncItem = new SyncItem(id, source, target, lastChecked, enabled, period);

        syncItem.Id.Should().Be(id);
        syncItem.SourceDir.Should().Be(source);
        syncItem.TargetDir.Should().Be(target);
        syncItem.LastChecked.Should().Be(lastChecked);
        syncItem.LastChecked.Should().NotBe(DateTime.Now);
        syncItem.Enabled.Should().Be(enabled);
        syncItem.PeriodMinutes.Should().Be(period);
    }

}