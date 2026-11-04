using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Installed;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// No release/preparation type has a representation in this B2-only protocol.
internal enum RecoveryMessage : byte { Associate = 1, Ready = 2, Stop = 3, Inspect = 4, Snapshot = 5, Reconnect = 6, Refused = 7, Rendezvous = 8, InstallationEchoRequest = 9, InstallationEchoResponse = 10, B2OwnerBound = 11, B2OwnerEchoAccepted = 12 }
internal sealed class RecoveryWire
{
    internal const int MaxPayload = 512;
    internal static int PayloadBound(RecoveryMessage type) => type is RecoveryMessage.Rendezvous or RecoveryMessage.Ready or RecoveryMessage.Snapshot ? 4096 : MaxPayload;
    private readonly OwnerProtocolSequencer _sequence;
    private readonly ushort _version;
    private readonly bool _integrated;
    private readonly bool? _controller;
    internal RecoveryWire(Guid execution, Guid generation, ushort version = 1, bool integrated = false, bool? controller = null)
    { _sequence = new(execution,generation); _version=version; _integrated=integrated; _controller=controller; }
    private bool Allowed(RecoveryMessage type,bool sending)
    {
        if((byte)type>=9 && (_version!=2 || (_integrated && (byte)type>=11))) return false;
        if(_controller is null) return (byte)type<9; // legacy inspection API cannot admit qualified extensions
        bool fromController=sending?_controller.Value:!_controller.Value;
        return fromController ? type is RecoveryMessage.Associate or RecoveryMessage.Stop or RecoveryMessage.Inspect
            or RecoveryMessage.Reconnect or RecoveryMessage.InstallationEchoResponse or RecoveryMessage.B2OwnerEchoAccepted
            : type is RecoveryMessage.Ready or RecoveryMessage.Snapshot or RecoveryMessage.Refused or RecoveryMessage.Rendezvous
                or RecoveryMessage.InstallationEchoRequest or RecoveryMessage.B2OwnerBound;
    }
    private bool Shape(RecoveryMessage type,ReadOnlySpan<byte> payload) => type switch
    {
        RecoveryMessage.Associate => payload.Length==48,
        RecoveryMessage.InstallationEchoRequest => InstallationWire.EchoShape(payload,0,_integrated),
        RecoveryMessage.InstallationEchoResponse => InstallationWire.ResponseShape(payload),
        RecoveryMessage.B2OwnerBound => BindingShape(payload),
        RecoveryMessage.B2OwnerEchoAccepted => payload.Length==48 && new Guid(payload[..16])!=Guid.Empty && payload[16..].IndexOfAnyExcept((byte)0)>=0,
        RecoveryMessage.Ready or RecoveryMessage.Snapshot or RecoveryMessage.Rendezvous => payload.Length<=PayloadBound(type),
        _ => payload.Length==0
    };
    private static bool BindingShape(ReadOnlySpan<byte> p)
    { try { _=InstallationWire.ReadBinding(p);return true; } catch(InvalidDataException) {return false;} }
    internal byte[] Create(RecoveryMessage type, ReadOnlySpan<byte> payload = default)
    {
        if (!Enum.IsDefined(type) || !Allowed(type,true) || payload.Length > PayloadBound(type) || !Shape(type,payload)) throw new ArgumentException("Invalid B2 frame");
        // Distinct magic prevents a controller packet being used as an owner packet.
        var frame = _sequence.Create((OwnerProtocolMessageType)(byte)type, payload);
        frame[0] ^= 0x20;
        return frame;
    }
    internal bool Accept(byte[] frame, out RecoveryMessage type, out byte[] payload)
    {
        type = default; payload = [];
        if (frame.Length < OwnerProtocol.HeaderBytes || frame.Length > OwnerProtocol.HeaderBytes + 4096) return false;
        if (!Enum.IsDefined((RecoveryMessage)frame[6]) || !Allowed((RecoveryMessage)frame[6],false)
            || frame.Length > OwnerProtocol.HeaderBytes + PayloadBound((RecoveryMessage)frame[6])
            || !Shape((RecoveryMessage)frame[6],frame.AsSpan(OwnerProtocol.HeaderBytes))) return false;
        var copy = frame.ToArray(); copy[0] ^= 0x20;
        var allowed = Enum.GetValues<RecoveryMessage>().Select(x => (OwnerProtocolMessageType)(byte)x).ToArray();
        // TryAccept also checks execution, generation and next per-direction sequence.
        if (!_sequence.TryAccept(copy, (OwnerProtocolMessageType)frame[6], out var message, out _)
            || !allowed.Contains(message!.Type)) return false;
        type = (RecoveryMessage)(byte)message.Type; payload = message.Payload;
        return true;
    }
}

internal sealed class RecoveryAssociation : IDisposable
{
    private readonly Guid _session;
    private readonly byte[] _secret;
    internal bool Associated { get; private set; }
    internal RecoveryAssociation(Guid session, byte[] secret)
    {
        if (session == Guid.Empty || secret.Length != 32) throw new ArgumentException("Invalid session");
        _session = session; _secret = secret.ToArray();
    }
    internal bool Authenticate(ReadOnlySpan<byte> credential)
    {
        if (Associated || credential.Length != 48 || new Guid(credential[..16]) != _session
            || !CryptographicOperations.FixedTimeEquals(credential[16..], _secret)) return false;
        Associated = true;
        CryptographicOperations.ZeroMemory(_secret);
        return true;
    }
    public void Dispose() => CryptographicOperations.ZeroMemory(_secret);
}

internal sealed class RecoveryObligation
{
    internal int Count { get; private set; }
    internal string? Cause { get; private set; }
    internal long? Deadline { get; private set; }
    internal bool OwnerAcknowledged { get; set; }
    internal bool ReleaseAuthority => false;
    internal bool Request(string cause, long now)
    {
        if (Count != 0) return false;
        Deadline = checked(now + 2 * Stopwatch.Frequency);
        Cause = cause; Count = 1;
        return true;
    }
}

internal sealed record PinnedExecutable(string Directory, Dictionary<string, string> Sha256)
{
    private static byte[] HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return SHA256.HashData(stream);
    }
    internal string Verify(string entry)
    {
        var expected = new[] { entry + ".dll", entry + ".deps.json", entry + ".runtimeconfig.json",
            "Gagamba.Execution.Linux.M1.dll", "Gagamba.Execution.dll" };
        if (!Path.IsPathFullyQualified(Directory) || Sha256 is null || Sha256.Count != expected.Length
            || expected.Any(name => !Sha256.ContainsKey(name))) throw new InvalidDataException("Installation closure refused");
        foreach (var name in expected)
        {
            var hash = Sha256[name];
            if (hash.Length != 64 || !hash.All(Uri.IsHexDigit)
                || !Convert.ToHexString(HashFile(Path.Combine(Directory, name)))
                    .Equals(hash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Installation hash refused");
        }
        return Path.Combine(Directory, entry + ".dll");
    }
}

internal sealed record RecoveryConfiguration(ushort Version, Guid Execution, Guid Generation, Guid Session,
    int ControllerPid, uint ControllerUid, byte[] Secret, PinnedExecutable Recovery, PinnedExecutable Owner,
    string EvidenceDirectory, string? DelegatedParent = null, IntegratedRecoverySpecification? Integrated = null, InstallationIdentity? B2InstallationIdentity = null, long? B2DeadlineNs = null)
{
    internal const int MaxBytes = 8192;
    internal static RecoveryConfiguration Decode(byte[] bytes)
    {
        if (bytes.Length == 0 || bytes.Length > MaxBytes) throw new InvalidDataException("B2 bootstrap size refused");
        var json = InstallJson.Read(bytes); // recursively reject duplicate keys, including nested pins
        _=InstallJson.Int(json,"Version");
        if(json.TryGetProperty("Version",out var v) && v.ValueKind==JsonValueKind.Number && v.TryGetInt32(out var version) && version==2)
        {
            InstallJson.Require(InstallJson.Int(json,"Version")==2,"bootstrap version grammar");
            foreach(string key in new[]{"ControllerPid","ControllerUid"}) _=InstallJson.Int(json,key);
            if(json.TryGetProperty("B2DeadlineNs",out var deadline) && deadline.ValueKind!=JsonValueKind.Null) _=InstallJson.Int(json,"B2DeadlineNs");
            if(json.TryGetProperty("B2InstallationIdentity",out var b2) && b2.ValueKind!=JsonValueKind.Null) InstallationIdentity.ValidateJson(b2);
            if(json.TryGetProperty("Integrated",out var integrated) && integrated.ValueKind!=JsonValueKind.Null)
                InstallationIdentity.ValidateJson(integrated.GetProperty("InstallationIdentity"));
        }
        var value = JsonSerializer.Deserialize<RecoveryConfiguration>(bytes, new JsonSerializerOptions {
            MaxDepth = 12, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
        }) ?? throw new InvalidDataException("Missing B2 bootstrap");
        value.Validate();
        return value;
    }
    internal void Validate()
    {
        if (Version is not (1 or 2) || Execution == Guid.Empty || Generation == Guid.Empty || Session == Guid.Empty
            || ControllerPid <= 0 || ControllerUid == 0 || Secret is not { Length: 32 }
            || Recovery is null || Owner is null || !Path.IsPathFullyQualified(EvidenceDirectory))
            throw new InvalidDataException("Invalid B2 bootstrap configuration");
        if(Version==1)
        {
            if(B2InstallationIdentity is not null || B2DeadlineNs is not null || Integrated?.InstallationIdentity is not null)
                throw new InvalidDataException("v1 cannot claim qualification");
        }
        else if(Integrated is null)
        {
            // This check precedes parent/journal/domain access in both actors.
            if(DelegatedParent is not null || B2InstallationIdentity is null || B2DeadlineNs is null or <=0)
                throw new InvalidDataException("qualified B2 topology refused");
            B2InstallationIdentity.Validate();
        }
        else
        {
            if(B2InstallationIdentity is not null || B2DeadlineNs is not null || Integrated.InstallationIdentity is null)
                throw new InvalidDataException("qualified integrated topology refused");
            Integrated.InstallationIdentity.Validate();
        }
    }
    internal static (int Control, int Credential) DescriptorRoles(string? control, string? credential)
    {
        if (!int.TryParse(control, out var c) || !int.TryParse(credential, out var s)
            || c <= 2 || s <= 2 || c == s) throw new InvalidDataException("Invalid B2 descriptor roles");
        return (c, s);
    }
}

internal sealed record RecoverySnapshot(int RecoveryPid, int OwnerPid, int ControllerPid, int SessionId,
    bool Subreaper, bool ControllerLost, bool OwnerExited, int StopCount, string? Cause, long? StopDeadline,
    bool OwnerAcknowledged, bool ReleaseAuthority = false, string NativeStop = "Unproven", int RetainedDomains = 0, bool DomainRemoved = false,
    bool OwnerReaped = false, bool WriterFence = false, bool StagingRemoved = false, bool JournalFinalized = false, bool DomainEmpty = false);

internal static class RecoveryEvidence
{
    // Bounded qualification facts only. No credential, bearer, handle or manifest is logged.
    internal static void Write(string directory, string name, object value)
    {
        var text = JsonSerializer.Serialize(value);
        if (text.Length > 4096) throw new InvalidDataException("Evidence bound exceeded");
        File.WriteAllText(Path.Combine(directory, name + ".tmp"), text);
        File.Move(Path.Combine(directory, name + ".tmp"), Path.Combine(directory, name), true);
    }
}
