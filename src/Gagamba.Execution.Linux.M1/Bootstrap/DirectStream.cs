using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// One synchronous bounded reader and one writer. No queued pump. The caller serializes each direction.
internal sealed class DirectStream : IDisposable
{
    internal const int MaxFrameBytes = 64 * 1024;
    private readonly SafeFileHandle _socket;
    private bool _failed;
    internal int Descriptor => _socket.DangerousGetHandle().ToInt32();
    internal DirectStream(int fd)
    {
        _socket = new SafeFileHandle((IntPtr)fd, true);
        if (!PreparationInterop.HasCloexec(fd)) { Dispose(); throw new IOException("Direct socket lacks CLOEXEC"); }
    }
    internal static long Deadline(int milliseconds)
    {
        if (milliseconds is < 0 or > 120000) throw new ArgumentOutOfRangeException(nameof(milliseconds));
        return checked(Stopwatch.GetTimestamp() + milliseconds * Stopwatch.Frequency / 1000);
    }
    internal static int Remaining(long deadline) => (int)Math.Clamp(
        (deadline - Stopwatch.GetTimestamp()) * 1000 / Stopwatch.Frequency, 0, 120000);
    internal static bool Wait(int fd, short events, long deadline)
    {
        do
        {
            var poll = new DirectUnix.PollFd { Fd = fd, Events = events };
            var result = DirectUnix.Poll(ref poll, (UIntPtr)1, Remaining(deadline));
            if (result > 0) return (poll.Revents & events) != 0;
            if (result == 0 || Marshal.GetLastPInvokeError() != 4) return false;
        } while (Remaining(deadline) > 0);
        return false;
    }
    internal void Send(byte[] frame, long deadline)
    {
        if (_failed || frame.Length is < 1 or > MaxFrameBytes) throw new IOException("Direct frame size/state refused");
        var packet = new byte[4 + frame.Length];
        BinaryPrimitives.WriteInt32LittleEndian(packet, frame.Length); frame.CopyTo(packet, 4);
        try
        {
            var pinned = GCHandle.Alloc(packet, GCHandleType.Pinned);
            try
            {
                var offset = 0;
                while (offset < packet.Length)
                {
                    if (!Wait(Descriptor, 4, deadline)) throw new IOException("Direct send timed out/lost peer");
                    var count = DirectUnix.Send(Descriptor, pinned.AddrOfPinnedObject() + offset,
                        (UIntPtr)(packet.Length - offset), 0x4000 | 0x40 /* NOSIGNAL DONTWAIT */);
                    if (count < 0 && Marshal.GetLastPInvokeError() is 4 or 11) continue;
                    if (count <= 0) throw new IOException("Direct send failed");
                    offset += checked((int)count);
                }
            }
            finally { pinned.Free(); }
        }
        catch { Fail(); throw; }
        finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(packet); }
    }
    internal bool TryReceive(long deadline, out byte[] frame)
    {
        frame = [];
        if (_failed) throw new IOException("Direct stream lost");
        var prefix = new byte[4];
        try
        {
            if (!ReadExact(prefix, deadline, idleAllowed: true)) return false;
            var length = BinaryPrimitives.ReadInt32LittleEndian(prefix);
            // Checked BEFORE allocation. A coalesced next frame remains in the kernel socket queue.
            if (length is < 1 or > MaxFrameBytes) throw new IOException("Direct frame bound refused");
            frame = new byte[length];
            _ = ReadExact(frame, deadline, idleAllowed: false);
            return true;
        }
        catch { Fail(); throw; }
    }
    private bool ReadExact(byte[] bytes, long deadline, bool idleAllowed)
    {
        var pinned = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        var vector = Marshal.AllocHGlobal(Marshal.SizeOf<DirectUnix.Iovec>());
        // Linux SCM_MAX_FD = 253. Capture and close ALL delivered rights before rejecting ancillary traffic.
        var ancillary = Marshal.AllocHGlobal(2048);
        try
        {
            var offset = 0;
            while (offset < bytes.Length)
            {
                if (!Wait(Descriptor, 1, deadline))
                {
                    if (offset == 0 && idleAllowed && IsAlive()) return false;
                    throw new IOException("Truncated/timed out direct frame");
                }
                Marshal.StructureToPtr(new DirectUnix.Iovec { Base = pinned.AddrOfPinnedObject() + offset,
                    Length = (UIntPtr)(bytes.Length - offset) }, vector, false);
                var message = new DirectUnix.Message { Vectors = vector, VectorCount = (UIntPtr)1,
                    Control = ancillary, ControlLength = (UIntPtr)2048 };
                var count = DirectUnix.Receive(Descriptor, ref message, 0x40000000 | 0x40 /* CMSG_CLOEXEC DONTWAIT */);
                if (count < 0 && Marshal.GetLastPInvokeError() is 4 or 11) continue;
                if (count < 0) throw new IOException("Direct receive failed");
                var controlLength = checked((int)message.ControlLength.ToUInt64());
                CloseRights(ancillary, Math.Min(controlLength, 2048));
                if (controlLength != 0 || (message.Flags & (0x8 | 0x20)) != 0)
                    throw new IOException("Ancillary/truncated direct frame refused");
                if (count <= 0) throw new IOException("Direct peer lost");
                offset += checked((int)count);
            }
            return true;
        }
        finally { pinned.Free(); Marshal.FreeHGlobal(vector); Marshal.FreeHGlobal(ancillary); }
    }
    private static void CloseRights(IntPtr buffer, int size)
    {
        for (var offset = 0; offset + 16 <= size;)
        {
            var length = Marshal.ReadInt64(buffer, offset);
            if (length < 16 || length > size - offset) break;
            if (Marshal.ReadInt32(buffer, offset + 8) == 1 && Marshal.ReadInt32(buffer, offset + 12) == 1)
                for (var i = offset + 16; i + 4 <= offset + length; i += 4)
                    PreparationInterop.CloseFd(Marshal.ReadInt32(buffer, i));
            offset += checked(((int)length + 7) & ~7);
        }
    }
    internal bool IsAlive()
    {
        var retained=false;
        try
        {
            if(_failed) return false;
            _socket.DangerousAddRef(ref retained);
            var poll = new DirectUnix.PollFd { Fd = Descriptor, Events = 0x2000 /* RDHUP */ };
            return !_failed && DirectUnix.Poll(ref poll, (UIntPtr)1, 0) >= 0 && (poll.Revents & (0x2000 | 0x8 | 0x10 | 0x20)) == 0;
        }
        catch(ObjectDisposedException) {return false;}
        finally {if(retained) _socket.DangerousRelease();}
    }
    internal void Fail() { _failed = true; _ = DirectUnix.Shutdown(Descriptor, 2); }
    public void Dispose() { _failed = true; _socket.Dispose(); }
}
