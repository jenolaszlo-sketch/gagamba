using System.Runtime.InteropServices;
using Gagamba.Execution;
using Gagamba.Execution.Linux;

var results = new List<object>();
string parent = "/sys/fs/cgroup/gagamba-review-" + Guid.NewGuid().ToString("N");
string ws = Path.Combine(Path.GetTempPath(), "gagamba-review-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(parent); Directory.CreateDirectory(ws);
try {
    await using var p = new LinuxExecutionProvider(parent);
    ExecutionHandle Start(string script) {
        var prep = p.Prepare(new ExecutionRequirements(Array.Empty<ExecutionRequirement>()));
        if (prep is not PrepareResult.Accepted a) throw new Exception(prep.ToString());
        var launched = p.Launch(a.Prepared, new ProcessStartSpec("/bin/sh", "-c '" + script + "'", ws, new Dictionary<string,string>{{"PATH","/usr/bin:/bin"}}));
        if (launched is not LaunchResult.Started s) throw new Exception(launched.ToString());
        return s.Handle;
    }
    // E8: Root status must survive cancellation of an observation-only wait.
    var h = Start("/bin/sleep 2 & exit 17");
    bool cancelled = false;
    using (var token = new CancellationTokenSource(250)) {
        try { await p.WaitForCompletionAsync(h, token.Token); }
        catch (OperationCanceledException) { cancelled = true; }
    }
    await Task.Delay(2300);
    using (var token = new CancellationTokenSource(5000)) {
        results.Add(new { experiment = "E8_linux_cancel_resume", cancelled, result = (await p.WaitForCompletionAsync(h, token.Token)).ToString() });
    }
    // E9: Inheritable descriptor contains only a disposable sentinel.
    string sentinel = Path.Combine(ws, "sentinel.txt");
    File.WriteAllText(sentinel, "");
    int fd = Native.open(sentinel, 2);
    if (fd < 0) throw new Exception("sentinel open failed");
    try {
        h = Start("printf inherited > /proc/self/fd/" + fd);
        using var token = new CancellationTokenSource(5000);
        var completed = await p.WaitForCompletionAsync(h, token.Token);
        results.Add(new { experiment = "E9_linux_inherited_fd", fd, contents = File.ReadAllText(sentinel), result = completed.ToString() });
    } finally { Native.close(fd); }
    // E10: Migration stays inside the unique audit parent, never into a host cgroup.
    string escape = Path.Combine(parent, "escape"); Directory.CreateDirectory(escape);
    string childScript = Path.Combine(ws, "escape.sh");
    File.WriteAllText(childScript, "#!/bin/sh\necho $$ > " + escape + "/cgroup.procs\nprintf moved > moved\nsleep 2\nprintf survived > survived\n");
    h = Start("/bin/sh " + childScript + " & /bin/sleep 15");
    var deadline = DateTime.UtcNow.AddSeconds(5);
    while (!File.Exists(Path.Combine(ws,"moved")) && DateTime.UtcNow < deadline) await Task.Delay(20);
    if (!File.Exists(Path.Combine(ws,"moved"))) throw new Exception("migration did not run");
    var term = p.Terminate(h);
    await Task.Delay(2500);
    results.Add(new { experiment = "E10_linux_migration", terminated = term.GetType().Name, wroteAfterTermination = File.Exists(Path.Combine(ws,"survived")), scope = "root host; sibling inside unique audit parent" });
    using (var token = new CancellationTokenSource(5000)) await p.WaitForCompletionAsync(h, token.Token);
} finally {
    // This path is generated here with a fixed prefix and has no user input.
    File.WriteAllText(Path.Combine(parent,"cgroup.kill"), "1");
    await Task.Delay(100);
    foreach (string dir in Directory.EnumerateDirectories(parent,"*",SearchOption.AllDirectories).OrderByDescending(d=>d.Length)) Directory.Delete(dir);
    Directory.Delete(parent);
    Directory.Delete(ws, true);
}
results.Add(new { experiment = "cleanup", auditCgroupRemoved = !Directory.Exists(parent), workspaceRemoved = !Directory.Exists(ws) });
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(results,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
static class Native {
    [DllImport("libc",SetLastError=true)] public static extern int open(string path,int flags);
    [DllImport("libc")] public static extern int close(int fd);
}
