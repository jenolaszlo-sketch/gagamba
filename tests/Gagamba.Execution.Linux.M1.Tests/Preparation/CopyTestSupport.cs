// WP02 portable test support: standard Section 7 budgets, plan builders
// and manifest/store helpers shared by the copy/bind/publish tests.
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Restricted;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests.Preparation;

internal static class CopyTestSupport
{
    internal const ulong ToolBudget = 4UL * 1024 * 1024;
    internal const ulong InputBudget = 256UL * 1024;
    internal const ulong TotalBudget = 16UL * 1024 * 1024;
    internal const int MaxObjects = 128;

    internal const string SourceRoot = "/fake/source";
    internal const string StagingParent = "/fake/staging";
    internal const string StagingDir = "prep-001";

    internal sealed class TestClock : ITransactionClock
    {
        public long NowNanoseconds { get; set; }
    }

    internal static readonly Guid Issuer = new("11111111-1111-1111-1111-111111111111");
    internal static readonly Guid Generation = new("22222222-2222-2222-2222-222222222222");

    internal static FakeCopyHost NewHost()
    {
        var host = new FakeCopyHost
        {
            RootPath = SourceRoot,
            ParentPath = StagingParent,
        };
        host.OpenDirectory(SourceRoot, out var root);
        root?.Dispose();
        host.OpenDirectory(StagingParent, out var parent);
        parent?.Dispose();
        return host;
    }

    internal static CopyPlan Plan(
        FakeCopyHost host,
        params CopyObjectPlan[] objects) =>
        new(
            SourceRoot,
            StagingParent,
            StagingDir,
            objects,
            TotalBudget,
            MaxObjects,
            Array.Empty<KeyValuePair<string, string>>(),
            _ => true,
            null);

    internal static CopyObjectPlan Tool(string name = "tool", ulong max = ToolBudget, byte[]? claimed = null) =>
        new("tool", name, max, claimed);

    internal static CopyObjectPlan Input(string name = "input", ulong max = InputBudget, byte[]? claimed = null) =>
        new("input", name, max, claimed);

    internal static byte[] Bytes(byte start, int length)
    {
        var bytes = new byte[length];
        for (var i = 0; i < length; i++)
        {
            bytes[i] = (byte)(start + i);
        }

        return bytes;
    }

    internal static byte[] Digest(byte[] content)
    {
        using var hash = System.Security.Cryptography.SHA256.Create();
        return hash.ComputeHash(content);
    }

    internal static RestrictedManifest ManifestWithClosure(IReadOnlyList<StagedObject> staged)
    {
        var baseline = SampleManifest();
        var objects = staged
            .Select(s => new ClosureObject(s.Role, (byte[])s.Sha256.Clone(), s.Length, "/" + s.Role, "test-build"))
            .ToArray();
        var closure = new ClosureIdentity(
            objects,
            Array.Empty<string>());
        return new RestrictedManifest(
            RestrictedManifest.CurrentSchemaVersion,
            RestrictedManifest.ImplementationIdentifier,
            baseline.Authority,
            baseline.Resources,
            closure,
            baseline.Invocation,
            baseline.Channels);
    }

    internal static PreparedTransaction.PreparationStore NewStore(TestClock clock) =>
        new(Issuer, Generation, clock);

    internal static PreparationIdentity IdentityFor(RestrictedManifest manifest) =>
        new(
            new Guid("33333333-3333-3333-3333-333333333333"),
            Issuer,
            Generation,
            new Guid("44444444-4444-4444-4444-444444444444"),
            ManifestCodec.ComputeDigestBytes(manifest));
}
