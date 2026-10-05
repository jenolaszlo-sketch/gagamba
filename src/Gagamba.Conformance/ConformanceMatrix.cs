// The expected conformance matrix, derived from GF-2's capability grants and
// the per-platform evidence (GW-2, GL-2, GM-2). This is the contract the
// conformance runner measures against; if a provider drifts, the runner
// fails rather than silently redefining the guarantee.
using Gagamba.Execution;

namespace Gagamba.Conformance;

/// <summary>How a same-session/setsid escape is expected to behave.</summary>
public enum EscapeExpectation
{
    /// <summary>Escape is prevented; the escapee dies with the domain.</summary>
    Resistant,
    /// <summary>Escape succeeds and is observable; the escapee survives.</summary>
    Observed,
    /// <summary>Not expressible on this OS (no setsid); capability attested.</summary>
    NotApplicable,
}

/// <summary>Expected conformance facts for one platform.</summary>
public sealed record PlatformConformance(
    string Platform,
    CapabilityLevel UnitTermination,
    CapabilityLevel SurvivesRootExit,
    EscapeExpectation Escape,
    CapabilityLevel EscapeResistant);

public static class ConformanceMatrix
{
    public static readonly string[] LegNames =
    {
        "capability-matrix",
        "opaque-handles",
        "prepare",
        "single-use",
        "working-directory",
        "no-ambient-inherit",
        "unit-termination",
        "root-exit",
        "dispose-cleanup",
        "completion",
        "setsid-escape",
    };

    public static bool TryForPlatform(string platform, out PlatformConformance expected)
    {
        expected = platform switch
        {
            "windows-job" => new("windows-job",
                CapabilityLevel.Full, CapabilityLevel.Full,
                EscapeExpectation.NotApplicable, CapabilityLevel.Full),
            "linux-cgroup-v2" => new("linux-cgroup-v2",
                CapabilityLevel.Full, CapabilityLevel.Full,
                EscapeExpectation.Resistant, CapabilityLevel.Partial),
            "macos-launchd-pg" => new("macos-launchd-pg",
                CapabilityLevel.Partial, CapabilityLevel.Full,
                EscapeExpectation.Observed, CapabilityLevel.Absent),
            _ => null!,
        };
        return expected is not null;
    }
}
