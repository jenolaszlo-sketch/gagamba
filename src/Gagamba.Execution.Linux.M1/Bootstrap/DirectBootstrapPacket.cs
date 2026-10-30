using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal enum DirectBootstrapRole:byte {OriginalControllerProcessReference=0,RecoverySafetyLifeline=1,OwnerProcessReference=2}
internal sealed record DirectBootstrapPacket(ushort Version,DirectAssociation Association,DirectBootstrapRole[] Roles)
{
    private static readonly JsonSerializerOptions Options=new() {MaxDepth=8,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
    internal static byte[] Encode(DirectAssociation association)=>JsonSerializer.SerializeToUtf8Bytes(new DirectBootstrapPacket(1,association,
        [DirectBootstrapRole.OriginalControllerProcessReference,DirectBootstrapRole.RecoverySafetyLifeline]),Options);
    internal static DirectAssociation Decode(byte[] payload)
    {
        if(payload.Length is <1 or >2048) throw new IOException("Direct bootstrap size refused");
        var packet=JsonSerializer.Deserialize<DirectBootstrapPacket>(payload,Options);
        if(packet is null || packet.Version!=1 || packet.Roles is not {Length:2}
            || packet.Roles[0]!=DirectBootstrapRole.OriginalControllerProcessReference
            || packet.Roles[1]!=DirectBootstrapRole.RecoverySafetyLifeline)
            throw new IOException("Non-release direct bootstrap roles refused");
        packet.Association.Validate(); return packet.Association;
    }
}
