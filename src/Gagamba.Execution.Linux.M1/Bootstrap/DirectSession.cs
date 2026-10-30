using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Gagamba.Execution.Linux.M1.Native;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal sealed record DirectAssociation(Guid Execution, Guid Generation, Guid OriginalSession,
    DirectProcessIdentity Controller, DirectProcessIdentity Owner, DirectProcessIdentity Recovery)
{
    internal void Validate()
    {
        if (Execution == Guid.Empty || Generation == Guid.Empty || OriginalSession == Guid.Empty
            || Controller.Pid == Owner.Pid || Controller.Pid == Recovery.Pid || Owner.Pid == Recovery.Pid)
            throw new InvalidDataException("Original direct association refused");
        Controller.Validate(); Owner.Validate(); Recovery.Validate();
    }
}

// No resumption API. This object owns exactly one fresh direct connection, both sequences,
// and socket-derived peer pidfd. HMAC confirms the role/identity transcript; OS identity is
// the trust root. This is not isolation against compromised bootstrap or hostile same-UID memory access.
internal sealed class DirectSession : IDisposable
{
    private const uint Magic = 0x31444947; // GID1
    private const int Header = 64;
    private readonly DirectStream _stream;
    private readonly DirectAssociation _association;
    private readonly SafeFileHandle _peer;
    private readonly DirectProcessIdentity _expected;
    private uint _sent, _received;
    private bool _established;
    internal int Descriptor => _stream.Descriptor;
    internal int PeerProcessDescriptor => _peer.DangerousGetHandle().ToInt32();
    internal bool IsAlive
    {
        get
        {
            var retained=false;
            try
            {
                if(!_established) return false;
                _peer.DangerousAddRef(ref retained);
                return _established && _stream.IsAlive()
                    && DirectPeerIdentity.LiveReference(_peer.DangerousGetHandle().ToInt32(),_expected);
            }
            catch(ObjectDisposedException) {return false;}
            finally {if(retained) _peer.DangerousRelease();}
        }
    }
    private DirectSession(DirectStream stream, DirectAssociation association, int expectedPidfd, bool owner)
    {
        association.Validate(); _stream = stream; _association = association;
        _expected = owner ? association.Controller : association.Owner;
        var self = DirectProcessIdentity.Current();
        if (self != (owner ? association.Owner : association.Controller)) throw new IOException("Direct local role refused");
        _peer = DirectPeerIdentity.Verify(stream.Descriptor, _expected, expectedPidfd);
    }
    internal static DirectSession Establish(DirectStream stream, DirectAssociation association,
        int expectedPidfd, bool owner, long deadline)
    {
        DirectSession? session = null;
        byte[] secret = [], ownChallenge = [], peerChallenge = [], transcript = [], payload = [];
        try
        {
            // Both local checks complete before either endpoint sends any challenge/key material.
            // Client validates server hello before sending its secret; server sends no secret.
            session = new DirectSession(stream, association, expectedPidfd, owner);
            ownChallenge = RandomNumberGenerator.GetBytes(32);
            if (owner)
            {
                session.SendFrame(1, ownChallenge, deadline);
                payload = session.ReceiveFrame(2, deadline);
                if (payload.Length != 64) throw new IOException("Direct challenge shape refused");
                peerChallenge = payload[..32]; secret = payload[32..];
                transcript = Transcript(association, peerChallenge, ownChallenge);
                session.SendFrame(3, Confirmation(secret, transcript, "owner-confirm"), deadline);
                var confirmation = session.ReceiveFrame(4, deadline);
                try { VerifyConfirmation(secret, transcript, "controller-confirm", confirmation); }
                finally { CryptographicOperations.ZeroMemory(confirmation); }
                session.SendFrame(5, Confirmation(secret, transcript, "owner-established"), deadline);
            }
            else
            {
                peerChallenge = session.ReceiveFrame(1, deadline);
                if (peerChallenge.Length != 32) throw new IOException("Direct challenge shape refused");
                secret = RandomNumberGenerator.GetBytes(32);
                payload = new byte[64]; ownChallenge.CopyTo(payload, 0); secret.CopyTo(payload, 32);
                session.SendFrame(2, payload, deadline);
                transcript = Transcript(association, ownChallenge, peerChallenge);
                var confirmation = session.ReceiveFrame(3, deadline);
                try { VerifyConfirmation(secret, transcript, "owner-confirm", confirmation); }
                finally { CryptographicOperations.ZeroMemory(confirmation); }
                session.SendFrame(4, Confirmation(secret, transcript, "controller-confirm"), deadline);
                confirmation = session.ReceiveFrame(5, deadline);
                try { VerifyConfirmation(secret, transcript, "owner-established", confirmation); }
                finally { CryptographicOperations.ZeroMemory(confirmation); }
            }
            if (!DirectPeerIdentity.LiveReference(expectedPidfd, session._expected)
                || !DirectPeerIdentity.LiveReference(session._peer.DangerousGetHandle().ToInt32(), session._expected))
                throw new IOException("Peer exited during direct handshake");
            session._established = true;
            return session;
        }
        catch { session?.Dispose(); stream.Dispose(); throw; }
        finally
        {
            foreach (var bytes in new[] { secret, ownChallenge, peerChallenge, transcript, payload })
                CryptographicOperations.ZeroMemory(bytes);
        }
    }
    internal static byte[] Transcript(DirectAssociation a, byte[] controllerChallenge, byte[] ownerChallenge)
    {
        using var buffer = new MemoryStream(); using var writer = new BinaryWriter(buffer, Encoding.UTF8, true);
        writer.Write("gagamba-int01-direct-v1"); writer.Write("original-controller"); writer.Write("transaction-owner");
        writer.Write(a.Execution.ToByteArray()); writer.Write(a.Generation.ToByteArray()); writer.Write(a.OriginalSession.ToByteArray());
        foreach (var identity in new[] { a.Controller, a.Owner, a.Recovery })
        {
            writer.Write(identity.Pid); writer.Write(identity.Uid); writer.Write(identity.Gid);
            writer.Write(identity.PidNamespace); writer.Write(identity.UserNamespace);
        }
        writer.Write(controllerChallenge); writer.Write(ownerChallenge); writer.Flush(); return buffer.ToArray();
    }
    internal static byte[] Confirmation(byte[] secret, byte[] transcript, string role)
    {
        var bytes = Encoding.ASCII.GetBytes(role);
        var input = new byte[transcript.Length + bytes.Length]; transcript.CopyTo(input, 0); bytes.CopyTo(input, transcript.Length);
        try { return HMACSHA256.HashData(secret, input); }
        finally { CryptographicOperations.ZeroMemory(input); }
    }
    private static void VerifyConfirmation(byte[] secret, byte[] transcript, string role, byte[] actual)
    {
        var expected = Confirmation(secret, transcript, role);
        try { if (!CryptographicOperations.FixedTimeEquals(expected, actual)) throw new IOException("Direct transcript confirmation refused"); }
        finally { CryptographicOperations.ZeroMemory(expected); }
    }
    private void SendFrame(ushort type, byte[] payload, long deadline)
    {
        var frame = new byte[Header + payload.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(frame, Magic); BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(6), type);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(8), checked(++_sent));
        _association.Execution.TryWriteBytes(frame.AsSpan(12, 16)); _association.Generation.TryWriteBytes(frame.AsSpan(28, 16));
        _association.OriginalSession.TryWriteBytes(frame.AsSpan(44, 16));
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(60), payload.Length); payload.CopyTo(frame, Header);
        try { _stream.Send(frame, deadline); }
        finally { CryptographicOperations.ZeroMemory(frame); }
    }
    private byte[] ReceiveFrame(ushort expected, long deadline)
    {
        if (!TryReceiveFrame(expected, deadline, out var payload)) throw new IOException("Direct handshake timed out");
        return payload;
    }
    private bool TryReceiveFrame(ushort expected, long deadline, out byte[] payload)
    {
        payload = [];
        if (!_stream.TryReceive(deadline, out var frame)) return false;
        try
        {
            if (frame.Length < Header || BinaryPrimitives.ReadUInt32LittleEndian(frame) != Magic
                || BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(4)) != 1
                || BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(6)) != expected
                || BinaryPrimitives.ReadUInt32LittleEndian(frame.AsSpan(8)) != checked(_received + 1)
                || new Guid(frame.AsSpan(12, 16)) != _association.Execution || new Guid(frame.AsSpan(28, 16)) != _association.Generation
                || new Guid(frame.AsSpan(44, 16)) != _association.OriginalSession
                || BinaryPrimitives.ReadInt32LittleEndian(frame.AsSpan(60)) != frame.Length - Header)
                throw new IOException("Direct version/role/identity/sequence refused");
            _received++; payload = frame[Header..]; return true;
        }
        catch { _stream.Fail(); throw; }
        finally { CryptographicOperations.ZeroMemory(frame); }
    }
    internal void SendControl(byte[] encodedOwnerFrame, int timeout = 5000)
    {
        if (!IsAlive) throw new IOException("Original direct session lost");
        SendFrame(16, encodedOwnerFrame, DirectStream.Deadline(timeout));
    }
    internal bool TryReceiveControl(int timeout, out byte[] frame)
    {
        if (!_established) throw new IOException("Direct session not established");
        return TryReceiveFrame(16, DirectStream.Deadline(timeout), out frame);
    }
    public void Dispose() { _established = false; _peer.Dispose(); _stream.Dispose(); }
}
