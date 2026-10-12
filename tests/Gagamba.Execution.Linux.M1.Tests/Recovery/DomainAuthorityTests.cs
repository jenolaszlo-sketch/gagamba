// WP03 Linux-native authority tests (G2-01, G2-02, G2-09): explicit FD
// ownership with number-reuse sentinel, strict SCM_RIGHTS receive
// (count/layout/CLOEXEC), role/membership validation, and exec/CLOEXEC
// leakage probe. Every case is finite and skipped honestly off Linux.
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class DomainAuthorityTests
{
    [DllImport("libc", SetLastError = true)]
    private static extern int open(string path, int flags);

    private const string CgroupRoot = "/sys/fs/cgroup";

    private sealed class TempParent : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(CgroupRoot, "gagamba-r2-" + Guid.NewGuid().ToString("N"));

        public TempParent()
        {
            Directory.CreateDirectory(Path);
            try { File.WriteAllText(System.IO.Path.Combine(Path, "cgroup.subtree_control"), "+memory +pids"); } catch { }
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    foreach (var dir in Directory.EnumerateDirectories(Path))
                    {
                        try { Directory.Delete(dir); } catch { }
                    }

                    Directory.Delete(Path);
                }
            }
            catch
            {
            }

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
            var probe = System.IO.Path.Combine(CgroupRoot, "gagamba-r2-probe-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(probe);
            var ok = File.Exists(System.IO.Path.Combine(probe, "cgroup.kill"));
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

    private static string Boot() => CgroupInterop.TryReadBootId(out var id, out _) ? id : "unknown";

    private sealed class Domain : IDisposable
    {
        public required string ChildName;
        public required int[] Ordered;
        public required string ParentPath;
        public required string ChildPath;

        public void Dispose()
        {
            foreach (var fd in Ordered)
            {
                PreparationInterop.CloseFd(fd);
            }
        }
    }

    private static Domain BuildDomain(TempParent temp)
    {
        var childName = "d-" + Guid.NewGuid().ToString("N");
        Assert.Equal(0, PreparationInterop.OpenDirectory(temp.Path, out var parentFd));

        if (PreparationInterop.MakeDirectory(parentFd, childName, 0x1ED) != 0)
        {
            PreparationInterop.CloseFd(parentFd);
            throw new InvalidOperationException("mkdirat failed");
        }

        Assert.Equal(0, PreparationInterop.OpenRelative(
            parentFd, childName,
            PreparationInterop.O_RDONLY | PreparationInterop.O_DIRECTORY | PreparationInterop.O_NOFOLLOW | PreparationInterop.O_CLOEXEC,
            PreparationInterop.RequiredResolve, out var domainFd));

        var ordered = new int[DomainHandleTransfer.ExactDescriptorCount];
        ordered[(int)DomainTransferRole.Parent] = parentFd;
        ordered[(int)DomainTransferRole.DomainDirectory] = domainFd;
        ordered[(int)DomainTransferRole.Kill] = Open(domainFd, "cgroup.kill", PreparationInterop.O_WRONLY);
        ordered[(int)DomainTransferRole.Events] = Open(domainFd, "cgroup.events", PreparationInterop.O_RDONLY);
        ordered[(int)DomainTransferRole.MemoryMax] = Open(domainFd, "memory.max", PreparationInterop.O_RDWR);
        ordered[(int)DomainTransferRole.MemorySwapMax] = Open(domainFd, "memory.swap.max", PreparationInterop.O_RDWR);
        ordered[(int)DomainTransferRole.PidsMax] = Open(domainFd, "pids.max", PreparationInterop.O_RDWR);

        return new Domain
        {
            ChildName = childName,
            Ordered = ordered,
            ParentPath = temp.Path,
            ChildPath = System.IO.Path.Combine(temp.Path, childName),
        };
    }

    private static int Open(int dirFd, string name, int flags)
    {
        Assert.Equal(0, PreparationInterop.OpenRelative(
            dirFd, name, flags | PreparationInterop.O_CLOEXEC, PreparationInterop.RequiredResolve, out var fd));
        return fd;
    }

    // G2-01: FD-number reuse sentinel.
    [Fact]
    public void ScopeDisposalDoesNotCloseReusedDescriptor()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
        var allocation = scope.AllocateDomain("d-" + Guid.NewGuid().ToString("N"), Guid.NewGuid(), Guid.NewGuid(), Boot());
        Assert.True(allocation.Ok, allocation.Error);
        allocation.RecoveryLease!.Dispose();
        PreparationInterop.CloseFd(allocation.OwnerSocketFd);

        // Open a sentinel that may reuse a number freed by any premature
        // close; disposing the scope must not close it.
        var sentinel = open("/dev/null", 0);
        Assert.True(sentinel >= 0);
        Assert.True(PreparationInterop.IsValidFd(sentinel));
        scope.Dispose();
        Assert.True(PreparationInterop.IsValidFd(sentinel), "scope disposal closed an unrelated reused descriptor");
        PreparationInterop.CloseFd(sentinel);
    }

    [Fact]
    public void RepeatedScopeDisposalIsSafe()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
        scope.Dispose();
        scope.Dispose();
    }

    // G2-09: role/membership validation.
    [Fact]
    public void ValidHandlesValidate()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var domain = BuildDomain(temp);
        Assert.True(CgroupInterop.TryValidateDomainHandles(
            domain.Ordered, domain.ChildName, Guid.NewGuid(), Guid.NewGuid(), Boot(),
            out var backend, out var identity, out var error), error);
        Assert.NotNull(backend);
        Assert.Equal(domain.ChildName, identity!.ChildName);
        backend!.Dispose();
    }

    [Fact]
    public void ParentAsChildRefused()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var domain = BuildDomain(temp);
        var mutated = (int[])domain.Ordered.Clone();
        mutated[(int)DomainTransferRole.DomainDirectory] = mutated[(int)DomainTransferRole.Parent];
        Assert.False(CgroupInterop.TryValidateDomainHandles(
            mutated, domain.ChildName, Guid.NewGuid(), Guid.NewGuid(), Boot(), out _, out _, out _));
    }

    [Fact]
    public void SwappedControlsRefused()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var domain = BuildDomain(temp);
        var mutated = (int[])domain.Ordered.Clone();
        (mutated[(int)DomainTransferRole.Kill], mutated[(int)DomainTransferRole.Events]) =
            (mutated[(int)DomainTransferRole.Events], mutated[(int)DomainTransferRole.Kill]);
        Assert.False(CgroupInterop.TryValidateDomainHandles(
            mutated, domain.ChildName, Guid.NewGuid(), Guid.NewGuid(), Boot(), out _, out _, out _));
    }

    [Fact]
    public void NullDescriptorsRefused()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var domain = BuildDomain(temp);
        var mutated = (int[])domain.Ordered.Clone();
        var devnull = open("/dev/null", 0);
        mutated[(int)DomainTransferRole.DomainDirectory] = devnull;
        Assert.False(CgroupInterop.TryValidateDomainHandles(
            mutated, domain.ChildName, Guid.NewGuid(), Guid.NewGuid(), Boot(), out _, out _, out _));
        PreparationInterop.CloseFd(devnull);
    }

    [Fact]
    public void SiblingControlRefused()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var domain = BuildDomain(temp);

        // A sibling cgroup with its own cgroup.events.
        var siblingName = "s-" + Guid.NewGuid().ToString("N");
        Assert.Equal(0, PreparationInterop.MakeDirectory(domain.Ordered[(int)DomainTransferRole.Parent], siblingName, 0x1ED));
        Assert.Equal(0, PreparationInterop.OpenRelative(
            domain.Ordered[(int)DomainTransferRole.Parent], siblingName,
            PreparationInterop.O_RDONLY | PreparationInterop.O_DIRECTORY | PreparationInterop.O_NOFOLLOW | PreparationInterop.O_CLOEXEC,
            PreparationInterop.RequiredResolve, out var siblingFd));
        var siblingEvents = Open(siblingFd, "cgroup.events", PreparationInterop.O_RDONLY);

        var mutated = (int[])domain.Ordered.Clone();
        mutated[(int)DomainTransferRole.Events] = siblingEvents;
        Assert.False(CgroupInterop.TryValidateDomainHandles(
            mutated, domain.ChildName, Guid.NewGuid(), Guid.NewGuid(), Boot(), out _, out _, out _));
        PreparationInterop.CloseFd(siblingEvents);
        PreparationInterop.CloseFd(siblingFd);
    }

    [Fact]
    public void MissingCloexecRefused()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var domain = BuildDomain(temp);
        // Replace one role with a descriptor opened without CLOEXEC;
        // validation must reject the missing FD_CLOEXEC.
        var noCloexec = open("/dev/null", 0);
        var mutated = (int[])domain.Ordered.Clone();
        mutated[(int)DomainTransferRole.Events] = noCloexec;
        Assert.False(CgroupInterop.TryValidateDomainHandles(
            mutated, domain.ChildName, Guid.NewGuid(), Guid.NewGuid(), Boot(), out _, out _, out _));
        PreparationInterop.CloseFd(noCloexec);
    }

    // G2-02: strict transfer over a real socketpair.
    [Fact]
    public void ValidTransferReceivesCloexecDescriptors()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var domain = BuildDomain(temp);
        var exec = Guid.NewGuid();
        var lease = Guid.NewGuid();
        var payload = DomainHandleTransfer.Encode(exec, lease);

        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var parentEnd, out var childEnd, out _));
        Assert.True(FdPassing.TrySendTransfer(parentEnd, domain.Ordered, payload, out var sendError), sendError);

        Assert.True(FdPassing.TryReceiveTransfer(
            childEnd, DomainHandleTransfer.ExactDescriptorCount, DomainHandleTransfer.ExactPayloadBytes,
            out var received, out var receivedPayload, out var recvError), recvError);
        Assert.True(DomainHandleTransfer.TryDecode(receivedPayload, exec, lease, out _, out _));
        foreach (var fd in received)
        {
            Assert.True(PreparationInterop.HasCloexec(fd), $"fd {fd} lacks CLOEXEC");
        }

        foreach (var fd in received)
        {
            PreparationInterop.CloseFd(fd);
        }

        PreparationInterop.CloseFd(parentEnd);
        PreparationInterop.CloseFd(childEnd);
    }

    [Fact]
    public void TooFewDescriptorsRefused()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var domain = BuildDomain(temp);
        var payload = DomainHandleTransfer.Encode(Guid.NewGuid(), Guid.NewGuid());
        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var parentEnd, out var childEnd, out _));
        var six = domain.Ordered[..6];
        Assert.True(FdPassing.TrySendTransfer(parentEnd, six, payload, out _));
        Assert.False(FdPassing.TryReceiveTransfer(
            childEnd, DomainHandleTransfer.ExactDescriptorCount, DomainHandleTransfer.ExactPayloadBytes,
            out _, out _, out _));
        PreparationInterop.CloseFd(parentEnd);
        PreparationInterop.CloseFd(childEnd);
    }

    [Fact]
    public void TooManyDescriptorsRefused()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var domain = BuildDomain(temp);
        var payload = DomainHandleTransfer.Encode(Guid.NewGuid(), Guid.NewGuid());
        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var parentEnd, out var childEnd, out _));
        var eight = new int[8];
        Array.Copy(domain.Ordered, eight, 7);
        eight[7] = domain.Ordered[0];
        Assert.True(FdPassing.TrySendTransfer(parentEnd, eight, payload, out _));
        Assert.False(FdPassing.TryReceiveTransfer(
            childEnd, DomainHandleTransfer.ExactDescriptorCount, DomainHandleTransfer.ExactPayloadBytes,
            out _, out _, out _));
        PreparationInterop.CloseFd(parentEnd);
        PreparationInterop.CloseFd(childEnd);
    }

    [Fact]
    public void TruncatedPayloadRefused()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var domain = BuildDomain(temp);
        var payload = DomainHandleTransfer.Encode(Guid.NewGuid(), Guid.NewGuid())[..^1];
        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var parentEnd, out var childEnd, out _));
        Assert.True(FdPassing.TrySendTransfer(parentEnd, domain.Ordered, payload, out _));
        Assert.False(FdPassing.TryReceiveTransfer(
            childEnd, DomainHandleTransfer.ExactDescriptorCount, DomainHandleTransfer.ExactPayloadBytes,
            out _, out _, out _));
        PreparationInterop.CloseFd(parentEnd);
        PreparationInterop.CloseFd(childEnd);
    }

    [Fact]
    public void ExecProbeProvesReceivedDescriptorsDoNotSurvive()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var domain = BuildDomain(temp);
        var exec = Guid.NewGuid();
        var lease = Guid.NewGuid();
        var payload = DomainHandleTransfer.Encode(exec, lease);
        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var parentEnd, out var childEnd, out _));
        Assert.True(FdPassing.TrySendTransfer(parentEnd, domain.Ordered, payload, out _));
        Assert.True(FdPassing.TryReceiveTransfer(
            childEnd, DomainHandleTransfer.ExactDescriptorCount, DomainHandleTransfer.ExactPayloadBytes,
            out var received, out _, out _));

        // A control descriptor without CLOEXEC must survive exec; the
        // received authority descriptors must not.
        var control = open("/dev/null", 0);
        var controlHex = control.ToString();
        var authorityHex = received[0].ToString();

        var probe = Process.Start(new ProcessStartInfo("/bin/sh")
        {
            Arguments = $"-c \"for f in {controlHex} {authorityHex}; do if [ -e /proc/self/fd/$f ]; then echo $f:present; else echo $f:absent; fi; done\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
        })!;
        var output = probe.StandardOutput.ReadToEnd();
        probe.WaitForExit();
        Assert.Contains($"{control}:present", output);
        Assert.Contains($"{authorityHex}:absent", output);

        foreach (var fd in received)
        {
            PreparationInterop.CloseFd(fd);
        }

        PreparationInterop.CloseFd(control);
        PreparationInterop.CloseFd(parentEnd);
        PreparationInterop.CloseFd(childEnd);
    }
}
