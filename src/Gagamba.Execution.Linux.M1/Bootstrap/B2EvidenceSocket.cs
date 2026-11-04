using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// This channel has only expectation 37 and echo 36. No lifecycle dispatch or rights sender.
internal sealed class B2EvidenceSocket : IDisposable
{
    private readonly SafeFileHandle _handle;
    internal int Descriptor=>_handle.DangerousGetHandle().ToInt32();
    private B2EvidenceSocket(int fd) {_handle=new((IntPtr)fd,true);}
    [DllImport("libc",EntryPoint="setsockopt",SetLastError=true)] private static extern int SetOption(int fd,int level,int option,ref int value,uint length);
    internal static byte[] Address(Guid session)
    {
        if(session==Guid.Empty) throw new InvalidDataException("B2 session missing");
        var name=System.Text.Encoding.ASCII.GetBytes("gagamba.b6.b2."+session.ToString("N"));
        var p=new byte[3+name.Length];p[0]=1;name.CopyTo(p,3);return p;
    }
    private static B2EvidenceSocket Create()
    {
        int fd=DirectUnix.Socket(1,5|DirectUnix.Cloexec|DirectUnix.Nonblock,0);
        if(fd<0) throw new IOException("B2 evidence socket unavailable");return new(fd);
    }
    private void PassCred()
    {int value=1;if(SetOption(Descriptor,1,16,ref value,4)!=0) throw new IOException("SO_PASSCRED unavailable");}
    internal static B2EvidenceSocket Listen(Guid session)
    {
        var socket=Create();try
        {
            var address=Address(session);
            if(DirectUnix.Bind(socket.Descriptor,address,(uint)address.Length)!=0) throw new IOException("B2 bind refused");
            socket.PassCred();if(DirectUnix.Listen(socket.Descriptor,1)!=0) throw new IOException("B2 listen refused");return socket;
        } catch {socket.Dispose();throw;}
    }
    internal B2EvidenceSocket Accept(long deadline,Action service)
    {
        try
        {
            while(true)
            {
                service();_=InstallationDeadline.Remaining(deadline);
                int fd=DirectUnix.Accept(Descriptor,IntPtr.Zero,IntPtr.Zero,DirectUnix.Cloexec|DirectUnix.Nonblock);
                if(fd>=0) {var socket=new B2EvidenceSocket(fd);try{socket.PassCred();return socket;}catch{socket.Dispose();throw;}}
                int error=Marshal.GetLastPInvokeError();if(error is not (4 or 11)) throw new IOException("B2 accept refused");Thread.Sleep(2);
            }
        }
        finally {Dispose();} // first connector consumes the only listener, even on refusal
    }
    internal static B2EvidenceSocket Connect(Guid session,long deadline,Action service)
    {
        var socket=Create();try
        {
            socket.PassCred();service();_=InstallationDeadline.Remaining(deadline);
            var address=Address(session);
            if(DirectUnix.Connect(socket.Descriptor,address,(uint)address.Length)!=0) throw new IOException("B2 connect refused");
            return socket;
        } catch {socket.Dispose();throw;}
    }
    internal void Send(OwnerProtocolSequencer wire,OwnerProtocolMessageType type,byte[] payload,long deadline,Action service)
    {
        if(type is not (OwnerProtocolMessageType.B2OwnerExpectation or OwnerProtocolMessageType.OwnerInstallationEcho))
            throw new IOException("B2 evidence direction/type");
        service();_=InstallationDeadline.Remaining(deadline);
        if(type==OwnerProtocolMessageType.B2OwnerExpectation)
        {
            var pending=new DirectUnix.PollFd{Fd=Descriptor,Events=1|0x2000};
            if(DirectUnix.Poll(ref pending,(UIntPtr)1,0)<0 || pending.Revents!=0)
                throw new IOException("premature B2 evidence record");
        }
        if(!FdPassing.TrySendMessage(Descriptor,wire.Create(type,payload),out _)) throw new IOException("B2 evidence send failed");
        if(DirectUnix.Shutdown(Descriptor,1)!=0) throw new IOException("B2 evidence half-close failed");
    }
    internal byte[] Receive(OwnerProtocolSequencer wire,OwnerProtocolMessageType type,DirectProcessIdentity sender,
        int pin,long deadline,Action service)
    {
        if(type is not (OwnerProtocolMessageType.B2OwnerExpectation or OwnerProtocolMessageType.OwnerInstallationEcho)) throw new IOException("evidence receive role");
        var frame=Record(sender,pin,deadline,service,false) ?? throw new IOException("missing evidence frame");
        if(!wire.TryAccept(frame,type,out var message,out _) || message!.Payload.Length>512) throw new IOException("B2 evidence frame refused");
        // Drain to an actual peer half-close. A second/empty record is terminal.
        if(Record(sender,pin,deadline,service,true) is not null) throw new IOException("surplus evidence record");
        return message.Payload;
    }
    private byte[]? Record(DirectProcessIdentity sender,int pin,long deadline,Action service,bool drain)
    {
        var bytes=new byte[561];var control=new byte[4096];
        var dataPin=GCHandle.Alloc(bytes,GCHandleType.Pinned);var ctrlPin=GCHandle.Alloc(control,GCHandleType.Pinned);
        var iov=Marshal.AllocHGlobal(16);
        try
        {
            Marshal.StructureToPtr(new DirectUnix.Iovec{Base=dataPin.AddrOfPinnedObject(),Length=(UIntPtr)bytes.Length},iov,false);
            while(true)
            {
                service();_=InstallationDeadline.Remaining(deadline);
                if(!DirectPeerIdentity.LiveReference(pin,sender)) throw new IOException("evidence sender lost");
                var poll=new DirectUnix.PollFd{Fd=Descriptor,Events=1|0x2000};
                if(DirectUnix.Poll(ref poll,(UIntPtr)1,0)<0) {if(Marshal.GetLastPInvokeError()==4) continue;throw new IOException("evidence poll");}
                var msg=new DirectUnix.Message{Vectors=iov,VectorCount=(UIntPtr)1,Control=ctrlPin.AddrOfPinnedObject(),ControlLength=(UIntPtr)control.Length};
                long rc=DirectUnix.Receive(Descriptor,ref msg,0x40000000|0x40 /* CMSG_CLOEXEC, DONTWAIT */);
                if(rc<0) {if(Marshal.GetLastPInvokeError() is 4 or 11) {Thread.Sleep(2);continue;}throw new IOException("evidence recvmsg");}
                var rights=new List<int>();bool ancillary=true;int credentials=0;RecoveryInterop.Peer actual=default;
                try
                {
                    int count=checked((int)Math.Min((ulong)msg.ControlLength,(ulong)control.Length));
                    int offset=0;
                    while(offset<count)
                    {
                        int remaining=count-offset;if(remaining<16) {ancillary=false;break;}
                        long length=Marshal.ReadInt64(msg.Control,offset);int level=Marshal.ReadInt32(msg.Control,offset+8),kind=Marshal.ReadInt32(msg.Control,offset+12);
                        if(length<16) {ancillary=false;break;}
                        int available=(int)Math.Min(length-16,remaining-16);
                        if(level==1 && kind==1)
                        {for(int i=0;i+4<=available;i+=4) rights.Add(Marshal.ReadInt32(msg.Control,offset+16+i));ancillary=false;}
                        else if(level==1 && kind==2 && length==28 && available==12)
                        {credentials++;actual=Marshal.PtrToStructure<RecoveryInterop.Peer>(msg.Control+offset+16);}
                        else ancillary=false;
                        if(length>remaining) {ancillary=false;break;}
                        if(length==remaining) {offset=count;break;}
                        long aligned=(length+7)&~7L;if(aligned>remaining) {ancillary=false;break;}offset+=checked((int)aligned);
                    }
                    if((msg.Flags&(0x8|0x20))!=0 || (ulong)msg.ControlLength>(ulong)control.Length || rc>560 || !ancillary || rights.Count!=0)
                        throw new IOException("evidence ancillary/truncation refused");
                    if(rc==0 && count==0 && credentials==0 && drain && (poll.Revents&0x2000)!=0 && DirectPeerIdentity.LiveReference(pin,sender)) return null;
                    if(rc<=0 || credentials!=1 || actual.Pid!=sender.Pid || actual.Uid!=sender.Uid || actual.Gid!=sender.Gid ||
                        !DirectPeerIdentity.LiveReference(pin,sender)) throw new IOException("actual evidence sender refused");
                    return bytes.AsSpan(0,(int)rc).ToArray();
                }
                finally {foreach(int fd in rights) if(fd>=0) PreparationInterop.CloseFd(fd);}
            }
        }
        finally {Marshal.FreeHGlobal(iov);dataPin.Free();ctrlPin.Free();}
    }
    public void Dispose()=>_handle.Dispose();
}
