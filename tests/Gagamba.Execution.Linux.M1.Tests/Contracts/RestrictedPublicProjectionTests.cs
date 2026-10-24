using System.Collections;
using System.Reflection;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Contracts;
using Xunit;
using BudgetValueKind = Gagamba.Execution.Restricted.BudgetValueKind;
using QualificationDisposition = Gagamba.Execution.Restricted.QualificationDisposition;

namespace Gagamba.Execution.Linux.M1.Tests.Contracts;

// Portable deterministic tests only; the existing M1 friend identity supplies the internal seam.
public sealed class RestrictedPublicProjectionTests
{
    [Fact]
    public void EveryModelPropertySurvivesProjectionAndDecode()
    {
        var model = ManifestTests.SampleManifest();
        var view = new RestrictedManifestView(model);
        AssertGraph(model, view);
        var result = RestrictedManifestCodec.Decode(ManifestCodec.Encode(model));
        Assert.Null(result.Error);
        AssertGraph(model, Assert.IsType<RestrictedManifestView>(result.Value));
        Assert.Equal(ReferenceEncoder.Encode(model), RestrictedManifestCodec.Encode(view));
    }

    [Theory]
    [InlineData(65536)]
    [InlineData(65537)]
    public void CompleteUtf8ByteCeilingIsInclusiveAndSymmetric(int size)
    {
        var original = ManifestTests.SampleManifest();
        var baseline = ManifestCodec.Encode(original).Length - original.Invocation.Mode.Length;
        // Multibyte text proves the boundary measures encoded bytes, not UTF-16 length.
        var modeBytes = size - baseline;
        var mode = new string('\u00e9', modeBytes / 2) + (modeBytes % 2 == 1 ? "x" : "");
        var model = ManifestTests.SampleManifest(mode: mode);
        var bytes = ManifestCodec.Encode(model); // legacy encoder remains unchanged/unbounded
        Assert.Equal(size, bytes.Length);
        if (size == 65536)
        {
            var view = new RestrictedManifestView(model);
            Assert.Equal(bytes, RestrictedManifestCodec.Encode(view));
            var decoded = RestrictedManifestCodec.Decode(bytes);
            Assert.Null(decoded.Error);
            AssertGraph(model, Assert.IsType<RestrictedManifestView>(decoded.Value));
            Assert.Equal(bytes, RestrictedManifestCodec.Encode(decoded.Value!));
            Assert.Equal(ReferenceEncoder.Encode(model), bytes);
        }
        else
        {
            Assert.Equal("manifest", Assert.Throws<ArgumentException>(() => ManifestCodec.Encode(model, 65536)).ParamName);
            Assert.Equal("manifest", Assert.Throws<ArgumentException>(() => new RestrictedManifestView(model)).ParamName);
            Assert.Equal(ManifestDecodeError.InputTooLarge, RestrictedManifestCodec.Decode(bytes).Error);
            Assert.Null(RestrictedManifestCodec.Decode(bytes).Value);
        }
    }

    [Fact]
    public void AllRepresentableDistinctionsRemainDistinct()
    {
        var original = ManifestTests.SampleManifest();
        var resources = original.Resources;
        var variants = new[]
        {
            Replace(original, "Authority", Replace(original.Authority, "NoNewPrivs", false)),
            Replace(original, "Resources", Replace(resources, "CoreDumps", new CoreDumpPolicy(true, true, "core\u0000\U00010000"))),
            Replace(original, "Resources", Replace(resources, "Memory", new ResourceBudget(BudgetValueKind.Absent, 0, "bytes", QualificationDisposition.Mandatory))),
            Replace(original, "Resources", Replace(resources, "Memory", new ResourceBudget(BudgetValueKind.Exact, 0, "bytes", QualificationDisposition.Mandatory))),
            Replace(original, "Resources", Replace(resources, "Memory", new ResourceBudget(BudgetValueKind.Unlimited, 0, "bytes", QualificationDisposition.PendingQualification))),
            Replace(original, "Invocation", new InvocationManifest("/tool", "work", "/input", new[] { "x", "x" }, new[] { KeyValuePair.Create("K", "") }, "/tmp")),
            Replace(original, "Closure", new ClosureIdentity(Array.Empty<ClosureObject>(), Array.Empty<string>())),
            Replace(original, "Authority", Replace(original.Authority, "WritableStores", new[] { new WritableStore("/tmp", 0, true, 0) })),
        };
        var golden = ManifestCodec.Encode(original);
        foreach (var variant in variants)
        {
            var bytes = RestrictedManifestCodec.Encode(new RestrictedManifestView(variant));
            Assert.False(golden.AsSpan().SequenceEqual(bytes));
            AssertGraph(variant, RestrictedManifestCodec.Decode(bytes).Value!);
        }
        Assert.Equal(variants.Length, variants.Select(m => Convert.ToHexString(ManifestCodec.ComputeDigestBytes(m))).Distinct().Count());
    }

    [Fact]
    public void InternalProjectionRejectsUndefinedEnumsAndInvalidUtf8()
    {
        var original = ManifestTests.SampleManifest();
        foreach (var budget in new[]
        {
            new ResourceBudget((BudgetValueKind)3, 0, "bytes", QualificationDisposition.Mandatory),
            new ResourceBudget(BudgetValueKind.Exact, 0, "bytes", (QualificationDisposition)2),
        })
        {
            var model = Replace(original, "Resources", Replace(original.Resources, "Memory", budget));
            Assert.Throws<ArgumentException>(() => new RestrictedManifestView(model));
            Assert.Equal(ManifestDecodeError.InvalidTag, RestrictedManifestCodec.Decode(ManifestCodec.Encode(model)).Error);
        }
        var bad = ManifestTests.SampleManifest(mode: "\ud800");
        Assert.ThrowsAny<ArgumentException>(() => new RestrictedManifestView(bad));
        var badCore = Replace(original, "Resources", Replace(original.Resources, "CoreDumps", new CoreDumpPolicy(true, false, "\ud800")));
        Assert.ThrowsAny<ArgumentException>(() => new RestrictedManifestView(badCore));
    }

    [Theory]
    [InlineData(16384)]
    [InlineData(16385)]
    public void ArgumentEnvironmentBoundUsesUtf8Bytes(int bytes)
    {
        var argument = new string('\u00e9', (bytes - 2) / 2) + ((bytes - 2) % 2 == 1 ? "x" : "");
        var environment = new[] { KeyValuePair.Create("K", "V") };
        if (bytes == 16385)
        {
            Assert.Throws<ArgumentException>(() => new InvocationManifest("/tool", "work", "/input", new[] { argument }, environment, "/tmp"));
            return;
        }
        var invocation = new InvocationManifest("/tool", "work", "/input", new[] { argument }, environment, "/tmp");
        var original = ManifestTests.SampleManifest();
        var model = Replace(original, "Invocation", invocation);
        var encoded = RestrictedManifestCodec.Encode(new RestrictedManifestView(model));
        AssertGraph(model, RestrictedManifestCodec.Decode(encoded).Value!);
        // Same canonical grammar, one additional argv byte, stays below the complete transport cap.
        var bad = AddArgumentByte(encoded, argument);
        Assert.Equal(ManifestDecodeError.InvalidValue, RestrictedManifestCodec.Decode(bad).Error);
    }

    [Fact]
    public void SetPermutationsAndSupplementaryOrderingUseOneEncoder()
    {
        var original = ManifestTests.SampleManifest();
        var a = original.Authority;
        var names = new[] { "\U00010000", "\ue000", "a" };
        var first = Replace(original, "Authority", Replace(a, "Namespaces", names));
        var second = Replace(original, "Authority", Replace(a, "Namespaces", names.Reverse().ToArray()));
        Assert.Equal(RestrictedManifestCodec.Encode(new RestrictedManifestView(first)), RestrictedManifestCodec.Encode(new RestrictedManifestView(second)));
        Assert.Equal(new[] { "a", "\ue000", "\U00010000" }, new RestrictedManifestView(first).Authority.Namespaces);
        Assert.Equal(ReferenceEncoder.Encode(first), RestrictedManifestCodec.Encode(new RestrictedManifestView(first)));
    }

    [Fact]
    public void CallerCollectionsAndDigestArraysAreCopiedBeforeProjection()
    {
        var original = ManifestTests.SampleManifest();
        var hash = ManifestTests.Bytes32(1);
        var namespaces = new[] { "a", "z" };
        var args = new[] { "/tool" };
        var environment = new[] { KeyValuePair.Create("K", "V") };
        var model = Replace(original, "Authority", Replace(Replace(original.Authority, "Namespaces", namespaces), "FilterSha256", hash));
        model = Replace(model, "Invocation", new InvocationManifest("/tool", "work", "/input", args, environment, "/tmp"));
        var view = new RestrictedManifestView(model);
        var before = RestrictedManifestCodec.Encode(view);
        Array.Fill(hash, (byte)255); namespaces[0] = "changed"; args[0] = "changed"; environment[0] = KeyValuePair.Create("changed", "changed");
        Assert.Equal(before, RestrictedManifestCodec.Encode(view));
        AssertGraph(model, view);
    }

    private static byte[] AddArgumentByte(byte[] bytes, string argument)
    {
        var text = System.Text.Encoding.UTF8.GetBytes(argument);
        var offset = bytes.AsSpan().IndexOf(text);
        Assert.True(offset > 4);
        var result = new byte[bytes.Length + 1];
        bytes.AsSpan(0, offset + text.Length).CopyTo(result);
        result[offset + text.Length] = (byte)'x';
        bytes.AsSpan(offset + text.Length).CopyTo(result.AsSpan(offset + text.Length + 1));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset - 4), (uint)(text.Length + 1));
        return result;
    }

    // Reconstruct through real validated model constructors, never alter get-only fields.
    private static T Replace<T>(T original, string property, object replacement) where T : notnull
    {
        var type = typeof(T);
        var constructor = type.GetConstructors().Single(c => c.GetParameters().Length == type.GetProperties().Length);
        var args = constructor.GetParameters().Select(p =>
        {
            var prop = type.GetProperties().Single(x => string.Equals(x.Name, p.Name, StringComparison.OrdinalIgnoreCase));
            if (prop.Name == property) return replacement;
            var value = prop.GetValue(original)!;
            return p.ParameterType == typeof(byte[]) ? ((IEnumerable<byte>)value).ToArray() : value;
        }).ToArray();
        return (T)constructor.Invoke(args);
    }

    private static void AssertGraph(object expected, object actual)
    {
        Assert.NotNull(actual);
        if (expected.GetType().IsEnum) { Assert.Equal(Convert.ToByte(expected), Convert.ToByte(actual)); return; }
        if (expected is string || expected.GetType().IsPrimitive) { Assert.Equal(expected, actual); return; }
        if (expected is IEnumerable items)
        {
            var left = items.Cast<object>().ToArray(); var right = ((IEnumerable)actual).Cast<object>().ToArray();
            Assert.Equal(left.Length, right.Length);
            for (var index = 0; index < left.Length; index++) AssertGraph(left[index], right[index]);
            return;
        }
        var expectedProperties = expected.GetType().GetProperties();
        var actualProperties = actual.GetType().GetProperties();
        Assert.Equal(expectedProperties.Select(p => p.Name).Order(), actualProperties.Select(p => p.Name).Order());
        foreach (var prop in expectedProperties)
        {
            var left = prop.GetValue(expected); var right = actual.GetType().GetProperty(prop.Name)!.GetValue(actual);
            if (left is null) Assert.Null(right);
            else AssertGraph(left, right!);
        }
    }
}
