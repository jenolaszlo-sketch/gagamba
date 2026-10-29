using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Native;

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
