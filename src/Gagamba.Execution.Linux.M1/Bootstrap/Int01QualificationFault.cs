using System.Diagnostics;
using System.Text.Json;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// Trusted experimental input only. This record conveys no OS or release authority.
// Every hook is inert without an explicit, validated internal fixture profile.
internal sealed record Int01QualificationFault(string Case, string EvidenceDirectory);

internal static class Int01Qualification
{
    internal static string Stage(string name) => name switch
    {
        "controller-before-preparation" => "controller-ready",
        "controller-during-preparation" => "owner-before-allocation",
        "recovery-retention-death" => "owner-domain-retained",
        "owner-before-retention" => "owner-before-domain-transfer",
        "owner-mid-staging" => "owner-first-object-accounted",
        "missing-domain-ack" => "recovery-before-domain-ack",
        "missing-staging-ack" => "recovery-before-staging-ack",
        "lost-retention-ack" => "recovery-before-prepared-ack",
        "wrong-descriptor-role-count" or "partial-scm-rights" => "owner-domain-transfer",
        "integrated-stop-before-mint" => "owner-before-mint",
        "controller-release-race" or "recovery-release-race" or "owner-after-intent"
            or "integrated-stop-after-intent-before-write" => "release-after-intent",
        "owner-ambiguous-delivery" => "release-after-write",
        "integrated-lost-release-ack" => "owner-before-release-reply",
        "b4-overflow-blocked" or "b4-recovery-loss-blocked" or "b4-status-backpressure" or "b4-controller-loss-blocked" or "blocked-observer-native-stop" => "owner-before-observation",
        "actual-recovery-release-impersonation" => "recovery-before-rendezvous",
        "integrated-ambiguous-intent" => "owner-intent-storage",
        "b4-store-failure" => "capture-store",
        _ => throw new ArgumentException("Unsupported INT01 qualification fault")
    };

    [System.Runtime.InteropServices.DllImport("libc", EntryPoint="ioctl", SetLastError=true)]
    private static extern int PendingBytes(int fd, uint request, out int value);
    [System.Runtime.InteropServices.DllImport("libc", EntryPoint="fcntl", SetLastError=true)]
    private static extern int PipeSize(int fd, int command);
    internal static void ObserveFullStatusPipe(Int01QualificationFault fault, int fd, long cutoff)
    {
        var capacity = PipeSize(fd, 1032); // Linux F_GETPIPE_SZ; observation only
        var pageSize = Environment.SystemPageSize; const int frameBytes = 96;
        // Atomic 96-byte writes can exhaust pipe page slots before every capacity byte is used.
        var minimumFullBytes = capacity / pageSize * (pageSize / frameBytes) * frameBytes;
        while (OwnerSafetyMonitor.Now() < cutoff)
        {
            if (capacity > 0 && PendingBytes(fd, 0x541b, out var pending) == 0 && pending >= minimumFullBytes)
            { Write(fault, "owner-status-pipe-full", new { Pending = pending, Capacity = capacity, PageSize = pageSize, FrameBytes = frameBytes, MinimumFullBytes = minimumFullBytes }); return; }
            Thread.Sleep(5);
        }
        throw new IOException("B4 status backpressure was not established");
    }

    internal static void Validate(Int01QualificationFault? fault)
    {
        if (fault is null) return;
        _ = Stage(fault.Case);
        ExperimentalRuntimePin.VerifyPrivateDirectory(fault.EvidenceDirectory);
    }

    internal static bool Is(Int01QualificationFault? fault, string name) => fault?.Case == name;

    internal static bool Reached(Int01QualificationFault? fault, string stage)
    {
        if (fault is null || Stage(fault.Case) != stage) return false;
        Write(fault, stage, new { Reached = true });
        return true;
    }

    internal static void Point(Int01QualificationFault? fault, string stage, long cutoff,
        Func<bool>? stopped = null)
    {
        if (!Reached(fault, stage)) return;
        var continuation = Path.Combine(fault!.EvidenceDirectory, "continue-" + stage);
        while (OwnerSafetyMonitor.Now() < cutoff)
        {
            if (File.Exists(continuation) || stopped?.Invoke() == true) return;
            Thread.Sleep(5);
        }
        throw new IOException("INT01 qualification barrier reached original cutoff");
    }

    internal static void Write(Int01QualificationFault fault, string stage, object detail)
    {
        // No secrets, payloads or descriptor contents. These are passive kernel observations.
        var fds = new Dictionary<string, string>();
        foreach (var path in Directory.EnumerateFiles("/proc/self/fd").Take(256))
            try { fds[Path.GetFileName(path)] = new FileInfo(path).LinkTarget ?? "unavailable"; }
            catch (IOException) { }
        var value = new { fault.Case, Stage = stage, Pid = Environment.ProcessId,
            ParentPid = RecoveryInterop.GetParentPid(), AtNs = OwnerSafetyMonitor.Now(),
            Status = File.ReadAllText("/proc/self/status"), Cgroup = File.ReadAllText("/proc/self/cgroup"),
            Fds = fds, Detail = detail };
        var pathFinal = Path.Combine(fault.EvidenceDirectory, "fault-" + stage + ".json");
        var pathTemporary = pathFinal + "." + Environment.ProcessId + ".tmp";
        var encoded=JsonSerializer.Serialize(value);
        if(encoded.Length>65536) throw new InvalidDataException("Qualification evidence bound exceeded" );
        File.WriteAllText(pathTemporary, encoded);
        File.Move(pathTemporary, pathFinal, true);
    }
}
