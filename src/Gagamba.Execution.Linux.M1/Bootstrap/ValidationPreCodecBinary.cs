namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal static class ValidationPreCodecBinary
{
    private const int EchoBytes = 76;
    private const int IntegratedExtendedBytes = 105;
    private const int ExpectationBytes = 246;
    private static ReadOnlySpan<byte> ProfilePair => "\u001aae4-linux-fixed-fixture-v1\u00011"u8;

    internal static ValidationPreRefusal ValidatePayload(
        ReadOnlySpan<byte> payload, ValidationPreBinaryContext context, ValidationPreBinaryExpectations? expected)
    {
        if (!context.Pre.IsPre)
            return ValidationPreRefusal.Context;
        if (expected is null || !KnownDigest(expected.ArtifactSetSha256.Span))
            return ValidationPreRefusal.Expectation;

        var b2 = context.Pre.Topology == ValidationPreTopology.B2;
        return context.Message switch
        {
            ValidationPreMessage.RecoveryEcho => Echo(payload, expected, b2, 0),
            ValidationPreMessage.OwnerEcho => Echo(payload, expected, b2, 1),
            ValidationPreMessage.B2OwnerExpectation when b2 => OwnerExpectation(payload, expected),
            ValidationPreMessage.RecoveryEchoResponse when context.MatchingPredecessor => EchoResponse(payload, expected),
            ValidationPreMessage.B2OwnerEchoAccepted when b2 && context.MatchingPredecessor =>
                OwnerEchoAccepted(payload, expected),
            _ => ValidationPreRefusal.Context
        };
    }

    private static ValidationPreRefusal Echo(
        ReadOnlySpan<byte> payload, ValidationPreBinaryExpectations expected, bool b2, byte role)
    {
        if (payload.Length != EchoBytes && (b2 || payload.Length != IntegratedExtendedBytes))
            return ValidationPreRefusal.Size;

        var closure = role == 0 ? expected.RecoveryClosureSha256.Span : expected.OwnerClosureSha256.Span;
        if (!KnownDigest(closure) || (!b2 && !ExpectedProfile(expected)))
            return ValidationPreRefusal.Expectation;

        // Owner, coordinator and bootstrap u16 LE constants, then role, status, reserved.
        if (!payload.Slice(64, 6).SequenceEqual("\u0001\0\u0001\0\u0001\0"u8) ||
            payload[70] != role || payload[71] != 0 || payload.Slice(72, 4).ContainsAnyExcept((byte)0))
            return ValidationPreRefusal.Layout;
        if (payload.Length == IntegratedExtendedBytes && !payload[EchoBytes..].SequenceEqual(ProfilePair))
            return ValidationPreRefusal.Layout;

        return payload[..32].SequenceEqual(expected.ArtifactSetSha256.Span)
            && payload.Slice(32, 32).SequenceEqual(closure)
            ? ValidationPreRefusal.None : ValidationPreRefusal.Comparison;
    }

    private static ValidationPreRefusal OwnerExpectation(
        ReadOnlySpan<byte> payload, ValidationPreBinaryExpectations expected)
    {
        if (payload.Length != ExpectationBytes)
            return ValidationPreRefusal.Size;
        if (expected.Session == Guid.Empty || !ExpectedProfile(expected)
            || !KnownDigest(expected.OwnerClosureSha256.Span) || !KnownDigest(expected.RecoveryClosureSha256.Span)
            || !KnownDigest(expected.CoordinatorSha256.Span) || !KnownDigest(expected.SeccompFilterSha256.Span)
            || !KnownDigest(expected.RuntimeInventorySha256.Span))
            return ValidationPreRefusal.Expectation;

        if (!payload.Slice(208, 8).SequenceEqual("\u0001\0\u0001\0\u0001\0\u0001\0"u8)
            || payload[216] != 0 || !payload[217..].SequenceEqual(ProfilePair))
            return ValidationPreRefusal.Layout;

        return new Guid(payload[..16]) == expected.Session
            && payload.Slice(16, 32).SequenceEqual(expected.ArtifactSetSha256.Span)
            && payload.Slice(48, 32).SequenceEqual(expected.OwnerClosureSha256.Span)
            && payload.Slice(80, 32).SequenceEqual(expected.RecoveryClosureSha256.Span)
            && payload.Slice(112, 32).SequenceEqual(expected.CoordinatorSha256.Span)
            && payload.Slice(144, 32).SequenceEqual(expected.SeccompFilterSha256.Span)
            && payload.Slice(176, 32).SequenceEqual(expected.RuntimeInventorySha256.Span)
            ? ValidationPreRefusal.None : ValidationPreRefusal.Comparison;
    }

    private static ValidationPreRefusal EchoResponse(ReadOnlySpan<byte> payload, ValidationPreBinaryExpectations expected)
    {
        if (payload.Length != 36)
            return ValidationPreRefusal.Size;
        var success = payload[0] == 1 && payload[1] == 0;
        var refusal = payload[0] == 0 && payload[1] is >= 1 and <= 5;
        if ((!success && !refusal) || payload[2] != 0 || payload[3] != 0)
            return ValidationPreRefusal.Layout;
        if (!payload[4..].SequenceEqual(expected.ArtifactSetSha256.Span))
            return ValidationPreRefusal.Comparison;
        return success ? ValidationPreRefusal.None : ValidationPreRefusal.PeerRefusal;
    }

    private static ValidationPreRefusal OwnerEchoAccepted(
        ReadOnlySpan<byte> payload, ValidationPreBinaryExpectations expected)
    {
        if (payload.Length != 48)
            return ValidationPreRefusal.Size;
        if (expected.Session == Guid.Empty)
            return ValidationPreRefusal.Expectation;
        return new Guid(payload[..16]) == expected.Session && payload[16..].SequenceEqual(expected.ArtifactSetSha256.Span)
            ? ValidationPreRefusal.None : ValidationPreRefusal.Comparison;
    }

    private static bool KnownDigest(ReadOnlySpan<byte> digest) => digest.Length == 32 && digest.ContainsAnyExcept((byte)0);

    private static bool ExpectedProfile(ValidationPreBinaryExpectations expected) =>
        expected.ProfileId == ValidationPreContext.ProfileId && expected.ProfileVersion == ValidationPreContext.ProfileVersion;
}
