using Gagamba.Execution.Linux.M1.Bootstrap;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Bootstrap.ValidationCustodyModelVectors;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

public sealed class ValidationCustodyModelTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void R02_R05_IndependentOperandsPreserveOriginals(int topology)
    {
        var original = new Inputs(topology);
        var suspect = new Inputs(topology);
        Assert.Equal(ValidationCustodyRefusal.None,
            ValidationCustodyModel.TryCreate(Expected(original), Observed(inputs: suspect), 0, false, out var model));
        Assert.NotNull(model);
        using (model)
        {
            for (int step = 1; step <= 5; step++)
            {
                var snapshot = Accept(model, step, step, Observed(step, new Inputs(topology)));
                Assert.Equal("supplied-structural-model", ValidationCustodyModelSnapshot.EvidenceKind);
                var binding = snapshot.Binding;
                Assert.Equal((ValidationPreTopology)topology, binding.Pre.Topology);
                Assert.Equal(original.Ids, binding.Ids);
                Assert.Equal(original.Host, binding.Host);
                Assert.Equal(original.Boot, binding.Boot);
                Assert.Equal(original.Selection, binding.Selection);
                Assert.Equal(original.Launcher, binding.LauncherOriginal);
                Assert.Equal(original.Controller, binding.ControllerOriginal);
                Assert.Equal(original.CaseId, binding.CaseId);
                Assert.Equal(original.Admit, binding.AdmitBeforeNs);
                Assert.Equal(original.End, binding.SessionEndNs);
                Assert.Null(binding.QualificationId);
                Assert.Null(binding.QualificationRecordSha256);
                var actual = Buffers(binding);
                for (int i = 0; i < actual.Length; i++)
                    Assert.Equal(original.Digests[i], actual[i]);
            }
        }
    }

    [Fact]
    public void R02_MissingOperandAndBindingRefuse()
    {
        CreateRefuses("R02 expected", ValidationCustodyRefusal.Context, null, Observed());
        CreateRefuses("R02 observed", ValidationCustodyRefusal.Context, Expected(), null);
        CreateRefuses("R02 expected binding", ValidationCustodyRefusal.Context, new(null!), Observed());
        CreateRefuses("R02 observed binding", ValidationCustodyRefusal.Context,
            Expected(), Evidence(null!, StageFacts(0)));
    }

    [Fact]
    public void R02_R05_MalformedScalarsRefuseOnEitherSide()
    {
        var nominal = new Inputs();
        var invalid = new Inputs[]
        {
            nominal with { Version = 0 }, nominal with { Version = 2 }, nominal with { Version = 4 },
            new(0), new(3), new(255),
            nominal with { Host = Guid.Empty }, nominal with { Boot = Guid.Empty },
            nominal with { Selection = Guid.Empty }, nominal with { Launcher = Guid.Empty },
            nominal with { Controller = Guid.Empty }, nominal with { Launcher = nominal.Controller },
            nominal with { Ids = nominal.Ids with { Session = Guid.Empty } },
            nominal with { Ids = nominal.Ids with { Execution = Guid.Empty } },
            nominal with { Ids = nominal.Ids with { Lease = Guid.Empty } },
            nominal with { QualificationId = "" }, nominal with { QualificationDigest = "x" },
            nominal with { Admit = 0 }, nominal with { Admit = -1 }, nominal with { End = 39 },
            nominal with { CaseId = null! }, nominal with { CaseId = "" }, nominal with { CaseId = "." },
            nominal with { CaseId = ".." }, nominal with { CaseId = ".case" }, nominal with { CaseId = "case." },
            nominal with { CaseId = "é" }, nominal with { CaseId = "case name" },
            nominal with { CaseId = "case/name" }, nominal with { CaseId = "case\0id" },
            nominal with { CaseId = new string('a', 129) },
        };
        for (int i = 0; i < invalid.Length; i++)
        {
            CreateRefuses($"R02 expected shape {i}", ValidationCustodyRefusal.Context, Expected(invalid[i]), Observed());
            CreateRefuses($"R02 observed shape {i}", ValidationCustodyRefusal.Context, Expected(), Observed(inputs: invalid[i]));
        }
    }

    [Theory]
    [InlineData("a")]
    [InlineData("0")]
    [InlineData("_")]
    [InlineData("-")]
    [InlineData("A.0_-")]
    public void R02_ValidCaseGrammarAndEqualDeadlines(string asciiCase)
    {
        using var model = Create(new Inputs { CaseId = asciiCase, Admit = 1, End = 1 });
        using var longest = Create(new Inputs { CaseId = new string('z', 128) });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public void R02_R03_R04_EveryDigestIsIndependentlyBound(int digest)
    {
        var nominal = new Inputs();
        foreach (int length in new[] { -1, 0, 31, 33, 4096 })
        {
            var invalid = new Inputs();
            invalid.Digests[digest] = length < 0 ? null : new byte[length];
            CreateRefuses($"R02 expected digest {digest}, length {length}", ValidationCustodyRefusal.Context,
                Expected(invalid), Observed());
            CreateRefuses($"R02 observed digest {digest}, length {length}", ValidationCustodyRefusal.Context,
                Expected(), Observed(inputs: invalid));
        }

        var changed = new Inputs();
        changed.Digests[digest]![31] ^= 0xff;
        CreateRefuses($"R03/R04 changed digest {digest}", ValidationCustodyRefusal.Comparison,
            Expected(nominal), Observed(inputs: changed));
        using var model = Create(nominal);
        AdvanceRefuses($"R03/R04 retained digest {digest}", model, ValidationCustodyEvent.ObserveIssue,
            Observed(1, changed), 1, ValidationCustodyRefusal.Comparison);
    }

    [Fact]
    public void R03_R04_AgreeingSuspectPairCannotSupplyExpectation()
    {
        var suspect = new Inputs();
        for (int i = 0; i < suspect.Digests.Length; i++)
            suspect.Digests[i]![0] ^= 0x80;

        using var pairOnly = Create(suspect);
        CreateRefuses("R03/R04 independent expectation", ValidationCustodyRefusal.Comparison, Expected(),
            Observed(inputs: suspect));
    }

    [Fact]
    public void R05_R09_EveryScalarAssociationUsesOriginalExpectation()
    {
        var original = new Inputs();
        var mismatches = new Inputs[]
        {
            new(2),
            original with { Host = Label(40) }, original with { Boot = Label(41) },
            original with { Selection = Label(42) }, original with { Launcher = Label(43) },
            original with { Controller = Label(44) },
            original with { Launcher = original.Controller, Controller = original.Launcher },
            original with { Launcher = Label(45), Controller = Label(46) },
            original with { Ids = original.Ids with { Session = Label(47) } },
            original with { Ids = original.Ids with { Execution = Label(48) } },
            original with { Ids = original.Ids with { Lease = Label(49) } },
            original with { CaseId = original.CaseId.ToUpperInvariant() },
            original with { Admit = 41 }, original with { End = 101 }
        };
        for (int i = 0; i < mismatches.Length; i++)
        {
            CreateRefuses($"R05/R09 scalar {i}", ValidationCustodyRefusal.Comparison, Expected(),
                Observed(inputs: mismatches[i]));
            using var model = Create();
            AdvanceRefuses($"R05/R09 original scalar {i}", model, ValidationCustodyEvent.ObserveIssue,
                Observed(1, mismatches[i]), 1, ValidationCustodyRefusal.Comparison);
        }

        // The allowed labels need only satisfy their individual shapes and L/C separation.
        using var sharedLabel = Create(original with { Boot = original.Host });
    }

    [Fact]
    public void R06_EveryFactIsCheckedAtEveryStage()
    {
        for (int step = 0; step <= 5; step++)
        {
            for (int fact = 0; fact < 11; fact++)
            {
                foreach (byte replacement in new byte[] { 0, 1, 2, 255 })
                {
                    var premise = StageFacts(step);
                    if (premise[fact] == (ValidationCustodyFact)replacement)
                        continue;

                    premise[fact] = (ValidationCustodyFact)replacement;
                    var refusal = replacement == 255 ? ValidationCustodyRefusal.Context : ValidationCustodyRefusal.Evidence;
                    var evidence = Evidence(new Inputs().Binding(), premise);
                    string control = $"R06 step {step}, fact {fact}, value {replacement}";
                    if (step == 0)
                    {
                        CreateRefuses(control, refusal, Expected(), evidence);
                        continue;
                    }

                    using var model = Create();
                    for (int prior = 1; prior < step; prior++)
                        Accept(model, prior, prior);
                    AdvanceRefuses(control, model, Sequence[step - 1].Event, evidence, step, refusal,
                        retryStep: step, retryAt: step);
                }
            }
        }
    }
}
