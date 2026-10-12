// WP03 strict transfer-message tests (G2-02): the versioned SCM_RIGHTS
// metadata message binds magic, version, flags, execution and lease
// identity and the exact ordered role list. Malformed messages refuse.
using Gagamba.Execution.Linux.M1.Native;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class DomainHandleTransferTests
{
    private static readonly Guid Exec = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Lease = new("bbbbbbbb-0000-0000-0000-000000000002");

    [Fact]
    public void ValidMessageDecodesToExactRoles()
    {
        var payload = DomainHandleTransfer.Encode(Exec, Lease);
        Assert.True(DomainHandleTransfer.TryDecode(payload, Exec, Lease, out var roles, out var error), error);
        Assert.Equal(DomainHandleTransfer.OrderedRoles, roles);
        Assert.Equal(DomainHandleTransfer.ExactDescriptorCount, roles.Count);
    }

    [Fact]
    public void WrongExecutionRefused()
    {
        var payload = DomainHandleTransfer.Encode(Exec, Lease);
        Assert.False(DomainHandleTransfer.TryDecode(payload, Guid.NewGuid(), Lease, out _, out _));
    }

    [Fact]
    public void WrongLeaseRefused()
    {
        var payload = DomainHandleTransfer.Encode(Exec, Lease);
        Assert.False(DomainHandleTransfer.TryDecode(payload, Exec, Guid.NewGuid(), out _, out _));
    }

    [Fact]
    public void BadMagicRefused()
    {
        var payload = DomainHandleTransfer.Encode(Exec, Lease);
        payload[0] ^= 0xFF;
        Assert.False(DomainHandleTransfer.TryDecode(payload, Exec, Lease, out _, out _));
    }

    [Fact]
    public void WrongVersionRefused()
    {
        var payload = DomainHandleTransfer.Encode(Exec, Lease);
        payload[4] = 2;
        Assert.False(DomainHandleTransfer.TryDecode(payload, Exec, Lease, out _, out _));
    }

    [Fact]
    public void NonzeroFlagsRefused()
    {
        var payload = DomainHandleTransfer.Encode(Exec, Lease);
        payload[6] = 1;
        Assert.False(DomainHandleTransfer.TryDecode(payload, Exec, Lease, out _, out _));
    }

    [Fact]
    public void TruncatedPayloadRefused()
    {
        var payload = DomainHandleTransfer.Encode(Exec, Lease);
        var short1 = payload[..^1];
        Assert.False(DomainHandleTransfer.TryDecode(short1, Exec, Lease, out _, out _));
    }

    [Fact]
    public void ReservedByteRefused()
    {
        var payload = DomainHandleTransfer.Encode(Exec, Lease);
        payload[41] = 1;
        Assert.False(DomainHandleTransfer.TryDecode(payload, Exec, Lease, out _, out _));
    }

    [Fact]
    public void RoleOrderMismatchRefused()
    {
        var payload = DomainHandleTransfer.Encode(Exec, Lease);
        // Swap the first two role bytes.
        (payload[DomainHandleTransfer.HeaderBytes], payload[DomainHandleTransfer.HeaderBytes + 1]) =
            (payload[DomainHandleTransfer.HeaderBytes + 1], payload[DomainHandleTransfer.HeaderBytes]);
        Assert.False(DomainHandleTransfer.TryDecode(payload, Exec, Lease, out _, out _));
    }

    [Fact]
    public void DuplicateRoleRefused()
    {
        var payload = DomainHandleTransfer.Encode(Exec, Lease);
        payload[DomainHandleTransfer.HeaderBytes + 1] = (byte)DomainTransferRole.Parent;
        Assert.False(DomainHandleTransfer.TryDecode(payload, Exec, Lease, out _, out _));
    }
}
