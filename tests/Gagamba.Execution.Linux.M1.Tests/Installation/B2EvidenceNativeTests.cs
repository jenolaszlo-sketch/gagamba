using System.Diagnostics;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installation;

[Collection("S3NativeProcessState")]
public sealed class B2EvidenceNativeTests
{
    private static string Fixture()
    {
        if(!OperatingSystem.IsLinux()) Assert.Skip("non-root Linux socket/process component qualification");
        if(Environment.GetEnvironmentVariable("GAGAMBA_S3_EVIDENCE_FIXTURE") is not {Length:>0} path)
        {Assert.Skip("explicit isolated synthetic-identity probe required");return "";}
        Assert.True(RecoveryInterop.IsUnprivileged);Assert.True(DirectPeerIdentity.FeatureAvailable());return path;
    }
    [Theory]
    [InlineData("premature",false)]
    [InlineData("valid",true)][InlineData("wrong-digest",false)][InlineData("wrong-closure",false)]
    [InlineData("wrong-role",false)][InlineData("wrong-execution",false)][InlineData("wrong-lease",false)]
    [InlineData("duplicate",false)][InlineData("release",false)][InlineData("oversize",false)]
    [InlineData("rights",false)][InlineData("empty",false)][InlineData("empty-rights",false)]
    [InlineData("missing",false)][InlineData("wrong-original-pin",false)][InlineData("impostor-writer",false)]
    public async Task ActualPeerAndActualRecordAreBothRequired(string mode,bool accepted)
    {
        string fixture=Fixture();Guid session=Guid.NewGuid(),execution=Guid.NewGuid(),lease=Guid.NewGuid();
        using var listener=B2EvidenceSocket.Listen(session);
        var start=new ProcessStartInfo("/usr/lib/dotnet/dotnet"){UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(string arg in new[]{fixture,"--b6-evidence",mode=="wrong-original-pin"?"valid":mode,session.ToString(),execution.ToString(),lease.ToString()}) start.ArgumentList.Add(arg);
        using var child=Process.Start(start)!;
        Assert.True(PidFd.TryOpen(child.Id,out int childFd,out _));using var childPin=new SafeFileHandle((IntPtr)childFd,true);
        Assert.True(PidFd.TryOpen(Environment.ProcessId,out int selfFd,out _));using var selfPin=new SafeFileHandle((IntPtr)selfFd,true);
        static int NativeHandles()=>Directory.GetFiles("/proc/self/fd").Select(p=>new FileInfo(p).LinkTarget)
            .Count(t=>t is not null && (t.StartsWith("socket:",StringComparison.Ordinal) || t=="anon_inode:[pidfd]" || t=="/tmp"));
        int before=NativeHandles();
        bool success=false;string? refusal=null;
        try
        {
            long deadline=InstallationDeadline.Create();using var socket=listener.Accept(deadline,()=>{});
            using var peer=DirectPeerIdentity.Verify(socket.Descriptor,B2ProcessIdentity.Identity(child.Id),mode=="wrong-original-pin"?selfFd:childFd);
            var seq=new OwnerProtocolSequencer(execution,lease);
            if(mode=="premature") Assert.Equal("SENT",await child.StandardOutput.ReadLineAsync(TestContext.Current.CancellationToken)
                .AsTask().WaitAsync(TimeSpan.FromSeconds(5),TestContext.Current.CancellationToken));
            socket.Send(seq,OwnerProtocolMessageType.B2OwnerExpectation,InstallationWire.Expectation(session,InstallationWireTests.Identity),deadline,()=>{});
            // Shorter missing-frame bound; all other cases use the original bounded exchange.
            if(mode=="missing") deadline=InstallationDeadline.Now+100_000_000L;
            var echo=socket.Receive(seq,OwnerProtocolMessageType.OwnerInstallationEcho,B2ProcessIdentity.Identity(child.Id),childFd,deadline,()=>{});
            success=InstallationWire.CompareEcho(echo,InstallationWireTests.Identity,1)==InstallationMismatch.None;
        }
        catch(IOException e) {success=false;refusal=e.Message;}
        catch(InvalidDataException e) {success=false;refusal=e.Message;}
        finally
        {
            try{child.StandardInput.WriteLine("done");child.StandardInput.Flush();}catch(IOException){}
            if(!child.WaitForExit(5000)) {child.Kill();Assert.True(child.WaitForExit(5000));}
        }
        Assert.Equal(accepted,success);
        if(mode!="wrong-original-pin") Assert.True(child.ExitCode==0,child.StandardError.ReadToEnd());
        Assert.True(PidFd.TryExited(childFd,0,out var exited,out _));Assert.True(exited);
        // Socket and peer/received-right lifetimes ended. The listener was consumed by accept.
        int after=NativeHandles();
        Assert.Equal(before-1,after); // accepting closed exactly the one listener; every transient copy is gone
        if(Environment.GetEnvironmentVariable("GAGAMBA_S3_OBSERVATIONS") is {Length:>0} observations)
        {
            Directory.CreateDirectory(observations);
            File.WriteAllText(Path.Combine(observations,"evidence-"+mode+".json"),System.Text.Json.JsonSerializer.Serialize(new{
                Scope="Native component; synthetic artifact identity; no production authority",Mode=mode,Accepted=success,
                ControllerPid=Environment.ProcessId,OriginalChildPid=child.Id,Uid=RecoveryInterop.GetUid(),Gid=DirectUnix.GetGid(),
                SenderPinCloexec=PreparationInterop.HasCloexec(childFd),ChildExited=exited,ChildExitCode=child.ExitCode,
                SocketAndPidfdCountBefore=before,SocketAndPidfdCountAfter=after,Refusal=refusal}));
        }
    }
    [Fact] public void TypedPinReceiverRejectsOrdinaryDescriptorAndClosesItsCopy()
    {
        _=Fixture();
        Assert.Equal(0,PreparationInterop.OpenDirectory("/tmp",out int fd));
        using var record=new ReceivedFdRecord([1],[fd]);
        Assert.Throws<IOException>(()=>B2ProcessIdentity.ReceivePin(record,Environment.ProcessId));
        record.Dispose();Assert.False(PreparationInterop.HasCloexec(fd));
    }
}

[CollectionDefinition("S3NativeProcessState",DisableParallelization=true)]
public sealed class S3NativeProcessStateCollection {}
