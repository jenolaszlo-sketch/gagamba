// WP03 strict SCM_RIGHTS wire contract (G2-02). One bounded versioned
// metadata message binds the primary DomainLease transfer: magic, version,
// flags, execution identity, lease-generation identity and the exact
// ordered role list. The metadata is validation evidence; the passed FDs
// remain the authority. No release authority is representable here.
using System.Buffers.Binary;

namespace Gagamba.Execution.Linux.M1.Native;

internal enum DomainTransferRole : byte
{
    Parent = 0,
    DomainDirectory = 1,
    Kill = 2,
    Events = 3,
    MemoryMax = 4,
    MemorySwapMax = 5,
    PidsMax = 6,
}

internal static class DomainHandleTransfer
{
    internal const uint Magic = 0x4D314654; // "M1FT"
    internal const ushort CurrentVersion = 1;
    internal const int HeaderBytes = 4 + 2 + 2 + 16 + 16 + 1 + 3;

    /// <summary>The exact ordered role list; role order is fixed, so a
    /// descriptor cannot be silently reassigned to another authority role.</summary>
    internal static readonly DomainTransferRole[] OrderedRoles =
    {
        DomainTransferRole.Parent,
        DomainTransferRole.DomainDirectory,
        DomainTransferRole.Kill,
        DomainTransferRole.Events,
        DomainTransferRole.MemoryMax,
        DomainTransferRole.MemorySwapMax,
        DomainTransferRole.PidsMax,
    };

    internal static int ExactDescriptorCount => OrderedRoles.Length;

    internal static int ExactPayloadBytes => HeaderBytes + OrderedRoles.Length;

    internal static byte[] Encode(Guid executionId, Guid leaseGeneration)
    {
        var payload = new byte[ExactPayloadBytes];
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0), Magic);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4), CurrentVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6), 0);
        executionId.TryWriteBytes(payload.AsSpan(8, 16));
        leaseGeneration.TryWriteBytes(payload.AsSpan(24, 16));
        payload[40] = (byte)OrderedRoles.Length;
        payload[41] = 0;
        payload[42] = 0;
        payload[43] = 0;
        for (var i = 0; i < OrderedRoles.Length; i++)
        {
            payload[HeaderBytes + i] = (byte)OrderedRoles[i];
        }

        return payload;
    }

    internal static bool TryDecode(
        byte[] payload,
        Guid expectedExecutionId,
        Guid expectedLeaseGeneration,
        out IReadOnlyList<DomainTransferRole> roles,
        out string error)
    {
        roles = Array.Empty<DomainTransferRole>();
        error = string.Empty;
        if (payload.Length != ExactPayloadBytes)
        {
            error = $"payload length {payload.Length} != {ExactPayloadBytes}";
            return false;
        }

        if (BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0)) != Magic)
        {
            error = "bad magic";
            return false;
        }

        if (BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4)) != CurrentVersion)
        {
            error = "unsupported version";
            return false;
        }

        if (BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(6)) != 0)
        {
            error = "nonzero flags";
            return false;
        }

        var executionId = new Guid(payload.AsSpan(8, 16));
        if (!executionId.Equals(expectedExecutionId))
        {
            error = "execution identity mismatch";
            return false;
        }

        var leaseGeneration = new Guid(payload.AsSpan(24, 16));
        if (!leaseGeneration.Equals(expectedLeaseGeneration))
        {
            error = "lease-generation mismatch";
            return false;
        }

        if (payload[40] != OrderedRoles.Length || payload[41] != 0 || payload[42] != 0 || payload[43] != 0)
        {
            error = "role count/reserved invalid";
            return false;
        }

        var decoded = new DomainTransferRole[OrderedRoles.Length];
        for (var i = 0; i < OrderedRoles.Length; i++)
        {
            var role = (DomainTransferRole)payload[HeaderBytes + i];
            if (role != OrderedRoles[i])
            {
                error = $"role order mismatch at {i}: {role} != {OrderedRoles[i]}";
                return false;
            }

            decoded[i] = role;
        }

        var seen = new HashSet<DomainTransferRole>();
        foreach (var role in decoded)
        {
            if (!seen.Add(role))
            {
                error = $"duplicate role {role}";
                return false;
            }
        }

        roles = decoded;
        return true;
    }
}
