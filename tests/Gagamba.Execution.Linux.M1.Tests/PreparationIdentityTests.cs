// WP01 capability identity tests: store minting, binding and refusal.
// Portable pure logic with a manually advanced fake clock.
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Restricted;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests;

public sealed class PreparationIdentityTests
{
    private sealed class FakeClock : ITransactionClock
    {
        public long NowNanoseconds { get; set; }
    }

    private static readonly Guid Issuer = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Generation = new("22222222-2222-2222-2222-222222222222");

    private static PreparedTransaction.PreparationStore NewStore(FakeClock clock) =>
        new(Issuer, Generation, clock);

    private static (PreparedTransaction.PreparationStore Store, FakeClock Clock, PreparedTransaction Capability) MintValid(long expiryNs = 1_000_000_000)
    {
        var clock = new FakeClock { NowNanoseconds = 0 };
        var store = NewStore(clock);
        var manifest = SampleManifest();
        var identity = new PreparationIdentity(
            new Guid("33333333-3333-3333-3333-333333333333"),
            Issuer,
            Generation,
            new Guid("44444444-4444-4444-4444-444444444444"),
            ManifestCodec.ComputeDigestBytes(manifest));
        return (store, clock, store.Mint(manifest, identity, expiryNs));
    }

    [Fact]
    public void MintBindsDigestAndResolvesSameReference()
    {
        var (store, _, capability) = MintValid();
        Assert.True(store.TryLookup(capability.Identity.PreparationId, out var found));
        Assert.Same(capability, found);
        Assert.Equal(
            ManifestCodec.ComputeDigestHex(capability.Manifest),
            Convert.ToHexString(capability.Identity.ManifestDigest.ToArray()).ToLowerInvariant());
    }

    [Fact]
    public void ForeignIssuerRefused()
    {
        var clock = new FakeClock();
        var store = NewStore(clock);
        var manifest = SampleManifest();
        var identity = new PreparationIdentity(
            Guid.NewGuid(), Guid.NewGuid(), Generation, Guid.NewGuid(),
            ManifestCodec.ComputeDigestBytes(manifest));
        Assert.Throws<ArgumentException>(() => store.Mint(manifest, identity, 1_000_000_000));
    }

    [Fact]
    public void ForeignGenerationRefused()
    {
        var clock = new FakeClock();
        var store = NewStore(clock);
        var manifest = SampleManifest();
        var identity = new PreparationIdentity(
            Guid.NewGuid(), Issuer, Guid.NewGuid(), Guid.NewGuid(),
            ManifestCodec.ComputeDigestBytes(manifest));
        Assert.Throws<ArgumentException>(() => store.Mint(manifest, identity, 1_000_000_000));
    }

    [Fact]
    public void DigestMismatchRefused()
    {
        var clock = new FakeClock();
        var store = NewStore(clock);
        var identity = new PreparationIdentity(
            Guid.NewGuid(), Issuer, Generation, Guid.NewGuid(), Bytes32(0xAA));
        Assert.Throws<ArgumentException>(() => store.Mint(SampleManifest(), identity, 1_000_000_000));
    }

    [Fact]
    public void MintPastExpiryRefused()
    {
        var clock = new FakeClock { NowNanoseconds = 5_000 };
        var store = NewStore(clock);
        var manifest = SampleManifest();
        var identity = new PreparationIdentity(
            Guid.NewGuid(), Issuer, Generation, Guid.NewGuid(),
            ManifestCodec.ComputeDigestBytes(manifest));
        Assert.Throws<ArgumentException>(() => store.Mint(manifest, identity, 5_000));
        Assert.Throws<ArgumentException>(() => store.Mint(manifest, identity, 1_000));
    }

    [Fact]
    public void DuplicateConsumeRefused()
    {
        var (store, _, capability) = MintValid();
        Assert.True(store.TryConsume(capability, out var first));
        Assert.Equal(InvariantViolation.None, first);
        Assert.False(store.TryConsume(capability, out var second));
        Assert.Equal(InvariantViolation.Consumed, second);
    }

    [Fact]
    public void AbortedCapabilityRefused()
    {
        var (store, _, capability) = MintValid();
        capability.Dispose();
        Assert.False(store.TryConsume(capability, out var violation));
        Assert.Equal(InvariantViolation.Aborted, violation);
        Assert.False(store.TryLookup(capability.Identity.PreparationId, out _));
    }

    [Fact]
    public void ExpiredRefused()
    {
        var (store, clock, capability) = MintValid(expiryNs: 1_000);
        clock.NowNanoseconds = 1_000;
        Assert.False(store.TryConsume(capability, out var violation));
        Assert.Equal(InvariantViolation.Expired, violation);
        Assert.False(store.TryLookup(capability.Identity.PreparationId, out _));
    }

    [Fact]
    public void FabricatedIdentifierRefused()
    {
        var (firstStore, _, _) = MintValid();
        var secondClock = new FakeClock();
        var secondStore = new PreparedTransaction.PreparationStore(
            new Guid("55555555-5555-5555-5555-555555555555"),
            new Guid("66666666-6666-6666-6666-666666666666"),
            secondClock);
        var manifest = SampleManifest();
        var identity = new PreparationIdentity(
            Guid.NewGuid(),
            new Guid("55555555-5555-5555-5555-555555555555"),
            new Guid("66666666-6666-6666-6666-666666666666"),
            Guid.NewGuid(),
            ManifestCodec.ComputeDigestBytes(manifest));
        var foreign = secondStore.Mint(manifest, identity, 1_000_000_000);
        Assert.False(firstStore.TryConsume(foreign, out var violation));
        Assert.Equal(InvariantViolation.Foreign, violation);
    }

    [Fact]
    public void LookupValidatesExpiryAndState()
    {
        var clock = new FakeClock { NowNanoseconds = 0 };
        var store = NewStore(clock);
        var manifest = SampleManifest();
        PreparedTransaction Mint(long expiry)
        {
            var identity = new PreparationIdentity(
                Guid.NewGuid(), Issuer, Generation, Guid.NewGuid(),
                ManifestCodec.ComputeDigestBytes(manifest));
            return store.Mint(manifest, identity, expiry);
        }

        var capability = Mint(1_000);
        Assert.True(store.TryLookup(capability.Identity.PreparationId, out _));
        Assert.True(store.TryConsume(capability, out _));
        Assert.False(store.TryLookup(capability.Identity.PreparationId, out _));
        var live = Mint(5_000);
        Assert.True(store.TryLookup(live.Identity.PreparationId, out _));
        clock.NowNanoseconds = 6_000;
        Assert.False(store.TryLookup(live.Identity.PreparationId, out _));
    }
}
