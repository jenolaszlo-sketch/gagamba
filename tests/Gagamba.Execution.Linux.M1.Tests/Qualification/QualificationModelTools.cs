// RC2A-02A qualification model tools (test-only): baseline factory aliases,
// constructor-reconstruction mutation, byte helpers and a reflection graph
// comparer. The graph comparer is intentionally equivalent to the one already
// accepted in RestrictedPublicProjectionTests; mutation always goes through the
// real validating model constructors, never through field or state mutation.
using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Contracts;
using Gagamba.Execution.Linux.M1.Tests.Contracts;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

internal static class QualificationModelTools
{
    internal static RestrictedManifest Baseline() => ManifestTests.SampleManifest();

    internal static RestrictedManifest Model(string mode = "work", IEnumerable<KeyValuePair<string, string>>? environment = null) =>
        ManifestTests.SampleManifest(mode: mode, environment: environment);

    internal static byte[] Bytes32(byte start) => ManifestTests.Bytes32(start);

    internal static byte[] GoldenBytes() => Convert.FromHexString(QualificationGolden.Hex);

    internal static byte[] ProductionEncode(RestrictedManifest model) =>
        RestrictedManifestCodec.Encode(new RestrictedManifestView(model));

    internal static byte[] ReferenceEncode(RestrictedManifest model) => ReferenceEncoder.Encode(model);

    internal static string ProductionDigestHex(RestrictedManifest model) =>
        Convert.ToHexString(ManifestCodec.ComputeDigestBytes(model)).ToLowerInvariant();

    internal static string ReferenceDigestHex(RestrictedManifest model) => ReferenceEncoder.DigestHex(model);

    /// <summary>Reconstructs T through its real validating constructor with one
    /// property replaced. Mirrors the accepted projection-test helper.</summary>
    internal static T Replace<T>(T original, string property, object replacement) where T : notnull
    {
        var type = typeof(T);
        var constructor = type.GetConstructors().Single(c => c.GetParameters().Length == type.GetProperties().Length);
        var args = constructor.GetParameters().Select(p =>
        {
            var prop = type.GetProperties().Single(x => string.Equals(x.Name, p.Name, StringComparison.OrdinalIgnoreCase));
            if (prop.Name == property)
            {
                return replacement;
            }

            var value = prop.GetValue(original)!;
            return p.ParameterType == typeof(byte[]) ? ((IEnumerable<byte>)value).ToArray() : value;
        }).ToArray();
        return (T)constructor.Invoke(args);
    }

    internal static RestrictedManifest WithAuthority(RestrictedManifest model, string property, object replacement) =>
        Replace(model, "Authority", Replace(model.Authority, property, replacement));

    internal static RestrictedManifest WithResources(RestrictedManifest model, string property, object replacement) =>
        Replace(model, "Resources", Replace(model.Resources, property, replacement));

    internal static RestrictedManifest WithInvocation(RestrictedManifest model, string property, object replacement) =>
        Replace(model, "Invocation", Replace(model.Invocation, property, replacement));

    internal static RestrictedManifest WithClosure(RestrictedManifest model, string property, object replacement) =>
        Replace(model, "Closure", Replace(model.Closure, property, replacement));

    internal static RestrictedManifest WithChannels(RestrictedManifest model, string property, object replacement) =>
        Replace(model, "Channels", Replace(model.Channels, property, replacement));

    internal static RestrictedManifest WithBudget(RestrictedManifest model, string budgetName, ResourceBudget replacement) =>
        WithResources(model, budgetName, replacement);

    /// <summary>Structural comparison used by the accepted projection tests: enum
    /// bytes, primitives/strings, enumerables (ordered, including pairs) and
    /// public property inventories by name.</summary>
    internal static void AssertGraph(object expected, object actual)
    {
        Xunit.Assert.NotNull(actual);
        if (expected.GetType().IsEnum)
        {
            Xunit.Assert.Equal(Convert.ToByte(expected), Convert.ToByte(actual));
            return;
        }

        if (expected is string || expected.GetType().IsPrimitive)
        {
            Xunit.Assert.Equal(expected, actual);
            return;
        }

        if (expected is IEnumerable items)
        {
            var left = items.Cast<object>().ToArray();
            var right = ((IEnumerable)actual).Cast<object>().ToArray();
            Xunit.Assert.Equal(left.Length, right.Length);
            for (var index = 0; index < left.Length; index++)
            {
                AssertGraph(left[index], right[index]);
            }

            return;
        }

        var expectedProperties = expected.GetType().GetProperties();
        var actualProperties = actual.GetType().GetProperties();
        Xunit.Assert.Equal(expectedProperties.Select(p => p.Name).Order(), actualProperties.Select(p => p.Name).Order());
        foreach (var prop in expectedProperties)
        {
            var left = prop.GetValue(expected);
            var right = actual.GetType().GetProperty(prop.Name)!.GetValue(actual);
            if (left is null)
            {
                Xunit.Assert.Null(right);
            }
            else
            {
                AssertGraph(left, right!);
            }
        }
    }

    internal static byte[] Diffusion32(byte seed)
    {
        var bytes = new byte[32];
        for (var i = 0; i < 32; i++)
        {
            bytes[i] = (byte)(seed ^ (i * 7 + 0x5A));
        }

        return bytes;
    }

    internal static ImmutableArray<byte> Immutable(byte[] bytes) => ImmutableArray.Create(bytes);
}
