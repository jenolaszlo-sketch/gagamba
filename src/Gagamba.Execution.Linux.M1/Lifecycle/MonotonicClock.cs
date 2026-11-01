// WP05 C3 sealed monotonic clock provenance. Durable StopRecord timestamps are
// produced by OwnerClock using Stopwatch.GetTimestamp() and the exact
// nanosecond conversion below. Recovery may consume those timestamps only
// through a clock that declares the same epoch and frequency. A clock whose
// values are rebased to its own construction (Stopwatch.StartNew elapsed
// ticks) is not numerically comparable and must never feed deadline
// arithmetic.
using System.Diagnostics;

namespace Gagamba.Execution.Linux.M1.Lifecycle;

/// <summary>The declared timestamp basis of a monotonic clock. Only
/// <see cref="Qpc"/> is compatible with durable StopRecord timestamps.</summary>
internal enum MonotonicEpoch : byte
{
    Unknown = 0,

    /// <summary><c>Stopwatch.GetTimestamp()</c> with the OwnerClock
    /// nanosecond conversion and <c>Stopwatch.Frequency</c>. On Linux this is
    /// the .NET monotonic source (CLOCK_MONOTONIC-backed), not a literal
    /// Windows QPC device; cross-process comparability holds only for the same
    /// supported basis and a compatible boot context.</summary>
    Qpc = 1,
}

internal readonly record struct MonotonicClockBasis(MonotonicEpoch Epoch, long Frequency);

/// <summary>Single shared timestamp conversion. Every durable timestamp and
/// every trusted clock reading passes through this formula so the epoch is
/// never silently different.</summary>
internal static class MonotonicTime
{
    internal const long NanosecondsPerSecond = 1_000_000_000L;

    /// <summary>The accepted OwnerClock conversion.</summary>
    internal static long ToNanoseconds(long timestamp)
    {
        var frequency = Stopwatch.Frequency;
        if (frequency <= 0)
        {
            throw new InvalidOperationException("Stopwatch frequency is unavailable");
        }

        checked
        {
            return (timestamp / frequency * NanosecondsPerSecond)
                + (timestamp % frequency * NanosecondsPerSecond / frequency);
        }
    }

    internal static long NowNanoseconds() => ToNanoseconds(Stopwatch.GetTimestamp());
}

/// <summary>Sealed production clock for durable deadline arithmetic. It has a
/// fixed basis, no constructor parameters and no injectable epoch label, and
/// it never rebases readings to its own construction instant. Production
/// composition passes this concrete type to the Reconciler; the open
/// <see cref="IDomainClock"/> constructor exists only as a deterministic test
/// seam and cannot acquire production trust from an enum value alone.</summary>
internal sealed class StopwatchDomainClock : IDomainClock
{
    private static readonly MonotonicClockBasis FixedBasis =
        new(MonotonicEpoch.Qpc, Stopwatch.Frequency);

    private StopwatchDomainClock()
    {
    }

    internal static StopwatchDomainClock Instance { get; } = new();

    public MonotonicClockBasis Basis => FixedBasis;

    public long NowNanoseconds => MonotonicTime.NowNanoseconds();

    public void Delay(TimeSpan duration) => Thread.Sleep(duration);
}
