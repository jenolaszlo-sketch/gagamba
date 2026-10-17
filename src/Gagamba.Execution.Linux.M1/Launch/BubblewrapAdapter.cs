using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Launch;

/// <summary>Fixed Bubblewrap 0.11.1 adapter for WP04. The adapter can enter
/// HELD and can abort; it has no managed release operation.</summary>
internal sealed class BubblewrapAdapter : IHeldLauncher
{
    internal const string FilterSha256 = "3649a083435cb7e7f1dad0157dfe6bd4c480ad635b7cecb88214a9332403395c";
    internal const ulong FilterLength = 424;
    private const int ConfigHeaderBytes = 202;
    private const int MaxConfigBytes = 16 * 1024;
    private const int MaxCaptureBytes = 65536;
    private const string FixtureConstructorMarker = "gagamba-m1-fixture: ctor-marker";

    public Task<HeldLaunchOutcome> PrepareHeldAsync(
        DomainLease domain,
        PreparedClosure closure,
        RestrictedManifest manifest,
        PreparationIdentity identity,
        long setupDeadlineNs,
        long overallDeadlineNs,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(closure);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(identity);
        cancellationToken.ThrowIfCancellationRequested();

        HeldExecution? execution = null;
        SafeFileHandle? bwrap = null, config = null, filter = null;
        SafeFileHandle? stdinRead = null, stdoutRead = null, stdoutWrite = null;
        SafeFileHandle? stderrRead = null, stderrWrite = null, controlRead = null, controlWrite = null;
        SafeFileHandle? coordinatorStatusRead = null, coordinatorStatusWrite = null;
        SafeFileHandle? helperStatusRead = null, helperStatusWrite = null;
        Process? wrapper = null;
        SafeFileHandle? wrapperPidFd = null;
        var spawned = false;
        try
        {
            if (MonotonicNowNanoseconds() >= setupDeadlineNs || setupDeadlineNs >= overallDeadlineNs)
                throw new InvalidOperationException("held launch setup deadline expired before Bubblewrap admission");

            var objects = closure.Objects.ToDictionary(item => item.Role, StringComparer.Ordinal);
            if (objects.Count != 4 || !new[] { "tool", "input", "coordinator", "filter" }.All(objects.ContainsKey))
                throw new InvalidOperationException("held launch requires exactly the tool, input, coordinator and filter objects");

            var tool = objects["tool"];
            var input = objects["input"];
            var coordinator = objects["coordinator"];
            var filterObject = objects["filter"];
            if (!ReadAndVerifyFilter(filterObject, out var filterBytes, out var filterError))
                throw new InvalidOperationException(filterError);

            if (!HelperInstallation.TryOpenPinnedBubblewrap(out bwrap, out var helperError))
                throw new InvalidOperationException(helperError);

            var canonicalManifest = ManifestCodec.Encode(manifest);
            if (ConfigHeaderBytes + canonicalManifest.Length > MaxConfigBytes)
                throw new InvalidOperationException("sealed launch configuration exceeds the 16 KiB bound");
            var configBytes = EncodeConfiguration(
                canonicalManifest, identity, tool, input, coordinator, filterObject,
                setupDeadlineNs, overallDeadlineNs);
            if (!NativeLaunchInterop.TryCreateSealedMemfd("gaga-coordinator-config", configBytes, out config, out var configError))
                throw new InvalidOperationException(configError);
            if (!NativeLaunchInterop.TryCreateSealedMemfd("gaga-seccomp-filter", filterBytes, out filter, out var filterError2))
                throw new InvalidOperationException(filterError2);

            if (!CreatePipe(out stdinRead, out var stdinWriter, out var pipeError))
                throw new InvalidOperationException(pipeError);
            stdinWriter.Dispose(); // FD 0 is provably EOF-only; no writer survives.
            if (!CreatePipe(out stdoutRead, out stdoutWrite, out pipeError)
                || !CreatePipe(out stderrRead, out stderrWrite, out pipeError)
                || !CreatePipe(out controlRead, out controlWrite, out pipeError)
                || !CreatePipe(out coordinatorStatusRead, out coordinatorStatusWrite, out pipeError)
                || !CreatePipe(out helperStatusRead, out helperStatusWrite, out pipeError))
                throw new InvalidOperationException(pipeError);

            var descriptors = new[]
            {
                new LaunchDescriptor(0, LaunchDescriptorRole.EofStdin, Fd(stdinRead)),
                new LaunchDescriptor(1, LaunchDescriptorRole.StdoutWriter, Fd(stdoutWrite)),
                new LaunchDescriptor(2, LaunchDescriptorRole.StderrWriter, Fd(stderrWrite)),
                new LaunchDescriptor(3, LaunchDescriptorRole.CoordinatorConfig, Fd(config!)),
                new LaunchDescriptor(4, LaunchDescriptorRole.ControlReader, Fd(controlRead)),
                new LaunchDescriptor(5, LaunchDescriptorRole.CoordinatorStatusWriter, Fd(coordinatorStatusWrite)),
                new LaunchDescriptor(6, LaunchDescriptorRole.SeccompProgram, Fd(filter!)),
                new LaunchDescriptor(7, LaunchDescriptorRole.ToolObject, Fd(tool.Handle)),
                new LaunchDescriptor(8, LaunchDescriptorRole.InputObject, Fd(input.Handle)),
                new LaunchDescriptor(9, LaunchDescriptorRole.CoordinatorObject, Fd(coordinator.Handle)),
                new LaunchDescriptor(10, LaunchDescriptorRole.HelperStatusWriter, Fd(helperStatusWrite)),
                new LaunchDescriptor(11, LaunchDescriptorRole.PinnedBubblewrap, Fd(bwrap!)),
            };
            if (!LaunchDescriptorMap.TryCreate(descriptors, out var map, out var mapError))
                throw new InvalidOperationException(mapError);

            if (!NativeLaunchInterop.TrySpawn(domain, map!, BubblewrapArguments.Build(), out var pid, out var spawnError))
                throw new InvalidOperationException(spawnError);
            spawned = true;
            wrapper = Process.GetProcessById(pid);
            if (!PidFd.TryOpen(pid, out var pidfd, out var pidfdError)
                || !PreparationInterop.HasCloexec(pidfd))
            {
                if (pidfd >= 0) PreparationInterop.CloseFd(pidfd);
                throw new InvalidOperationException($"Bubblewrap pidfd acquisition failed: {pidfdError}");
            }
            wrapperPidFd = new SafeFileHandle(new IntPtr(pidfd), ownsHandle: true);

            // Parent closes every child endpoint immediately after the atomic spawn.
            Dispose(ref stdinRead);
            Dispose(ref stdoutWrite);
            Dispose(ref stderrWrite);
            Dispose(ref controlRead);
            Dispose(ref coordinatorStatusWrite);
            Dispose(ref helperStatusWrite);
            Dispose(ref config);
            Dispose(ref filter);
            Dispose(ref bwrap);

            if (!TryReadJsonLine(Fd(helperStatusRead), setupDeadlineNs, out var helperLine, out var helperPid, out var helperReadError))
                throw new InvalidOperationException($"Bubblewrap setup status refused: {helperReadError}");
            if (!TryReadCoordinatorFrame(Fd(coordinatorStatusRead), setupDeadlineNs, out var statusFrame, out var frameError))
                throw new InvalidOperationException($"trusted coordinator HELD status refused: {frameError}");
            if (!CoordinatorProtocol.TryDecode(statusFrame!, identity.ExecutionId, identity.PreparationId,
                    identity.ManifestDigest.AsSpan(), 1, out var decoded, out var decodeError)
                || !CoordinatorProtocol.TryDecodeHeldReport(decoded!, out var report, out decodeError))
                throw new InvalidOperationException($"trusted coordinator HELD frame invalid: {decodeError}");
            if (!ValidateHeldReport(report!, tool, input, coordinator, helperPid, domain, out var reportError))
                throw new InvalidOperationException(reportError);
            var stdoutCaptured = TryReadAvailable(Fd(stdoutRead!), MaxCaptureBytes, out var initialStdout, out var stdoutError);
            var stderrCaptured = TryReadAvailable(Fd(stderrRead!), MaxCaptureBytes, out var initialStderr, out var stderrError);
            if (!stdoutCaptured || !stderrCaptured)
                throw new InvalidOperationException($"bounded setup capture failed: {stdoutError}{stderrError}");
            if (Encoding.UTF8.GetString(initialStdout).Contains(FixtureConstructorMarker, StringComparison.Ordinal)
                || Encoding.UTF8.GetString(initialStderr).Contains(FixtureConstructorMarker, StringComparison.Ordinal))
                throw new InvalidOperationException("fixture constructor marker appeared before trusted HELD");
            if (MonotonicNowNanoseconds() >= overallDeadlineNs)
                throw new InvalidOperationException("overall execution deadline expired before HELD acceptance");

            execution = new HeldExecution(wrapper, wrapperPidFd!, controlWrite!, coordinatorStatusRead!, helperStatusRead!,
                stdoutRead!, stderrRead!, helperPid, report!);
            wrapper = null;
            wrapperPidFd = controlWrite = coordinatorStatusRead = helperStatusRead = stdoutRead = stderrRead = null;
            return Task.FromResult(new HeldLaunchOutcome(true, execution, statusFrame, report, string.Empty,
                NoTargetInitializationMarkerAbsent: true));
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or JsonException
            or CryptographicException or OverflowException or DllNotFoundException or EntryPointNotFoundException)
        {
            string cleanupError = string.Empty;
            if (spawned)
            {
                controlWrite?.Dispose();
                var stop = domain.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new LaunchDomainClock());
                if (!stop.Stopped) cleanupError = $"; domain stop failed: {stop.Error}";
                if (wrapper is not null)
                {
                    if (wrapperPidFd is not null
                        && PidFd.TryExited(Fd(wrapperPidFd), 5000, out var exited, out _)
                        && exited)
                    {
                        if (!wrapper.WaitForExit(5000)) cleanupError = "; Bubblewrap was not reapable after pidfd exit";
                    }
                    else cleanupError = "; Bubblewrap pidfd did not prove exit";
                }
                if (stdoutRead is not null)
                {
                    if (ReadToEofBounded(Fd(stdoutRead), MaxCaptureBytes, out var failedStdout, out var failedStdoutError))
                    {
                        if (Encoding.UTF8.GetString(failedStdout).Contains(FixtureConstructorMarker, StringComparison.Ordinal))
                            cleanupError += "; target constructor marker appeared on stdout before HELD";
                    }
                    else cleanupError += $"; stdout cleanup drain failed: {failedStdoutError}";
                }
                if (stderrRead is not null)
                {
                    if (ReadToEofBounded(Fd(stderrRead), MaxCaptureBytes, out var failedStderr, out var failedStderrError))
                    {
                        if (Encoding.UTF8.GetString(failedStderr).Contains(FixtureConstructorMarker, StringComparison.Ordinal))
                            cleanupError += "; target constructor marker appeared on stderr before HELD";
                    }
                    else cleanupError += $"; stderr cleanup drain failed: {failedStderrError}";
                }
            }
            return Task.FromResult(new HeldLaunchOutcome(false, null, null, null, ex.Message + cleanupError));
        }
        finally
        {
            Dispose(ref stdinRead); Dispose(ref stdoutRead); Dispose(ref stdoutWrite);
            Dispose(ref stderrRead); Dispose(ref stderrWrite); Dispose(ref controlRead); Dispose(ref controlWrite);
            Dispose(ref coordinatorStatusRead); Dispose(ref coordinatorStatusWrite);
            Dispose(ref helperStatusRead); Dispose(ref helperStatusWrite);
            Dispose(ref config); Dispose(ref filter); Dispose(ref bwrap);
            wrapperPidFd?.Dispose();
            wrapper?.Dispose();
            _ = execution;
        }
    }

    public Task<HeldAbortOutcome> AbortAsync(
        HeldExecution execution,
        DomainLease domain,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(domain);
        cancellationToken.ThrowIfCancellationRequested();
        execution.CloseAbortControl();
        var stop = domain.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new LaunchDomainClock());
        var pidfdOk = PidFd.TryExited(Fd(execution.WrapperPidFd), 5000, out var exited, out var pidfdError);
        var reaped = pidfdOk && exited && execution.Wrapper.WaitForExit(5000);
        var stdoutOk = ReadToEofBounded(Fd(execution.StdoutReader), MaxCaptureBytes, out var stdout, out var stdoutError);
        var stderrOk = ReadToEofBounded(Fd(execution.StderrReader), MaxCaptureBytes, out var stderr, out var stderrError);
        var helperOk = ReadToEofBounded(Fd(execution.HelperStatusReader), 16 * 1024, out var helperTail, out var helperError);
        var coordinatorOk = ReadToEofBounded(Fd(execution.CoordinatorStatusReader), 16 * 1024, out _, out var coordinatorError);
        var markerAbsent = !Encoding.UTF8.GetString(stdout).Contains(FixtureConstructorMarker, StringComparison.Ordinal)
            && !Encoding.UTF8.GetString(stderr).Contains(FixtureConstructorMarker, StringComparison.Ordinal);
        var statusValid = ValidateHelperTail(helperTail, execution.HelperChildPid);
        var errors = new[]
        {
            !stop.Stopped ? $"domain stop/empty failed: {stop.Error}" : string.Empty,
            !pidfdOk ? $"wrapper pidfd observation failed: {pidfdError}" : string.Empty,
            !reaped ? "wrapper did not exit and reap exactly once" : string.Empty,
            !stdoutOk ? $"stdout drain failed: {stdoutError}" : string.Empty,
            !stderrOk ? $"stderr drain failed: {stderrError}" : string.Empty,
            !helperOk ? $"helper status drain failed: {helperError}" : string.Empty,
            !coordinatorOk ? $"coordinator status drain failed: {coordinatorError}" : string.Empty,
            !statusValid ? "Bubblewrap JSON completion status was malformed or mismatched" : string.Empty,
            !markerAbsent ? "fixture constructor marker appeared before release" : string.Empty,
        }.Where(value => value.Length != 0).ToArray();
        return Task.FromResult(new HeldAbortOutcome(errors.Length == 0, reaped, string.Join("; ", errors), markerAbsent));
    }

    private static bool CreatePipe(out SafeFileHandle read, out SafeFileHandle write, out string error)
    {
        if (!NativeLaunchInterop.TryCreatePipe(out var readHandle, out var writeHandle, out error))
        {
            read = write = new SafeFileHandle(IntPtr.Zero, ownsHandle: false);
            return false;
        }
        read = readHandle!;
        write = writeHandle!;
        return true;
    }

    private static bool ReadAndVerifyFilter(StagedObject filter, out byte[] bytes, out string error)
    {
        bytes = Array.Empty<byte>();
        error = string.Empty;
        if (filter.Length != FilterLength || filter.Length > 4096)
        {
            error = "staged seccomp filter length is not the plan-pinned 424 bytes";
            return false;
        }
        bytes = new byte[checked((int)filter.Length)];
        var fd = Fd(filter.Handle);
        var offset = 0;
        while (offset < bytes.Length)
        {
            var temporary = new byte[bytes.Length - offset];
            var read = NativeLaunchInterop.ReadAt(fd, temporary, (nuint)temporary.Length, offset);
            if (read <= 0) { error = "staged seccomp filter read failed"; return false; }
            Buffer.BlockCopy(temporary, 0, bytes, offset, (int)read);
            offset += (int)read;
        }
        var digest = SHA256.HashData(bytes);
        if (!digest.AsSpan().SequenceEqual(filter.Sha256)
            || !string.Equals(Convert.ToHexString(digest).ToLowerInvariant(), FilterSha256, StringComparison.Ordinal))
        {
            error = "staged seccomp filter digest does not match the approved artifact";
            return false;
        }
        return true;
    }

    private static byte[] EncodeConfiguration(byte[] manifest, PreparationIdentity identity,
        StagedObject tool, StagedObject input, StagedObject coordinator, StagedObject filter,
        long setupDeadlineNs, long overallDeadlineNs)
    {
        var result = new byte[checked(ConfigHeaderBytes + manifest.Length)];
        Encoding.ASCII.GetBytes("GGM1CFG\0").CopyTo(result, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(8), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(10), checked((uint)result.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(14), checked((uint)manifest.Length));
        CoordinatorProtocol.GuidToRfcBytes(identity.ExecutionId).CopyTo(result, 18);
        CoordinatorProtocol.GuidToRfcBytes(identity.PreparationId).CopyTo(result, 34);
        CoordinatorProtocol.GuidToRfcBytes(identity.GenerationId).CopyTo(result, 50);
        identity.ManifestDigest.CopyTo(result, 66);
        WriteIdentity(result, 98, tool);
        WriteIdentity(result, 114, input);
        WriteIdentity(result, 130, coordinator);
        filter.Sha256.CopyTo(result, 146);
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(178), filter.Length);
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(186), checked((ulong)setupDeadlineNs));
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(194), checked((ulong)overallDeadlineNs));
        manifest.CopyTo(result, ConfigHeaderBytes);
        return result;
    }

    private static void WriteIdentity(byte[] result, int offset, StagedObject item)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(offset), item.Device);
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(offset + 8), item.Inode);
    }

    private static bool TryReadJsonLine(int fd, long deadlineNs, out byte[] line, out int childPid, out string error)
    {
        line = Array.Empty<byte>();
        childPid = 0;
        error = string.Empty;
        using var stream = new MemoryStream(256);
        while (stream.Length < 4096)
        {
            var b = new byte[1];
            if (!ReadExact(fd, b, 0, 1, deadlineNs, out error)) return false;
            if (b[0] == (byte)'\n') break;
            stream.WriteByte(b[0]);
        }
        if (stream.Length is 0 or >= 4096) { error = "Bubblewrap status line is empty or overlong"; return false; }
        line = stream.ToArray();
        try
        {
            using var json = JsonDocument.Parse(line);
            if (!json.RootElement.TryGetProperty("child-pid", out var value)
                || !value.TryGetInt32(out childPid) || childPid <= 0)
            {
                error = "Bubblewrap did not report a positive child-pid";
                return false;
            }
            return true;
        }
        catch (JsonException)
        {
            error = "Bubblewrap status line is not valid JSON";
            return false;
        }
    }

    private static bool TryReadCoordinatorFrame(int fd, long deadlineNs, out byte[]? frame, out string error)
    {
        frame = null;
        var header = new byte[CoordinatorProtocol.HeaderBytes];
        if (!ReadExact(fd, header, 0, header.Length, deadlineNs, out error)) return false;
        var total = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(12));
        if (total < CoordinatorProtocol.HeaderBytes || total > CoordinatorProtocol.MaxFrameBytes)
        {
            error = "coordinator status length is outside its fixed bound";
            return false;
        }
        var result = new byte[checked((int)total)];
        header.CopyTo(result, 0);
        if (!ReadExact(fd, result, CoordinatorProtocol.HeaderBytes, result.Length - CoordinatorProtocol.HeaderBytes, deadlineNs, out error))
            return false;
        frame = result;
        return true;
    }

    private static bool ReadExact(int fd, byte[] target, int offset, int count, long deadlineNs, out string error)
    {
        error = string.Empty;
        while (count > 0)
        {
            var remainingNs = deadlineNs - MonotonicNowNanoseconds();
            if (remainingNs <= 0) { error = "status deadline expired"; return false; }
            var poll = new[] { new NativeLaunchInterop.PollFd { Fd = fd, Events = 1 } };
            if (NativeLaunchInterop.Poll(poll, 1, (int)Math.Clamp(remainingNs / 1_000_000, 1, 250)) < 0)
            { error = $"status poll errno {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}"; return false; }
            if (poll[0].Revents == 0) continue;
            var scratch = new byte[count];
            var read = NativeLaunchInterop.Read(fd, scratch, (nuint)count);
            if (read < 0 && System.Runtime.InteropServices.Marshal.GetLastWin32Error() == NativeLaunchInterop.EINTR) continue;
            if (read <= 0) { error = "status channel closed before a complete record"; return false; }
            Buffer.BlockCopy(scratch, 0, target, offset, (int)read);
            offset += (int)read;
            count -= (int)read;
        }
        return true;
    }

    private static bool ValidateHeldReport(CoordinatorHeldReport report, StagedObject tool, StagedObject input,
        StagedObject coordinator, int helperPid, DomainLease domain, out string error)
    {
        error = string.Empty;
        if (report.Uid != 0 || report.EffectiveUid != 0 || report.Gid != 0 || report.EffectiveGid != 0
            || report.NoNewPrivs != 1 || report.SeccompMode != 2 || report.Dumpable != 0
            || report.CapEff != 0 || report.CapPrm != 0 || report.CapAmb != 0
            || report.NoFileSoft != 64 || report.NoFileHard != 64 || report.CoreSoft != 0 || report.CoreHard != 0
            || report.CoordinatorNamespacePid <= 0 || report.NamespaceInodes.Any(value => value == 0))
        {
            error = "trusted coordinator HELD report does not prove the required uid/capability/seccomp/rlimit state";
            return false;
        }
        var expected = new[] { tool, input, coordinator };
        for (var i = 0; i < expected.Length; i++)
        {
            if (report.Objects[i] != (expected[i].Device, expected[i].Inode))
            {
                error = "trusted coordinator object identities differ from the prepared closure";
                return false;
            }
        }
        if (!report.Cgroup.TrimEnd('\n', '\r').EndsWith("/" + domain.Identity.ChildName, StringComparison.Ordinal))
        {
            error = "trusted coordinator reported a cgroup path outside its registered domain";
            return false;
        }
        if (!domain.ContainsProcess(helperPid, out var containmentError))
        {
            error = $"Bubblewrap child was not observed in the retained domain: {containmentError}";
            return false;
        }
        if (!CoordinatorMountVerifier.Validate(report.MountInfo, out error)) return false;
        return true;
    }

    private static bool ValidateHelperTail(byte[] bytes, int childPid)
    {
        if (bytes.Length > 16 * 1024) return false;
        var lines = Encoding.UTF8.GetString(bytes).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            try
            {
                using var json = JsonDocument.Parse(line);
                var root = json.RootElement;
                if (root.TryGetProperty("child-pid", out var pid)
                    && (!pid.TryGetInt32(out var found) || found != childPid)) return false;
                if (root.TryGetProperty("exit-code", out var code) && !code.TryGetInt32(out _)) return false;
            }
            catch (JsonException) { return false; }
        }
        return true;
    }

    private static bool ReadToEofBounded(int fd, int maximum, out byte[] bytes, out string error)
    {
        bytes = Array.Empty<byte>();
        error = string.Empty;
        using var output = new MemoryStream(Math.Min(maximum, 4096));
        var deadline = MonotonicNowNanoseconds() + 1_000_000_000L;
        var buffer = new byte[4096];
        while (true)
        {
            var remaining = deadline - MonotonicNowNanoseconds();
            if (remaining <= 0) { error = "final drain deadline expired"; return false; }
            var poll = new[] { new NativeLaunchInterop.PollFd { Fd = fd, Events = 1 } };
            var rc = NativeLaunchInterop.Poll(poll, 1, (int)Math.Clamp(remaining / 1_000_000, 1, 100));
            if (rc < 0)
            {
                if (System.Runtime.InteropServices.Marshal.GetLastWin32Error() == NativeLaunchInterop.EINTR) continue;
                error = $"final drain poll errno {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}";
                return false;
            }
            if (rc == 0) continue;
            var read = NativeLaunchInterop.Read(fd, buffer, (nuint)Math.Min(buffer.Length, maximum + 1 - (int)output.Length));
            if (read < 0 && System.Runtime.InteropServices.Marshal.GetLastWin32Error() == NativeLaunchInterop.EINTR) continue;
            if (read < 0) { error = "final drain read failed"; return false; }
            if (read == 0) { bytes = output.ToArray(); return true; }
            if (output.Length + read > maximum) { error = "bounded status/capture stream overflow"; return false; }
            output.Write(buffer, 0, (int)read);
        }
    }

    private static bool TryReadAvailable(int fd, int maximum, out byte[] bytes, out string error)
    {
        bytes = Array.Empty<byte>();
        error = string.Empty;
        using var output = new MemoryStream(Math.Min(maximum, 4096));
        var buffer = new byte[4096];
        while (true)
        {
            var poll = new[] { new NativeLaunchInterop.PollFd { Fd = fd, Events = 1 } };
            var rc = NativeLaunchInterop.Poll(poll, 1, 0);
            if (rc < 0)
            {
                if (System.Runtime.InteropServices.Marshal.GetLastWin32Error() == NativeLaunchInterop.EINTR) continue;
                error = $"capture poll errno {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}";
                return false;
            }
            if (rc == 0) { bytes = output.ToArray(); return true; }
            var read = NativeLaunchInterop.Read(fd, buffer, (nuint)Math.Min(buffer.Length, maximum + 1 - (int)output.Length));
            if (read < 0)
            {
                if (System.Runtime.InteropServices.Marshal.GetLastWin32Error() == NativeLaunchInterop.EINTR) continue;
                error = $"capture read errno {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}";
                return false;
            }
            if (read == 0) { bytes = output.ToArray(); return true; }
            if (output.Length + read > maximum) { error = "setup capture exceeded its fixed stream budget"; return false; }
            output.Write(buffer, 0, (int)read);
        }
    }

    private static int Fd(SafeHandle handle) => checked((int)handle.DangerousGetHandle());

    private static void Dispose(ref SafeFileHandle? handle)
    {
        handle?.Dispose();
        handle = null;
    }

    private static long MonotonicNowNanoseconds()
    {
        var timestamp = Stopwatch.GetTimestamp();
        return (timestamp / Stopwatch.Frequency * 1_000_000_000L)
            + (timestamp % Stopwatch.Frequency * 1_000_000_000L / Stopwatch.Frequency);
    }

    private sealed class LaunchDomainClock : IDomainClock
    {
        public long NowNanoseconds => MonotonicNowNanoseconds();
        public void Delay(TimeSpan duration) => Thread.Sleep(duration);
    }
}

internal static class CoordinatorMountVerifier
{
    private static readonly HashSet<string> Expected = new(StringComparer.Ordinal)
    { "/", "/tool", "/input", "/coordinator", "/proc", "/tmp", "/dev/shm" };

    internal static bool Validate(string mountInfo, out string error)
    {
        error = string.Empty;
        var found = new Dictionary<string, (string FsType, string MountOptions, string SuperOptions)>(StringComparer.Ordinal);
        foreach (var line in mountInfo.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = line.IndexOf(" - ", StringComparison.Ordinal);
            if (separator < 0) { error = "malformed coordinator mount inventory"; return false; }
            var left = line[..separator].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var right = line[(separator + 3)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (left.Length < 6 || right.Length < 3) { error = "truncated coordinator mount record"; return false; }
            var point = UnescapeMountPoint(left[4]);
            var fs = right[0];
            var options = left[5];
            var super = right[2];
            if (found.ContainsKey(point)) { error = $"duplicate mount point in coordinator inventory: {point}"; return false; }
            found.Add(point, (fs, options, super));
        }
        if (!Expected.SetEquals(found.Keys)) { error = "sandbox mount inventory differs from the exact WP04 tree"; return false; }

        foreach (var point in new[] { "/", "/tool", "/input", "/coordinator", "/proc" })
        {
            if (!found[point].MountOptions.Split(',').Contains("ro", StringComparer.Ordinal))
            { error = $"required read-only mount is writable: {point}"; return false; }
        }
        if (found["/"].FsType != "tmpfs" || found["/proc"].FsType != "proc"
            || found["/tmp"].FsType != "tmpfs" || found["/dev/shm"].FsType != "tmpfs")
        { error = "sandbox mount filesystem type mismatch"; return false; }
        foreach (var point in new[] { "/tmp", "/dev/shm" })
        {
            if (!found[point].MountOptions.Split(',').Contains("rw", StringComparer.Ordinal)
                || !found[point].MountOptions.Split(',').Contains("nodev", StringComparer.Ordinal)
                || !found[point].MountOptions.Split(',').Contains("nosuid", StringComparer.Ordinal))
            { error = $"scratch mount policy mismatch: {point}"; return false; }
        }
        if (!found["/tmp"].SuperOptions.Split(',').Contains("size=7340032", StringComparer.Ordinal)
            || !found["/dev/shm"].SuperOptions.Split(',').Contains("size=1048576", StringComparer.Ordinal))
        { error = "scratch tmpfs capacity differs from Section 11"; return false; }
        return true;
    }

    private static string UnescapeMountPoint(string value) => value
        .Replace("\\040", " ", StringComparison.Ordinal)
        .Replace("\\011", "\t", StringComparison.Ordinal)
        .Replace("\\012", "\n", StringComparison.Ordinal)
        .Replace("\\134", "\\", StringComparison.Ordinal);
}
