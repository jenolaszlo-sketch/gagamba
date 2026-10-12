// WP03 DomainLease: owns concrete live authority for one delegated cgroup
// domain and, separately, the recovery scope's independent duplicate
// handles. Disposal never implies successful stop/cleanup. Whole-domain
// stop writes through the retained cgroup.kill handle and observes
// cgroup.events; removal re-checks the child identity under the retained
// parent so a same-name replacement can never be removed or adopted.
namespace Gagamba.Execution.Linux.M1.Lifecycle;

internal readonly record struct StopOutcome(bool Stopped, bool TimedOut, string Error)
{
    internal static StopOutcome Success { get; } = new(true, false, string.Empty);

    internal static StopOutcome Timeout(string error) => new(false, true, error);

    internal static StopOutcome Failure(string error) => new(false, false, error);
}

internal readonly record struct IdentityCheck(bool Match, string Error)
{
    internal static IdentityCheck Matched { get; } = new(true, string.Empty);

    internal static IdentityCheck Mismatch(string error) => new(false, error);
}

internal readonly record struct RemovalOutcome(bool Removed, bool Refused, string Error)
{
    internal static RemovalOutcome Done { get; } = new(true, false, string.Empty);

    internal static RemovalOutcome Refusal(string error) => new(false, true, error);

    internal static RemovalOutcome Failure(string error) => new(false, false, error);
}

/// <summary>Bounded monotonic clock and sleeper for stop/observe loops.</summary>
internal interface IDomainClock
{
    long NowNanoseconds { get; }

    void Delay(TimeSpan duration);
}

internal sealed class DomainLease : IDisposable
{
    private readonly IDomainBackend _backend;
    private bool _disposed;

    internal DomainLease(IDomainBackend backend, DomainIdentity identity)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
    }

    internal DomainIdentity Identity { get; }

    internal Guid LeaseGeneration => Identity.LeaseGeneration;

    internal bool AuthorityAlive => !_disposed && _backend.HandlesAlive;

    /// <summary>An independent handle set for the recovery scope. Disposing
    /// either side must not affect the other.</summary>
    internal DomainLease DuplicateForRecovery() =>
        new(_backend.DuplicateForRecovery(), Identity);

    internal DomainIoResult RequestStop() => _disposed
        ? DomainIoResult.Failure("lease disposed; no stop authority")
        : _backend.WriteKill();

    internal DomainPopulationResult ObservePopulation() => _disposed
        ? DomainPopulationResult.Unknown("lease disposed; no observation authority")
        : _backend.ReadPopulation();

    /// <summary>Whole-domain stop: kill through the retained handle, then
    /// bounded poll of the retained events handle for populated=0.</summary>
    internal StopOutcome StopAndAwaitEmpty(TimeSpan timeout, IDomainClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (_disposed)
        {
            return StopOutcome.Failure("lease disposed; no stop authority");
        }

        var kill = _backend.WriteKill();
        if (!kill.Ok)
        {
            return StopOutcome.Failure(kill.Error);
        }

        var deadline = clock.NowNanoseconds + (long)(timeout.TotalSeconds * 1_000_000_000d);
        while (true)
        {
            var population = _backend.ReadPopulation();
            if (!population.Known)
            {
                return StopOutcome.Failure(population.Error);
            }

            if (population.Empty)
            {
                return StopOutcome.Success;
            }

            if (clock.NowNanoseconds >= deadline)
            {
                return StopOutcome.Timeout("domain did not empty within the stop allowance");
            }

            clock.Delay(TimeSpan.FromMilliseconds(25));
        }
    }

    /// <summary>Re-verifies the retained directory handle still refers to
    /// the exact captured object (dev/ino and directory type).</summary>
    internal IdentityCheck VerifyIdentity()
    {
        if (_disposed)
        {
            return IdentityCheck.Mismatch("lease disposed; identity unverifiable");
        }

        var stat = _backend.StatDomain();
        if (!stat.Ok)
        {
            return IdentityCheck.Mismatch(stat.Error);
        }

        if ((stat.Mode & 0xF000) != 0x4000)
        {
            return IdentityCheck.Mismatch("domain handle is not a directory");
        }

        if (stat.Device != Identity.ChildDevice || stat.Inode != Identity.ChildInode)
        {
            return IdentityCheck.Mismatch("domain handle identity changed");
        }

        return IdentityCheck.Matched;
    }

    /// <summary>Identity-checked removal: look the child up again under the
    /// retained parent, require the exact captured dev/ino, then remove. A
    /// mismatch or replacement refuses removal and reports without adopting.</summary>
    internal RemovalOutcome RemoveIfIdentityMatches()
    {
        if (_disposed)
        {
            return RemovalOutcome.Failure("lease disposed; no removal authority");
        }

        var lookup = _backend.LookupChild(Identity.ChildName);
        if (!lookup.Ok)
        {
            return RemovalOutcome.Refusal(lookup.Error);
        }

        if (lookup.Device != Identity.ChildDevice || lookup.Inode != Identity.ChildInode)
        {
            return RemovalOutcome.Refusal("child identity mismatch; refusing removal");
        }

        var remove = _backend.RemoveChild(Identity.ChildName);
        return remove.Ok ? RemovalOutcome.Done : RemovalOutcome.Failure(remove.Error);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _backend.Dispose();
        GC.SuppressFinalize(this);
    }
}
