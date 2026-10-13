// WP03 portable test support: in-memory durable-storage and domain
// backends with per-step fault injection, plus an injectable clock.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Recovery;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

internal sealed class FakeJournalStorage : IJournalStorage
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _locks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _calls = new(StringComparer.Ordinal);

    /// <summary>Fault hook keyed by step name; non-null result fails that step.</summary>
    public Func<string, JournalIoResult?>? Fault { get; set; }

    /// <summary>Occurrence-counted faults: fail exactly the Nth invocation
    /// of a step (1-indexed) counted from arming time, so multi-phase
    /// protocols can fault one precise durability operation after any
    /// seeding writes have completed.</summary>
    public HashSet<(string Step, int Occurrence)> FailAtOccurrences { get; } = new();

    public void FailAt(string step, int occurrence)
    {
        _calls.TryGetValue(step, out var consumed);
        FailAtOccurrences.Add((step, consumed + occurrence));
    }

    private JournalIoResult? CheckFault(string step)
    {
        var fault = Fault?.Invoke(step);
        if (fault is { } f) { return f; }
        _calls.TryGetValue(step, out var count);
        _calls[step] = count + 1;
        if (FailAtOccurrences.Contains((step, count + 1)))
        {
            return JournalIoResult.Failure($"{step} fault at occurrence {count + 1}");
        }

        return null;
    }

    public void DirectPut(string directory, string name, byte[] content) =>
        _files[directory + "/" + name] = (byte[])content.Clone();

    public JournalIoResult CreateExclusive(string directory, string name)
    {
        var fault = CheckFault("create");
        if (fault is { } f) { return f; }
        var key = directory + "/" + name;
        if (_files.ContainsKey(key)) { return JournalIoResult.Failure("EEXIST"); }
        _files[key] = Array.Empty<byte>();
        return JournalIoResult.Success;
    }

    public JournalIoResult WriteAll(string directory, string name, byte[] content)
    {
        var fault = CheckFault("write");
        if (fault is { } f) { return f; }
        _files[directory + "/" + name] = (byte[])content.Clone();
        return JournalIoResult.Success;
    }

    public JournalIoResult FsyncFile(string directory, string name)
    {
        var fault = CheckFault("fsyncfile");
        if (fault is { } f) { return f; }
        return JournalIoResult.Success;
    }

    public JournalIoResult Rename(string directory, string sourceName, string targetName)
    {
        var fault = CheckFault("rename");
        if (fault is { } f) { return f; }
        var source = directory + "/" + sourceName;
        if (!_files.ContainsKey(source)) { return JournalIoResult.Failure("ENOENT source"); }
        _files[directory + "/" + targetName] = _files[source];
        _files.Remove(source);
        return JournalIoResult.Success;
    }

    public JournalIoResult FsyncDirectory(string directory)
    {
        var fault = CheckFault("fsyncdir");
        if (fault is { } f) { return f; }
        return JournalIoResult.Success;
    }

    public JournalIoResult Unlink(string directory, string name)
    {
        var fault = CheckFault("unlink");
        if (fault is { } f) { return f; }
        _files.Remove(directory + "/" + name);
        return JournalIoResult.Success;
    }

    public bool Exists(string directory, string name) => _files.ContainsKey(directory + "/" + name);

    public byte[]? ReadAll(string directory, string name)
    {
        if (!_files.TryGetValue(directory + "/" + name, out var value))
        {
            return null;
        }

        if (value.Length > OwnershipSnapshotCodec.MaxSnapshotBytes)
        {
            return null;
        }

        return (byte[])value.Clone();
    }

    public JournalIoResult TryAcquireExclusiveLock(string directory, string name)
    {
        var fault = Fault?.Invoke("lock");
        if (fault is { } f) { return f; }
        var key = directory + "/lock/" + name;
        if (_locks.ContainsKey(key)) { return JournalIoResult.Failure("lock held"); }
        _locks[key] = true;
        return JournalIoResult.Success;
    }

    public JournalIoResult ReleaseExclusiveLock(string directory, string name)
    {
        _locks.Remove(directory + "/lock/" + name);
        return JournalIoResult.Success;
    }

    public bool IsLockHeld(string directory, string name) => _locks.ContainsKey(directory + "/lock/" + name);

    /// <summary>Corrupts a stored file for torn-record tests.</summary>
    public void Corrupt(string directory, string name)
    {
        var key = directory + "/" + name;
        var bytes = _files[key];
        bytes[^1] ^= 0xFF;
    }
}

internal sealed class FakeDomainBackend : IDomainBackend
{
    public ulong ParentDevice { get; set; } = 0xABCD;
    public ulong ParentInode { get; set; } = 100;
    public ulong DomainDevice { get; set; } = 0xABCD;
    public ulong DomainInode { get; set; } = 200;
    public uint DomainMode { get; set; } = 0x4000 | 0x1ED;
    public bool Populated { get; set; }
    public bool KillAccepted { get; set; } = true;
    public bool ReadFault { get; set; }
    public bool KillFault { get; set; }
    public bool RemoveFault { get; set; }
    public bool Replacement { get; set; }
    public ulong ReplacementInode { get; set; } = 999;
    public string ChildName { get; set; } = "domain-1";
    public int KillWrites { get; private set; }
    private bool _disposed;

    public bool HandlesAlive => !_disposed;

    public DomainIoResult WriteKill()
    {
        if (_disposed) { return DomainIoResult.Failure("disposed"); }
        if (KillFault) { return DomainIoResult.Failure("cgroup.kill fault"); }
        KillWrites++;
        if (KillAccepted) { Populated = false; }
        return DomainIoResult.Success;
    }

    public DomainPopulationResult ReadPopulation()
    {
        if (_disposed) { return DomainPopulationResult.Unknown("disposed"); }
        if (ReadFault) { return DomainPopulationResult.Unknown("cgroup.events fault"); }
        return Populated ? DomainPopulationResult.Populated : DomainPopulationResult.Emptied;
    }

    public DomainStatResult StatDomain() => _disposed
        ? DomainStatResult.Failure("disposed")
        : new DomainStatResult(true, DomainDevice, DomainInode, DomainMode, string.Empty);

    public DomainStatResult StatParent() => _disposed
        ? DomainStatResult.Failure("disposed")
        : new DomainStatResult(true, ParentDevice, ParentInode, 0x4000 | 0x1ED, string.Empty);

    public DomainLookupResult LookupChild(string name)
    {
        if (_disposed) { return DomainLookupResult.Failure("disposed"); }
        var inode = Replacement ? ReplacementInode : DomainInode;
        return new DomainLookupResult(true, DomainDevice, inode, DomainMode, string.Empty);
    }

    public DomainIoResult RemoveChild(string name) =>
        RemoveFault ? DomainIoResult.Failure("rmdir fault") : DomainIoResult.Success;

    public IDomainBackend DuplicateForRecovery() => new FakeDomainBackend
    {
        ParentDevice = ParentDevice,
        ParentInode = ParentInode,
        DomainDevice = DomainDevice,
        DomainInode = DomainInode,
        DomainMode = DomainMode,
        Populated = Populated,
        KillAccepted = KillAccepted,
        ReadFault = ReadFault,
        KillFault = KillFault,
        RemoveFault = RemoveFault,
        Replacement = Replacement,
        ReplacementInode = ReplacementInode,
        ChildName = ChildName,
    };

    public void Dispose() => _disposed = true;
}

internal sealed class FakeDomainClock : IDomainClock
{
    public long NowNanoseconds { get; set; }
    public int Delays { get; private set; }

    public void Delay(TimeSpan duration) => NowNanoseconds += (long)(duration.TotalSeconds * 1_000_000_000d);

    public void Advance(TimeSpan duration) => NowNanoseconds += (long)(duration.TotalSeconds * 1_000_000_000d);
}

internal static class RecoveryTestSupport
{
    internal static readonly Guid Execution = new("aaaaaaaa-0000-0000-0000-000000000001");
    internal static readonly Guid Lease = new("bbbbbbbb-0000-0000-0000-000000000002");

    internal static DomainIdentity Identity(
        ulong childInode = 200, string childName = "domain-1", Guid? lease = null) =>
        new(
            Execution, "boot-1", 356, "cgroup2",
            0xABCD, 100, childName, 0xABCD, childInode, lease ?? Lease);

    internal static OwnershipSnapshot Snapshot(
        OwnershipSnapshot? basis = null,
        PreparationStage stage = PreparationStage.AllocationRegistered,
        OwnershipWriterKind writer = OwnershipWriterKind.Owner,
        OwnershipCleanupState cleanup = OwnershipCleanupState.NotStarted,
        IReadOnlyList<AllocationEntry>? allocations = null,
        RecordedPreparation? preparation = null,
        string bootId = "boot-1") =>
        (basis ?? new OwnershipSnapshot(
            OwnershipCodecVersion, 0, Execution, bootId, 356, "cgroup2",
            0xABCD, 100, "domain-1", 0xABCD, 200, Lease,
            writer, cleanup, stage,
            StagingParentDevice: 0xABCD, StagingParentInode: 101,
            Allocations: allocations ?? Array.Empty<AllocationEntry>(), Preparation: preparation)) with { };

    internal const ushort OwnershipCodecVersion = 2;
}
