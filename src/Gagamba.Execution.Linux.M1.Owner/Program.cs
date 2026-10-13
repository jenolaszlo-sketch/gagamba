// WP03 per-execution owner process. It accepts only an inherited private
// bootstrap (inherited stdio pipes plus one SCM_RIGHTS handle channel),
// receives the primary DomainLease descriptors from the recovery scope,
// verifies the domain identity, acquires the exclusive journal writer
// lock, and can be commanded or terminated at a named barrier to exercise
// owner-death and orphan-adoption recovery. It never receives release
// authority and never launches a target.
using System.Diagnostics;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;

var parsed = ParseArgs(args);

static Dictionary<string, string> ParseArgs(string[] a)
{
    var map = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var i = 0; i < a.Length; i++)
    {
        if (a[i].StartsWith("--", StringComparison.Ordinal) && i + 1 < a.Length)
        {
            map[a[i]] = a[i + 1];
            i++;
        }
    }

    return map;
}

void Emit(string line)
{
    Console.Out.WriteLine(line);
    Console.Out.Flush();
}

if (!OperatingSystem.IsLinux())
{
    Emit("ERR not-linux");
    return 2;
}

var bootstrapFdText = Environment.GetEnvironmentVariable("GAGAMBA_BOOTSTRAP_FD");
if (bootstrapFdText is null || !int.TryParse(bootstrapFdText, out var bootstrapFd))
{
    Emit("ERR missing-handle-channel");
    return 3;
}

var map = new BootstrapMap(
    OwnerBootstrap.CurrentVersion,
    new[]
    {
        new BootstrapDescriptor(0, BootstrapRole.ControlRead),
        new BootstrapDescriptor(1, BootstrapRole.ControlWrite),
        new BootstrapDescriptor(2, BootstrapRole.RecoveryEvidence),
        new BootstrapDescriptor(bootstrapFd, BootstrapRole.HandleChannel),
    });
if (!OwnerBootstrap.TryValidate(map, out var bootstrapError))
{
    Emit($"ERR bootstrap {bootstrapError}");
    return 3;
}

Emit("BOOTSTRAP_OK");

var barrier = parsed.GetValueOrDefault("--barrier", "none");
if (barrier == "before-receive")
{
    PreparationInterop.CloseFd(bootstrapFd);
    Emit("BARRIER before-receive");
    return 0;
}

var execution = Guid.Parse(parsed["--exec"]);
var leaseGen = Guid.Parse(parsed["--lease"]);
var domainName = parsed["--domain"];

// Receive the strict transfer, then close the bootstrap-only endpoint.
var received = FdPassing.TryReceiveTransfer(
    bootstrapFd,
    DomainHandleTransfer.ExactDescriptorCount,
    DomainHandleTransfer.ExactPayloadBytes,
    out var primaryFds,
    out var transferPayload,
    out var recvError);
PreparationInterop.CloseFd(bootstrapFd);
if (!received)
{
    Emit($"ERR receive {recvError}");
    return 4;
}

if (!DomainHandleTransfer.TryDecode(transferPayload, execution, leaseGen, out _, out var decodeError))
{
    foreach (var fd in primaryFds)
    {
        PreparationInterop.CloseFd(fd);
    }

    Emit($"ERR transfer {decodeError}");
    return 4;
}

if (!CgroupInterop.TryReadBootId(out var bootId, out var bootError))
{
    foreach (var fd in primaryFds)
    {
        PreparationInterop.CloseFd(fd);
    }

    Emit($"ERR boot-id {bootError}");
    return 5;
}

if (!CgroupInterop.TryValidateDomainHandles(
    primaryFds, domainName, execution, leaseGen, bootId,
    out var backend, out var identity, out var validateError))
{
    foreach (var fd in primaryFds)
    {
        PreparationInterop.CloseFd(fd);
    }

    Emit($"ERR validate {validateError}");
    return 5;
}

var lease = new DomainLease(backend!, identity!);
Emit($"PRIMARY dev={identity!.ChildDevice} ino={identity.ChildInode} mount={identity.CgroupMountId}");

if (barrier == "after-receive")
{
    Emit("BARRIER after-receive");
    return 0;
}

var journalDir = parsed["--journal-dir"];
var storage = new JournalFileStorage();
var journal = new OwnershipJournal(storage, journalDir);
if (!journal.TryAcquireWriter(out var writerLease, out var lockError))
{
    Emit($"ERR writer-lock {lockError}");
    return 6;
}

using (writerLease!)
{
    Emit("WRITER_LOCKED");
    if (barrier == "after-lock")
    {
        Emit("BARRIER after-lock");
        return 0;
    }

    if (barrier == "before-registered")
    {
        Emit("BARRIER before-registered");
        return 0;
    }

    var registered = new OwnershipSnapshot(
        OwnershipSnapshotCodec.CurrentVersion, 0, execution, bootId, identity.CgroupMountId, identity.CgroupMountFsType,
        identity.ParentDevice, identity.ParentInode, domainName, identity.ChildDevice, identity.ChildInode, leaseGen,
        OwnershipWriterKind.Owner, OwnershipCleanupState.NotStarted, PreparationStage.AllocationRegistered,
        StagingParentDevice: 0, StagingParentInode: 0,
        Allocations: new[] { new AllocationEntry(AllocationKind.Domain, domainName, 0, Array.Empty<byte>(), 0, 0, true) }, Preparation: null);
    var registeredWrite = journal.WriteNext(writerLease, registered);
    Emit($"REGISTERED ok={registeredWrite.Ok} err={registeredWrite.Error}");

if (barrier == "after-registered")
{
    Emit("BARRIER after-registered");
    return 0;
}

string? line;
while ((line = Console.In.ReadLine()) is not null)
{
    var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    switch (parts[0])
    {
        case "stop":
            var stop = lease.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new OwnerClock());
            Emit($"STOPPED ok={stop.Stopped} err={stop.Error}");
            break;
        case "empty":
            var pop = lease.ObservePopulation();
            Emit($"POPULATION known={pop.Known} empty={pop.Empty}");
            break;
        case "identity":
            Emit($"IDENTITY match={lease.VerifyIdentity().Match}");
            break;
        case "orphan":
            // Spawn a finite trusted child, report it, then exit so it is
            // orphaned to the subreaper scope.
            var seconds = parts.Length > 1 ? parts[1] : "2";
            var child = Process.Start("/bin/sleep", seconds);
            Emit($"ORPHAN_PID {child!.Id}");
            return 0;
        case "die":
            Emit("DYING");
            return 0;
    }
}

Emit("CONTROL_EOF");
return 0;
}

internal sealed class OwnerClock : IDomainClock
{
    private readonly Stopwatch _watch = Stopwatch.StartNew();

    public long NowNanoseconds => _watch.ElapsedTicks * (1_000_000_000L / Stopwatch.Frequency);

    public void Delay(TimeSpan duration) => Thread.Sleep(duration);
}
