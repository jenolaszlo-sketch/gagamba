using Gagamba.Execution;
using Xunit;

namespace Gagamba.Execution.Tests;

public sealed class Ar4CoreTests
{
    [Fact]
    public void SharedCapabilityMapsRejectMutationAndKeepNegotiationStable()
    {
        var before = WellKnownPlatforms.Windows.Grants[ExecutionCapability.UnitTermination];
        var grants = Assert.IsAssignableFrom<IDictionary<ExecutionCapability, CapabilityGrant>>(
            WellKnownPlatforms.Windows.Grants);
        Assert.ThrowsAny<Exception>(() => grants[ExecutionCapability.UnitTermination] =
            new CapabilityGrant(CapabilityLevel.Absent, GuaranteeKind.None, "forged"));
        var composed = Assert.IsAssignableFrom<IDictionary<string,
            IReadOnlyDictionary<ExecutionCapability, CapabilityGrant>>>(WellKnownPlatforms.ComposedOwnerDeath);
        Assert.ThrowsAny<Exception>(() => composed.Clear());
        Assert.Equal(before, WellKnownPlatforms.Windows.Grants[ExecutionCapability.UnitTermination]);
        Assert.True(ExecutionNegotiator.Negotiate(WellKnownPlatforms.Windows,
            new[] { ExecutionRequirement.Require(ExecutionCapability.UnitTermination) }).Accepted);
        var negotiation = ExecutionNegotiator.Negotiate(WellKnownPlatforms.Windows,
            new[] { ExecutionRequirement.Require(ExecutionCapability.UnitTermination) });
        Assert.ThrowsAny<Exception>(() => ((IList<string>)negotiation.Met)[0] = "forged");
    }

    [Fact]
    public void InvocationAndRequirementsSnapshotCallerCollections()
    {
        var args = new List<string> { "", "two words", "a\"b", "C:\\trail\\", "雪" };
        var env = new Dictionary<string, string> { ["PATH"] = "first" };
        var input = ProcessStartSpec.Vector("tool", args, Path.GetTempPath(), env);
        args[1] = "changed";
        Assert.True(ProcessStartSpec.TrySnapshot(input, windows: false, out var captured, out var error), error);
        env["PATH"] = "changed";
        Assert.Equal("two words", captured!.ArgumentVector![1]);
        Assert.Equal("first", captured.Environment["PATH"]);
        Assert.ThrowsAny<Exception>(() => ((IDictionary<string, string>)captured.Environment)["PATH"] = "forged");
        var req = new List<ExecutionRequirement> { ExecutionRequirement.Require(ExecutionCapability.UnitTermination) };
        Assert.True(ExecutionRequirements.TrySnapshot(new ExecutionRequirements(req), out var snapshot, out error), error);
        req.Clear();
        Assert.Single(snapshot!.Required);
    }

    [Fact]
    public void InvalidInvocationFailsBeforeAnyProviderConsumesPreparation()
    {
        var invalid = new ProcessStartSpec("tool", "", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            new Dictionary<string, string>());
        Assert.False(ProcessStartSpec.TrySnapshot(invalid, windows: false, out _, out _));
        Assert.False(ProcessStartSpec.TrySnapshot(new ProcessStartSpec("\0", "", "",
            new Dictionary<string, string>()), windows: false, out _, out _));
        Assert.False(ProcessStartSpec.TrySnapshot(new ProcessStartSpec("tool", "", "",
            new Dictionary<string, string> { ["A=B"] = "x" }), windows: false, out _, out _));
        Assert.False(ProcessStartSpec.TrySnapshot(new ProcessStartSpec("tool", "", "",
            new Dictionary<string, string>()) { ArgumentVector = new[] { "bad\0" } },
            windows: false, out _, out _));
    }
}
