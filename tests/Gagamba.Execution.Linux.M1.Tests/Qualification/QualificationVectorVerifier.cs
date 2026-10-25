// RC2A-02A independent decode-side vector verifier (test-only). It reads only
// the public view surface and compares every property against the committed
// independent template values; it never uses production encoding, digest or
// equality code to derive expectations.
using Gagamba.Execution.Restricted.Contracts;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

internal static class QualificationVectorVerifier
{
    internal static void VerifyGraph(QualificationWireTemplate expected, RestrictedManifestView view)
    {
        Assert.Equal(expected.SchemaVersion, view.SchemaVersion);
        Assert.Equal(expected.ImplementationId, view.ImplementationId);

        var authority = view.Authority;
        Assert.Equal(expected.Namespaces, authority.Namespaces.ToArray());
        Assert.Equal(expected.Capabilities, authority.Capabilities.ToArray());
        Assert.Equal(expected.NoNewPrivs, authority.NoNewPrivs);
        Assert.Equal(expected.FilterSha256, authority.FilterSha256.ToArray());
        Assert.Equal(expected.FilterLength, authority.FilterLength);
        Assert.Equal(expected.ReadOnlyDestinations, authority.ReadOnlyDestinations.ToArray());
        Assert.Equal(expected.Stores.Length, authority.WritableStores.Length);
        for (var i = 0; i < expected.Stores.Length; i++)
        {
            Assert.Equal(expected.Stores[i].Path, authority.WritableStores[i].Path);
            Assert.Equal(expected.Stores[i].Bytes, authority.WritableStores[i].Bytes);
            Assert.Equal(expected.Stores[i].HasInodeBudget, authority.WritableStores[i].HasInodeBudget);
            Assert.Equal(expected.Stores[i].InodeBudget, authority.WritableStores[i].InodeBudget);
        }

        Assert.Equal(expected.NoAmbientEnvironment, authority.NoAmbientEnvironment);
        Assert.Equal(expected.NoIpcEndpoints, authority.NoIpcEndpoints);
        Assert.Equal(expected.NoExtraInheritedFds, authority.NoExtraInheritedFds);
        Assert.Equal(expected.ToolExecutable, authority.MountPolicy.ToolExecutable);
        Assert.Equal(expected.ScratchNoexec, authority.MountPolicy.ScratchNoexec);
        Assert.Equal((byte)expected.NetworkQualification, (byte)authority.NetworkQualification);

        var resources = view.Resources;
        foreach (var name in QualificationWireTemplate.BudgetNames)
        {
            var budget = expected.Budget(name);
            var actual = ViewBudget(resources, name);
            Assert.Equal((byte)budget.Kind, (byte)actual.Kind);
            Assert.Equal(budget.Value, actual.Value);
            Assert.Equal(budget.Unit, actual.Unit);
            Assert.Equal((byte)budget.Qualification, (byte)actual.Qualification);
        }

        Assert.Equal((byte)expected.InodeQualification, (byte)resources.InodeQualification);
        Assert.Equal(expected.CoreDisabled, resources.CoreDumps.CoreDisabled);
        Assert.Equal(expected.RuntimeDumpsDisabled, resources.CoreDumps.RuntimeDumpsDisabled);
        Assert.Equal(expected.CorePatternObserved, resources.CoreDumps.CorePatternObserved);

        var owner = resources.OwnerAllocation;
        Assert.Equal(expected.OwnerStagingBytes, owner.StagingBytes);
        Assert.Equal(expected.OwnerStagingObjects, owner.StagingObjects);
        Assert.Equal(expected.OwnerConfigBytes, owner.ConfigBytes);
        Assert.Equal(expected.OwnerCoordinatorStatusBytes, owner.CoordinatorStatusBytes);
        Assert.Equal(expected.OwnerHelperStatusBytes, owner.HelperStatusBytes);
        Assert.Equal(expected.OwnerJournalBytes, owner.JournalBytes);
        Assert.Equal(expected.OwnerReceiptBytes, owner.ReceiptBytes);

        Assert.Equal(expected.Objects.Length, view.Closure.Objects.Length);
        for (var i = 0; i < expected.Objects.Length; i++)
        {
            Assert.Equal(expected.Objects[i].Role, view.Closure.Objects[i].Role);
            Assert.Equal(expected.Objects[i].Sha256, view.Closure.Objects[i].Sha256.ToArray());
            Assert.Equal(expected.Objects[i].Length, view.Closure.Objects[i].Length);
            Assert.Equal(expected.Objects[i].Destination, view.Closure.Objects[i].Destination);
            Assert.Equal(expected.Objects[i].BuildIdentity, view.Closure.Objects[i].BuildIdentity);
        }

        Assert.Equal(expected.HelperDependencies, view.Closure.HelperDependencies.ToArray());

        var invocation = view.Invocation;
        Assert.Equal(expected.ToolPath, invocation.ToolPath);
        Assert.Equal(expected.Mode, invocation.Mode);
        Assert.Equal(expected.InputPath, invocation.InputPath);
        Assert.Equal(expected.Argv, invocation.Argv.ToArray());
        Assert.Equal(
            expected.Environment.Select(p => new KeyValuePair<string, string>(p.Key, p.Value)).ToArray(),
            invocation.Environment.ToArray());
        Assert.Equal(expected.WorkDirectory, invocation.WorkDirectory);

        var channels = view.Channels;
        Assert.Equal(expected.StdinEof, channels.StdinEof);
        Assert.Equal(expected.StdoutLimitBytes, channels.StdoutLimitBytes);
        Assert.Equal(expected.StderrLimitBytes, channels.StderrLimitBytes);
        Assert.Equal(expected.AggregateLimitBytes, channels.AggregateLimitBytes);
    }

    private static ResourceBudgetView ViewBudget(ResourceManifestView resources, string name) => name switch
    {
        "Memory" => resources.Memory,
        "Swap" => resources.Swap,
        "Tasks" => resources.Tasks,
        "FileDescriptors" => resources.FileDescriptors,
        "TmpBytes" => resources.TmpBytes,
        "ShmBytes" => resources.ShmBytes,
        "StdoutBytes" => resources.StdoutBytes,
        "StderrBytes" => resources.StderrBytes,
        "CaptureAggregateBytes" => resources.CaptureAggregateBytes,
        "SetupTimeoutNs" => resources.SetupTimeoutNs,
        "OverallTimeoutNs" => resources.OverallTimeoutNs,
        "StopAllowanceNs" => resources.StopAllowanceNs,
        "FinalDrainNs" => resources.FinalDrainNs,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown budget name."),
    };
}
