// Per-OS workload scripts for conformance. Kept minimal and positional:
// no shell quoting of caller data. Unix uses /bin/sh + a tiny python
// escaper (macOS ships no setsid(1)); Windows uses Windows PowerShell.
namespace Gagamba.Conformance;

internal static class ConformanceScripts
{
    internal const string UnixWorkSh =
        "#!/bin/sh\n" +
        "mode=\"$1\"; dir=\"$2\"; name=\"${3:-hb}\"\n" +
        "beat() { date +%s > \"$dir/$1\"; }\n" +
        "case \"$mode\" in\n" +
        "  cwd) pwd > cwd.tmp && mv cwd.tmp cwd.txt ;;\n" +
        "  env) env > \"$dir/env.tmp\" && mv \"$dir/env.tmp\" \"$dir/env.txt\" ;;\n" +
        "  hold) while true; do beat \"$name\"; sleep 1; done ;;\n" +
        "  tree) sh \"$0\" hold \"$dir\" child & while true; do beat root; sleep 1; done ;;\n" +
        "  exitroot) sh \"$0\" hold \"$dir\" child & sleep 1 ;;\n" +
        "esac\n";

    internal const string UnixEscapePy =
        "import os, sys, time\n" +
        "d = sys.argv[1]; mode = sys.argv[2]\n" +
        "stopfile = os.path.join(d, 'stopfile')\n" +
        "def beat(n):\n" +
        "    with open(os.path.join(d, n), 'w') as f:\n" +
        "        f.write(str(int(time.time())))\n" +
        "def launch(m):\n" +
        "    os.spawnl(os.P_NOWAIT, sys.executable, sys.executable,\n" +
        "              os.path.abspath(__file__), d, m)\n" +
        "if mode == 'root':\n" +
        "    launch('leaf'); launch('escaper')\n" +
        "    while not os.path.exists(stopfile):\n" +
        "        time.sleep(0.2)\n" +
        "elif mode == 'leaf':\n" +
        "    while not os.path.exists(stopfile):\n" +
        "        beat('hb-leaf'); time.sleep(1)\n" +
        "elif mode == 'escaper':\n" +
        "    dl = time.time() + 15\n" +
        "    while time.time() < dl and not os.path.exists(os.path.join(d, 'hb-leaf')):\n" +
        "        time.sleep(0.1)\n" +
        "    try:\n" +
        "        os.setsid()\n" +
        "    except OSError as ex:\n" +
        "        with open(os.path.join(d, 'esc-error.txt'), 'w') as f:\n" +
        "            f.write(str(ex))\n" +
        "        sys.exit(3)\n" +
        "    while not os.path.exists(stopfile):\n" +
        "        beat('hb-esc'); time.sleep(1)\n";

    internal const string WindowsWorkPs1 =
        "param([string]$Mode,[string]$Dir,[string]$Name='hb')\n" +
        "$self = $MyInvocation.MyCommand.Path\n" +
        "function Beat([string]$n) { (Get-Date).Ticks | Set-Content -Path (Join-Path $Dir $n) }\n" +
        "function Spawn([string]$mode,[string]$nm) {\n" +
        "  Start-Process -FilePath 'powershell' -WindowStyle Hidden -ArgumentList " +
        "@('-NoProfile','-ExecutionPolicy','Bypass','-File',$self,$mode,$Dir,$nm)\n" +
        "}\n" +
        "switch ($Mode) {\n" +
        "  'cwd' { (Get-Location).Path | Set-Content cwd.tmp; " +
        "Move-Item -Force cwd.tmp cwd.txt }\n" +
        "  'env' { Get-ChildItem Env: | ForEach-Object { '{0}={1}' -f $_.Name, $_.Value } | " +
        "Set-Content (Join-Path $Dir 'env.tmp'); Move-Item -Force (Join-Path $Dir 'env.tmp') (Join-Path $Dir 'env.txt') }\n" +
        "  'hold' { while ($true) { Beat $Name; Start-Sleep -Seconds 1 } }\n" +
        "  'tree' { Spawn 'hold' 'child'; while ($true) { Beat 'root'; Start-Sleep -Seconds 1 } }\n" +
        "  'exitroot' { Spawn 'hold' 'child'; Start-Sleep -Seconds 1 }\n" +
        "}\n";

    internal static string UnixWorkPath(string ws) => Path.Combine(ws, "work.sh");
    internal static string UnixEscapePath(string ws) => Path.Combine(ws, "escape.py");
    internal static string WindowsWorkPath(string ws) => Path.Combine(ws, "work.ps1");

    internal static void Write(string ws)
    {
        if (OperatingSystem.IsWindows())
        {
            File.WriteAllText(WindowsWorkPath(ws), WindowsWorkPs1);
        }
        else
        {
            File.WriteAllText(UnixWorkPath(ws), UnixWorkSh);
            File.WriteAllText(UnixEscapePath(ws), UnixEscapePy);
        }
    }

    internal static string? FindPython()
    {
        foreach (string p in new[] { "/usr/bin/python3", "/usr/local/bin/python3", "/opt/homebrew/bin/python3" })
            if (File.Exists(p)) return p;
        return null;
    }

    internal static string? FindPowerShell()
    {
        string p = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        return File.Exists(p) ? p : null;
    }

    /// <summary>Explicit environment for the Unix workload: system PATH
    /// only (the workload needs date/sh/mv/env), plus the test's granted
    /// marker. No ambient inheritance.</summary>
    internal static Dictionary<string, string> UnixEnv(string grantedMarker, string grantedValue) =>
        new()
        {
            ["PATH"] = "/usr/bin:/bin:/usr/sbin:/sbin",
            [grantedMarker] = grantedValue,
        };

    /// <summary>Explicit environment a Windows PowerShell workload needs
    /// to boot and spawn children (module path; PATH for Start-Process).
    /// Declared, never inherited (GW-2 runner finding).</summary>
    internal static Dictionary<string, string> WindowsEnv(string grantedMarker, string grantedValue)
    {
        string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SYSTEMROOT"] = win,
            ["SYSTEMDRIVE"] = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\",
            ["WINDIR"] = win,
            ["PATH"] = Environment.GetEnvironmentVariable("PATH") ?? "",
            ["PATHEXT"] = Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD",
            ["PSModulePath"] = Environment.GetEnvironmentVariable("PSModulePath") ?? "",
            ["TEMP"] = Path.GetTempPath(),
            ["TMP"] = Path.GetTempPath(),
            ["USERPROFILE"] = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ["APPDATA"] = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ["LOCALAPPDATA"] = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ["COMPUTERNAME"] = Environment.MachineName,
            ["NUMBER_OF_PROCESSORS"] = Environment.ProcessorCount.ToString(),
            [grantedMarker] = grantedValue,
        };
    }
}
