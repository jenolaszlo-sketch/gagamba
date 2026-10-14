// WP02 bound-object representation and atomic publication. A StagedObject
// binds semantic content identity (SHA-256, length, role) to filesystem
// object identity (device, inode) and live read-only descriptor
// ownership. Publication verifies staged bindings against the manifest,
// requires durable acknowledgement, then mints through the WP01 store:
// Mint remains the atomic capability-publication boundary. No capability
// exists before acknowledgement; accounted objects are retained, never
// silently deleted, when publication fails.
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Preparation;

/// <summary>Bounded non-authoritative copy result sent to the recovery
/// coordinator when it must build/authorize a manifest. It intentionally
/// contains no path, descriptor, closure or capability handle.</summary>
internal sealed record StagedObjectDescription(
    string Role,
    byte[] Sha256,
    ulong Length,
    ulong Device,
    ulong Inode,
    string StagingName);

/// <summary>One immutably bound staged object. A sealed class (never a
/// record): handle identity must not participate in value equality.</summary>
internal sealed class StagedObject
{
    public StagedObject(
        string role,
        byte[] sha256,
        ulong length,
        ulong device,
        ulong inode,
        uint mode,
        string stagingName,
        SafeHandle handle)
    {
        ArgumentException.ThrowIfNullOrEmpty(role);
        ArgumentNullException.ThrowIfNull(sha256);
        ArgumentException.ThrowIfNullOrEmpty(stagingName);
        ArgumentNullException.ThrowIfNull(handle);
        if (sha256.Length != 32)
        {
            throw new ArgumentException("Staged digests must be exactly 32 bytes.", nameof(sha256));
        }

        Role = role;
        Sha256 = (byte[])sha256.Clone();
        Length = length;
        Device = device;
        Inode = inode;
        Mode = mode;
        StagingName = stagingName;
        Handle = handle;
    }

    public string Role { get; }

    public byte[] Sha256 { get; }

    public ulong Length { get; }

    public ulong Device { get; }

    public ulong Inode { get; }

    public uint Mode { get; }

    public string StagingName { get; }

    public SafeHandle Handle { get; }
}

/// <summary>Closure ownership state (G2-06 R6A). A concrete closure may be
/// transferred into preparation authority exactly once. The single
/// builder-owned → store-owned transition is enforced by the closure object
/// itself under its own lock — never by caller convention — and Released is
/// terminal, so no second Mint can claim the same authority and no two
/// entries can share or independently release it.</summary>
internal enum ClosureOwnership : byte
{
    /// <summary>Live and builder-owned. Builder Dispose/DiscardAccounted act.</summary>
    BuilderOwned = 0,

    /// <summary>Live and owned by exactly one store entry. Builder-side
    /// disposal is a no-op; only the owning entry may release.</summary>
    StoreOwned = 1,

    /// <summary>Handles closed. Terminal; no further transfer or release.</summary>
    Released = 2,
}

/// <summary>Accounted staged allocation: bound objects plus the retained
/// staging-directory handle that keeps descriptor-relative cleanup
/// possible. Disposal closes every retained descriptor.</summary>
internal sealed class PreparedClosure : IDisposable
{
    private readonly object _gate = new();
    private ClosureOwnership _ownership = ClosureOwnership.BuilderOwned;

    public PreparedClosure(
        IReadOnlyList<StagedObject> objects,
        SafeHandle? stagingDirectory,
        ulong totalBytes,
        IReadOnlyList<KeyValuePair<string, string>> environment)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(environment);
        Objects = objects;
        StagingDirectory = stagingDirectory;
        TotalBytes = totalBytes;
        Environment = environment;
    }

    public IReadOnlyList<StagedObject> Objects { get; }

    public SafeHandle? StagingDirectory { get; }

    public ulong TotalBytes { get; }

    public IReadOnlyList<KeyValuePair<string, string>> Environment { get; }

    /// <summary>Atomically claims this closure for exactly one store entry.
    /// Succeeds only for a live builder-owned closure; refuses an already
    /// store-owned or released closure. Synchronized so concurrent claims
    /// yield exactly one success.</summary>
    internal bool TryTransferToStore()
    {
        lock (_gate)
        {
            if (_ownership != ClosureOwnership.BuilderOwned)
            {
                return false;
            }

            _ownership = ClosureOwnership.StoreOwned;
            return true;
        }
    }

    internal ClosureOwnership Ownership
    {
        get
        {
            lock (_gate)
            {
                return _ownership;
            }
        }
    }

    internal bool StoreOwned => Ownership == ClosureOwnership.StoreOwned;

    /// <summary>True while this closure still holds live authoritative
    /// handles. Defense in depth: exclusive ownership already guarantees a
    /// live entry's closure is live, but the store re-checks before
    /// exposing it.</summary>
    internal bool AuthorityLive => Ownership != ClosureOwnership.Released;

    /// <summary>Identity-checked accounted cleanup (G2-07): removes each
    /// staged object only if the current directory entry still is the exact
    /// object this preparation created. Refuses once ownership has moved to
    /// the store (the store releases handles on invalidation). Disposes on
    /// full confirmation; otherwise retains handle authority and returns
    /// cleanup debt. Never invoked implicitly by the engine on accounted
    /// objects.</summary>
    public StagingCleanupReport DiscardAccounted(ICopyHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (Ownership != ClosureOwnership.BuilderOwned)
        {
            var refused = Objects
                .Select(s => new CleanupItemResult(CleanupItemOutcome.IdentityMismatch, s.StagingName, "closure ownership has moved to the store"))
                .ToArray();
            return new StagingCleanupReport(refused);
        }

        StagingCleanupReport report;
        if (StagingDirectory is null)
        {
            var debt = Objects
                .Select(s => new CleanupItemResult(CleanupItemOutcome.InspectionFailed, s.StagingName, "no retained staging directory"))
                .ToArray();
            report = new StagingCleanupReport(debt);
        }
        else
        {
            var entries = Objects
                .Select(s => (s.StagingName, s.Device, s.Inode))
                .ToArray();
            report = StagingCleanup.CleanAll(host, StagingDirectory, entries);
        }

        if (report.Confirmed)
        {
            Dispose();
        }

        return report;
    }

    /// <summary>Releases handle authority. Acts only on a live builder-owned
    /// closure; a no-op once ownership has moved to the store (callers
    /// holding a stale builder reference cannot close store-owned handles)
    /// or once already released.</summary>
    public void Dispose()
    {
        bool release;
        lock (_gate)
        {
            release = _ownership == ClosureOwnership.BuilderOwned;
            if (release)
            {
                _ownership = ClosureOwnership.Released;
            }
        }

        if (release)
        {
            DisposeHandles();
        }
    }

    /// <summary>Store-side release on invalidation/abort. Disposes handles
    /// exactly once regardless of stale builder references: the Released
    /// transition is atomic, and sharing cannot exist because transfer
    /// succeeds exactly once.</summary>
    internal void ReleaseFromStore()
    {
        bool release;
        lock (_gate)
        {
            release = _ownership != ClosureOwnership.Released;
            _ownership = ClosureOwnership.Released;
        }

        if (release)
        {
            DisposeHandles();
        }
    }

    private void DisposeHandles()
    {
        foreach (var staged in Objects)
        {
            staged.Handle.Dispose();
        }

        StagingDirectory?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>Publication outcome. On success the capability owns the exact
/// closure and <see cref="Closure"/> is null (ownership transferred into
/// the store). On failure <see cref="Capability"/> is null and
/// <see cref="Closure"/> carries the accounted objects for explicit
/// checked cleanup, never a usable capability.</summary>
internal sealed record PublicationResult(
    bool Published,
    CopyFailureKind Failure,
    string Detail,
    PreparedTransaction? Capability,
    PreparedClosure? Closure);

/// <summary>Atomic publication through the WP01 store: binding check,
/// durable acknowledgement, then Mint. No early publication.</summary>
internal static class PreparationPublisher
{
    internal static PublicationResult Publish(
        PreparedTransaction.PreparationStore store,
        PreparedClosure closure,
        RestrictedManifest manifest,
        PreparationIdentity identity,
        long expiresAtNs,
        Func<CancellationToken, bool> acknowledge,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(closure);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(acknowledge);

        if (cancellationToken.IsCancellationRequested)
        {
            return new PublicationResult(false, CopyFailureKind.Cancelled, "Cancelled before publication.", null, closure);
        }

        // Binding check first: every staged object must appear in the
        // manifest closure with equal digest and length, and every
        // manifest entry must have a staged object. A fabricated or
        // foreign manifest cannot substitute staged bindings.
        var stagedByRole = new Dictionary<string, StagedObject>(StringComparer.Ordinal);
        foreach (var staged in closure.Objects)
        {
            stagedByRole[staged.Role] = staged;
        }

        if (manifest.Closure.Objects.Length != closure.Objects.Count)
        {
            return new PublicationResult(false, CopyFailureKind.PublicationFailure, "Manifest closure does not match staged bindings.", null, closure);
        }

        foreach (var bound in manifest.Closure.Objects)
        {
            if (!stagedByRole.TryGetValue(bound.Role, out var staged)
                || !staged.Sha256.SequenceEqual(bound.Sha256.ToArray())
                || staged.Length != bound.Length)
            {
                return new PublicationResult(false, CopyFailureKind.PublicationFailure, "Manifest closure does not match staged bindings.", null, closure);
            }
        }

        bool acknowledged;
        try
        {
            acknowledged = acknowledge(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new PublicationResult(false, CopyFailureKind.Cancelled, "Cancelled during acknowledgement.", null, closure);
        }
        catch (Exception ex)
        {
            return new PublicationResult(false, CopyFailureKind.PublicationFailure, $"Durable acknowledgement failed: {ex.GetType().Name}.", null, closure);
        }

        if (!acknowledged)
        {
            return new PublicationResult(false, CopyFailureKind.PublicationFailure, "Durable acknowledgement refused.", null, closure);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new PublicationResult(false, CopyFailureKind.Cancelled, "Cancelled after acknowledgement; no late publication.", null, closure);
        }

        try
        {
            // Ownership of the verified closure transfers exactly once
            // into the capability/store entry; the caller no longer owns it
            // on success.
            var capability = store.Mint(manifest, identity, expiresAtNs, closure);
            return new PublicationResult(true, CopyFailureKind.None, "Preparation published.", capability, null);
        }
        catch (OperationCanceledException)
        {
            return new PublicationResult(false, CopyFailureKind.Cancelled, "Cancelled during mint.", null, closure);
        }
        catch (Exception ex)
        {
            return new PublicationResult(false, CopyFailureKind.PublicationFailure, $"Mint failed: {ex.GetType().Name}; staged objects remain accounted.", null, closure);
        }
    }
}
