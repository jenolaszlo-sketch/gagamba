// GP-2 contract tests: the matrix stays honest and negotiation stays
// fail-closed. Portable pure logic; runs on every CI OS.
using Gagamba.Execution;
using Xunit;

namespace Gagamba.Execution.Tests;

public sealed class MatrixTests
{
    [Fact]
    public void AllPlatformsListEveryCapabilityWithEvidence()
    {
        Assert.Empty(MatrixValidation.ValidateAll(WellKnownPlatforms.All));
    }

    [Fact]
    public void NoSandboxBooleanAnywhere()
    {
        // Hard rule: a workload can be well-owned without a security
        // boundary and vice versa, so no boolean may pretend otherwise.
        var offenders = typeof(PlatformCapabilities).Assembly.GetTypes()
            .SelectMany(t => t.GetMembers(
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(m => m.Name.Contains("Sandbox", StringComparison.OrdinalIgnoreCase))
                .Select(m => $"{t.Name}.{m.Name}"))
            .ToList();
        Assert.True(offenders.Count == 0,
            "sandbox-shaped API surface: " + string.Join(", ", offenders));
    }
}

public sealed class NegotiationTests
{
    private static ExecutionRequirement Req(
        ExecutionCapability cap,
        CapabilityLevel min = CapabilityLevel.Full,
        bool constructed = false) =>
        ExecutionRequirement.Require(cap, min, constructed);

    [Fact]
    public void StrictCodingAgent_WindowsOnly()
    {
        // require: unit-termination, survive-root-exit, escape-resistant.
        var reqs = new[]
        {
            Req(ExecutionCapability.UnitTermination),
            Req(ExecutionCapability.SurvivesRootExit),
            Req(ExecutionCapability.EscapeResistant),
        };
        var win = ExecutionNegotiator.Negotiate(WellKnownPlatforms.Windows, reqs);
        var lin = ExecutionNegotiator.Negotiate(WellKnownPlatforms.LinuxCgroupV2, reqs);
        var mac = ExecutionNegotiator.Negotiate(WellKnownPlatforms.MacOs, reqs);
        Assert.True(win.Accepted);
        Assert.False(lin.Accepted); // EscapeResistant is Partial there
        Assert.False(mac.Accepted); // EscapeResistant Absent, UnitTermination Partial
        Assert.Contains(lin.Unmet, u => u.Contains("EscapeResistant"));
        Assert.True(mac.Unmet.Count >= 2); // escape + unit-termination floor
    }

    [Fact]
    public void OwnerDeath_NativeOnlyMeansWindows()
    {
        var reqs = new[] { Req(ExecutionCapability.OwnerDeathCleanup) };
        Assert.True(ExecutionNegotiator.Negotiate(WellKnownPlatforms.Windows, reqs).Accepted);
        Assert.False(ExecutionNegotiator.Negotiate(WellKnownPlatforms.LinuxCgroupV2, reqs).Accepted);
        Assert.False(ExecutionNegotiator.Negotiate(WellKnownPlatforms.MacOs, reqs).Accepted);
    }

    [Fact]
    public void OwnerDeath_ComposedAcceptedSeparatelyAndNeverEquated()
    {
        var reqs = new[] { Req(ExecutionCapability.OwnerDeathCleanup, CapabilityLevel.Partial, true) };
        var lin = ExecutionNegotiator.Negotiate(
            WellKnownPlatforms.LinuxCgroupV2, reqs, null,
            WellKnownPlatforms.ComposedOwnerDeath["linux-cgroup-v2"]);
        var mac = ExecutionNegotiator.Negotiate(
            WellKnownPlatforms.MacOs, reqs, null,
            WellKnownPlatforms.ComposedOwnerDeath["macos-launchd-pg"]);
        Assert.True(lin.Accepted);
        Assert.True(mac.Accepted);
        // Both accepted, but on visibly different grounds: Partial/Constructed
        // with different evidence. Never silently identical.
        Assert.Contains(lin.Met, m => m.Contains("Constructed"));
        Assert.Contains(mac.Met, m => m.Contains("Constructed"));
        Assert.NotEqual(
            WellKnownPlatforms.ComposedOwnerDeath["linux-cgroup-v2"][ExecutionCapability.OwnerDeathCleanup].Evidence,
            WellKnownPlatforms.ComposedOwnerDeath["macos-launchd-pg"][ExecutionCapability.OwnerDeathCleanup].Evidence);
    }

    [Fact]
    public void PreferredNeverDecides()
    {
        var r = ExecutionNegotiator.Negotiate(
            WellKnownPlatforms.MacOs,
            Array.Empty<ExecutionRequirement>(),
            new[] { Req(ExecutionCapability.EscapeResistant) });
        Assert.True(r.Accepted);
        Assert.Single(r.PreferredNotes);
    }

    [Fact]
    public void RejectionNamesEveryUnmetRequirement()
    {
        var reqs = new[]
        {
            Req(ExecutionCapability.EscapeResistant),
            Req(ExecutionCapability.KernelOwnedLifecycle),
        };
        var r = ExecutionNegotiator.Negotiate(WellKnownPlatforms.MacOs, reqs);
        Assert.False(r.Accepted);
        Assert.Equal(2, r.Unmet.Count);
        Assert.All(r.Unmet, u => Assert.Contains("macos-launchd-pg", u));
    }
}
