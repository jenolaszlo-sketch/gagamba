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
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

static string ShaHex(byte[] data, int count)
{
    byte[] h = SHA256.HashData(data.AsSpan(0, count));
    var sb = new StringBuilder(h.Length * 2);
    foreach (byte b in h) sb.Append(b.ToString("x2"));
    return sb.ToString();
}

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

// --mmap-read <path>: read via memory-mapped view (how compilers and
// metadata readers consume DLLs). Hashes the WHOLE file (cap 32MB) so the
// harness can prove full content-integrity against host-side hashes.
if (args.Length == 2 && (args[0] == "--read-file" || args[0] == "--mmap-read"))
{
    const int cap = 32 * 1024 * 1024;
    string target = args[1];
    bool mmap = args[0] == "--mmap-read";
    long size = -1;
    int read = 0;
    string sha = "";
    string overNote = "";
    string? fErr = null;
    try
    {
        using var inc = SHA256.Create();
        // NOTE: views may extend to the page boundary; cap reads at the file
        // size (like every correct reader) and report the tail bytes so
        // zero-padding vs leaked content is distinguishable.
        if (mmap)
        {
            using var fsi = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            size = fsi.Length;
            using var mmf = MemoryMappedFile.CreateFromFile(target, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            using var vs = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            byte[] buf = new byte[65536];
            int n;
            long total = 0;
            while (total < Math.Min(cap, size) && (n = vs.Read(buf, 0, (int)Math.Min(buf.Length, Math.Min(cap, size) - total))) > 0)
            {
                inc.TransformBlock(buf, 0, n, null, 0);
                total += n;
            }
            read = (int)Math.Min(total, int.MaxValue);
            inc.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            sha = BitConverter.ToString(inc.Hash ?? Array.Empty<byte>()).Replace("-", "").ToLowerInvariant();
            long over = 0;
            {
                byte[] ob = new byte[4096];
                int on;
                while ((on = vs.Read(ob, 0, ob.Length)) > 0) over += on;
            }
            overNote = $"overEofBytes={over}";
        }
        else
        {
            using var fs = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            size = fs.Length;
            byte[] buf = new byte[65536];
            int n;
            long total = 0;
            while (total < cap && (n = fs.Read(buf, 0, (int)Math.Min(buf.Length, cap - total))) > 0)
            {
                inc.TransformBlock(buf, 0, n, null, 0);
                total += n;
            }
            read = (int)Math.Min(total, int.MaxValue);
            inc.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            sha = BitConverter.ToString(inc.Hash ?? Array.Empty<byte>()).Replace("-", "").ToLowerInvariant();
        }
    }
    catch (Exception ex) { fErr = $"{ex.GetType().Name}: {ex.Message.Split('\n')[0].Trim()}"; }
    string feJson = fErr is null ? "null" : J(fErr, 300);
    string overJson = overNote.Length > 0 ? J(overNote) : "null";
    Console.WriteLine($"{{\"runnerPid\":{selfPid},\"mode\":\"{(mmap ? "mmap-read" : "read-file")}\",\"path\":{J(target)},\"size\":{size},\"readBytes\":{read},\"sha256\":{(sha.Length > 0 ? J(sha) : "null")},\"overEof\":{overJson},\"error\":{feJson}}}");
    return 0;
}

// --list-dir <path>: enumerate one level in-process (progression step 2).
if (args.Length == 2 && args[0] == "--list-dir")
{
    string target = args[1];
    string? dErr = null;
    var names = new System.Collections.Generic.List<string>();
    try
    {
        foreach (string e in Directory.EnumerateFileSystemEntries(target))
        {
            names.Add(System.IO.Path.GetFileName(e));
            if (names.Count >= 12) break;
        }
    }
    catch (Exception ex) { dErr = $"{ex.GetType().Name}: {ex.Message.Split('\n')[0].Trim()}"; }
    string deJson = dErr is null ? "null" : J(dErr, 300);
    Console.WriteLine($"{{\"runnerPid\":{selfPid},\"mode\":\"list-dir\",\"path\":{J(target)},\"countCapped\":{names.Count},\"names\":[{string.Join(",", names.ConvertAll(n => J(n)))}],\"error\":{deJson}}}");
    return 0;
}

// --load-test <path>: Assembly.LoadFrom + GetTypes in-process (.NET 8
// context, unlike Windows PowerShell). Reports type count or the exact
// loader failure chain (progression: can managed code CONSUME the DLL?).
if (args.Length == 2 && args[0] == "--load-test")
{
    string target = args[1];
    string? lErr = null;
    int typeCount = -1;
    string loaderNotes = "";
    try
    {
        var asm = System.Reflection.Assembly.LoadFrom(target);
        try { typeCount = asm.GetTypes().Length; }
        catch (System.Reflection.ReflectionTypeLoadException rtle)
        {
            typeCount = rtle.Types.Count(t => t is not null);
            var msgs = new System.Collections.Generic.List<string>();
            foreach (var le in rtle.LoaderExceptions)
                if (le is not null && msgs.Count < 3) msgs.Add($"{le.GetType().Name}:{le.Message.Split('\n')[0].Trim()}");
            loaderNotes = string.Join(" | ", msgs);
        }
    }
    catch (Exception ex)
    {
        lErr = $"{ex.GetType().Name}: {ex.Message.Split('\n')[0].Trim()}";
        if (ex.InnerException is not null) lErr += $" /in:{ex.InnerException.GetType().Name}";
    }
    string leJson = lErr is null ? "null" : J(lErr, 400);
    Console.WriteLine($"{{\"runnerPid\":{selfPid},\"mode\":\"load-test\",\"path\":{J(target)},\"typeCount\":{typeCount},\"loaderNotes\":{J(loaderNotes)},\"error\":{leJson}}}");
    return 0;
}
if (args.Length == 2 && args[0] == "--mkdir")
{
    string target = args[1];
    string? mErr = null;
    try { Directory.CreateDirectory(target); }
    catch (Exception ex) { mErr = $"{ex.GetType().Name}: {ex.Message.Split('\n')[0].Trim()}"; }
    string meJson = mErr is null ? "null" : J(mErr, 300);
    Console.WriteLine($"{{\"runnerPid\":{selfPid},\"mode\":\"mkdir\",\"path\":{J(target)},\"error\":{meJson}}}");
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
