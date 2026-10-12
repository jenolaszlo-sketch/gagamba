// WP02 Linux-native identity-checked cleanup tests (G2-07): real files,
// real descriptors, replacement detection, and truthful failure reporting.
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Preparation;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class NativeStagingCleanupTests
{
    /// <summary>Wraps the production host to inject inspection/unlink
    /// failures deterministically.</summary>
    private sealed class FaultingCopyHost : ICopyHost
    {
        private readonly ICopyHost _inner = LibcCopyHost.Instance;

        public Func<string, int>? OpenSourceErrno { get; set; }

        public Func<string, int>? UnlinkErrno { get; set; }

        public int OpenDirectory(string path, out SafeHandle? handle) => _inner.OpenDirectory(path, out handle);

        public int OpenSubdirectory(SafeHandle parent, string name, out SafeHandle? handle) => _inner.OpenSubdirectory(parent, name, out handle);

        public int StatHandle(SafeHandle handle, out FileIdentity identity) => _inner.StatHandle(handle, out identity);

        public int OpenSource(SafeHandle root, string name, out SafeHandle? handle)
        {
            if (OpenSourceErrno is not null && OpenSourceErrno(name) is var e && e != 0)
            {
                handle = null;
                return e;
            }

            return _inner.OpenSource(root, name, out handle);
        }

        public int CreateStaging(SafeHandle parent, string name, out SafeHandle? handle) => _inner.CreateStaging(parent, name, out handle);

        public int MakeDirectory(SafeHandle parent, string name, uint mode) => _inner.MakeDirectory(parent, name, mode);

        public Stream OpenRead(SafeHandle handle) => _inner.OpenRead(handle);

        public Stream OpenWrite(SafeHandle handle) => _inner.OpenWrite(handle);

        public int SetMode(SafeHandle handle, uint mode) => _inner.SetMode(handle, mode);

        public int SyncFile(SafeHandle handle) => _inner.SyncFile(handle);

        public int SyncDirectory(SafeHandle handle) => _inner.SyncDirectory(handle);

        public int Unlink(SafeHandle parent, string name)
        {
            if (UnlinkErrno is not null && UnlinkErrno(name) is var e && e != 0)
            {
                return e;
            }

            return _inner.Unlink(parent, name);
        }
    }

    private sealed class Dirs : IDisposable
    {
        public string Source { get; } = Path.Combine(Path.GetTempPath(), "wp02-clean-src-" + Guid.NewGuid().ToString("N"));

        public string Staging { get; } = Path.Combine(Path.GetTempPath(), "wp02-clean-stg-" + Guid.NewGuid().ToString("N"));

        public Dirs()
        {
            Directory.CreateDirectory(Source);
            Directory.CreateDirectory(Staging);
        }

        public void Dispose()
        {
            try { Directory.Delete(Source, true); } catch { }
            try { Directory.Delete(Staging, true); } catch { }
            GC.SuppressFinalize(this);
        }
    }

    private static bool Usable()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        return PreparationInterop.ProbeOpenat2Support() != Openat2SupportKind.Unsupported;
    }

    private static (Dirs Dirs, PreparedClosure Closure, StagedObject Staged, string StagedPath) Stage()
    {
        var dirs = new Dirs();
        var content = new byte[2048];
        new Random(5).NextBytes(content);
        File.WriteAllBytes(Path.Combine(dirs.Source, "tool"), content);
        var plan = new CopyPlan(
            dirs.Source, dirs.Staging, "prep-001",
            new[] { new CopyObjectPlan("tool", "tool", 4UL * 1024 * 1024, null) },
            16UL * 1024 * 1024, 128,
            Array.Empty<KeyValuePair<string, string>>(), _ => true, null);
        var result = SafeCopyIn.Prepare(plan, LibcCopyHost.Instance, CancellationToken.None);
        Assert.True(result.Succeeded, result.Detail);
        var closure = result.Closure!;
        var staged = closure.Objects[0];
        var path = Path.Combine(dirs.Staging, "prep-001", staged.StagingName);
        return (dirs, closure, staged, path);
    }

    [Fact]
    public void OriginalEntryRemoved()
    {
        if (!Usable()) { Assert.Skip("requires Linux openat2"); return; }
        var (dirs, closure, staged, path) = Stage();
        using (dirs)
        {
            var result = StagingCleanup.CleanOne(LibcCopyHost.Instance, closure.StagingDirectory!, staged.StagingName, staged.Device, staged.Inode);
            Assert.Equal(CleanupItemOutcome.Removed, result.Outcome);
            Assert.False(File.Exists(path));
            closure.Dispose();
        }
    }

    [Fact]
    public void AlreadyAbsentIsIdempotent()
    {
        if (!Usable()) { Assert.Skip("requires Linux openat2"); return; }
        var (dirs, closure, staged, path) = Stage();
        using (dirs)
        {
            File.Delete(path);
            var result = StagingCleanup.CleanOne(LibcCopyHost.Instance, closure.StagingDirectory!, staged.StagingName, staged.Device, staged.Inode);
            Assert.Equal(CleanupItemOutcome.AlreadyAbsent, result.Outcome);
            closure.Dispose();
        }
    }

    [Fact]
    public void ReplacementRegularFileSurvives()
    {
        if (!Usable()) { Assert.Skip("requires Linux openat2"); return; }
        var (dirs, closure, staged, path) = Stage();
        using (dirs)
        {
            File.Delete(path);
            File.WriteAllBytes(path, new byte[128]);
            var result = StagingCleanup.CleanOne(LibcCopyHost.Instance, closure.StagingDirectory!, staged.StagingName, staged.Device, staged.Inode);
            Assert.Equal(CleanupItemOutcome.IdentityMismatch, result.Outcome);
            Assert.True(File.Exists(path));
            closure.Dispose();
        }
    }

    [Fact]
    public void ReplacementSymlinkSurvives()
    {
        if (!Usable()) { Assert.Skip("requires Linux openat2"); return; }
        var (dirs, closure, staged, path) = Stage();
        using (dirs)
        {
            File.Delete(path);
            File.CreateSymbolicLink(path, Path.Combine(dirs.Source, "tool"));
            var result = StagingCleanup.CleanOne(LibcCopyHost.Instance, closure.StagingDirectory!, staged.StagingName, staged.Device, staged.Inode);
            Assert.Equal(CleanupItemOutcome.IdentityMismatch, result.Outcome);
            Assert.True(File.Exists(path));
            closure.Dispose();
        }
    }

    [Fact]
    public void SameContentDifferentInodeSurvives()
    {
        if (!Usable()) { Assert.Skip("requires Linux openat2"); return; }
        var (dirs, closure, staged, path) = Stage();
        using (dirs)
        {
            var bytes = File.ReadAllBytes(path);
            File.Delete(path);
            File.WriteAllBytes(path, bytes);
            var result = StagingCleanup.CleanOne(LibcCopyHost.Instance, closure.StagingDirectory!, staged.StagingName, staged.Device, staged.Inode);
            Assert.Equal(CleanupItemOutcome.IdentityMismatch, result.Outcome);
            Assert.True(File.Exists(path));
            closure.Dispose();
        }
    }

    [Fact]
    public void InspectionFailureIsNotConfirmed()
    {
        if (!Usable()) { Assert.Skip("requires Linux openat2"); return; }
        var (dirs, closure, staged, path) = Stage();
        using (dirs)
        {
            var host = new FaultingCopyHost { OpenSourceErrno = _ => PreparationInterop.EACCES };
            var result = StagingCleanup.CleanOne(host, closure.StagingDirectory!, staged.StagingName, staged.Device, staged.Inode);
            Assert.Equal(CleanupItemOutcome.InspectionFailed, result.Outcome);
            Assert.True(File.Exists(path));
            closure.Dispose();
        }
    }

    [Fact]
    public void UnlinkFailureIsNotConfirmedAndRetainsDebt()
    {
        if (!Usable()) { Assert.Skip("requires Linux openat2"); return; }
        var (dirs, closure, staged, path) = Stage();
        using (dirs)
        {
            var host = new FaultingCopyHost { UnlinkErrno = _ => PreparationInterop.EACCES };
            var result = StagingCleanup.CleanOne(host, closure.StagingDirectory!, staged.StagingName, staged.Device, staged.Inode);
            Assert.Equal(CleanupItemOutcome.RemovalFailed, result.Outcome);
            Assert.True(File.Exists(path));
            closure.Dispose();
        }
    }

    [Fact]
    public void RepeatedCleanupIdempotentAndNeverRemovesMismatch()
    {
        if (!Usable()) { Assert.Skip("requires Linux openat2"); return; }
        var (dirs, closure, staged, path) = Stage();
        using (dirs)
        {
            File.Delete(path);
            File.WriteAllBytes(path, new byte[64]);
            for (var i = 0; i < 3; i++)
            {
                var result = StagingCleanup.CleanOne(LibcCopyHost.Instance, closure.StagingDirectory!, staged.StagingName, staged.Device, staged.Inode);
                Assert.Equal(CleanupItemOutcome.IdentityMismatch, result.Outcome);
                Assert.True(File.Exists(path));
            }

            closure.Dispose();
        }
    }
}
