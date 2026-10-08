using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Gagamba.Execution;
using Gagamba.Execution.Windows;
using Gagamba.Execution.MacOS;

if (args.Length > 0 && args[0] == "sleep") { Thread.Sleep(1500); return; }
var results = new List<object>();
string exe = Environment.ProcessPath!;
var empty = new Dictionary<string, string>();
PreparedExecution Prep(WindowsExecutionProvider p) => ((PrepareResult.Accepted)p.Prepare(new ExecutionRequirements(Array.Empty<ExecutionRequirement>()))).Prepared;
ExecutionHandle Start(WindowsExecutionProvider p) => ((LaunchResult.Started)p.Launch(Prep(p), new ProcessStartSpec(exe, "sleep", AppContext.BaseDirectory, empty))).Handle;

// E1: Measure time until the async API returns its ValueTask, separately from completion.
await using (var p = new WindowsExecutionProvider()) {
    var h = Start(p);
    var sw = Stopwatch.StartNew();
    var pending = p.WaitForCompletionAsync(h);
    long callMs = sw.ElapsedMilliseconds;
    var outcome = await pending;
    results.Add(new { experiment = "E1_async_call_blocks", callMs, outcome = outcome.ToString() });
}

// E2: An ordinary mutable input is used only as a deterministic scheduling barrier.
// It blocks after Launch consumed the preparation, while provider disposal runs.
var raced = new WindowsExecutionProvider();
var prep = Prep(raced);
using var entered = new ManualResetEventSlim();
using var release = new ManualResetEventSlim();
var env = new BarrierDictionary(entered, release);
var launchTask = Task.Run(() => raced.Launch(prep, new ProcessStartSpec(exe, "sleep", AppContext.BaseDirectory, env)));
if (!entered.Wait(5000)) throw new Exception("Launch barrier never entered");
await raced.DisposeAsync();
release.Set();
var launchResult = await launchTask;
var records = (IDictionary)typeof(WindowsExecutionProvider).GetField("_executions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(raced)!;
results.Add(new { experiment = "E2_launch_after_disposal", outcome = launchResult.GetType().Name, recordsAfterDispose = records.Count });
// Test-only cleanup: the public API is disposed and rejects Terminate. Close every
// test-owned job and process handle; the finite child also self-exits after 1.5 s.
foreach (var rec in records.Values) {
    ((IDisposable)rec!.GetType().GetProperty("Job")!.GetValue(rec)!).Dispose();
    Gagamba.Execution.Windows.NativeMethods.CloseHandle((IntPtr)rec.GetType().GetProperty("ProcessHandle")!.GetValue(rec)!);
}
records.Clear();

// E3: Disposal must settle pending completion, not leave it polling invalid handles.
var disposing = new WindowsExecutionProvider();
var running = Start(disposing);
using (var cancel = new CancellationTokenSource(700)) {
    var wait = Task.Run(async () => {
        try { return (await disposing.WaitForCompletionAsync(running, cancel.Token)).GetType().Name; }
        catch (OperationCanceledException) { return "CancelledAfterDisposal"; }
    });
    await Task.Delay(100);
    await disposing.DisposeAsync();
    results.Add(new { experiment = "E3_dispose_during_wait", outcome = await wait });
}

await using (var first = new WindowsExecutionProvider())
await using (var second = new WindowsExecutionProvider()) {
    var foreign = Prep(first);
    results.Add(new { experiment = "E4_foreign_discard", outcome = second.Discard(foreign).GetType().Name });
    first.Discard(foreign);
}

// Pure code probes: these do not establish launchd/cgroup OS behavior on Windows.
string print = new string('x', 2100) + "\nstate = running\nlast exit code = 17\n";
results.Add(new { experiment = "E5_launchd_truncation", fullRunning = Launchd.ParseState(print, 0).Running, truncatedRunning = Launchd.ParseState(print[..2000], 0).Running, observationFailureRunning = Launchd.ParseState("launchctl timed out", 124).Running });

string fakeGroupPath = Path.Combine(AppContext.BaseDirectory, "fake-cgroup-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(fakeGroupPath);
string killFile = Path.Combine(fakeGroupPath, "cgroup.kill");
File.WriteAllText(killFile, "not-written");
var fakeGroup = (IDisposable)Activator.CreateInstance(typeof(Gagamba.Execution.Linux.OwnedCgroup), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { fakeGroupPath }, null)!;
fakeGroup.Dispose();
results.Add(new { experiment = "E6_cgroup_dispose_skips_kill", killContents = File.ReadAllText(killFile), directoryStillExists = Directory.Exists(fakeGroupPath) });
File.Delete(killFile); Directory.Delete(fakeGroupPath);

results.Add(new { experiment = "E7_unix_argument_parser", input = "\"a\\qb\"", output = Gagamba.Execution.Linux.NativeMethods.SplitArguments("\"a\\qb\"") });
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

sealed class BarrierDictionary(ManualResetEventSlim entered, ManualResetEventSlim release) : IReadOnlyDictionary<string, string> {
    private int calls;
    public string this[string key] => throw new KeyNotFoundException();
    public IEnumerable<string> Keys => Array.Empty<string>();
    public IEnumerable<string> Values => Array.Empty<string>();
    public int Count => 0;
    public bool ContainsKey(string key) => false;
    public bool TryGetValue(string key, out string value) { value = ""; return false; }
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
        if (Interlocked.Increment(ref calls) == 1) { entered.Set(); if (!release.Wait(5000)) throw new TimeoutException(); }
        return Enumerable.Empty<KeyValuePair<string, string>>().GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
