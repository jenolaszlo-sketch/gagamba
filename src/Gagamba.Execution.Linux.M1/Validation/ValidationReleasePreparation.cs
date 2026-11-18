using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Security.Cryptography;
using static Gagamba.Execution.Linux.M1.Validation.ValidationReleasePreparationStatus;

namespace Gagamba.Execution.Linux.M1.Validation;

internal enum ValidationReleasePhase : byte
{
    Pre = 1,
    Post = 2,
}

internal enum ValidationReleaseTopology : byte
{
    B2Lifeline = 1,
    B3Integrated = 2,
}

internal enum ValidationReleaseOpcode : byte
{
    ConnectB2 = 1,
    PrepareB3 = 2,
    Inspect = 3,
    Release = 4,
    Stop = 5,
    Cleanup = 6,
}

internal enum ValidationReleasePreparationStatus : byte
{
    MissingInput = 0,
    UnsupportedVersion = 1,
    InvalidScalar = 2,
    InvalidCaseId = 3,
    InvalidLength = 4,
    InvalidTemplate = 5,
    NoReleaseTemplate = 6,
    CaseMismatch = 7,
    ProjectionMismatch = 8,
    MissingExpectedDigest = 9,
    DigestMismatch = 10,
    StructuralMatch = 11,
}

internal readonly struct ValidationReleaseStep
{
    internal ValidationReleaseStep(uint index, ValidationReleaseOpcode opcode)
    {
        Index = index;
        Opcode = opcode;
    }

    internal uint Index { get; }
    internal ValidationReleaseOpcode Opcode { get; }
}

internal sealed class ValidationReleaseProjection
{
    internal ValidationReleaseProjection(
        uint? schemaVersion,
        ValidationReleasePhase? phase,
        ValidationReleaseTopology? topology,
        string? caseId,
        bool? catalogAllowsRelease,
        bool? fixturePolicyAllowsRelease,
        ValidationReleaseStep[]? steps,
        uint? requestedActionIndex,
        byte[]? sealedLaunchRecordHash,
        byte[]? catalogHash,
        byte[]? scriptHash,
        byte[]? releaseActionHash,
        Guid? sessionId,
        Guid? executionId,
        Guid? protocolLeaseId,
        Guid? preparationId,
        byte[]? preparationManifestDigest)
    {
        SchemaVersion = schemaVersion;
        Phase = phase;
        Topology = topology;
        CaseId = caseId;
        CatalogAllowsRelease = catalogAllowsRelease;
        FixturePolicyAllowsRelease = fixturePolicyAllowsRelease;
        Steps = steps;
        RequestedActionIndex = requestedActionIndex;
        SealedLaunchRecordHash = sealedLaunchRecordHash;
        CatalogHash = catalogHash;
        ScriptHash = scriptHash;
        ReleaseActionHash = releaseActionHash;
        SessionId = sessionId;
        ExecutionId = executionId;
        ProtocolLeaseId = protocolLeaseId;
        PreparationId = preparationId;
        PreparationManifestDigest = preparationManifestDigest;
    }

    internal uint? SchemaVersion { get; }
    internal ValidationReleasePhase? Phase { get; }
    internal ValidationReleaseTopology? Topology { get; }
    internal string? CaseId { get; }
    internal bool? CatalogAllowsRelease { get; }
    internal bool? FixturePolicyAllowsRelease { get; }
    internal ValidationReleaseStep[]? Steps { get; }
    internal uint? RequestedActionIndex { get; }
    internal byte[]? SealedLaunchRecordHash { get; }
    internal byte[]? CatalogHash { get; }
    internal byte[]? ScriptHash { get; }
    internal byte[]? ReleaseActionHash { get; }
    internal Guid? SessionId { get; }
    internal Guid? ExecutionId { get; }
    internal Guid? ProtocolLeaseId { get; }
    internal Guid? PreparationId { get; }
    internal byte[]? PreparationManifestDigest { get; }
}

internal sealed class ValidationReleaseMaterial
{
    internal ValidationReleaseMaterial(ImmutableArray<byte> preimage, ImmutableArray<byte> correlationSha256)
    {
        Preimage = preimage;
        CorrelationSha256 = correlationSha256;
    }

    internal ImmutableArray<byte> Preimage { get; }
    internal ImmutableArray<byte> CorrelationSha256 { get; }
}

internal static class ValidationReleasePreparation
{
    private const int DigestLength = 32;
    private const int PreimageLength = 258;
    private const uint ReleaseIndex = 2;

    private static ReadOnlySpan<byte> Lifeline => [
        (byte)ValidationReleaseOpcode.ConnectB2, (byte)ValidationReleaseOpcode.Inspect,
        (byte)ValidationReleaseOpcode.Stop, (byte)ValidationReleaseOpcode.Cleanup,
    ];

    private static ReadOnlySpan<byte> IntegratedWithoutRelease => [
        (byte)ValidationReleaseOpcode.PrepareB3, (byte)ValidationReleaseOpcode.Inspect,
        (byte)ValidationReleaseOpcode.Stop, (byte)ValidationReleaseOpcode.Cleanup,
    ];

    private static ReadOnlySpan<byte> IntegratedWithRelease => [
        (byte)ValidationReleaseOpcode.PrepareB3, (byte)ValidationReleaseOpcode.Inspect,
        (byte)ValidationReleaseOpcode.Release, (byte)ValidationReleaseOpcode.Inspect,
        (byte)ValidationReleaseOpcode.Stop, (byte)ValidationReleaseOpcode.Cleanup,
    ];

    internal static ValidationReleasePreparationStatus Prepare(
        ValidationReleaseProjection? candidate,
        ValidationReleaseProjection? expected,
        out ValidationReleaseMaterial? material)
    {
        material = null;
        var status = ValidateAgreement(candidate, expected, out var binding);
        if (status != StructuralMatch)
            return status;

        byte[] preimage = Encode(binding);
        material = Publish(preimage, SHA256.HashData(preimage));
        return StructuralMatch;
    }

    internal static ValidationReleasePreparationStatus Compare(
        ValidationReleaseProjection? candidate,
        ValidationReleaseProjection? expected,
        byte[]? expectedCorrelationSha256,
        out ValidationReleaseMaterial? material)
    {
        material = null;
        var status = ValidateAgreement(candidate, expected, out var binding);
        if (status != StructuralMatch)
            return status;
        if (expectedCorrelationSha256 is null)
            return MissingExpectedDigest;
        if (expectedCorrelationSha256.Length != DigestLength)
            return InvalidLength;

        byte[] expectedDigest = (byte[])expectedCorrelationSha256.Clone();
        byte[] preimage = Encode(binding);
        byte[] digest = SHA256.HashData(preimage);
        if (!CryptographicOperations.FixedTimeEquals(digest, expectedDigest))
            return DigestMismatch;

        material = Publish(preimage, digest);
        return StructuralMatch;
    }

    private static ValidationReleasePreparationStatus ValidateAgreement(
        ValidationReleaseProjection? candidate,
        ValidationReleaseProjection? expected,
        out Binding actual)
    {
        actual = default;
        if (candidate is null || expected is null)
            return MissingInput;

        var status = Validate(candidate, out actual);
        if (status != StructuralMatch)
            return status;
        status = Validate(expected, out var original);
        if (status != StructuralMatch)
            return status;

        if (!string.Equals(actual.CaseId, original.CaseId, StringComparison.Ordinal))
            return CaseMismatch;

        // Validation has already fixed the schema, topology, steps, flags and index.
        if (actual.Phase != original.Phase ||
            actual.SessionId != original.SessionId ||
            actual.ExecutionId != original.ExecutionId ||
            actual.ProtocolLeaseId != original.ProtocolLeaseId ||
            actual.PreparationId != original.PreparationId ||
            !actual.SealedLaunchRecordHash.AsSpan().SequenceEqual(original.SealedLaunchRecordHash) ||
            !actual.CatalogHash.AsSpan().SequenceEqual(original.CatalogHash) ||
            !actual.ScriptHash.AsSpan().SequenceEqual(original.ScriptHash) ||
            !actual.ReleaseActionHash.AsSpan().SequenceEqual(original.ReleaseActionHash) ||
            !actual.PreparationManifestDigest.AsSpan().SequenceEqual(original.PreparationManifestDigest))
            return ProjectionMismatch;

        return StructuralMatch;
    }

    private static ValidationReleasePreparationStatus Validate(
        ValidationReleaseProjection input,
        out Binding binding)
    {
        binding = default;
        if (input.SchemaVersion is not uint version ||
            input.Phase is not ValidationReleasePhase phase ||
            input.Topology is not ValidationReleaseTopology topology ||
            input.CaseId is not string caseId ||
            input.CatalogAllowsRelease is not bool catalogAllowsRelease ||
            input.FixturePolicyAllowsRelease is not bool fixturePolicyAllowsRelease ||
            input.Steps is not { } steps ||
            input.RequestedActionIndex is not uint index ||
            input.SealedLaunchRecordHash is not { } sealedLaunchRecordHash ||
            input.CatalogHash is not { } catalogHash ||
            input.ScriptHash is not { } scriptHash ||
            input.ReleaseActionHash is not { } releaseActionHash ||
            input.SessionId is not Guid session ||
            input.ExecutionId is not Guid execution ||
            input.ProtocolLeaseId is not Guid protocolLease ||
            input.PreparationId is not Guid preparation ||
            input.PreparationManifestDigest is not { } manifestDigest)
            return MissingInput;

        if (version != 1)
            return UnsupportedVersion;

        if (phase is not (ValidationReleasePhase.Pre or ValidationReleasePhase.Post) ||
            topology is not (ValidationReleaseTopology.B2Lifeline or ValidationReleaseTopology.B3Integrated) ||
            session == Guid.Empty || execution == Guid.Empty ||
            protocolLease == Guid.Empty || preparation == Guid.Empty)
            return InvalidScalar;

        if (!IsValidCaseId(caseId))
            return InvalidCaseId;

        if (steps.Length is not (4 or 6) ||
            sealedLaunchRecordHash.Length != DigestLength ||
            catalogHash.Length != DigestLength ||
            scriptHash.Length != DigestLength ||
            releaseActionHash.Length != DigestLength ||
            manifestDigest.Length != DigestLength)
            return InvalidLength;

        var localSteps = (ValidationReleaseStep[])steps.Clone();
        var localBinding = new Binding(phase, caseId, session, execution, protocolLease, preparation,
            (byte[])sealedLaunchRecordHash.Clone(), (byte[])catalogHash.Clone(), (byte[])scriptHash.Clone(),
            (byte[])releaseActionHash.Clone(), (byte[])manifestDigest.Clone());

        bool release = topology == ValidationReleaseTopology.B3Integrated && localSteps.Length == 6;
        ReadOnlySpan<byte> template = release ? IntegratedWithRelease
            : topology == ValidationReleaseTopology.B2Lifeline ? Lifeline
            : IntegratedWithoutRelease;
        if (!MatchesTemplate(localSteps, template) ||
            catalogAllowsRelease != release ||
            fixturePolicyAllowsRelease != release)
            return InvalidTemplate;
        if (!release)
            return NoReleaseTemplate;
        if (index != ReleaseIndex)
            return InvalidScalar;

        binding = localBinding;
        return StructuralMatch;
    }

    private static bool IsValidCaseId(string value)
    {
        if (value.Length is < 1 or > 128 || value[0] == '.' || value[^1] == '.')
            return false;
        foreach (char c in value)
        {
            if (c is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or '-'))
                return false;
        }
        return true;
    }

    private static bool MatchesTemplate(ValidationReleaseStep[] steps, ReadOnlySpan<byte> template)
    {
        if (steps.Length != template.Length)
            return false;
        for (int i = 0; i < template.Length; i++)
        {
            if (steps[i].Index != (uint)i || (byte)steps[i].Opcode != template[i])
                return false;
        }
        return true;
    }

    private static byte[] Encode(Binding binding)
    {
        byte[] preimage = new byte[PreimageLength];
        Span<byte> bytes = preimage;
        "gagamba-validation-release-v1\0"u8.CopyTo(bytes[..30]);
        binding.SealedLaunchRecordHash.CopyTo(bytes[30..62]);
        binding.CatalogHash.CopyTo(bytes[62..94]);
        binding.ScriptHash.CopyTo(bytes[94..126]);
        binding.ReleaseActionHash.CopyTo(bytes[126..158]);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes[158..162], ReleaseIndex);
        binding.SessionId.TryWriteBytes(bytes[162..178]);
        binding.ExecutionId.TryWriteBytes(bytes[178..194]);
        binding.ProtocolLeaseId.TryWriteBytes(bytes[194..210]);
        binding.PreparationId.TryWriteBytes(bytes[210..226]);
        binding.PreparationManifestDigest.CopyTo(bytes[226..258]);
        return preimage;
    }

    private static ValidationReleaseMaterial Publish(byte[] preimage, byte[] digest) =>
        new(ImmutableArray.CreateRange(preimage), ImmutableArray.CreateRange(digest));

    private readonly record struct Binding(
        ValidationReleasePhase Phase,
        string CaseId,
        Guid SessionId,
        Guid ExecutionId,
        Guid ProtocolLeaseId,
        Guid PreparationId,
        byte[] SealedLaunchRecordHash,
        byte[] CatalogHash,
        byte[] ScriptHash,
        byte[] ReleaseActionHash,
        byte[] PreparationManifestDigest);
}
