using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Gagamba.Execution.Restricted.Contracts;
using Xunit;

namespace Gagamba.Execution.Tests.Restricted;

public sealed class RestrictedContractTests
{
    // Exact existing WP01 literal; embedded as source so these tests need no sibling/test assembly.
    internal const string GoldenHex = "01001b0000006c696e75782d6d312d6e61746976652d7472616e73616374696f6e0600000003000000697063050000006d6f756e74030000006e65740300000070696404000000757365720300000075747300000000010102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f203801000000000000030000000c0000002f636f6f7264696e61746f72060000002f696e707574050000002f746f6f6c02000000080000002f6465762f73686d0000100000000000000000000000000000040000002f746d7000007000000000000000000000000000000101010100010100000008000000000500000062797465730001000000000000000005000000627974657300013000000000000000050000007461736b7300014000000000000000030000006664730001000070000000000005000000627974657300010000100000000000050000006279746573000100000100000000000500000062797465730001000001000000000005000000627974657300010000020000000000050000006279746573000100e40b5402000000020000006e73000100ac23fc06000000020000006e73000100f2052a01000000020000006e73000100ca9a3b00000000020000006e730001010000000000010000000080000000004000000000000000400000000000000040000000000000000001000000000000800000000000000300000003000000617070202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3fc800000000000000040000002f6170700e000000646f746e65742d7075626c69736805000000696e707574303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f2c01000000000000060000002f696e70757406000000636f7079696e04000000746f6f6c101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f6400000000000000050000002f746f6f6c0a0000006763632d7374617469630200000005000000612d646570050000007a2d646570050000002f746f6f6c04000000776f726b060000002f696e70757402000000040000002d2d696e060000002f696e7075740200000004000000484f4d45040000002f746d70040000004c414e470100000043040000002f746d7001000001000000000000000100000000000000020000000000";
    private const string GoldenDigest = "82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3";
    private static byte[] Golden() => Convert.FromHexString(GoldenHex);
    private static RestrictedManifestView View() => Assert.IsType<RestrictedManifestView>(RestrictedManifestCodec.Decode(Golden()).Value);

    [Fact]
    public void PreservedGoldenRoundTripsAndVerifiesDigest()
    {
        var bytes = Golden();
        var result = RestrictedManifestCodec.Decode(bytes);
        var view = Assert.IsType<RestrictedManifestView>(result.Value);
        Assert.Null(result.Error);
        Assert.Null(result.Offset);
        Assert.Equal(873, bytes.Length);
        Assert.Equal(bytes, RestrictedManifestCodec.Encode(view));
        Assert.Equal(Convert.FromHexString(GoldenDigest), RestrictedManifestCodec.ComputeSha256(view).ToArray());
        Assert.True(RestrictedManifestCodec.VerifySha256(view, Convert.FromHexString(GoldenDigest)));
        Assert.Equal((ushort)1, view.SchemaVersion);
        Assert.Equal("linux-m1-native-transaction", view.ImplementationId);
        Assert.Equal("work", view.Invocation.Mode);
        Assert.Equal(new[] { "--in", "/input" }, view.Invocation.Argv);
        Assert.Null(view.Resources.CoreDumps.CorePatternObserved);
        Assert.Equal(QualificationDisposition.PendingQualification, view.Authority.NetworkQualification);
        Assert.Equal(QualificationDisposition.PendingQualification, view.Resources.InodeQualification);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(33)]
    public void WrongDigestLengthReturnsFalse(int length) =>
        Assert.False(RestrictedManifestCodec.VerifySha256(View(), new byte[length]));

    [Fact]
    public void NonmatchingAndAllZeroDigestsAreNotWildcards()
    {
        var digest = Convert.FromHexString(GoldenDigest);
        digest[0] ^= 1;
        Assert.False(RestrictedManifestCodec.VerifySha256(View(), digest));
        Assert.False(RestrictedManifestCodec.VerifySha256(View(), new byte[32]));
    }

    [Theory]
    [InlineData(1, "linux-m1-native-transaction", true)]
    [InlineData(0, "linux-m1-native-transaction", false)]
    [InlineData(2, "linux-m1-native-transaction", false)]
    [InlineData(65535, "linux-m1-native-transaction", false)]
    [InlineData(1, "unknown", false)]
    [InlineData(1, "", false)]
    [InlineData(1, " ", false)]
    [InlineData(1, "Linux-m1-native-transaction", false)]
    public void SupportsIsExact(int schema, string implementation, bool expected) =>
        Assert.Equal(expected, RestrictedManifestCodec.Supports((ushort)schema, implementation));

    [Fact]
    public void NullArgumentsAreProgrammingErrors()
    {
        Assert.Throws<ArgumentNullException>(() => RestrictedManifestCodec.Supports(0, null!));
        Assert.Throws<ArgumentNullException>(() => RestrictedManifestCodec.Supports(1, null!));
        Assert.Throws<ArgumentNullException>(() => RestrictedManifestCodec.Supports(ushort.MaxValue, null!));
        Assert.Throws<ArgumentNullException>(() => RestrictedManifestCodec.Encode(null!));
        Assert.Throws<ArgumentNullException>(() => RestrictedManifestCodec.ComputeSha256(null!));
        Assert.Throws<ArgumentNullException>(() => RestrictedManifestCodec.VerifySha256(null!, new byte[32]));
    }

    [Fact]
    public void EveryTruncationRefusesWithoutPartialView()
    {
        var bytes = Golden();
        for (var length = 0; length < bytes.Length; length++)
        {
            var result = RestrictedManifestCodec.Decode(bytes.AsSpan(0, length));
            Assert.Null(result.Value);
            Assert.NotNull(result.Error);
            Assert.InRange(result.Offset!.Value, 0, length);
        }
        Refuses(Array.Empty<byte>(), ManifestDecodeError.EmptyInput);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(65535)]
    public void UnknownSchemasRefuse(int schema)
    {
        var bytes = Golden();
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, (ushort)schema);
        Refuses(bytes, ManifestDecodeError.UnsupportedSchema);
    }

    [Fact]
    public void UnknownImplementationAndMalformedWireRefuse()
    {
        var bytes = Golden();
        bytes[6] = (byte)'L';
        Refuses(bytes, ManifestDecodeError.UnsupportedImplementation);
        bytes = Golden(); bytes[6] = 0xff;
        Refuses(bytes, ManifestDecodeError.InvalidUtf8);
        bytes = Golden(); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2), uint.MaxValue);
        Refuses(bytes, ManifestDecodeError.InvalidLengthOrCount);
        bytes = Golden(); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(33), uint.MaxValue);
        Refuses(bytes, ManifestDecodeError.InvalidLengthOrCount);
        bytes = Golden().Concat(new byte[] { 0 }).ToArray();
        Refuses(bytes, ManifestDecodeError.TrailingData);
        bytes = Golden(); bytes[^25] = 2; // stdin boolean
        Refuses(bytes, ManifestDecodeError.InvalidTag);
        bytes = Golden(); bytes[^25] = 0;
        Refuses(bytes, ManifestDecodeError.InvalidValue);
    }

    [Fact]
    public void UnsortedAndDuplicateSetsAreNotNormalized()
    {
        var bytes = Golden();
        // namespaces "ipc" and "net" have equal byte lengths.
        Encoding.UTF8.GetBytes("net").CopyTo(bytes, 41);
        Encoding.UTF8.GetBytes("ipc").CopyTo(bytes, 57);
        Refuses(bytes, ManifestDecodeError.NonCanonicalOrder);
        bytes = Golden(); Encoding.UTF8.GetBytes("pid").CopyTo(bytes, 57);
        Refuses(bytes, ManifestDecodeError.Duplicate);
    }

    [Theory]
    [InlineData(86, 2, ManifestDecodeError.InvalidTag)] // noNewPrivs bool
    [InlineData(224, 0, ManifestDecodeError.InvalidValue)] // ambient environment denial
    [InlineData(227, 0, ManifestDecodeError.InvalidValue)] // tool executable
    [InlineData(229, 0, ManifestDecodeError.InvalidValue)] // network must remain pending
    [InlineData(230, 3, ManifestDecodeError.InvalidTag)] // budget kind
    [InlineData(230, 0, ManifestDecodeError.InvalidValue)] // absent cannot carry nonzero budget
    [InlineData(248, 2, ManifestDecodeError.InvalidTag)] // budget qualification
    [InlineData(463, 0, ManifestDecodeError.InvalidValue)] // inode must remain pending
    [InlineData(464, 0, ManifestDecodeError.InvalidValue)] // core disabled
    [InlineData(466, 2, ManifestDecodeError.InvalidTag)] // core-pattern presence
    public void TagsAndForcedValuesRejectAtTheirField(int offset, int value, ManifestDecodeError error)
    {
        var bytes = Golden(); bytes[offset] = (byte)value;
        var result = RestrictedManifestCodec.Decode(bytes);
        Assert.Null(result.Value);
        Assert.Equal(error, result.Error);
        Assert.Equal(offset, result.Offset);
    }

    [Theory]
    [InlineData("c0af")] // overlong encoding
    [InlineData("eda080")] // UTF-8 encoded surrogate
    [InlineData("f4908080")] // beyond Unicode range
    public void StrictUtf8DoesNotRepairMalformedSequences(string hex)
    {
        var bytes = Golden();
        Convert.FromHexString(hex).CopyTo(bytes, 6);
        Refuses(bytes, ManifestDecodeError.InvalidUtf8);
    }

    [Fact]
    public void InputAndOutputArraysDoNotAliasViews()
    {
        var bytes = Golden();
        var view = Assert.IsType<RestrictedManifestView>(RestrictedManifestCodec.Decode(bytes).Value);
        Array.Fill(bytes, (byte)0);
        var encoded = RestrictedManifestCodec.Encode(view);
        Array.Fill(encoded, (byte)0);
        var digestCopy = view.Authority.FilterSha256.ToArray();
        Array.Fill(digestCopy, (byte)0);
        Assert.Equal(Golden(), RestrictedManifestCodec.Encode(view));
        Assert.Equal((byte)1, view.Authority.FilterSha256[0]);
    }

    [Fact]
    public void DecodeRefusesOversizedInputBeforeParsing()
    {
        Refuses(new byte[65537], ManifestDecodeError.InputTooLarge);
        Refuses(new byte[1000000], ManifestDecodeError.InputTooLarge);
    }

    [Fact]
    public void PassiveReferenceCopiesAndComparesContents()
    {
        var ids = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var bytes = Convert.FromHexString(GoldenDigest);
        var reference = new RestrictedPreparationReference(ids[0], ids[1], ids[2], ids[3], bytes);
        var equal = new RestrictedPreparationReference(ids[0], ids[1], ids[2], ids[3], bytes.ToArray());
        Assert.Equal(reference, equal);
        Assert.Equal(reference.GetHashCode(), equal.GetHashCode());
        Assert.True(reference.Equals((object)equal));
        Assert.False(reference.Equals(null));
        bytes[0] ^= 1;
        Assert.NotEqual(reference, new RestrictedPreparationReference(ids[0], ids[1], ids[2], ids[3], bytes));
        Assert.Equal(Convert.FromHexString(GoldenDigest), reference.ManifestSha256.ToArray());
        for (var i = 0; i < 4; i++)
        {
            var changed = ids.ToArray(); changed[i] = Guid.NewGuid();
            Assert.NotEqual(reference, new RestrictedPreparationReference(changed[0], changed[1], changed[2], changed[3], reference.ManifestSha256.AsSpan()));
            changed[i] = Guid.Empty;
            Assert.Throws<ArgumentException>(() => new RestrictedPreparationReference(changed[0], changed[1], changed[2], changed[3], bytes));
        }
        foreach (var length in new[] { 0, 31, 33 })
            Assert.Throws<ArgumentException>(() => new RestrictedPreparationReference(ids[0], ids[1], ids[2], ids[3], new byte[length]));
        Assert.Equal(new byte[32], new RestrictedPreparationReference(ids[0], ids[1], ids[2], ids[3], new byte[32]).ManifestSha256.ToArray());
    }

    [Fact]
    public void PublicSurfaceIsSealedReadOnlyAndPassive()
    {
        var types = typeof(RestrictedManifestCodec).Assembly.GetExportedTypes()
            .Where(t => t.Namespace == typeof(RestrictedManifestCodec).Namespace).ToArray();
        Assert.NotEmpty(types);
        foreach (var type in types.Where(t => !t.IsEnum))
        {
            Assert.True(type.IsSealed);
            foreach (var property in type.GetProperties())
                Assert.Null(property.SetMethod);
            if (type != typeof(RestrictedPreparationReference))
                Assert.Empty(type.GetConstructors());
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                Assert.DoesNotContain("Async", method.Name);
                Assert.DoesNotContain("Release", method.Name);
                Assert.DoesNotContain("Prepare", method.Name);
                Assert.DoesNotContain("Start", method.Name);
                Assert.DoesNotContain("Lookup", method.Name);
            }
        }
        Assert.Equal(new[] { "ExecutionId", "GenerationId", "IssuerId", "ManifestSha256", "PreparationId" },
            typeof(RestrictedPreparationReference).GetProperties().Select(p => p.Name).Order().ToArray());
    }

    private static void Refuses(byte[] bytes, ManifestDecodeError error)
    {
        var result = RestrictedManifestCodec.Decode(bytes);
        Assert.Null(result.Value);
        Assert.Equal(error, result.Error);
        Assert.InRange(result.Offset!.Value, 0, bytes.Length);
    }
}
