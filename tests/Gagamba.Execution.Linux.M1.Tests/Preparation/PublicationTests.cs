// WP02 portable publication tests: acknowledgement ordering, accounted
// retention on ack/mint/cancel failures, and binding substitution refusal.
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Restricted;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Preparation.CopyTestSupport;

namespace Gagamba.Execution.Linux.M1.Tests.Preparation;

public sealed class PublicationTests
{
    private static (PreparedTransaction.PreparationStore Store, TestClock Clock) NewStore()
    {
        var clock = new TestClock();
        return (CopyTestSupport.NewStore(clock), clock);
    }

    private static PreparedClosure Stage(FakeCopyHost host)
    {
        host.AddSourceFile("tool", Bytes(0x21, 1024));
        host.AddSourceFile("input", Bytes(0x22, 512));
        var result = SafeCopyIn.Prepare(Plan(host, Tool(), Input()), host, CancellationToken.None);
        Assert.True(result.Succeeded);
        return result.Closure!;
    }

    [Fact]
    public void NoCapabilityExistsBeforeAcknowledgement()
    {
        var host = NewHost();
        using var closure = Stage(host);
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        var identity = IdentityFor(manifest);
        var ackOrder = new List<string>();
        var published = PreparationPublisher.Publish(
            store,
            closure,
            manifest,
            identity,
            1_000_000_000,
            _ =>
            {
                ackOrder.Add("ack");
                Assert.False(store.TryLookup(identity.PreparationId, out var duringAck));
                Assert.Null(duringAck);
                ackOrder.Add("no-capability-during-ack");
                return true;
            },
            CancellationToken.None);
        Assert.True(published.Published);
        Assert.NotNull(published.Capability);
        Assert.Equal(new[] { "ack", "no-capability-during-ack" }, ackOrder);
        Assert.True(store.TryLookup(identity.PreparationId, out _));
    }

    [Fact]
    public void RefusedAcknowledgementKeepsAccountedObjects()
    {
        var host = NewHost();
        using var closure = Stage(host);
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        var published = PreparationPublisher.Publish(
            store, closure, manifest, IdentityFor(manifest), 1_000_000_000,
            _ => false, CancellationToken.None);
        Assert.False(published.Published);
        Assert.Equal(CopyFailureKind.PublicationFailure, published.Failure);
        Assert.Null(published.Capability);
        Assert.Same(closure, published.Closure);
        Assert.Equal(2, host.StagingFileNames().Count);
        published.Closure.DiscardAccounted(host);
        Assert.Empty(host.StagingFileNames());
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void MintFailureRemainsAccounted()
    {
        var host = NewHost();
        using var closure = Stage(host);
        var clock = new TestClock();
        var foreignStore = new PreparedTransaction.PreparationStore(
            new Guid("99999999-9999-9999-9999-999999999999"), Generation, clock);
        var manifest = ManifestWithClosure(closure.Objects);
        var published = PreparationPublisher.Publish(
            foreignStore, closure, manifest, IdentityFor(manifest), 1_000_000_000,
            _ => true, CancellationToken.None);
        Assert.False(published.Published);
        Assert.Equal(CopyFailureKind.PublicationFailure, published.Failure);
        Assert.Null(published.Capability);
        Assert.Same(closure, published.Closure);
        Assert.Equal(2, host.StagingFileNames().Count);
        published.Closure.DiscardAccounted(host);
        Assert.Empty(host.StagingFileNames());
    }

    [Fact]
    public void FabricatedManifestCannotSubstituteBindings()
    {
        var host = NewHost();
        using var closure = Stage(host);
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        var forged = new RestrictedManifest(
            manifest.SchemaVersion,
            manifest.ImplementationId,
            manifest.Authority,
            manifest.Resources,
            new ClosureIdentity(
                new[] { new ClosureObject("tool", new byte[32], 1024, "/tool", "forged") },
                Array.Empty<string>()),
            manifest.Invocation,
            manifest.Channels);
        var ackCalls = 0;
        var published = PreparationPublisher.Publish(
            store, closure, forged, IdentityFor(forged), 1_000_000_000,
            _ =>
            {
                ackCalls++;
                return true;
            },
            CancellationToken.None);
        Assert.False(published.Published);
        Assert.Equal(CopyFailureKind.PublicationFailure, published.Failure);
        Assert.Equal(0, ackCalls);
        Assert.Null(published.Capability);
        Assert.Same(closure, published.Closure);
    }

    [Fact]
    public void ForeignIdentityCannotMint()
    {
        var host = NewHost();
        using var closure = Stage(host);
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        var valid = IdentityFor(manifest);
        var foreign = new PreparationIdentity(
            valid.ExecutionId, valid.IssuerId, valid.GenerationId, valid.PreparationId,
            new byte[32]);
        var published = PreparationPublisher.Publish(
            store, closure, manifest, foreign, 1_000_000_000,
            _ => true, CancellationToken.None);
        Assert.False(published.Published);
        Assert.Equal(CopyFailureKind.PublicationFailure, published.Failure);
        Assert.Null(published.Capability);
        Assert.False(store.TryLookup(valid.PreparationId, out _));
        Assert.Same(closure, published.Closure);
    }

    [Fact]
    public void CancelBeforePublishRetainsAccounted()
    {
        var host = NewHost();
        using var closure = Stage(host);
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var published = PreparationPublisher.Publish(
            store, closure, manifest, IdentityFor(manifest), 1_000_000_000,
            _ => true, cts.Token);
        Assert.False(published.Published);
        Assert.Equal(CopyFailureKind.Cancelled, published.Failure);
        Assert.Null(published.Capability);
        Assert.Same(closure, published.Closure);
        Assert.Equal(2, host.StagingFileNames().Count);
    }

    [Fact]
    public void CancelDuringAcknowledgementPublishesNothing()
    {
        var host = NewHost();
        using var closure = Stage(host);
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        using var cts = new CancellationTokenSource();
        var published = PreparationPublisher.Publish(
            store,
            closure,
            manifest,
            IdentityFor(manifest),
            1_000_000_000,
            token =>
            {
                cts.Cancel();
                token.ThrowIfCancellationRequested();
                return true;
            },
            cts.Token);
        Assert.False(published.Published);
        Assert.Equal(CopyFailureKind.Cancelled, published.Failure);
        Assert.Null(published.Capability);
        Assert.False(store.TryLookup(IdentityFor(manifest).PreparationId, out _));
        Assert.Same(closure, published.Closure);
    }

    [Fact]
    public void PublishedCapabilityMatchesStagedBindings()
    {
        var host = NewHost();
        using var closure = Stage(host);
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        var identity = IdentityFor(manifest);
        var published = PreparationPublisher.Publish(
            store, closure, manifest, identity, 1_000_000_000,
            _ => true, CancellationToken.None);
        Assert.True(published.Published);
        Assert.True(store.TryLookup(identity.PreparationId, out var live));
        Assert.Same(published.Capability, live);
        Assert.Equal(
            ManifestCodec.ComputeDigestHex(manifest),
            ManifestCodec.ComputeDigestHex(live!.Manifest));
    }
}
