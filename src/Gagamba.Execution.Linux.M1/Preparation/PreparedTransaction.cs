// WP01/WP02 capability: single-use preparation identity with a private
// constructor. Only the nested PreparationStore can mint it; only a
// release arbiter may consume it (WP05). G2-06: the exact verified
// PreparedClosure and its retained read-only object handles are owned by
// the store entry for the capability's whole lifetime, so the future
// PrepareHeldAsync(PreparedTransaction, DomainLease, ...) boundary can
// borrow the exact copied FDs. Disposal/abort invalidates permanently and
// releases closure authority exactly once.
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Preparation;

/// <summary>Single-use preparation capability. A sealed partial non-record
/// with a private constructor, no public factory, no clone/deserialize
/// route and no mutable public fields.</summary>
internal sealed partial class PreparedTransaction : IDisposable
{
    private PreparationStore? _store;

    private PreparedTransaction(
        RestrictedManifest manifest,
        PreparationIdentity identity,
        long expiresAtNs,
        PreparationStore store)
    {
        Manifest = manifest;
        Identity = identity;
        ExpiresAtNs = expiresAtNs;
        _store = store;
    }

    public RestrictedManifest Manifest { get; }

    public PreparationIdentity Identity { get; }

    public long ExpiresAtNs { get; }

    /// <summary>Controlled internal borrow of the exact store-owned bound
    /// closure for the future held-launch binding path. Ownership remains
    /// with the store; the caller must not dispose it and must not use it
    /// after the capability is invalidated/disposed. Fails once the
    /// capability is no longer live.</summary>
    internal bool TryBorrowClosure(out PreparedClosure? closure)
    {
        closure = null;
        return _store is not null && _store.TryBorrowClosure(this, out closure);
    }

    /// <summary>True while this exact capability is the live, unexpired,
    /// unconsumed store issuance. Used by the release arbiter before it
    /// commits intent; consumption remains the store's single-use decision.</summary>
    internal bool IsLive =>
        _store is not null
        && _store.TryLookup(Identity.PreparationId, out var live)
        && ReferenceEquals(live, this);

    /// <summary>Consumes this exact capability through its issuing store.
    /// The release arbiter is the only production caller; recovery cannot
    /// reconstruct a consumed capability.</summary>
    internal bool TryConsume(out InvariantViolation violation)
    {
        if (_store is null)
        {
            violation = InvariantViolation.Consumed;
            return false;
        }

        return _store.TryConsume(this, out violation);
    }

    public void Dispose()
    {
        _store?.Invalidate(this);
        _store = null;
        GC.SuppressFinalize(this);
    }
}
