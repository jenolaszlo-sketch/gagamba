using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Native;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installation;

public sealed class InstallationCompletionTests
{
    private static RecoverySnapshot Clean=>new(102,103,101,102,true,false,false,0,null,null,false);
    private static byte[] Bytes(JsonNode node)=>Encoding.UTF8.GetBytes(node.ToJsonString());
    [Theory]
    [InlineData("RecoveryPid","104")][InlineData("OwnerPid","104")][InlineData("ControllerPid","104")]
    [InlineData("SessionId","103")][InlineData("Subreaper","false")][InlineData("ControllerLost","true")]
    [InlineData("OwnerExited","true")][InlineData("StopCount","1")][InlineData("Cause","\"stop\"")]
    [InlineData("StopDeadline","1")][InlineData("OwnerAcknowledged","true")][InlineData("ReleaseAuthority","true")]
    [InlineData("NativeStop","\"Confirmed\"")][InlineData("RetainedDomains","1")][InlineData("DomainRemoved","true")]
    [InlineData("OwnerReaped","true")][InlineData("WriterFence","true")][InlineData("StagingRemoved","true")]
    [InlineData("JournalFinalized","true")][InlineData("DomainEmpty","true")]
    public void EveryFinalReadyProofIsIndependentlyRequired(string key,string value)
    {
        var clean=JsonSerializer.SerializeToUtf8Bytes(Clean);
        Assert.Equal(Clean,InstallationWire.Ready(clean,InstallationWireTests.Binding));
        var mutated=JsonNode.Parse(clean)!;mutated[key]=JsonNode.Parse(value);
        Assert.Throws<InvalidDataException>(()=>InstallationWire.Ready(Bytes(mutated),InstallationWireTests.Binding));
        var barrier=new B2InstallationBarrier(InstallationWireTests.Identity,InstallationWireTests.Session,101,102,7000000001);
        barrier.Recovery(InstallationWire.Echo(InstallationWireTests.Identity,0),1);
        barrier.Bound(InstallationWireTests.Binding,2);barrier.Owner(InstallationWire.Echo(InstallationWireTests.Identity,1),3);_=barrier.Confirmation(4);
        Assert.Throws<InvalidDataException>(()=>barrier.Ready(Bytes(mutated),5));
        Assert.False(barrier.Qualified);Assert.Throws<InvalidDataException>(()=>barrier.Commit(6));
    }
    [Fact] public void ReadyRejectsEveryMissingPropertyWrongNativeTypeDuplicateAndTail()
    {
        string text=JsonSerializer.Serialize(Clean);var original=JsonNode.Parse(text)!.AsObject();
        foreach(var pair in original.ToArray())
        {
            var missing=JsonNode.Parse(text)!.AsObject();missing.Remove(pair.Key);
            Assert.ThrowsAny<Exception>(()=>InstallationWire.Ready(Bytes(missing),InstallationWireTests.Binding));
            var wrong=JsonNode.Parse(text)!;wrong[pair.Key]=new JsonArray();
            Assert.ThrowsAny<Exception>(()=>InstallationWire.Ready(Bytes(wrong),InstallationWireTests.Binding));
            string duplicate=text[..^1]+",\""+pair.Key+"\":"+(pair.Value?.ToJsonString()??"null")+"}";
            Assert.ThrowsAny<Exception>(()=>InstallationWire.Ready(Encoding.UTF8.GetBytes(duplicate),InstallationWireTests.Binding));
        }
        foreach(string bad in new[]{text+"{}",text[..^1]+",\"Unknown\":false}",text.Replace("\"StopCount\":0","\"StopCount\":-0")})
            Assert.ThrowsAny<Exception>(()=>InstallationWire.Ready(Encoding.UTF8.GetBytes(bad),InstallationWireTests.Binding));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void RepeatedConfirmationCannotReachReadyOrRenewOriginalObligation(bool freshSequence)
    {
        var wire=new RecoveryWire(InstallationWireTests.Execution,InstallationWireTests.Lease,2,false,true);
        var receive=new RecoveryWire(InstallationWireTests.Execution,InstallationWireTests.Lease,2,false,false);
        Assert.True(receive.Accept(wire.Create(RecoveryMessage.Associate,new byte[48]),out _,out _));
        Assert.True(receive.Accept(wire.Create(RecoveryMessage.InstallationEchoResponse,InstallationWire.Response(InstallationWireTests.Identity,InstallationMismatch.None)),out _,out _));
        byte[] payload=InstallationWire.Confirmation(InstallationWireTests.Session,InstallationWireTests.Identity);
        byte[] first=wire.Create(RecoveryMessage.B2OwnerEchoAccepted,payload);
        var barrier=new B2RecoveryReadyBarrier(InstallationWireTests.Binding,InstallationWireTests.Identity);barrier.Lifeline(1);barrier.ReferencesSent(2);
        Assert.True(receive.Accept(first,out _,out var accepted));barrier.Confirm(accepted,3);
        byte[] duplicate=freshSequence?wire.Create(RecoveryMessage.B2OwnerEchoAccepted,payload):first;
        if(receive.Accept(duplicate,out _,out accepted))Assert.Throws<InvalidDataException>(()=>barrier.Confirm(accepted,4));
        else barrier.Stop("Sequence");
        Assert.Throws<InvalidDataException>(()=>barrier.Ready(JsonSerializer.SerializeToUtf8Bytes(Clean),5));
    }
    [Fact] public void ActiveSelectionCannotBeConstructedFromAValidatedWireIdentity()
    {
        InstallationWireTests.Identity.Validate();
        Assert.Empty(typeof(LoadedInstallation).GetConstructors(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance));
        var fresh=InstalledResult.Unqualified("synthetic packet is not S2");Assert.Null(fresh.Identity);Assert.Null(fresh.Manifest);
        Assert.False(new RecoveryObligation().ReleaseAuthority);
    }
}
