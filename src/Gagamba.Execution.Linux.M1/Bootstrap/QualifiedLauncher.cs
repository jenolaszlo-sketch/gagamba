using System.Diagnostics;
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// Linux x64 glibc spawn-local mappings. Parent descriptors are ALWAYS CLOEXEC.
// Only the explicit child ends become 3/4 in the child, without an inheritable window.
internal static class QualifiedLauncher
{
    private sealed class ChildExit {internal readonly object Gate=new();internal int? Code;}
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Process,ChildExit> Children=new();
    [DllImport("libc",EntryPoint="waitpid",SetLastError=true)] private static extern int WaitPid(int pid,out int status,int options);
    internal static bool Owns(Process process)=>Children.TryGetValue(process,out _);
    internal static bool TryReap(Process process,int timeoutMilliseconds,out int code)
    {
        code=-1;
        if(!Children.TryGetValue(process,out var child)) return false;
        long deadline=Stopwatch.GetTimestamp()+Math.Max(0,timeoutMilliseconds)*Stopwatch.Frequency/1000;
        lock(child.Gate)
        {
            if(child.Code is { } retained) {code=retained;return true;}
            do
            {
                int result=WaitPid(process.Id,out int status,1 /* WNOHANG */);
                if(result==process.Id)
                {
                    int signal=status&0x7f;
                    if(signal==0x7f) return false; // stopped/traced is not exit
                    code=signal==0?(status>>8)&255:128+signal;child.Code=code;return true;
                }
                if(result<0 && Marshal.GetLastPInvokeError()!=4) return false;
                if(Stopwatch.GetTimestamp()>=deadline) return false;
                Thread.Sleep(1);
            } while(true);
        }
    }
    [DllImport("libc",EntryPoint="posix_spawn_file_actions_init")] private static extern int Init(IntPtr actions);
    [DllImport("libc",EntryPoint="posix_spawn_file_actions_destroy")] private static extern int Destroy(IntPtr actions);
    [DllImport("libc",EntryPoint="posix_spawn_file_actions_adddup2")] private static extern int Dup(IntPtr actions,int source,int target);
    [DllImport("libc",EntryPoint="posix_spawn_file_actions_addclosefrom_np")] private static extern int CloseFrom(IntPtr actions,int from);
    [DllImport("libc",EntryPoint="posix_spawn_file_actions_addopen")] private static extern int Open(IntPtr actions,int fd,string path,int flags,uint mode);
    [DllImport("libc",EntryPoint="posix_spawn")] private static extern int Spawn(out int pid,string path,IntPtr actions,IntPtr attributes,IntPtr argv,IntPtr env);
    [DllImport("libc",EntryPoint="fcntl",SetLastError=true)] private static extern int Fcntl(int fd,int command,int argument);
    internal static (int Parent,int Child) Pair()
    {
        var pair=new int[2];
        if(DirectUnix.SocketPair(1,5|DirectUnix.Cloexec|DirectUnix.Nonblock,0,pair)!=0) throw new IOException("qualified private pair");
        return(pair[0],pair[1]);
    }
    internal static Process Start(ExperimentalRuntimePin runtime,string assembly,string[] arguments,params (int Source,int Target,string Variable)[] mappings)
    {
        runtime.VerifyLaunch();return StartPinned(runtime,assembly,arguments,mappings);
    }
    // Called only with a plan prepared from the argument-free S2 result on a bounded worker.
    internal sealed class InstalledPlan
    {
        internal ExperimentalRuntimePin Runtime {get;}
        internal string Assembly {get;}
        private InstalledPlan(ExperimentalRuntimePin runtime,string assembly) {Runtime=runtime;Assembly=assembly;}
        internal static InstalledPlan Prepare(Installed.LoadedInstallation selected,string role)
        {
            string entry="Gagamba.Execution.Linux.M1."+(role=="owner"?"Owner":role=="recovery"?"Recovery":throw new IOException("launch role"));
            var runtime=selected.Runtime();runtime.VerifyLaunch();
            string assembly=selected.Executable(role).Verify(entry);
            return new(runtime,assembly);
        }
        internal Process Spawn(string[] arguments,params (int Source,int Target,string Variable)[] mappings)=>StartPinned(Runtime,Assembly,arguments,mappings);
    }
    private static Process StartPinned(ExperimentalRuntimePin runtime,string assembly,string[] arguments,(int Source,int Target,string Variable)[] mappings)
    {
        if(!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture!=Architecture.X64 ||
            mappings.Length is <1 or >2 || mappings.Select(m=>m.Target).Order().Where((v,i)=>v!=i+3).Any())
            throw new IOException("qualified spawn mapping");
        var actions=Marshal.AllocHGlobal(256);bool initialized=false;
        var copies=new List<int>();IntPtr argv=IntPtr.Zero,env=IntPtr.Zero;var strings=new List<IntPtr>();
        try
        {
            Check(Init(actions));initialized=true;
            Check(Open(actions,0,"/dev/null",0,0));Check(Open(actions,1,"/dev/null",1,0));Check(Open(actions,2,"/dev/null",1,0));
            foreach(var m in mappings)
            {
                if(m.Source<=2 || !PreparationInterop.HasCloexec(m.Source)) throw new IOException("non-CLOEXEC source refused");
                int copy=Fcntl(m.Source,1030 /* F_DUPFD_CLOEXEC */,10);
                if(copy<0) throw new IOException("spawn-local duplicate failed");copies.Add(copy);Check(Dup(actions,copy,m.Target));
            }
            Check(CloseFrom(actions,3+mappings.Length));
            string prefix=assembly[..^4];
            argv=Array(new[]{runtime.HostPath,"exec","--fx-version",runtime.RuntimeVersion,"--depsfile",prefix+".deps.json",
                "--runtimeconfig",prefix+".runtimeconfig.json",assembly}.Concat(arguments));
            env=Array(new[]{"PATH=/usr/bin:/bin","DOTNET_ROOT=/usr/lib/dotnet","DOTNET_MULTILEVEL_LOOKUP=0",
                "DOTNET_EnableDiagnostics=0","DOTNET_ROLL_FORWARD=Disable"}.Concat(mappings.Select(m=>m.Variable+"="+m.Target)));
            Check(Spawn(out int pid,runtime.HostPath,actions,IntPtr.Zero,argv,env));
            // The returned child, never a packet-selected PID. Post-pin private exchange closes
            // the spawn/open gap; callers retain this exact Process object and safe handle.
            var process=Process.GetProcessById(pid);Children.Add(process,new ChildExit());return process;
        }
        finally
        {
            if(initialized) _=Destroy(actions);Marshal.FreeHGlobal(actions);
            foreach(int fd in copies) PreparationInterop.CloseFd(fd);
            foreach(var p in strings) Marshal.FreeCoTaskMem(p);
            if(argv!=IntPtr.Zero) Marshal.FreeHGlobal(argv);if(env!=IntPtr.Zero) Marshal.FreeHGlobal(env);
        }
        IntPtr Array(IEnumerable<string> values)
        {
            var list=values.ToArray();var p=Marshal.AllocHGlobal((list.Length+1)*IntPtr.Size);
            for(int i=0;i<list.Length;i++) {var s=Marshal.StringToCoTaskMemUTF8(list[i]);strings.Add(s);Marshal.WriteIntPtr(p,i*IntPtr.Size,s);}
            Marshal.WriteIntPtr(p,list.Length*IntPtr.Size,IntPtr.Zero);return p;
        }
    }
    private static void Check(int result) {if(result!=0) throw new IOException("qualified spawn refused: "+result);}
}
