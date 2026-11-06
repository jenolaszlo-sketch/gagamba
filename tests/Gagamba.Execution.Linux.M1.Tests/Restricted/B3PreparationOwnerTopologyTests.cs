using System.Reflection;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Restricted;
using Gagamba.Execution.Restricted.Control;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Restricted;

public sealed class B3PreparationOwnerTopologyTests
{
    private static B3FixtureProfile Profile(string journalDirectory, bool qualified = false,
        B3IntegratedProfile? integrated = null) => new(
        OwnerDllPath: "owner.dll",
        OwnerDllSha256: new string('0', 64),
        DelegatedParentPath: "parent",
        SourceRootPath: "source",
        StagingParentPath: "staging",
        JournalDirectory: journalDirectory,
        ToolSourceName: "tool",
        InputSourceName: "input",
        CoordinatorSourceName: "coordinator",
        FilterSourceName: "filter",
        ToolSha256: new string('0', 64),
        InputSha256: new string('0', 64),
        CoordinatorSha256: new string('0', 64),
        ToolEnrollmentId: "tool-enrollment",
        InputEnrollmentId: "input-enrollment",
        TotalBudgetBytes: 1,
        MaxObjects: 4,
        ExpiresLeadNanoseconds: 1,
        IntegratedRecovery: integrated)
        { QualifiedInstallation = qualified };

    [Fact]
    public void QualifiedNonIntegratedConstructorRefusesBeforeJournalDirectoryCreation()
    {
        var journalDirectory = Path.Combine(Path.GetTempPath(), "gagamba-f02-" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.Throws<InvalidOperationException>(() => new B3PreparationOwner(
                Profile(journalDirectory, qualified: true), new RestrictedProviderDescriptor()));
            Assert.False(Directory.Exists(journalDirectory));
        }
        finally
        {
            if (Directory.Exists(journalDirectory)) Directory.Delete(journalDirectory);
        }
    }

    [Fact]
    public void LegacyNonIntegratedConstructorStillCreatesJournalDirectory()
    {
        var journalDirectory = Path.Combine(Path.GetTempPath(), "gagamba-f02-" + Guid.NewGuid().ToString("N"));
        try
        {
            var owner = new B3PreparationOwner(Profile(journalDirectory), new RestrictedProviderDescriptor());
            Assert.True(Directory.Exists(journalDirectory));
            Assert.NotNull(typeof(B3PreparationOwner).GetField("_scope", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(owner));
            Assert.Null(typeof(B3PreparationOwner).GetField("_integrated", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(owner));
        }
        finally
        {
            if (Directory.Exists(journalDirectory)) Directory.Delete(journalDirectory);
        }
    }

    [Fact]
    public void IntegratedConstructorRemainsOnIntegratedClassification()
    {
        var journalDirectory = Path.Combine(Path.GetTempPath(), "gagamba-f02-" + Guid.NewGuid().ToString("N"));
        var integrated = new B3IntegratedProfile(
            new PinnedExecutable("recovery", new()),
            new PinnedExecutable("owner", new()),
            new ExperimentalRuntimePin("host", "0", "runtime", "0", "hostfxr", "0", "version"),
            "evidence");
        try
        {
            var owner = new B3PreparationOwner(Profile(journalDirectory, qualified: true, integrated: integrated),
                new RestrictedProviderDescriptor());
            Assert.True(Directory.Exists(journalDirectory));
            Assert.Null(typeof(B3PreparationOwner).GetField("_scope", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(owner));
            Assert.NotNull(typeof(B3PreparationOwner).GetField("_integrated", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(owner));
        }
        finally
        {
            if (Directory.Exists(journalDirectory)) Directory.Delete(journalDirectory);
        }
    }
}
