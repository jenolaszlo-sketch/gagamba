using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Gagamba.Execution.Restricted.Contracts;

/// <summary>Closed schema-1 refusal vocabulary; errors contain no input text.</summary>
public enum ManifestDecodeError
{
    EmptyInput, InputTooLarge, UnsupportedSchema, UnsupportedImplementation, Truncated,
    InvalidLengthOrCount, InvalidUtf8, InvalidTag, InvalidValue, NonCanonicalOrder,
    Duplicate, TrailingData,
}

/// <summary>Either one complete validated view or an error and zero-based byte offset.</summary>
public sealed class ManifestDecodeResult
{
    internal ManifestDecodeResult(RestrictedManifestView value) => Value = value;
    internal ManifestDecodeResult(ManifestDecodeError error, int offset) { Error = error; Offset = offset; }
    public RestrictedManifestView? Value { get; }
    public ManifestDecodeError? Error { get; }
    public int? Offset { get; }
}

/// <summary>Passive canonical schema-1 inspection. A valid description or digest grants no authority.</summary>
public static class RestrictedManifestCodec
{
    public const ushort SupportedSchemaVersion = 1;
    public const int MaxCanonicalBytes = 65536;

    public static bool Supports(ushort schemaVersion, string implementationId)
    {
        ArgumentNullException.ThrowIfNull(implementationId);
        return schemaVersion == SupportedSchemaVersion &&
            string.Equals(implementationId, RestrictedManifest.ImplementationIdentifier, StringComparison.Ordinal);
    }

    public static ManifestDecodeResult Decode(ReadOnlySpan<byte> canonicalBytes)
    {
        if (canonicalBytes.Length > MaxCanonicalBytes) return new(ManifestDecodeError.InputTooLarge, 0);
        if (canonicalBytes.IsEmpty) return new(ManifestDecodeError.EmptyInput, 0);
        return ManifestReader.Decode(canonicalBytes);
    }

    /// <exception cref="ArgumentException">The complete encoding exceeds 65,536 bytes or the model is invalid.</exception>
    public static byte[] Encode(RestrictedManifestView manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ManifestValidation.Validate(manifest.Model);
        return ManifestCodec.Encode(manifest.Model, MaxCanonicalBytes);
    }

    public static ImmutableArray<byte> ComputeSha256(RestrictedManifestView manifest) =>
        ImmutableArray.Create(SHA256.HashData(Encode(manifest)));

    /// <summary>Returns false for a digest of the wrong length or a nonmatching digest.</summary>
    public static bool VerifySha256(RestrictedManifestView manifest, ReadOnlySpan<byte> expectedSha256)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (expectedSha256.Length != 32) return false;
        return CryptographicOperations.FixedTimeEquals(ComputeSha256(manifest).AsSpan(), expectedSha256);
    }
}
