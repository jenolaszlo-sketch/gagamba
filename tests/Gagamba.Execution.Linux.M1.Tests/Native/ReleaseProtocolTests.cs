// WP05 Linux no-target release protocol tests. The qualification coordinator
// validates the exact frame and exits before any target fork; the real fixture
// is never launched.
using System.Diagnostics;
using System.IO.Pipes;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Native;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class ReleaseProtocolTests
{
    private static readonly Guid Execution = new("aaaaaaaa-0000-0000-0000-00000000002a");
    private static readonly Guid Issuer = new("bbbbbbbb-0000-0000-0000-00000000002b");
    private static readonly Guid Generation = new("cccccccc-0000-0000-0000-00000000002c");
    private static readonly Guid Preparation = new("dddddddd-0000-0000-0000-00000000002d");
    private static readonly byte[] Digest = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
    private const long OverallDeadline = long.MaxValue / 2;

    private sealed class StreamStatusFrameSource : IStatusFrameSource
    {
        private readonly Stream _stream;

        public StreamStatusFrameSource(Stream stream) => _stream = stream;

        public bool TryReadFrame(long deadlineNanoseconds, out byte[]? frame, out string error)
        {
            frame = null;
            error = string.Empty;
            var header = new byte[CoordinatorProtocol.HeaderBytes];
            if (!ReadExact(header))
            {
                error = "status stream closed";
                return false;
            }

            var total = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(12));
            if (total < CoordinatorProtocol.HeaderBytes || total > CoordinatorProtocol.MaxFrameBytes)
            {
                error = "status frame length invalid";
                return false;
            }

            var payload = new byte[checked((int)total) - CoordinatorProtocol.HeaderBytes];
            if (payload.Length > 0 && !ReadExact(payload))
            {
                error = "status payload closed";
                return false;
            }

            frame = new byte[checked((int)total)];
            header.CopyTo(frame, 0);
            payload.CopyTo(frame, CoordinatorProtocol.HeaderBytes);
            return true;
        }

        private bool ReadExact(byte[] target)
        {
            var offset = 0;
            while (offset < target.Length)
            {
                var read = _stream.Read(target, offset, target.Length - offset);
                if (read <= 0) return false;
                offset += read;
            }

            return true;
        }
    }

    private sealed record RunResult(int ExitCode, string Output, byte[] StatusBytes);

    [Fact]
    public void ValidReleaseIsConsumedOnceAndTrustedStatusIsReadable()
    {
        var frame = CoordinatorProtocol.EncodeRelease(Identity(), OverallDeadline);
        var result = Run(frame, "exec_confirmed_root", rawWait: 0);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("wp05-release-accepted", result.Output);

        using var status = new MemoryStream(result.StatusBytes);
        var reader = new CoordinatorStatusReader(
            new StreamStatusFrameSource(status), Execution, Preparation, Digest);
        Assert.True(reader.TryReadNext(1_000_000, out var exec, out var error), error);
        Assert.Equal(ExecTag.Confirmed, exec!.Exec!.Tag);
        Assert.True(reader.TryReadNext(1_000_000, out var root, out error), error);
        Assert.Equal(RootKind.Exited, root!.Root!.Kind);
        Assert.False(reader.TryReadNext(1_000_000, out _, out _));
    }

    [Fact]
    public void MalformedAndExtraReleaseFramesAreRefusedWithoutTarget()
    {
        var frame = CoordinatorProtocol.EncodeRelease(Identity(), OverallDeadline);
        var wrongIdentity = (byte[])frame.Clone();
        wrongIdentity[24] ^= 1;
        Assert.NotEqual(0, Run(wrongIdentity, null, 0).ExitCode);

        var wrongMagic = (byte[])frame.Clone();
        wrongMagic[0] ^= 1;
        Assert.NotEqual(0, Run(wrongMagic, null, 0).ExitCode);

        var duplicate = frame.Concat(frame).ToArray();
        var duplicateResult = Run(duplicate, null, 0);
        Assert.NotEqual(0, duplicateResult.ExitCode);
        Assert.DoesNotContain("wp05-release-accepted", duplicateResult.Output);
    }

    [Fact]
    public void ExpiredReleaseDeadlineIsRefused()
    {
        var frame = CoordinatorProtocol.EncodeRelease(Identity(), 1);
        var result = Run(frame, null, 0);
        Assert.NotEqual(0, result.ExitCode);
        Assert.DoesNotContain("wp05-release-accepted", result.Output);
    }

    [Fact]
    public void NonblockingPipeTransportMapsDeliveredBrokenPipeAndWouldBlock()
    {
        SkipUnlessLinux();
        using (var pipe = new LinuxPipe())
        {
            Assert.True(NativeLaunchInterop.TrySetNonBlocking(pipe.WriteFd, out var error), error);
            var frame = CoordinatorProtocol.EncodeRelease(Identity(), OverallDeadline);
            Assert.Equal(ReleasePipeWriteResult.Delivered,
                ReleasePipeWrite.WriteOnce(pipe.WriteFd, frame));
        }

        using (var pipe = new LinuxPipe())
        {
            Assert.True(NativeLaunchInterop.TrySetNonBlocking(pipe.WriteFd, out _));
            pipe.CloseRead();
            var frame = CoordinatorProtocol.EncodeRelease(Identity(), OverallDeadline);
            Assert.Equal(ReleasePipeWriteResult.BrokenPipe,
                ReleasePipeWrite.WriteOnce(pipe.WriteFd, frame));
        }

        using (var pipe = new LinuxPipe())
        {
            Assert.True(NativeLaunchInterop.TrySetNonBlocking(pipe.WriteFd, out _));
            var fill = new byte[4096];
            for (var i = 0; i < 256; i++)
            {
                if (NativeLaunchInterop.WriteBytesOnce(pipe.WriteFd, fill) < 0)
                {
                    break;
                }
            }

            var frame = CoordinatorProtocol.EncodeRelease(Identity(), OverallDeadline);
            Assert.Equal(ReleasePipeWriteResult.WouldBlock,
                ReleasePipeWrite.WriteOnce(pipe.WriteFd, frame));
        }
    }

    private sealed class LinuxPipe : IDisposable
    {
        private readonly Microsoft.Win32.SafeHandles.SafeFileHandle _read;
        private readonly Microsoft.Win32.SafeHandles.SafeFileHandle _write;
        private bool _readClosed;

        public LinuxPipe()
        {
            Assert.True(NativeLaunchInterop.TryCreatePipe(out var read, out var write, out var error), error);
            _read = read!;
            _write = write!;
        }

        public int WriteFd => checked((int)_write.DangerousGetHandle());

        public void CloseRead()
        {
            if (_readClosed) return;
            _readClosed = true;
            _read.Dispose();
        }

        public void Dispose()
        {
            _write.Dispose();
            if (!_readClosed) _read.Dispose();
        }
    }

    private static void SkipUnlessLinux()
    {
        if (!OperatingSystem.IsLinux()) Assert.Skip("requires Linux");
    }

    private static PreparationIdentity Identity() =>
        new(Execution, Issuer, Generation, Preparation, Digest);

    private static RunResult Run(byte[] frame, string? statusMode, uint rawWait)
    {
        SkipUnlessLinux();
        var binary = QualificationCoordinator.Value;
        using var releasePipe = new AnonymousPipeServerStream(
            PipeDirection.Out, HandleInheritability.Inheritable);
        using var statusPipe = new AnonymousPipeServerStream(
            PipeDirection.In, HandleInheritability.Inheritable);
        var start = new ProcessStartInfo(binary)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.Environment["WP05_RELEASE_FD"] = releasePipe.GetClientHandleAsString();
        start.Environment["WP05_STATUS_FD"] = statusPipe.GetClientHandleAsString();
        start.Environment["WP05_EXPECT_EXEC"] = Hex(CoordinatorProtocol.GuidToRfcBytes(Execution));
        start.Environment["WP05_EXPECT_PREP"] = Hex(CoordinatorProtocol.GuidToRfcBytes(Preparation));
        start.Environment["WP05_EXPECT_GEN"] = Hex(CoordinatorProtocol.GuidToRfcBytes(Generation));
        start.Environment["WP05_EXPECT_DIGEST"] = Hex(Digest);
        start.Environment["WP05_EXPECT_OVERALL"] = OverallDeadline.ToString();
        if (statusMode is not null)
        {
            start.Environment["WP05_STATUS_MODE"] = statusMode;
            start.Environment["WP05_STATUS_VALUE"] = rawWait.ToString();
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("coordinator did not start");
        releasePipe.DisposeLocalCopyOfClientHandle();
        statusPipe.DisposeLocalCopyOfClientHandle();
        releasePipe.Write(frame, 0, frame.Length);
        releasePipe.Flush();
        releasePipe.Dispose();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(10_000), $"coordinator did not exit: {error}");
        byte[] statusBytes;
        using (var memory = new MemoryStream())
        {
            statusPipe.CopyTo(memory);
            statusBytes = memory.ToArray();
        }

        return new RunResult(process.ExitCode, output, statusBytes);
    }

    private static string Hex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(bytes).ToLowerInvariant();

    private static readonly Lazy<string> QualificationCoordinator = new(CompileQualificationCoordinator);

    private static string CompileQualificationCoordinator()
    {
        var testsRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        var source = Path.Combine(testsRoot, "Fixtures", "wp05-qualification-coordinator.c");
        var include = Path.GetFullPath(Path.Combine(testsRoot, "..", "native", "linux-m1"));
        var output = Path.Combine(Path.GetTempPath(), $"wp05-qualification-{Guid.NewGuid():N}");
        var start = new ProcessStartInfo("/usr/bin/cc")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add("-static");
        start.ArgumentList.Add("-O2");
        start.ArgumentList.Add("-std=c11");
        start.ArgumentList.Add("-I" + include);
        start.ArgumentList.Add("-o");
        start.ArgumentList.Add(output);
        start.ArgumentList.Add(source);
        using var compiler = Process.Start(start) ?? throw new InvalidOperationException("compiler did not start");
        var error = compiler.StandardError.ReadToEnd();
        Assert.True(compiler.WaitForExit(30_000), "compiler timed out");
        Assert.True(compiler.ExitCode == 0, error);
        return output;
    }
}
