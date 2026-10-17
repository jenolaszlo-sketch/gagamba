using System.Text.Json;
using System.Text.Json.Serialization;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Lifecycle;

/// <summary>Bounded serializable inputs for one owner-owned preparation.
/// It contains requested data only; all OS authority arrives as retained
/// descriptors on the private owner channel.</summary>
internal sealed record OwnerPreparationSpecification(
    string JournalDirectory,
    string DomainChildName,
    Guid ExecutionId,
    Guid LeaseGeneration,
    string BootId,
    Guid IssuerId,
    Guid StoreGenerationId,
    string SourceRootPath,
    string StagingParentPath,
    ulong StagingParentDevice,
    ulong StagingParentInode,
    string StagingDirectoryName,
    IReadOnlyList<CopyObjectPlan> Objects,
    ulong TotalBudgetBytes,
    int MaxObjects,
    IReadOnlyList<KeyValuePair<string, string>> Environment,
    long ExpiresAtNs,
    string? TestBarrier = null,
    bool EnableHeldCoordinator = false,
    long AcceptedAtNs = 0);

internal sealed record OwnerManifestAuthorization(
    RestrictedManifest Manifest,
    PreparationIdentity Identity);

internal sealed record OwnerPreparedDescription(
    Guid ExecutionId,
    Guid IssuerId,
    Guid GenerationId,
    Guid PreparationId,
    byte[] ManifestDigest);

/// <summary>Strict bounded JSON codec carried inside the versioned
/// SOCK_SEQPACKET Start frame. Unknown fields, excessive depth and oversized
/// input are refused before any owner authority is used.</summary>
internal static class OwnerPreparationSpecificationCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        MaxDepth = 12,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive = false,
    };

    private static readonly JsonSerializerOptions DescriptionOptions = new()
    {
        MaxDepth = 24,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive = false,
    };

    internal static byte[] Encode(OwnerPreparationSpecification specification)
    {
        Validate(specification);
        var encoded = JsonSerializer.SerializeToUtf8Bytes(specification, Options);
        if (encoded.Length > OwnerProtocol.MaxPayloadBytes)
        {
            throw new ArgumentException("Owner preparation request exceeds the protocol payload bound.", nameof(specification));
        }

        return encoded;
    }

    internal static bool TryDecode(
        ReadOnlySpan<byte> encoded,
        out OwnerPreparationSpecification? specification,
        out string error)
    {
        specification = null;
        error = string.Empty;
        if (encoded.Length == 0 || encoded.Length > OwnerProtocol.MaxPayloadBytes)
        {
            error = "owner preparation request length is outside the bound";
            return false;
        }

        try
        {
            var decoded = JsonSerializer.Deserialize<OwnerPreparationSpecification>(encoded, Options);
            if (decoded is null)
            {
                error = "owner preparation request is empty";
                return false;
            }

            Validate(decoded);
            specification = decoded;
            return true;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException)
        {
            error = $"invalid owner preparation request: {ex.GetType().Name}";
            return false;
        }
    }

    private static void Validate(OwnerPreparationSpecification value)
    {
        ArgumentNullException.ThrowIfNull(value);
        RequireBounded(value.JournalDirectory, 4096, nameof(value.JournalDirectory));
        RequireBounded(value.DomainChildName, 255, nameof(value.DomainChildName));
        RequireBounded(value.BootId, 64, nameof(value.BootId));
        RequireBounded(value.SourceRootPath, 4096, nameof(value.SourceRootPath));
        RequireBounded(value.StagingParentPath, 4096, nameof(value.StagingParentPath));
        RequireBounded(value.StagingDirectoryName, 255, nameof(value.StagingDirectoryName));
        if (value.ExecutionId == Guid.Empty || value.LeaseGeneration == Guid.Empty
            || value.IssuerId == Guid.Empty || value.StoreGenerationId == Guid.Empty)
        {
            throw new ArgumentException("Owner preparation identities must be non-empty.");
        }

        if (!Path.IsPathFullyQualified(value.JournalDirectory)
            || !Path.IsPathFullyQualified(value.SourceRootPath)
            || !Path.IsPathFullyQualified(value.StagingParentPath))
        {
            throw new ArgumentException("Owner preparation paths must be absolute.");
        }

        if (value.Objects is null || value.Objects.Count is 0 or > OwnershipSnapshotCodec.MaxAllocations - 2
            || value.Environment is null || value.Environment.Count > 256
            || value.StagingParentInode == 0 || value.TotalBudgetBytes == 0 || value.MaxObjects < value.Objects.Count
            || value.MaxObjects > 128 || value.ExpiresAtNs <= 0
            || value.EnableHeldCoordinator && (value.AcceptedAtNs <= 0 || value.ExpiresAtNs <= value.AcceptedAtNs))
        {
            throw new ArgumentException("Owner preparation bounds are invalid.");
        }

        RequireSingleName(value.DomainChildName, nameof(value.DomainChildName));
        RequireSingleName(value.StagingDirectoryName, nameof(value.StagingDirectoryName));
        for (var i = 0; i < value.Objects.Count; i++)
        {
            var item = value.Objects[i];
            if (item is null)
            {
                throw new ArgumentException($"Owner object request {i} is null.");
            }
            RequireBounded(item.Role, 32, $"Objects[{i}].Role");
            RequireBounded(item.SourceName, 255, $"Objects[{i}].SourceName");
            RequireSingleName(item.SourceName, $"Objects[{i}].SourceName");
            var allowedRole = item.Role is "tool" or "input"
                || value.EnableHeldCoordinator && item.Role is "coordinator" or "filter";
            if (!allowedRole || item.MaxBytes == 0
                || item.ClaimedDigest is { Length: not 32 })
            {
                throw new ArgumentException($"Owner object request {i} is invalid.");
            }
        }

        var roles = value.Objects.Select(item => item.Role).ToHashSet(StringComparer.Ordinal);
        if (roles.Count != value.Objects.Count
            || value.EnableHeldCoordinator && !new[] { "tool", "input", "coordinator", "filter" }.All(roles.Contains)
            || !value.EnableHeldCoordinator && roles.Any(role => role is "coordinator" or "filter"))
        {
            throw new ArgumentException("Owner object roles are duplicated or do not match the held-launch mode.");
        }

        foreach (var pair in value.Environment)
        {
            RequireBounded(pair.Key, 1024, "Environment key");
            RequireBounded(pair.Value, 4096, "Environment value");
        }

        if (value.TestBarrier is not null
            && value.TestBarrier is not ("after-intent" or "after-domain-retained" or "after-registered"
                or "after-staging-accounted" or "mid-staging" or "after-copy" or "after-prepared-before-mint"))
        {
            throw new ArgumentException("Unknown owner test barrier.", nameof(value.TestBarrier));
        }
    }

    internal static OwnershipSnapshot BuildIntent(OwnerPreparationSpecification request) =>
        new(
            OwnershipSnapshotCodec.LegacyVersion,
            0,
            request.ExecutionId,
            request.BootId,
            0,
            string.Empty,
            0,
            0,
            request.DomainChildName,
            0,
            0,
            request.LeaseGeneration,
            OwnershipWriterKind.Owner,
            OwnershipCleanupState.NotStarted,
            PreparationStage.AllocationIntent,
            request.StagingParentDevice,
            request.StagingParentInode,
            PlannedAllocations(request),
            Preparation: null);

    internal static IReadOnlyList<AllocationEntry> PlannedAllocations(OwnerPreparationSpecification request)
    {
        var allocations = new List<AllocationEntry>
        {
            new(AllocationKind.Domain, request.DomainChildName, 0, Array.Empty<byte>(), 0, 0, Confirmed: false),
            new(AllocationKind.StagingDirectory, request.StagingDirectoryName, 0, Array.Empty<byte>(), 0, 0, Confirmed: false),
        };

        for (var index = 0; index < request.Objects.Count; index++)
        {
            var item = request.Objects[index];
            allocations.Add(new AllocationEntry(
                AllocationKind.Staging, $"{item.Role}-{index:D3}.staged", 0, Array.Empty<byte>(), 0, 0, Confirmed: false));
        }

        return allocations;
    }

    internal static byte[] EncodeStagedObjects(IReadOnlyList<StagedObjectDescription> objects)
    {
        ArgumentNullException.ThrowIfNull(objects);
        if (objects.Count is 0 or > 128
            || objects.Any(item => item is null || item.Sha256 is null || item.Role is null || item.StagingName is null
                || item.Sha256.Length != 32 || item.Role.Length > 32
                || item.StagingName.Length > 255))
        {
            throw new ArgumentException("Staged-object description is outside its bound.", nameof(objects));
        }

        return EncodeJson(objects);
    }

    internal static bool TryDecodeStagedObjects(
        ReadOnlySpan<byte> payload,
        out IReadOnlyList<StagedObjectDescription>? objects,
        out string error)
    {
        objects = null;
        if (!TryDecodeJson<IReadOnlyList<StagedObjectDescription>>(payload, out var decoded, out error)
            || decoded is null || decoded.Count is 0 or > 128
            || decoded.Any(item => item is null || item.Sha256 is null || item.Role is null || item.StagingName is null
                || item.Sha256.Length != 32 || item.Role.Length > 32
                || item.StagingName.Length > 255))
        {
            error = error.Length == 0 ? "staged-object description is invalid" : error;
            return false;
        }

        objects = decoded;
        return true;
    }

    internal static byte[] EncodeAuthorization(OwnerManifestAuthorization authorization)
    {
        return OwnerManifestAuthorizationCodec.Encode(authorization);
    }

    internal static bool TryDecodeAuthorization(
        ReadOnlySpan<byte> payload,
        out OwnerManifestAuthorization? authorization,
        out string error)
    {
        return OwnerManifestAuthorizationCodec.TryDecode(payload, out authorization, out error);
    }

    internal static byte[] EncodePreparedDescription(OwnerPreparedDescription description)
    {
        ArgumentNullException.ThrowIfNull(description);
        if (description.ExecutionId == Guid.Empty || description.IssuerId == Guid.Empty
            || description.GenerationId == Guid.Empty || description.PreparationId == Guid.Empty
            || description.ManifestDigest.Length != 32)
        {
            throw new ArgumentException("Prepared description is invalid.", nameof(description));
        }

        return EncodeJson(description);
    }

    internal static bool TryDecodePreparedDescription(
        ReadOnlySpan<byte> payload,
        out OwnerPreparedDescription? description,
        out string error)
    {
        description = null;
        if (!TryDecodeJson<OwnerPreparedDescription>(payload, out var decoded, out error) || decoded is null)
        {
            error = error.Length == 0 ? "prepared description is invalid" : error;
            return false;
        }

        if (decoded.ManifestDigest is null || decoded.ExecutionId == Guid.Empty || decoded.IssuerId == Guid.Empty
            || decoded.GenerationId == Guid.Empty || decoded.PreparationId == Guid.Empty
            || decoded.ManifestDigest.Length != 32)
        {
            error = "prepared description identity is invalid";
            return false;
        }

        description = decoded;
        return true;
    }

    private static byte[] EncodeJson<T>(T value)
    {
        var encoded = JsonSerializer.SerializeToUtf8Bytes(value, DescriptionOptions);
        if (encoded.Length > OwnerProtocol.MaxPayloadBytes)
        {
            throw new ArgumentException("Owner protocol description exceeds its payload bound.");
        }

        return encoded;
    }

    private static bool TryDecodeJson<T>(ReadOnlySpan<byte> payload, out T? value, out string error)
    {
        value = default;
        error = string.Empty;
        if (payload.Length == 0 || payload.Length > OwnerProtocol.MaxPayloadBytes)
        {
            error = "owner protocol description length is outside the bound";
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(payload, DescriptionOptions);
            return value is not null;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException)
        {
            error = $"invalid owner protocol description: {ex.GetType().Name}";
            return false;
        }
    }

    private static void RequireSingleName(string value, string parameterName)
    {
        if (string.IsNullOrEmpty(value) || value is "." or ".."
            || value.Contains('/') || value.Contains('\\') || value.Contains('\0'))
        {
            throw new ArgumentException("Expected one safe path component.", parameterName);
        }
    }

    private static void RequireBounded(string value, int maxChars, string parameterName)
    {
        if (value is null || string.IsNullOrEmpty(value) || value.Length > maxChars || value.Contains('\0'))
        {
            throw new ArgumentException("Text field is empty or exceeds its bound.", parameterName);
        }
    }
}
