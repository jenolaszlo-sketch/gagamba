using System.Text.Json;
using System.Text.Json.Serialization;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Lifecycle;

/// <summary>JSON-only value transport for the caller-authorized manifest.
/// Arrays and byte arrays are used on the wire so deserialization never
/// bypasses the validated manifest constructors.</summary>
internal sealed record OwnerManifestWire(
    ushort SchemaVersion,
    string ImplementationId,
    OwnerAuthorityWire Authority,
    ResourceManifest Resources,
    OwnerClosureObjectWire[] ClosureObjects,
    string[] HelperDependencies,
    OwnerInvocationWire Invocation,
    ChannelManifest Channels);

internal sealed record OwnerAuthorityWire(
    string[] Namespaces,
    string[] Capabilities,
    bool NoNewPrivs,
    byte[] FilterSha256,
    ulong FilterLength,
    string[] ReadOnlyDestinations,
    WritableStore[] WritableStores,
    bool NoAmbientEnvironment,
    bool NoIpcEndpoints,
    bool NoExtraInheritedFds,
    ExecutableMountPolicy MountPolicy,
    QualificationDisposition NetworkQualification);

internal sealed record OwnerClosureObjectWire(
    string Role,
    byte[] Sha256,
    ulong Length,
    string Destination,
    string BuildIdentity);

internal sealed record OwnerInvocationWire(
    string ToolPath,
    string Mode,
    string InputPath,
    string[] Argv,
    KeyValuePair<string, string>[] Environment,
    string WorkDirectory);

internal sealed record OwnerManifestAuthorizationWire(
    OwnerManifestWire Manifest,
    Guid ExecutionId,
    Guid IssuerId,
    Guid GenerationId,
    Guid PreparationId,
    byte[] ManifestDigest);

internal static class OwnerManifestAuthorizationCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        MaxDepth = 24,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive = false,
    };

    internal static byte[] Encode(OwnerManifestAuthorization authorization)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        var digest = ManifestCodec.ComputeDigestBytes(authorization.Manifest);
        if (!authorization.Identity.ManifestDigest.AsSpan().SequenceEqual(digest))
        {
            throw new ArgumentException("Preparation authorization digest does not bind the manifest.", nameof(authorization));
        }

        var wire = new OwnerManifestAuthorizationWire(
            ToWire(authorization.Manifest),
            authorization.Identity.ExecutionId,
            authorization.Identity.IssuerId,
            authorization.Identity.GenerationId,
            authorization.Identity.PreparationId,
            digest);
        return Encode(wire);
    }

    internal static bool TryDecode(
        ReadOnlySpan<byte> payload,
        out OwnerManifestAuthorization? authorization,
        out string error)
    {
        authorization = null;
        error = string.Empty;
        if (payload.Length == 0 || payload.Length > Native.OwnerProtocol.MaxPayloadBytes)
        {
            error = "manifest authorization length is outside its bound";
            return false;
        }

        try
        {
            var wire = JsonSerializer.Deserialize<OwnerManifestAuthorizationWire>(payload, Options);
            if (wire is null || wire.ManifestDigest is not { Length: 32 }
                || wire.ExecutionId == Guid.Empty || wire.IssuerId == Guid.Empty
                || wire.GenerationId == Guid.Empty || wire.PreparationId == Guid.Empty)
            {
                error = "manifest authorization identity is invalid";
                return false;
            }

            var manifest = FromWire(wire.Manifest);
            var digest = ManifestCodec.ComputeDigestBytes(manifest);
            if (!digest.AsSpan().SequenceEqual(wire.ManifestDigest))
            {
                error = "manifest authorization digest mismatch";
                return false;
            }

            authorization = new OwnerManifestAuthorization(
                manifest,
                new PreparationIdentity(
                    wire.ExecutionId,
                    wire.IssuerId,
                    wire.GenerationId,
                    wire.PreparationId,
                    digest));
            return true;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or NullReferenceException)
        {
            error = $"invalid manifest authorization: {ex.GetType().Name}";
            return false;
        }
    }

    private static byte[] Encode(OwnerManifestAuthorizationWire wire)
    {
        var encoded = JsonSerializer.SerializeToUtf8Bytes(wire, Options);
        if (encoded.Length > Native.OwnerProtocol.MaxPayloadBytes)
        {
            throw new ArgumentException("Manifest authorization exceeds the owner protocol bound.");
        }

        return encoded;
    }

    private static OwnerManifestWire ToWire(RestrictedManifest manifest) => new(
        manifest.SchemaVersion,
        manifest.ImplementationId,
        new OwnerAuthorityWire(
            manifest.Authority.Namespaces.ToArray(),
            manifest.Authority.Capabilities.ToArray(),
            manifest.Authority.NoNewPrivs,
            manifest.Authority.FilterSha256.ToArray(),
            manifest.Authority.FilterLength,
            manifest.Authority.ReadOnlyDestinations.ToArray(),
            manifest.Authority.WritableStores.ToArray(),
            manifest.Authority.NoAmbientEnvironment,
            manifest.Authority.NoIpcEndpoints,
            manifest.Authority.NoExtraInheritedFds,
            manifest.Authority.MountPolicy,
            manifest.Authority.NetworkQualification),
        manifest.Resources,
        manifest.Closure.Objects.Select(o => new OwnerClosureObjectWire(
            o.Role, o.Sha256.ToArray(), o.Length, o.Destination, o.BuildIdentity)).ToArray(),
        manifest.Closure.HelperDependencies.ToArray(),
        new OwnerInvocationWire(
            manifest.Invocation.ToolPath,
            manifest.Invocation.Mode,
            manifest.Invocation.InputPath,
            manifest.Invocation.Argv.ToArray(),
            manifest.Invocation.Environment.ToArray(),
            manifest.Invocation.WorkDirectory),
        manifest.Channels);

    private static RestrictedManifest FromWire(OwnerManifestWire wire)
    {
        ArgumentNullException.ThrowIfNull(wire);
        var authority = wire.Authority;
        var closure = new ClosureIdentity(
            (wire.ClosureObjects ?? throw new ArgumentException("Closure object list is missing."))
                .Select(o => new ClosureObject(o.Role, o.Sha256, o.Length, o.Destination, o.BuildIdentity)),
            wire.HelperDependencies ?? throw new ArgumentException("Helper dependency list is missing."));
        var invocation = wire.Invocation;
        var builtAuthority = new AuthorityManifest(
            authority.Namespaces,
            authority.Capabilities,
            authority.NoNewPrivs,
            authority.FilterSha256,
            authority.FilterLength,
            authority.ReadOnlyDestinations,
            authority.WritableStores,
            authority.NoAmbientEnvironment,
            authority.NoIpcEndpoints,
            authority.NoExtraInheritedFds,
            authority.MountPolicy,
            authority.NetworkQualification);
        var builtInvocation = new InvocationManifest(
            invocation.ToolPath,
            invocation.Mode,
            invocation.InputPath,
            invocation.Argv,
            invocation.Environment,
            invocation.WorkDirectory);
        return new RestrictedManifest(
            wire.SchemaVersion,
            wire.ImplementationId,
            builtAuthority,
            wire.Resources,
            closure,
            builtInvocation,
            wire.Channels);
    }
}
