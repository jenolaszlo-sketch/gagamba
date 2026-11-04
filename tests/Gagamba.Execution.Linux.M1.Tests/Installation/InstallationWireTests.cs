using System.Buffers.Binary;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Native;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installation;

public sealed class InstallationWireTests
{
    internal static readonly Guid Execution=Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
    internal static readonly Guid Lease=Guid.Parse("10213243-5465-7687-98a9-bacbdcedfe0f");
    internal static readonly Guid Session=Guid.Parse("ffeeddcc-bbaa-9988-7766-554433221100");
    internal static InstallationIdentity Identity => new(new('1',64),new('2',64),new('3',64),new('4',64),new('5',64),new('6',64),
        1,1,1,1,InstalledManifest.Profile,"1",new('1',64),true);
    internal static B2Binding Binding => new(Session,101,102,103,7000000001);
    [Fact] public void ProjectionUsesCanonicalRawRoleOrderAndPreservesS2Identity()
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("B6.ManifestV1")!;
        using var memory=new MemoryStream();stream.CopyTo(memory);
        var manifest=InstalledManifest.Parse(memory.ToArray());
        var identity=InstallationIdentity.Project(manifest);
        Assert.Equal(manifest.Digest,identity.Generation);
        foreach(byte role in new byte[]{0,1})
        {
            var prefix=role==0?"recovery-":"owner-";
            var bytes=Encoding.ASCII.GetBytes("gagamba-b6-role-v1\0").Concat(new[]{role}).Concat(
                new[]{"contract","deps","entry","m1","runtimeconfig"}.SelectMany(s=>Convert.FromHexString(manifest.Artifacts.Single(a=>a.Role==prefix+s).Sha256))).ToArray();
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)),role==0?identity.RecoveryClosureSha256:identity.OwnerClosureSha256);
        }
        Assert.Null(InstalledResult.Fresh(manifest).Identity);
        Assert.Null(InstalledResult.Unqualified("test").Identity);
    }
    [Fact] public void SevenPayloadsHaveIndependentGoldenBytes()
    {
        static string H(byte[] b)=>Convert.ToHexStringLower(b);
        var tail="010001000100000100000000";
        Assert.Equal(new string('1',64)+new string('3',64)+tail,H(InstallationWire.Echo(Identity,0)));
        Assert.Equal(new string('1',64)+new string('2',64)+"010001000100010100000000",H(InstallationWire.Echo(Identity,1)));
        Assert.Equal("01000000"+new string('1',64),H(InstallationWire.Response(Identity,InstallationMismatch.None)));
        Assert.Equal("ccddeeffaabb889977665544332211006500000066000000670000000000000001863ba101000000",H(InstallationWire.Binding(Binding)));
        Assert.Equal("ccddeeffaabb88997766554433221100"+new string('1',64),H(InstallationWire.Confirmation(Session,Identity)));
        var expectation="ccddeeffaabb88997766554433221100"+string.Concat(Enumerable.Range(1,6).Select(i=>new string((char)('0'+i),64)))+
            "0100010001000100011a"+Convert.ToHexStringLower(Encoding.ASCII.GetBytes(InstalledManifest.Profile))+"0131";
        Assert.Equal(expectation,H(InstallationWire.Expectation(Session,Identity)));
        foreach(var tuple in new[]{(RecoveryMessage.InstallationEchoRequest,InstallationWire.Echo(Identity,0)),
            (RecoveryMessage.InstallationEchoResponse,InstallationWire.Response(Identity,InstallationMismatch.None)),
            (RecoveryMessage.B2OwnerBound,InstallationWire.Binding(Binding)),(RecoveryMessage.B2OwnerEchoAccepted,InstallationWire.Confirmation(Session,Identity))})
        {
            bool controller=tuple.Item1 is RecoveryMessage.InstallationEchoResponse or RecoveryMessage.B2OwnerEchoAccepted;
            var frame=new RecoveryWire(Execution,Lease,2,false,controller).Create(tuple.Item1,tuple.Item2);
            Assert.Equal("704f314d0100"+((byte)tuple.Item1).ToString("x2")+"0033221100554477668899aabbccddeeff433221106554877698a9bacbdcedfe0f01000000"+
                H(BitConverter.GetBytes(tuple.Item2.Length))+H(tuple.Item2),H(frame));
        }
    }
    [Theory][InlineData(0)][InlineData(1)] public void EchoPositiveAndMutations(byte role)
    {
        var p=InstallationWire.Echo(Identity,role);
        Assert.Equal(InstallationMismatch.None,InstallationWire.CompareEcho(p,Identity,role));
        for(int i=0;i<p.Length;i++)
        {
            var bad=p.ToArray();bad[i]^=0x80;
            if(i<64) Assert.NotEqual(InstallationMismatch.None,InstallationWire.CompareEcho(bad,Identity,role));
            else Assert.False(InstallationWire.EchoShape(bad,role));
        }
        Assert.False(InstallationWire.EchoShape(p, (byte)(1-role)));
        Assert.False(InstallationWire.EchoShape(p[..75],role));
        Assert.False(InstallationWire.EchoShape(p.Concat(new byte[]{0}).ToArray(),role));
        Assert.False(InstallationWire.EchoShape(new byte[513],role));
    }
    [Fact] public void ExpectationCanonicalDecodingRejectsEveryMutatedConstantAndTail()
    {
        var p=InstallationWire.Expectation(Session,Identity);
        Assert.Equal(Identity,InstallationWire.ReadExpectation(p,Session));
        for(int i=208;i<p.Length;i++)
        {
            var bad=p.ToArray();bad[i]^=0x80;
            Assert.Throws<InvalidDataException>(()=>InstallationWire.ReadExpectation(bad,Session));
        }
        Assert.Throws<InvalidDataException>(()=>InstallationWire.ReadExpectation(p,Guid.NewGuid()));
        Assert.Throws<InvalidDataException>(()=>InstallationWire.ReadExpectation(p[..^1],Session));
        Assert.Throws<InvalidDataException>(()=>InstallationWire.ReadExpectation(p.Concat(new byte[]{0}).ToArray(),Session));
    }
    [Theory][InlineData(0,0,false)][InlineData(0,1,true)][InlineData(0,5,true)][InlineData(0,6,false)]
    [InlineData(1,0,true)][InlineData(1,1,false)][InlineData(2,0,false)]
    public void ResponsePairingIsExact(byte success,byte reason,bool valid)
    {
        var p=InstallationWire.Response(Identity,InstallationMismatch.None);p[0]=success;p[1]=reason;
        Assert.Equal(valid,InstallationWire.ResponseShape(p));
        if(success!=1 || reason!=0) Assert.Throws<InvalidDataException>(()=>InstallationWire.AcceptResponse(p,Identity));
    }
    [Fact] public void BindingReservedRolesAndConfirmationAreExact()
    {
        var p=InstallationWire.Binding(Binding);Assert.Equal(Binding,InstallationWire.ReadBinding(p));
        p[28]=1;Assert.Throws<InvalidDataException>(()=>InstallationWire.ReadBinding(p));
        Assert.Throws<InvalidDataException>(()=>InstallationWire.Binding(Binding with {OwnerPid=102}));
        var c=InstallationWire.Confirmation(Session,Identity);InstallationWire.AcceptConfirmation(c,Session,Identity);
        for(int i=0;i<48;i++) { var bad=c.ToArray();bad[i]^=1;Assert.Throws<InvalidDataException>(()=>InstallationWire.AcceptConfirmation(bad,Session,Identity)); }
    }
    [Fact] public void DirectionsVersionsTopologyAndEnvelopeAreIndependent()
    {
        var p=InstallationWire.Echo(Identity,0);
        var send=new RecoveryWire(Execution,Lease,2,false,false);var receive=new RecoveryWire(Execution,Lease,2,false,true);
        var f=send.Create(RecoveryMessage.InstallationEchoRequest,p);
        Assert.True(receive.Accept(f,out _,out _));Assert.False(receive.Accept(f,out _,out _));
        Assert.False(new RecoveryWire(Execution,Lease).Accept(f,out _,out _));
        Assert.False(new RecoveryWire(Execution,Lease,2,false,false).Accept(f,out _,out _));
        Assert.False(new RecoveryWire(Guid.NewGuid(),Lease,2,false,true).Accept(f,out _,out _));
        Assert.False(new RecoveryWire(Execution,Guid.NewGuid(),2,false,true).Accept(f,out _,out _));
        var owner=f.ToArray();owner[0]^=0x20;
        Assert.False(new RecoveryWire(Execution,Lease,2,false,true).Accept(owner,out _,out _));
        Assert.False(OwnerProtocol.TryDecode(f,Execution,Lease,out _,out _));
        Assert.Throws<ArgumentException>(()=>new RecoveryWire(Execution,Lease,2,true,false).Create(RecoveryMessage.B2OwnerBound,InstallationWire.Binding(Binding)));
        Assert.Throws<ArgumentException>(()=>new RecoveryWire(Execution,Lease,2,false,false).Create(RecoveryMessage.InstallationEchoResponse,InstallationWire.Response(Identity,InstallationMismatch.None)));
        f[4]=2;Assert.False(new RecoveryWire(Execution,Lease,2,false,true).Accept(f,out _,out _));
        Assert.True(OwnerProtocolChannel.DirectControl(OwnerProtocolMessageType.OwnerInstallationEcho));
        Assert.False(OwnerProtocolChannel.DirectControl(OwnerProtocolMessageType.B2OwnerExpectation));
        Assert.False(OwnerProtocolChannel.DirectControl(OwnerProtocolMessageType.B2EchoBootstrap));
        Assert.False(OwnerProtocol.HasValidControlPayloadShape(new(OwnerProtocolMessageType.OwnerInstallationEcho,Execution,Lease,1,new byte[77])));
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)]
    public void InvalidIdentitiesCannotClaimQualification(int field)
    {
        var bad=field switch{0=>Identity with{Qualified=false},1=>Identity with{Generation=new('9',64)},
            2=>Identity with{ArtifactSetSha256=new('0',64)},3=>Identity with{ProfileId="unknown"},
            4=>Identity with{Bootstrap=2},_=>Identity with{RecoveryClosureSha256=new('A',64)}};
        Assert.Throws<InvalidDataException>(()=>bad.Validate());
    }
    [Fact] public void BootstrapSeparatesV1V2AndRefusesDelegatedQualifiedBeforeAllocation()
    {
        var c=new RecoveryConfiguration(2,Execution,Lease,Session,101,1000,new byte[32],new(Path.GetFullPath("r"),[]),new(Path.GetFullPath("o"),[]),Path.GetFullPath("e"),
            B2InstallationIdentity:Identity,B2DeadlineNs:123);
        c.Validate();Assert.Equal(c,RecoveryConfiguration.Decode(JsonSerializer.SerializeToUtf8Bytes(c)) with{Secret=c.Secret,Recovery=c.Recovery,Owner=c.Owner});
        Assert.Throws<InvalidDataException>(()=>(c with{Version=1}).Validate());
        Assert.Throws<InvalidDataException>(()=>(c with{DelegatedParent="/must-not-open"}).Validate());
        Assert.Throws<InvalidDataException>(()=>(c with{B2InstallationIdentity=null}).Validate());
        var json=JsonSerializer.Serialize(c);
        Assert.Throws<InvalidDataException>(()=>RecoveryConfiguration.Decode(Encoding.UTF8.GetBytes(json[..^1]+",\"Version\":2}")));
        Assert.Throws<InvalidDataException>(()=>RecoveryConfiguration.Decode(Encoding.UTF8.GetBytes(json.Replace("\"Version\":2","\"Version\":2.0"))));
    }
    [Fact] public void ReadyRequiresExactCleanSnapshot()
    {
        var s=new RecoverySnapshot(102,103,101,102,true,false,false,0,null,null,false);
        var p=JsonSerializer.SerializeToUtf8Bytes(s);Assert.Equal(s,InstallationWire.Ready(p,Binding));
        foreach(var bad in new[]{s with{StopCount=1},s with{OwnerPid=104},s with{ReleaseAuthority=true},
            s with{SessionId=103},s with{Subreaper=false},s with{NativeStop="Confirmed"},s with{Cause="stop"},s with{DomainEmpty=true}})
            Assert.Throws<InvalidDataException>(()=>InstallationWire.Ready(JsonSerializer.SerializeToUtf8Bytes(bad),Binding));
        var json=Encoding.UTF8.GetString(p);
        Assert.Throws<InvalidDataException>(()=>InstallationWire.Ready(Encoding.UTF8.GetBytes(json[..^1]+",\"StopCount\":0}"),Binding));
        Assert.Throws<InvalidDataException>(()=>InstallationWire.Ready(Encoding.UTF8.GetBytes(json.Replace("\"StopCount\":0","\"StopCount\":0.0")),Binding));
    }
}
