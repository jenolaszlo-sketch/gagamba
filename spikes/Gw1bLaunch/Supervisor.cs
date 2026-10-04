// Supervisor sweep prototype: find engine-job descendants via ToolHelp
// parent-PID walks (the launch API returns no job handle) and terminate them
// with PID + creation-time identity, so reused PIDs are never touched.
// Shared by the unsandboxed control and the sandboxed leg: the mechanism is
// identical, only the target tree differs.
using System.Runtime.InteropServices;

namespace Gagamba.Spikes.Gw1bLaunch;

internal sealed record TreeMember(uint Pid, uint Ppid, string Exe);

internal static class Supervisor
{
    /// <summary>All descendants of rootPid (any depth), excluding PID 0/4 and self.</summary>
    public static List<TreeMember> DescendantsOf(uint rootPid)
    {
        var all = SnapshotProcesses();
        var children = new Dictionary<uint, List<TreeMember>>();
        foreach (var m in all)
        {
            if (!children.TryGetValue(m.Ppid, out var list))
                children[m.Ppid] = list = [];
            list.Add(m);
        }
        var found = new List<TreeMember>();
        var seen = new HashSet<uint>();
        var queue = new Queue<uint>();
        queue.Enqueue(rootPid);
        uint self = (uint)Environment.ProcessId;
        while (queue.Count > 0)
        {
            uint parent = queue.Dequeue();
            if (!children.TryGetValue(parent, out var kids))
                continue;
            foreach (var k in kids)
            {
                if (k.Pid is 0 or 4 || k.Pid == self || !seen.Add(k.Pid))
                    continue;
                found.Add(k);
                queue.Enqueue(k.Pid);
            }
        }
        return found;
    }

    /// <summary>
    /// Terminates cmd.exe descendants created at/after <paramref name="sinceUtc"/>
    /// (minus a small tolerance). Creation time is read from the same handle
    /// used to terminate, so it is never trusted from the snapshot. Returns
    /// (found, killed, notes).
    /// </summary>
    public static (int Found, int Killed, List<string> Notes) SweepDescendants(
        uint rootPid, DateTime sinceUtc, TimeSpan stopBudget)
    {
        var notes = new List<string>();
        var found = DescendantsOf(rootPid);
        int killed = 0;
        foreach (var m in found)
        {
            // Blast-radius bound: only cmd.exe descendants of our tree are ever
            // touched. Anything else is reported, never killed.
            if (!string.Equals(m.Exe, "cmd.exe", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add($"skip {m.Pid} ({m.Exe}): not cmd.exe");
                continue;
            }
            IntPtr h = Native.OpenProcess(
                Native.PROCESS_TERMINATE | Native.PROCESS_QUERY_LIMITED_INFORMATION | Native.SYNCHRONIZE,
                false, m.Pid);
            if (h == IntPtr.Zero)
            {
                notes.Add($"skip {m.Pid}: already gone");
                continue;
            }
            try
            {
                if (!Native.GetProcessTimes(h, out var c, out _, out _, out _))
                {
                    notes.Add($"skip {m.Pid}: no times (exiting?)");
                    continue;
                }
                DateTime created = DateTime.FromFileTimeUtc((long)(((ulong)c.dwHighDateTime << 32) | c.dwLowDateTime));
                if (created < sinceUtc - TimeSpan.FromSeconds(5))
                {
                    notes.Add($"skip {m.Pid} ({m.Exe}): predates launch window");
                    continue;
                }
                if (!Native.TerminateProcess(h, 99))
                {
                    notes.Add($"kill {m.Pid} ({m.Exe}) term-api=False err={Marshal.GetLastWin32Error()}");
                    continue;
                }
                uint wait = Native.WaitForSingleObject(h, (uint)stopBudget.TotalMilliseconds);
                bool gotCode = Native.GetExitCodeProcess(h, out uint code);
                if (wait == Native.WAIT_OBJECT_0 && gotCode && code != Native.STILL_ACTIVE)
                {
                    killed++;
                    notes.Add($"killed {m.Pid} ({m.Exe}) exit={code}");
                }
                else
                {
                    notes.Add($"kill {m.Pid} ({m.Exe}) unconfirmed wait=0x{wait:X} gotCode={gotCode}");
                }
            }
            finally
            {
                try { Native.CloseHandle(h); } catch { }
            }
        }
        return (found.Count, killed, notes);
    }

    /// <summary>
    /// Debug dump: cmd.exe rows (pid/ppid/age), always including focusPid if
    /// present. Ages are resolved for the printed rows only.
    /// </summary>
    public static string DescribeCmdTable(uint focusPid)
    {
        try
        {
            var rows = SnapshotProcesses()
                .Where(m => m.Exe.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase))
                .ToList();
            // Keep the focus process visible even when the table is truncated.
            rows = rows.OrderBy(m => m.Pid).ToList();
            var show = rows.Where(m => m.Pid != focusPid).OrderBy(m => m.Pid).Take(11).ToList();
            var focus = rows.FirstOrDefault(m => m.Pid == focusPid);
            if (focus is not null)
                show.Insert(0, focus);
            var now = DateTime.UtcNow;
            return string.Join(",", show.Select(m =>
            {
                DateTime created = TryCreation(m.Pid);
                string age = created == DateTime.MinValue ? "?" : $"{(now - created).TotalSeconds:F0}s";
                return $"{m.Pid}/{m.Ppid}/{age}{(m.Pid == focusPid ? "*" : "")}";
            }));
        }
        catch (Exception ex)
        {
            return $"snapshot-fault:{ex.GetType().Name}";
        }
    }

    /// <summary>Cheap snapshot: pid/ppid/exe only, one toolhelp handle total.</summary>
    private static List<TreeMember> SnapshotProcesses()
    {
        IntPtr snap = Native.CreateToolhelp32Snapshot(Native.TH32CS_SNAPPROCESS, 0);
        if (snap == new IntPtr(-1))
            throw new InvalidOperationException($"snapshot failed err={Marshal.GetLastWin32Error()}");
        try
        {
            var members = new List<TreeMember>();
            var entry = new Native.ProcessEntry32 { dwSize = (uint)Marshal.SizeOf<Native.ProcessEntry32>() };
            if (!Native.Process32First(snap, ref entry))
                return members;
            do
            {
                members.Add(new(entry.th32ProcessID, entry.th32ParentProcessID, entry.szExeFile));
            } while (Native.Process32Next(snap, ref entry));
            return members;
        }
        finally
        {
            try { Native.CloseHandle(snap); } catch { }
        }
    }

    private static DateTime TryCreation(uint pid)
    {
        IntPtr h = Native.OpenProcess(Native.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (h == IntPtr.Zero)
            return DateTime.MinValue;
        try
        {
            return Native.GetProcessTimes(h, out var c, out _, out _, out _)
                ? DateTime.FromFileTimeUtc((long)(((ulong)c.dwHighDateTime << 32) | c.dwLowDateTime))
                : DateTime.MinValue;
        }
        finally
        {
            try { Native.CloseHandle(h); } catch { }
        }
    }
}
