using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installation;

[Collection("S3NativeProcessState")]
public sealed class InstallationEvidenceEdgeTests
{
    [DllImport("libc",EntryPoint="setsockopt",SetLastError=true)]private static extern int Option(int fd,int level,int name,ref int value,uint length);
    [Theory]
    [InlineData("27")][InlineData("32")][InlineData("38")][InlineData("unknown")][InlineData("wrong37")]
    [InlineData("truncated-header")][InlineData("truncated-echo")][InlineData("declared-length")][InlineData("reserved")]
    [InlineData("qualified")][InlineData("tail77")][InlineData("tail-profile")][InlineData("magic")]
    [InlineData("sentinel")][InlineData("empty-sentinel")][InlineData("overflow-rights")][InlineData("surplus-empty")]
    [InlineData("missing-credentials")][InlineData("unknown-timestamp-cmsg")]
    public void NativeMutationRefusesAgainstItsOwnPositiveControl(string mutation)
    {
        if(!OperatingSystem.IsLinux())Assert.Skip("non-root Linux native ancillary/frame tests required");
        string? fixture=Environment.GetEnvironmentVariable("GAGAMBA_S3_EVIDENCE_FIXTURE");
        if(fixture is null)Assert.Skip("explicit isolated S3 fixture required");
        Assert.True(RecoveryInterop.IsUnprivileged);
        Exchange(fixture,"valid",true);Exchange(fixture,mutation,false);
    }
    private static void Exchange(string fixture,string mode,bool positive)
    {
        var session=Guid.NewGuid();var execution=Guid.NewGuid();var lease=Guid.NewGuid();using var listener=B2EvidenceSocket.Listen(session);
        var start=new ProcessStartInfo("/usr/lib/dotnet/dotnet"){UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
        string senderMode=mode is "missing-credentials" or "unknown-timestamp-cmsg" or "valid"?"valid":"edge-"+mode;
        foreach(string arg in new[]{fixture,"--b6-evidence",senderMode,session.ToString(),execution.ToString(),lease.ToString()})start.ArgumentList.Add(arg);
        using var child=Process.Start(start)!;Assert.True(PidFd.TryOpen(child.Id,out int fd,out _));using var pin=new SafeFileHandle((IntPtr)fd,true);
        bool accepted=false;string? refusal=null;
        try
        {
            long deadline=InstallationDeadline.Create();using var socket=listener.Accept(deadline,()=>{});
            using var peer=DirectPeerIdentity.Verify(socket.Descriptor,B2ProcessIdentity.Identity(child.Id),fd);
            if(mode=="missing-credentials"){int zero=0;Assert.Equal(0,Option(socket.Descriptor,1,16,ref zero,4));}
            if(mode=="unknown-timestamp-cmsg"){int one=1;Assert.Equal(0,Option(socket.Descriptor,1,35,ref one,4));}
            var sequence=new OwnerProtocolSequencer(execution,lease);
            socket.Send(sequence,OwnerProtocolMessageType.B2OwnerExpectation,InstallationWire.Expectation(session,InstallationWireTests.Identity),deadline,()=>{});
            var echo=socket.Receive(sequence,OwnerProtocolMessageType.OwnerInstallationEcho,B2ProcessIdentity.Identity(child.Id),fd,deadline,()=>{});
            accepted=InstallationWire.CompareEcho(echo,InstallationWireTests.Identity,1)==InstallationMismatch.None;
        }
        catch(IOException e){refusal=e.Message;}
        catch(InvalidDataException e){refusal=e.Message;}
        finally
        {
            try{child.StandardInput.WriteLine("done");child.StandardInput.Flush();}catch(IOException){}
            if(!child.WaitForExit(5000)){child.Kill();Assert.True(child.WaitForExit(3000));}
        }
        string output=child.StandardOutput.ReadToEnd();Assert.True(child.ExitCode==0,child.StandardError.ReadToEnd());Assert.Equal(positive,accepted);
        Assert.Throws<IOException>(()=>{using var stale=B2EvidenceSocket.Connect(session,InstallationDeadline.Create(),()=>{});});
        if(mode is "sentinel" or "empty-sentinel" or "overflow-rights")
        {
            string target=output.Split('\n').Single(s=>s.StartsWith("PIPE=",StringComparison.Ordinal))[5..].Trim();
            Assert.Contains("SENTINEL-UNCHANGED",output);Assert.DoesNotContain(Directory.GetFiles("/proc/self/fd"),p=>new FileInfo(p).LinkTarget==target);
        }
        if(mode=="unknown-timestamp-cmsg")Assert.Contains("ancillary",refusal!);
        if(mode=="missing-credentials")Assert.Contains("actual evidence sender",refusal!);
        if(Environment.GetEnvironmentVariable("GAGAMBA_S3_OBSERVATIONS") is {Length:>0} directory)
        {
            Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"edge-"+mode+"-"+Guid.NewGuid().ToString("N")+".json"),JsonSerializer.Serialize(new{Scope="native mechanism only; no installed identity authority",Mode=mode,Accepted=accepted,Refusal=refusal,ChildPid=child.Id,ExitCode=child.ExitCode,SentinelUnchanged=output.Contains("SENTINEL-UNCHANGED",StringComparison.Ordinal)}));
        }
    }
}
