// D5 test-only native FD5 qualification. The native qualification coordinator
// is launched as a real child with inherited release/status pipes; status bytes
// are read from the actual read descriptor, not from a synthetic queue.
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Linux.M1.Tests.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.D4;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[Trait("Category", "ProcessGlobalReaping")]
public sealed class Wp05D5NativeStatusTests
{
    private static readonly Guid Execution = new("aaaaaaaa-0000-0000-0000-0000000000d5");
    private static readonly Guid Preparation = new("dddddddd-0000-0000-0000-0000000000d6");
    private static readonly Guid Generation = new("cccccccc-0000-0000-0000-0000000000d7");
    private static readonly byte[] Digest = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
    private const long OverallDeadline = long.MaxValue / 2;
    private static readonly string Artifact = "artifacts/linux-m1/wp05-d5/wp05-qualification-coordinator";

    [Fact]
    public void D5_S01_ExecConfirmedIsNativeFd5Observation()
    {
        SkipUnlessQualified();
        var run = Run("exec_confirmed", 0);
        Assert.Equal(0, run.ExitCode);
        var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var observation, out var error), error);
        Assert.Equal(ExecTag.Confirmed, observation!.Exec!.Tag);
        Assert.Null(observation.Root);
        Assert.True(run.Source!.RawBytes > 0, "native status source produced no bytes");
    }

    [Fact]
    public void D5_S02_ExecFailedIsNativeFd5Observation()
    {
        SkipUnlessQualified();
        var run = Run("exec_failed", 2);
        var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var observation, out var error), error);
        Assert.Equal(ExecTag.FailedBeforeExec, observation!.Exec!.Tag);
        Assert.Equal(2, observation.Exec.Errno);
        Assert.Null(observation.Root);
    }

    [Fact]
    public void D5_S03_ExecConfirmedThenRootIsOrdered()
    {
        SkipUnlessQualified();
        var run = Run("exec_confirmed_root", 7 << 8);
        var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var exec, out var error), error);
        Assert.Equal(ExecTag.Confirmed, exec!.Exec!.Tag);
        Assert.Null(exec.Root);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var root, out error), error);
        Assert.Equal(RootKind.Exited, root!.Root!.Kind);
        Assert.Equal(7, root.Root.ExitCode);
        Assert.Equal(ExecTag.Confirmed, root.Exec!.Tag);
    }

    [Fact]
    public void D5_S04_UnconfirmedRootRemainsUnknown()
    {
        SkipUnlessQualified();
        var run = Run("root_unconfirmed", 7);
        var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var observation, out var error), error);
        Assert.Equal(ExecTag.Unknown, observation!.Exec!.Tag);
        Assert.Equal(RootKind.Unknown, observation.Root!.Kind);
        Assert.True(observation.Root.RawWaitPresent);
    }

    [Fact]
    public void D5_S05_ExecFailedThenRootNotStarted()
    {
        SkipUnlessQualified();
        var run = Run("exec_failed_root", 2);
        var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var exec, out var error), error);
        Assert.Equal(ExecTag.FailedBeforeExec, exec!.Exec!.Tag);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var root, out error), error);
        Assert.Equal(RootKind.NotStarted, root!.Root!.Kind);
        Assert.True(root.Root.RawWaitPresent);
    }

    [Fact]
    public void D5_S06_ProtocolFailedIsTerminal()
    {
        SkipUnlessQualified();
        var run = Run("protocol_failed", 30);
        var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var observation, out var error), error);
        Assert.True(observation!.ProtocolFailed);
        Assert.Equal(30u, observation.FailureCode);
        Assert.Null(observation.Exec);
        Assert.False(reader.TryReadNext(Monotonic() + 2_000_000_000L, out _, out _));
    }

    [Theory]
    [InlineData("d5_wrong_exec")]
    [InlineData("d5_wrong_prep")]
    [InlineData("d5_wrong_digest")]
    [InlineData("d5_wrong_sequence")]
    [InlineData("d5_replayed_sequence")]
    [InlineData("d5_duplicate_exec")]
    [InlineData("d5_root_after_terminal")]
    [InlineData("d5_contradictory")]
    [InlineData("d5_wrong_magic")]
    [InlineData("d5_wrong_version")]
    [InlineData("d5_short_header")]
    [InlineData("d5_truncated_payload")]
    [InlineData("d5_length_below_header")]
    [InlineData("d5_length_above_max")]
    [InlineData("d5_invalid_kind")]
    [InlineData("d5_invalid_payload_length")]
    [InlineData("d5_premature_eof")]
    public void D5_NativeIdentityAndFrameNegativesAreRejected(string mode)
    {
        SkipUnlessQualified();
        var run = Run(mode, 2);
        var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
        var accepted = reader.TryReadNext(Monotonic() + 2_000_000_000L, out var first, out var firstError);
        Assert.True(run.Source!.RawBytes > 0 || mode is "d5_premature_eof", "native status source produced no bytes");
        switch (mode)
        {
            case "d5_replayed_sequence":
            case "d5_duplicate_exec":
            case "d5_root_after_terminal":
            case "d5_contradictory":
                Assert.True(accepted, firstError);
                Assert.False(reader.TryReadNext(Monotonic() + 2_000_000_000L, out _, out _));
                break;
            default:
                Assert.False(accepted, "invalid native status was accepted");
                Assert.Null(first);
                break;
        }
    }

    [Fact]
    public void D5_N19_ExtraTrailingBytesDoNotBecomeASecondObservation()
    {
        SkipUnlessQualified();
        var run = Run("d5_extra_trailing", 2);
        var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var first, out var error), error);
        Assert.Equal(ExecTag.Confirmed, first!.Exec!.Tag);
        Assert.False(reader.TryReadNext(Monotonic() + 2_000_000_000L, out _, out _));
    }

    [Fact]
    public void D5_DescriptorTopologyUsesRealFd5Pipe()
    {
        SkipUnlessQualified();
        using var pipe = new StatusPipe();
        var run = Run("d5_hold_exec", 0, pipe);
        var reader = new CoordinatorStatusReader(pipe.Source!, Execution, Preparation, Digest);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var observation, out var error), error);
        Assert.Equal(ExecTag.Confirmed, observation!.Exec!.Tag);
        var coordinatorPid = run.ProcessId;
        var coordinatorFd5 = Link($"/proc/{coordinatorPid}/fd/5");
        var ownerRead = Link($"/proc/{Environment.ProcessId}/fd/{pipe.ReadFd}");
        Assert.Contains("pipe:[", coordinatorFd5, StringComparison.Ordinal);
        Assert.Equal(coordinatorFd5, ownerRead);
        Record("descriptor_hold", run);
        var evidence = Environment.GetEnvironmentVariable("D5_EVIDENCE");
        if (!string.IsNullOrEmpty(evidence))
        {
            Directory.CreateDirectory(evidence);
            File.WriteAllText(Path.Combine(evidence, "descriptor-topology.txt"),
                $"coordinator_pid={coordinatorPid}\ncoordinator_fd5={coordinatorFd5}\nowner_read_fd={pipe.ReadFd}\nowner_read_link={ownerRead}\n");
        }
        run.Process!.Kill();
        run.Process.WaitForExit(5000);
        pipe.Dispose();
    }

    [Fact]
    public void D5_ReleaseValidationExits20And21And22()
    {
        SkipUnlessQualified();
        var valid = CoordinatorProtocol.EncodeRelease(Identity(), OverallDeadline);
        Assert.Equal(20, RunRelease(valid[..64], includeStatus: false).ExitCode);
        Assert.Equal(21, RunRelease(valid.Concat(new byte[] { 0xaa }).ToArray(), includeStatus: false).ExitCode);
        var invalid = (byte[])valid.Clone();
        invalid[56] ^= 1;
        Assert.Equal(22, RunRelease(invalid, includeStatus: false).ExitCode);
        Assert.Equal(0, RunRelease(valid, includeStatus: false).ExitCode);
    }

    [Fact]
    public void D5_N18_StatusDeadlineExpiresWithoutObservation()
    {
        SkipUnlessQualified();
        var run = Run("d5_no_status", 0);
        var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
        Assert.False(reader.TryReadNext(Monotonic() + 10_000_000L, out var observation, out var error));
        Assert.Null(observation);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void D5_O02_NoDurableCommittedMeansNoDurableObservation()
    {
        SkipUnlessQualified();
        var run = Run("exec_confirmed", 0);
        var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var observation, out var error), error);
        var (journal, writer, temp) = SeedJournal();
        using (temp)
        using (writer)
        {
            var buffer = new ReleaseObservationBuffer();
            Assert.True(buffer.OfferExec(observation!.Exec!));
            Assert.True(buffer.TryFlush(journal, writer, out error), error);
            Assert.Null(journal.ReadLatest().Snapshot!.Exec);
            Assert.Null(journal.ReadLatest().Snapshot!.Root);
        }
    }

    [Fact]
    public void D5_O03_JournalFailureIsNotReportedAsDurableObservation()
    {
        SkipUnlessQualified();
        var run = Run("exec_confirmed", 0);
        var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var observation, out var error), error);
        var storage = new FakeJournalStorage();
        var journal = new OwnershipJournal(storage, "/wp05-d5-status-failure");
        Assert.True(journal.TryAcquireWriter(out var writer, out error), error);
        using (writer)
        {
            Assert.True(journal.WriteNext(writer!, RecoveryTestSupport.V3Snapshot(
                release: RecoveryTestSupport.ReleaseCommitted())).Ok);
            var buffer = new ReleaseObservationBuffer();
            Assert.True(buffer.OfferExec(observation!.Exec!));
            buffer.MarkDurableCommitted();
            storage.FailAt("write", 1);
            Assert.False(buffer.TryFlush(journal, writer!, out error));
            Assert.Contains("exec", error, StringComparison.OrdinalIgnoreCase);
            Assert.Null(journal.ReadLatest().Snapshot!.Exec);
        }
    }

    [Fact]
    public void D5_EvidenceDumpNativeStatusFrames()
    {
        SkipUnlessQualified();
        foreach (var mode in new[]
                 {
                     "exec_confirmed", "exec_failed", "exec_confirmed_root",
                     "root_unconfirmed", "exec_failed_root", "protocol_failed",
                     "d5_wrong_exec", "d5_wrong_prep", "d5_wrong_digest",
                     "d5_wrong_sequence", "d5_replayed_sequence", "d5_duplicate_exec",
                     "d5_root_after_terminal", "d5_contradictory", "d5_wrong_magic",
                     "d5_wrong_version", "d5_short_header", "d5_truncated_payload",
                     "d5_length_below_header", "d5_length_above_max", "d5_invalid_kind",
                     "d5_invalid_payload_length", "d5_premature_eof", "d5_extra_trailing",
                 })
        {
            var run = Run(mode, 2);
            var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
            reader.TryReadNext(Monotonic() + 1_000_000_000L, out _, out _);
            reader.TryReadNext(Monotonic() + 1_000_000_000L, out _, out _);
            Record(mode, run);
        }
    }

    [Fact]
    public void D5_O01_NativeStatusIsGatedByDurableCommitted()
    {
        SkipUnlessQualified();
        var run = Run("exec_confirmed_root", 7 << 8);
        var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var exec, out var error), error);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var root, out error), error);
        var (journal, writer, temp) = SeedJournal();
        using (temp)
        using (writer)
        {
            var buffer = new ReleaseObservationBuffer();
            Assert.True(buffer.OfferExec(exec!.Exec!));
            Assert.True(buffer.OfferRoot(root!.Root!));
            Assert.True(buffer.TryFlush(journal, writer, out error), error);
            Assert.Null(journal.ReadLatest().Snapshot!.Exec);
            buffer.MarkDurableCommitted();
            Assert.True(buffer.TryFlush(journal, writer, out error), error);
            var snapshot = journal.ReadLatest().Snapshot!;
            Assert.Equal(ExecTag.Confirmed, snapshot.Exec!.Tag);
            Assert.Equal(RootKind.Exited, snapshot.Root!.Kind);
        }
    }

    [Fact]
    public void D5_O04_ContradictoryNativeStatusDoesNotOverwrite()
    {
        SkipUnlessQualified();
        var run = Run("d5_contradictory", 2);
        var reader = new CoordinatorStatusReader(run.Source!, Execution, Preparation, Digest);
        Assert.True(reader.TryReadNext(Monotonic() + 2_000_000_000L, out var exec, out var error), error);
        Assert.False(reader.TryReadNext(Monotonic() + 2_000_000_000L, out _, out _));
        var (journal, writer, temp) = SeedJournal();
        using (temp)
        using (writer)
        {
            var buffer = new ReleaseObservationBuffer();
            Assert.True(buffer.OfferExec(exec!.Exec!));
            buffer.MarkDurableCommitted();
            Assert.True(buffer.TryFlush(journal, writer, out error), error);
            Assert.Equal(ExecTag.Confirmed, journal.ReadLatest().Snapshot!.Exec!.Tag);
            Assert.Null(journal.ReadLatest().Snapshot!.Root);
        }
    }

    private static RunResult Run(string mode, uint value, StatusPipe? pipe = null)
    {
        var statusPipe = pipe ?? new StatusPipe();
        var start = new ProcessStartInfo(ArtifactPath())
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        var release = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
        start.Environment["WP05_RELEASE_FD"] = release.GetClientHandleAsString();
        start.Environment["WP05_STATUS_FD"] = statusPipe.ClientHandle;
        start.Environment["WP05_EXPECT_EXEC"] = Hex(CoordinatorProtocol.GuidToRfcBytes(Execution));
        start.Environment["WP05_EXPECT_PREP"] = Hex(CoordinatorProtocol.GuidToRfcBytes(Preparation));
        start.Environment["WP05_EXPECT_GEN"] = Hex(CoordinatorProtocol.GuidToRfcBytes(Generation));
        start.Environment["WP05_EXPECT_DIGEST"] = Hex(Digest);
        start.Environment["WP05_EXPECT_OVERALL"] = OverallDeadline.ToString();
        start.Environment["WP05_STATUS_MODE"] = mode;
        start.Environment["WP05_STATUS_VALUE"] = value.ToString();
        var process = Process.Start(start) ?? throw new InvalidOperationException("D5 coordinator did not start");
        release.DisposeLocalCopyOfClientHandle();
        var frame = CoordinatorProtocol.EncodeRelease(Identity(), OverallDeadline);
        release.Write(frame, 0, frame.Length);
        release.Flush();
        release.Dispose();
        if (mode != "d5_hold_exec")
            process.WaitForExit(5000);
        var stdout = string.Empty;
        var stderr = string.Empty;
        if (mode != "d5_hold_exec")
        {
            stdout = process.StandardOutput.ReadToEnd();
            stderr = process.StandardError.ReadToEnd();
        }
        return new RunResult(
            process.Id, process.HasExited ? process.ExitCode : int.MinValue,
            process, statusPipe.Source!, statusPipe.Source!.RawBytes, stdout + stderr,
            $"fd5={Link($"/proc/{process.Id}/fd/5")}", statusPipe);
    }

    private static void Record(string label, RunResult run)
    {
        var root = Environment.GetEnvironmentVariable("D5_EVIDENCE");
        if (string.IsNullOrEmpty(root)) return;
        var dir = Path.Combine(root, "native-status");
        Directory.CreateDirectory(dir);
        var raw = run.Source?.Raw ?? Array.Empty<byte>();
        File.WriteAllBytes(Path.Combine(dir, label + ".bin"), raw);
        File.WriteAllText(Path.Combine(dir, label + ".txt"),
            $"mode={label}\nexit={run.ExitCode}\nraw_bytes={raw.Length}\nraw_hex={Convert.ToHexString(raw)}\nfd5={run.RawIdentity}\noutput={run.Output}\n");
    }

    private static RunResult RunRelease(byte[] bytes, bool includeStatus)
    {
        var release = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
        var start = new ProcessStartInfo(ArtifactPath())
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.Environment["WP05_RELEASE_FD"] = release.GetClientHandleAsString();
        start.Environment["WP05_EXPECT_EXEC"] = Hex(CoordinatorProtocol.GuidToRfcBytes(Execution));
        start.Environment["WP05_EXPECT_PREP"] = Hex(CoordinatorProtocol.GuidToRfcBytes(Preparation));
        start.Environment["WP05_EXPECT_GEN"] = Hex(CoordinatorProtocol.GuidToRfcBytes(Generation));
        start.Environment["WP05_EXPECT_DIGEST"] = Hex(Digest);
        start.Environment["WP05_EXPECT_OVERALL"] = OverallDeadline.ToString();
        using var process = Process.Start(start) ?? throw new InvalidOperationException("D5 coordinator did not start");
        release.DisposeLocalCopyOfClientHandle();
        release.Write(bytes, 0, bytes.Length);
        release.Flush();
        release.Dispose();
        process.WaitForExit(5000);
        return new RunResult(process.Id, process.ExitCode, process, null, 0,
            process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd(), string.Empty, null);
    }

    private static (OwnershipJournal Journal, JournalWriterLease Writer, D5Temp Temp) SeedJournal()
    {
        var temp = new D5Temp();
        var dir = temp.Path;
        Directory.CreateDirectory(dir);
        var journal = new OwnershipJournal(new JournalFileStorage(), dir);
        Assert.True(journal.TryAcquireWriter(out var writer, out var error), error);
        Assert.True(journal.WriteNext(writer!, RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted())).Ok);
        return (journal, writer!, temp);
    }

    private sealed class D5Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "gagamba-d5-status-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
        }
    }

    private static string ArtifactPath()
    {
        var root = RepositoryRoot();
        var path = Path.Combine(root, Artifact);
        if (!File.Exists(path)) Assert.Skip($"HELD - REQUIRED NATIVE ARTIFACT UNAVAILABLE: {path}");
        return path;
    }

    private static void SkipUnlessQualified()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("linux only");
            return;
        }

        if (Environment.GetEnvironmentVariable("GAGAMBA_DELEGATED_CGROUP") is not { Length: > 0 })
            Assert.Skip("Unavailable: delegated cgroup is not set");
        if (StatusField("Uid").Split('\t')[0] == "0")
            Assert.Skip("Unqualified: test process is root");
        if (StatusField("CapEff") != "0000000000000000")
            Assert.Skip($"Unqualified: CapEff={StatusField("CapEff")}");
    }

    private static string StatusField(string name)
    {
        foreach (var line in File.ReadAllLines("/proc/self/status"))
            if (line.StartsWith(name + ":", StringComparison.Ordinal))
                return line[(name.Length + 1)..].Trim();
        return string.Empty;
    }

    private static string RepositoryRoot()
    {
        var configured = Environment.GetEnvironmentVariable("GAGAMBA_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(configured);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Gagamba.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("repository root not found");
    }

    private static long Monotonic()
    {
        var ticks = Stopwatch.GetTimestamp();
        return ticks / Stopwatch.Frequency * 1_000_000_000L
            + ticks % Stopwatch.Frequency * 1_000_000_000L / Stopwatch.Frequency;
    }

    private static string Hex(ReadOnlySpan<byte> bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    private static string Link(string path)
    {
        try { return new FileInfo(path).LinkTarget ?? "none"; } catch { return "none"; }
    }

    private static PreparationIdentity Identity() => new(Execution, Guid.Parse("eeeeeeee-0000-0000-0000-0000000000d8"), Generation, Preparation, Digest);

    private sealed record RunResult(
        int ProcessId, int ExitCode, Process? Process, RecordingFdSource? Source,
        int RawBytes, string Output, string RawIdentity, StatusPipe? statusPipe);

    private sealed class StatusPipe : IDisposable
    {
        private readonly AnonymousPipeServerStream _server;
        public StatusPipe()
        {
            _server = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            ClientHandle = _server.GetClientHandleAsString();
            Source = new RecordingFdSource(ReadFd);
        }

        public int ReadFd => (int)_server.SafePipeHandle.DangerousGetHandle();
        public string ClientHandle { get; }
        public RecordingFdSource? Source { get; }

        public void Dispose()
        {
            _server.Dispose();
        }
    }

    private sealed class RecordingFdSource : IStatusFrameSource
    {
        private readonly int _fd;
        private readonly List<byte> _raw = new();
        public RecordingFdSource(int fd) => _fd = fd;
        public int RawBytes => _raw.Count;
        public byte[] Raw => _raw.ToArray();

        public bool TryReadFrame(long deadlineNanoseconds, out byte[]? frame, out string error)
        {
            frame = null;
            var header = new byte[CoordinatorProtocol.HeaderBytes];
            if (!ReadExact(header, deadlineNanoseconds, out error)) return false;
            var total = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(12));
            if (total < CoordinatorProtocol.HeaderBytes || total > CoordinatorProtocol.MaxFrameBytes)
            {
                error = "status frame length is outside the fixed bound";
                return false;
            }

            var payload = new byte[checked((int)total) - CoordinatorProtocol.HeaderBytes];
            if (payload.Length > 0 && !ReadExact(payload, deadlineNanoseconds, out error)) return false;
            frame = new byte[total];
            header.CopyTo(frame, 0);
            payload.CopyTo(frame, CoordinatorProtocol.HeaderBytes);
            return true;
        }

        private bool ReadExact(byte[] target, long deadlineNanoseconds, out string error)
        {
            error = string.Empty;
            var offset = 0;
            while (offset < target.Length)
            {
                var remaining = deadlineNanoseconds - Monotonic();
                if (remaining <= 0) { error = "status deadline expired"; return false; }
                var poll = new[] { new NativeLaunchInterop.PollFd { Fd = _fd, Events = 1 } };
                var rc = NativeLaunchInterop.Poll(poll, 1, (int)Math.Clamp(remaining / 1_000_000, 1, 250));
                if (rc < 0)
                {
                    if (Marshal.GetLastWin32Error() == NativeLaunchInterop.EINTR) continue;
                    error = $"status poll errno {Marshal.GetLastWin32Error()}";
                    return false;
                }

                if (rc == 0) continue;
                var scratch = new byte[target.Length - offset];
                var read = (int)NativeLaunchInterop.Read(_fd, scratch, (nuint)scratch.Length);
                if (read < 0 && Marshal.GetLastWin32Error() == NativeLaunchInterop.EINTR) continue;
                if (read <= 0) { error = "status channel closed before a complete record"; return false; }
                for (var i = 0; i < read; i++) _raw.Add(scratch[i]);
                Buffer.BlockCopy(scratch, 0, target, offset, read);
                offset += read;
            }

            return true;
        }
    }
}
