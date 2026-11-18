using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Gagamba.Execution.Linux.M1.Validation;
using Xunit;
using static Gagamba.Execution.Linux.M1.Validation.ValidationReleaseOpcode;
using static Gagamba.Execution.Linux.M1.Validation.ValidationReleasePreparationStatus;

namespace Gagamba.Execution.Linux.M1.Tests.Validation;

public sealed class ValidationReleasePreparationTests
{
    private const string R01Digest = "3737572b7b3d2ad8313d109d6d8a9d0df4b5bc7b20e43c71a0a6061e1988ce3e";
    private const string R02Digest = "3637572b7b3d2ad8313d109d6d8a9d0df4b5bc7b20e43c71a0a6061e1988ce3e";
    private const string MixedDigest = "d4573bb166ab7a4f73743297a4b3b2b9f2e19c73ef4fb6c36c94cf2a20fd7d90";
    private const string R01Preimage =
        "676167616d62612d76616c69646174696f6e2d72656c656173652d763100" +
        "1111111111111111111111111111111111111111111111111111111111111111" +
        "2222222222222222222222222222222222222222222222222222222222222222" +
        "3333333333333333333333333333333333333333333333333333333333333333" +
        "4444444444444444444444444444444444444444444444444444444444444444" +
        "02000000" +
        "5555555555555555555555555555555566666666666666666666666666666666" +
        "7777777777777777777777777777777788888888888888888888888888888888" +
        "9999999999999999999999999999999999999999999999999999999999999999";
    private const string MixedPreimage =
        "676167616d62612d76616c69646174696f6e2d72656c656173652d763100" +
        "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f" +
        "202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f" +
        "404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f" +
        "606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f" +
        "02000000" +
        "33221100554477668899aabbccddeeff78563412ab90efcd0123456789abcdef" +
        "efcdab892301674589abcdef0123456798badcfe54761032fedcba9876543210" +
        "808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f";

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void NormativeVectors(bool mixed, bool post)
    {
        var phase = post ? ValidationReleasePhase.Post : ValidationReleasePhase.Pre;
        var candidate = new ProjectionData(mixed) { Phase = phase }.Create();
        var original = new ProjectionData(mixed) { Phase = phase }.Create();
        var preimage = mixed ? MixedPreimage : R01Preimage;
        var digest = mixed ? MixedDigest : R01Digest;
        Assert.NotSame(candidate.Steps, original.Steps);
        for (int slot = 0; slot < 5; slot++)
            Assert.NotSame(Hashes(candidate)[slot], Hashes(original)[slot]);

        Assert.Equal(StructuralMatch, ValidationReleasePreparation.Prepare(candidate, original, out var prepared));
        AssertMaterial(prepared, preimage, digest);
        Assert.Equal(StructuralMatch,
            ValidationReleasePreparation.Compare(candidate, original, Hex(digest), out var compared));
        AssertMaterial(compared, preimage, digest);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NullProjection(bool candidateSide)
    {
        var present = new ProjectionData().Create();
        AssertRefusal(candidateSide ? null : present, candidateSide ? present : null, MissingInput);
    }

    public static TheoryData<string, bool> MissingMembers
    {
        get
        {
            TheoryData<string, bool> cases = new();
            foreach (string member in ProjectionMembers())
            {
                cases.Add(member, false);
                cases.Add(member, true);
            }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(MissingMembers))]
    public void MissingMember(string member, bool candidateSide)
    {
        var malformed = new ProjectionData().Create(missing: member);
        AssertOneSided(malformed, candidateSide, MissingInput);
    }

    public static TheoryData<int, bool, int> HashWidths
    {
        get
        {
            TheoryData<int, bool, int> cases = new();
            for (int slot = 0; slot < 5; slot++)
            {
                foreach (var length in new[] { 0, 1, 31, 32, 33, 4096 })
                {
                    cases.Add(slot, false, length);
                    cases.Add(slot, true, length);
                }
            }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(HashWidths))]
    public void ProjectionHashWidths(int slot, bool candidateSide, int width)
    {
        var data = new ProjectionData();
        data.Digests[slot] = Repeat((byte)(0x11 * (slot + (slot == 4 ? 5 : 1))), width);
        if (width == 32)
        {
            var peer = new ProjectionData().Create();
            var sized = data.Create();
            Assert.Equal(StructuralMatch, ValidationReleasePreparation.Prepare(
                candidateSide ? sized : peer, candidateSide ? peer : sized, out var material));
            AssertMaterial(material, R01Preimage, R01Digest);
        }
        else
        {
            AssertOneSided(data.Create(), candidateSide, InvalidLength);
        }
    }

    public static TheoryData<int, bool> HashSlots => new()
    {
        { 0, false }, { 0, true }, { 1, false }, { 1, true }, { 2, false }, { 2, true },
        { 3, false }, { 3, true }, { 4, false }, { 4, true },
    };

    [Theory]
    [MemberData(nameof(HashSlots))]
    public void ExactHashAssociation(int slot, bool candidateSide)
    {
        var data = new ProjectionData();
        data.Digests[slot]![31] ^= 1;
        AssertOneSided(data.Create(), candidateSide, ProjectionMismatch);
    }

    public static TheoryData<int, bool> GuidRoles => new()
    {
        { 0, false }, { 0, true }, { 1, false }, { 1, true },
        { 2, false }, { 2, true }, { 3, false }, { 3, true },
    };

    [Theory]
    [MemberData(nameof(GuidRoles))]
    public void ExactGuidAssociation(int role, bool candidateSide)
    {
        var data = new ProjectionData();
        data.Ids[role] = new Guid("11121314-2122-3132-4142-515253545556");
        AssertOneSided(data.Create(), candidateSide, ProjectionMismatch);
    }

    [Theory]
    [MemberData(nameof(GuidRoles))]
    public void EmptyGuidRefuses(int role, bool candidateSide)
    {
        var data = new ProjectionData();
        data.Ids[role] = Guid.Empty;
        AssertOneSided(data.Create(), candidateSide, InvalidScalar);
    }

    [Fact]
    public void SwappedRolesRefuse()
    {
        var candidate = new ProjectionData();
        var original = new ProjectionData();
        (original.Ids[2], original.Ids[3]) = (original.Ids[3], original.Ids[2]);
        AssertRefusal(candidate.Create(), original.Create(), ProjectionMismatch);
    }

    [Theory]
    [InlineData(0U)]
    [InlineData(2U)]
    [InlineData(uint.MaxValue)]
    public void UnsupportedSchema(uint version)
    {
        var wrong = new ProjectionData { Version = version }.Create();
        AssertOneSided(wrong, true, UnsupportedVersion);
        AssertOneSided(wrong, false, UnsupportedVersion);
        AssertRefusal(wrong, wrong, UnsupportedVersion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(255)]
    public void UndefinedPhaseOrTopology(byte value)
    {
        AssertOneSided(new ProjectionData { Phase = (ValidationReleasePhase)value }.Create(), true, InvalidScalar);
        AssertOneSided(new ProjectionData { Topology = (ValidationReleaseTopology)value }.Create(), false, InvalidScalar);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PhaseAssociation(bool candidateSide) =>
        AssertOneSided(new ProjectionData { Phase = ValidationReleasePhase.Post }.Create(),
            candidateSide, ProjectionMismatch);

    [Theory]
    [InlineData("r01")]
    [InlineData("another.case")]
    public void OrdinalCaseAssociation(string otherCase)
    {
        var wrong = new ProjectionData { CaseId = otherCase }.Create();
        AssertOneSided(wrong, true, CaseMismatch);
        AssertOneSided(wrong, false, CaseMismatch);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData(".a")]
    [InlineData("a.")]
    [InlineData(" a")]
    [InlineData("a ")]
    [InlineData("a\tb")]
    [InlineData("a\0b")]
    [InlineData("a/b")]
    [InlineData("é")]
    [InlineData("😀")]
    public void InvalidCaseGrammar(string invalid)
    {
        AssertOneSided(new ProjectionData { CaseId = invalid }.Create(), true, InvalidCaseId);
        AssertOneSided(new ProjectionData { CaseId = invalid }.Create(), false, InvalidCaseId);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void CaseLengthBoundaries(int length, bool valid)
    {
        var name = new string('a', length);
        var candidate = new ProjectionData { CaseId = name }.Create();
        var expected = new ProjectionData { CaseId = name }.Create();
        if (!valid)
        {
            AssertRefusal(candidate, expected, InvalidCaseId);
            return;
        }

        Assert.Equal(StructuralMatch,
            ValidationReleasePreparation.Compare(candidate, expected, Hex(R01Digest), out var material));
        AssertMaterial(material, R01Preimage, R01Digest);
    }

    [Theory]
    [InlineData(false, 0U)]
    [InlineData(false, 2U)]
    [InlineData(true, 2U)]
    [InlineData(true, uint.MaxValue)]
    public void CompleteNoReleaseTemplates(bool integrated, uint index)
    {
        var noRelease = new ProjectionData
        {
            Topology = integrated ? ValidationReleaseTopology.B3Integrated : ValidationReleaseTopology.B2Lifeline,
            Steps = NoReleaseSteps(integrated),
            CatalogAllows = false,
            FixtureAllows = false,
            RequestedIndex = index,
        }.Create();
        AssertOneSided(noRelease, true, NoReleaseTemplate);
        AssertOneSided(noRelease, false, NoReleaseTemplate);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void ReleaseFlagsAreRequired(bool catalog, bool fixture)
    {
        var wrong = new ProjectionData { CatalogAllows = catalog, FixtureAllows = fixture }.Create();
        AssertOneSided(wrong, true, InvalidTemplate);
        AssertOneSided(wrong, false, InvalidTemplate);
    }

    [Fact]
    public void NonReleaseProjectionConflicts()
    {
        var b2WithRelease = new ProjectionData { Topology = ValidationReleaseTopology.B2Lifeline }.Create();
        AssertOneSided(b2WithRelease, true, InvalidTemplate);
        AssertOneSided(b2WithRelease, false, InvalidTemplate);
        var b3WithB2Steps = new ProjectionData
        {
            CatalogAllows = false, FixtureAllows = false, Steps = NoReleaseSteps(integrated: false),
        }.Create();
        AssertOneSided(b3WithB2Steps, true, InvalidTemplate);
        var b2WithConflictingFlags = new ProjectionData
        {
            Topology = ValidationReleaseTopology.B2Lifeline, Steps = NoReleaseSteps(integrated: false),
            CatalogAllows = true, FixtureAllows = false,
        }.Create();
        AssertOneSided(b2WithConflictingFlags, true, InvalidTemplate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(4096)]
    public void InvalidStepWidth(int length)
    {
        var wrong = new ProjectionData { Steps = new ValidationReleaseStep[length] }.Create();
        AssertOneSided(wrong, true, InvalidLength);
        AssertOneSided(wrong, false, InvalidLength);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void EveryIndexedSlotIsValidated(int slot)
    {
        var sequence = new ProjectionData().Steps;
        var correct = sequence[slot];
        var alternatives = new[]
        {
            default,
            new ValidationReleaseStep(correct.Index, (ValidationReleaseOpcode)255),
            new ValidationReleaseStep(correct.Index, correct.Opcode == Cleanup ? Stop : Cleanup),
            new ValidationReleaseStep(correct.Index + 1, correct.Opcode),
            new ValidationReleaseStep(uint.MaxValue, correct.Opcode),
            sequence[(slot + 1) % sequence.Length],
        };
        foreach (var replacement in alternatives)
        {
            sequence[slot] = replacement;
            var wrong = new ProjectionData { Steps = sequence }.Create();
            AssertOneSided(wrong, true, InvalidTemplate);
            AssertOneSided(wrong, false, InvalidTemplate);
        }
        sequence[slot] = correct;
        int peer = (slot + 1) % sequence.Length;
        (sequence[slot], sequence[peer]) = (sequence[peer], sequence[slot]);
        AssertOneSided(new ProjectionData { Steps = sequence }.Create(), true, InvalidTemplate);
    }

    [Theory]
    [InlineData(0U)]
    [InlineData(1U)]
    [InlineData(3U)]
    [InlineData(uint.MaxValue)]
    public void ReleaseIndexAssociation(uint index)
    {
        var wrong = new ProjectionData { RequestedIndex = index }.Create();
        AssertOneSided(wrong, true, InvalidScalar);
        AssertOneSided(wrong, false, InvalidScalar);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(4096)]
    public void ExpectedDigestWidth(int length)
    {
        var candidate = new ProjectionData().Create();
        var original = new ProjectionData().Create();
        byte[] digest = new byte[length];
        Hex(R01Digest).AsSpan(0, Math.Min(32, length)).CopyTo(digest);
        if (length == 32)
        {
            Assert.Equal(StructuralMatch, ValidationReleasePreparation.Compare(candidate, original, digest, out var material));
            AssertMaterial(material, R01Preimage, R01Digest);
        }
        else
        {
            AssertCompareRefusal(candidate, original, digest, InvalidLength);
        }
    }

    [Fact]
    public void IndependentDigestRefusals()
    {
        var candidate = new ProjectionData().Create();
        var original = new ProjectionData().Create();
        AssertCompareRefusal(candidate, original, null, MissingExpectedDigest);
        AssertCompareRefusal(candidate, original, Hex(R02Digest), DigestMismatch);
        var lastByte = Hex(R01Digest);
        lastByte[^1] ^= 1;
        AssertCompareRefusal(candidate, original, lastByte, DigestMismatch);
    }

    [Fact]
    public void ZeroRawOperandsAndIndependentCommonModeDigest()
    {
        var candidate = new ProjectionData();
        var original = new ProjectionData();
        candidate.Digests[0] = new byte[32];
        original.Digests[0] = new byte[32];
        string preimage = R01Preimage[..60] + new string('0', 64) + R01Preimage[124..];
        const string zeroCapabilityDigest = "898bc1385c1d85ae80f6bf673b78ccc343d0c350ad0691a34a1429a799456d64";

        Assert.Equal(StructuralMatch,
            ValidationReleasePreparation.Prepare(candidate.Create(), original.Create(), out var prepared));
        AssertMaterial(prepared, preimage, zeroCapabilityDigest);
        AssertCompareRefusal(candidate.Create(), original.Create(), Hex(R01Digest), DigestMismatch);
        Assert.Equal(StructuralMatch, ValidationReleasePreparation.Compare(candidate.Create(), original.Create(),
            Hex(zeroCapabilityDigest), out var compared));
        AssertMaterial(compared, preimage, zeroCapabilityDigest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void IndependentDigestCatchesCommonModeChange(int slot)
    {
        var candidate = new ProjectionData();
        var original = new ProjectionData();
        candidate.Digests[slot]![0] ^= 1;
        original.Digests[slot]![0] ^= 1;
        Assert.Equal(StructuralMatch,
            ValidationReleasePreparation.Prepare(candidate.Create(), original.Create(), out var material));
        Assert.NotNull(material);
        AssertCompareRefusal(candidate.Create(), original.Create(), Hex(R01Digest), DigestMismatch);
    }

    [Fact]
    public void BetweenCallMutationsKeepBothPriorResults()
    {
        var candidate = new ProjectionData().Create();
        var original = new ProjectionData().Create();
        byte[] expectation = Hex(R01Digest);
        Assert.Equal(StructuralMatch, ValidationReleasePreparation.Prepare(candidate, original, out var prepared));
        Assert.Equal(StructuralMatch, ValidationReleasePreparation.Compare(candidate, original, expectation, out var compared));

        for (int slot = 0; slot < 5; slot++)
        {
            foreach (var input in new[] { candidate, original })
            {
                byte[] hash = Hashes(input)[slot]!;
                hash[0] ^= 1;
                AssertRefusal(candidate, original, ProjectionMismatch);
                hash[0] ^= 1;
            }
        }
        foreach (var input in new[] { candidate, original })
        {
            var steps = input.Steps!;
            var prior = steps[2];
            steps[2] = new ValidationReleaseStep(2, Stop);
            AssertRefusal(candidate, original, InvalidTemplate);
            steps[2] = prior;
        }
        expectation[31] ^= 1;
        AssertCompareRefusal(candidate, original, expectation, DigestMismatch);
        AssertMaterial(compared, R01Preimage, R01Digest);
        expectation[31] ^= 1;
        Assert.Equal(StructuralMatch, ValidationReleasePreparation.Compare(candidate, original, expectation, out var repeated));
        AssertMaterial(repeated, R01Preimage, R01Digest);
        Assert.NotNull(compared);
        Assert.False(repeated!.Preimage == compared.Preimage);
        Assert.False(repeated.CorrelationSha256 == compared.CorrelationSha256);

        // A later success with different data must not overwrite earlier results.
        Assert.Equal(StructuralMatch, ValidationReleasePreparation.Prepare(
            new ProjectionData(mixed: true).Create(), new ProjectionData(mixed: true).Create(), out var later));
        AssertMaterial(later, MixedPreimage, MixedDigest);
        AssertMaterial(prepared, R01Preimage, R01Digest);
        AssertMaterial(compared, R01Preimage, R01Digest);
    }

    [Fact]
    public void ConcurrentInvocationsUseStableInputs()
    {
        var candidate = new ProjectionData(mixed: true).Create();
        var original = new ProjectionData(mixed: true).Create();
        var expected = Hex(MixedDigest);
        Parallel.For(0, 32, new ParallelOptions { CancellationToken = TestContext.Current.CancellationToken }, i =>
        {
            ValidationReleaseMaterial? material;
            var status = i % 2 == 0
                ? ValidationReleasePreparation.Prepare(candidate, original, out material)
                : ValidationReleasePreparation.Compare(candidate, original, expected, out material);
            Assert.Equal(StructuralMatch, status);
            AssertMaterial(material, MixedPreimage, MixedDigest);
        });
    }

    [Fact]
    public void RefusalPrecedenceAndRecovery()
    {
        var good = new ProjectionData().Create();
        var version = new ProjectionData { Version = 2, Phase = 0 }.Create();
        AssertRefusal(version, null, MissingInput);
        AssertRefusal(new ProjectionData { Version = 2 }.Create(missing: "CaseId"), version, MissingInput);
        AssertRefusal(version, new ProjectionData().Create(missing: "CaseId"), UnsupportedVersion);
        AssertRefusal(new ProjectionData { Phase = 0, CaseId = "." }.Create(), good, InvalidScalar);
        AssertRefusal(new ProjectionData { CaseId = ".", Steps = [] }.Create(), good, InvalidCaseId);
        AssertRefusal(new ProjectionData { Steps = [], CatalogAllows = false }.Create(), good, InvalidLength);
        AssertRefusal(new ProjectionData { CatalogAllows = false, RequestedIndex = 3 }.Create(), good, InvalidTemplate);
        AssertRefusal(new ProjectionData
        {
            Steps = NoReleaseSteps(integrated: true), CatalogAllows = false, FixtureAllows = false, RequestedIndex = 3,
        }.Create(), good, NoReleaseTemplate);
        AssertRefusal(new ProjectionData { RequestedIndex = 3 }.Create(), version, InvalidScalar);
        var mismatched = new ProjectionData { CaseId = "r01", Phase = ValidationReleasePhase.Post }.Create();
        AssertRefusal(mismatched, version, UnsupportedVersion);
        AssertCompareRefusal(mismatched, good, null, CaseMismatch);
        AssertCompareRefusal(new ProjectionData { Phase = ValidationReleasePhase.Post }.Create(), good, [0], ProjectionMismatch);
        AssertCompareRefusal(good, new ProjectionData().Create(), [], InvalidLength);
        AssertCompareRefusal(good, new ProjectionData().Create(), Hex(R02Digest), DigestMismatch);
        Assert.Equal(StructuralMatch,
            ValidationReleasePreparation.Compare(good, new ProjectionData().Create(), Hex(R01Digest), out var fresh));
        AssertMaterial(fresh, R01Preimage, R01Digest);
    }

    [Fact]
    public void SurfaceMatchesFrozenInternalApi()
    {
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
                                      BindingFlags.DeclaredOnly;
        const BindingFlags methods = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public |
                                     BindingFlags.DeclaredOnly;
        var helper = typeof(ValidationReleasePreparation);
        var projection = typeof(ValidationReleaseProjection);
        var material = typeof(ValidationReleaseMaterial);
        var step = typeof(ValidationReleaseStep);
        Assert.True(helper.IsAbstract && helper.IsSealed);
        Assert.True(projection.IsSealed);
        Assert.True(material.IsSealed);
        Assert.True(step.IsValueType && step.IsDefined(typeof(IsReadOnlyAttribute)));

        var outType = material.MakeByRefType();
        foreach (var (name, parameters) in new[]
        {
            ("Prepare", new[] { projection, projection, outType }),
            ("Compare", new[] { projection, projection, typeof(byte[]), outType }),
        })
        {
            var method = helper.GetMethod(name, methods, parameters);
            Assert.NotNull(method);
            Assert.True(method.IsAssembly && method.GetParameters()[^1].IsOut);
            Assert.Equal(typeof(ValidationReleasePreparationStatus), method.ReturnType);
        }
        Assert.Equal(2, helper.GetMethods(methods).Count(m => m.IsAssembly));
        Assert.DoesNotContain(helper.GetMethods(methods), m => m.IsPublic);

        AssertCarrier(projection, ProjectionMembers(), new[]
        {
            typeof(uint?), typeof(ValidationReleasePhase?), typeof(ValidationReleaseTopology?), typeof(string),
            typeof(bool?), typeof(bool?), typeof(ValidationReleaseStep[]), typeof(uint?),
            typeof(byte[]), typeof(byte[]), typeof(byte[]), typeof(byte[]), typeof(Guid?), typeof(Guid?),
            typeof(Guid?), typeof(Guid?), typeof(byte[]),
        }, instance);
        AssertCarrier(step, ["Index", "Opcode"], [typeof(uint), typeof(ValidationReleaseOpcode)], instance);
        AssertCarrier(material, ["Preimage", "CorrelationSha256"],
            [typeof(ImmutableArray<byte>), typeof(ImmutableArray<byte>)], instance);
        Assert.Empty(material.GetMethods(methods));
        Assert.All(material.GetMethods(instance), m => Assert.True(m.IsSpecialName && m.IsAssembly));

        AssertEnum<ValidationReleasePhase>(1, ["Pre", "Post"]);
        AssertEnum<ValidationReleaseTopology>(1, ["B2Lifeline", "B3Integrated"]);
        AssertEnum<ValidationReleaseOpcode>(1, ["ConnectB2", "PrepareB3", "Inspect", "Release", "Stop", "Cleanup"]);
        AssertEnum<ValidationReleasePreparationStatus>(0,
        [
            "MissingInput", "UnsupportedVersion", "InvalidScalar", "InvalidCaseId", "InvalidLength",
            "InvalidTemplate", "NoReleaseTemplate", "CaseMismatch", "ProjectionMismatch",
            "MissingExpectedDigest", "DigestMismatch", "StructuralMatch",
        ]);
        Assert.Equal(MissingInput, default(ValidationReleasePreparationStatus));
    }

    private static void AssertCarrier(Type carrier, string[] names, Type[] types, BindingFlags flags)
    {
        Assert.False(carrier.IsVisible);
        var constructor = Assert.Single(carrier.GetConstructors(flags));
        Assert.True(constructor.IsAssembly);
        Assert.Equal(types, constructor.GetParameters().Select(p => p.ParameterType));
        Assert.All(constructor.GetParameters(), p => Assert.False(p.HasDefaultValue));
        Assert.Equal(names.Length, carrier.GetProperties(flags).Length);
        for (int i = 0; i < names.Length; i++)
        {
            var property = carrier.GetProperty(names[i], flags);
            Assert.NotNull(property);
            Assert.Equal(types[i], property.PropertyType);
            Assert.True(property.GetMethod!.IsAssembly);
            Assert.Null(property.SetMethod);
        }
    }

    private static void AssertEnum<T>(int first, string[] names) where T : struct, Enum
    {
        Assert.False(typeof(T).IsVisible);
        Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(T)));
        Assert.Equal(names, Enum.GetNames<T>());
        Assert.Equal(Enumerable.Range(first, names.Length), Enum.GetValues<T>().Select(v => Convert.ToInt32(v)));
    }

    private static void AssertOneSided(
        ValidationReleaseProjection invalid, bool candidateSide, ValidationReleasePreparationStatus status)
    {
        var valid = new ProjectionData().Create();
        AssertRefusal(candidateSide ? invalid : valid, candidateSide ? valid : invalid, status);
    }

    private static void AssertRefusal(ValidationReleaseProjection? candidate,
        ValidationReleaseProjection? expected, ValidationReleasePreparationStatus status)
    {
        ValidationReleaseMaterial? material = Sentinel();
        Assert.Equal(status, ValidationReleasePreparation.Prepare(candidate, expected, out material));
        Assert.Null(material);
        AssertCompareRefusal(candidate, expected, Hex(R01Digest), status);
    }

    private static void AssertCompareRefusal(ValidationReleaseProjection? candidate,
        ValidationReleaseProjection? expected, byte[]? expectedDigest, ValidationReleasePreparationStatus status)
    {
        ValidationReleaseMaterial? material = Sentinel();
        Assert.Equal(status, ValidationReleasePreparation.Compare(candidate, expected, expectedDigest, out material));
        Assert.Null(material);
    }

    private static ValidationReleaseMaterial Sentinel()
    {
        // Seed the out slot with an existing result to catch refusal paths that leave material there.
        Assert.Equal(StructuralMatch, ValidationReleasePreparation.Prepare(new ProjectionData().Create(),
            new ProjectionData().Create(), out var material));
        return Assert.IsType<ValidationReleaseMaterial>(material);
    }

    private static void AssertMaterial(ValidationReleaseMaterial? material, string expectedPreimage, string digest)
    {
        Assert.NotNull(material);
        Assert.Equal(258, material.Preimage.Length);
        Assert.Equal(32, material.CorrelationSha256.Length);
        Assert.Equal(Hex(expectedPreimage), material.Preimage.ToArray());
        Assert.Equal(Hex(digest), material.CorrelationSha256.ToArray());
    }

    private static byte[] Hex(string value) => Convert.FromHexString(value);

    private static byte[] Repeat(byte value, int count) => Enumerable.Repeat(value, count).ToArray();

    private static ValidationReleaseStep[] NoReleaseSteps(bool integrated) =>
        [new(0, integrated ? PrepareB3 : ConnectB2), new(1, Inspect), new(2, Stop), new(3, Cleanup)];

    private static byte[]?[] Hashes(ValidationReleaseProjection input) =>
        [input.SealedLaunchRecordHash, input.CatalogHash, input.ScriptHash, input.ReleaseActionHash, input.PreparationManifestDigest];

    private static string[] ProjectionMembers() =>
    [
        "SchemaVersion", "Phase", "Topology", "CaseId", "CatalogAllowsRelease", "FixturePolicyAllowsRelease",
        "Steps", "RequestedActionIndex", "SealedLaunchRecordHash", "CatalogHash", "ScriptHash", "ReleaseActionHash",
        "SessionId", "ExecutionId", "ProtocolLeaseId", "PreparationId", "PreparationManifestDigest",
    ];

    private sealed class ProjectionData
    {
        internal ProjectionData(bool mixed = false)
        {
            CaseId = mixed ? "mixed.guid-01" : "R01";
            Phase = mixed ? ValidationReleasePhase.Post : ValidationReleasePhase.Pre;
            Digests = mixed ? Enumerable.Range(0, 5).Select(n =>
                (byte[]?)Enumerable.Range(n * 32, 32).Select(i => (byte)i).ToArray()).ToArray()
                : [Repeat(0x11, 32), Repeat(0x22, 32), Repeat(0x33, 32), Repeat(0x44, 32), Repeat(0x99, 32)];
            Ids = mixed
                ? [new("00112233-4455-6677-8899-aabbccddeeff"), new("12345678-90ab-cdef-0123-456789abcdef"),
                   new("89abcdef-0123-4567-89ab-cdef01234567"), new("fedcba98-7654-3210-fedc-ba9876543210")]
                : [new("55555555-5555-5555-5555-555555555555"), new("66666666-6666-6666-6666-666666666666"),
                   new("77777777-7777-7777-7777-777777777777"), new("88888888-8888-8888-8888-888888888888")];
        }

        internal uint Version { get; init; } = 1;
        internal ValidationReleasePhase Phase { get; init; }
        internal ValidationReleaseTopology Topology { get; init; } = ValidationReleaseTopology.B3Integrated;
        internal string CaseId { get; init; }
        internal bool CatalogAllows { get; init; } = true;
        internal bool FixtureAllows { get; init; } = true;
        internal uint RequestedIndex { get; init; } = 2;
        internal ValidationReleaseStep[] Steps { get; init; } =
            [new(0, PrepareB3), new(1, Inspect), new(2, Release), new(3, Inspect), new(4, Stop), new(5, Cleanup)];
        internal byte[]?[] Digests { get; }
        internal Guid[] Ids { get; }

        internal ValidationReleaseProjection Create(string? missing = null) => new(
            missing == "SchemaVersion" ? null : Version,
            missing == "Phase" ? null : Phase,
            missing == "Topology" ? null : Topology,
            missing == "CaseId" ? null : CaseId,
            missing == "CatalogAllowsRelease" ? null : CatalogAllows,
            missing == "FixturePolicyAllowsRelease" ? null : FixtureAllows,
            missing == "Steps" ? null : Steps,
            missing == "RequestedActionIndex" ? null : RequestedIndex,
            missing == "SealedLaunchRecordHash" ? null : Digests[0],
            missing == "CatalogHash" ? null : Digests[1],
            missing == "ScriptHash" ? null : Digests[2],
            missing == "ReleaseActionHash" ? null : Digests[3],
            missing == "SessionId" ? null : Ids[0],
            missing == "ExecutionId" ? null : Ids[1],
            missing == "ProtocolLeaseId" ? null : Ids[2],
            missing == "PreparationId" ? null : Ids[3],
            missing == "PreparationManifestDigest" ? null : Digests[4]);
    }
}
