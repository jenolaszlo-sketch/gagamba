// WP01 capability skeleton: single-use preparation identity with a private
// constructor. Only the nested PreparationStore can mint it; only a release
// arbiter may consume it (WP05). Disposal/abort invalidates permanently.
// No native handles exist in WP01; those arrive with later packages.
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

    public void Dispose()
    {
        _store?.Invalidate(this);
        _store = null;
        GC.SuppressFinalize(this);
    }
}
