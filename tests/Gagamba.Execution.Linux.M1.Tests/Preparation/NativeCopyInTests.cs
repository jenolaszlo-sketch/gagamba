// WP02 Linux-native tests: qualify the actual copy mechanism on Linux.
// Every test skips honestly when not on Linux or when openat2 is
// unavailable. The native target fixture is built, copied, hashed and
// bound here, never executed: no Process use exists in WP02 tests.
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Preparation.CopyTestSupport;

namespace Gagamba.Execution.Linux.M1.Tests.Preparation;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class NativeCopyInTests
{
    private sealed class NativeDirs : IDisposable
    {
        public NativeDirs()
        {
            Root = Path.Combine(Path.GetTempPath(), "gagamba-wp02-src-" + Guid.NewGuid().ToString("N"));
            Parent = Path.Combine(Path.GetTempPath(), "gagamba-wp02-staging-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(Parent);
        }

        public string Root { get; }

        public string Parent { get; }

        public void Dispose()
        {
            try { Directory.Delete(Root, true); } catch { }
            try { Directory.Delete(Parent, true); } catch { }
            GC.SuppressFinalize(this);
        }
    }

    private static bool Usable(out Openat2SupportKind support)
    {
        support = Openat2SupportKind.Unsupported;
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        support = PreparationInterop.ProbeOpenat2Support();
        return support != Openat2SupportKind.Unsupported;
    }

    private static void SkipUnlessUsable(out Openat2SupportKind support)
    {
        if (!Usable(out support))
        {
            Assert.Skip("requires Linux with openat2 support");
        }
    }

    private static CopyPlan NativePlan(NativeDirs dirs, params CopyObjectPlan[] objects) =>
        new(
            dirs.Root,
            dirs.Parent,
            "prep-native",
            objects,
            TotalBudget,
            MaxObjects,
            Array.Empty<KeyValuePair<string, string>>(),
            _ => true,
            null);

    [DllImport("libc", SetLastError = true)]
    private static extern int symlink(string target, string linkpath);

    [DllImport("libc", SetLastError = true)]
    private static extern int link(string oldpath, string newpath);

    [Fact]
    public void Openat2ProbeReportsSupportMethod()
    {
        SkipUnlessUsable(out var support);
        Assert.True(
            support is Openat2SupportKind.LibcSymbol or Openat2SupportKind.RawSyscall,
            $"support={support}");
    }

    [Fact]
    public void RawSyscallPathOpensSourceDirectly()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        var path = Path.Combine(dirs.Root, "tool");
        File.WriteAllBytes(path, Bytes(0x51, 4096));
        Assert.Equal(0, PreparationInterop.OpenDirectory(dirs.Root, out var rootFd));
        try
        {
            var errno = PreparationInterop.OpenSourceRaw(
                rootFd, "tool", PreparationInterop.RequiredResolve, out var fd);
            Assert.Equal(0, errno);
            try
            {
                Assert.Equal(0, PreparationInterop.StatFd(fd, out var identity));
                Assert.True(PreparationInterop.IsRegularFile(identity.Mode));
                Assert.Equal(4096, identity.Size);
            }
            finally
            {
                LibcClose(fd);
            }
        }
        finally
        {
            LibcClose(rootFd);
        }
    }

    [DllImport("libc")]
    private static extern int close(int fd);

    private static void LibcClose(int fd) => close(fd);

    [Fact]
    public void InvalidResolveBitsAreRefused()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        var path = Path.Combine(dirs.Root, "tool");
        File.WriteAllBytes(path, Bytes(0x52, 64));
        Assert.Equal(0, PreparationInterop.OpenDirectory(dirs.Root, out var rootFd));
        try
        {
            var errno = PreparationInterop.OpenSourceWithResolve(
                rootFd, "tool", PreparationInterop.ProbeInvalidResolve, out var fd);
            Assert.NotEqual(0, errno);
            Assert.Equal(-1, fd);
            Assert.Equal(PreparationInterop.EINVAL, errno);
        }
        finally
        {
            LibcClose(rootFd);
        }
    }

    [Fact]
    public void RegularFileCopyEndToEnd()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        var toolBytes = Bytes(0x53, 100000);
        var inputBytes = Bytes(0x54, 5000);
        File.WriteAllBytes(Path.Combine(dirs.Root, "tool"), toolBytes);
        File.WriteAllBytes(Path.Combine(dirs.Root, "input"), inputBytes);
        var result = SafeCopyIn.Prepare(
            NativePlan(dirs, Tool("tool", ToolBudget, Digest(toolBytes)), Input("input", InputBudget, Digest(inputBytes))),
            LibcCopyHost.Instance,
            CancellationToken.None);
        Assert.True(result.Succeeded, result.Detail);
        Assert.NotNull(result.Closure);
        try
        {
            Assert.Equal(2, result.Closure!.Objects.Count);
            foreach (var staged in result.Closure.Objects)
            {
                var expected = staged.Role == "tool" ? toolBytes : inputBytes;
                Assert.Equal(Digest(expected), staged.Sha256);
                Assert.Equal((ulong)expected.Length, staged.Length);
                var stagedPath = Path.Combine(dirs.Parent, "prep-native", staged.StagingName);
                Assert.Equal(expected, File.ReadAllBytes(stagedPath));
                var mode = File.GetUnixFileMode(stagedPath);
                var expectedMode = staged.Role == "tool"
                    ? UnixFileMode.UserRead | UnixFileMode.UserExecute
                    : UnixFileMode.UserRead;
                Assert.Equal(expectedMode, mode & (UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute));
            }

            Assert.Equal(
                UnixFileMode.UserRead | UnixFileMode.UserExecute,
                File.GetUnixFileMode(Path.Combine(dirs.Parent, "prep-native", "tool-000.staged"))
                    & (UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute));
        }
        finally
        {
            result.Closure!.Dispose();
        }
    }

    [Fact]
    public void SourceSymlinkRejected()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        File.WriteAllBytes(Path.Combine(dirs.Root, "real"), Bytes(0x55, 64));
        Assert.Equal(0, symlink("real", Path.Combine(dirs.Root, "tool")));
        var result = SafeCopyIn.Prepare(
            NativePlan(dirs, Tool()), LibcCopyHost.Instance, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.SymlinkRefused, result.Failure);
        Assert.Null(result.Closure);
    }

    [Fact]
    public void IntermediateSymlinkRejected()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        var sub = Directory.CreateDirectory(Path.Combine(dirs.Root, "sub"));
        File.WriteAllBytes(Path.Combine(sub.FullName, "tool"), Bytes(0x56, 64));
        Assert.Equal(0, symlink("sub", Path.Combine(dirs.Root, "linkdir")));
        var plan = NativePlan(dirs, new CopyObjectPlan("tool", "linkdir", ToolBudget, null));
        var result = SafeCopyIn.Prepare(plan, LibcCopyHost.Instance, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.SymlinkRefused, result.Failure);
    }

    [Fact]
    public void MagicLinkRejected()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        Assert.Equal(0, symlink("/proc/self/fd/0", Path.Combine(dirs.Root, "tool")));
        var result = SafeCopyIn.Prepare(
            NativePlan(dirs, Tool()), LibcCopyHost.Instance, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.SymlinkRefused, result.Failure);
    }

    [Fact]
    public void EscapeAboveRootRefused()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        var engine = SafeCopyIn.Prepare(
            NativePlan(dirs, new CopyObjectPlan("tool", "..", ToolBudget, null)),
            LibcCopyHost.Instance, CancellationToken.None);
        Assert.Equal(CopyFailureKind.InvalidSource, engine.Failure);

        Assert.Equal(0, PreparationInterop.OpenDirectory(dirs.Root, out var rootFd));
        try
        {
            var errno = PreparationInterop.OpenSourceWithResolve(
                rootFd, "..", PreparationInterop.RequiredResolve, out var fd);
            Assert.NotEqual(0, errno);
        }
        finally
        {
            LibcClose(rootFd);
        }
    }

    [Fact]
    public void DirectorySourceRefused()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        Directory.CreateDirectory(Path.Combine(dirs.Root, "tool"));
        var result = SafeCopyIn.Prepare(
            NativePlan(dirs, Tool()), LibcCopyHost.Instance, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.InvalidSource, result.Failure);
    }

    [Fact]
    public void HardlinkedSourceProducesIndependentCopy()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        var first = Path.Combine(dirs.Root, "tool");
        var second = Path.Combine(dirs.Root, "alias");
        var content = Bytes(0x57, 2048);
        File.WriteAllBytes(first, content);
        Assert.Equal(0, link(first, second));
        var result = SafeCopyIn.Prepare(
            NativePlan(dirs, Tool("tool", ToolBudget, Digest(content))),
            LibcCopyHost.Instance, CancellationToken.None);
        Assert.True(result.Succeeded, result.Detail);
        try
        {
            File.WriteAllBytes(first, Bytes(0x58, 2048));
            var stagedPath = Path.Combine(dirs.Parent, "prep-native", result.Closure!.Objects[0].StagingName);
            Assert.Equal(content, File.ReadAllBytes(stagedPath));
            Assert.Equal(Digest(content), result.Closure.Objects[0].Sha256);
        }
        finally
        {
            result.Closure!.Dispose();
        }
    }

    [Fact]
    public void ReplacementRaceDeterministicallyRefused()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        File.WriteAllBytes(Path.Combine(dirs.Root, "tool"), Bytes(0x59, 512));
        var plan = NativePlan(dirs, Tool()) with
        {
            Hooks = new CopyTestHooks(
                null,
                name => File.WriteAllBytes(
                    Path.Combine(dirs.Parent, "prep-native", name), Bytes(0x5A, 512))),
        };
        var result = SafeCopyIn.Prepare(plan, LibcCopyHost.Instance, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.ObjectReplaced, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(Directory.GetFiles(Path.Combine(dirs.Parent, "prep-native")));
    }

    [Fact]
    public void GrowthDuringCopyRefused()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        var path = Path.Combine(dirs.Root, "tool");
        File.WriteAllBytes(path, Bytes(0x5B, 100000));
        var grown = false;
        var plan = NativePlan(dirs, Tool()) with
        {
            Hooks = new CopyTestHooks(
                _ =>
                {
                    if (!grown)
                    {
                        grown = true;
                        using var appender = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        appender.Write(Bytes(0x5C, 1000), 0, 1000);
                    }
                },
                null),
        };
        var result = SafeCopyIn.Prepare(plan, LibcCopyHost.Instance, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.ObjectReplaced, result.Failure);
        Assert.Null(result.Closure);
    }

    [Fact]
    public void CancellationCleansPartialNatively()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        File.WriteAllBytes(Path.Combine(dirs.Root, "tool"), Bytes(0x5D, 300000));
        using var cts = new CancellationTokenSource();
        var plan = NativePlan(dirs, Tool()) with
        {
            Hooks = new CopyTestHooks(_ => cts.Cancel(), null),
        };
        var result = SafeCopyIn.Prepare(plan, LibcCopyHost.Instance, cts.Token);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.Cancelled, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(Directory.GetFiles(Path.Combine(dirs.Parent, "prep-native")));
    }

    [Fact]
    public void ExclusiveCreateEnforcedNatively()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        File.WriteAllBytes(Path.Combine(dirs.Root, "tool"), Bytes(0x5E, 64));
        var errno = PreparationInterop.CreateExclusive(
            PreparationInterop.AT_FDCWD, Path.Combine(dirs.Parent, "reserved"), out var fd);
        Assert.Equal(0, errno);
        LibcClose(fd);
        errno = PreparationInterop.CreateExclusive(
            PreparationInterop.AT_FDCWD, Path.Combine(dirs.Parent, "reserved"), out fd);
        Assert.Equal(PreparationInterop.EEXIST, errno);
    }

    [Fact]
    public void StagingDirectoryCollisionRefused()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        File.WriteAllBytes(Path.Combine(dirs.Root, "tool"), Bytes(0x5F, 64));
        Directory.CreateDirectory(Path.Combine(dirs.Parent, "prep-native"));
        var result = SafeCopyIn.Prepare(
            NativePlan(dirs, Tool()), LibcCopyHost.Instance, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.StagingFailure, result.Failure);
        Assert.Null(result.Closure);
    }

    [Fact]
    public void NoWritableFdRemainsAtPublication()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        File.WriteAllBytes(Path.Combine(dirs.Root, "tool"), Bytes(0x60, 1024));
        var result = SafeCopyIn.Prepare(
            NativePlan(dirs, Tool()), LibcCopyHost.Instance, CancellationToken.None);
        Assert.True(result.Succeeded, result.Detail);
        try
        {
            var staged = result.Closure!.Objects[0];
            var hit = FindProcFd(staged.Device, staged.Inode);
            Assert.True(hit.Found);
            Assert.False(hit.Writable, "a writable FD to the staged object remains open");
        }
        finally
        {
            result.Closure!.Dispose();
        }
    }

    private static (bool Found, bool Writable) FindProcFd(ulong device, ulong inode)
    {
        var found = false;
        var writable = false;
        string[] entries;
        try
        {
            entries = Directory.GetFiles("/proc/self/fd");
        }
        catch
        {
            return (false, false);
        }

        foreach (var entry in entries)
        {
            if (!int.TryParse(Path.GetFileName(entry), out var number))
            {
                continue;
            }

            if (PreparationInterop.StatFd(number, out var identity) != 0)
            {
                continue;
            }

            if (identity.Device != device || identity.Inode != inode)
            {
                continue;
            }

            found = true;
            try
            {
                var info = File.ReadAllText($"/proc/self/fdinfo/{number}");
                foreach (var line in info.Split('\n'))
                {
                    if (line.StartsWith("flags:", StringComparison.Ordinal))
                    {
                        var parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            try
                            {
                                // fdinfo flags print in octal; access mode is the low two bits.
                                if ((Convert.ToInt32(parts[1].Trim(), 8) & 0x3) != 0)
                                {
                                    writable = true;
                                }
                            }
                            catch (FormatException)
                            {
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        return (found, writable);
    }

    [Fact]
    public void RetainedFdRefersToVerifiedObject()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        File.WriteAllBytes(Path.Combine(dirs.Root, "tool"), Bytes(0x61, 512));
        var result = SafeCopyIn.Prepare(
            NativePlan(dirs, Tool()), LibcCopyHost.Instance, CancellationToken.None);
        Assert.True(result.Succeeded, result.Detail);
        try
        {
            var staged = result.Closure!.Objects[0];
            var fdNumber = ((SafeFileHandle)staged.Handle).DangerousGetHandle().ToInt32();
            Assert.Equal(0, PreparationInterop.StatFd(fdNumber, out var identity));
            Assert.Equal(staged.Device, identity.Device);
            Assert.Equal(staged.Inode, identity.Inode);
            Assert.Equal((ulong)512, (ulong)identity.Size);
        }
        finally
        {
            result.Closure!.Dispose();
        }
    }

    [Fact]
    public void NativeDigestMismatchRefused()
    {
        SkipUnlessUsable(out _);
        using var dirs = new NativeDirs();
        var content = Bytes(0x62, 256);
        File.WriteAllBytes(Path.Combine(dirs.Root, "tool"), content);
        var result = SafeCopyIn.Prepare(
            NativePlan(dirs, Tool("tool", ToolBudget, Digest(Bytes(0x63, 256)))),
            LibcCopyHost.Instance, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.DigestMismatch, result.Failure);
        Assert.Null(result.Closure);
    }

    [Fact]
    public void FixtureBuildArtifactBindsWithoutExecution()
    {
        SkipUnlessUsable(out _);
        var artifact = Environment.GetEnvironmentVariable("GAGAMBA_M1_FIXTURE");
        if (artifact is null)
        {
            var candidates = new[]
            {
                Path.GetFullPath("tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"),
                Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory, "..", "..", "..", "..",
                    "Fixtures", "Gagamba.LinuxM1.Native", "out", "fixture")),
            };
            artifact = candidates.FirstOrDefault(File.Exists);
        }

        if (artifact is null || !File.Exists(artifact))
        {
            Assert.Skip("fixture not built; run eng/linux-m1/build-native.sh first");
            return;
        }

        var expected = Digest(File.ReadAllBytes(artifact));
        using var dirs = new NativeDirs();
        File.Copy(artifact, Path.Combine(dirs.Root, "tool"));
        var result = SafeCopyIn.Prepare(
            NativePlan(dirs, Tool("tool", ToolBudget, expected)),
            LibcCopyHost.Instance, CancellationToken.None);
        Assert.True(result.Succeeded, result.Detail);
        try
        {
            Assert.Equal(expected, result.Closure!.Objects[0].Sha256);
            Assert.Equal(
                UnixFileMode.UserRead | UnixFileMode.UserExecute,
                (UnixFileMode)(result.Closure.Objects[0].Mode & 0x1FFu));
        }
        finally
        {
            result.Closure!.Dispose();
        }
    }
}
