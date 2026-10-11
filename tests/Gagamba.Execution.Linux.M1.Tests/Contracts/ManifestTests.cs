// WP01 contract tests: canonical encoding, digest stability and
// fail-closed manifest construction. Portable pure logic.
using System.Collections.Immutable;
using System.Text;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Contracts;

public sealed class ManifestTests
{
    internal static byte[] Bytes32(byte start)
    {
        var bytes = new byte[32];
        for (var i = 0; i < 32; i++)
        {
            bytes[i] = (byte)(start + i);
        }

        return bytes;
    }

    internal static RestrictedManifest SampleManifest(
        string mode = "work",
        IEnumerable<KeyValuePair<string, string>>? environment = null) =>
        new(
            RestrictedManifest.CurrentSchemaVersion,
            RestrictedManifest.ImplementationIdentifier,
            new AuthorityManifest(
                new[] { "uts", "net", "mount", "ipc", "pid", "user" },
                Array.Empty<string>(),
                true,
                Bytes32(0x01),
                312,
                new[] { "/tool", "/input", "/coordinator" },
                new[]
                {
                    new WritableStore("/dev/shm", 1048576, false, 0),
                    new WritableStore("/tmp", 7340032, false, 0),
                },
                true,
                true,
                true),
            new ResourceManifest(
                new ResourceBudget(BudgetValueKind.Exact, 134217728, "bytes", QualificationDisposition.Mandatory),
                new ResourceBudget(BudgetValueKind.Exact, 0, "bytes", QualificationDisposition.Mandatory),
                new ResourceBudget(BudgetValueKind.Exact, 48, "tasks", QualificationDisposition.Mandatory),
                new ResourceBudget(BudgetValueKind.Exact, 64, "fds", QualificationDisposition.Mandatory),
                new ResourceBudget(BudgetValueKind.Exact, 7340032, "bytes", QualificationDisposition.Mandatory),
                new ResourceBudget(BudgetValueKind.Exact, 1048576, "bytes", QualificationDisposition.Mandatory),
                new ResourceBudget(BudgetValueKind.Exact, 65536, "bytes", QualificationDisposition.Mandatory),
                new ResourceBudget(BudgetValueKind.Exact, 65536, "bytes", QualificationDisposition.Mandatory),
                new ResourceBudget(BudgetValueKind.Exact, 131072, "bytes", QualificationDisposition.Mandatory),
                new ResourceBudget(BudgetValueKind.Exact, 10000000000, "ns", QualificationDisposition.Mandatory),
                new ResourceBudget(BudgetValueKind.Exact, 30000000000, "ns", QualificationDisposition.Mandatory),
                new ResourceBudget(BudgetValueKind.Exact, 5000000000, "ns", QualificationDisposition.Mandatory),
                new ResourceBudget(BudgetValueKind.Exact, 1000000000, "ns", QualificationDisposition.Mandatory),
                QualificationDisposition.PendingQualification),
            new ClosureIdentity(
                new[]
                {
                    new ClosureObject("tool", Bytes32(0x10), 100, "/tool", "gcc-static"),
                    new ClosureObject("app", Bytes32(0x20), 200, "/app", "dotnet-publish"),
                    new ClosureObject("input", Bytes32(0x30), 300, "/input", "copyin"),
                },
                Array.Empty<string>()),
            new InvocationManifest(
                "/tool",
                mode,
                "/input",
                new[] { "--in", "/input" },
                environment ?? new[] { new KeyValuePair<string, string>("LANG", "C"), new KeyValuePair<string, string>("HOME", "/tmp") },
                "/tmp"),
            new ChannelManifest(true, 65536, 65536, 131072));

    [Fact]
    public void DigestIsStableAcrossEquivalentConstructions()
    {
        Assert.Equal(
            ManifestCodec.ComputeDigestHex(SampleManifest()),
            ManifestCodec.ComputeDigestHex(SampleManifest()));
    }

    [Fact]
    public void MutatingSourceArraysCannotAlterDigest()
    {
        var sha = Bytes32(0x10);
        var manifest = SampleManifest();
        var before = ManifestCodec.ComputeDigestHex(manifest);
        for (var i = 0; i < sha.Length; i++)
        {
            sha[i] = 0xFF;
        }

        Assert.Equal(before, ManifestCodec.ComputeDigestHex(manifest));
        Assert.Equal(0x10, manifest.Closure.Objects[2].Sha256[0]);
        Assert.Equal(0x30, manifest.Closure.Objects[1].Sha256[0]);
    }

    [Fact]
    public void CanonicalGoldenBytes()
    {
        var bytes = ManifestCodec.Encode(SampleManifest());
        Assert.Equal(Convert.FromHexString(GoldenHex), bytes);
        Assert.Equal(GoldenDigestHex, ManifestCodec.ComputeDigestHex(SampleManifest()));
    }

    [Fact]
    public void AbsentZeroUnlimitedAndPendingAreDistinct()
    {
        var absent = new ResourceBudget(BudgetValueKind.Absent, 0, "bytes", QualificationDisposition.Mandatory);
        var zero = new ResourceBudget(BudgetValueKind.Exact, 0, "bytes", QualificationDisposition.Mandatory);
        var unlimited = new ResourceBudget(BudgetValueKind.Unlimited, 0, "bytes", QualificationDisposition.Mandatory);
        var pending = new ResourceBudget(BudgetValueKind.Exact, 48, "tasks", QualificationDisposition.PendingQualification);
        Assert.NotEqual(absent, zero);
        Assert.NotEqual(absent, unlimited);
        Assert.NotEqual(zero, unlimited);
        Assert.NotEqual(zero, pending);
        Assert.Throws<ArgumentException>(() => new ResourceBudget(BudgetValueKind.Absent, 1, "bytes", QualificationDisposition.Mandatory));
        Assert.Throws<ArgumentException>(() => new ResourceBudget(BudgetValueKind.Unlimited, 7, "bytes", QualificationDisposition.Mandatory));
    }

    [Fact]
    public void DuplicateKeysRolesNamespacesAndStoresAreRejected()
    {
        Assert.Throws<ArgumentException>(() => SampleManifest(environment: new[]
        {
            new KeyValuePair<string, string>("HOME", "/tmp"),
            new KeyValuePair<string, string>("HOME", "/x"),
        }));
        Assert.Throws<ArgumentException>(() => new ClosureIdentity(
            new[]
            {
                new ClosureObject("tool", Bytes32(0x10), 1, "/tool", "b"),
                new ClosureObject("tool", Bytes32(0x11), 1, "/tool2", "b"),
            },
            Array.Empty<string>()));
        Assert.Throws<ArgumentException>(() => new AuthorityManifest(
            new[] { "user", "user" },
            Array.Empty<string>(),
            true,
            Bytes32(0x01),
            1,
            Array.Empty<string>(),
            Array.Empty<WritableStore>(),
            true,
            true,
            true));
        Assert.Throws<ArgumentException>(() => new AuthorityManifest(
            new[] { "user" },
            Array.Empty<string>(),
            true,
            Bytes32(0x01),
            1,
            Array.Empty<string>(),
            new[] { new WritableStore("/tmp", 1, false, 0), new WritableStore("/tmp", 2, false, 0) },
            true,
            true,
            true));
    }

    [Fact]
    public void InvalidDigestsAndIdentitiesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new ClosureObject("tool", new byte[31], 1, "/tool", "b"));
        Assert.Throws<ArgumentException>(() => new ClosureObject("tool", new byte[33], 1, "/tool", "b"));
        Assert.Throws<ArgumentException>(() => new PreparationIdentity(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new byte[31]));
        Assert.Throws<ArgumentException>(() => new PreparationIdentity(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Bytes32(0x01)));
        Assert.Throws<ArgumentException>(() => new RestrictedManifest(2, RestrictedManifest.ImplementationIdentifier,
            SampleManifest().Authority, SampleManifest().Resources, SampleManifest().Closure,
            SampleManifest().Invocation, SampleManifest().Channels));
        Assert.Throws<ArgumentException>(() => new RestrictedManifest(1, "other-profile",
            SampleManifest().Authority, SampleManifest().Resources, SampleManifest().Closure,
            SampleManifest().Invocation, SampleManifest().Channels));
        Assert.Throws<ArgumentException>(() => new AuthorityManifest(
            new[] { "user" }, new[] { "cap" }, true, Bytes32(0x01), 1,
            Array.Empty<string>(), Array.Empty<WritableStore>(), true, true, true));
        Assert.Throws<ArgumentException>(() => new AuthorityManifest(
            new[] { "user" }, Array.Empty<string>(), true, Bytes32(0x01), 1,
            Array.Empty<string>(), Array.Empty<WritableStore>(), false, true, true));
        Assert.Throws<ArgumentException>(() => new InvocationManifest("/tool", "m", "/elsewhere",
            Array.Empty<string>(), Array.Empty<KeyValuePair<string, string>>(), "/tmp"));
        Assert.Throws<ArgumentException>(() => new ChannelManifest(false, 1, 1, 2));
        Assert.Throws<ArgumentException>(() => new ResourceManifest(
            new ResourceBudget(BudgetValueKind.Exact, 1, "bytes", QualificationDisposition.Mandatory),
            new ResourceBudget(BudgetValueKind.Exact, 0, "bytes", QualificationDisposition.Mandatory),
            new ResourceBudget(BudgetValueKind.Exact, 1, "tasks", QualificationDisposition.Mandatory),
            new ResourceBudget(BudgetValueKind.Exact, 1, "fds", QualificationDisposition.Mandatory),
            new ResourceBudget(BudgetValueKind.Exact, 1, "bytes", QualificationDisposition.Mandatory),
            new ResourceBudget(BudgetValueKind.Exact, 1, "bytes", QualificationDisposition.Mandatory),
            new ResourceBudget(BudgetValueKind.Exact, 1, "bytes", QualificationDisposition.Mandatory),
            new ResourceBudget(BudgetValueKind.Exact, 1, "bytes", QualificationDisposition.Mandatory),
            new ResourceBudget(BudgetValueKind.Exact, 1, "bytes", QualificationDisposition.Mandatory),
            new ResourceBudget(BudgetValueKind.Exact, 1, "ns", QualificationDisposition.Mandatory),
            new ResourceBudget(BudgetValueKind.Exact, 1, "ns", QualificationDisposition.Mandatory),
            new ResourceBudget(BudgetValueKind.Exact, 1, "ns", QualificationDisposition.Mandatory),
            new ResourceBudget(BudgetValueKind.Exact, 1, "ns", QualificationDisposition.Mandatory),
            QualificationDisposition.Mandatory));
    }

    [Fact]
    public void CollectionsAreStoredSorted()
    {
        var manifest = SampleManifest();
        Assert.Equal(new[] { "ipc", "mount", "net", "pid", "user", "uts" }, manifest.Authority.Namespaces.ToArray());
        Assert.Equal(new[] { "app", "input", "tool" }, manifest.Closure.Objects.Select(o => o.Role).ToArray());
        Assert.Equal(new[] { "HOME", "LANG" }, manifest.Invocation.Environment.Select(kv => kv.Key).ToArray());
        Assert.Equal(new[] { "/dev/shm", "/tmp" }, manifest.Authority.WritableStores.Select(s => s.Path).ToArray());
    }

    [Fact]
    public void NonAsciiKeysSortByUtf8Bytes()
    {
        var manifest = SampleManifest(environment: new[]
        {
            new KeyValuePair<string, string>("z", "1"),
            new KeyValuePair<string, string>("é", "2"),
            new KeyValuePair<string, string>("ß", "3"),
            new KeyValuePair<string, string>("a", "4"),
        });
        Assert.Equal(new[] { "a", "z", "ß", "é" }, manifest.Invocation.Environment.Select(kv => kv.Key).ToArray());
    }

    [Fact]
    public void StrictUtf8RejectsLoneSurrogates()
    {
        Assert.Throws<EncoderFallbackException>(() => ManifestCodec.Utf8ByteCount("\uD800"));
    }

    [Fact]
    public void GuidRfcByteOrderVector()
    {
        Assert.Equal(
            new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF },
            ManifestCodec.GuidToRfc4122Bytes(new Guid("00112233-4455-6677-8899-aabbccddeeff")));
    }

    [Fact]
    public void EnumNumbersArePinned()
    {
        Assert.Equal(0, (int)QualificationDisposition.Mandatory);
        Assert.Equal(1, (int)QualificationDisposition.PendingQualification);
        Assert.Equal(0, (int)BudgetValueKind.Absent);
        Assert.Equal(1, (int)BudgetValueKind.Exact);
        Assert.Equal(2, (int)BudgetValueKind.Unlimited);
        Assert.Equal(0, (int)ReleaseDisposition.NotCommitted);
        Assert.Equal(1, (int)ReleaseDisposition.Committed);
        Assert.Equal(2, (int)ReleaseDisposition.MayHaveExecuted);
        Assert.Equal(0, (int)ExecDisposition.NotAttempted);
        Assert.Equal(1, (int)ExecDisposition.FailedBeforeExec);
        Assert.Equal(2, (int)ExecDisposition.Confirmed);
        Assert.Equal(3, (int)ExecDisposition.Unknown);
        Assert.Equal(0, (int)RootResultKind.Unknown);
        Assert.Equal(1, (int)RootResultKind.NotStarted);
        Assert.Equal(2, (int)RootResultKind.Exited);
        Assert.Equal(3, (int)RootResultKind.Signalled);
        Assert.Equal(0, (int)HelperResultKind.Unknown);
        Assert.Equal(1, (int)HelperResultKind.NotStarted);
        Assert.Equal(2, (int)HelperResultKind.Exited);
        Assert.Equal(3, (int)HelperResultKind.Signalled);
        Assert.Equal(0, (int)ExecutionOutcome.Unsupported);
        Assert.Equal(1, (int)ExecutionOutcome.FailedToEstablish);
        Assert.Equal(2, (int)ExecutionOutcome.CancelledBeforeRelease);
        Assert.Equal(3, (int)ExecutionOutcome.Succeeded);
        Assert.Equal(4, (int)ExecutionOutcome.TargetFailed);
        Assert.Equal(5, (int)ExecutionOutcome.Stopped);
        Assert.Equal(6, (int)ExecutionOutcome.Unknown);
        Assert.Equal(0, (int)StopCauseKind.CallerStop);
        Assert.Equal(1, (int)StopCauseKind.CallerLoss);
        Assert.Equal(2, (int)StopCauseKind.SetupExpired);
        Assert.Equal(3, (int)StopCauseKind.SetupFault);
        Assert.Equal(4, (int)StopCauseKind.OutputOverflow);
        Assert.Equal(5, (int)StopCauseKind.OutputReadFailure);
        Assert.Equal(6, (int)StopCauseKind.RecoveryTakeover);
        Assert.Equal(7, (int)StopCauseKind.TransportFailure);
        Assert.Equal(0, (int)FinalDrainDisposition.Complete);
        Assert.Equal(1, (int)FinalDrainDisposition.Incomplete);
        Assert.Equal(2, (int)FinalDrainDisposition.Expired);
        Assert.Equal(0, (int)CleanupState.Confirmed);
        Assert.Equal(1, (int)CleanupState.Failed);
        Assert.Equal(2, (int)CleanupState.Unknown);
        Assert.Equal(0, (int)TransactionState.Preparing);
        Assert.Equal(1, (int)TransactionState.Prepared);
        Assert.Equal(2, (int)TransactionState.Owned);
        Assert.Equal(3, (int)TransactionState.ReleaseCommitted);
        Assert.Equal(4, (int)TransactionState.Running);
        Assert.Equal(5, (int)TransactionState.Stopping);
        Assert.Equal(6, (int)TransactionState.Completed);
        Assert.Equal(7, (int)TransactionState.Failed);
        Assert.Equal(8, (int)TransactionState.Cancelled);
        Assert.Equal(9, (int)TransactionState.Unknown);
        Assert.Equal(0, (int)InvariantViolation.None);
        Assert.Equal(1, (int)InvariantViolation.InvalidStateOrder);
        Assert.Equal(2, (int)InvariantViolation.Expired);
        Assert.Equal(3, (int)InvariantViolation.Foreign);
        Assert.Equal(4, (int)InvariantViolation.Consumed);
        Assert.Equal(5, (int)InvariantViolation.Aborted);
        Assert.Equal(6, (int)InvariantViolation.StoppedFirst);
        Assert.Equal(7, (int)InvariantViolation.TransportFailed);
    }

    private const string GoldenHex = "01001b0000006c696e75782d6d312d6e61746976652d7472616e73616374696f6e0600000003000000697063050000006d6f756e74030000006e65740300000070696404000000757365720300000075747300000000010102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f203801000000000000030000000c0000002f636f6f7264696e61746f72060000002f696e707574050000002f746f6f6c02000000080000002f6465762f73686d0000100000000000000000000000000000040000002f746d7000007000000000000000000000000000000101010100000008000000000500000062797465730001000000000000000005000000627974657300013000000000000000050000007461736b7300014000000000000000030000006664730001000070000000000005000000627974657300010000100000000000050000006279746573000100000100000000000500000062797465730001000001000000000005000000627974657300010000020000000000050000006279746573000100e40b5402000000020000006e73000100ac23fc06000000020000006e73000100f2052a01000000020000006e73000100ca9a3b00000000020000006e7300010300000003000000617070202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3fc800000000000000040000002f6170700e000000646f746e65742d7075626c69736805000000696e707574303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f2c01000000000000060000002f696e70757406000000636f7079696e04000000746f6f6c101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f6400000000000000050000002f746f6f6c0a0000006763632d73746174696300000000050000002f746f6f6c04000000776f726b060000002f696e70757402000000040000002d2d696e060000002f696e7075740200000004000000484f4d45040000002f746d70040000004c414e470100000043040000002f746d7001000001000000000000000100000000000000020000000000";
    private const string GoldenDigestHex = "dee322bcada586327661c5bcbc1c9673b8837e28354838ff9a53cfef01f22530";
}
