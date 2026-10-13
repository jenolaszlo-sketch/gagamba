// WP03 integration tests: the durable ordering around WP02 preparation —
// AllocationIntent before staging, AllocationRegistered + Prepared before
// Mint — without changing WP02 copy/binding.
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Linux.M1.Tests.Preparation;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Preparation.CopyTestSupport;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class PreparationOwnershipTests
{
    private sealed class FakeAuthority : ICleanupAuthoritySink
    {
        public bool Accept = true;
        public DomainLease? Retained;
        public int Calls;

        public bool TryRetainRecoveryHandles(DomainLease recoveryLease, PreparedClosure closure, out string error)
        {
            Calls++;
            if (!Accept)
            {
                error = "recovery authority refused";
                return false;
            }

            Retained = recoveryLease;
            error = string.Empty;
            return true;
        }
    }

    private static RestrictedManifest ManifestFor(params (string Role, byte[] Content)[] objects)
    {
        var baseline = SampleManifest();
        var closureObjects = objects
            .Select(o => new ClosureObject(o.Role, Digest(o.Content), (ulong)o.Content.Length, "/" + o.Role, "test-build"))
            .ToArray();
        return new RestrictedManifest(
            RestrictedManifest.CurrentSchemaVersion,
            RestrictedManifest.ImplementationIdentifier,
            baseline.Authority,
            baseline.Resources,
            new ClosureIdentity(closureObjects, Array.Empty<string>()),
            baseline.Invocation,
            baseline.Channels);
    }

    private static (PreparationOwnershipCoordinator Coordinator, OwnershipJournal Journal, FakeJournalStorage Storage)
        NewCoordinator()
    {
        var storage = new FakeJournalStorage();
        var journal = new OwnershipJournal(storage, "/recovery");
        var template = RecoveryTestSupport.Snapshot(stage: PreparationStage.None);
        return (new PreparationOwnershipCoordinator(journal, template), journal, storage);
    }

    private static JournalWriterLease Acquire(OwnershipJournal journal)
    {
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        return lease!;
    }

    [Fact]
    public void IntegratedOrderPublishesWithDurableRecords()
    {
        var host = Preparation.CopyTestSupport.NewHost();
        var tool = Bytes(0x10, 1024);
        var input = Bytes(0x20, 256);
        host.AddSourceFile("tool", tool);
        host.AddSourceFile("input", input);

        var (coordinator, journal, storage) = NewCoordinator();
        var manifest = ManifestFor(("tool", tool), ("input", input));
        var identity = IdentityFor(manifest);
        var (store, _) = NewStoreAndClock();
        var domain = new DomainLease(new FakeDomainBackend(), RecoveryTestSupport.Identity());
        var authority = new FakeAuthority();
        using var writer = Acquire(journal);

        var outcome = coordinator.Run(
            host, Plan(host, Tool(), Input()), domain, authority, writer, store, manifest, identity,
            1_000_000_000, CancellationToken.None);

        Assert.True(outcome.Published, outcome.Detail);
        Assert.NotNull(outcome.Capability);
        Assert.Equal(1, authority.Calls);

        var read = new OwnershipJournal(storage, "/recovery").ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(PreparationStage.Prepared, read.Snapshot!.Stage);
        Assert.Equal(3, read.Snapshot.Sequence);
        Assert.NotNull(read.Snapshot.Preparation);
        Assert.Contains(read.Snapshot.Allocations, a => a.Kind == AllocationKind.Domain);
        Assert.Contains(read.Snapshot.Allocations, a => a.Kind == AllocationKind.Staging);
        outcome.Closure?.Dispose();
    }

    [Fact]
    public void IntentFailurePreventsStaging()
    {
        var host = Preparation.CopyTestSupport.NewHost();
        host.AddSourceFile("tool", Bytes(0x30, 128));

        var (coordinator, journal, storage) = NewCoordinator();
        storage.Fault = step => step == "create" ? JournalIoResult.Failure("intent not durable") : null;

        var manifest = ManifestFor(("tool", Bytes(0x30, 128)));
        var identity = IdentityFor(manifest);
        var (store, _) = NewStoreAndClock();
        var domain = new DomainLease(new FakeDomainBackend(), RecoveryTestSupport.Identity());
        using var writer = Acquire(journal);

        var outcome = coordinator.Run(
            host, Plan(host, Tool()), domain, new FakeAuthority(), writer, store, manifest, identity,
            1_000_000_000, CancellationToken.None);

        Assert.False(outcome.Published);
        Assert.Contains("AllocationIntent", outcome.Detail);
        Assert.Empty(host.StagingFileNames());
    }

    [Fact]
    public void RegistrationFailurePublishesNothingAndRetainsAccounted()
    {
        var host = Preparation.CopyTestSupport.NewHost();
        var tool = Bytes(0x40, 512);
        host.AddSourceFile("tool", tool);

        var (coordinator, journal, storage) = NewCoordinator();
        // Succeed the first write (intent) then fail the registered write.
        var writes = 0;
        storage.Fault = step =>
        {
            if (step == "create")
            {
                writes++;
                if (writes == 2)
                {
                    return JournalIoResult.Failure("registered not durable");
                }
            }

            return null;
        };

        var manifest = ManifestFor(("tool", tool));
        var identity = IdentityFor(manifest);
        var (store, _) = NewStoreAndClock();
        var domain = new DomainLease(new FakeDomainBackend(), RecoveryTestSupport.Identity());
        using var writer = Acquire(journal);

        var outcome = coordinator.Run(
            host, Plan(host, Tool()), domain, new FakeAuthority(), writer, store, manifest, identity,
            1_000_000_000, CancellationToken.None);

        Assert.False(outcome.Published);
        Assert.Null(outcome.Capability);
        Assert.NotNull(outcome.Closure);
        Assert.False(store.TryLookup(identity.PreparationId, out _));
        outcome.Closure!.Dispose();
    }

    [Fact]
    public void RecoveryAuthorityRefusalPublishesNothing()
    {
        var host = Preparation.CopyTestSupport.NewHost();
        var tool = Bytes(0x50, 256);
        host.AddSourceFile("tool", tool);

        var (coordinator, journal, _) = NewCoordinator();
        var manifest = ManifestFor(("tool", tool));
        var identity = IdentityFor(manifest);
        var (store, _) = NewStoreAndClock();
        var domain = new DomainLease(new FakeDomainBackend(), RecoveryTestSupport.Identity());
        var authority = new FakeAuthority { Accept = false };
        using var writer = Acquire(journal);

        var outcome = coordinator.Run(
            host, Plan(host, Tool()), domain, authority, writer, store, manifest, identity,
            1_000_000_000, CancellationToken.None);

        Assert.False(outcome.Published);
        Assert.False(store.TryLookup(identity.PreparationId, out _));
        outcome.Closure?.Dispose();
    }

    private static (PreparedTransaction.PreparationStore Store, TestClock Clock) NewStoreAndClock()
    {
        var clock = new TestClock();
        return (CopyTestSupport.NewStore(clock), clock);
    }
}
