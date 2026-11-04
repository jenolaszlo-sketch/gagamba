using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Installed;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal enum InstallationMismatch : byte { None, Digest, Closure, Protocol, Generation, Qualified }
internal sealed record B2Binding(Guid Session,int ControllerPid,int RecoveryPid,int OwnerPid,long DeadlineNs)
{
    internal void Validate()
    {
        if(Session==Guid.Empty || ControllerPid<=0 || RecoveryPid<=0 || OwnerPid<=0 || DeadlineNs<=0 ||
            new[]{ControllerPid,RecoveryPid,OwnerPid}.Distinct().Count()!=3)
            throw new InvalidDataException("B2 process binding");
    }
}

// Exact additive payloads. Framing and per-direction counters remain M1OP/RecoveryWire.
internal static class InstallationWire
{
    internal const int EchoBytes=76, ResponseBytes=36, BindingBytes=40, ConfirmationBytes=48;
    internal static byte[] Echo(InstallationIdentity identity,byte role)
    {
        identity.Validate(); if(role>1) throw new InvalidDataException("echo role");
        var p=new byte[EchoBytes]; Convert.FromHexString(identity.ArtifactSetSha256).CopyTo(p,0);
        Convert.FromHexString(role==0?identity.RecoveryClosureSha256:identity.OwnerClosureSha256).CopyTo(p,32);
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(64),identity.OwnerProtocol);
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(66),identity.CoordinatorProtocol);
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(68),identity.Bootstrap);
        p[70]=role;p[71]=1;return p;
    }
    internal static bool EchoShape(ReadOnlySpan<byte> p,byte role,bool integrated=false)
    {
        if(p.Length<76 || p.Length>512 || role>1 || p[70]!=role || p[71]!=1 ||
            p.Slice(72,4).IndexOfAnyExcept((byte)0)>=0 || p[..32].IndexOfAnyExcept((byte)0)<0 ||
            p.Slice(32,32).IndexOfAnyExcept((byte)0)<0 ||
            BinaryPrimitives.ReadUInt16LittleEndian(p[64..])!=1 ||
            BinaryPrimitives.ReadUInt16LittleEndian(p[66..])!=1 ||
            BinaryPrimitives.ReadUInt16LittleEndian(p[68..])!=1) return false;
        if(p.Length==76) return true;
        // Accepted integrated optional tail: two u8 lengths and exact profile ASCII.
        if(!integrated) return false;
        return ExactStrings(p[76..],InstalledManifest.Profile,InstalledManifest.Version);
    }
    internal static InstallationMismatch CompareEcho(ReadOnlySpan<byte> p,InstallationIdentity expected,byte role,bool integrated=false)
    {
        expected.Validate();
        if(!EchoShape(p,role,integrated)) throw new InvalidDataException("echo shape");
        if(!p[..32].SequenceEqual(Convert.FromHexString(expected.ArtifactSetSha256))) return InstallationMismatch.Digest;
        if(!p.Slice(32,32).SequenceEqual(Convert.FromHexString(role==0?expected.RecoveryClosureSha256:expected.OwnerClosureSha256))) return InstallationMismatch.Closure;
        return InstallationMismatch.None;
    }
    internal static byte[] Response(InstallationIdentity expected,InstallationMismatch reason)
    {
        expected.Validate(); if(!Enum.IsDefined(reason)) throw new InvalidDataException("echo reason");
        var p=new byte[36];p[0]=(byte)(reason==InstallationMismatch.None?1:0);p[1]=(byte)reason;
        Convert.FromHexString(expected.ArtifactSetSha256).CopyTo(p,4);return p;
    }
    internal static bool ResponseShape(ReadOnlySpan<byte> p) => p.Length==36 && p[2]==0 && p[3]==0 &&
        p[4..].IndexOfAnyExcept((byte)0)>=0 && ((p[0]==1 && p[1]==0) || (p[0]==0 && p[1] is >=1 and <=5));
    internal static void AcceptResponse(ReadOnlySpan<byte> p,InstallationIdentity expected)
    {
        if(!ResponseShape(p) || p[0]!=1 || !p[4..].SequenceEqual(Convert.FromHexString(expected.ArtifactSetSha256)))
            throw new InvalidDataException("recovery identity refused");
    }
    internal static byte[] Binding(B2Binding binding)
    {
        binding.Validate();var p=new byte[40];binding.Session.TryWriteBytes(p);
        BinaryPrimitives.WriteInt32LittleEndian(p.AsSpan(16),binding.ControllerPid);
        BinaryPrimitives.WriteInt32LittleEndian(p.AsSpan(20),binding.RecoveryPid);
        BinaryPrimitives.WriteInt32LittleEndian(p.AsSpan(24),binding.OwnerPid);
        BinaryPrimitives.WriteInt64LittleEndian(p.AsSpan(32),binding.DeadlineNs);return p;
    }
    internal static B2Binding ReadBinding(ReadOnlySpan<byte> p)
    {
        if(p.Length!=40 || BinaryPrimitives.ReadUInt32LittleEndian(p[28..])!=0) throw new InvalidDataException("binding shape");
        var b=new B2Binding(new Guid(p[..16]),BinaryPrimitives.ReadInt32LittleEndian(p[16..]),
            BinaryPrimitives.ReadInt32LittleEndian(p[20..]),BinaryPrimitives.ReadInt32LittleEndian(p[24..]),
            BinaryPrimitives.ReadInt64LittleEndian(p[32..]));b.Validate();return b;
    }
    internal static byte[] Expectation(Guid session,InstallationIdentity identity)
    {
        identity.Validate();if(session==Guid.Empty) throw new InvalidDataException("session missing");
        var id=Encoding.ASCII.GetBytes(identity.ProfileId);var version=Encoding.ASCII.GetBytes(identity.ProfileVersion);
        var p=new byte[219+id.Length+version.Length];session.TryWriteBytes(p);
        var hashes=new[]{identity.ArtifactSetSha256,identity.OwnerClosureSha256,identity.RecoveryClosureSha256,
            identity.CoordinatorSha256,identity.SeccompFilterSha256,identity.RuntimeInventorySha256};
        for(int i=0;i<6;i++) Convert.FromHexString(hashes[i]).CopyTo(p,16+32*i);
        for(int i=0;i<4;i++) BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(208+2*i),1);
        p[216]=1;p[217]=(byte)id.Length;id.CopyTo(p,218);p[218+id.Length]=(byte)version.Length;
        version.CopyTo(p,219+id.Length);return p;
    }
    internal static InstallationIdentity ReadExpectation(ReadOnlySpan<byte> p,Guid session)
    {
        if(session==Guid.Empty || p.Length is <221 or >347 || new Guid(p[..16])!=session || p[216]!=1 ||
            !ExactStrings(p[217..],InstalledManifest.Profile,InstalledManifest.Version)) throw new InvalidDataException("expectation shape/session");
        var hashes=new string[6];for(int i=0;i<6;i++) hashes[i]=Convert.ToHexStringLower(p.Slice(16+32*i,32));
        var v=new InstallationIdentity(hashes[0],hashes[1],hashes[2],hashes[3],hashes[4],hashes[5],
            BinaryPrimitives.ReadUInt16LittleEndian(p[208..]),BinaryPrimitives.ReadUInt16LittleEndian(p[210..]),
            BinaryPrimitives.ReadUInt16LittleEndian(p[212..]),BinaryPrimitives.ReadUInt16LittleEndian(p[214..]),
            InstalledManifest.Profile,InstalledManifest.Version,hashes[0],true);v.Validate();return v;
    }
    private static bool ExactStrings(ReadOnlySpan<byte> p,string id,string version)
    {
        var a=Encoding.ASCII.GetBytes(id);var b=Encoding.ASCII.GetBytes(version);
        return p.Length==2+a.Length+b.Length && p[0]==a.Length && p.Slice(1,a.Length).SequenceEqual(a) &&
            p[1+a.Length]==b.Length && p[(2+a.Length)..].SequenceEqual(b);
    }
    internal static byte[] Confirmation(Guid session,InstallationIdentity identity)
    {
        identity.Validate();if(session==Guid.Empty) throw new InvalidDataException("confirmation session");
        var p=new byte[48];session.TryWriteBytes(p);Convert.FromHexString(identity.ArtifactSetSha256).CopyTo(p,16);return p;
    }
    internal static void AcceptConfirmation(ReadOnlySpan<byte> p,Guid session,InstallationIdentity identity)
    {
        if(!p.SequenceEqual(Confirmation(session,identity))) throw new InvalidDataException("owner confirmation refused");
    }
    internal static RecoverySnapshot Ready(byte[] data,B2Binding binding)
    {
        if(data.Length is <1 or >4096) throw new InvalidDataException("Ready size");
        var obj=InstallJson.Read(data);
        InstallJson.Keys(obj,"RecoveryPid OwnerPid ControllerPid SessionId Subreaper ControllerLost OwnerExited StopCount Cause StopDeadline OwnerAcknowledged ReleaseAuthority NativeStop RetainedDomains DomainRemoved OwnerReaped WriterFence StagingRemoved JournalFinalized DomainEmpty",false);
        foreach(string key in new[]{"RecoveryPid","OwnerPid","ControllerPid","SessionId","StopCount","RetainedDomains"}) _=InstallJson.Int(obj,key);
        var s=JsonSerializer.Deserialize<RecoverySnapshot>(data) ?? throw new InvalidDataException("Ready missing");
        if(s.RecoveryPid!=binding.RecoveryPid || s.OwnerPid!=binding.OwnerPid || s.ControllerPid!=binding.ControllerPid ||
            s.SessionId!=binding.RecoveryPid || !s.Subreaper || s.StopCount!=0 || s.RetainedDomains!=0 || s.Cause!=null ||
            s.StopDeadline!=null || s.NativeStop!="Unproven" || s.ControllerLost || s.OwnerExited || s.OwnerAcknowledged ||
            s.ReleaseAuthority || s.DomainRemoved || s.OwnerReaped || s.WriterFence || s.StagingRemoved || s.JournalFinalized || s.DomainEmpty)
            throw new InvalidDataException("Ready not clean");
        return s;
    }
}
