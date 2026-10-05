// One execution cgroup owned for exactly one activity lifetime. Created
// under the provider's delegated parent; killed via cgroup.kill (never PID
// enumeration); removed when empty at disposal. Deterministic: kill, reap,
// remove exactly once.
namespace Gagamba.Execution.Linux;

internal sealed class OwnedCgroup : IDisposable
{
    public string Path { get; }
    private bool _disposed;

    private OwnedCgroup(string path) => Path = path;

    public static (OwnedCgroup? Group, string Error) Create(string parent)
    {
        string path = System.IO.Path.Combine(parent,
            "gagamba-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(path);
        }
        catch (Exception ex)
        {
            return (null, $"cgroup delegation unavailable at '{parent}': {ex.GetType().Name}");
        }
        if (!File.Exists(System.IO.Path.Combine(path, "cgroup.kill")))
            return (null, $"not a cgroup v2 mount at '{parent}' (no cgroup.kill)");
        return (new OwnedCgroup(path), "");
    }

    /// <summary>Kernel-recursive kill of the cgroup and descendant cgroups.</summary>
    public (bool Ok, string Error) Kill()
    {
        if (_disposed) return (false, "cgroup already removed");
        try
        {
            File.WriteAllText(System.IO.Path.Combine(Path, "cgroup.kill"), "1");
            return (true, "");
        }
        catch (Exception ex)
        {
            return (false, $"cgroup.kill fault: {ex.GetType().Name}");
        }
    }

    /// <summary>Member PIDs currently in this cgroup subtree.</summary>
    public IReadOnlyList<int> Members()
    {
        var out_ = new List<int>();
        try
        {
            foreach (string dir in Directory.EnumerateDirectories(Path, "*", SearchOption.AllDirectories).Prepend(Path))
            {
                string procs = System.IO.Path.Combine(dir, "cgroup.procs");
                if (!File.Exists(procs)) continue;
                foreach (string line in File.ReadAllLines(procs))
                    if (int.TryParse(line.Trim(), out int pid)) out_.Add(pid);
            }
        }
        catch { }
        return out_;
    }

    /// <summary>True when the cgroup has no processes (the domain reached its
    /// terminal state). Uses cgroup.events populated; a missing cgroup counts
    /// as empty, an unreadable one does not.</summary>
    public bool IsEmpty()
    {
        try
        {
            string events = System.IO.Path.Combine(Path, "cgroup.events");
            if (!File.Exists(events)) return true; // cgroup removed
            foreach (string line in File.ReadAllLines(events))
                if (line.StartsWith("populated ", StringComparison.Ordinal))
                    return line.TrimEnd().EndsWith(" 0", StringComparison.Ordinal);
            return false;
        }
        catch { return false; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Best effort in fixed order: kill, then remove emptied dirs.
        try { Kill(); } catch { }
        try
        {
            foreach (string dir in Directory.EnumerateDirectories(Path, "*", SearchOption.AllDirectories)
                         .OrderByDescending(d => d.Length))
            {
                try { Directory.Delete(dir); } catch { }
            }
            Directory.Delete(Path);
        }
        catch { }
        GC.SuppressFinalize(this);
    }
}
