using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gagamba.Execution.Restricted.Contracts;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Restricted;
using Gagamba.Execution.Restricted.Control;

internal static class IntegratedControllerProbe
{
    private static readonly JsonSerializerOptions EvidenceJson = new() { Converters = { new ReferenceEvidenceConverter() } };
    private sealed class ReferenceEvidenceConverter : JsonConverter<RestrictedPreparationReference>
    {
        public override RestrictedPreparationReference Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => throw new NotSupportedException("Evidence output only");
        public override void Write(Utf8JsonWriter writer, RestrictedPreparationReference value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, new { value.ExecutionId, value.IssuerId, value.GenerationId, value.PreparationId, ManifestSha256 = value.ManifestSha256.ToArray() }, options);
    }
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
            Task<RestrictedReleaseReply>? releasing=null;
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
                        Console.WriteLine(JsonSerializer.Serialize(new {Kind=result.Kind,Reference=prepared?.Reference,Deadlines=prepared?.Deadlines,Establishment=prepared is null ? null : new { prepared.Establishment.EvidenceVersion, Bytes=prepared.Establishment.Bytes.ToArray(), Sha256=prepared.Establishment.Sha256.ToArray() }},EvidenceJson));break;
                    case "prepare-status":
                        Console.WriteLine(JsonSerializer.Serialize(new { Completed=preparing?.IsCompleted, Prepared=preparing?.IsCompletedSuccessfully==true && preparing.Result.Kind==RestrictedPrepareKind.Prepared }));break;
                    case "begin-release":
                        if(prepared is null || releasing is not null) return 4;
                        releasing=operation.RequestReleaseAsync(prepared,ImmutableArray.Create(new byte[32])).AsTask();
                        Console.WriteLine("RELEASING");break;
                    case "release-result":
                        if(releasing is null) return 4;
                        Console.WriteLine(JsonSerializer.Serialize(await releasing.WaitAsync(TimeSpan.FromSeconds(15))));break;
                    case "queue-stop":
                        _=operation.RequestStopAsync(RestrictedStopReason.CallerStop).AsTask();
                        Console.WriteLine("STOP-QUEUED");break;
                    case "journal-status":
                        using(var statusStorage=new Gagamba.Execution.Linux.M1.Native.JournalFileStorage())
                            Console.WriteLine(JsonSerializer.Serialize(new Gagamba.Execution.Linux.M1.Recovery.OwnershipJournal(statusStorage,profile.JournalDirectory).ReadLatest()));
                        break;
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
                    case "observe": Console.WriteLine(JsonSerializer.Serialize(new {Observation=await operation.ObserveAsync(),PhysicalReleaseAttempts=creation.ExperimentalOwner!.PhysicalReleaseAttempts},EvidenceJson));break;
                    case "capture-stdout": Console.WriteLine(JsonSerializer.Serialize(await operation.ReadCaptureAsync(RestrictedCaptureStream.Stdout, 0, 16384)));break;
                    case "capture-stderr": Console.WriteLine(JsonSerializer.Serialize(await operation.ReadCaptureAsync(RestrictedCaptureStream.Stderr, 0, 16384)));break;
                    case "dispose": await operation.DisposeAsync();Console.WriteLine(JsonSerializer.Serialize(await operation.ObserveAsync(),EvidenceJson));break;
                    default:return 4;
                }
                Console.Out.Flush();
            }
            return 0;
        }
        catch(Exception error) {Console.Error.WriteLine($"INT01 controller refused ({error.GetType().Name})");if(error is InvalidOperationException) Console.Error.WriteLine(error.Message);Console.Error.WriteLine(error.StackTrace);return 3;}
    }
}
