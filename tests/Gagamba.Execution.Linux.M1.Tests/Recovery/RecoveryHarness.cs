// WP03 native checks for cgroup identity, subreaper setup, and journal persistence. Owner preparation/recovery are covered through the R6D.2 process path.
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Xunit;
using PreparationInterop = Gagamba.Execution.Linux.M1.Preparation.Native.PreparationInterop;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class RecoveryHarness
{
    private const string CgroupRoot = "/sys/fs/cgroup";

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gagamba-wp03-fs-" + Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
            GC.SuppressFinalize(this);
        }
    }

    private static bool Usable()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        try
        {
            var probe = System.IO.Path.Combine(CgroupRoot, "gagamba-wp03-probe-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(probe);
            var ok = File.Exists(System.IO.Path.Combine(probe, "cgroup.kill"))
                && File.Exists(System.IO.Path.Combine(probe, "cgroup.events"));
            Directory.Delete(probe);
            return ok;
        }
        catch
        {
            return false;
        }
    }

    private static void SkipUnlessUsable()
    {
        if (!Usable())
        {
            Assert.Skip("requires delegated Linux cgroup v2");
        }
    }

    private static string Boot() =>
        CgroupInterop.TryReadBootId(out var id, out _) ? id : "unknown";

    internal static string? LocateOwnerDll()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(System.IO.Path.Combine(dir.FullName, "Gagamba.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            return null;
        }

        var candidate = System.IO.Path.Combine(
            dir.FullName, "src", "Gagamba.Execution.Linux.M1.Owner", "bin", "Debug", "net10.0",
            "Gagamba.Execution.Linux.M1.Owner.dll");
        return File.Exists(candidate) ? candidate : null;
    }

    [Fact]
    public void CgroupMountAndBootIdentityAreReadable()
    {
        SkipUnlessUsable();
        Assert.True(CgroupInterop.TryReadCgroupMount(out var mount, out var error), error);
        Assert.Equal("cgroup2", mount!.FileSystemType);
        Assert.True(mount.MountId > 0);
        Assert.True(CgroupInterop.TryReadBootId(out var boot, out var bootError), bootError);
        Assert.NotEqual(string.Empty, boot);
    }

    [Fact]
    public void MissingDelegationRefused()
    {
        SkipUnlessUsable();
        var errno = PreparationInterop.OpenDirectory("/nonexistent-gagamba-parent", out _);
        Assert.NotEqual(0, errno);
    }

    [Fact]
    public void SubreaperEnabledAndVerified()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("linux only");
        }

        Assert.True(RecoverySubreaper.TryEnableAndVerify(out var error), error);
    }

    [Fact]
    public void NativeJournalDurableThenTornDetected()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("linux only");
        }

        using var dir = new TempDir();
        using var storage = new JournalFileStorage();
        var journal = new OwnershipJournal(storage, dir.Path);
        Assert.True(journal.TryAcquireWriter(out var writer, out var acquireError), acquireError);
        using (writer!)
        {
            Assert.True(journal.WriteNext(writer, RecoveryTestSupport.Snapshot()).Ok);
        }

        Assert.True(journal.ReadLatest().Ok);
        File.WriteAllBytes(System.IO.Path.Combine(dir.Path, OwnershipJournal.SlotA), new byte[] { 1, 2, 3, 4, 5 });
        Assert.False(journal.ReadLatest().Ok);
    }

    [Fact]
    public void NativeWriterLockIsExclusiveAcrossProcesses()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("linux only");
        }

        using var dir = new TempDir();
        using var storage = new JournalFileStorage();
        Assert.True(storage.TryAcquireExclusiveLock(dir.Path, OwnershipJournal.DefaultLockName).Ok);
        using var other = new JournalFileStorage();
        Assert.False(other.TryAcquireExclusiveLock(dir.Path, OwnershipJournal.DefaultLockName).Ok);
    }

}
