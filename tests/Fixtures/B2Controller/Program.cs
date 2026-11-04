using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Native;

if(args.Length==1 && args[0]=="--b6-launch")
{
    var fdText=Environment.GetEnvironmentVariable("GAGAMBA_BOOTSTRAP_FD");
    if(fdText!="3" || !RecoveryInterop.Protect(3)) return 30;
    var targets=Directory.GetFiles("/proc/self/fd").Select(p=>(Path:p,Target:new FileInfo(p).LinkTarget)).ToArray();
    // Other than stdio and the explicitly mapped endpoint, no socket or pidfd survived exec.
    if(targets.Any(p=>int.TryParse(Path.GetFileName(p.Path),out int fd) && fd>3 && p.Target is not null &&
        (p.Target.StartsWith("socket:",StringComparison.Ordinal) || p.Target=="anon_inode:[pidfd]"))) return 31;
    if(!FdPassing.TryReceiveMessage(3,5000,out var hello,out _) || !hello.AsSpan().SequenceEqual(new byte[]{42})) return 32;
    if(!FdPassing.TrySendMessage(3,System.Text.Encoding.ASCII.GetBytes("MAPPED"),out _)) return 33;
    return FdPassing.TryReceiveMessage(3,5000,out var stop,out _) && stop.AsSpan().SequenceEqual(new byte[]{43})?0:34;
}
if (args.Length == 5 && args[0] == "--b6-evidence") return B6EvidenceProbe.Run(args);
if (args.Length == 1 && args[0] == "--int01-controller") return await IntegratedControllerProbe.Run();
if (args.Length == 1 && args[0] == "--direct-peer") return DirectAuthProbe.Run();
if (args.Length != 1 || args[0] != "--controller" || !RecoveryInterop.Supported) return 2;
try
{
    var line = Console.ReadLine();
    if (line is null || line.Length > RecoveryConfiguration.MaxBytes) return 3;
    var config = JsonSerializer.Deserialize<RecoveryConfiguration>(line)!;
    using var connection = RecoveryConnection.Start(config);
    RecoveryEvidence.Write(config.EvidenceDirectory, "controller-ready.json", new { ControllerPid = Environment.ProcessId, RecoveryPid = connection.Process.Id });
    Console.WriteLine("READY"); Console.Out.Flush();
    while (true)
    {
        var command = Console.ReadLine();
        if (command is null) return 0;
        switch (command)
        {
            case "cancel-observer":
                using (var cancelled = new CancellationTokenSource())
                {
                    cancelled.Cancel();
                    try { await connection.ObserveAsync(cancelled.Token); return 4; }
                    catch (OperationCanceledException) { File.WriteAllText(Path.Combine(config.EvidenceDirectory, "observer-cancelled"), "cancelled"); }
                }
                Console.WriteLine("CANCELLED"); break;
            case "reassociate":
                FdPassing.TrySendMessage(connection.Control, connection.Wire.Create(RecoveryMessage.Associate, new byte[48]), out _);
                Console.WriteLine("SENT"); break;
            case "replay-stop":
                var replay = connection.Wire.Create(RecoveryMessage.Stop);
                FdPassing.TrySendMessage(connection.Control, replay, out _); FdPassing.TrySendMessage(connection.Control, replay, out _);
                Console.WriteLine("SENT"); break;
            case "inspect": Console.WriteLine(JsonSerializer.Serialize(connection.Inspect())); break;
            case "stop": connection.Send(RecoveryMessage.Stop); Console.WriteLine("SENT"); break;
            case "reconnect": connection.Send(RecoveryMessage.Reconnect); Console.WriteLine("SENT"); break;
            case "close": connection.Dispose(); Console.WriteLine("CLOSED"); Console.Out.Flush(); Thread.Sleep(30000); return 0;
            case "malformed": FdPassing.TrySendMessage(connection.Control, new byte[49], out _); Console.WriteLine("SENT"); break;
            case "flood": for (var i = 0; i < 20000; i++) { if (!connection.Send(RecoveryMessage.Inspect)) break; } Console.WriteLine("SENT"); break;
            default: return 3;
        }
        Console.Out.Flush();
    }
}
catch { Console.Error.WriteLine("B2 controller refused"); return 3; }
