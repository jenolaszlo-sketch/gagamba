using System.Buffers.Binary;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal static class Gava1ContextCodec
{
    private const int HeaderSize = 64;
    private const int MaxPayload = 128;
    private const int DigestSize = 32;
    private const uint FirstSequence = 1;

    internal static Gava1Refusal Decode(ReadOnlySpan<byte> frame, Gava1Expectation expectation,
        out Gava1ContextData? data)
    {
        data = null;
        var originals = expectation.Originals;
        var message = expectation.Message;
        var payloadSize = message switch
        {
            Gava1Message.Init or Gava1Message.DirectContext => 2 * DigestSize,
            Gava1Message.ContextAck or Gava1Message.DirectAck => DigestSize,
            _ => 0,
        };
        if (originals is not { Complete: true } || !expectation.Pending ||
            !expectation.OriginalAssociationMatches || !expectation.AncillaryMatches ||
            expectation.Sequence != FirstSequence || !ValidRoute(expectation) ||
            (message != Gava1Message.Init && !expectation.PriorContextEstablished))
            return Gava1Refusal.Context;

        if (frame.Length < HeaderSize || frame.Length > HeaderSize + MaxPayload ||
            !frame[..4].SequenceEqual("GAVA"u8) || BinaryPrimitives.ReadUInt16LittleEndian(frame[4..]) != 1 ||
            BinaryPrimitives.ReadUInt32LittleEndian(frame[8..]) != payloadSize ||
            frame.Length != HeaderSize + payloadSize)
            return Gava1Refusal.Framing;

        var ids = new Gava1Ids(new Guid(frame.Slice(16, 16)), new Guid(frame.Slice(32, 16)),
            new Guid(frame.Slice(48, 16)));
        if (frame[6] != (byte)message || frame[7] != (byte)expectation.Sender ||
            BinaryPrimitives.ReadUInt32LittleEndian(frame[12..]) != FirstSequence || ids != originals.Ids ||
            !originals.MatchesCapability(frame.Slice(HeaderSize, DigestSize)) ||
            (payloadSize == 2 * DigestSize && !originals.MatchesCatalog(frame[(HeaderSize + DigestSize)..])))
            return Gava1Refusal.Comparison;

        data = new Gava1ContextData(message, expectation.Sender, expectation.Recipient, originals);
        return Gava1Refusal.None;
    }

    internal static Gava1Refusal MatchPreBootstrap(Gava1PreBootstrap bootstrap, Gava1ContextData? context,
        Gava1PreTopology expectedTopology, Gava1Originals? originals, out Gava1Originals? handoff)
    {
        handoff = null;
        if (context is not { Message: Gava1Message.Init or Gava1Message.DirectContext, Originals.Complete: true } ||
            originals is not { Complete: true } || bootstrap.Version != 3 ||
            bootstrap.Topology is not (Gava1PreTopology.B2 or Gava1PreTopology.Integrated) ||
            bootstrap.Topology != expectedTopology ||
            bootstrap.Mode != "validation-pre" || !bootstrap.Ids.Complete ||
            bootstrap.Capability.Length != DigestSize)
            return Gava1Refusal.Context;

        if (!originals.Matches(context.Originals) || bootstrap.Ids != originals.Ids ||
            !originals.MatchesCapability(bootstrap.Capability.Span))
            return Gava1Refusal.Comparison;

        handoff = originals;
        return Gava1Refusal.None;
    }

    private static bool ValidRoute(Gava1Expectation context) =>
        (context.Message, context.Sender, context.Recipient) switch
        {
            (Gava1Message.Init, Gava1Role.Controller, Gava1Role.Recovery) => true,
            (Gava1Message.Init, Gava1Role.Recovery, Gava1Role.Owner) => true,
            (Gava1Message.ContextAck, Gava1Role.Recovery, Gava1Role.Controller) => true,
            (Gava1Message.ContextAck, Gava1Role.Owner, Gava1Role.Recovery) => true,
            (Gava1Message.DirectContext, Gava1Role.Controller, Gava1Role.Owner) => true,
            (Gava1Message.DirectAck, Gava1Role.Owner, Gava1Role.Controller) => true,
            _ => false,
        };
}
