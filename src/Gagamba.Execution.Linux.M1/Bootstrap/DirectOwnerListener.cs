using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// Owner constructs this AFTER recovery has spawned it. Recovery receives only Address.
// Descriptor-relative creation/cleanup pins both directories; substituted names fail closed.
internal sealed class DirectOwnerListener : IDisposable
{
    private readonly SafeFileHandle _parent;
    private readonly SafeFileHandle _directory;
    private readonly SafeFileHandle _listener;
    private readonly FileIdentity _directoryIdentity;
    private readonly FileIdentity _socketIdentity;
    private readonly string _name;
    private bool _closed;
    internal string Address { get; }
    internal const int MaxAttempts = 4;
    private DirectOwnerListener(SafeFileHandle parent, SafeFileHandle directory, SafeFileHandle listener,
        string name, string address, FileIdentity directoryIdentity, FileIdentity socketIdentity)
    {
        _parent = parent; _directory = directory; _listener = listener; _name = name;
        Address = address; _directoryIdentity = directoryIdentity; _socketIdentity = socketIdentity;
    }
    internal static DirectOwnerListener Create(string trustedParent)
    {
        if (!RecoveryInterop.IsUnprivileged || !DirectPeerIdentity.FeatureAvailable())
            throw new IOException("Direct association prerequisites unavailable");
        if (!Path.IsPathFullyQualified(trustedParent)) throw new IOException("Rendezvous parent refused");
        // Resolve beneath / with no symlink or magic-link traversal, including ancestors.
        if (PreparationInterop.OpenDirectory("/", out var root) != 0) throw new IOException("Root open refused");
        using var rootHandle = new SafeFileHandle((IntPtr)root, true);
        var how = new PreparationInterop.OpenHow { Flags = (ulong)(PreparationInterop.O_RDONLY |
            PreparationInterop.O_DIRECTORY | PreparationInterop.O_CLOEXEC), Resolve = PreparationInterop.RequiredResolve };
        if (PreparationInterop.OpenRelative(root, trustedParent.TrimStart('/'), (int)how.Flags, how.Resolve, out var parentFd) != 0)
            throw new IOException("Trusted rendezvous parent refused");
        var parent = new SafeFileHandle((IntPtr)parentFd, true);
        SafeFileHandle? directory = null, listener = null;
        var name = "gagamba-" + Guid.NewGuid().ToString("N");
        try
        {
            var address = Path.Combine(trustedParent, name, "owner.sock"); _ = DirectUnix.Address(address);
            if (PreparationInterop.MakeDirectory(parentFd, name, PreparationInterop.Mode0700) != 0)
                throw new IOException("Fresh rendezvous creation refused");
            how.Resolve = PreparationInterop.RequiredResolve;
            if (PreparationInterop.OpenRelative(parentFd, name, (int)how.Flags, how.Resolve, out var dirFd) != 0)
                throw new IOException("Fresh rendezvous open refused");
            directory = new SafeFileHandle((IntPtr)dirFd, true);
            if (PreparationInterop.StatFd(dirFd, out var dirStat) != 0 || !PreparationInterop.IsDirectory(dirStat.Mode)
                || (dirStat.Mode & 0x1ff) != PreparationInterop.Mode0700)
                throw new IOException("Rendezvous directory mode refused");
            var socket = DirectUnix.Socket(1, 1 | DirectUnix.Cloexec | DirectUnix.Nonblock, 0);
            if (socket < 0) throw new IOException("Atomic CLOEXEC stream socket unavailable");
            listener = new SafeFileHandle((IntPtr)socket, true);
            // Kernel resolves the socket name relative to our retained directory, not a replaceable ancestor path.
            var bindAddress = DirectUnix.Address("/proc/self/fd/" + dirFd + "/owner.sock");
            if (DirectUnix.Bind(socket, bindAddress, (uint)bindAddress.Length) != 0
                || DirectUnix.ChmodAt(dirFd, "owner.sock", PreparationInterop.Mode0600, 0) != 0
                || !TryIdentity(dirFd, "owner.sock", out var socketStat) || (socketStat.Mode & 0xf000) != 0xc000
                || (socketStat.Mode & 0x1ff) != PreparationInterop.Mode0600
                || DirectUnix.Listen(socket, MaxAttempts) != 0)
                throw new IOException("Protected listener creation refused");
            var result = new DirectOwnerListener(parent, directory, listener, name, address, dirStat, socketStat);
            if (!result.VerifyEntry()) { result.Dispose(); throw new IOException("Listener entry substituted"); }
            return result;
        }
        catch
        {
            listener?.Dispose();
            // Only remove names relative to the private directory. Never follow a substituted entry.
            if (directory is not null) { PreparationInterop.Unlink(directory.DangerousGetHandle().ToInt32(), "owner.sock"); directory.Dispose(); }
            PreparationInterop.RemoveDirectory(parentFd, name); parent.Dispose(); throw;
        }
    }
    private static bool TryIdentity(int directory, string name, out FileIdentity identity)
    {
        identity = default;
        var how = new PreparationInterop.OpenHow { Flags = 0x200000 /* O_PATH */ | PreparationInterop.O_CLOEXEC |
            PreparationInterop.O_NOFOLLOW, Resolve = PreparationInterop.RequiredResolve };
        if (PreparationInterop.OpenRelative(directory, name, (int)how.Flags, how.Resolve, out var fd) != 0) return false;
        using var handle = new SafeFileHandle((IntPtr)fd, true);
        return PreparationInterop.StatFd(fd, out identity) == 0;
    }
    internal bool VerifyEntry() => !_closed
        && TryIdentity(_parent.DangerousGetHandle().ToInt32(), _name, out var directory)
        && directory.Device == _directoryIdentity.Device && directory.Inode == _directoryIdentity.Inode
        && (directory.Mode & 0xffff) == (_directoryIdentity.Mode & 0xffff)
        && TryIdentity(_directory.DangerousGetHandle().ToInt32(), "owner.sock", out var socket)
        && socket.Device == _socketIdentity.Device && socket.Inode == _socketIdentity.Inode
        && socket.Mode == _socketIdentity.Mode;
    internal DirectStream Accept(long deadline)
    {
        if (!VerifyEntry() || !DirectStream.Wait(_listener.DangerousGetHandle().ToInt32(), 1, deadline))
            throw new IOException("Owner association deadline/entry refused");
        var fd = DirectUnix.Accept(_listener.DangerousGetHandle().ToInt32(), IntPtr.Zero, IntPtr.Zero,
            DirectUnix.Cloexec | DirectUnix.Nonblock);
        if (fd < 0) throw new IOException("Atomic CLOEXEC accept refused");
        return new DirectStream(fd);
    }
    internal static DirectStream Connect(string address, long deadline)
    {
        var fd = DirectUnix.Socket(1, 1 | DirectUnix.Cloexec | DirectUnix.Nonblock, 0);
        if (fd < 0) throw new IOException("Atomic CLOEXEC connection unavailable");
        var stream = new DirectStream(fd);
        try
        {
            var bytes = DirectUnix.Address(address);
            var result = DirectUnix.Connect(fd, bytes, (uint)bytes.Length);
            if (result != 0)
            {
                var error = System.Runtime.InteropServices.Marshal.GetLastPInvokeError();
                if (error is not (115 or 11) || !DirectStream.Wait(fd, 4, deadline))
                    throw new IOException("Direct connect refused");
                uint length = 4;
                if (DirectUnix.IntOption(fd, 1, 4 /* SO_ERROR */, out error, ref length) != 0 || error != 0)
                    throw new IOException("Direct connect failed");
            }
            return stream;
        }
        catch { stream.Dispose(); throw; }
    }
    public void Dispose()
    {
        if (_closed) return;
        var verified = VerifyEntry(); _closed = true; _listener.Dispose();
        if (verified)
        {
            PreparationInterop.Unlink(_directory.DangerousGetHandle().ToInt32(), "owner.sock");
            PreparationInterop.RemoveDirectory(_parent.DangerousGetHandle().ToInt32(), _name);
        }
        _directory.Dispose(); _parent.Dispose();
    }
}
