using System.Collections.Immutable;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Restricted;
using Gagamba.Execution.Restricted.Control;

internal static class IntegratedControllerProbe
{
    // Fixed pinned internal profile only. This process is the ACTUAL original controller in fault tests.
    internal static async Task<int> Run()
    {
        try
        {
            var line=Console.ReadLine(); if(line is null || line.Length>8192) return 2;
            var profile=JsonSerializer.Deserialize<B3FixtureProfile>(line)!;
            if(profile.IntegratedRecovery is null) return 2;
            var creation=B3Qualification.TryCreate(profile);
            if(creation.Operation is null) {Console.WriteLine(JsonSerializer.Serialize(new {Refused=creation.Reason}));return 3;}
            await using var operation=creation.Operation;
            Task<RestrictedPrepareResult>? preparing=null;PreparedRestrictedExecution? prepared=null;
            Console.WriteLine("READY");Console.Out.Flush();
            while((line=Console.ReadLine()) is not null)
            {
                switch(line)
                {
                    case "prepare":
                        if(preparing is not null) return 4;
                        preparing=operation.PrepareAsync(new(profile.ToolEnrollmentId,profile.InputEnrollmentId)).AsTask();
                        Console.WriteLine("PREPARING");break;
                    case "prepared":
                        if(preparing is null) return 4;
                        var result=await preparing.WaitAsync(TimeSpan.FromSeconds(15));prepared=result.Preparation;
                        Console.WriteLine(JsonSerializer.Serialize(new {Kind=result.Kind,Reference=prepared?.Reference,Deadlines=prepared?.Deadlines}));break;
                    case "journal":
                        using(var storage=new Gagamba.Execution.Linux.M1.Native.JournalFileStorage())
                        {
                            var journal=new Gagamba.Execution.Linux.M1.Recovery.OwnershipJournal(storage,profile.JournalDirectory);
                            Console.WriteLine(JsonSerializer.Serialize(journal.ReadLatest().Snapshot));
                        }
                        break;
                    case "inspect": Console.WriteLine(JsonSerializer.Serialize(creation.ExperimentalOwner!.InspectIntegrated()));break;
                    case "release":
                        if(prepared is null) return 4;
                        Console.WriteLine(JsonSerializer.Serialize(await operation.RequestReleaseAsync(prepared,ImmutableArray.Create(new byte[32]))));break;
                    case "stop": Console.WriteLine(JsonSerializer.Serialize(await operation.RequestStopAsync(RestrictedStopReason.CallerStop)));break;
                    case "observe": Console.WriteLine(JsonSerializer.Serialize(new {Observation=await operation.ObserveAsync(),PhysicalReleaseAttempts=creation.ExperimentalOwner!.PhysicalReleaseAttempts}));break;
                    case "dispose": await operation.DisposeAsync();Console.WriteLine(JsonSerializer.Serialize(await operation.ObserveAsync()));break;
                    default:return 4;
                }
                Console.Out.Flush();
            }
            return 0;
        }
        catch {Console.Error.WriteLine("INT01 controller refused");return 3;}
    }
}
