using System;
using System.Diagnostics;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 1) { Console.Error.WriteLine("usage: <mode>"); return 2; }
        var mode = args[0];

        if (mode == "kill-term-trapping-child")
        {
            var psi = new ProcessStartInfo("/bin/bash")
            {
                UseShellExecute = false,
                ArgumentList = { "-c", "trap \"\" TERM; echo READY; sleep 100" },
            };
            using var p = Process.Start(psi);
            if (p is null) { Console.WriteLine("start_failed"); return 3; }
            // Give the child time to install its TERM trap.
            System.Threading.Thread.Sleep(1500);
            p.Kill();
            p.WaitForExit(10000);
            Console.WriteLine($"kid_pid={p.Id} exited={p.HasExited} exit_code={p.ExitCode}");
            return 0;
        }

        if (mode == "self-exit-137")
        {
            var psi = new ProcessStartInfo("/bin/bash")
            {
                UseShellExecute = false,
                ArgumentList = { "-c", "exit 137" },
            };
            using var p = Process.Start(psi);
            if (p is null) { Console.WriteLine("start_failed"); return 3; }
            p.WaitForExit(10000);
            Console.WriteLine($"kid_pid={p.Id} exited={p.HasExited} exit_code={p.ExitCode}");
            return 0;
        }

        return 2;
    }
}
