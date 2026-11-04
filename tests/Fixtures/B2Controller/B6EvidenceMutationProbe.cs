using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

// Adversarial wire sender only, incapable of constructing installed authority.
internal static class B6EvidenceMutationProbe
{
    [DllImport("libc",EntryPoint="pipe2")] private static extern int Pipe(int[] fds,int flags);
    [DllImport("libc",EntryPoint="write")] private static extern nint Write(int fd,byte[] bytes,nuint count);
    [DllImport("libc",EntryPoint="read",SetLastError=true)] private static extern nint Read(int fd,byte[] bytes,nuint count);
    internal static int Run(string[] args)
    {
        try
        {
            string mode=args[1][5..];Guid session=Guid.Parse(args[2]),execution=Guid.Parse(args[3]),lease=Guid.Parse(args[4]);
            int parent=RecoveryInterop.GetParentPid();if(!PidFd.TryOpen(parent,out int pin,out _))return 2;
            using var retained=new SafeFileHandle((IntPtr)pin,true);long deadline=InstallationDeadline.Create();
            using var socket=B2EvidenceSocket.Connect(session,deadline,()=>B2ProcessIdentity.RequireLive(pin,parent));
            using var peer=DirectPeerIdentity.Verify(socket.Descriptor,B2ProcessIdentity.Identity(parent),pin);
            var seq=new OwnerProtocolSequencer(execution,lease);
            byte[] challenge=socket.Receive(seq,OwnerProtocolMessageType.B2OwnerExpectation,B2ProcessIdentity.Identity(parent),pin,deadline,()=>{});
            if(InstallationWire.ReadExpectation(challenge,session)!=B6EvidenceProbe.Synthetic)return 3;
            byte[] payload=InstallationWire.Echo(B6EvidenceProbe.Synthetic,1);
            if(mode=="qualified")payload[71]=0;
            if(mode=="tail77")payload=[..payload,0];
            if(mode=="tail-profile")payload=[..payload,1,(byte)'x',1,(byte)'1'];
            byte[] frame=seq.Create(OwnerProtocolMessageType.OwnerInstallationEcho,payload);
            if(mode=="27")frame[6]=27;if(mode=="32")frame[6]=32;if(mode=="38")frame[6]=38;if(mode=="unknown")frame[6]=255;
            if(mode=="wrong37")frame=new OwnerProtocolSequencer(execution,lease).Create(OwnerProtocolMessageType.B2OwnerExpectation,InstallationWire.Expectation(session,B6EvidenceProbe.Synthetic));
            if(mode=="truncated-header")frame=frame[..47];if(mode=="truncated-echo")frame=frame[..^1];
            if(mode=="declared-length")frame[44]--;
            if(mode=="reserved")frame[7]=1;
            if(mode=="magic")frame[0]^=0x20;
            int[] pipe=[-1,-1];
            try
            {
                if(mode is "sentinel" or "empty-sentinel" or "overflow-rights")
                {
                    if(Pipe(pipe,0x80000|0x800)!=0 || Write(pipe[1],[17,34],2)!=2)return 6;
                    Console.WriteLine("PIPE="+new FileInfo("/proc/self/fd/"+pipe[1]).LinkTarget);
                    int[] rights=Enumerable.Repeat(pipe[1],mode=="overflow-rights"?253:1).ToArray();
                    if(!FdPassing.TrySendTransfer(socket.Descriptor,rights,mode=="empty-sentinel"?[]:frame,out _))return 6;
                }
                else if(!FdPassing.TrySendMessage(socket.Descriptor,frame,out _))return 6;
                if(mode=="surplus-empty" && DirectUnix.Send(socket.Descriptor,IntPtr.Zero,UIntPtr.Zero,0x4000)!=0)return 6;
                if(DirectUnix.Shutdown(socket.Descriptor,1)!=0)return 6;
                Console.WriteLine("SENT");Console.Out.Flush();Console.ReadLine();
                if(pipe[0]>=0)
                {
                    var bytes=new byte[4];if(Read(pipe[0],bytes,4)!=2 || bytes[0]!=17 || bytes[1]!=34)return 7;
                    if(Read(pipe[0],bytes,4)!=-1 || Marshal.GetLastPInvokeError()!=11)return 7;
                    Console.WriteLine("SENTINEL-UNCHANGED");
                }
            }
            finally{foreach(int fd in pipe)if(fd>=0)PreparationInterop.CloseFd(fd);}
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e.GetType().Name+": "+e.Message);return 4;}
    }
}
