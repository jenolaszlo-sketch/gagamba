// B5 T1: sealed monotonic clock trust. Production trust comes from the sealed
// concrete type and the shared Stopwatch.GetTimestamp() conversion, not from
// an enum label supplied by a caller.
using System.Diagnostics;
using System.Reflection;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class Wp05B5C3ClockTrustTests
{
    private const long Accepted = 2_000_000_000;
    private const long Deadline = Accepted + 5_000_000_000;

    private static StopRecord Stop() => new(Accepted, StopCauseKind.CallerStop, Deadline);

    private static OwnershipSnapshot Record(StopRecord? stop) =>
        RecoveryTestSupport.V3Snapshot(
            stop: stop,
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: (byte)CleanupComponents.DomainRemoval);

    private static (DomainLease Lease, FakeDomainBackend Backend) LiveLease()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = false };
        return (new DomainLease(backend, RecoveryTestSupport.Identity()), backend);
    }

    [Fact]
    public void ProductionClockIsSealedAndUsesSharedConversion()
    {
        var type = typeof(StopwatchDomainClock);
        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Single(type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance));

        // The conversion is the accepted OwnerClock formula.
        Assert.Equal(0, MonotonicTime.ToNanoseconds(0));
        Assert.Equal(1_000_000_000, MonotonicTime.ToNanoseconds(Stopwatch.Frequency));

        // The production clock preserves the shared epoch: it is not rebased
        // to object construction (the singleton already existed).
        var before = MonotonicTime.NowNanoseconds();
        var reading = StopwatchDomainClock.Instance.NowNanoseconds;
        var after = MonotonicTime.NowNanoseconds();
        Assert.InRange(reading, before, after);
        Assert.Equal(MonotonicEpoch.Qpc, StopwatchDomainClock.Instance.Basis.Epoch);
        Assert.Equal(Stopwatch.Frequency, StopwatchDomainClock.Instance.Basis.Frequency);
    }

    [Fact]
    public void CompatibleClockConsumesPersistedDeadline()
    {
        var stop = Stop();
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };
        var (lease, backend) = LiveLease();

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(stop), "boot-1", lease, null, null, null);

        // One second of the original deadline remains: 40 x 25 ms polls, not
        // the caller's five-second allowance.
        Assert.Equal(40, clock.Delays);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.True(backend.WriteKillCalls > 0);
    }

    [Fact]
    public void UnknownEpochRefusedBeforeDestructiveAction()
    {
        var clock = new FakeDomainClock
        {
            NowNanoseconds = Deadline - 1_000_000_000,
            Basis = new MonotonicClockBasis(MonotonicEpoch.Unknown, Stopwatch.Frequency),
        };
        var (lease, backend) = LiveLease();

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(Stop()), "boot-1", lease, null, null, null);

        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
        Assert.Equal(0, backend.WriteKillCalls);
        Assert.Equal(0, clock.Delays);
    }

    [Fact]
    public void ConstructionRelativeClockRefused()
    {
        var clock = new RelativeTestClock();
        var (lease, backend) = LiveLease();

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(Stop()), "boot-1", lease, null, null, null);

        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
        Assert.Equal(0, backend.WriteKillCalls);
    }

    [Fact]
    public void FrequencyMismatchRefused()
    {
        var clock = new FakeDomainClock
        {
            NowNanoseconds = Deadline - 1_000_000_000,
            Basis = new MonotonicClockBasis(MonotonicEpoch.Qpc, Stopwatch.Frequency + 1),
        };
        var (lease, backend) = LiveLease();

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(Stop()), "boot-1", lease, null, null, null);

        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
        Assert.Equal(0, backend.WriteKillCalls);
    }

    [Fact]
    public void UnavailableClockRefused()
    {
        var (lease, backend) = LiveLease();

        var result = new Reconciler(new ThrowingTestClock(), TimeSpan.FromSeconds(5))
            .Reconcile(Record(Stop()), "boot-1", lease, null, null, null);

        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
        Assert.Equal(0, backend.WriteKillCalls);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void NonpositiveTimestampRefused(long now)
    {
        var clock = new FakeDomainClock { NowNanoseconds = now };
        var (lease, backend) = LiveLease();

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(Stop()), "boot-1", lease, null, null, null);

        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
        Assert.Equal(0, backend.WriteKillCalls);
    }

    [Theory]
    [InlineData(0L, 5_000_000_000L)]
    [InlineData(-1L, 4_000_000_000L)]
    [InlineData(Accepted, Deadline + 1)]
    [InlineData(Deadline, Accepted)]
    public void InvalidDeadlineProvenanceRefused(long accepted, long deadline)
    {
        var clock = new FakeDomainClock { NowNanoseconds = 3_000_000_000 };
        var (lease, backend) = LiveLease();
        var stop = new StopRecord(accepted, StopCauseKind.CallerStop, deadline);

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(stop), "boot-1", lease, null, null, null);

        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
        Assert.Equal(0, backend.WriteKillCalls);
    }

    [Fact]
    public void DeadlineArithmeticOverflowRefusedConservatively()
    {
        // Near-maximum accepted time plus the fixed allowance overflows the
        // persisted deadline relation; nothing may be fabricated.
        var stop = new StopRecord(long.MaxValue - 1, StopCauseKind.CallerStop, long.MaxValue);
        var clock = new FakeDomainClock { NowNanoseconds = 1 };
        var (lease, backend) = LiveLease();

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(stop), "boot-1", lease, null, null, null);

        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
        Assert.Equal(0, backend.WriteKillCalls);
    }

    [Fact]
    public void CrossBootMismatchStillRefused()
    {
        var clock = new FakeDomainClock { NowNanoseconds = 3_000_000_000 };
        var (lease, _) = LiveLease();

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(Stop()), "different-boot", lease, null, null, null);

        Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
    }

    private sealed class RelativeTestClock : IDomainClock
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();

        public MonotonicClockBasis Basis => new(MonotonicEpoch.Unknown, Stopwatch.Frequency);

        public long NowNanoseconds => _watch.ElapsedTicks * (1_000_000_000L / Stopwatch.Frequency);

        public void Delay(TimeSpan duration) => Thread.Sleep(duration);
    }

    private sealed class ThrowingTestClock : IDomainClock
    {
        public MonotonicClockBasis Basis => new(MonotonicEpoch.Qpc, Stopwatch.Frequency);

        public long NowNanoseconds => throw new InvalidOperationException("clock unavailable");

        public void Delay(TimeSpan duration) => throw new InvalidOperationException("clock unavailable");
    }
}
