// WP03 domain-lease tests: live-handle authority, identity verification,
// whole-domain stop, bounded stop timeout, identity-checked removal and
// independent recovery duplicates.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class DomainLeaseTests
{
    private static (DomainLease Lease, FakeDomainBackend Backend) New(FakeDomainBackend? backend = null)
    {
        var b = backend ?? new FakeDomainBackend();
        return (new DomainLease(b, RecoveryTestSupport.Identity()), b);
    }

    [Fact]
    public void IdentityVerificationMatches()
    {
        var (lease, _) = New();
        Assert.True(lease.VerifyIdentity().Match);
    }

    [Fact]
    public void IdentityMismatchDetected()
    {
        var backend = new FakeDomainBackend { DomainInode = 555 };
        var (lease, _) = New(backend);
        Assert.False(lease.VerifyIdentity().Match);
    }

    [Fact]
    public void StopEmptiesPopulatedDomain()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var (lease, _) = New(backend);
        var clock = new FakeDomainClock();
        var stop = lease.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), clock);
        Assert.True(stop.Stopped);
        Assert.Equal(1, backend.KillWrites);
    }

    [Fact]
    public void StopTimesOutWhenDomainStaysPopulated()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = false };
        var (lease, _) = New(backend);
        var clock = new FakeDomainClock();
        var stop = lease.StopAndAwaitEmpty(TimeSpan.FromMilliseconds(200), clock);
        Assert.False(stop.Stopped);
        Assert.True(stop.TimedOut);
    }

    [Fact]
    public void KillFaultReported()
    {
        var backend = new FakeDomainBackend { Populated = true, KillFault = true };
        var (lease, _) = New(backend);
        var stop = lease.StopAndAwaitEmpty(TimeSpan.FromSeconds(1), new FakeDomainClock());
        Assert.False(stop.Stopped);
        Assert.Contains("kill", stop.Error);
    }

    [Fact]
    public void ReadFaultReported()
    {
        var backend = new FakeDomainBackend { Populated = true, ReadFault = true };
        var (lease, _) = New(backend);
        var stop = lease.StopAndAwaitEmpty(TimeSpan.FromSeconds(1), new FakeDomainClock());
        Assert.False(stop.Stopped);
        Assert.Contains("fault", stop.Error);
    }

    [Fact]
    public void RemovalSucceedsWhenIdentityMatches()
    {
        var (lease, _) = New();
        var removal = lease.RemoveIfIdentityMatches();
        Assert.True(removal.Removed);
    }

    [Fact]
    public void RemovalRefusedOnReplacement()
    {
        var backend = new FakeDomainBackend { Replacement = true, ReplacementInode = 999 };
        var (lease, _) = New(backend);
        var removal = lease.RemoveIfIdentityMatches();
        Assert.True(removal.Refused);
        Assert.Contains("mismatch", removal.Error);
    }

    [Fact]
    public void RemovalFaultReported()
    {
        var backend = new FakeDomainBackend { RemoveFault = true };
        var (lease, _) = New(backend);
        var removal = lease.RemoveIfIdentityMatches();
        Assert.False(removal.Removed);
        Assert.False(removal.Refused);
    }

    [Fact]
    public void RecoveryDuplicateIsIndependent()
    {
        var (lease, backend) = New();
        var recovery = lease.DuplicateForRecovery();
        Assert.True(recovery.AuthorityAlive);
        lease.Dispose();
        Assert.False(lease.AuthorityAlive);
        Assert.True(recovery.AuthorityAlive);
        Assert.True(recovery.VerifyIdentity().Match);
        recovery.Dispose();
        Assert.False(recovery.AuthorityAlive);
        Assert.True(backend.HandlesAlive == false);
    }

    [Fact]
    public void DisposedLeaseHasNoAuthority()
    {
        var (lease, _) = New();
        lease.Dispose();
        Assert.False(lease.RequestStop().Ok);
        Assert.False(lease.ObservePopulation().Known);
        Assert.False(lease.VerifyIdentity().Match);
        Assert.False(lease.RemoveIfIdentityMatches().Removed);
    }
}
