namespace Gagamba.Execution.Linux;

// A cgroup stays owned until population and removal have both been confirmed.
internal sealed class OwnedCgroup
{
    public string Path { get; }
    public bool IsRemoved => _removed;
    internal Func<string, bool>? FaultForTest { get; set; }
    private volatile bool _removed;
    private readonly SemaphoreSlim _cleanupGate = new(1, 1);
    private OwnedCgroup(string path) => Path = path;
    internal static OwnedCgroup Adopt(string path) => new(path);

    public static (OwnedCgroup? Group, string Error) Create(string parent)
    {
        string path = System.IO.Path.Combine(parent, "gagamba-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(path);
            if (!File.Exists(System.IO.Path.Combine(path, "cgroup.kill")))
            {
                Directory.Delete(path);
                return (null, $"not a cgroup v2 mount at '{parent}' (no cgroup.kill)");
            }
            return (new OwnedCgroup(path), "");
        }
        catch (Exception ex)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path); } catch { }
            return (null, $"cgroup delegation unavailable at '{parent}': {ex.GetType().Name}: {ex.Message}");
        }
    }

    public (bool Ok, string Error) Kill()
    {
        if (_removed) return (true, "");
        if (FaultForTest?.Invoke("kill") == true) return (false, "cgroup.kill fault: injected denial");
        try
        {
            File.WriteAllText(System.IO.Path.Combine(Path, "cgroup.kill"), "1");
            return (true, "");
        }
        catch (Exception ex) { return (false, $"cgroup.kill fault: {ex.GetType().Name}: {ex.Message}"); }
    }

    public (bool Empty, string Error) Population()
    {
        if (_removed) return (true, "");
        if (FaultForTest?.Invoke("read") == true)
            return (false, "cgroup.events read fault: injected denial");
        string events = System.IO.Path.Combine(Path, "cgroup.events");
        try
        {
            string[] lines = File.ReadAllLines(events);
            foreach (string line in lines)
            {
                if (line == "populated 0") return (true, "");
                if (line == "populated 1") return (false, "");
            }
            return (false, $"cgroup.events lacks populated at '{Path}'");
        }
        catch (Exception ex) { return (false, $"cgroup.events read fault at '{Path}': {ex.GetType().Name}: {ex.Message}"); }
    }

    public async Task<string?> StopAndRemoveAsync(bool kill, TimeSpan timeout)
    {
        await _cleanupGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_removed) return null;
            if (kill)
            {
                var killed = Kill();
                if (!killed.Ok) return killed.Error;
            }
            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                var population = Population();
                if (population.Error.Length != 0) return population.Error;
                if (population.Empty) break;
                if (timer.Elapsed >= timeout) return $"cgroup population did not empty within {timeout.TotalSeconds}s at '{Path}'";
                await Task.Delay(50).ConfigureAwait(false);
            }
            try
            {
                if (FaultForTest?.Invoke("remove") == true)
                    return "cgroup remove fault: injected denial";
                foreach (string dir in Directory.EnumerateDirectories(Path, "*", SearchOption.AllDirectories)
                    .OrderByDescending(dir => dir.Length)) Directory.Delete(dir);
                Directory.Delete(Path);
                _removed = true;
                return null;
            }
            catch (Exception ex) { return $"cgroup remove fault at '{Path}': {ex.GetType().Name}: {ex.Message}"; }
        }
        finally { _cleanupGate.Release(); }
    }
}
