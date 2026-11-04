using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Native;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installation;

public sealed class InstallationHandshakeTests
{
    private static B2InstallationBarrier Controller()=>new(InstallationWireTests.Identity,InstallationWireTests.Session,101,102,7000000001);
    private static B2RecoveryReadyBarrier Recovery()=>new(InstallationWireTests.Binding,InstallationWireTests.Identity);
    private static byte[] Snapshot()=>JsonSerializer.SerializeToUtf8Bytes(new RecoverySnapshot(102,103,101,102,true,false,false,0,null,null,false));
    private static byte[] Echo(byte role)=>InstallationWire.Echo(InstallationWireTests.Identity,role);
    private static void ToOwner(B2InstallationBarrier c)
    {c.Recovery(Echo(0),1);c.Bound(InstallationWireTests.Binding,2);}
    [Fact] public void PositiveTraceRequiresBothIdentityChecksConfirmationAndCleanReady()
    {
        var c=Controller();var r=Recovery();
        c.Recovery(Echo(0),1);Assert.False(c.Qualified);
        r.Lifeline(2);r.ReferencesSent(3);c.Bound(InstallationWireTests.Binding,3);
        c.Owner(Echo(1),4);Assert.False(c.Qualified);
        var confirmation=c.Confirmation(5);r.Confirm(confirmation,5);r.Ready(Snapshot(),6);
        c.Ready(Snapshot(),6);c.Commit(7);Assert.True(c.Qualified);
    }
    [Theory][InlineData(0)][InlineData(1)] public void T20bMismatchNeverQualifies(byte role)
    {
        var c=Controller();var p=Echo(role);p[32]^=1;
        if(role==1) ToOwner(c);
        Assert.Throws<InvalidDataException>(()=>{if(role==0)c.Recovery(p,1);else c.Owner(p,3);});
        Assert.False(c.Qualified);Assert.Throws<InvalidDataException>(()=>c.Confirmation(4));
    }
    [Theory][InlineData("ControllerDeath")][InlineData("RecoveryDeath")][InlineData("OwnerDeath")]
    [InlineData("ObservedStop")][InlineData("TransportLost")][InlineData("ClosureMismatch")]
    public void FirstTerminalEventPreemptsEveryPositivePhase(string cause)
    {
        for(int phase=0;phase<=5;phase++)
        {
            var c=Controller();
            if(phase>=1)c.Recovery(Echo(0),1);if(phase>=2)c.Bound(InstallationWireTests.Binding,2);
            if(phase>=3)c.Owner(Echo(1),3);if(phase>=4)_=c.Confirmation(4);if(phase>=5)c.Ready(Snapshot(),5);
            c.Stop(cause);c.Stop("LaterCause");
            Assert.False(c.Qualified);Assert.Equal(cause,c.FirstFailure);
            Assert.Throws<InvalidDataException>(()=>c.Commit(6));Assert.Throws<InvalidDataException>(()=>c.Recovery(Echo(0),6));
        }
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)]
    public void ReplayedCurrentOrPriorSessionBindingAndEchoCannotResynchronize(int phase)
    {
        var c=Controller();c.Recovery(Echo(0),1);
        if(phase==0) Assert.Throws<InvalidDataException>(()=>c.Recovery(Echo(0),2));
        else if(phase==1) Assert.Throws<InvalidDataException>(()=>c.Bound(InstallationWireTests.Binding with{Session=Guid.NewGuid()},2));
        else
        {
            c.Bound(InstallationWireTests.Binding,2);
            if(phase==2) Assert.Throws<InvalidDataException>(()=>c.Bound(InstallationWireTests.Binding,3));
            else {c.Owner(Echo(1),3);Assert.Throws<InvalidDataException>(()=>c.Owner(Echo(1),4));}
        }
        Assert.False(c.Qualified);Assert.Throws<InvalidDataException>(()=>c.Confirmation(5));
    }
    [Theory][InlineData(false)][InlineData(true)] public void MissingConfirmationOrReadyCannotComplete(bool confirmation)
    {
        var c=Controller();var r=Recovery();ToOwner(c);c.Owner(Echo(1),3);r.Lifeline(2);r.ReferencesSent(3);
        if(confirmation) {r.Confirm(c.Confirmation(4),4);Assert.Throws<InvalidDataException>(()=>c.Commit(5));}
        else Assert.Throws<InvalidDataException>(()=>r.Ready(Snapshot(),5));
        Assert.False(c.Qualified);
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)] public void RecoveryRequiresActualLifelineAndBothTransfers(int missing)
    {
        var r=Recovery();
        if(missing>=1)r.Lifeline(1);if(missing>=2)r.ReferencesSent(2);
        if(missing==0) Assert.Throws<InvalidDataException>(()=>r.ReferencesSent(2));
        else if(missing==1) Assert.Throws<InvalidDataException>(()=>r.Confirm(InstallationWire.Confirmation(InstallationWireTests.Session,InstallationWireTests.Identity),3));
        else if(missing==2) Assert.Throws<InvalidDataException>(()=>r.Ready(Snapshot(),3));
        else
        {r.Stop("ObservedStop");Assert.Throws<InvalidDataException>(()=>r.Confirm(InstallationWire.Confirmation(InstallationWireTests.Session,InstallationWireTests.Identity),4));}
    }
    [Fact] public void DelayedConfirmationAndFinalReturnUseOriginalAbsoluteDeadline()
    {
        var c=Controller();ToOwner(c);c.Owner(Echo(1),3);
        Assert.Throws<InvalidDataException>(()=>c.Confirmation(7000000001));Assert.False(c.Qualified);
        var r=Recovery();r.Lifeline(1);r.ReferencesSent(2);
        Assert.Throws<InvalidDataException>(()=>r.Confirm(InstallationWire.Confirmation(InstallationWireTests.Session,InstallationWireTests.Identity),7000000001));
    }
    [Fact] public void StopPreservesOriginalC3ObligationAndCannotCreateReleaseAuthority()
    {
        var r=Recovery();r.Lifeline(1);var stop=new RecoveryObligation();Assert.True(stop.Request("FirstLoss",100));long? deadline=stop.Deadline;
        r.Stop("FirstLoss");Assert.False(stop.Request("DelayedEcho",100000));Assert.Equal(deadline,stop.Deadline);
        Assert.False(stop.ReleaseAuthority);Assert.False(stop.OwnerAcknowledged);
        Assert.Throws<InvalidDataException>(()=>r.Ready(Snapshot(),3));
    }
}
