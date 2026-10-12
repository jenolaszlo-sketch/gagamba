// WP02 identity-checked staging cleanup (G2-07): a staged object may be
// removed only when the current directory entry still is the exact object
// this preparation created. The check resolves relative to the retained
// trusted staging-directory descriptor, never follows symlinks, compares
// the entry's (device, inode) to the originally retained identity, and
// reports truthful outcomes instead of silently dropping authority.
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Preparation;

internal enum CleanupItemOutcome : byte
{
    Removed = 0,
    AlreadyAbsent = 1,
    IdentityMismatch = 2,
    InspectionFailed = 3,
    RemovalFailed = 4,
}

internal readonly record struct CleanupItemResult(CleanupItemOutcome Outcome, string Name, string Error)
{
    internal bool Clean => Outcome is CleanupItemOutcome.Removed or CleanupItemOutcome.AlreadyAbsent;
}

/// <summary>Aggregate staging-cleanup report. Confirmed only when every
/// item is Removed/AlreadyAbsent; any mismatch/failure retains cleanup
/// debt.</summary>
internal sealed record StagingCleanupReport(IReadOnlyList<CleanupItemResult> Items)
{
    internal bool Confirmed => Items.All(i => i.Clean);

    internal bool DebtRetained => !Confirmed;

    internal static StagingCleanupReport Empty { get; } = new(Array.Empty<CleanupItemResult>());
}

/// <summary>Narrow WP02 checked-cleanup primitive. WP03's reconciler must
/// adopt this rather than duplicate unsafe unlink logic (G2-08).</summary>
internal static class StagingCleanup
{
    /// <summary>Identity-checked removal of one staging entry. The parent
    /// handle is the retained staging directory; (device, inode) is the
    /// identity captured when the object was created.</summary>
    internal static CleanupItemResult CleanOne(
        ICopyHost host, SafeHandle parent, string name, ulong device, ulong inode)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentException.ThrowIfNullOrEmpty(name);

        // Resolve the current entry beneath the retained directory only.
        var openErrno = host.OpenSource(parent, name, out var entry);
        if (openErrno == PreparationInterop.ENOENT)
        {
            return new CleanupItemResult(CleanupItemOutcome.AlreadyAbsent, name, string.Empty);
        }

        if (openErrno == PreparationInterop.ELOOP)
        {
            // A symlink now occupies the name: never unlink it.
            return new CleanupItemResult(CleanupItemOutcome.IdentityMismatch, name, "name is now a symlink");
        }

        if (openErrno != 0 || entry is null)
        {
            return new CleanupItemResult(CleanupItemOutcome.InspectionFailed, name, $"open errno {openErrno}");
        }

        try
        {
            if (host.StatHandle(entry, out var identity) != 0)
            {
                return new CleanupItemResult(CleanupItemOutcome.InspectionFailed, name, "stat failed");
            }

            if (identity.Device != device || identity.Inode != inode)
            {
                return new CleanupItemResult(CleanupItemOutcome.IdentityMismatch, name, "entry identity does not match the created object");
            }
        }
        finally
        {
            entry.Dispose();
        }

        var unlinkErrno = host.Unlink(parent, name);
        return unlinkErrno == 0
            ? new CleanupItemResult(CleanupItemOutcome.Removed, name, string.Empty)
            : new CleanupItemResult(CleanupItemOutcome.RemovalFailed, name, $"unlink errno {unlinkErrno}");
    }

    internal static StagingCleanupReport CleanAll(
        ICopyHost host, SafeHandle parent, IReadOnlyList<(string Name, ulong Device, ulong Inode)> entries)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(entries);
        var results = new List<CleanupItemResult>(entries.Count);
        foreach (var (name, device, inode) in entries)
        {
            results.Add(CleanOne(host, parent, name, device, inode));
        }

        return new StagingCleanupReport(results);
    }
}
