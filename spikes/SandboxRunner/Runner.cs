// SandboxRunner: generic sandbox-resident child launcher and evidence carrier.
// Usage: SandboxRunner.exe [--selftest] | [--env K=V ...] [--] <childExe> [childArgs...]
//   $RUNNER_PID in child args is replaced with the runner's own PID.
//   $HOST_PID is replaced with $SANDBOX_RUNNER_HOST_PID from the environment.
//   --env pairs are applied to the CHILD's environment only (the runner's own
//   environment comes from whatever the sandbox engine provides).
// Always exits 0 with a single JSON document on stdout (launch/observation
// failures are fields, not exit codes), so the outer harness keeps evidence
// even when the topology under test is broken.
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

static string J(string? s, int cap = 4096)
{
    if (s is null) return "null";
    if (s.Length > cap) s = s[..cap] + "...[truncated]";
    var sb = new StringBuilder(s.Length + 2);
    sb.Append('"');
    foreach (char c in s)
    {
        if (c == '\\' || c == '"') { sb.Append('\\'); sb.Append(c); }
        else if (c == '\r') sb.Append("\\r");
        else if (c == '\n') sb.Append("\\n");
        else if (c == '\t') sb.Append("\\t");
        else if (c < 0x20) { sb.Append("\\u"); sb.Append(((int)c).ToString("x4")); }
        else sb.Append(c);
    }
    sb.Append('"');
    return sb.ToString();
}

[DllImport("kernel32.dll", SetLastError = true)]
static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool CloseHandle(IntPtr hObject);

static uint FindParentPid(uint pid)
{
    const uint TH32CS_SNAPPROCESS = 0x00000002;
    IntPtr snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snap == (IntPtr)(-1)) return 0;
    try
    {
        var pe = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };
        if (Process32First(snap, ref pe))
        {
            do { if (pe.th32ProcessID == pid) return pe.th32ParentProcessID; }
            while (Process32Next(snap, ref pe));
        }
    }
    finally { CloseHandle(snap); }
    return 0;
}

int selfPid = Environment.ProcessId;
uint parentPid = 0;
int visible = -1;
try
{
    parentPid = FindParentPid((uint)selfPid);
    visible = Process.GetProcesses().Length;
}
catch { }

// Env as the runner itself sees it (answers whether the engine propagates
// caller env into the sandbox). Values capped; null when absent.
string SeenEnv(string k)
{
    string v = Environment.GetEnvironmentVariable(k);
    if (v is null) return "null";
    if (v.Length > 160) v = v[..160] + "...";
    return J(v);
}
string runnerEnvJson = "\"DOTNET_CLI_HOME\":" + SeenEnv("DOTNET_CLI_HOME")
    + ",\"TMP\":" + SeenEnv("TMP")
    + ",\"TEMP\":" + SeenEnv("TEMP")
    + ",\"SANDBOX_RUNNER_HOST_PID\":" + SeenEnv("SANDBOX_RUNNER_HOST_PID");

if (args.Length == 1 && args[0] == "--selftest")
{
    Console.WriteLine($"{{\"runnerPid\":{selfPid},\"runnerParentPid\":{parentPid},\"visibleProcessCount\":{visible},\"runnerEnv\":{{{runnerEnvJson}}},\"mode\":\"selftest\"}}");
    return 0;
}

// Leading --env K=V pairs (then optional --) apply to the child only.
var childEnv = new System.Collections.Generic.List<(string, string)>();
int ai = 0;
for (; ai < args.Length; ai++)
{
    if (args[ai] == "--env" && ai + 1 < args.Length)
    {
        string pair = args[ai + 1];
        int eq = pair.IndexOf('=');
        if (eq > 0) childEnv.Add((pair[..eq], pair[(eq + 1)..]));
        ai++;
    }
    else if (args[ai] == "--") { ai++; break; }
    else break;
}

if (ai >= args.Length)
{
    Console.WriteLine($"{{\"runnerPid\":{selfPid},\"runnerParentPid\":{parentPid},\"visibleProcessCount\":{visible},\"runnerEnv\":{{{runnerEnvJson}}},\"mode\":\"usage\",\"launchError\":\"no child given\"}}");
    return 0;
}

string hostPid = Environment.GetEnvironmentVariable("SANDBOX_RUNNER_HOST_PID") ?? "0";
string[] childArgs = new string[args.Length - ai - 1];
for (int i = ai + 1; i < args.Length; i++)
    childArgs[i - ai - 1] = args[i].Replace("$RUNNER_PID", selfPid.ToString()).Replace("$HOST_PID", hostPid);

string launchError = null;
int childPid = 0;
bool childVisibleWhileRunning = false;
int childExit = -1;
bool timedOut = false;
string childOut = "";
string childErr = "";
try
{
    var psi = new ProcessStartInfo(args[ai])
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    foreach (var (k, v) in childEnv) psi.Environment[k] = v;
    foreach (string a in childArgs) psi.ArgumentList.Add(a);
    using var child = Process.Start(psi);
    if (child is null) throw new InvalidOperationException("Process.Start returned null");
    childPid = child.Id;
    try { using var q = Process.GetProcessById(childPid); childVisibleWhileRunning = true; }
    catch { childVisibleWhileRunning = false; }
    childOut = "";
    childErr = "";
    var outTask = child.StandardOutput.ReadToEndAsync();
    var errTask = child.StandardError.ReadToEndAsync();
    if (!child.WaitForExit(120_000)) { timedOut = true; try { child.Kill(); } catch { } child.WaitForExit(10_000); }
    try { childOut = await outTask; } catch (Exception ex) { childOut = $"[read-fail {ex.GetType().Name}]"; }
    try { childErr = await errTask; } catch (Exception ex) { childErr = $"[read-fail {ex.GetType().Name}]"; }
    try { childExit = child.HasExited ? child.ExitCode : -1; } catch { }
}
catch (Exception ex) { launchError = $"{ex.GetType().Name}: {ex.Message.Split('\n')[0].Trim()}"; }

string errJson = launchError is null ? "null" : J(launchError, 300);
Console.WriteLine($"{{\"runnerPid\":{selfPid},\"runnerParentPid\":{parentPid},\"visibleProcessCount\":{visible},\"runnerEnv\":{{{runnerEnvJson}}},\"mode\":\"run\",\"childExe\":{J(args[ai])},\"childPid\":{childPid},\"childVisibleWhileRunning\":{childVisibleWhileRunning.ToString().ToLowerInvariant()},\"childExit\":{childExit},\"timedOut\":{timedOut.ToString().ToLowerInvariant()},\"childStdout\":{J(childOut)},\"childStderr\":{J(childErr, 2000)},\"launchError\":{errJson}}}");
return 0;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
struct PROCESSENTRY32
{
    public uint dwSize;
    public uint cntUsage;
    public uint th32ProcessID;
    public IntPtr th32DefaultHeapID;
    public uint th32ModuleID;
    public uint cntThreads;
    public uint th32ParentProcessID;
    public int pcPriClassBase;
    public uint dwFlags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string szExeFile;
}
