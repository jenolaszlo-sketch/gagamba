// Supervisor sweep prototype: find engine-job descendants via ToolHelp
// parent-PID walks (the launch API returns no job handle) and terminate them
// with PID + creation-time identity, so reused PIDs are never touched.
// Shared by the unsandboxed control and the sandboxed leg: the mechanism is
// identical, only the target tree differs.
using System.Runtime.InteropServices;

namespace Gagamba.Spikes.Gw1bLaunch;

internal sealed record TreeMember(uint Pid, uint Ppid, string Exe, DateTime CreatedUtc);

internal static class Supervisor
{
    /// <summary>All descendants of rootPid (any depth), excluding PID 0/4 and self.</summary>
    public static List<TreeMember> DescendantsOf(uint rootPid)
    {
        var all = Snapshot();
        var children = new Dictionary<uint, List<TreeMember>>();
        foreach (var m in all)
        {
            if (!children.TryGetValue(m.Ppid, out var list))
                children[m.Ppid] = list = [];
            list.Add(m);
        }
        var found = new List<TreeMember>();
        var queue = new Queue<uint>();
        queue.Enqueue(rootPid);
        int self = Environment.ProcessId;
        while (queue.Count > 0)
        {
            uint parent = queue.Dequeue();
            if (!children.TryGetValue(parent, out var kids))
                continue;
            foreach (var k in kids)
            {
                if (k.Pid is 0 or 4 || k.Pid == (uint)self || found.Any(f => f.Pid == k.Pid))
                    continue;
                found.Add(k);
                queue.Enqueue(k.Pid);
            }
        }
        return found;
    }

    /// <summary>
    /// Terminates descendants created at/after <paramref name="sinceUtc"/>
    /// (minus tolerance). Returns (found, killed, notes).
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
            if (m.CreatedUtc < sinceUtc - TimeSpan.FromSeconds(5))
            {
                notes.Add($"skip {m.Pid} ({m.Exe}): predates launch window");
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
                if (Math.Abs((created - m.CreatedUtc).TotalSeconds) > 30)
                {
                    notes.Add($"skip {m.Pid}: snapshot/handle disagree (reuse?)");
                    continue;
                }
                if (!Native.TerminateProcess(h, 99))
                {
                    notes.Add($"kill {m.Pid} ({m.Exe} @{m.CreatedUtc:O}) term-api=False err={Marshal.GetLastWin32Error()}");
                    continue;
                }
                uint wait = Native.WaitForSingleObject(h, (uint)stopBudget.TotalMilliseconds);
                int waitErr = wait == 0xFFFFFFFF ? Marshal.GetLastWin32Error() : 0;
                bool gotCode = Native.GetExitCodeProcess(h, out uint code);
                if (wait == Native.WAIT_OBJECT_0 && gotCode && code != Native.STILL_ACTIVE)
                {
                    killed++;
                    notes.Add($"killed {m.Pid} ({m.Exe} @{m.CreatedUtc:O}) exit={code}");
                }
                else
                {
                    notes.Add($"kill {m.Pid} ({m.Exe} @{m.CreatedUtc:O}) unconfirmed term=True wait=0x{wait:X}(err={waitErr}) gotCode={gotCode}");
                }
            }
            finally
            {
                try { Native.CloseHandle(h); } catch { }
            }
        }
        return (found.Count, killed, notes);
    }

    private static List<TreeMember> Snapshot()
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
                // Creation time needs a handle; snapshot first, resolve lazily.
                // Store DateTime.MinValue here; SweepDescendants re-reads via handle.
                members.Add(new(entry.th32ProcessID, entry.th32ParentProcessID,
                    entry.szExeFile, DateTime.MinValue));
            } while (Native.Process32Next(snap, ref entry));
            // Resolve creation times for candidates only (cheap: open + query).
            var resolved = new List<TreeMember>();
            foreach (var m in members)
            {
                if (m.Pid is 0 or 4)
                {
                    resolved.Add(m);
                    continue;
                }
                IntPtr h = Native.OpenProcess(Native.PROCESS_QUERY_LIMITED_INFORMATION, false, m.Pid);
                if (h == IntPtr.Zero)
                {
                    resolved.Add(m); // dead or denied; sweep treats as gone
                    continue;
                }
                try
                {
                    if (Native.GetProcessTimes(h, out var c, out _, out _, out _))
                        resolved.Add(m with
                        {
                            CreatedUtc = DateTime.FromFileTimeUtc(
                                (long)(((ulong)c.dwHighDateTime << 32) | c.dwLowDateTime)),
                        });
                    else
                        resolved.Add(m);
                }
                finally
                {
                    try { Native.CloseHandle(h); } catch { }
                }
            }
            return resolved;
        }
        finally
        {
            try { Native.CloseHandle(snap); } catch { }
        }
    }
}
