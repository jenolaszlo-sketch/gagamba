using Gagamba.Execution.Linux.M1.Bootstrap;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

internal static class ValidationCustodyModelVectors
{
    internal static readonly (ValidationCustodyEvent Event, ValidationCustodyState State)[] Sequence =
    [
        (ValidationCustodyEvent.ObserveIssue, ValidationCustodyState.IssuedModel),
        (ValidationCustodyEvent.ObserveOffer, ValidationCustodyState.OfferedModel),
        (ValidationCustodyEvent.ObserveDrop, ValidationCustodyState.DroppedModel),
        (ValidationCustodyEvent.ObserveConsume, ValidationCustodyState.ConsumedModel),
        (ValidationCustodyEvent.Complete, ValidationCustodyState.Closed)
    ];

    internal static Guid Label(int value) => new(value, 0x3456, 0x1234, 1, 2, 3, 4, 5, 6, 7, 8);

    internal sealed record Inputs(int Topology = 1)
    {
        internal ushort Version { get; init; } = 3;
        internal Gava1Ids Ids { get; init; } = new(Label(8), Label(9), Label(10));
        internal Guid Host { get; init; } = Label(11);
        internal Guid Boot { get; init; } = Label(12);
        internal Guid Selection { get; init; } = Label(13);
        internal Guid Launcher { get; init; } = Label(14);
        internal Guid Controller { get; init; } = Label(15);
        internal string CaseId { get; init; } = "synthetic_case-1.alpha";
        internal long Admit { get; init; } = 40;
        internal long End { get; init; } = 100;
        internal string? QualificationId { get; init; }
        internal string? QualificationDigest { get; init; }
        internal byte[]?[] Digests { get; } = Enumerable.Range(1, 15)
            .Select(i => Enumerable.Repeat((byte)i, 32).ToArray()).ToArray();

        internal ValidationCustodyBinding Binding() => new(new(Version, (ValidationPreTopology)Topology),
            Ids, Host, Boot, Selection, Launcher, Controller, Digests[0], Digests[1], Digests[2], Digests[3],
            Digests[4], Digests[5], Digests[6], Digests[7], Digests[8], Digests[9], Digests[10],
            Digests[11], Digests[12], Digests[13], Digests[14], CaseId, Admit, End,
            QualificationId, QualificationDigest);
    }

    internal static byte[][] Buffers(ValidationCustodyBinding value) =>
    [
        value.LauncherExecutable,
        value.LauncherClosure,
        value.ControllerExecutable,
        value.ControllerClosure,
        value.DriverExecutable,
        value.DriverClosure,
        value.ArtifactSet,
        value.Catalog,
        value.Manifest,
        value.Policy,
        value.Script,
        value.Review,
        value.OwnerReceipt,
        value.Approval,
        value.Capability
    ];

    internal static ValidationCustodyExpectations Expected(Inputs? inputs = null) =>
        new((inputs ?? new Inputs()).Binding());

    internal static ValidationCustodyEvidence Observed(int step = 0, Inputs? inputs = null) =>
        Evidence((inputs ?? new Inputs()).Binding(), StageFacts(step));

    internal static ValidationCustodyFact[] StageFacts(int step)
    {
        var yes = ValidationCustodyFact.Consistent;
        var unknown = ValidationCustodyFact.Unknown;
        var milestones = step switch
        {
            0 => new[] { unknown, unknown, unknown, unknown, unknown, unknown },
            1 => new[] { yes, yes, unknown, unknown, unknown, unknown },
            2 => new[] { yes, yes, yes, unknown, unknown, unknown },
            3 => new[] { yes, yes, yes, yes, unknown, unknown },
            4 => new[] { yes, yes, yes, yes, yes, unknown },
            5 => new[] { yes, yes, yes, yes, yes, yes },
            _ => throw new ArgumentOutOfRangeException(nameof(step))
        };
        return [yes, yes, yes, yes, yes, .. milestones];
    }

    internal static ValidationCustodyEvidence Evidence(ValidationCustodyBinding binding, ValidationCustodyFact[] facts) =>
        new(binding, facts[0], facts[1], facts[2], facts[3], facts[4], facts[5],
            facts[6], facts[7], facts[8], facts[9], facts[10]);

    internal static ValidationCustodyModel Create(Inputs? inputs = null, long now = 0)
    {
        var result = ValidationCustodyModel.TryCreate(Expected(inputs), Observed(inputs: inputs), now, false, out var model);
        Assert.Equal(ValidationCustodyRefusal.None, result);
        return Assert.IsType<ValidationCustodyModel>(model);
    }

    internal static ValidationCustodyModelSnapshot Accept(ValidationCustodyModel model, int step,
        long now, ValidationCustodyEvidence? evidence = null)
    {
        var transition = Sequence[step - 1];
        var result = model.Advance(transition.Event, evidence ?? Observed(step), now, false, out var snapshot);
        Assert.Equal(ValidationCustodyRefusal.None, result);
        Assert.NotNull(snapshot);
        Assert.Equal(transition.State, snapshot.State);
        return snapshot;
    }

    internal static void CreateRefuses(string control, ValidationCustodyRefusal expected,
        ValidationCustodyExpectations? expectations, ValidationCustodyEvidence? evidence, long now = 0, bool stop = false)
    {
        var actual = ValidationCustodyModel.TryCreate(expectations, evidence, now, stop, out var model);
        Assert.True(expected == actual, $"{control}: expected {expected}, observed {actual}");
        Assert.Null(model);
    }

    internal static void AdvanceRefuses(string control, ValidationCustodyModel model, ValidationCustodyEvent next,
        ValidationCustodyEvidence? evidence, long now, ValidationCustodyRefusal expected, bool stop = false,
        int retryStep = 1, long retryAt = 1, Inputs? original = null)
    {
        var actual = model.Advance(next, evidence, now, stop, out var snapshot);
        Assert.True(expected == actual, $"{control}: expected {expected}, observed {actual}");
        Assert.Null(snapshot);
        var later = model.Advance(Sequence[retryStep - 1].Event, Observed(retryStep, original), retryAt, false, out var retry);
        Assert.True(later == ValidationCustodyRefusal.Transition, $"{control}: retained state allowed retry ({later})");
        Assert.Null(retry);
    }
}
