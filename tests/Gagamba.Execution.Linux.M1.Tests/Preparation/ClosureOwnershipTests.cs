// WP02 exclusive closure-ownership tests (G2-06 R6A): a concrete
// PreparedClosure transfers into preparation authority exactly once. A
// second Mint of the same closure refuses without harming the first
// capability; invalidating the sole owner closes handles exactly once;
// concurrent claims yield exactly one success.
using System.Collections.Concurrent;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Restricted;
using Xunit;
using Gagamba.Execution.Linux.M1.Tests;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;
using static Gagamba.Execution.Linux.M1.Tests.Preparation.CopyTestSupport;

namespace Gagamba.Execution.Linux.M1.Tests.Preparation;

public sealed class ClosureOwnershipTests
{
    private static (PreparedTransaction.PreparationStore Store, TestClock Clock) NewStore()
    {
        var clock = new TestClock();
        return (CopyTestSupport.NewStore(clock), clock);
    }

    private static PreparedClosure Stage(FakeCopyHost host, byte[] content)
    {
        host.AddSourceFile("tool", content);
        var result = SafeCopyIn.Prepare(Plan(host, Tool()), host, CancellationToken.None);
        Assert.True(result.Succeeded, result.Detail);
        return result.Closure!;
    }

    private static PreparationIdentity DistinctIdentity(RestrictedManifest manifest)
    {
        // Same authority, distinct preparation: otherwise fully valid.
        var reference = IdentityFor(manifest);
        return new PreparationIdentity(
            reference.ExecutionId, reference.IssuerId, reference.GenerationId,
            Guid.NewGuid(), reference.ManifestDigest.ToArray());
    }

    [Fact]
    public void RereviewSharedClosureSecondPublicationRefuses()
    {
        // Exact rereview counterexample: real copied closure, publish A,
        // stale builder dispose, publish the same exact closure as B,
        // invalidate A. Old result was secondPublished=True with B staying
        // usable after A's handles closed; that state is now impossible.
        var host = NewHost();
        var closure = Stage(host, Bytes(0x31, 128));
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        var identityA = IdentityFor(manifest);
        var publishedA = PreparationPublisher.Publish(
            store, closure, manifest, identityA, 1_000_000_000, _ => true, CancellationToken.None);
        Assert.True(publishedA.Published);
        var capabilityA = publishedA.Capability!;
        Assert.Equal(ClosureOwnership.StoreOwned, closure.Ownership);

        // Stale builder dispose stays harmless: handles stay open.
        closure.Dispose();
        Assert.True(capabilityA.TryBorrowClosure(out var borrowedA));
        Assert.All(borrowedA!.Objects, o => Assert.False(o.Handle.IsClosed));

        // B: the same exact closure with an otherwise valid distinct
        // identity must refuse; no second capability is published.
        var publishedB = PreparationPublisher.Publish(
            store, closure, manifest, DistinctIdentity(manifest), 1_000_000_000, _ => true, CancellationToken.None);
        Assert.False(publishedB.Published);
        Assert.Null(publishedB.Capability);
        Assert.Same(closure, publishedB.Closure);

        // A is untouched: lookup, borrow and consume still work on live handles.
        Assert.True(store.TryLookup(identityA.PreparationId, out var liveA));
        Assert.Same(capabilityA, liveA);
        Assert.True(liveA!.TryBorrowClosure(out var borrowedAgain));
        Assert.All(borrowedAgain!.Objects, o =>
        {
            Assert.False(o.Handle.IsClosed);
            Assert.Equal(0, host.StatHandle(o.Handle, out _));
        });
        Assert.True(store.TryConsume(liveA, out var violation));
        Assert.Equal(InvariantViolation.None, violation);

        // Invalidating the sole owner closes handles exactly once.
        Assert.True(capabilityA.TryBorrowClosure(out var before));
        var handle = before!.Objects[0].Handle;
        Assert.False(handle.IsClosed);
        capabilityA.Dispose();
        Assert.True(handle.IsClosed);
        capabilityA.Dispose();
        Assert.False(store.TryLookup(identityA.PreparationId, out _));
        Assert.False(capabilityA.TryBorrowClosure(out _));
    }

    [Fact]
    public void SecondMintSameIdentityRefusesWithoutOwnershipDamage()
    {
        var host = NewHost();
        var closure = Stage(host, Bytes(0x32, 128));
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        var identity = IdentityFor(manifest);
        var first = store.Mint(manifest, identity, 1_000_000_000, closure);
        Assert.Throws<ArgumentException>(() => store.Mint(manifest, identity, 1_000_000_000, closure));
        Assert.Equal(ClosureOwnership.StoreOwned, closure.Ownership);
        Assert.True(first.TryBorrowClosure(out _));
        first.Dispose();
    }

    [Fact]
    public void SecondMintDistinctIdentityRefusesAndCreatesNoEntry()
    {
        var host = NewHost();
        var closure = Stage(host, Bytes(0x33, 128));
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        var first = store.Mint(manifest, IdentityFor(manifest), 1_000_000_000, closure);
        var secondIdentity = DistinctIdentity(manifest);
        var ex = Assert.Throws<ArgumentException>(() =>
            store.Mint(manifest, secondIdentity, 1_000_000_000, closure));
        Assert.Contains("at most one preparation", ex.Message);
        Assert.False(store.TryLookup(secondIdentity.PreparationId, out _));
        Assert.True(first.TryBorrowClosure(out _));
        first.Dispose();
    }

    [Fact]
    public void MismatchedManifestMintLeavesClosureUnclaimed()
    {
        var host = NewHost();
        var closure = Stage(host, Bytes(0x34, 128));
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        Assert.Throws<ArgumentException>(() =>
            store.Mint(SampleManifest(), IdentityFor(manifest), 1_000_000_000, closure));
        // Refusal happened before the irreversible transition: the closure
        // is still builder-owned and a later valid Mint succeeds.
        Assert.Equal(ClosureOwnership.BuilderOwned, closure.Ownership);
        var capability = store.Mint(manifest, IdentityFor(manifest), 1_000_000_000, closure);
        Assert.True(capability.TryBorrowClosure(out _));
        capability.Dispose();
    }

    [Fact]
    public void ReleasedClosureCannotMint()
    {
        var host = NewHost();
        var closure = Stage(host, Bytes(0x35, 128));
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        closure.Dispose();
        Assert.Equal(ClosureOwnership.Released, closure.Ownership);
        Assert.False(closure.AuthorityLive);
        Assert.Throws<ArgumentException>(() =>
            store.Mint(manifest, IdentityFor(manifest), 1_000_000_000, closure));
    }

    [Fact]
    public async Task ConcurrentMintOfSameClosureYieldsExactlyOneSuccess()
    {
        var host = NewHost();
        var closure = Stage(host, Bytes(0x36, 128));
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        var digest = ManifestCodec.ComputeDigestBytes(manifest);
        const int Attempts = 32;
        var winners = new ConcurrentBag<PreparedTransaction>();
        var refusals = 0;
        var tasks = Enumerable.Range(0, Attempts).Select(_ => Task.Run(() =>
        {
            var identity = new PreparationIdentity(
                Guid.NewGuid(), Issuer, Generation, Guid.NewGuid(), (byte[])digest.Clone());
            try
            {
                winners.Add(store.Mint(manifest, identity, 1_000_000_000, closure));
            }
            catch (ArgumentException)
            {
                Interlocked.Increment(ref refusals);
            }
        })).ToArray();
        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        var winner = Assert.Single(winners);
        Assert.Equal(Attempts - 1, refusals);

        Assert.True(store.TryLookup(winner.Identity.PreparationId, out _));
        Assert.True(winner.TryBorrowClosure(out var borrowed));
        Assert.All(borrowed!.Objects, o => Assert.False(o.Handle.IsClosed));
        winner.Dispose();
    }

    [Fact]
    public void FabricatedEquivalentIdentityCannotBorrow()
    {
        var host = NewHost();
        var closure = Stage(host, Bytes(0x37, 128));
        var (store, _) = NewStore();
        var manifest = ManifestWithClosure(closure.Objects);
        var capability = store.Mint(manifest, IdentityFor(manifest), 1_000_000_000, closure);

        // A twin capability from another store carries identical identifiers
        // but is a different object: foreign here, borrows nothing.
        var other = CopyTestSupport.NewStore(new TestClock());
        var twin = other.Mint(
            manifest, IdentityFor(manifest), 1_000_000_000, TestClosure.Empty());
        Assert.False(store.TryBorrowClosure(twin, out _));
        Assert.False(other.TryBorrowClosure(capability, out _));
        capability.Dispose();
        twin.Dispose();
    }
}
