using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal static class QualifiedOwnerPreflight
{
    // The original recovery bootstrap already carries the S2 expectation. Verify the
    // actual role before taking the writer, without reinterpreting WriterReady=2.
    // DirectBootstrap/DirectSession subsequently authenticate C before echo 36.
    internal static void Verify(OwnerPreparationSpecification request,OwnerProtocolChannel recovery)
    {
        var identity=request.DirectIdentity!;identity.Recovery.Validate();
        if(identity.Recovery.Pid!=RecoveryInterop.GetParentPid() ||
            !RecoveryInterop.VerifyPeer(recovery.Socket,identity.Recovery.Pid,identity.Recovery.Uid) ||
            !PidFd.TryOpen(identity.Recovery.Pid,out int fd,out _)) throw new IOException("original pre-writer recovery refused");
        using var original=new SafeFileHandle((IntPtr)fd,true);
        using var peer=DirectPeerIdentity.Verify(recovery.Socket,identity.Recovery,fd);
        long deadline=Math.Min(Math.Min(request.ExpiresAtNs,checked(request.AcceptedAtNs+10_000_000_000L)),
            checked(InstallationDeadline.Now+5_000_000_000L));
        void Service()
        {
            B2ProcessIdentity.RequireLive(fd,identity.Recovery.Pid);
            if(RecoveryInterop.GetParentPid()!=identity.Recovery.Pid) throw new IOException("pre-writer recovery lost");
            var poll=new DirectUnix.PollFd{Fd=recovery.Socket,Events=1|0x2000};
            if(DirectUnix.Poll(ref poll,(UIntPtr)1,0)<0 || poll.Revents!=0)
                throw new IOException("pre-writer stop or premature control");
        }
        _=InstallationDeadline.Verify(()=>LoadedInstallation.Verify("owner",identity.InstallationIdentity),deadline,Service);
    }
}
