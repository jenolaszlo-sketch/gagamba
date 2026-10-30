using System.Diagnostics;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// Recovery-only bootstrap roles, disjoint from the seven domain descriptors.
// This object cannot hold a release socket, secret, capability or physical writer.
internal sealed class IntegratedOwnerLaunch : IDisposable
{
    internal OwnerDirectIdentity Identity { get; }
    internal int ControllerPidfd { get; }
    internal long AcceptedAtNs { get; }
    internal ExperimentalRuntimePin Runtime => Identity.Runtime;
    internal Action<Process,int> OwnerSpawned { get; }
    internal Action<OwnerRendezvous,int> Rendezvous { get; }
    private readonly SafeFileHandle _safety;
    private readonly SafeFileHandle _ownerSafety;
    internal int OwnerSafetyFd => _ownerSafety.DangerousGetHandle().ToInt32();
    internal IntegratedOwnerLaunch(OwnerDirectIdentity identity, int controllerPidfd, long acceptedAt,
        Action<Process,int> ownerSpawned, Action<OwnerRendezvous,int> rendezvous)
    {
        Identity=identity; ControllerPidfd=controllerPidfd; AcceptedAtNs=acceptedAt;
        OwnerSpawned=ownerSpawned; Rendezvous=rendezvous;
        var pair=new int[2];
        if(DirectUnix.SocketPair(1,5|DirectUnix.Cloexec|DirectUnix.Nonblock,0,pair)!=0)
            throw new IOException("Dedicated recovery safety lifeline unavailable");
        _safety=new SafeFileHandle((IntPtr)pair[0],true); _ownerSafety=new SafeFileHandle((IntPtr)pair[1],true);
    }
    internal void CloseTransferredSafetyEnd() => _ownerSafety.Dispose();
    internal bool ObserveOwnerStop() => Native.FdPassing.TryReceiveMessage(_safety.DangerousGetHandle().ToInt32(),0,
        out var payload,out _) && payload.AsSpan().SequenceEqual(new byte[] {2});
    internal bool RequestStop() => Native.FdPassing.TrySendMessage(_safety.DangerousGetHandle().ToInt32(),[1],out _);
    public void Dispose() { _safety.Dispose(); _ownerSafety.Dispose(); }
}
