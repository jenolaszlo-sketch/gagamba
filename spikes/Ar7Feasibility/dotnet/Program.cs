// Disposable AR-7 P04 workload only. No production code.
// Usage: P04Workload <allocMiB> <threadCount> [inputPath] [scratchDir]
// Defaults suit the candidate (/input, /tmp); the positive control passes
// fixture paths explicitly. Bounded diagnostics only (well under 32 KiB).
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.WriteLine("dotnet " + Environment.Version);
        Console.WriteLine("servergc " + System.Runtime.GCSettings.IsServerGC);
        int allocMiB = args.Length > 0 ? int.Parse(args[0]) : 0;
        int threads = args.Length > 1 ? int.Parse(args[1]) : 0;
        string inputPath = args.Length > 2 ? args[2] : "/input";
        string scratchDir = args.Length > 3 ? args[3] : "/tmp";
        foreach (string key in new[] { "HOME", "DOTNET_GCServer", "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT" })
            Console.WriteLine("env " + key + "=" + Environment.GetEnvironmentVariable(key));
        string input = File.ReadAllText(inputPath);
        Console.WriteLine("input_read " + input.Length);
        File.WriteAllText(Path.Combine(scratchDir, "dotnetscratch"), "p04");
        Console.WriteLine("scratch_write ok");
        byte[] hold = null;
        if (allocMiB > 0)
        {
            hold = new byte[(long)allocMiB * 1024 * 1024];
            long sum = 0;
            for (long i = 0; i < hold.Length; i += 4096) { hold[i] = (byte)(i >> 12); sum += hold[i]; }
            for (long i = 0; i < hold.Length; i += 4096) sum += hold[i];
            Console.WriteLine("touched_sum " + sum);
        }
        int created = 0;
        if (threads > 0)
        {
            var list = new System.Collections.Generic.List<Thread>();
            try
            {
                for (int i = 0; i < threads; i++)
                {
                    var t = new Thread(() =>
                    {
                        var b = new byte[1024 * 1024];
                        for (int k = 0; k < b.Length; k += 4096) b[k] = 1;
                        Thread.Sleep(1000);
                    });
                    t.Start();
                    list.Add(t);
                    created++;
                }
            }
            catch (Exception ex) { Console.WriteLine("thread_fail " + ex.GetType().Name); }
            foreach (var t in list) t.Join();
            Console.WriteLine("threads_created " + created);
        }
        Console.WriteLine("gc_total " + GC.GetTotalMemory(false));
        using (var p = Process.GetCurrentProcess())
            Console.WriteLine("task_count " + p.Threads.Count + " workingset " + p.WorkingSet64);
        Console.WriteLine("complete");
        return 0;
    }
}
