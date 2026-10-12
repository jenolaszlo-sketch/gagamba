// WP03 per-execution owner process. It accepts only an inherited private
// bootstrap (mapped to inherited stdio pipes), verifies the supplied
// domain identity beneath the retained parent, acquires the exclusive
// journal writer lock, and records OwnerAccepted. It never receives
// release authority, never launches a target, and can be terminated at a
// named barrier to exercise owner-death recovery.
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

// Bootstrap: the owner accepts exactly the inherited stdio roles.
var map = new BootstrapMap(
    OwnerBootstrap.CurrentVersion,
    new[]
    {
        new BootstrapDescriptor(0, BootstrapRole.ControlRead),
        new BootstrapDescriptor(1, BootstrapRole.ControlWrite),
        new BootstrapDescriptor(2, BootstrapRole.RecoveryEvidence),
    });
if (!OwnerBootstrap.TryValidate(map, out var bootstrapError))
{
    Emit($"ERR bootstrap {bootstrapError}");
    return 3;
}

Emit("BOOTSTRAP_OK");

var barrier = parsed.GetValueOrDefault("--barrier", "none");
if (barrier == "before-open")
{
    Emit("BARRIER before-open");
    return 0;
}

var parent = parsed["--parent"];
var journalDir = parsed["--journal-dir"];
var domainName = parsed["--domain"];
var leaseGen = Guid.Parse(parsed["--lease"]);

if (PreparationInterop.OpenDirectory(parent, out var parentFd) != 0)
{
    Emit("ERR parent-open");
    return 4;
}

try
{
    var openErrno = PreparationInterop.OpenRelative(
        parentFd, domainName,
        PreparationInterop.O_RDONLY | PreparationInterop.O_DIRECTORY
            | PreparationInterop.O_NOFOLLOW | PreparationInterop.O_CLOEXEC,
        PreparationInterop.RequiredResolve, out var domainFd);
    if (openErrno != 0)
    {
        Emit($"ERR domain-open {openErrno}");
        return 5;
    }

    try
    {
        if (PreparationInterop.StatFd(domainFd, out var stat) != 0)
        {
            Emit("ERR domain-stat");
            return 6;
        }

        Emit($"DOMAIN dev={stat.Device} ino={stat.Inode} lease={leaseGen:D}");
        if (barrier == "after-open")
        {
            Emit("BARRIER after-open");
            return 0;
        }

        var storage = new JournalFileStorage();
        var journal = new OwnershipJournal(storage, journalDir);
        var lockResult = journal.TryAcquireExclusiveWriter();
        if (!lockResult.Ok)
        {
            Emit($"ERR writer-lock {lockResult.Error}");
            return 7;
        }

        Emit("WRITER_LOCKED");
        if (barrier == "after-lock")
        {
            Emit("BARRIER after-lock");
            return 0;
        }

        // Block until the control channel closes, then exit normally. This
        // keeps the owner alive as the scope's direct child.
        string? line;
        while ((line = Console.In.ReadLine()) is not null)
        {
            if (line == "die")
            {
                Emit("DYING");
                return 0;
            }
        }

        Emit("CONTROL_EOF");
        return 0;
    }
    finally
    {
        PreparationInterop.CloseFd(domainFd);
    }
}
finally
{
    PreparationInterop.CloseFd(parentFd);
}
