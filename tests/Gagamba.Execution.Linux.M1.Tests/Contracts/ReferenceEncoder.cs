// G1-07 independent reference encoder (test-only): reimplements the WP01
// canonical manifest layout from the plan Appendix D field order using only
// public manifest state and its own byte-level primitives. It never calls
// ManifestCodec and never reuses Utf8BytewiseComparer; byte equality with
// the production codec on permuted and non-ASCII inputs proves the layout,
// framing, counts and sort order independently.
using System.Collections.Immutable;
using System.Text;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Tests.Contracts;

internal static class ReferenceEncoder
{
    private static readonly Encoding StrictUtf8 =
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    internal static byte[] Encode(RestrictedManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var output = new List<byte>(1024);
        WriteU16(output, manifest.SchemaVersion);
        WriteString(output, manifest.ImplementationId);
        WriteAuthority(output, manifest.Authority);
        WriteResources(output, manifest.Resources);
        WriteClosure(output, manifest.Closure);
        WriteInvocation(output, manifest.Invocation);
        WriteChannels(output, manifest.Channels);
        return output.ToArray();
    }

    internal static string DigestHex(RestrictedManifest manifest) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encode(manifest))).ToLowerInvariant();

    // Independent bytewise ordering over UTF-8 bytes with its own loop.
    internal static IComparer<string> BytewiseOrder { get; } = Comparer<string>.Create(static (x, y) =>
    {
        var xb = StrictUtf8.GetBytes(x);
        var yb = StrictUtf8.GetBytes(y);
        var shared = Math.Min(xb.Length, yb.Length);
        for (var i = 0; i < shared; i++)
        {
            var diff = xb[i].CompareTo(yb[i]);
            if (diff != 0)
            {
                return diff;
            }
        }

        return xb.Length.CompareTo(yb.Length);
    });

    private static void WriteU16(List<byte> output, ushort value)
    {
        output.Add((byte)(value & 0xFF));
        output.Add((byte)((value >> 8) & 0xFF));
    }

    private static void WriteU32(List<byte> output, uint value)
    {
        output.Add((byte)(value & 0xFF));
        output.Add((byte)((value >> 8) & 0xFF));
        output.Add((byte)((value >> 16) & 0xFF));
        output.Add((byte)((value >> 24) & 0xFF));
    }

    private static void WriteU64(List<byte> output, ulong value)
    {
        for (var shift = 0; shift < 64; shift += 8)
        {
            output.Add((byte)((value >> shift) & 0xFF));
        }
    }

    private static void WriteString(List<byte> output, string value)
    {
        var bytes = StrictUtf8.GetBytes(value);
        WriteU32(output, checked((uint)bytes.Length));
        output.AddRange(bytes);
    }

    private static void WriteRawDigest(List<byte> output, ImmutableArray<byte> digest)
    {
        if (digest.Length != 32)
        {
            throw new ArgumentException("Digests must be exactly 32 bytes.", nameof(digest));
        }

        foreach (var b in digest)
        {
            output.Add(b);
        }
    }

    private static void WriteBudget(List<byte> output, ResourceBudget budget)
    {
        output.Add((byte)budget.Kind);
        WriteU64(output, budget.Value);
        WriteString(output, budget.Unit);
        output.Add((byte)budget.Qualification);
    }

    private static void WriteAuthority(List<byte> output, AuthorityManifest authority)
    {
        WriteU32(output, checked((uint)authority.Namespaces.Length));
        foreach (var name in authority.Namespaces)
        {
            WriteString(output, name);
        }

        WriteU32(output, checked((uint)authority.Capabilities.Length));
        foreach (var capability in authority.Capabilities)
        {
            WriteString(output, capability);
        }

        output.Add(authority.NoNewPrivs ? (byte)1 : (byte)0);
        WriteRawDigest(output, authority.FilterSha256);
        WriteU64(output, authority.FilterLength);
        WriteU32(output, checked((uint)authority.ReadOnlyDestinations.Length));
        foreach (var destination in authority.ReadOnlyDestinations)
        {
            WriteString(output, destination);
        }

        WriteU32(output, checked((uint)authority.WritableStores.Length));
        foreach (var store in authority.WritableStores)
        {
            WriteString(output, store.Path);
            WriteU64(output, store.Bytes);
            output.Add(store.HasInodeBudget ? (byte)1 : (byte)0);
            WriteU64(output, store.InodeBudget);
        }

        output.Add(authority.NoAmbientEnvironment ? (byte)1 : (byte)0);
        output.Add(authority.NoIpcEndpoints ? (byte)1 : (byte)0);
        output.Add(authority.NoExtraInheritedFds ? (byte)1 : (byte)0);
        output.Add(authority.MountPolicy.ToolExecutable ? (byte)1 : (byte)0);
        output.Add(authority.MountPolicy.ScratchNoexec ? (byte)1 : (byte)0);
        output.Add((byte)authority.NetworkQualification);
    }

    private static void WriteResources(List<byte> output, ResourceManifest resources)
    {
        WriteBudget(output, resources.Memory);
        WriteBudget(output, resources.Swap);
        WriteBudget(output, resources.Tasks);
        WriteBudget(output, resources.FileDescriptors);
        WriteBudget(output, resources.TmpBytes);
        WriteBudget(output, resources.ShmBytes);
        WriteBudget(output, resources.StdoutBytes);
        WriteBudget(output, resources.StderrBytes);
        WriteBudget(output, resources.CaptureAggregateBytes);
        WriteBudget(output, resources.SetupTimeoutNs);
        WriteBudget(output, resources.OverallTimeoutNs);
        WriteBudget(output, resources.StopAllowanceNs);
        WriteBudget(output, resources.FinalDrainNs);
        output.Add((byte)resources.InodeQualification);
        output.Add(resources.CoreDumps.CoreDisabled ? (byte)1 : (byte)0);
        output.Add(resources.CoreDumps.RuntimeDumpsDisabled ? (byte)1 : (byte)0);
        output.Add(resources.CoreDumps.CorePatternObserved is null ? (byte)0 : (byte)1);
        if (resources.CoreDumps.CorePatternObserved is not null)
        {
            WriteString(output, resources.CoreDumps.CorePatternObserved);
        }

        WriteU64(output, resources.OwnerAllocation.StagingBytes);
        WriteU32(output, resources.OwnerAllocation.StagingObjects);
        WriteU64(output, resources.OwnerAllocation.ConfigBytes);
        WriteU64(output, resources.OwnerAllocation.CoordinatorStatusBytes);
        WriteU64(output, resources.OwnerAllocation.HelperStatusBytes);
        WriteU64(output, resources.OwnerAllocation.JournalBytes);
        WriteU64(output, resources.OwnerAllocation.ReceiptBytes);
    }

    private static void WriteClosure(List<byte> output, ClosureIdentity closure)
    {
        WriteU32(output, checked((uint)closure.Objects.Length));
        foreach (var bound in closure.Objects)
        {
            WriteString(output, bound.Role);
            WriteRawDigest(output, bound.Sha256);
            WriteU64(output, bound.Length);
            WriteString(output, bound.Destination);
            WriteString(output, bound.BuildIdentity);
        }

        WriteU32(output, checked((uint)closure.HelperDependencies.Length));
        foreach (var dependency in closure.HelperDependencies)
        {
            WriteString(output, dependency);
        }
    }

    private static void WriteInvocation(List<byte> output, InvocationManifest invocation)
    {
        WriteString(output, invocation.ToolPath);
        WriteString(output, invocation.Mode);
        WriteString(output, invocation.InputPath);
        WriteU32(output, checked((uint)invocation.Argv.Length));
        foreach (var argument in invocation.Argv)
        {
            WriteString(output, argument);
        }

        WriteU32(output, checked((uint)invocation.Environment.Length));
        foreach (var pair in invocation.Environment)
        {
            WriteString(output, pair.Key);
            WriteString(output, pair.Value);
        }

        WriteString(output, invocation.WorkDirectory);
    }

    private static void WriteChannels(List<byte> output, ChannelManifest channels)
    {
        output.Add(channels.StdinEof ? (byte)1 : (byte)0);
        WriteU64(output, channels.StdoutLimitBytes);
        WriteU64(output, channels.StderrLimitBytes);
        WriteU64(output, channels.AggregateLimitBytes);
    }
}
