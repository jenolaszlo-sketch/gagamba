using Gagamba.Execution.Linux.M1.Bootstrap;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

internal static class Gava1ContextVectors
{
    internal static readonly Gava1Ids WireIds = new(
        Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
        Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210"),
        Guid.Parse("10203040-5060-7080-90a0-b0c0d0e0f001"));
    internal static readonly Gava1Ids BootstrapIds = new(
        Guid.Parse("00000000-0000-0000-0000-000000000003"),
        Guid.Parse("00000000-0000-0000-0000-000000000001"),
        Guid.Parse("00000000-0000-0000-0000-000000000002"));

    internal static byte[] Capability => Enumerable.Repeat((byte)0x11, 32).ToArray();
    internal static byte[] Catalog => Enumerable.Repeat((byte)0x22, 32).ToArray();
    // Frozen E-3 binary-vectors.md, release-field witness R01.
    internal static byte[] ReleaseWitness =>
        Convert.FromHexString("3737572b7b3d2ad8313d109d6d8a9d0df4b5bc7b20e43c71a0a6061e1988ce3e");

    internal static Gava1Expectation Expect(byte type = 4, byte sender = 2, byte recipient = 3,
        Gava1Originals? originals = null) =>
        new((Gava1Message)type, (Gava1Role)sender, (Gava1Role)recipient, 1,
            originals ?? new Gava1Originals(WireIds, Capability, Catalog), true, true, true, true);

    internal static byte[] Pack(byte type, byte sender, Gava1Ids ids, byte[] capability, byte[] catalog)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(new byte[] { 0x47, 0x41, 0x56, 0x41, 1, 0, type, sender });
        writer.Write(type is 4 or 6 ? 64u : 32u);
        writer.Write(1u);
        writer.Write(ids.Session.ToByteArray());
        writer.Write(ids.Execution.ToByteArray());
        writer.Write(ids.Lease.ToByteArray());
        writer.Write(capability);
        if (type is 4 or 6)
            writer.Write(catalog);
        return stream.ToArray();
    }

    internal static byte[] Pack(byte type = 4, byte sender = 2) =>
        Pack(type, sender, WireIds, Capability, Catalog);
}
