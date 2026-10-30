using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

public sealed class DirectAuthenticationTests
{
    private static void RequireLinux()
    {
        if (!OperatingSystem.IsLinux()) Assert.Skip("Linux direct socket primitive qualification required");
        Assert.True(RecoveryInterop.IsUnprivileged, "requires an unprivileged test process");
        Assert.True(DirectPeerIdentity.FeatureAvailable(), "SO_PEERPIDFD must be available; no fallback");
    }
    private sealed class Probe : IDisposable
    {
        internal Process Process { get; }
        internal SafeFileHandle OwnerPidfd { get; }
        internal DirectAssociation Association { get; }
        internal string Address { get; }
        internal Probe()
        {
            var dll = Environment.GetEnvironmentVariable("GAGAMBA_B2_CONTROLLER_DLL");
            Assert.True(dll is not null && File.Exists(dll), "explicit test-only direct probe artifact required");
            var start = new ProcessStartInfo("/usr/bin/dotnet") { UseShellExecute = false,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(dll!); start.ArgumentList.Add("--direct-peer");
            Process = Process.Start(start)!;
            Assert.True(PidFd.TryOpen(Process.Id, out var fd, out var error), error);
            OwnerPidfd = new SafeFileHandle((IntPtr)fd, true);
            var current = DirectProcessIdentity.Current();
            Association = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), current,
                current with { Pid = Process.Id }, current with { Pid = RecoveryInterop.GetParentPid() });
            Process.StandardInput.WriteLine(JsonSerializer.Serialize(Association)); Process.StandardInput.Flush();
            Address = Process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult()!;
            Assert.NotNull(Address);
        }
        public void Dispose()
        {
            if (!Process.HasExited) Process.Kill();
            Assert.True(Process.WaitForExit(5000)); OwnerPidfd.Dispose(); Process.Dispose();
        }
    }
    [Fact] public void PeerPidfdFeatureAndExactLiveReference()
    {
        RequireLinux();
        Assert.True(PidFd.TryOpen(Environment.ProcessId, out var fd, out _));
        using var handle = new SafeFileHandle((IntPtr)fd, true);
        Assert.True(DirectPeerIdentity.LiveReference(fd, DirectProcessIdentity.Current()));
        Assert.False(DirectPeerIdentity.LiveReference(fd, DirectProcessIdentity.Current() with { Pid = 1 }));
    }
    [Fact] public void RealDistinctProcessesConfirmOneDirectSessionAndRemoveListener()
    {
        RequireLinux(); using var probe = new Probe(); var deadline = DirectStream.Deadline(2000);
        using var session = DirectSession.Establish(DirectOwnerListener.Connect(probe.Address, deadline),
            probe.Association, probe.OwnerPidfd.DangerousGetHandle().ToInt32(), false, deadline);
        session.SendControl([42]); Assert.True(session.TryReceiveControl(2000, out var response)); Assert.Equal(new byte[] {43}, response);
        Assert.True(probe.Process.WaitForExit(5000)); Assert.Equal(0, probe.Process.ExitCode);
        Assert.False(File.Exists(probe.Address));
        Assert.Throws<IOException>(() => DirectOwnerListener.Connect(probe.Address, DirectStream.Deadline(100)));
    }
    [Theory] [InlineData("owner")][InlineData("controller")][InlineData("execution")][InlineData("generation")][InlineData("session")]
    public void WrongExactIdentityOrTranscriptNeverAssociates(string change)
    {
        RequireLinux(); using var probe = new Probe(); var original = probe.Association;
        var association = change switch
        {
            "owner" => original with { Owner = original.Owner with { Pid = int.MaxValue } },
            "controller" => original with { Controller = original.Controller with { Pid = int.MaxValue } },
            "execution" => original with { Execution = Guid.NewGuid() },
            "generation" => original with { Generation = Guid.NewGuid() },
            _ => original with { OriginalSession = Guid.NewGuid() },
        };
        var deadline = DirectStream.Deadline(500);
        using var stream = DirectOwnerListener.Connect(probe.Address, deadline);
        Assert.ThrowsAny<IOException>(() => DirectSession.Establish(stream, association,
            probe.OwnerPidfd.DangerousGetHandle().ToInt32(), false, deadline));
    }
    [Fact] public void SameUidDifferentProcessCannotImpersonateExpectedPeer()
    {
        RequireLinux(); var pair = Pair(); using var a = new DirectStream(pair[0]); using var b = new DirectStream(pair[1]);
        using var helper = Process.Start(new ProcessStartInfo("/bin/sleep", "2") { UseShellExecute = false })!;
        try
        {
            Assert.True(PidFd.TryOpen(helper.Id, out var fd, out _)); using var retained = new SafeFileHandle((IntPtr)fd, true);
            Assert.Throws<IOException>(() => DirectPeerIdentity.Verify(pair[0], DirectProcessIdentity.Current() with { Pid = helper.Id }, fd));
        }
        finally { if (!helper.HasExited) helper.Kill(); Assert.True(helper.WaitForExit(5000)); }
    }
    [Fact] public void StaleRetainedPeerReferenceRefusesWithoutReopeningPid()
    {
        RequireLinux(); using var helper = Process.Start(new ProcessStartInfo("/bin/sleep", "2") { UseShellExecute = false })!;
        Assert.True(PidFd.TryOpen(helper.Id, out var fd, out _)); using var retained = new SafeFileHandle((IntPtr)fd, true);
        helper.Kill(); Assert.True(helper.WaitForExit(5000));
        Assert.False(DirectPeerIdentity.LiveReference(fd, DirectProcessIdentity.Current() with { Pid = helper.Id }));
    }
    [Fact] public async Task FragmentedAndCoalescedStreamFramesAreReadExactly()
    {
        RequireLinux(); var pair = Pair(); using var receiver = new DirectStream(pair[0]); using var sender = new DirectStream(pair[1]);
        var first = Packet([1, 2, 3]); var second = Packet([4, 5]);
        var writing = Task.Run(() => { foreach (var item in first) RawSend(pair[1], [item]); RawSend(pair[1], second.Concat(Packet([6])).ToArray()); }, TestContext.Current.CancellationToken);
        Assert.True(receiver.TryReceive(DirectStream.Deadline(1000), out var one)); Assert.Equal(new byte[] {1,2,3}, one);
        Assert.True(receiver.TryReceive(DirectStream.Deadline(1000), out var two)); Assert.Equal(new byte[] {4,5}, two);
        Assert.True(receiver.TryReceive(DirectStream.Deadline(1000), out var three)); Assert.Equal(new byte[] {6}, three);
        await writing;
    }
    [Theory][InlineData(-1)][InlineData(0)][InlineData(65537)][InlineData(int.MaxValue)]
    public void LengthIsRefusedBeforeAllocation(int length)
    {
        RequireLinux(); var pair = Pair(); using var receiver = new DirectStream(pair[0]); using var sender = new DirectStream(pair[1]);
        var prefix = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(prefix, length); RawSend(pair[1], prefix);
        Assert.Throws<IOException>(() => receiver.TryReceive(DirectStream.Deadline(100), out _));
    }
    [Fact] public void TruncationAndPartialFrameTimeoutAreTerminal()
    {
        RequireLinux(); var pair = Pair(); using var receiver = new DirectStream(pair[0]); using var sender = new DirectStream(pair[1]);
        RawSend(pair[1], [4,0]);
        Assert.Throws<IOException>(() => receiver.TryReceive(DirectStream.Deadline(30), out _));
        Assert.Throws<IOException>(() => receiver.TryReceive(DirectStream.Deadline(30), out _));
    }
    [Fact] public void UnexpectedAncillaryRightsAreClosedAndRefused()
    {
        RequireLinux(); var pair = Pair(); using var receiver = new DirectStream(pair[0]); using var sender = new DirectStream(pair[1]);
        Assert.True(PidFd.TryOpen(Environment.ProcessId, out var fd, out _)); using var original = new SafeFileHandle((IntPtr)fd, true);
        var count = Directory.GetFiles("/proc/self/fd").Length;
        Assert.True(FdPassing.TrySendTransfer(pair[1], [fd], Packet([1]), out var error), error);
        Assert.Throws<IOException>(() => receiver.TryReceive(DirectStream.Deadline(100), out _));
        Assert.Equal(count, Directory.GetFiles("/proc/self/fd").Length);
    }
    [Fact] public void ListenerModesAndSymlinkSubstitutionRefuse()
    {
        if (!OperatingSystem.IsLinux()) { Assert.Skip("Linux listener required"); return; }
        RequireLinux(); using var listener = DirectOwnerListener.Create("/tmp");
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(listener.Address));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
            File.GetUnixFileMode(Path.GetDirectoryName(listener.Address)!));
        File.Delete(listener.Address); File.CreateSymbolicLink(listener.Address, "/dev/null");
        Assert.False(listener.VerifyEntry()); Assert.Throws<IOException>(() => listener.Accept(DirectStream.Deadline(50)));
        File.Delete(listener.Address); Directory.Delete(Path.GetDirectoryName(listener.Address)!);
    }
    [Fact] public void TranscriptBindsRolesIdentitiesAndChallenges()
    {
        var controller = new DirectProcessIdentity(101,1000,1000,"pid:[1]","user:[1]");
        var a = new DirectAssociation(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),controller,controller with {Pid=102},controller with {Pid=103});
        var key = RandomNumberGenerator.GetBytes(32); var x = RandomNumberGenerator.GetBytes(32); var y = RandomNumberGenerator.GetBytes(32);
        var transcript = DirectSession.Transcript(a,x,y); var mac = DirectSession.Confirmation(key,transcript,"owner-confirm");
        Assert.NotEqual(mac,DirectSession.Confirmation(key,transcript,"controller-confirm"));
        Assert.NotEqual(mac,DirectSession.Confirmation(key,DirectSession.Transcript(a,y,x),"owner-confirm"));
        Assert.NotEqual(mac,DirectSession.Confirmation(key,DirectSession.Transcript(a with {Generation=Guid.NewGuid()},x,y),"owner-confirm"));
        Assert.NotEqual(mac,DirectSession.Confirmation(key,DirectSession.Transcript(a with {Controller=a.Recovery},x,y),"owner-confirm"));
    }
    private static int[] Pair() { var pair = new int[2]; Assert.Equal(0, DirectUnix.SocketPair(1,1|DirectUnix.Cloexec|DirectUnix.Nonblock,0,pair)); return pair; }
    private static byte[] Packet(byte[] payload) { var bytes = new byte[payload.Length+4]; BinaryPrimitives.WriteInt32LittleEndian(bytes,payload.Length); payload.CopyTo(bytes,4); return bytes; }
    private static void RawSend(int fd, byte[] bytes)
    {
        var pin = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try { Assert.Equal(bytes.Length, DirectUnix.Send(fd,pin.AddrOfPinnedObject(),(UIntPtr)bytes.Length,0x4000)); }
        finally { pin.Free(); }
    }
}
