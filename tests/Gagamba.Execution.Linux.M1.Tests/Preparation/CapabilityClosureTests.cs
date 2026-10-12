// WP02 capability/closure lifetime tests (G2-06): the published capability
// owns the exact verified PreparedClosure; borrow is controlled; builder
// disposal after publication cannot invalidate store-owned handles; and no
// valid capability can survive destruction of its authoritative closure.
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Restricted;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Preparation.CopyTestSupport;

namespace Gagamba.Execution.Linux.M1.Tests.Preparation;

public sealed class CapabilityClosureTests
{
    private static (PreparedTransaction.PreparationStore Store, TestClock Clock) NewStore()
    {
        var clock = new TestClock();
        return (CopyTestSupport.NewStore(clock), clock);
    }

    private static PreparedClosure Stage(FakeCopyHost host)
    {
        host.AddSourceFile("tool", Bytes(0x31, 1024));
        host.AddSourceFile("input", Bytes(0x32, 512));
        var result = SafeCopyIn.Prepare(Plan(host, Tool(), Input()), host, CancellationToken.None);
        Assert.True(result.Succeeded, result.Detail);
        return result.Closure!;
    }

    private static (FakeCopyHost Host, PreparedTransaction.PreparationStore Store, RestrictedManifest Manifest,
        PreparationIdentity Identity, PublicationResult Published, PreparedClosure BuilderClosure) Publish()
    {
        var host = NewHost();
        var closure = Stage(host);
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        var identity = IdentityFor(manifest);
        var published = PreparationPublisher.Publish(
            store, closure, manifest, identity, 1_000_000_000, _ => true, CancellationToken.None);
        return (host, store, manifest, identity, published, closure);
    }

    [Fact]
    public void PublishedCapabilityOwnsClosureAndBorrowsExactHandles()
    {
        var (host, store, _, identity, published, _) = Publish();
        Assert.True(published.Published);
        // Ownership transferred: no independently disposable closure is returned.
        Assert.Null(published.Closure);
        Assert.NotNull(published.Capability);
        Assert.True(store.TryLookup(identity.PreparationId, out var live));
        Assert.True(live!.TryBorrowClosure(out var borrowed));
        Assert.NotNull(borrowed);
        Assert.Equal(2, borrowed!.Objects.Count);
        foreach (var obj in borrowed.Objects)
        {
            Assert.False(obj.Handle.IsClosed);
            Assert.Equal(0, host.StatHandle(obj.Handle, out _));
        }
    }

    [Fact]
    public void BuilderDisposalAfterPublicationDoesNotInvalidateStoreClosure()
    {
        var (host, store, _, identity, _, builderClosure) = Publish();
        // A stale builder reference disposing must be a no-op after transfer.
        builderClosure.Dispose();
        Assert.True(store.TryLookup(identity.PreparationId, out var live));
        Assert.True(live!.TryBorrowClosure(out var borrowed));
        foreach (var obj in borrowed!.Objects)
        {
            Assert.False(obj.Handle.IsClosed);
            Assert.Equal(0, host.StatHandle(obj.Handle, out _));
        }
    }

    [Fact]
    public void ConsumeRetainsClosureThroughConsumedLifetime()
    {
        var (_, store, _, identity, _, _) = Publish();
        Assert.True(store.TryLookup(identity.PreparationId, out var live));
        Assert.True(store.TryConsume(live!, out var violation));
        Assert.Equal(InvariantViolation.None, violation);
        // Consuming does not close the closure.
        Assert.True(live!.TryBorrowClosure(out var borrowed));
        foreach (var obj in borrowed!.Objects)
        {
            Assert.False(obj.Handle.IsClosed);
        }
    }

    [Fact]
    public void DuplicateConsumeDoesNotDuplicateOwnership()
    {
        var (_, store, _, identity, _, _) = Publish();
        Assert.True(store.TryLookup(identity.PreparationId, out var live));
        Assert.True(store.TryConsume(live!, out _));
        Assert.False(store.TryConsume(live!, out var second));
        Assert.Equal(InvariantViolation.Consumed, second);
        Assert.True(live!.TryBorrowClosure(out _));
    }

    [Fact]
    public void InvalidationReleasesClosureExactlyOnce()
    {
        var (_, store, _, identity, published, _) = Publish();
        var capability = published.Capability!;
        Assert.True(capability.TryBorrowClosure(out var borrowed));
        var handle = borrowed!.Objects[0].Handle;
        Assert.False(handle.IsClosed);

        capability.Dispose();
        Assert.True(handle.IsClosed);
        // Second disposal is a no-op; no double-close.
        capability.Dispose();
        Assert.False(capability.TryBorrowClosure(out _));
        Assert.False(store.TryLookup(identity.PreparationId, out _));
    }

    [Fact]
    public void StoreAbortReleasesClosure()
    {
        var (_, store, _, identity, published, _) = Publish();
        Assert.True(published.Capability!.TryBorrowClosure(out var borrowed));
        Assert.True(store.Abort(identity.PreparationId));
        Assert.True(borrowed!.Objects[0].Handle.IsClosed);
        Assert.False(store.TryLookup(identity.PreparationId, out _));
    }

    [Fact]
    public void NoValidCapabilitySurvivesClosureDestruction()
    {
        // Astra's negative case: publish -> DiscardAccounted -> lookup/consume.
        // The published result exposes no disposable closure, and discarding
        // through the stale builder reference is refused; the store keeps the
        // handles live, so there is no valid capability referring to closed
        // handles.
        var (host, store, _, identity, published, builderClosure) = Publish();
        Assert.Null(published.Closure);
        var refused = builderClosure.DiscardAccounted(host);
        Assert.False(refused.Confirmed);
        Assert.All(refused.Items, i => Assert.Equal(CleanupItemOutcome.IdentityMismatch, i.Outcome));
        Assert.True(store.TryLookup(identity.PreparationId, out var live));
        Assert.True(live!.TryBorrowClosure(out var borrowed));
        Assert.All(borrowed!.Objects, o => Assert.False(o.Handle.IsClosed));
        // And no staged file was removed by the refused discard.
        Assert.Equal(2, host.StagingFileNames().Count);
    }

    [Fact]
    public void PublicationFailureDoesNotTransferClosure()
    {
        var host = NewHost();
        var closure = Stage(host);
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        var published = PreparationPublisher.Publish(
            store, closure, manifest, IdentityFor(manifest), 1_000_000_000, _ => false, CancellationToken.None);
        Assert.False(published.Published);
        Assert.Null(published.Capability);
        Assert.Same(closure, published.Closure);
        Assert.False(closure.StoreOwned);
        // The failed/accounted closure supports checked cleanup.
        Assert.True(published.Closure!.DiscardAccounted(host).Confirmed);
        Assert.Empty(host.StagingFileNames());
    }

    [Fact]
    public void SeparatePreparationsOwnSeparateClosures()
    {
        var (_, storeA, _, identityA, publishedA, _) = Publish();
        var (_, storeB, _, identityB, publishedB, _) = Publish();
        Assert.True(publishedA.Capability!.TryBorrowClosure(out var borrowA));
        Assert.True(publishedB.Capability!.TryBorrowClosure(out var borrowB));
        Assert.NotSame(borrowA, borrowB);
        publishedA.Capability.Dispose();
        // Disposing A does not affect B's closure.
        Assert.True(storeB.TryLookup(identityB.PreparationId, out var liveB));
        Assert.True(liveB!.TryBorrowClosure(out var stillB));
        Assert.False(stillB!.Objects[0].Handle.IsClosed);
        Assert.False(storeA.TryLookup(identityA.PreparationId, out _));
    }
}
