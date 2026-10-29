using System.Collections.Immutable;
using Gagamba.Execution.Restricted.Contracts;

namespace Gagamba.Execution.Restricted.Control;

/// <summary>Trusted-host control contract, separate from the ordinary lifecycle SPI.
/// Interface conformance is not provider attestation. B1 installs no live provider.</summary>
public interface IRestrictedExecutionProvider
{
    RestrictedProviderDescriptor Describe();
    RestrictedOperationCreation CreateOperation(RestrictedProfileSelection selection);
    ValueTask<RestrictedInspection> InspectExecutionAsync(RestrictedPreparationReference reference,
        CancellationToken waitCancellation = default);
    ValueTask<RestrictedReconciliation> ReconcileAsync(RestrictedPreparationReference reference,
        CancellationToken waitCancellation = default);
}

internal static class ControlValidation
{
    internal static string Id(string value, string name, int maximum = 128)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        if (value.Length == 0 || value.Length > maximum || value.Any(c =>
                !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':')))
            throw new ArgumentException("A bounded ASCII identity is required.", name);
        return value;
    }

    internal static ImmutableArray<byte> Digest(ReadOnlySpan<byte> value, string name)
    {
        if (value.Length != 32) throw new ArgumentException("Exactly 32 bytes are required.", name);
        return ImmutableArray.Create(value);
    }

    internal static T Tag<T>(T value) where T : struct, Enum => Enum.IsDefined(value)
        ? value : throw new ArgumentOutOfRangeException(nameof(value));
}

/// <summary>Evidence identifying an installed profile. These values alone cannot select
/// a provider, enroll sources, establish qualification or confer release authority.</summary>
public sealed class RestrictedProfileSelection
{
    public RestrictedProfileSelection(string profileId, string profileVersion, string runtimeIdentifier,
        string qualificationId, ReadOnlySpan<byte> artifactSetSha256)
    {
        ProfileId = ControlValidation.Id(profileId, nameof(profileId));
        ProfileVersion = ControlValidation.Id(profileVersion, nameof(profileVersion));
        RuntimeIdentifier = ControlValidation.Id(runtimeIdentifier, nameof(runtimeIdentifier));
        QualificationId = ControlValidation.Id(qualificationId, nameof(qualificationId));
        ArtifactSetSha256 = ControlValidation.Digest(artifactSetSha256, nameof(artifactSetSha256));
    }

    public string ProfileId { get; }
    public string ProfileVersion { get; }
    public string RuntimeIdentifier { get; }
    public string QualificationId { get; }
    public ImmutableArray<byte> ArtifactSetSha256 { get; }
}

/// <summary>Names protected sources already enrolled by a trusted installation. No paths,
/// argv, manifests, handles, deadlines or caller-supplied readiness evidence.</summary>
public sealed class RestrictedPreparationInputs
{
    public RestrictedPreparationInputs(string toolEnrollmentId, string inputEnrollmentId)
    {
        ToolEnrollmentId = ControlValidation.Id(toolEnrollmentId, nameof(toolEnrollmentId));
        InputEnrollmentId = ControlValidation.Id(inputEnrollmentId, nameof(inputEnrollmentId));
    }

    public string ToolEnrollmentId { get; }
    public string InputEnrollmentId { get; }
    internal bool Matches(RestrictedPreparationInputs other) =>
        ToolEnrollmentId == other.ToolEnrollmentId && InputEnrollmentId == other.InputEnrollmentId;
}

public enum RestrictedInstallationState { Unavailable = 0, Unqualified = 1, Qualified = 2 }
public enum RestrictedRefusalReason
{
    None = 0, UnsupportedProfile, InstallationUnavailable, UnqualifiedInstallation,
    InvalidBinding, InvalidCorrelation, StopRequested, Disposed, PreparationAlreadyStarted,
    OwnerUnavailable, LiveReleaseNotImplemented, RecoveryAuthorityUnavailable,
    // B3 additive reasons for the real release-admission path. Existing B1
    // values keep their meaning and numeric identity.
    ReleaseInProgress = 12, DeadlineExpired = 13, PrerequisitesUnavailable = 14
}

public sealed class RestrictedArtifactIdentity
{
    internal RestrictedArtifactIdentity(string role, ReadOnlySpan<byte> sha256, long length, string buildIdentity)
    {
        Role = ControlValidation.Id(role, nameof(role));
        Sha256 = ControlValidation.Digest(sha256, nameof(sha256));
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        Length = length;
        BuildIdentity = ControlValidation.Id(buildIdentity, nameof(buildIdentity));
    }
    public string Role { get; }
    public ImmutableArray<byte> Sha256 { get; }
    public long Length { get; }
    public string BuildIdentity { get; }
}

/// <summary>Provider-origin deployment description. Absent installation is explicit:
/// null identities and an empty inventory never qualify selection. ArtifactSetSha256
/// identifies exact retained inventory bytes, not a manifest or a version string.</summary>
public sealed class RestrictedProviderDescriptor
{
    internal RestrictedProviderDescriptor() { }

    // No public descriptor constructor or registration path. B6 must provide the
    // reviewed manifest, closure verification and authenticated handshake first.
    public ushort DescriptorVersion => 1;
    public string ContractId => "gagamba-restricted-control-v1";
    public string ContractPackageId => "Gagamba.Execution";
    public RestrictedInstallationState InstallationState => RestrictedInstallationState.Unavailable;
    public string? ProviderId => null;
    public string? ImplementationId => null;
    public string? ContractPackageVersion => null;
    public string? ProviderPackageId => null;
    public string? ProviderPackageVersion => null;
    public string? SourceCommit => null;
    public string? SourceTree => null;
    public string? RuntimeIdentifier => null;
    public ushort? ManifestSchemaVersion => null;
    public ushort? OwnerProtocolVersion => null;
    public ushort? CoordinatorProtocolVersion => null;
    public ImmutableArray<RestrictedArtifactIdentity> Artifacts => [];
    public ImmutableArray<byte> ArtifactSetSha256 => default;
    public string? QualificationId => null;
    public string? QualificationScope => null;
}

public sealed class RestrictedOperationCreation
{
    internal RestrictedOperationCreation(RestrictedRefusalReason reason) { Reason = reason; }
    // B1 deliberately has no successful production creation path.
    public RestrictedExecutionOperation? Operation => null;
    public RestrictedRefusalReason Reason { get; }
}

/// <summary>Default-disabled selection boundary. It never consults ordinary execution
/// providers, opens sources, starts processes or allocates sandbox resources.</summary>
public sealed class UnavailableRestrictedExecutionProvider : IRestrictedExecutionProvider
{
    private readonly RestrictedProviderDescriptor _descriptor = new();
    public RestrictedProviderDescriptor Describe() => _descriptor;

    public RestrictedOperationCreation CreateOperation(RestrictedProfileSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        return new(selection.ProfileId is "ae4-linux-fixed-fixture-v1" or "restricted-offline-closure-v1"
            ? RestrictedRefusalReason.InstallationUnavailable : RestrictedRefusalReason.UnsupportedProfile);
    }

    public ValueTask<RestrictedInspection> InspectExecutionAsync(RestrictedPreparationReference reference,
        CancellationToken waitCancellation = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        waitCancellation.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new RestrictedInspection(RestrictedInspectionKind.Unavailable, null));
    }

    public ValueTask<RestrictedReconciliation> ReconcileAsync(RestrictedPreparationReference reference,
        CancellationToken waitCancellation = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        waitCancellation.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new RestrictedReconciliation(RestrictedReconciliationKind.Refused,
            RestrictedRefusalReason.RecoveryAuthorityUnavailable, RestrictedCleanup.Unknown));
    }
}
