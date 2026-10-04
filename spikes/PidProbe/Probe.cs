// PID-visibility probe: reports which BCL process-introspection APIs work.
// Never throws: every check prints ok/FAIL and the probe exits 0, so the
// harness sees the report even when the sandbox blinds an API.
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

static string Head(Exception ex) => ex.Message.Split('\n')[0].Trim();

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

static string JsonEscape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

// inspect <pid>: machine-readable single-target visibility check.
// Always exits 0 with a JSON line; "targetVisible:false" is data, not failure.
if (args.Length == 2 && args[0] == "inspect" && int.TryParse(args[1], out int targetPid))
{
    int selfPid = Environment.ProcessId;
    uint ppid = 0;
    int visible = -1;
    bool targetVisible = false;
    bool directVisible = false;
    string? targetName = null;
    string? error = null;
    try
    {
        ppid = FindParentPid((uint)selfPid);
        try { using var direct = Process.GetProcessById(targetPid); directVisible = true; targetName = direct.ProcessName; }
        catch { /* direct query failed; fall back to enumeration */ }
        var all = Process.GetProcesses();
        visible = all.Length;
        foreach (var p in all)
        {
            try { if (p.Id == targetPid) { targetVisible = true; targetName ??= p.ProcessName; } }
            catch { /* per-process query may fail; keep scanning */ }
            finally { p.Dispose(); }
        }
    }
    catch (Exception ex) { error = $"{ex.GetType().Name}: {Head(ex)}"; }
    string errJson = error is null ? "null" : $"\"{JsonEscape(error)}\"";
    string nameJson = targetName is null ? "null" : $"\"{JsonEscape(targetName)}\"";
    Console.WriteLine($"{{\"selfPid\":{selfPid},\"parentPid\":{ppid},\"targetPid\":{targetPid},\"visibleProcessCount\":{visible},\"targetVisible\":{targetVisible.ToString().ToLowerInvariant()},\"directVisible\":{directVisible.ToString().ToLowerInvariant()},\"targetProcessName\":{nameJson},\"error\":{errJson}}}");
    return 0;
}

[DllImport("kernel32.dll", SetLastError = true)]
static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);
[DllImport("kernel32.dll", SetLastError = true)]
static extern bool CloseHandle(IntPtr hObject);

Console.WriteLine($"pidprobe start pid={Environment.ProcessId}");

try { using var self = Process.GetCurrentProcess(); Console.WriteLine($"GetCurrentProcess ok id={self.Id} name={self.ProcessName}"); }
catch (Exception ex) { Console.WriteLine($"GetCurrentProcess FAIL {ex.GetType().Name}: {Head(ex)}"); }

try { using var byId = Process.GetProcessById(Environment.ProcessId); Console.WriteLine($"GetProcessById(self) ok id={byId.Id}"); }
catch (Exception ex) { Console.WriteLine($"GetProcessById(self) FAIL {ex.GetType().Name}: {Head(ex)}"); }

try
{
    var all = Process.GetProcesses();
    Console.WriteLine($"GetProcesses ok count={all.Length}");
    foreach (var p in all) p.Dispose();
}
catch (Exception ex) { Console.WriteLine($"GetProcesses FAIL {ex.GetType().Name}: {Head(ex)}"); }

try { using var self2 = Process.GetCurrentProcess(); var m = self2.MainModule; Console.WriteLine($"MainModule ok {(m is null ? "null" : m.FileName)}"); }
catch (Exception ex) { Console.WriteLine($"MainModule FAIL {ex.GetType().Name}: {Head(ex)}"); }

try
{
    const uint TH32CS_SNAPPROCESS = 0x00000002;
    IntPtr snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snap == (IntPtr)(-1))
    {
        Console.WriteLine($"ToolHelp FAIL snap err={Marshal.GetLastWin32Error()}");
    }
    else
    {
        try
        {
            var pe = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };
            uint ppid = 0;
            var seen = new System.Collections.Generic.List<string>();
            if (Process32First(snap, ref pe))
            {
                do
                {
                    seen.Add($"{pe.th32ProcessID}/{pe.th32ParentProcessID}:{pe.szExeFile}");
                    if (pe.th32ProcessID == (uint)Environment.ProcessId)
                        ppid = pe.th32ParentProcessID;
                } while (Process32Next(snap, ref pe));
            }
            Console.WriteLine($"ToolHelp ok count={seen.Count} self-ppid={ppid} [{string.Join(",", seen)}]");
            if (ppid != 0)
            {
                try { using var par = Process.GetProcessById((int)ppid); Console.WriteLine($"GetProcessById(parent={ppid}) ok name={par.ProcessName}"); }
                catch (Exception ex) { Console.WriteLine($"GetProcessById(parent={ppid}) FAIL {ex.GetType().Name}: {Head(ex)}"); }
            }
        }
        finally { CloseHandle(snap); }
    }
}
catch (Exception ex) { Console.WriteLine($"ToolHelp FAIL {ex.GetType().Name}: {Head(ex)}"); }

Console.WriteLine("pidprobe done");
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
