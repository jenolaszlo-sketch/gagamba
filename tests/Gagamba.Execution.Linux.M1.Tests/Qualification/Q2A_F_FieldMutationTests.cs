// RC2A-02A Q2A-F matrix execution and completeness (test-only). Every generated
// case is a separately reported qualification row; the matrix self-tests prove
// all 72 committed fields are present in order, every applicable mutation class
// has at least one case, no inapplicable class is fabricated, and composite
// subfield inventories match the shipped public view types.
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

public sealed class Q2A_F_FieldMutationTests
{
    internal static readonly string[] FieldMapIds =
    {
        "RestrictedManifest.SchemaVersion",
        "RestrictedManifest.ImplementationId",
        "RestrictedManifest.Authority",
        "RestrictedManifest.Resources",
        "RestrictedManifest.Closure",
        "RestrictedManifest.Invocation",
        "RestrictedManifest.Channels",
        "AuthorityManifest.Namespaces",
        "AuthorityManifest.Capabilities",
        "AuthorityManifest.NoNewPrivs",
        "AuthorityManifest.FilterSha256",
        "AuthorityManifest.FilterLength",
        "AuthorityManifest.ReadOnlyDestinations",
        "AuthorityManifest.WritableStores",
        "AuthorityManifest.NoAmbientEnvironment",
        "AuthorityManifest.NoIpcEndpoints",
        "AuthorityManifest.NoExtraInheritedFds",
        "AuthorityManifest.MountPolicy",
        "AuthorityManifest.NetworkQualification",
        "ResourceManifest.Memory",
        "ResourceManifest.Swap",
        "ResourceManifest.Tasks",
        "ResourceManifest.FileDescriptors",
        "ResourceManifest.TmpBytes",
        "ResourceManifest.ShmBytes",
        "ResourceManifest.StdoutBytes",
        "ResourceManifest.StderrBytes",
        "ResourceManifest.CaptureAggregateBytes",
        "ResourceManifest.SetupTimeoutNs",
        "ResourceManifest.OverallTimeoutNs",
        "ResourceManifest.StopAllowanceNs",
        "ResourceManifest.FinalDrainNs",
        "ResourceManifest.InodeQualification",
        "ResourceManifest.CoreDumps",
        "ResourceManifest.OwnerAllocation",
        "ResourceBudget.Kind",
        "ResourceBudget.Value",
        "ResourceBudget.Unit",
        "ResourceBudget.Qualification",
        "WritableStore.Path",
        "WritableStore.Bytes",
        "WritableStore.HasInodeBudget",
        "WritableStore.InodeBudget",
        "ExecutableMountPolicy.ToolExecutable",
        "ExecutableMountPolicy.ScratchNoexec",
        "CoreDumpPolicy.CoreDisabled",
        "CoreDumpPolicy.RuntimeDumpsDisabled",
        "CoreDumpPolicy.CorePatternObserved",
        "OwnerAllocationLimits.StagingBytes",
        "OwnerAllocationLimits.StagingObjects",
        "OwnerAllocationLimits.ConfigBytes",
        "OwnerAllocationLimits.CoordinatorStatusBytes",
        "OwnerAllocationLimits.HelperStatusBytes",
        "OwnerAllocationLimits.JournalBytes",
        "OwnerAllocationLimits.ReceiptBytes",
        "ClosureIdentity.Objects",
        "ClosureIdentity.HelperDependencies",
        "ClosureObject.Role",
        "ClosureObject.Sha256",
        "ClosureObject.Length",
        "ClosureObject.Destination",
        "ClosureObject.BuildIdentity",
        "InvocationManifest.ToolPath",
        "InvocationManifest.Mode",
        "InvocationManifest.InputPath",
        "InvocationManifest.Argv",
        "InvocationManifest.Environment",
        "InvocationManifest.WorkDirectory",
        "ChannelManifest.StdinEof",
        "ChannelManifest.StdoutLimitBytes",
        "ChannelManifest.StderrLimitBytes",
        "ChannelManifest.AggregateLimitBytes",
    };

    public static IEnumerable<object[]> FieldCaseIds() =>
        QualificationFieldMatrix.Cases.Select(item => new object[] { item.CaseId });

    [Theory]
    [MemberData(nameof(FieldCaseIds))]
    public void Q2A_F_FieldMutationCase(string caseId)
    {
        var item = QualificationFieldMatrix.ByCaseId[caseId];
        item.Execute();
    }

    [Fact]
    public void Q2A_F_FieldInventoryMatchesCommittedSeventyTwoFieldMap()
    {
        Assert.Equal(72, FieldMapIds.Length);
        Assert.Equal(FieldMapIds, QualificationFieldMatrix.Fields.Select(field => field.FieldId).ToArray());
        Assert.Equal(72, QualificationFieldMatrix.Fields.Select(field => field.FieldId).Distinct(StringComparer.Ordinal).Count());
        for (var i = 0; i < QualificationFieldMatrix.Fields.Length; i++)
        {
            Assert.Equal(i + 1, QualificationFieldMatrix.Fields[i].Index);
        }
    }

    [Fact]
    public void Q2A_F_MatrixAssignsEveryApplicableClassAndRejectsInapplicableOnes()
    {
        foreach (var field in QualificationFieldMatrix.Fields)
        {
            var applicable = QualificationFieldMatrix.ApplicableClasses(field.Kind);
            foreach (var mutationClass in QualificationFieldMatrix.MutationClasses)
            {
                var hasCase = QualificationFieldMatrix.Cases.Any(item =>
                    string.Equals(item.FieldId, field.FieldId, StringComparison.Ordinal) &&
                    string.Equals(item.MutationClass, mutationClass, StringComparison.Ordinal));
                if (QualificationFieldMatrix.IsApplicable(field.Kind, mutationClass))
                {
                    Assert.True(hasCase, $"{field.FieldId} ({field.Kind}) lacks an applicable '{mutationClass}' case.");
                }
                else
                {
                    Assert.False(hasCase, $"{field.FieldId} ({field.Kind}) has a fabricated '{mutationClass}' case.");
                    Assert.False(string.IsNullOrWhiteSpace(
                        QualificationFieldMatrix.NotApplicableReason(field.Kind, mutationClass)));
                }
            }

            Assert.Contains(applicable, mutationClass =>
                QualificationFieldMatrix.Cases.Any(item => item.FieldId == field.FieldId && item.MutationClass == mutationClass));
        }
    }

    [Fact]
    public void Q2A_F_CaseIdentitiesAreUniqueStableAndClassified()
    {
        var cases = QualificationFieldMatrix.Cases;
        Assert.True(cases.Length > 200);
        Assert.Equal(cases.Length, cases.Select(item => item.CaseId).Distinct(StringComparer.Ordinal).Count());
        var fieldIds = QualificationFieldMatrix.Fields.Select(field => field.FieldId).ToHashSet(StringComparer.Ordinal);
        foreach (var item in cases)
        {
            Assert.Contains(item.FieldId, fieldIds);
            Assert.Contains(item.MutationClass, QualificationFieldMatrix.MutationClasses);
            Assert.Contains(item.CaseClass, new[] { "valid", "malformed", "noncanonical", "misuse", "trace" });
            Assert.False(string.IsNullOrWhiteSpace(item.Description));
            var number = int.Parse(item.CaseId.Split('-')[2], System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(
                QualificationFieldMatrix.Fields.Single(field => field.FieldId == item.FieldId).Index,
                number);
        }
    }

    [Fact]
    public void Q2A_F_CompositeSubfieldsMatchTheCommittedFieldMapAndShippedViews()
    {
        foreach (var field in QualificationFieldMatrix.Fields.Where(field => field.Kind == "composite"))
        {
            Assert.NotNull(field.ViewType);
            Assert.NotNull(field.Subfields);
            var viewProperties = field.ViewType!.GetProperties().Select(property => property.Name).ToArray();
            Assert.Equal(field.Subfields, viewProperties);
            foreach (var subfield in field.Subfields!)
            {
                Assert.Contains(
                    QualificationFieldMatrix.Fields,
                    candidate => !string.Equals(candidate.FieldId, field.FieldId, StringComparison.Ordinal) &&
                        candidate.FieldId.EndsWith("." + subfield, StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public void Q2A_F_EveryFieldHasAtLeastOneExecutedCase()
    {
        foreach (var field in QualificationFieldMatrix.Fields)
        {
            Assert.Contains(QualificationFieldMatrix.Cases, item => item.FieldId == field.FieldId);
        }

        var counts = QualificationFieldMatrix.Cases
            .GroupBy(item => item.FieldId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        Assert.Equal(72, counts.Count);
    }
}
