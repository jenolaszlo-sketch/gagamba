// WP01 value contracts: immutable manifest vocabulary and the canonical
// codec for the internal linux-m1-native-transaction preparation identity.
// All types are internal; no public API or provider registration is added.
// Fail-closed construction: required invariants are enforced in explicit
// constructors (records carry no primary constructors so validation cannot
// be bypassed), so an invalid manifest cannot be represented.
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Gagamba.Execution.Linux.M1")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Gagamba.Execution.Linux.M1.Tests")]

namespace Gagamba.Execution.Restricted;

/// <summary>Mandatory versus explicitly pending qualification.</summary>
internal enum QualificationDisposition : byte
{
    Mandatory = 0,
    PendingQualification = 1,
}

/// <summary>How a budget value is expressed. Absence, zero (Exact 0) and
/// unlimited are distinct; pending is carried by <see cref="QualificationDisposition"/>.</summary>
internal enum BudgetValueKind : byte
{
    Absent = 0,
    Exact = 1,
    Unlimited = 2,
}

/// <summary>One resource budget with units and qualification disposition.
/// Absent and Unlimited require a canonical zero value.</summary>
internal sealed record ResourceBudget
{
    public ResourceBudget(BudgetValueKind kind, ulong value, string unit, QualificationDisposition qualification)
    {
        ArgumentException.ThrowIfNullOrEmpty(unit);
        if ((kind is BudgetValueKind.Absent or BudgetValueKind.Unlimited) && value != 0)
        {
            throw new ArgumentException("Absent and Unlimited budgets require a canonical zero value.", nameof(value));
        }

        Kind = kind;
        Value = value;
        Unit = unit;
        Qualification = qualification;
    }

    public BudgetValueKind Kind { get; }
    public ulong Value { get; }
    public string Unit { get; }
    public QualificationDisposition Qualification { get; }
}

/// <summary>One writable store in the authority inventory.</summary>
internal sealed record WritableStore
{
    public WritableStore(string path, ulong bytes, bool hasInodeBudget, ulong inodeBudget)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!hasInodeBudget && inodeBudget != 0)
        {
            throw new ArgumentException("A store without an inode budget requires a canonical zero value.", nameof(inodeBudget));
        }

        Path = path;
        Bytes = bytes;
        HasInodeBudget = hasInodeBudget;
        InodeBudget = inodeBudget;
    }

    public string Path { get; }
    public ulong Bytes { get; }
    public bool HasInodeBudget { get; }
    public ulong InodeBudget { get; }
}

/// <summary>Executable-mount policy: the tool must be executable for any
/// execution to occur; scratch noexec is recorded as observed, never
/// required or claimed by M1.</summary>
internal sealed record ExecutableMountPolicy
{
    public ExecutableMountPolicy(bool toolExecutable, bool scratchNoexec)
    {
        if (!toolExecutable)
        {
            throw new ArgumentException("The tool must be executable.", nameof(toolExecutable));
        }

        ToolExecutable = toolExecutable;
        ScratchNoexec = scratchNoexec;
    }

    public bool ToolExecutable { get; }
    public bool ScratchNoexec { get; }
}

/// <summary>Authority manifest: required namespaces, empty capability set,
/// no-new-privs, filter identity, read-only destinations, writable-store
/// inventory, executable-mount policy, network qualification and denied
/// ambient authority. The capability set must be empty, network qualification
/// must remain pending, and the three denial flags must hold; a violating
/// manifest cannot be constructed.</summary>
internal sealed record AuthorityManifest
{
    public AuthorityManifest(
        IEnumerable<string> namespaces,
        IEnumerable<string> capabilities,
        bool noNewPrivs,
        byte[] filterSha256,
        ulong filterLength,
        IEnumerable<string> readOnlyDestinations,
        IEnumerable<WritableStore> writableStores,
        bool noAmbientEnvironment,
        bool noIpcEndpoints,
        bool noExtraInheritedFds,
        ExecutableMountPolicy mountPolicy,
        QualificationDisposition networkQualification)
    {
        Namespaces = ManifestCodec.SortedUnique(namespaces, nameof(namespaces));
        Capabilities = (capabilities ?? throw new ArgumentNullException(nameof(capabilities))).ToImmutableArray();
        if (Capabilities.Length != 0)
        {
            throw new ArgumentException("The M1 capability set must be empty.", nameof(capabilities));
        }

        NoNewPrivs = noNewPrivs;
        FilterSha256 = ManifestCodec.FixedBytes(filterSha256 ?? throw new ArgumentNullException(nameof(filterSha256)), 32, nameof(filterSha256));
        FilterLength = filterLength;
        ReadOnlyDestinations = ManifestCodec.SortedUnique(readOnlyDestinations, nameof(readOnlyDestinations));
        WritableStores = ManifestCodec.SortedStores(writableStores, nameof(writableStores));
        if (!noAmbientEnvironment || !noIpcEndpoints || !noExtraInheritedFds)
        {
            throw new ArgumentException("Ambient environment, IPC endpoints and extra inherited FDs must all be denied.", nameof(noAmbientEnvironment));
        }

        NoAmbientEnvironment = noAmbientEnvironment;
        NoIpcEndpoints = noIpcEndpoints;
        NoExtraInheritedFds = noExtraInheritedFds;
        MountPolicy = mountPolicy ?? throw new ArgumentNullException(nameof(mountPolicy));
        if (networkQualification != QualificationDisposition.PendingQualification)
        {
            throw new ArgumentException("Production no-IP qualification must remain PendingQualification in M1.", nameof(networkQualification));
        }

        NetworkQualification = networkQualification;
    }

    public ImmutableArray<string> Namespaces { get; }
    public ImmutableArray<string> Capabilities { get; }
    public bool NoNewPrivs { get; }
    public ImmutableArray<byte> FilterSha256 { get; }
    public ulong FilterLength { get; }
    public ImmutableArray<string> ReadOnlyDestinations { get; }
    public ImmutableArray<WritableStore> WritableStores { get; }
    public bool NoAmbientEnvironment { get; }
    public bool NoIpcEndpoints { get; }
    public bool NoExtraInheritedFds { get; }
    public ExecutableMountPolicy MountPolicy { get; }
    public QualificationDisposition NetworkQualification { get; }
}

/// <summary>One bound closure object: role, SHA-256, length, fixed
/// destination and build identity.</summary>
internal sealed record ClosureObject
{
    public ClosureObject(string role, byte[] sha256, ulong length, string destination, string buildIdentity)
    {
        ArgumentException.ThrowIfNullOrEmpty(role);
        ArgumentException.ThrowIfNullOrEmpty(destination);
        ArgumentException.ThrowIfNullOrEmpty(buildIdentity);
        Role = role;
        Sha256 = ManifestCodec.FixedBytes(sha256 ?? throw new ArgumentNullException(nameof(sha256)), 32, nameof(sha256));
        Length = length;
        Destination = destination;
        BuildIdentity = buildIdentity;
    }

    public string Role { get; }
    public ImmutableArray<byte> Sha256 { get; }
    public ulong Length { get; }
    public string Destination { get; }
    public string BuildIdentity { get; }
}

/// <summary>Bound closure: objects sorted by ordinal role with duplicates
/// rejected, plus separately identified helper dependencies.</summary>
internal sealed record ClosureIdentity
{
    public ClosureIdentity(IEnumerable<ClosureObject> objects, IEnumerable<string> helperDependencies)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(helperDependencies);
        var sorted = objects.OrderBy(o => o.Role, Utf8BytewiseComparer.Instance).ToArray();
        for (var i = 1; i < sorted.Length; i++)
        {
            if (string.Equals(sorted[i - 1].Role, sorted[i].Role, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Duplicate closure role: {sorted[i].Role}.", nameof(objects));
            }
        }

        Objects = sorted.ToImmutableArray();
        HelperDependencies = ManifestCodec.SortedUnique(helperDependencies, nameof(helperDependencies));
    }

    public ImmutableArray<ClosureObject> Objects { get; }
    public ImmutableArray<string> HelperDependencies { get; }
}

/// <summary>Fixed invocation: exact tool/input paths, allowlisted mode,
/// bounded argument vector and environment, fixed work directory. Combined
/// argument/environment UTF-8 is bounded to 16 KiB.</summary>
internal sealed record InvocationManifest
{
    internal const int MaxArgumentEnvironmentBytes = 16 * 1024;

    public InvocationManifest(
        string toolPath,
        string mode,
        string inputPath,
        IEnumerable<string> argv,
        IEnumerable<KeyValuePair<string, string>> environment,
        string workDirectory)
    {
        ArgumentNullException.ThrowIfNull(toolPath);
        ArgumentNullException.ThrowIfNull(mode);
        ArgumentNullException.ThrowIfNull(inputPath);
        ArgumentNullException.ThrowIfNull(argv);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(workDirectory);
        if (!string.Equals(toolPath, "/tool", StringComparison.Ordinal))
        {
            throw new ArgumentException("Tool path must be exactly /tool.", nameof(toolPath));
        }

        if (string.IsNullOrEmpty(mode))
        {
            throw new ArgumentException("Mode must be non-empty.", nameof(mode));
        }

        if (!string.Equals(inputPath, "/input", StringComparison.Ordinal))
        {
            throw new ArgumentException("Input path must be exactly /input.", nameof(inputPath));
        }

        if (!string.Equals(workDirectory, "/tmp", StringComparison.Ordinal))
        {
            throw new ArgumentException("Work directory must be exactly /tmp.", nameof(workDirectory));
        }

        var sortedEnvironment = environment.OrderBy(kv => kv.Key, Utf8BytewiseComparer.Instance).ToArray();
        for (var i = 1; i < sortedEnvironment.Length; i++)
        {
            if (string.Equals(sortedEnvironment[i - 1].Key, sortedEnvironment[i].Key, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Duplicate environment key: {sortedEnvironment[i].Key}.", nameof(environment));
            }
        }

        var total = 0L;
        var argvArray = argv.ToArray();
        foreach (var a in argvArray)
        {
            ArgumentException.ThrowIfNullOrEmpty(a);
            total += ManifestCodec.Utf8ByteCount(a);
        }

        foreach (var kv in sortedEnvironment)
        {
            ArgumentException.ThrowIfNullOrEmpty(kv.Key);
            ArgumentNullException.ThrowIfNull(kv.Value);
            total += ManifestCodec.Utf8ByteCount(kv.Key) + ManifestCodec.Utf8ByteCount(kv.Value);
        }

        if (total > MaxArgumentEnvironmentBytes)
        {
            throw new ArgumentException("Combined argument/environment size exceeds 16 KiB.", nameof(argv));
        }

        ToolPath = toolPath;
        Mode = mode;
        InputPath = inputPath;
        Argv = argvArray.ToImmutableArray();
        Environment = sortedEnvironment.ToImmutableArray();
        WorkDirectory = workDirectory;
    }

    public string ToolPath { get; }
    public string Mode { get; }
    public string InputPath { get; }
    public ImmutableArray<string> Argv { get; }
    public ImmutableArray<KeyValuePair<string, string>> Environment { get; }
    public string WorkDirectory { get; }
}

/// <summary>Channel manifest: stdin EOF with no retained writer, per-stream
/// and aggregate byte limits.</summary>
internal sealed record ChannelManifest
{
    public ChannelManifest(bool stdinEof, ulong stdoutLimitBytes, ulong stderrLimitBytes, ulong aggregateLimitBytes)
    {
        if (!stdinEof)
        {
            throw new ArgumentException("Stdin must be EOF with no retained writer.", nameof(stdinEof));
        }

        StdinEof = stdinEof;
        StdoutLimitBytes = stdoutLimitBytes;
        StderrLimitBytes = stderrLimitBytes;
        AggregateLimitBytes = aggregateLimitBytes;
    }

    public bool StdinEof { get; }
    public ulong StdoutLimitBytes { get; }
    public ulong StderrLimitBytes { get; }
    public ulong AggregateLimitBytes { get; }
}

/// <summary>Preparation identity: execution/issuer/generation/preparation
/// UUIDs plus the manifest digest. An identifier, never a release
/// capability.</summary>
internal sealed record PreparationIdentity
{
    public PreparationIdentity(Guid executionId, Guid issuerId, Guid generationId, Guid preparationId, byte[] manifestDigest)
    {
        if (executionId == Guid.Empty)
        {
            throw new ArgumentException("Execution ID must be non-empty.", nameof(executionId));
        }

        if (issuerId == Guid.Empty)
        {
            throw new ArgumentException("Issuer ID must be non-empty.", nameof(issuerId));
        }

        if (generationId == Guid.Empty)
        {
            throw new ArgumentException("Generation ID must be non-empty.", nameof(generationId));
        }

        if (preparationId == Guid.Empty)
        {
            throw new ArgumentException("Preparation ID must be non-empty.", nameof(preparationId));
        }

        ExecutionId = executionId;
        IssuerId = issuerId;
        GenerationId = generationId;
        PreparationId = preparationId;
        ManifestDigest = ManifestCodec.FixedBytes(manifestDigest ?? throw new ArgumentNullException(nameof(manifestDigest)), 32, nameof(manifestDigest));
    }

    public Guid ExecutionId { get; }
    public Guid IssuerId { get; }
    public Guid GenerationId { get; }
    public Guid PreparationId { get; }
    public ImmutableArray<byte> ManifestDigest { get; }
}

/// <summary>Core-dump policy: dumps disabled via zero rlimits (required),
/// runtime dump facilities disabled where applicable, and an optional
/// observed host core pattern. This covers only the observed tested crash
/// behavior, never exhaustive host core-sink exclusion.</summary>
internal sealed record CoreDumpPolicy
{
    public CoreDumpPolicy(bool coreDisabled, bool runtimeDumpsDisabled, string? corePatternObserved)
    {
        if (!coreDisabled)
        {
            throw new ArgumentException("Core dumps must be disabled.", nameof(coreDisabled));
        }

        if (corePatternObserved is not null && corePatternObserved.Length == 0)
        {
            throw new ArgumentException("An observed core pattern must be non-empty.", nameof(corePatternObserved));
        }

        CoreDisabled = coreDisabled;
        RuntimeDumpsDisabled = runtimeDumpsDisabled;
        CorePatternObserved = corePatternObserved;
    }

    public bool CoreDisabled { get; }
    public bool RuntimeDumpsDisabled { get; }
    public string? CorePatternObserved { get; }
}

/// <summary>Owner allocation/log bounded-resource semantics: staging bytes
/// and object counts plus fixed config/status/journal/receipt budgets.</summary>
internal sealed record OwnerAllocationLimits
{
    public OwnerAllocationLimits(
        ulong stagingBytes,
        uint stagingObjects,
        ulong configBytes,
        ulong coordinatorStatusBytes,
        ulong helperStatusBytes,
        ulong journalBytes,
        ulong receiptBytes)
    {
        StagingBytes = stagingBytes;
        StagingObjects = stagingObjects;
        ConfigBytes = configBytes;
        CoordinatorStatusBytes = coordinatorStatusBytes;
        HelperStatusBytes = helperStatusBytes;
        JournalBytes = journalBytes;
        ReceiptBytes = receiptBytes;
    }

    public ulong StagingBytes { get; }
    public uint StagingObjects { get; }
    public ulong ConfigBytes { get; }
    public ulong CoordinatorStatusBytes { get; }
    public ulong HelperStatusBytes { get; }
    public ulong JournalBytes { get; }
    public ulong ReceiptBytes { get; }
}

/// <summary>Resource manifest: exact budgets with units and per-constraint
/// disposition. Inode/object qualification is always PendingQualification
/// in M1; it is never an exception for other constraints.</summary>
internal sealed record ResourceManifest
{
    public ResourceManifest(
        ResourceBudget memory,
        ResourceBudget swap,
        ResourceBudget tasks,
        ResourceBudget fileDescriptors,
        ResourceBudget tmpBytes,
        ResourceBudget shmBytes,
        ResourceBudget stdoutBytes,
        ResourceBudget stderrBytes,
        ResourceBudget captureAggregateBytes,
        ResourceBudget setupTimeoutNs,
        ResourceBudget overallTimeoutNs,
        ResourceBudget stopAllowanceNs,
        ResourceBudget finalDrainNs,
        QualificationDisposition inodeQualification,
        CoreDumpPolicy coreDumps,
        OwnerAllocationLimits ownerAllocation)
    {
        Memory = memory ?? throw new ArgumentNullException(nameof(memory));
        Swap = swap ?? throw new ArgumentNullException(nameof(swap));
        Tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
        FileDescriptors = fileDescriptors ?? throw new ArgumentNullException(nameof(fileDescriptors));
        TmpBytes = tmpBytes ?? throw new ArgumentNullException(nameof(tmpBytes));
        ShmBytes = shmBytes ?? throw new ArgumentNullException(nameof(shmBytes));
        StdoutBytes = stdoutBytes ?? throw new ArgumentNullException(nameof(stdoutBytes));
        StderrBytes = stderrBytes ?? throw new ArgumentNullException(nameof(stderrBytes));
        CaptureAggregateBytes = captureAggregateBytes ?? throw new ArgumentNullException(nameof(captureAggregateBytes));
        SetupTimeoutNs = setupTimeoutNs ?? throw new ArgumentNullException(nameof(setupTimeoutNs));
        OverallTimeoutNs = overallTimeoutNs ?? throw new ArgumentNullException(nameof(overallTimeoutNs));
        StopAllowanceNs = stopAllowanceNs ?? throw new ArgumentNullException(nameof(stopAllowanceNs));
        FinalDrainNs = finalDrainNs ?? throw new ArgumentNullException(nameof(finalDrainNs));
        if (inodeQualification != QualificationDisposition.PendingQualification)
        {
            throw new ArgumentException("Inode/object qualification must remain PendingQualification in M1.", nameof(inodeQualification));
        }

        InodeQualification = inodeQualification;
        CoreDumps = coreDumps ?? throw new ArgumentNullException(nameof(coreDumps));
        OwnerAllocation = ownerAllocation ?? throw new ArgumentNullException(nameof(ownerAllocation));
    }

    public ResourceBudget Memory { get; }
    public ResourceBudget Swap { get; }
    public ResourceBudget Tasks { get; }
    public ResourceBudget FileDescriptors { get; }
    public ResourceBudget TmpBytes { get; }
    public ResourceBudget ShmBytes { get; }
    public ResourceBudget StdoutBytes { get; }
    public ResourceBudget StderrBytes { get; }
    public ResourceBudget CaptureAggregateBytes { get; }
    public ResourceBudget SetupTimeoutNs { get; }
    public ResourceBudget OverallTimeoutNs { get; }
    public ResourceBudget StopAllowanceNs { get; }
    public ResourceBudget FinalDrainNs { get; }
    public QualificationDisposition InodeQualification { get; }
    public CoreDumpPolicy CoreDumps { get; }
    public OwnerAllocationLimits OwnerAllocation { get; }
}

/// <summary>Establishment status: absent until trusted observation exists,
/// observed once a distinct immutable report supplies it, unknown when a
/// previously observed establishment can no longer be confirmed.</summary>
internal enum EstablishmentStatus : byte
{
    Absent = 0,
    Observed = 1,
    Unknown = 2,
}

/// <summary>Distinct immutable establishment representation for later work
/// packages. An immutable requested manifest is never establishment
/// evidence. WP01 produces only <see cref="Absent"/>; native readback
/// implementation remains outside WP01.</summary>
internal sealed record EstablishedManifest
{
    public static EstablishedManifest Absent { get; } = new(EstablishmentStatus.Absent, null);

    public static EstablishedManifest Unknown() => new(EstablishmentStatus.Unknown, null);

    public static EstablishedManifest Observed(RestrictedManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return new EstablishedManifest(EstablishmentStatus.Observed, manifest);
    }

    private EstablishedManifest(EstablishmentStatus status, RestrictedManifest? manifest)
    {
        if (status == EstablishmentStatus.Observed && manifest is null)
        {
            throw new ArgumentException("Observed establishment requires a manifest.", nameof(manifest));
        }

        if (status != EstablishmentStatus.Observed && manifest is not null)
        {
            throw new ArgumentException("Only observed establishment carries a manifest.", nameof(manifest));
        }

        Status = status;
        Manifest = manifest;
    }

    public EstablishmentStatus Status { get; }
    public RestrictedManifest? Manifest { get; }
}

/// <summary>Top-level immutable preparation manifest, schema version 1.</summary>
internal sealed record RestrictedManifest
{
    internal const ushort CurrentSchemaVersion = 1;
    internal const string ImplementationIdentifier = "linux-m1-native-transaction";

    public RestrictedManifest(
        ushort schemaVersion,
        string implementationId,
        AuthorityManifest authority,
        ResourceManifest resources,
        ClosureIdentity closure,
        InvocationManifest invocation,
        ChannelManifest channels)
    {
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new ArgumentException("Only schema version 1 is supported.", nameof(schemaVersion));
        }

        ArgumentNullException.ThrowIfNull(implementationId);
        if (!string.Equals(implementationId, ImplementationIdentifier, StringComparison.Ordinal))
        {
            throw new ArgumentException("Implementation ID must be linux-m1-native-transaction.", nameof(implementationId));
        }

        SchemaVersion = schemaVersion;
        ImplementationId = implementationId;
        Authority = authority ?? throw new ArgumentNullException(nameof(authority));
        Resources = resources ?? throw new ArgumentNullException(nameof(resources));
        Closure = closure ?? throw new ArgumentNullException(nameof(closure));
        Invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
        Channels = channels ?? throw new ArgumentNullException(nameof(channels));
    }

    public ushort SchemaVersion { get; }
    public string ImplementationId { get; }
    public AuthorityManifest Authority { get; }
    public ResourceManifest Resources { get; }
    public ClosureIdentity Closure { get; }
    public InvocationManifest Invocation { get; }
    public ChannelManifest Channels { get; }
}

/// <summary>Compares strings by unsigned UTF-8 byte sequence. This
/// differs from UTF-16 ordinal comparison for supplementary-plane
/// characters (whose surrogate lead units sort before BMP characters
/// above U+DFFF that their UTF-8 four-byte forms sort after).</summary>
internal sealed class Utf8BytewiseComparer : IComparer<string>
{
    internal static readonly Utf8BytewiseComparer Instance = new();

    private static readonly Encoding StrictOrderEncoding =
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private Utf8BytewiseComparer()
    {
    }

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var xb = StrictOrderEncoding.GetBytes(x);
        var yb = StrictOrderEncoding.GetBytes(y);
        var length = Math.Min(xb.Length, yb.Length);
        for (var i = 0; i < length; i++)
        {
            if (xb[i] != yb[i])
            {
                return xb[i] < yb[i] ? -1 : 1;
            }
        }

        return xb.Length.CompareTo(yb.Length);
    }
}

/// <summary>Canonical codec for <see cref="RestrictedManifest"/>: versioned
/// field order, fixed-width little-endian integers, one-byte kind tags, u32
/// lengths/counts, strict UTF-8 without normalization, raw 32-byte digests
/// and RFC byte-order UUIDs. The SHA-256 digest covers this encoding, never
/// incidental serializer property order. Live readbacks, native handles,
/// timestamps and result records are never hashed into the manifest.</summary>
internal static class ManifestCodec
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    internal static int Utf8ByteCount(string value) => StrictUtf8.GetByteCount(value);

    internal static ImmutableArray<string> SortedUnique(IEnumerable<string> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);
        var sorted = values.OrderBy(v => v, Utf8BytewiseComparer.Instance).ToArray();
        foreach (var v in sorted)
        {
            ArgumentException.ThrowIfNullOrEmpty(v);
        }

        for (var i = 1; i < sorted.Length; i++)
        {
            if (string.Equals(sorted[i - 1], sorted[i], StringComparison.Ordinal))
            {
                throw new ArgumentException($"Duplicate value: {sorted[i]}.", parameterName);
            }
        }

        return sorted.ToImmutableArray();
    }

    internal static ImmutableArray<WritableStore> SortedStores(IEnumerable<WritableStore> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);
        var sorted = values.OrderBy(s => s.Path, Utf8BytewiseComparer.Instance).ToArray();
        for (var i = 1; i < sorted.Length; i++)
        {
            if (string.Equals(sorted[i - 1].Path, sorted[i].Path, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Duplicate store path: {sorted[i].Path}.", parameterName);
            }
        }

        return sorted.ToImmutableArray();
    }

    internal static ImmutableArray<byte> FixedBytes(byte[] value, int length, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != length)
        {
            throw new ArgumentException($"Expected exactly {length} bytes.", parameterName);
        }

        return value.ToImmutableArray();
    }

    /// <summary>Converts a .NET Guid (mixed-endian fields) to RFC 4122 byte
    /// order (big-endian first three fields). Collection ordering uses
    /// <see cref="Utf8BytewiseComparer"/>, never UTF-16 ordinal order.</summary>
    internal static byte[] GuidToRfc4122Bytes(Guid value)
    {
        var raw = value.ToByteArray();
        var canonical = new byte[16];
        canonical[0] = raw[3];
        canonical[1] = raw[2];
        canonical[2] = raw[1];
        canonical[3] = raw[0];
        canonical[4] = raw[5];
        canonical[5] = raw[4];
        canonical[6] = raw[7];
        canonical[7] = raw[6];
        Buffer.BlockCopy(raw, 8, canonical, 8, 8);
        return canonical;
    }

    internal static byte[] Encode(RestrictedManifest manifest, int? maxCanonicalBytes = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        using var stream = new CanonicalStream(maxCanonicalBytes ?? int.MaxValue);
        WriteU16(stream, manifest.SchemaVersion);
        WriteString(stream, manifest.ImplementationId);
        WriteAuthority(stream, manifest.Authority);
        WriteResources(stream, manifest.Resources);
        WriteClosure(stream, manifest.Closure);
        WriteInvocation(stream, manifest.Invocation);
        WriteChannels(stream, manifest.Channels);
        return stream.ToArray();
    }

    internal static byte[] ComputeDigestBytes(RestrictedManifest manifest) =>
        SHA256.HashData(Encode(manifest));

    internal static string ComputeDigestHex(RestrictedManifest manifest) =>
        Convert.ToHexString(ComputeDigestBytes(manifest)).ToLowerInvariant();

    private static void WriteU16(MemoryStream stream, ushort value)
    {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
    }

    private static void WriteU32(MemoryStream stream, uint value)
    {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 24) & 0xFF));
    }

    private static void WriteU64(MemoryStream stream, ulong value)
    {
        for (var shift = 0; shift < 64; shift += 8)
        {
            stream.WriteByte((byte)((value >> shift) & 0xFF));
        }
    }

    private static void WriteString(MemoryStream stream, string value)
    {
        if (stream is CanonicalStream bounded)
            bounded.CheckAdditional(4L + StrictUtf8.GetByteCount(value));
        var bytes = StrictUtf8.GetBytes(value);
        WriteU32(stream, checked((uint)bytes.Length));
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteDigest(MemoryStream stream, ImmutableArray<byte> digest)
    {
        if (digest.Length != 32)
        {
            throw new ArgumentException("Digests must be exactly 32 bytes.", nameof(digest));
        }

        foreach (var b in digest)
        {
            stream.WriteByte(b);
        }
    }

    private static void WriteBudget(MemoryStream stream, ResourceBudget budget)
    {
        stream.WriteByte((byte)budget.Kind);
        WriteU64(stream, budget.Value);
        WriteString(stream, budget.Unit);
        stream.WriteByte((byte)budget.Qualification);
    }

    private static void WriteAuthority(MemoryStream stream, AuthorityManifest authority)
    {
        WriteU32(stream, checked((uint)authority.Namespaces.Length));
        foreach (var name in authority.Namespaces)
        {
            WriteString(stream, name);
        }

        WriteU32(stream, checked((uint)authority.Capabilities.Length));
        foreach (var capability in authority.Capabilities)
        {
            WriteString(stream, capability);
        }

        stream.WriteByte(authority.NoNewPrivs ? (byte)1 : (byte)0);
        WriteDigest(stream, authority.FilterSha256);
        WriteU64(stream, authority.FilterLength);
        WriteU32(stream, checked((uint)authority.ReadOnlyDestinations.Length));
        foreach (var destination in authority.ReadOnlyDestinations)
        {
            WriteString(stream, destination);
        }

        WriteU32(stream, checked((uint)authority.WritableStores.Length));
        foreach (var store in authority.WritableStores)
        {
            WriteString(stream, store.Path);
            WriteU64(stream, store.Bytes);
            stream.WriteByte(store.HasInodeBudget ? (byte)1 : (byte)0);
            WriteU64(stream, store.InodeBudget);
        }

        stream.WriteByte(authority.NoAmbientEnvironment ? (byte)1 : (byte)0);
        stream.WriteByte(authority.NoIpcEndpoints ? (byte)1 : (byte)0);
        stream.WriteByte(authority.NoExtraInheritedFds ? (byte)1 : (byte)0);
        stream.WriteByte(authority.MountPolicy.ToolExecutable ? (byte)1 : (byte)0);
        stream.WriteByte(authority.MountPolicy.ScratchNoexec ? (byte)1 : (byte)0);
        stream.WriteByte((byte)authority.NetworkQualification);
    }

    private static void WriteResources(MemoryStream stream, ResourceManifest resources)
    {
        WriteBudget(stream, resources.Memory);
        WriteBudget(stream, resources.Swap);
        WriteBudget(stream, resources.Tasks);
        WriteBudget(stream, resources.FileDescriptors);
        WriteBudget(stream, resources.TmpBytes);
        WriteBudget(stream, resources.ShmBytes);
        WriteBudget(stream, resources.StdoutBytes);
        WriteBudget(stream, resources.StderrBytes);
        WriteBudget(stream, resources.CaptureAggregateBytes);
        WriteBudget(stream, resources.SetupTimeoutNs);
        WriteBudget(stream, resources.OverallTimeoutNs);
        WriteBudget(stream, resources.StopAllowanceNs);
        WriteBudget(stream, resources.FinalDrainNs);
        stream.WriteByte((byte)resources.InodeQualification);
        stream.WriteByte(resources.CoreDumps.CoreDisabled ? (byte)1 : (byte)0);
        stream.WriteByte(resources.CoreDumps.RuntimeDumpsDisabled ? (byte)1 : (byte)0);
        stream.WriteByte(resources.CoreDumps.CorePatternObserved is null ? (byte)0 : (byte)1);
        if (resources.CoreDumps.CorePatternObserved is not null)
        {
            WriteString(stream, resources.CoreDumps.CorePatternObserved);
        }

        WriteU64(stream, resources.OwnerAllocation.StagingBytes);
        WriteU32(stream, resources.OwnerAllocation.StagingObjects);
        WriteU64(stream, resources.OwnerAllocation.ConfigBytes);
        WriteU64(stream, resources.OwnerAllocation.CoordinatorStatusBytes);
        WriteU64(stream, resources.OwnerAllocation.HelperStatusBytes);
        WriteU64(stream, resources.OwnerAllocation.JournalBytes);
        WriteU64(stream, resources.OwnerAllocation.ReceiptBytes);
    }

    private static void WriteClosure(MemoryStream stream, ClosureIdentity closure)
    {
        WriteU32(stream, checked((uint)closure.Objects.Length));
        foreach (var bound in closure.Objects)
        {
            WriteString(stream, bound.Role);
            WriteDigest(stream, bound.Sha256);
            WriteU64(stream, bound.Length);
            WriteString(stream, bound.Destination);
            WriteString(stream, bound.BuildIdentity);
        }

        WriteU32(stream, checked((uint)closure.HelperDependencies.Length));
        foreach (var dependency in closure.HelperDependencies)
        {
            WriteString(stream, dependency);
        }
    }

    private static void WriteInvocation(MemoryStream stream, InvocationManifest invocation)
    {
        WriteString(stream, invocation.ToolPath);
        WriteString(stream, invocation.Mode);
        WriteString(stream, invocation.InputPath);
        WriteU32(stream, checked((uint)invocation.Argv.Length));
        foreach (var argument in invocation.Argv)
        {
            WriteString(stream, argument);
        }

        WriteU32(stream, checked((uint)invocation.Environment.Length));
        foreach (var pair in invocation.Environment)
        {
            WriteString(stream, pair.Key);
            WriteString(stream, pair.Value);
        }

        WriteString(stream, invocation.WorkDirectory);
    }

    private static void WriteChannels(MemoryStream stream, ChannelManifest channels)
    {
        stream.WriteByte(channels.StdinEof ? (byte)1 : (byte)0);
        WriteU64(stream, channels.StdoutLimitBytes);
        WriteU64(stream, channels.StderrLimitBytes);
        WriteU64(stream, channels.AggregateLimitBytes);
    }

    // The same writer/grammar serves legacy internal encoding and the bounded public boundary.
    // Check before growing storage or returning any output; inclusive ceiling, no truncation.
    private sealed class CanonicalStream(int maximum) : MemoryStream
    {
        internal void CheckAdditional(long count)
        {
            if (count > maximum - Position)
                throw new ArgumentException("Complete canonical encoding exceeds the byte limit.", "manifest");
        }

        public override void WriteByte(byte value) { CheckAdditional(1); base.WriteByte(value); }
        public override void Write(byte[] buffer, int offset, int count)
        { CheckAdditional(count); base.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer)
        { CheckAdditional(buffer.Length); base.Write(buffer); }
    }
}
