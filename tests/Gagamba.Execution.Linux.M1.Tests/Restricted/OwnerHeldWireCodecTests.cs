// Deterministic B3 held-phase wire tests: strict bounded round-trips, refusal
// of unknown or malformed fields and the exact owner-protocol shape rules for
// the new message types. Pure managed; no native workload.
using System.Collections.Immutable;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Control;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Restricted;

public sealed class OwnerHeldWireCodecTests
{
    private static readonly Guid Execution = Guid.NewGuid();
    private static readonly Guid Preparation = Guid.NewGuid();
    private static readonly Guid Generation = Guid.NewGuid();

    private static byte[] Digest(byte seed)
    {
        var bytes = new byte[32];
        Array.Fill(bytes, seed);
        return bytes;
    }

    private static OwnerReleaseRequestWire Request(
        byte[]? correlation = null) =>
        new(Execution, Preparation, Generation, Digest(0x11), correlation);

    [Fact]
    public void ReleaseRequestRoundTripsAbsentAndPresentCorrelations()
    {
        foreach (var correlation in new byte[]?[] { null, Digest(0x00), Digest(0x22) })
        {
            var encoded = OwnerHeldWireCodec.EncodeReleaseRequest(Request(correlation));
            Assert.True(OwnerHeldWireCodec.TryDecodeReleaseRequest(encoded, out var decoded, out var error), error);
            Assert.NotNull(decoded);
            Assert.Equal(Execution, decoded!.ExecutionId);
            Assert.Equal(Preparation, decoded.PreparationId);
            Assert.Equal(Generation, decoded.GenerationId);
            Assert.True(decoded.ManifestDigest.AsSpan().SequenceEqual(Digest(0x11)));
            if (correlation is null)
            {
                Assert.Null(decoded.ApprovalCorrelationSha256);
            }
            else
            {
                Assert.True(decoded.ApprovalCorrelationSha256!.AsSpan().SequenceEqual(correlation));
            }
        }
    }

    [Fact]
    public void ReleaseRequestRefusesMalformedIdentityLengthsAndUnknownFields()
    {
        Assert.False(OwnerHeldWireCodec.TryDecodeReleaseRequest([], out _, out _));
        Assert.Throws<ArgumentException>(() => OwnerHeldWireCodec.EncodeReleaseRequest(
            Request() with { ManifestDigest = new byte[31] }));
        Assert.Throws<ArgumentException>(() => OwnerHeldWireCodec.EncodeReleaseRequest(
            Request(new byte[33])));
        Assert.Throws<ArgumentException>(() => OwnerHeldWireCodec.EncodeReleaseRequest(
            Request() with { ExecutionId = Guid.Empty }));

        var encoded = OwnerHeldWireCodec.EncodeReleaseRequest(Request(Digest(1)));
        var json = System.Text.Encoding.UTF8.GetString(encoded);
        var unknown = System.Text.Encoding.UTF8.GetBytes(json[..^1] + ",\"Extra\":1}");
        Assert.False(OwnerHeldWireCodec.TryDecodeReleaseRequest(unknown, out _, out _));
        Assert.False(OwnerHeldWireCodec.TryDecodeReleaseRequest(new byte[1025], out _, out _));
    }

    [Fact]
    public void ReleaseDecisionRoundTripsEveryKindAndRefusesUnknownTags()
    {
        var decisions = new[]
        {
            new ReleaseDecision(ReleaseDecisionKind.Refused, ReleaseDisposition.NotCommitted,
                DeliveryResult.NotAttempted, false, true, "refused",
                ReleaseCorrelationBinding.Mismatch, RestrictedRefusalReason.InvalidBinding),
            new ReleaseDecision(ReleaseDecisionKind.Released, ReleaseDisposition.MayHaveExecuted,
                DeliveryResult.Delivered, true, false, "released",
                ReleaseCorrelationBinding.Accepted, RestrictedRefusalReason.None),
            new ReleaseDecision(ReleaseDecisionKind.AmbiguousPersistence, ReleaseDisposition.MayHaveExecuted,
                DeliveryResult.NotAttempted, false, true, "ambiguous",
                ReleaseCorrelationBinding.Accepted, RestrictedRefusalReason.None),
        };
        foreach (var decision in decisions)
        {
            var encoded = OwnerHeldWireCodec.EncodeReleaseDecision(decision);
            Assert.True(OwnerHeldWireCodec.TryDecodeReleaseDecision(encoded, out var decoded, out var error), error);
            Assert.Equal(decision, decoded);
        }

        var json = System.Text.Encoding.UTF8.GetString(
            OwnerHeldWireCodec.EncodeReleaseDecision(decisions[1]));
        Assert.False(OwnerHeldWireCodec.TryDecodeReleaseDecision(
            System.Text.Encoding.UTF8.GetBytes(json.Replace("\"Released\"", "\"Invented\"")), out _, out _));
        Assert.False(OwnerHeldWireCodec.TryDecodeReleaseDecision(
            System.Text.Encoding.UTF8.GetBytes(json.Replace("\"Accepted\"", "\"UnknownValue\"")), out _, out _));
    }

    [Fact]
    public void StopDecisionRoundTripsAndRefusesInvertedTimes()
    {
        var decision = new StopDecision(true, false, true, true, StopCauseKind.CallerStop, 100, 200, "stop");
        var encoded = OwnerHeldWireCodec.EncodeStopDecision(decision);
        Assert.True(OwnerHeldWireCodec.TryDecodeStopDecision(encoded, out var decoded, out var error), error);
        Assert.Equal(decision, decoded);

        var json = System.Text.Encoding.UTF8.GetString(encoded);
        Assert.False(OwnerHeldWireCodec.TryDecodeStopDecision(
            System.Text.Encoding.UTF8.GetBytes(json.Replace("\"CallerStop\"", "\"CallerStopX\"")), out _, out _));
        var inverted = json.Replace("\"AcceptedAtNanoseconds\":100", "\"AcceptedAtNanoseconds\":300");
        Assert.False(OwnerHeldWireCodec.TryDecodeStopDecision(
            System.Text.Encoding.UTF8.GetBytes(inverted), out _, out _));
    }

    [Fact]
    public void ExecutionOutcomeRoundTripsAndRefusesInvalidFields()
    {
        var facts = new OwnerExecutionFacts(
            RestrictedOutcome.Succeeded, ExecTag.Confirmed, 0, RootKind.Exited, 0, 0, false,
            true, 0, true, 0, true, false,
            29, 29, true, false, 30, 30, true, false, false, Digest(3), Digest(4), 999, StdoutReadFailure:false, StderrReadFailure:false, CaptureDrainExpired:false);
        var encoded = OwnerHeldWireCodec.EncodeExecutionOutcome(facts, "observed");
        Assert.True(OwnerHeldWireCodec.TryDecodeExecutionOutcome(encoded, out var decoded, out var reason, out var error),
            error);
        Assert.NotNull(decoded);
        Assert.Equal(facts.Outcome, decoded!.Outcome);
        Assert.Equal(facts.Exec, decoded.Exec);
        Assert.Equal(facts.Root, decoded.Root);
        Assert.Equal(facts.ExitCode, decoded.ExitCode);
        Assert.Equal(facts.RawWaitPresent, decoded.RawWaitPresent);
        Assert.Equal(facts.WrapperExited, decoded.WrapperExited);
        Assert.Equal(facts.DomainKnown, decoded.DomainKnown);
        Assert.Equal(facts.StdoutStoredBytes, decoded.StdoutStoredBytes);
        Assert.Equal(facts.StdoutEof, decoded.StdoutEof);
        Assert.True(decoded.StdoutSha256!.AsSpan().SequenceEqual(Digest(3)));
        Assert.True(decoded.StderrSha256!.AsSpan().SequenceEqual(Digest(4)));
        Assert.Equal(facts.ObservedAtNanoseconds, decoded.ObservedAtNanoseconds);
        Assert.Equal("observed", reason);
        Assert.False(decoded.StdoutReadFailure); Assert.False(decoded.StderrReadFailure); Assert.False(decoded.CaptureDrainExpired);
        var invalid = System.Text.Encoding.UTF8.GetString(encoded).Replace("\"StdoutStoredBytes\":29", "\"StdoutStoredBytes\":65537");
        Assert.False(OwnerHeldWireCodec.TryDecodeExecutionOutcome(System.Text.Encoding.UTF8.GetBytes(invalid),out _,out _,out _));

        Assert.False(OwnerHeldWireCodec.TryDecodeExecutionOutcome(
            System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(encoded)
                .Replace("\"Succeeded\"", "\"Imaginary\"")), out _, out _, out _));
        Assert.False(OwnerHeldWireCodec.TryDecodeExecutionOutcome(
            System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(encoded)
                .Replace("\"ObservedAtNanoseconds\":999", "\"ObservedAtNanoseconds\":0")), out _, out _, out _));
    }

    [Fact]
    public void OwnerProtocolShapeRulesMatchTheNewMessageTypes()
    {
        Assert.False(OwnerProtocol.HasValidControlPayloadShape(
            new(OwnerProtocolMessageType.HeldReady, Execution, Generation, 1, new byte[112])));
        Assert.True(OwnerProtocol.HasValidControlPayloadShape(
            new(OwnerProtocolMessageType.HeldReady, Execution, Generation, 1, new byte[113])));
        Assert.True(OwnerProtocol.HasValidControlPayloadShape(
            new(OwnerProtocolMessageType.HeldReady, Execution, Generation, 1, new byte[4113])));
        Assert.False(OwnerProtocol.HasValidControlPayloadShape(
            new(OwnerProtocolMessageType.HeldReady, Execution, Generation, 1, new byte[4114])));

        foreach (var length in new[] { 0, 1, 4 })
        {
            Assert.Equal(length is 2 or 3, OwnerProtocol.HasValidControlPayloadShape(
                new(OwnerProtocolMessageType.HeldAborted, Execution, Generation, 1, new byte[length])));
        }

        Assert.True(OwnerProtocol.HasValidControlPayloadShape(
            new(OwnerProtocolMessageType.StopRequest, Execution, Generation, 1, [1])));
        Assert.False(OwnerProtocol.HasValidControlPayloadShape(
            new(OwnerProtocolMessageType.StopRequest, Execution, Generation, 1, [])));
        Assert.False(OwnerProtocol.HasValidControlPayloadShape(
            new(OwnerProtocolMessageType.ReleaseRequest, Execution, Generation, 1, [])));
        Assert.True(OwnerProtocol.HasValidControlPayloadShape(
            new(OwnerProtocolMessageType.ReleaseRequest, Execution, Generation, 1, new byte[1])));
        Assert.True(OwnerProtocol.HasValidControlPayloadShape(
            new(OwnerProtocolMessageType.ExecutionOutcome, Execution, Generation, 1, new byte[4096])));
        Assert.False(OwnerProtocol.HasValidControlPayloadShape(
            new(OwnerProtocolMessageType.ExecutionOutcome, Execution, Generation, 1, new byte[4097])));
        foreach (var type in new[]
                 {
                     OwnerProtocolMessageType.ReleaseRequest, OwnerProtocolMessageType.ReleaseDecision,
                     OwnerProtocolMessageType.StopDecision, OwnerProtocolMessageType.ExecutionOutcome,
                 })
        {
            Assert.False(OwnerProtocol.IsTransferType(type));
        }

        Assert.False(OwnerProtocol.IsKnownBarrierPayload(
            System.Text.Encoding.UTF8.GetBytes("release-after-committed")));
        Assert.True(OwnerProtocol.IsKnownBarrierPayload(
            System.Text.Encoding.UTF8.GetBytes("release-after-intent")));
    }
}
