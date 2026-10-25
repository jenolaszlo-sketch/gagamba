// RC2A-02A Q2A-B boundary and review follow-up tests (test-only). Covers the
// symmetric manifest-size ceiling, argv/environment UTF-8 byte boundaries, argv
// list/item distinctions, independent digest recomputation, identifier
// behavior, full truncation and single-byte mutation sweeps, and the R2A-F01
// defensive re-encode branch probe. No production change is made.
using System.Buffers.Binary;
using System.Diagnostics;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Contracts;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

public sealed class Q2A_B_BoundaryTests
{
    private const int GoldenBaseWithoutModeContent = 869;

    [Fact]
    public void Q2A_B_01_ManifestSize65535IsAccepted() => AssertAcceptedManifestSize(65535);

    [Fact]
    public void Q2A_B_02_ManifestSize65536IsAccepted() => AssertAcceptedManifestSize(65536);

    [Fact]
    public void Q2A_B_03_ManifestSize65537IsRefusedSymmetrically()
    {
        const int size = 65537;
        var model = PaddedModel(size);
        var legacy = ManifestCodec.Encode(model);
        Assert.Equal(size, legacy.Length);
        Assert.Equal("manifest", Assert.Throws<ArgumentException>(
            () => ManifestCodec.Encode(model, RestrictedManifestCodec.MaxCanonicalBytes)).ParamName);
        Assert.Equal("manifest", Assert.Throws<ArgumentException>(
            () => new RestrictedManifestView(model)).ParamName);
        var result = RestrictedManifestCodec.Decode(legacy);
        Assert.Null(result.Value);
        Assert.Equal(ManifestDecodeError.InputTooLarge, result.Error);
        Assert.Equal(0, result.Offset);
    }

    [Fact]
    public void Q2A_B_04_ManifestSize1000000IsRefusedBeforeParsing()
    {
        const int size = 1000000;
        var model = PaddedModel(size);
        var legacy = ManifestCodec.Encode(model);
        Assert.Equal(size, legacy.Length);
        Assert.Equal("manifest", Assert.Throws<ArgumentException>(
            () => ManifestCodec.Encode(model, RestrictedManifestCodec.MaxCanonicalBytes)).ParamName);
        Assert.Equal("manifest", Assert.Throws<ArgumentException>(
            () => new RestrictedManifestView(model)).ParamName);
        var result = RestrictedManifestCodec.Decode(legacy);
        Assert.Null(result.Value);
        Assert.Equal(ManifestDecodeError.InputTooLarge, result.Error);
        Assert.Equal(0, result.Offset);
    }

    [Fact]
    public void Q2A_B_05_EmptyInputRefusesEmptyInputAtZero()
    {
        var result = RestrictedManifestCodec.Decode(ReadOnlySpan<byte>.Empty);
        Assert.Null(result.Value);
        Assert.Equal(ManifestDecodeError.EmptyInput, result.Error);
        Assert.Equal(0, result.Offset);
    }

    [Fact]
    public void Q2A_B_06_TrailingDataRefusesAtReaderEnd()
    {
        var golden = QualificationModelTools.GoldenBytes();
        var withTrailing = golden.Concat(new byte[] { 0x00 }).ToArray();
        var result = RestrictedManifestCodec.Decode(withTrailing);
        Assert.Null(result.Value);
        Assert.Equal(ManifestDecodeError.TrailingData, result.Error);
        Assert.Equal(golden.Length, result.Offset);
    }

    [Theory]
    [InlineData(16383)]
    [InlineData(16384)]
    public void Q2A_B_07_ArgenvBoundaryIsAcceptedAtTheInclusiveCeiling(int bytes)
    {
        var model = ArgenvModel(bytes);
        Assert.Equal(bytes, CombinedArgenvBytes(model));
        var view = new RestrictedManifestView(model);
        var reference = QualificationModelTools.ReferenceEncode(model);
        var production = RestrictedManifestCodec.Encode(view);
        Assert.Equal(reference, production);
        var decoded = RestrictedManifestCodec.Decode(production);
        Assert.Null(decoded.Error);
        var decodedView = Assert.IsType<RestrictedManifestView>(decoded.Value);
        QualificationModelTools.AssertGraph(model, decodedView);
        Assert.Equal(production, RestrictedManifestCodec.Encode(decodedView));
    }

    [Fact]
    public void Q2A_B_08_Argenv16385RefusesConstruction()
    {
        Assert.Throws<ArgumentException>(() => ArgenvModel(16385));
    }

    [Fact]
    public void Q2A_B_09_Argenv16385OnTheWireRefusesInvalidValueAtTheInvocationEnd()
    {
        var refusal = new WireRefusalCase(
            "Q2A-B-09-argenv-16385-wire", "InvocationManifest.Argv", "length", "malformed",
            "A wire manifest whose combined argv/environment payload is 16,385 bytes refuses InvalidValue at the reader position after the invocation fields",
            ManifestDecodeError.InvalidValue,
            () =>
            {
                var template = QualificationWireTemplate.Golden();
                template.Argv = new[] { new string('a', 16384) };
                template.Environment = new[] { new QualificationWireEnvPair { Key = "K", Value = string.Empty } };
                return template.Build();
            },
            "channels.stdinEof",
            buildCanonical: () =>
            {
                var template = QualificationWireTemplate.Golden();
                template.Argv = new[] { new string('a', 16383) };
                template.Environment = new[] { new QualificationWireEnvPair { Key = "K", Value = string.Empty } };
                return template.Build();
            });
        refusal.Execute();
    }

    [Fact]
    public void Q2A_B_10_NonAsciiBoundaryIsMeasuredInUtf8Bytes()
    {
        var argument = new string('\u00E9', 8191); // 16,382 UTF-8 bytes, 8,191 chars
        var key = "\u00E9"; // 2 UTF-8 bytes
        Assert.Equal(16382, QualificationUtf8.Strict.GetByteCount(argument));
        var model = QualificationModelTools.Replace(
            QualificationModelTools.Baseline(), "Invocation",
            new InvocationManifest("/tool", "work", "/input", new[] { argument },
                new[] { new KeyValuePair<string, string>(key, string.Empty) }, "/tmp"));
        Assert.Equal(16384, CombinedArgenvBytes(model));
        var view = new RestrictedManifestView(model);
        var encoded = RestrictedManifestCodec.Encode(view);
        Assert.Equal(QualificationModelTools.ReferenceEncode(model), encoded);
        Assert.Null(RestrictedManifestCodec.Decode(encoded).Error);

        Assert.Throws<ArgumentException>(() => new InvocationManifest("/tool", "work", "/input",
            new[] { argument }, new[] { new KeyValuePair<string, string>(key, "x") }, "/tmp"));
    }

    [Fact]
    public void Q2A_B_11_EmptyArgvItemRefusesConstruction()
    {
        Assert.Throws<ArgumentException>(() => new InvocationManifest("/tool", "work", "/input",
            new[] { string.Empty }, Array.Empty<KeyValuePair<string, string>>(), "/tmp"));
    }

    [Fact]
    public void Q2A_B_12_EmptyArgvItemRefusesOnTheWire()
    {
        var refusal = QualificationRefusalFactories.EmptyText(
            "Q2A-B-12-empty-argv-item-wire", "InvocationManifest.Argv", "argv.item[1]",
            "An empty argv item on the wire refuses InvalidValue at the item length-prefix start");
        refusal.Execute();
    }

    [Fact]
    public void Q2A_B_13_ZeroLengthArgvListIsAcceptedAndEncodesAZeroCount()
    {
        var model = QualificationModelTools.WithInvocation(
            QualificationModelTools.Baseline(), "Argv", Array.Empty<string>());
        var view = new RestrictedManifestView(model);
        var encoded = RestrictedManifestCodec.Encode(view);
        var template = QualificationWireTemplate.Golden();
        template.Argv = Array.Empty<string>();
        var built = template.Build();
        Assert.Equal(built.Bytes, encoded);
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(
            encoded.AsSpan(built.Offsets["argv.count"])));
        var decoded = RestrictedManifestCodec.Decode(encoded);
        Assert.Null(decoded.Error);
        var decodedView = Assert.IsType<RestrictedManifestView>(decoded.Value);
        Assert.Empty(decodedView.Invocation.Argv);
        Assert.Equal(encoded, RestrictedManifestCodec.Encode(decodedView));
    }

    [Fact]
    public void Q2A_B_14_EmptyArgvItemAndZeroLengthArgvListAreDistinct()
    {
        var emptyList = QualificationModelTools.WithInvocation(
            QualificationModelTools.Baseline(), "Argv", Array.Empty<string>());
        Assert.Empty(new RestrictedManifestView(emptyList).Invocation.Argv);
        Assert.Throws<ArgumentException>(() => new InvocationManifest("/tool", "work", "/input",
            new[] { string.Empty }, Array.Empty<KeyValuePair<string, string>>(), "/tmp"));

        var template = QualificationWireTemplate.Golden();
        template.Argv = new[] { string.Empty };
        var built = template.Build();
        var result = RestrictedManifestCodec.Decode(built.Bytes);
        Assert.Null(result.Value);
        Assert.Equal(ManifestDecodeError.InvalidValue, result.Error);
        Assert.Equal(built.Offsets["argv.item[0]"], result.Offset);
    }

    [Fact]
    public void Q2A_B_15_ComputeSha256MatchesIndependentShaForEveryVector()
    {
        foreach (var vector in QualificationVectorRecipes.All)
        {
            var committed = QualificationVectorBytes.ById[vector.Id];
            var bytes = Convert.FromHexString(committed.Hex);
            var decoded = RestrictedManifestCodec.Decode(bytes);
            var view = Assert.IsType<RestrictedManifestView>(decoded.Value);
            Assert.Equal(committed.Sha256, QualificationHash.Sha256Hex(bytes));
            Assert.Equal(
                System.Security.Cryptography.SHA256.HashData(bytes),
                RestrictedManifestCodec.ComputeSha256(view).ToArray());
        }
    }

    [Fact]
    public void Q2A_B_16_IdentifierBehaviorMatchesTheQualifiedCases()
    {
        QualificationFieldMatrix.ByCaseId["Q2A-F-01-supported-id"].Execute();
        QualificationFieldMatrix.ByCaseId["Q2A-F-02-supported-id"].Execute();
        QualificationFieldMatrix.ByCaseId["Q2A-F-02-misuse"].Execute();
    }

    [Fact]
    public void Q2A_B_17_EveryProperGoldenPrefixRefusesWithoutPartialValue()
    {
        var golden = QualificationModelTools.GoldenBytes();
        var errors = new Dictionary<ManifestDecodeError, int>();
        for (var length = 0; length < golden.Length; length++)
        {
            var result = RestrictedManifestCodec.Decode(golden.AsSpan(0, length));
            Assert.Null(result.Value);
            Assert.NotNull(result.Error);
            Assert.NotNull(result.Offset);
            Assert.InRange(result.Offset!.Value, 0, length);
            if (length == 0)
            {
                Assert.Equal(ManifestDecodeError.EmptyInput, result.Error);
                Assert.Equal(0, result.Offset);
            }
            else if (result.Error == ManifestDecodeError.Truncated)
            {
                Assert.Equal(length, result.Offset);
            }

            errors[result.Error!.Value] = errors.GetValueOrDefault(result.Error.Value) + 1;
        }

        Assert.True(errors.ContainsKey(ManifestDecodeError.Truncated));
        Assert.False(errors.ContainsKey(ManifestDecodeError.InvalidValue));
    }

    [Fact]
    public void Q2A_B_18_SingleByteFlipSweepEnforcesCanonicalRigidity()
    {
        var sweep = FlipSweep.Value;
        Assert.True(sweep.Accepted > 0);
        Assert.True(sweep.Refused > 0);
        Assert.Equal(0, sweep.InvalidValueAtZero);
        Assert.Contains(ManifestDecodeError.InvalidTag, sweep.ErrorCounts.Keys);
        Assert.Contains(ManifestDecodeError.InvalidLengthOrCount, sweep.ErrorCounts.Keys);
        Assert.Contains(ManifestDecodeError.NonCanonicalOrder, sweep.ErrorCounts.Keys);
        Assert.Contains(ManifestDecodeError.InvalidUtf8, sweep.ErrorCounts.Keys);

        var regions = QualificationWireTemplate.Golden().Build(recordRegions: true).Regions;
        QualificationEvidenceExports.WriteFlipSweep(sweep, regions);
        var immutable = ImmutableByteMap(regions);
        foreach (var position in sweep.Survivors)
        {
            var slot = regions.First(region => position >= region.Start && position < region.Start + region.Length).SlotId;
            Assert.False(immutable[position],
                $"Single-byte mutation at byte {position} (slot {slot}, values {string.Join(",", sweep.SurvivorValues[position].Order().Select(value => $"0x{value:X2}"))}) was accepted inside an immutable region; the decoder accepted a noncanonical image.");
        }
    }

    [Fact]
    public void Q2A_B_19_ReencodeBranchAssessmentProbe()
    {
        // R2A-F01 assessment: the defensive exact re-encode comparison in
        // ManifestReader.Decode executes as a pass-through assertion on every
        // accepted decode. Its failure path (InvalidValue at zero) is the only
        // decoder refusal with offset zero besides EmptyInput/InputTooLarge and
        // UnsupportedSchema; a reachable divergence would be observed as
        // InvalidValue@0 for a parseable input. Neither the complete 222,615
        // single-byte mutation sweep nor the entire vector and refusal catalog
        // observes that refusal, and every accepted input re-encodes byte-exactly.
        var sweep = FlipSweep.Value;
        Assert.Equal(0, sweep.InvalidValueAtZero);
        Assert.True(sweep.Accepted > 0);

        foreach (var vector in QualificationVectorRecipes.All)
        {
            var bytes = Convert.FromHexString(QualificationVectorBytes.ById[vector.Id].Hex);
            var decoded = RestrictedManifestCodec.Decode(bytes);
            Assert.Null(decoded.Error);
            Assert.Equal(bytes, RestrictedManifestCodec.Encode(
                Assert.IsType<RestrictedManifestView>(decoded.Value)));
        }

        foreach (var item in QualificationRefusalCatalog.Cases)
        {
            item.Execute();
        }
    }

    private static void AssertAcceptedManifestSize(int size)
    {
        var model = PaddedModel(size);
        var reference = QualificationModelTools.ReferenceEncode(model);
        Assert.Equal(size, reference.Length);
        var view = new RestrictedManifestView(model);
        var production = RestrictedManifestCodec.Encode(view);
        Assert.Equal(reference, production);
        var decoded = RestrictedManifestCodec.Decode(production);
        Assert.Null(decoded.Error);
        Assert.Null(decoded.Offset);
        var decodedView = Assert.IsType<RestrictedManifestView>(decoded.Value);
        QualificationModelTools.AssertGraph(model, decodedView);
        Assert.Equal(production, RestrictedManifestCodec.Encode(decodedView));
    }

    private static RestrictedManifest PaddedModel(int totalBytes) =>
        QualificationModelTools.Model(mode: new string('a', totalBytes - GoldenBaseWithoutModeContent));

    private static RestrictedManifest ArgenvModel(int totalBytes)
    {
        var argument = new string('a', totalBytes - 1);
        return QualificationModelTools.Replace(
            QualificationModelTools.Baseline(), "Invocation",
            new InvocationManifest("/tool", "work", "/input", new[] { argument },
                new[] { new KeyValuePair<string, string>("K", string.Empty) }, "/tmp"));
    }

    private static int CombinedArgenvBytes(RestrictedManifest model)
    {
        var invocation = model.Invocation;
        return invocation.Argv.Sum(QualificationUtf8.Strict.GetByteCount) +
            invocation.Environment.Sum(pair =>
                QualificationUtf8.Strict.GetByteCount(pair.Key) +
                QualificationUtf8.Strict.GetByteCount(pair.Value));
    }

    private static bool[] ImmutableByteMap(IReadOnlyList<QualificationWireRegion> regions)
    {
        var byteCount = QualificationModelTools.GoldenBytes().Length;
        var immutable = new bool[byteCount];
        var covered = new bool[byteCount];
        foreach (var region in regions)
        {
            for (var i = 0; i < region.Length; i++)
            {
                var position = region.Start + i;
                Assert.False(covered[position], $"Region coverage overlaps at byte {position}.");
                covered[position] = true;
                immutable[position] = QualificationEvidenceExports.ImmutableSlots.Contains(region.SlotId, StringComparer.Ordinal);
            }
        }

        Assert.All(covered, item => Assert.True(item, "The golden byte map must cover every byte."));
        return immutable;
    }

    private static readonly Lazy<QualificationFlipSweepResult> FlipSweep = new(ComputeFlipSweep);

    private static QualificationFlipSweepResult ComputeFlipSweep()
    {
        var golden = QualificationModelTools.GoldenBytes();
        var survivors = new HashSet<int>();
        var survivorValues = new Dictionary<int, List<int>>();
        var errors = new Dictionary<ManifestDecodeError, int>();
        var accepted = 0;
        var refused = 0;
        var invalidValueAtZero = 0;
        var mutated = new byte[golden.Length];
        var stopwatch = Stopwatch.StartNew();
        for (var position = 0; position < golden.Length; position++)
        {
            for (var value = 0; value < 256; value++)
            {
                if (value == golden[position])
                {
                    continue;
                }

                Array.Copy(golden, mutated, golden.Length);
                mutated[position] = (byte)value;
                var result = RestrictedManifestCodec.Decode(mutated);
                if (result.Value is null)
                {
                    refused++;
                    Assert.NotNull(result.Error);
                    Assert.NotNull(result.Offset);
                    Assert.InRange(result.Offset!.Value, 0, mutated.Length);
                    errors[result.Error!.Value] = errors.GetValueOrDefault(result.Error.Value) + 1;
                    if (result.Error == ManifestDecodeError.InvalidValue && result.Offset == 0)
                    {
                        invalidValueAtZero++;
                    }

                    continue;
                }

                accepted++;
                survivors.Add(position);
                if (!survivorValues.TryGetValue(position, out var values))
                {
                    values = new List<int>();
                    survivorValues[position] = values;
                }

                values.Add(value);
                var reencoded = RestrictedManifestCodec.Encode(result.Value);
                if (!reencoded.AsSpan().SequenceEqual(mutated))
                {
                    throw new Xunit.Sdk.XunitException(
                        $"Accepted single-byte mutation at byte {position} (value 0x{value:X2}) is not its own canonical encoding.");
                }
            }
        }

        stopwatch.Stop();
        return new QualificationFlipSweepResult(accepted, refused, invalidValueAtZero, survivors, survivorValues, errors, stopwatch.Elapsed, golden.Length);
    }
}

internal sealed record QualificationFlipSweepResult(
    int Accepted,
    int Refused,
    int InvalidValueAtZero,
    HashSet<int> Survivors,
    Dictionary<int, List<int>> SurvivorValues,
    Dictionary<ManifestDecodeError, int> ErrorCounts,
    TimeSpan Duration,
    int ByteCount);
