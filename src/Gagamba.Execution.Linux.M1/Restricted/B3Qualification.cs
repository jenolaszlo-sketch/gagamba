// RC2B-03 B3 experimental qualification composition. This is the only
// trusted internal path that binds the accepted B1 restricted-execution
// control contract to the real owner transaction (real delegated cgroup,
// real staging copy, owner-local capability mint, owner-local HELD release
// arbiter). It is not a production provider, registration or arbitrary
// executable path: the fixed profile pins the owner executable and every
// staged artifact by SHA-256 before any allocation, and the production
// RestrictedExecutionRuntime.Create() stays unavailable.
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Contracts;
using Gagamba.Execution.Restricted.Control;
using Microsoft.Win32.SafeHandles;
// The manifest model tags share names with the passive view tags; the fixed
// B3 profile manifest uses the model vocabulary.
using BudgetValueKind = Gagamba.Execution.Restricted.BudgetValueKind;
using QualificationDisposition = Gagamba.Execution.Restricted.QualificationDisposition;

namespace Gagamba.Execution.Linux.M1.Restricted;

/// <summary>Fixed, trusted and bounded B3 experimental profile. Every field
/// is a pin or a fixed operational input; no caller-supplied executable,
/// argv, manifest, handle, deadline or readiness value is accepted.</summary>
internal sealed record B3FixtureProfile(
    string OwnerDllPath,
    string OwnerDllSha256,
    string DelegatedParentPath,
    string SourceRootPath,
    string StagingParentPath,
    string JournalDirectory,
    string ToolSourceName,
    string InputSourceName,
    string CoordinatorSourceName,
    string FilterSourceName,
    string ToolSha256,
    string InputSha256,
    string CoordinatorSha256,
    string ToolEnrollmentId,
    string InputEnrollmentId,
    ulong TotalBudgetBytes,
    int MaxObjects,
    long ExpiresLeadNanoseconds)
{
    internal const string ProfileId = "ae4-linux-fixed-fixture-v1";
    internal const long MaxExpiresLeadNanoseconds = 120_000_000_000L;
    internal const ulong MaxBudgetBytes = 64UL * 1024 * 1024;
    internal const int MaxObjectsBound = 128;
    internal const ulong MaxObjectBytes = 16UL * 1024 * 1024;
}

internal sealed record B3QualificationCreation(RestrictedExecutionOperation? Operation,
    RestrictedRefusalReason Reason);

/// <summary>Trusted internal qualification entry point. It verifies the
/// executable, artifact, protocol and deployment identities, then returns one
/// B1 operation bound to the real owner transaction. All refusals are typed
/// and precede every allocation.</summary>
internal static class B3Qualification
{
    internal static B3QualificationCreation TryCreate(B3FixtureProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        try
        {
            if (!OperatingSystem.IsLinux() || !RecoveryInterop.Supported || !RecoveryInterop.IsUnprivileged)
            {
                return new B3QualificationCreation(null, RestrictedRefusalReason.UnqualifiedInstallation);
            }

            // In this WSL2 development environment the fixture topology runs
            // the owner as an unprivileged direct child of the caller; the
            // uid check keeps the qualification bound to that topology.
            if (RecoveryInterop.GetUid() == 0)
            {
                return new B3QualificationCreation(null, RestrictedRefusalReason.UnqualifiedInstallation);
            }

            foreach (var path in new[] { profile.OwnerDllPath, profile.DelegatedParentPath, profile.SourceRootPath,
                         profile.StagingParentPath, profile.JournalDirectory })
            {
                if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
                {
                    return new B3QualificationCreation(null, RestrictedRefusalReason.InvalidBinding);
                }
            }

            foreach (var name in new[] { profile.ToolSourceName, profile.InputSourceName,
                         profile.CoordinatorSourceName, profile.FilterSourceName })
            {
                if (string.IsNullOrEmpty(name) || name is "." or ".." || name.Contains('/') || name.Contains('\\'))
                {
                    return new B3QualificationCreation(null, RestrictedRefusalReason.InvalidBinding);
                }
            }

            if (profile.ExpiresLeadNanoseconds <= 0
                || profile.ExpiresLeadNanoseconds > B3FixtureProfile.MaxExpiresLeadNanoseconds
                || profile.TotalBudgetBytes == 0 || profile.TotalBudgetBytes > B3FixtureProfile.MaxBudgetBytes
                || profile.MaxObjects is < 4 or > B3FixtureProfile.MaxObjectsBound)
            {
                return new B3QualificationCreation(null, RestrictedRefusalReason.InvalidBinding);
            }

            if (!File.Exists(profile.OwnerDllPath)
                || !VerifySha256(profile.OwnerDllPath, profile.OwnerDllSha256)
                || !Directory.Exists(profile.DelegatedParentPath)
                || !Directory.Exists(profile.SourceRootPath)
                || !Directory.Exists(profile.StagingParentPath))
            {
                return new B3QualificationCreation(null, RestrictedRefusalReason.InstallationUnavailable);
            }

            // Independently verify every source artifact against its pin and
            // the compile-time seccomp pin before any staging is requested.
            if (!VerifySha256(Path.Combine(profile.SourceRootPath, profile.ToolSourceName), profile.ToolSha256)
                || !VerifySha256(Path.Combine(profile.SourceRootPath, profile.InputSourceName), profile.InputSha256)
                || !VerifySha256(Path.Combine(profile.SourceRootPath, profile.CoordinatorSourceName), profile.CoordinatorSha256))
            {
                return new B3QualificationCreation(null, RestrictedRefusalReason.InstallationUnavailable);
            }

            var filterPath = Path.Combine(profile.SourceRootPath, profile.FilterSourceName);
            if (!File.Exists(filterPath) || new FileInfo(filterPath).Length != (long)BubblewrapAdapter.FilterLength
                || !VerifySha256(filterPath, BubblewrapAdapter.FilterSha256))
            {
                return new B3QualificationCreation(null, RestrictedRefusalReason.InstallationUnavailable);
            }

            // Protocol identities: the fixed B3 versions compiled into both
            // sides must be the accepted wire versions.
            if (!WireIdentitiesMatch())
            {
                return new B3QualificationCreation(null, RestrictedRefusalReason.UnqualifiedInstallation);
            }

            var provider = new RestrictedProviderDescriptor();
            var owner = new B3PreparationOwner(profile, provider);
            return new B3QualificationCreation(
                new RestrictedExecutionOperation(provider, owner), RestrictedRefusalReason.None);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
            or NotSupportedException or CryptographicException)
        {
            return new B3QualificationCreation(null, RestrictedRefusalReason.InstallationUnavailable);
        }
    }

    internal static bool WireIdentitiesMatch() =>
        OwnerProtocol.CurrentVersion == 1
        && CoordinatorProtocol.HeaderBytes == 96
        && CoordinatorProtocol.ReleaseFrameBytes == 128
        && CoordinatorProtocol.ReleaseSequence == 1;

    internal static byte[] DecodeHex(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length % 2 != 0)
        {
            throw new ArgumentException("Hex digest length is invalid.", nameof(value));
        }

        var bytes = new byte[value.Length / 2];
        for (var index = 0; index < bytes.Length; index++)
        {
            bytes[index] = byte.Parse(value.AsSpan(index * 2, 2), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture);
        }

        return bytes;
    }

    internal static bool VerifySha256(string path, string expectedHex)
    {
        if (expectedHex.Length != 64 || !expectedHex.All(Uri.IsHexDigit) || !File.Exists(path))
        {
            return false;
        }

        using var stream = File.OpenRead(path);
        var digest = SHA256.HashData(stream);
        return Convert.ToHexString(digest).Equals(expectedHex, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>The B3 owner bridge: prepares through the authoritative owner
/// transaction, retains the prepared view bound to the original operation,
/// and submits release/stop only over the controller-owned association. The
/// live capability, capability store and RELEASE writer remain owner-local;
/// this object never receives them.</summary>
internal sealed class B3PreparationOwner : IRestrictedOperationOwner, IDisposable
{
    private readonly object _gate = new();
    private readonly B3FixtureProfile _profile;
    private readonly RestrictedProviderDescriptor _provider;
    private readonly M1RecoveryScope _scope = new();
    private readonly OwnershipJournal _journal;
    private OwnerPreparationOutcome? _held;
    private OwnerPreparedDescription? _description;
    private RestrictedManifest? _manifest;
    private RestrictedOwnerPreparation? _source;
    private ImmutableArray<byte> _admittedCorrelation;
    private bool _hasAdmittedCorrelation;
    private RestrictedReleaseDecision? _retained;
    private RestrictedOwnerExecutionFacts? _facts;
    private Task<RestrictedStopReply>? _stopTask;
    private int _disposed;
    private long _acceptedAtNs;
    private long _expiresAtNs;
    private RestrictedCleanup? _cleanup;

    internal B3PreparationOwner(B3FixtureProfile profile, RestrictedProviderDescriptor provider)
    {
        _profile = profile;
        _provider = provider;
        _journal = new OwnershipJournal(new JournalFileStorage(), profile.JournalDirectory);
        if (!Directory.Exists(profile.JournalDirectory))
        {
            Directory.CreateDirectory(profile.JournalDirectory);
        }
    }

    internal RestrictedProviderDescriptor Descriptor => _provider;

    public RestrictedReleaseDecision? RetainedRelease
    {
        get
        {
            lock (_gate)
            {
                return _retained;
            }
        }
    }

    public bool IsLive(RestrictedPreparationReference reference)
    {
        if (reference is null)
        {
            return false;
        }

        lock (_gate)
        {
            var held = _held;
            var description = _description;
            if (held?.Owner is null || description is null)
            {
                return false;
            }

            if (reference.ExecutionId != description.ExecutionId
                || reference.GenerationId != description.GenerationId
                || reference.PreparationId != description.PreparationId
                || !reference.ManifestSha256.AsSpan().SequenceEqual(description.ManifestDigest))
            {
                return false;
            }

            return _scope.TryOwnerPidFdExited(held.Owner, 0, out var exited, out _) && !exited;
        }
    }

    public ValueTask<RestrictedOwnerPrepareResult> PrepareAsync(RestrictedPreparationInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        return new(Task.Run(() => PrepareCore(inputs)));
    }

    public ValueTask<RestrictedOwnerReleaseResult> ReleaseAsync(ImmutableArray<byte> approvalCorrelationSha256) =>
        new(Task.Run(() => ReleaseCore(approvalCorrelationSha256)));

    public ValueTask<RestrictedStopReply> StopAsync(RestrictedStopReason reason)
    {
        var task = Task.Run(() => StopCore(reason));
        lock (_gate)
        {
            _stopTask = task;
        }

        return new(task);
    }

    public ValueTask<RestrictedCleanup> DisposeAsync() => new(Task.Run(DisposeCore));

    private RestrictedOwnerPrepareResult PrepareCore(RestrictedPreparationInputs inputs)
    {
        if (inputs.ToolEnrollmentId != _profile.ToolEnrollmentId
            || inputs.InputEnrollmentId != _profile.InputEnrollmentId)
        {
            return new RestrictedOwnerPrepareResult(RestrictedPrepareKind.Failed);
        }

        try
        {
            if (!_scope.TryInitialize().Ok
                || !_scope.TryOpenDelegatedParent(_profile.DelegatedParentPath).Ok
                || !CgroupInterop.TryReadBootId(out var bootId, out _)
                || !Guid.TryParse(bootId, out var bootGuid))
            {
                return new RestrictedOwnerPrepareResult(RestrictedPrepareKind.Failed);
            }

            if (!_journal.TryAcquireWriter(out var writer, out _) || writer is null)
            {
                return new RestrictedOwnerPrepareResult(RestrictedPrepareKind.Failed);
            }

            var executionId = Guid.NewGuid();
            var leaseGeneration = Guid.NewGuid();
            var issuerId = Guid.NewGuid();
            var storeGenerationId = Guid.NewGuid();
            var preparationId = Guid.NewGuid();
            var domainChildName = "b3-" + executionId.ToString("N");
            _acceptedAtNs = MonotonicNowNanoseconds();
            _expiresAtNs = _acceptedAtNs + _profile.ExpiresLeadNanoseconds;
            RestrictedManifest? manifest = null;
            OwnerPreparationOutcome outcome;
            try
            {
                var request = new OwnerPreparationRequest(
                    _profile.OwnerDllPath,
                    domainChildName,
                    executionId,
                    leaseGeneration,
                    bootId,
                    BuildPlan(),
                    _journal,
                    writer,
                    issuerId,
                    storeGenerationId,
                    staged =>
                    {
                        var authorized = BuildManifest(staged, executionId, issuerId, storeGenerationId, preparationId);
                        manifest = authorized.Manifest;
                        return authorized;
                    },
                    _expiresAtNs,
                    TestBarrier: null,
                    WithholdStagingRetentionAckForTest: false,
                    EnableHeldCoordinator: true);
                outcome = _scope.StartOwnerPreparation(request, CancellationToken.None);
            }
            finally
            {
                // The handoff disposes this lease; a failed start releases it here.
                writer.Dispose();
            }

            if (!outcome.Ok || !outcome.Held || outcome.HeldReport is null
                || outcome.CoordinatorStatusFrame is null || outcome.Description is null
                || !outcome.NoTargetInitializationMarkerAbsent
                || !outcome.OwnerSetupDeadlineNs.HasValue || !outcome.OwnerOverallDeadlineNs.HasValue
                || manifest is null)
            {
                return new RestrictedOwnerPrepareResult(RestrictedPrepareKind.Failed);
            }

            // Independent retained-authority evidence: exactly one recovery
            // lease corroborating this domain with the WP04 limits readable.
            var lease = _scope.RetainedRecoveryLeases.Count == 1 ? _scope.RetainedRecoveryLeases[0] : null;
            if (lease is null || !lease.AuthorityAlive || !lease.VerifyIdentity().Match
                || !lease.TryReadWp04Limits(out var limits, out _)
                || limits.GetValueOrDefault("memory.max") != "134217728"
                || limits.GetValueOrDefault("memory.swap.max") != "0"
                || limits.GetValueOrDefault("pids.max") != "48")
            {
                return new RestrictedOwnerPrepareResult(RestrictedPrepareKind.Failed);
            }

            // Re-verify the owner-origin HELD report independently: raised
            // privileges removed, seccomp active, private namespaces, exact
            // mount tree and the retained domain cgroup path.
            var report = outcome.HeldReport;
            if (report.Uid != 0 || report.EffectiveUid != 0 || report.Gid != 0 || report.EffectiveGid != 0
                || report.NoNewPrivs != 1 || report.SeccompMode != 2 || report.Dumpable != 0
                || report.CapEff != 0 || report.CapPrm != 0 || report.CapAmb != 0
                || !CoordinatorMountVerifier.HasPrivateNamespaces(report.NamespaceInodes, out _)
                || !CoordinatorMountVerifier.Validate(report.MountInfo, out _)
                || !report.Cgroup.TrimEnd('\n', '\r').EndsWith("/" + domainChildName, StringComparison.Ordinal))
            {
                return new RestrictedOwnerPrepareResult(RestrictedPrepareKind.Failed);
            }

            var description = outcome.Description;
            var reference = new RestrictedPreparationReference(
                description.ExecutionId, description.IssuerId, description.GenerationId,
                description.PreparationId, description.ManifestDigest);
            if (reference.GenerationId != storeGenerationId
                || outcome.OwnerSetupDeadlineNs.Value <= _acceptedAtNs
                || outcome.OwnerOverallDeadlineNs.Value <= outcome.OwnerSetupDeadlineNs.Value)
            {
                return new RestrictedOwnerPrepareResult(RestrictedPrepareKind.Failed);
            }

            var deadlines = new RestrictedDeadlines(
                _acceptedAtNs, _expiresAtNs,
                outcome.OwnerSetupDeadlineNs.Value, outcome.OwnerOverallDeadlineNs.Value,
                "owner-monotonic-stopwatch-v1", bootGuid, leaseGeneration,
                (ulong)Stopwatch.Frequency, description.GenerationId);
            var source = new RestrictedOwnerPreparation(reference, new RestrictedManifestView(manifest),
                _provider, deadlines, new RestrictedEstablishmentReceipt(1, outcome.CoordinatorStatusFrame));
            lock (_gate)
            {
                _held = outcome;
                _description = description;
                _manifest = manifest;
                _source = source;
            }

            return new RestrictedOwnerPrepareResult(RestrictedPrepareKind.Prepared, source);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException
            or OverflowException or CryptographicException)
        {
            return new RestrictedOwnerPrepareResult(RestrictedPrepareKind.Failed);
        }
    }

    private CopyPlan BuildPlan() => new(
        _profile.SourceRootPath,
        _profile.StagingParentPath,
        "prep-001",
        new[]
        {
            new CopyObjectPlan("tool", _profile.ToolSourceName, B3FixtureProfile.MaxObjectBytes,
                B3Qualification.DecodeHex(_profile.ToolSha256)),
            new CopyObjectPlan("input", _profile.InputSourceName, B3FixtureProfile.MaxObjectBytes,
                B3Qualification.DecodeHex(_profile.InputSha256)),
            new CopyObjectPlan("coordinator", _profile.CoordinatorSourceName, B3FixtureProfile.MaxObjectBytes,
                B3Qualification.DecodeHex(_profile.CoordinatorSha256)),
            new CopyObjectPlan("filter", _profile.FilterSourceName, B3FixtureProfile.MaxObjectBytes,
                B3Qualification.DecodeHex(BubblewrapAdapter.FilterSha256)),
        },
        _profile.TotalBudgetBytes,
        _profile.MaxObjects,
        Array.Empty<KeyValuePair<string, string>>(),
        Acknowledge: null,
        Hooks: null);

    private (RestrictedManifest Manifest, PreparationIdentity Identity) BuildManifest(
        IReadOnlyList<StagedObjectDescription> staged,
        Guid executionId, Guid issuerId, Guid storeGenerationId, Guid preparationId)
    {
        if (staged.Count != 4)
        {
            throw new InvalidOperationException("B3 closure requires exactly the four fixed objects");
        }

        var byRole = staged.ToDictionary(item => item.Role, StringComparer.Ordinal);
        if (byRole.Count != 4
            || !new[] { "tool", "input", "coordinator", "filter" }.All(byRole.ContainsKey))
        {
            throw new InvalidOperationException("B3 closure roles do not match the fixed profile");
        }

        // Independently verify the prepared source identities against the pins.
        VerifyStaged(byRole["tool"], _profile.ToolSha256);
        VerifyStaged(byRole["input"], _profile.InputSha256);
        VerifyStaged(byRole["coordinator"], _profile.CoordinatorSha256);
        VerifyStaged(byRole["filter"], BubblewrapAdapter.FilterSha256);
        if (byRole["filter"].Length != BubblewrapAdapter.FilterLength)
        {
            throw new InvalidOperationException("B3 filter length does not match the pinned policy");
        }

        var closure = new ClosureIdentity(
            new[]
            {
                new ClosureObject("tool", byRole["tool"].Sha256, byRole["tool"].Length, "/tool", "b3-static-fixture"),
                new ClosureObject("input", byRole["input"].Sha256, byRole["input"].Length, "/input", "b3-static-fixture"),
                new ClosureObject("coordinator", byRole["coordinator"].Sha256, byRole["coordinator"].Length,
                    "/coordinator", "b3-static-fixture"),
                new ClosureObject("filter", byRole["filter"].Sha256, byRole["filter"].Length, "fd:6",
                    "p07-seccomp-filter"),
            },
            new[]
            {
                "bubblewrap-0.11.1:" + HelperInstallation.PinnedSha256,
                "seccomp-policy:" + BubblewrapAdapter.FilterSha256,
            });
        var manifest = new RestrictedManifest(
            RestrictedManifest.CurrentSchemaVersion,
            RestrictedManifest.ImplementationIdentifier,
            new AuthorityManifest(
                new[] { "uts", "net", "mount", "ipc", "pid", "user" },
                Array.Empty<string>(),
                true,
                B3Qualification.DecodeHex(BubblewrapAdapter.FilterSha256),
                BubblewrapAdapter.FilterLength,
                new[] { "/tool", "/input", "/coordinator" },
                new[]
                {
                    new WritableStore("/dev/shm", 1048576, false, 0),
                    new WritableStore("/tmp", 7340032, false, 0),
                },
                true,
                true,
                true,
                new ExecutableMountPolicy(true, false),
                QualificationDisposition.PendingQualification),
            FixedResources,
            closure,
            new InvocationManifest(
                "/tool",
                "work",
                "/input",
                new[] { "--in", "/input" },
                new[]
                {
                    new KeyValuePair<string, string>("LANG", "C"),
                    new KeyValuePair<string, string>("HOME", "/tmp"),
                },
                "/tmp"),
            new ChannelManifest(true, 65536, 65536, 131072));
        var identity = new PreparationIdentity(executionId, issuerId, storeGenerationId, preparationId,
            ManifestCodec.ComputeDigestBytes(manifest));
        return (manifest, identity);
    }

    private static readonly ResourceManifest FixedResources = new(
        new ResourceBudget(BudgetValueKind.Exact, 134217728, "bytes", QualificationDisposition.Mandatory),
        new ResourceBudget(BudgetValueKind.Exact, 0, "bytes", QualificationDisposition.Mandatory),
        new ResourceBudget(BudgetValueKind.Exact, 48, "tasks", QualificationDisposition.Mandatory),
        new ResourceBudget(BudgetValueKind.Exact, 64, "fds", QualificationDisposition.Mandatory),
        new ResourceBudget(BudgetValueKind.Exact, 7340032, "bytes", QualificationDisposition.Mandatory),
        new ResourceBudget(BudgetValueKind.Exact, 1048576, "bytes", QualificationDisposition.Mandatory),
        new ResourceBudget(BudgetValueKind.Exact, 65536, "bytes", QualificationDisposition.Mandatory),
        new ResourceBudget(BudgetValueKind.Exact, 65536, "bytes", QualificationDisposition.Mandatory),
        new ResourceBudget(BudgetValueKind.Exact, 131072, "bytes", QualificationDisposition.Mandatory),
        new ResourceBudget(BudgetValueKind.Exact, 10000000000, "ns", QualificationDisposition.Mandatory),
        new ResourceBudget(BudgetValueKind.Exact, 30000000000, "ns", QualificationDisposition.Mandatory),
        new ResourceBudget(BudgetValueKind.Exact, 5000000000, "ns", QualificationDisposition.Mandatory),
        new ResourceBudget(BudgetValueKind.Exact, 1000000000, "ns", QualificationDisposition.Mandatory),
        QualificationDisposition.PendingQualification,
        new CoreDumpPolicy(true, false, null),
        new OwnerAllocationLimits(16777216, 128, 16384, 16384, 16384, 65536, 32768));

    private static void VerifyStaged(StagedObjectDescription staged, string expectedHex)
    {
        var expected = B3Qualification.DecodeHex(expectedHex);
        if (staged.Sha256 is not { Length: 32 } || !staged.Sha256.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException($"staged object '{staged.Role}' does not match its pinned identity");
        }
    }

    private RestrictedOwnerReleaseResult ReleaseCore(ImmutableArray<byte> correlation)
    {
        OwnerPreparationOutcome? held;
        lock (_gate)
        {
            held = _held;
        }

        if (held is not { Held: true, Owner: not null } || _disposed != 0)
        {
            return RefusedResult(RestrictedRefusalReason.OwnerUnavailable);
        }

        var outcome = _scope.ReleaseHeld(held, correlation);
        if (!outcome.Accepted || outcome.Decision is null)
        {
            return new RestrictedOwnerReleaseResult(RestrictedReleaseKind.Unknown,
                RestrictedReleaseEvidence.Unknown, RestrictedDelivery.Unknown, RestrictedEvidenceState.Unknown,
                null, RestrictedRequestBinding.Unknown, RestrictedRefusalReason.OwnerUnavailable);
        }

        var decision = outcome.Decision;
        var facts = outcome.Outcome is null ? null : MapFacts(outcome.Outcome);
        lock (_gate)
        {
            _facts = facts ?? _facts;
            if (IsRetained(decision))
            {
                if (!_hasAdmittedCorrelation)
                {
                    _admittedCorrelation = correlation.IsDefault
                        ? default
                        : ImmutableArray.Create(correlation.AsSpan());
                    _hasAdmittedCorrelation = true;
                }

                _retained ??= BuildPublicDecision(decision);
            }
        }

        return new RestrictedOwnerReleaseResult(
            MapKind(decision.Kind),
            MapEvidence(decision),
            MapDelivery(decision.Delivery),
            decision.DurableCommitted ? RestrictedEvidenceState.Confirmed : RestrictedEvidenceState.Unknown,
            decision.LiveExecutionExcluded,
            MapBinding(decision.Binding),
            decision.RefusalReason,
            facts);
    }

    private RestrictedStopReply StopCore(RestrictedStopReason reason)
    {
        OwnerPreparationOutcome? held;
        lock (_gate)
        {
            held = _held;
        }

        if (held is not { Held: true, Owner: not null })
        {
            // No owner runtime exists to accept a stop; the local latch still
            // prevents publication and release.
            return new RestrictedStopReply(RestrictedStopSubmission.Refused, false, reason);
        }

        var outcome = _scope.StopHeld(held, MapCause(reason));
        if (!outcome.Accepted || outcome.Decision is null)
        {
            return new RestrictedStopReply(RestrictedStopSubmission.Unknown, false, reason);
        }

        var decision = outcome.Decision;
        if (!decision.Accepted || decision.Duplicate
            || decision.StopAcceptedAtNanoseconds <= 0 || decision.StopDeadlineNanoseconds <= 0
            || decision.StopDeadlineNanoseconds <= decision.StopAcceptedAtNanoseconds)
        {
            return new RestrictedStopReply(RestrictedStopSubmission.Unknown, false, reason);
        }

        return new RestrictedStopReply(RestrictedStopSubmission.RuntimeAccepted, false, reason,
            decision.ReleaseAlreadyCommitted, decision.StopAcceptedAtNanoseconds, decision.StopDeadlineNanoseconds,
            decision.DurableStop ? RestrictedEvidenceState.Confirmed : RestrictedEvidenceState.Unknown);
    }

    private RestrictedCleanup DisposeCore()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            lock (_gate)
            {
                return _cleanup ?? RestrictedCleanup.Unknown;
            }
        }

        OwnerPreparationOutcome? held;
        Task<RestrictedStopReply>? pendingStop;
        lock (_gate)
        {
            held = _held;
            pendingStop = _stopTask;
        }

        // A disposal-requested stop and the abort share one sequenced owner
        // channel: wait for the submitted stop exchange to finish (bounded)
        // before the abort exchange starts, so frames can never interleave.
        if (pendingStop is not null)
        {
            try
            {
                pendingStop.Wait(TimeSpan.FromSeconds(20));
            }
            catch
            {
                // The stop outcome is already recorded as Unknown by StopCore.
            }
        }

        HeldOwnerAbortOutcome abort;
        try
        {
            abort = held is { Held: true, Owner: not null }
                ? _scope.AbortHeld(held)
                : new HeldOwnerAbortOutcome(false, false, false, "no held owner to abort");
        }
        catch (Exception)
        {
            abort = new HeldOwnerAbortOutcome(false, false, false, "held abort failed");
        }

        var reaped = abort.OwnerExited
            ? RestrictedEvidenceState.Confirmed
            : RestrictedEvidenceState.Unknown;
        RestrictedCleanup cleanup;
        try
        {
            cleanup = Reconcile(reaped);
        }
        catch (Exception)
        {
            cleanup = new RestrictedCleanup(RestrictedEvidenceState.Unknown, reaped,
                RestrictedEvidenceState.Unknown, RestrictedEvidenceState.Unknown);
        }
        finally
        {
            _scope.Dispose();
        }

        lock (_gate)
        {
            _cleanup = cleanup;
        }

        return cleanup;
    }

    private RestrictedCleanup Reconcile(RestrictedEvidenceState reaped)
    {
        var read = _journal.ReadLatest();
        if (!read.Ok || read.Snapshot is null || !CgroupInterop.TryReadBootId(out var bootId, out _))
        {
            return new RestrictedCleanup(RestrictedEvidenceState.Unknown, reaped,
                RestrictedEvidenceState.Unknown, RestrictedEvidenceState.Unknown);
        }

        if (!_journal.TryAcquireWriter(out var writer, out _) || writer is null)
        {
            return new RestrictedCleanup(RestrictedEvidenceState.Unknown, reaped,
                RestrictedEvidenceState.Unknown, RestrictedEvidenceState.Unknown);
        }

        using (writer)
        {
            var lease = _scope.RetainedRecoveryLeases.Count == 1 ? _scope.RetainedRecoveryLeases[0] : null;
            var cleaner = BuildCleaner();
            var reconciler = new Reconciler(new RealDomainClock(), TimeSpan.FromSeconds(5));
            var result = reconciler.Reconcile(read.Snapshot, bootId, lease, cleaner, _journal, writer);
            return new RestrictedCleanup(
                Component(result, CleanupComponents.DomainEmpty),
                reaped,
                Component(result, CleanupComponents.Staging),
                Component(result, CleanupComponents.JournalFinalization));
        }
    }

    private static RestrictedEvidenceState Component(ReconcileResult result, CleanupComponents component)
    {
        var outcome = result.Components?.FirstOrDefault(item => item.Component == component);
        return outcome?.State switch
        {
            ComponentState.Confirmed => RestrictedEvidenceState.Confirmed,
            ComponentState.Failed => RestrictedEvidenceState.Failed,
            _ => RestrictedEvidenceState.Unknown,
        };
    }

    private IAllocationCleaner? BuildCleaner()
    {
        if (_scope.RetainedStagingParents.Count != 1 || _scope.RetainedStagingDirectories.Count != 1)
        {
            return null;
        }

        return new B3ScopeCleaner(
            _scope.RetainedStagingParents[0].DangerousGetHandle().ToInt32(),
            _scope.RetainedStagingDirectories[0].DangerousGetHandle().ToInt32());
    }

    private RestrictedOwnerExecutionFacts MapFacts(OwnerExecutionFacts facts)
    {
        RestrictedExecResult? exec = null;
        if (facts.Exec == ExecTag.Confirmed)
        {
            exec = new RestrictedExecResult(RestrictedExecKind.Confirmed,
                confirmationBasis: "coordinator-exec-confirmed-frame");
        }
        else if (facts.Exec == ExecTag.FailedBeforeExec && facts.ExecErrno > 0)
        {
            exec = new RestrictedExecResult(RestrictedExecKind.FailedBeforeExec, errno: facts.ExecErrno);
        }

        RestrictedProcessResult? root = null;
        if (facts.RawWaitPresent && facts.Root == RootKind.Exited)
        {
            root = new RestrictedProcessResult(RestrictedProcessKind.Exited, facts.ExitCode,
                rawWaitStatus: facts.RawWaitStatus);
        }
        else if (facts.RawWaitPresent && facts.Root == RootKind.Signalled && facts.Signal > 0)
        {
            root = new RestrictedProcessResult(RestrictedProcessKind.Signalled, signal: facts.Signal,
                coreDumped: facts.CoreDumped, rawWaitStatus: facts.RawWaitStatus);
        }

        var domain = facts.DomainKnown
            ? facts.DomainPopulated ? RestrictedDomainState.Populated : RestrictedDomainState.Empty
            : RestrictedDomainState.Unknown;
        return new RestrictedOwnerExecutionFacts(facts.Outcome, exec, root, Helper: null, domain,
            facts.ObservedAtNanoseconds > 0 ? facts.ObservedAtNanoseconds : null);
    }

    private RestrictedReleaseDecision BuildPublicDecision(ReleaseDecision decision) =>
        new(MapKind(decision.Kind), MapEvidence(decision), MapDelivery(decision.Delivery),
            decision.DurableCommitted ? RestrictedEvidenceState.Confirmed : RestrictedEvidenceState.Unknown,
            decision.LiveExecutionExcluded, _admittedCorrelation, RestrictedEvidenceState.Unknown);

    private static bool IsRetained(ReleaseDecision decision) =>
        decision.Kind is ReleaseDecisionKind.Released or ReleaseDecisionKind.Stopped
            or ReleaseDecisionKind.AmbiguousPersistence
        || decision.Kind == ReleaseDecisionKind.Refused
            && decision.DurableRelease == ReleaseDisposition.MayHaveExecuted;

    private static RestrictedReleaseKind MapKind(ReleaseDecisionKind kind) => kind switch
    {
        ReleaseDecisionKind.Refused => RestrictedReleaseKind.Refused,
        ReleaseDecisionKind.Released => RestrictedReleaseKind.Released,
        ReleaseDecisionKind.Duplicate => RestrictedReleaseKind.Duplicate,
        ReleaseDecisionKind.Stopped => RestrictedReleaseKind.Stopped,
        ReleaseDecisionKind.AmbiguousPersistence => RestrictedReleaseKind.AmbiguousPersistence,
        _ => RestrictedReleaseKind.Unknown,
    };

    private static RestrictedReleaseEvidence MapEvidence(ReleaseDecision decision) =>
        decision.DurableCommitted ? RestrictedReleaseEvidence.Committed
        : decision.DurableRelease == ReleaseDisposition.NotCommitted
            ? RestrictedReleaseEvidence.NotCommitted
            : RestrictedReleaseEvidence.MayHaveExecuted;

    private static RestrictedDelivery MapDelivery(DeliveryResult delivery) => delivery switch
    {
        DeliveryResult.NotAttempted => RestrictedDelivery.NotAttempted,
        DeliveryResult.Delivered => RestrictedDelivery.Delivered,
        DeliveryResult.WouldBlock => RestrictedDelivery.WouldBlock,
        DeliveryResult.BrokenPipe => RestrictedDelivery.BrokenPipe,
        DeliveryResult.Partial => RestrictedDelivery.Partial,
        _ => RestrictedDelivery.Unknown,
    };

    private static RestrictedRequestBinding MapBinding(ReleaseCorrelationBinding binding) => binding switch
    {
        ReleaseCorrelationBinding.Accepted => RestrictedRequestBinding.Accepted,
        ReleaseCorrelationBinding.Mismatch => RestrictedRequestBinding.Mismatch,
        ReleaseCorrelationBinding.InvalidArgument => RestrictedRequestBinding.InvalidArgument,
        _ => RestrictedRequestBinding.Unknown,
    };

    private static StopCauseKind MapCause(RestrictedStopReason reason) => reason switch
    {
        RestrictedStopReason.CallerStop => StopCauseKind.CallerStop,
        RestrictedStopReason.Deadline => StopCauseKind.SetupExpired,
        RestrictedStopReason.Disposal => StopCauseKind.CallerLoss,
        RestrictedStopReason.Invalidation => StopCauseKind.SetupFault,
        _ => StopCauseKind.CallerStop,
    };

    private static RestrictedOwnerReleaseResult RefusedResult(RestrictedRefusalReason reason) =>
        new(RestrictedReleaseKind.Refused, RestrictedReleaseEvidence.NotCommitted,
            RestrictedDelivery.NotAttempted, RestrictedEvidenceState.Unknown, null,
            RestrictedRequestBinding.Unknown, reason);

    private static long MonotonicNowNanoseconds()
    {
        var timestamp = Stopwatch.GetTimestamp();
        return (timestamp / Stopwatch.Frequency * 1_000_000_000L)
            + (timestamp % Stopwatch.Frequency * 1_000_000_000L / Stopwatch.Frequency);
    }

    public void Dispose() => DisposeCore();

    private sealed class RealDomainClock : IDomainClock
    {
        public long NowNanoseconds => MonotonicNowNanoseconds();

        public void Delay(TimeSpan duration) => Thread.Sleep(duration);
    }
}

/// <summary>Identity-checked staging cleanup over the recovery scope's
/// retained parent and staging-directory handles. It never removes by an
/// arbitrary path and refuses any device/inode mismatch.</summary>
internal sealed class B3ScopeCleaner : IAllocationCleaner
{
    private readonly int _parentFd;
    private readonly int _stagingDirFd;

    internal B3ScopeCleaner(int parentFd, int stagingDirFd)
    {
        _parentFd = parentFd;
        _stagingDirFd = stagingDirFd;
    }

    public AllocationCleanResult CleanOne(AllocationEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Kind == AllocationKind.StagingDirectory)
        {
            if (PreparationInterop.StatFd(_stagingDirFd, out var current) != 0
                || current.Device != entry.Device || current.Inode != entry.Inode)
            {
                return new AllocationCleanResult(AllocationCleanOutcome.Mismatch,
                    "staging directory identity mismatch");
            }

            var errno = PreparationInterop.RemoveDirectory(_parentFd, entry.Name);
            return errno == 0
                ? new AllocationCleanResult(AllocationCleanOutcome.Cleaned, string.Empty)
                : new AllocationCleanResult(AllocationCleanOutcome.Failed, $"rmdir errno {errno}");
        }

        if (entry.Kind != AllocationKind.Staging)
        {
            return new AllocationCleanResult(AllocationCleanOutcome.Failed,
                $"unexpected entry kind {entry.Kind}");
        }

        using var directory = new SafeFileHandle(new IntPtr(_stagingDirFd), ownsHandle: false);
        var result = StagingCleanup.CleanOne(LibcCopyHost.Instance, directory, entry.Name,
            entry.Device, entry.Inode);
        return result.Outcome switch
        {
            CleanupItemOutcome.Removed => new AllocationCleanResult(AllocationCleanOutcome.Cleaned, string.Empty),
            CleanupItemOutcome.AlreadyAbsent => new AllocationCleanResult(AllocationCleanOutcome.AlreadyAbsent, string.Empty),
            CleanupItemOutcome.IdentityMismatch => new AllocationCleanResult(AllocationCleanOutcome.Mismatch, result.Error),
            _ => new AllocationCleanResult(AllocationCleanOutcome.Failed, result.Error),
        };
    }
}
