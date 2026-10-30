using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Native;
using Microsoft.Win32.SafeHandles;

internal static class DirectAuthProbe
{
    // Primitive qualification ONLY; no transaction, capability, cgroup or RELEASE exists.
    internal static int Run()
    {
        try
        {
            if (!RecoveryInterop.IsUnprivileged) return 2;
            var line = Console.ReadLine();
            if (line is null || line.Length > 4096) return 3;
            var association = JsonSerializer.Deserialize<DirectAssociation>(line)!;
            if (association.Controller.Pid != RecoveryInterop.GetParentPid()) return 4;
            if (!PidFd.TryOpen(association.Controller.Pid, out var fd, out _)) return 5;
            using var controller = new SafeFileHandle((IntPtr)fd, true);
            using var listener = DirectOwnerListener.Create("/tmp");
            Console.WriteLine(listener.Address); Console.Out.Flush();
            var deadline = DirectStream.Deadline(2000);
            for (var attempt = 0; attempt < DirectOwnerListener.MaxAttempts; attempt++)
            {
                var stream = listener.Accept(deadline);
                try {using var checkedPeer=DirectPeerIdentity.Verify(stream.Descriptor,association.Controller,fd);}
                catch(IOException) {stream.Dispose();continue;}
                {
                    using var session = DirectSession.Establish(stream, association, fd, owner: true, deadline);
                    listener.SealAssociation();
                    uint peerLength=12;
                    if(DirectUnix.PeerCredentials(session.Descriptor,1,17,out var actual,ref peerLength)!=0) return 9;
                    if(Environment.GetEnvironmentVariable("GAGAMBA_INT01_EVIDENCE") is {Length:>0} evidence)
                        File.WriteAllText(Path.Combine(evidence,"direct-owner-auth.json"),JsonSerializer.Serialize(new {
                            Scope="Authentication primitive only; no integrated B3 topology",Association=association,
                            ActualPeerPid=actual.Pid,ActualPeerUid=actual.Uid,ActualPeerGid=actual.Gid,
                            SocketDerivedPidfd=session.PeerProcessDescriptor,
                            SocketPidfdCloexec=Gagamba.Execution.Linux.M1.Preparation.Native.PreparationInterop.HasCloexec(session.PeerProcessDescriptor),
                            ExpectedPidfdLive=DirectPeerIdentity.LiveReference(fd,association.Controller)}));
                    if (!session.TryReceiveControl(2000, out var bytes) || bytes.Length != 1 || bytes[0] != 42) return 6;
                    session.SendControl([43]);
                    Console.WriteLine("ESTABLISHED"); Console.Out.Flush(); return 0;
                }
            }
            return 7;
        }
        catch { return 8; }
    }
}
