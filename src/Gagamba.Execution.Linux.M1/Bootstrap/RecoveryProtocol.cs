using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Native;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// No release/preparation type has a representation in this B2-only protocol.
internal enum RecoveryMessage : byte { Associate = 1, Ready = 2, Stop = 3, Inspect = 4, Snapshot = 5, Reconnect = 6, Refused = 7 }
internal sealed class RecoveryWire
{
    internal const int MaxPayload = 512;
    private readonly OwnerProtocolSequencer _sequence;
    internal RecoveryWire(Guid execution, Guid generation) => _sequence = new(execution, generation);
    internal byte[] Create(RecoveryMessage type, ReadOnlySpan<byte> payload = default)
    {
        if (!Enum.IsDefined(type) || payload.Length > MaxPayload) throw new ArgumentException("Invalid B2 frame");
        // Distinct magic prevents a controller packet being used as an owner packet.
        var frame = _sequence.Create((OwnerProtocolMessageType)(byte)type, payload);
        frame[0] ^= 0x20;
        return frame;
    }
    internal bool Accept(byte[] frame, out RecoveryMessage type, out byte[] payload)
    {
        type = default; payload = [];
        if (frame.Length < OwnerProtocol.HeaderBytes || frame.Length > OwnerProtocol.HeaderBytes + MaxPayload) return false;
        var copy = frame.ToArray(); copy[0] ^= 0x20;
        var allowed = Enum.GetValues<RecoveryMessage>().Select(x => (OwnerProtocolMessageType)(byte)x).ToArray();
        // TryAccept also checks execution, generation and next per-direction sequence.
        if (!_sequence.TryAccept(copy, (OwnerProtocolMessageType)frame[6], out var message, out _)
            || !allowed.Contains(message!.Type)) return false;
        type = (RecoveryMessage)(byte)message.Type; payload = message.Payload;
        return type == RecoveryMessage.Associate ? payload.Length == 48
            : type is RecoveryMessage.Ready or RecoveryMessage.Snapshot ? payload.Length <= MaxPayload
            : payload.Length == 0;
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
    string EvidenceDirectory, string? DelegatedParent = null)
{
    internal const int MaxBytes = 8192;
    internal static RecoveryConfiguration Decode(byte[] bytes)
    {
        if (bytes.Length == 0 || bytes.Length > MaxBytes) throw new InvalidDataException("B2 bootstrap size refused");
        var value = JsonSerializer.Deserialize<RecoveryConfiguration>(bytes, new JsonSerializerOptions {
            MaxDepth = 12, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
        }) ?? throw new InvalidDataException("Missing B2 bootstrap");
        value.Validate();
        return value;
    }
    internal void Validate()
    {
        if (Version != 1 || Execution == Guid.Empty || Generation == Guid.Empty || Session == Guid.Empty
            || ControllerPid <= 0 || ControllerUid == 0 || Secret is not { Length: 32 }
            || Recovery is null || Owner is null || !Path.IsPathFullyQualified(EvidenceDirectory))
            throw new InvalidDataException("Invalid B2 bootstrap configuration");
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
    bool OwnerAcknowledged, bool ReleaseAuthority = false, string NativeStop = "Unproven", int RetainedDomains = 0, bool DomainRemoved = false);

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
