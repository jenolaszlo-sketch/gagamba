using System.Diagnostics;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

// Explicit synthetic identity fixture. This executable has no production installation,
// qualification publisher, journal, domain, capability, physical writer or RELEASE path.
internal static class B6EvidenceProbe
{
    internal static InstallationIdentity Synthetic => new(new('1',64),new('2',64),new('3',64),new('4',64),new('5',64),new('6',64),
        1,1,1,1,"ae4-linux-fixed-fixture-v1","1",new('1',64),true);
    internal static int Run(string[] args)
    {
        try
        {
            if(args[1].StartsWith("edge-",StringComparison.Ordinal))return B6EvidenceMutationProbe.Run(args);
            string mode=args[1];Guid session=Guid.Parse(args[2]),execution=Guid.Parse(args[3]),lease=Guid.Parse(args[4]);
            if(mode=="writer")
            {
                byte[] bytes=InstallationWire.Echo(Synthetic,1);
                var frame=new OwnerProtocolSequencer(execution,lease).Create(OwnerProtocolMessageType.OwnerInstallationEcho,bytes);
                if(!FdPassing.TrySendMessage(3,frame,out _)) return 5;DirectUnix.Shutdown(3,1);Thread.Sleep(100);return 0;
            }
            int parent=RecoveryInterop.GetParentPid();
            if(!RecoveryInterop.IsUnprivileged || !PidFd.TryOpen(parent,out int pin,out _)) return 2;
            using var parentPin=new SafeFileHandle((IntPtr)pin,true);
            long deadline=InstallationDeadline.Create();
            using var socket=B2EvidenceSocket.Connect(session,deadline,()=>B2ProcessIdentity.RequireLive(pin,parent));
            using var checkedPeer=DirectPeerIdentity.Verify(socket.Descriptor,B2ProcessIdentity.Identity(parent),pin);
            var seq=new OwnerProtocolSequencer(execution,lease);
            if(mode=="premature")
            {
                socket.Send(seq,OwnerProtocolMessageType.OwnerInstallationEcho,InstallationWire.Echo(Synthetic,1),deadline,()=>{});
                Console.WriteLine("SENT");Console.Out.Flush();Console.ReadLine();return 0;
            }
            var expected=socket.Receive(seq,OwnerProtocolMessageType.B2OwnerExpectation,B2ProcessIdentity.Identity(parent),pin,deadline,()=>{});
            if(InstallationWire.ReadExpectation(expected,session)!=Synthetic) return 3;
            byte[] p=InstallationWire.Echo(Synthetic,1);
            if(mode=="valid") socket.Send(seq,OwnerProtocolMessageType.OwnerInstallationEcho,p,deadline,()=>{});
            else
            {
                if(mode=="wrong-digest") p[0]^=1;
                if(mode=="wrong-closure") p[32]^=1;
                if(mode=="wrong-role") p[70]=0;
                if(mode=="wrong-execution") execution=Guid.NewGuid();
                if(mode=="wrong-lease") lease=Guid.NewGuid();
                if(mode=="missing") {Thread.Sleep(2500);return 0;}
                if(mode=="impostor-writer")
                {
                    // Only a fixture intentionally gives its connected socket to a different writer.
                    // The live SO_PEERPIDFD remains this process while actual SCM_CREDENTIALS changes.
                    var runtime=RuntimePin();
                    using var writer=QualifiedLauncher.Start(runtime,System.Reflection.Assembly.GetExecutingAssembly().Location,
                        ["--b6-evidence","writer",session.ToString(),execution.ToString(),lease.ToString()],(socket.Descriptor,3,"GAGAMBA_B6_FIXTURE_FD"));
                    Console.WriteLine("SENT");Console.Out.Flush();Console.ReadLine();return QualifiedLauncher.TryReap(writer,5000,out var code)&&code==0?0:7;
                }
                var raw=new OwnerProtocolSequencer(execution,lease).Create(
                    mode=="release"?OwnerProtocolMessageType.ReleaseRequest:OwnerProtocolMessageType.OwnerInstallationEcho,p);
                if(mode=="oversize") raw=new OwnerProtocolSequencer(execution,lease).Create(OwnerProtocolMessageType.OwnerInstallationEcho,new byte[513]);
                if(mode=="rights" || mode=="empty-rights")
                {
                    if(PreparationInterop.OpenDirectory("/tmp",out int fd)!=0) return 6;
                    try {if(!FdPassing.TrySendTransfer(socket.Descriptor,[fd],mode=="empty-rights"?[]:raw,out _)) return 6;}finally{PreparationInterop.CloseFd(fd);}
                }
                else if(mode=="empty") {if(DirectUnix.Send(socket.Descriptor,IntPtr.Zero,UIntPtr.Zero,0x4000)!=0) return 6;}
                else
                {
                    if(!FdPassing.TrySendMessage(socket.Descriptor,raw,out _)) return 6;
                    if(mode=="duplicate" && !FdPassing.TrySendMessage(socket.Descriptor,raw,out _)) return 6;
                }
                DirectUnix.Shutdown(socket.Descriptor,1);
            }
            // Keep the actual sender live until the controller completes its bracketed observations.
            Console.WriteLine("SENT");Console.Out.Flush();Console.ReadLine();return 0;
        }
        catch(Exception e) {Console.Error.WriteLine(e.GetType().Name+": "+e.Message);return 4;}
    }
    internal static ExperimentalRuntimePin RuntimePin()
    {
        string version=Environment.Version.ToString(),host="/usr/lib/dotnet/dotnet",shared="/usr/lib/dotnet/shared/Microsoft.NETCore.App/"+version,fxr="/usr/lib/dotnet/host/fxr/"+version;
        using var stream=File.OpenRead(host);
        return new(host,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)),shared,
            ExperimentalRuntimePin.Inventory(shared),fxr,ExperimentalRuntimePin.Inventory(fxr),version);
    }
}
