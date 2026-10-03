// GP-1B owned-tree helpers: node ownership records, stop, survivor sweep.
// PID identity always pairs the numeric PID with the process start time so a
// reused PID is never mistaken for a surviving worker. Unsandboxed host only;
// kernel-backed ownership arrives with the backend spikes.
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Gagamba.Fixture.Host;

public sealed record NodeRecord(
    int Pid,
    int ClaimedPpid,
    int Depth,
    bool Orphan,
    string RunId,
    string WorkerId,
    DateTime StartedUtc);

public sealed record SurvivorInfo(int Pid, string Detail);

public static class TreeRunner
{
    public static TimeSpan StopBudget = TimeSpan.FromSeconds(5);

    public static List<NodeRecord> ReadNodes(string workspaceRoot)
    {
        var records = new List<NodeRecord>();
        string nodesDir = Path.Combine(workspaceRoot, "nodes");
        if (!Directory.Exists(nodesDir))
            return records;
        foreach (string file in Directory.GetFiles(nodesDir, "*.json"))
        {
            JsonNode? node;
            try { node = JsonNode.Parse(File.ReadAllText(file)); }
            catch (Exception ex) { throw new InvalidOperationException($"unparseable node file {Path.GetFileName(file)}: {ex.Message}"); }
            var obj = node as JsonObject ?? throw new InvalidOperationException($"bad node envelope {Path.GetFileName(file)}");
            records.Add(new(
                obj["pid"]?.GetValue<int>() ?? throw new InvalidOperationException("node missing pid"),
                obj["claimedPpid"]?.GetValue<int>() ?? -1,
                obj["depth"]?.GetValue<int>() ?? throw new InvalidOperationException("node missing depth"),
                obj["orphan"]?.GetValue<bool>() ?? false,
                obj["runId"]?.GetValue<string>() ?? throw new InvalidOperationException("node missing runId"),
                obj["workerId"]?.GetValue<string>() ?? "?",
                DateTime.Parse(obj["startedUtc"]?.GetValue<string>() ?? throw new InvalidOperationException("node missing startedUtc"),
                    null, System.Globalization.DateTimeStyles.RoundtripKind)));
        }
        return records;
    }

    public static async Task<List<NodeRecord>> WaitForNodesAsync(
        string workspaceRoot, int expectedCount, TimeSpan deadline, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var nodes = ReadNodes(workspaceRoot);
            if (nodes.Count >= expectedCount)
                return nodes;
            await Task.Delay(100, ct);
        }
        return ReadNodes(workspaceRoot);
    }

    public static Process Launch(string workerPath, string workerArgs)
    {
        var (fileName, arguments) = FixtureRunner.BuildLaunch(workerPath, workerArgs);
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        return Process.Start(psi) ?? throw new InvalidOperationException("tree root start returned null");
    }

    /// <summary>
    /// Stops the owned tree: tree-kill the root, then sweep node-recorded PIDs
    /// (catches orphans whose parent already exited). Returns live survivors.
    /// </summary>
    public static async Task<List<SurvivorInfo>> StopTreeAsync(
        Process? root, string workspaceRoot, TimeSpan? budget = null)
    {
        TimeSpan limit = budget ?? StopBudget;
        if (root is not null)
        {
            try
            {
                if (!root.HasExited)
                    root.Kill(entireProcessTree: true);
            }
            catch { /* already dead or denying; sweep decides */ }
        }

        var sw = Stopwatch.StartNew();
        List<SurvivorInfo> alive = CheckSurvivors(workspaceRoot);
        // Give tree-kill a moment, then escalate to per-record kills for stragglers.
        while (alive.Count > 0 && sw.Elapsed < limit)
        {
            foreach (var s in alive)
                TryKillPid(s.Pid);
            await Task.Delay(200);
            alive = CheckSurvivors(workspaceRoot);
        }
        // Final escalation: one more direct pass, then report.
        foreach (var s in alive)
            TryKillPid(s.Pid);
        await Task.Delay(300);
        return CheckSurvivors(workspaceRoot);
    }

    /// <summary>Check-only survivor sweep. Never kills. PID reuse safe via start-time match.</summary>
    public static List<SurvivorInfo> CheckSurvivors(string workspaceRoot)
    {
        var alive = new List<SurvivorInfo>();
        foreach (var node in ReadNodes(workspaceRoot))
        {
            Process? proc;
            try { proc = Process.GetProcessById(node.Pid); }
            catch (ArgumentException) { continue; } // no such PID: dead, not a survivor
            using (proc)
            {
                DateTime actualStart;
                try
                {
                    if (proc.HasExited)
                        continue;
                    actualStart = proc.StartTime.ToUniversalTime();
                }
                catch { continue; } // exited/denied meanwhile: not provably ours
                if (Math.Abs((actualStart - node.StartedUtc).TotalSeconds) > 3)
                    continue; // PID reused by another process: explicitly not ours
                alive.Add(new(node.Pid, $"workerId={node.WorkerId} depth={node.Depth} orphan={node.Orphan}"));
            }
        }
        return alive;
    }

    private static void TryKillPid(int pid)
    {
        try
        {
            using var proc = Process.GetProcessById(pid);
            if (!proc.HasExited)
                proc.Kill();
        }
        catch { /* dead, reused, or denied: CheckSurvivors is authoritative */ }
    }
}
