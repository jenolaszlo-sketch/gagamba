using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// Internal commissioning bootstrap only. No public provider registration or reconnect listener.
// M-01: the control descriptor is shared between Inspect()/ObserveAsync()/Send() and
// concurrent disposal, so every use and the single close serialize on one gate. A
// disposed or closing connection never exposes its descriptor to IPC.
internal sealed class RecoveryConnection : IDisposable
{
    private readonly GuardedDescriptor _control;
    internal Process Process { get; }
    internal RecoveryWire Wire { get; }
    internal int Control => _control.Descriptor;
    internal OwnerRendezvous? Rendezvous { get; private set; }
    internal Microsoft.Win32.SafeHandles.SafeFileHandle? OwnerReference { get; private set; }
    internal Microsoft.Win32.SafeHandles.SafeFileHandle? RecoveryReference { get; private set; }
    internal RecoveryConnection(Process process, RecoveryWire wire, int control)
    {
        Process = process;
        Wire = wire;
        _control = new GuardedDescriptor(control);
    }
    internal static RecoveryConnection Start(RecoveryConfiguration requested)
    {
        if (!RecoveryInterop.Supported || !RecoveryInterop.IsUnprivileged) throw new InvalidOperationException("B2 requires unprivileged Linux");
        var config = requested with { ControllerPid = Environment.ProcessId, ControllerUid = RecoveryInterop.GetUid(), Secret = RandomNumberGenerator.GetBytes(32) };
        if(config.Integrated is not null)
            config=config with {Integrated=config.Integrated with {Controller=DirectProcessIdentity.Current()}};
        config.Validate();
        if(config.Integrated is { } integrated)
        {
            integrated.Runtime.VerifyCurrent();
            ExperimentalRuntimePin.VerifyPrivateClosure(config.Recovery);
            ExperimentalRuntimePin.VerifyPrivateClosure(config.Owner);
            if(!DirectPeerIdentity.FeatureAvailable()) throw new IOException("SO_PEERPIDFD prerequisite absent");
        }
        var recovery = config.Recovery.Verify("Gagamba.Execution.Linux.M1.Recovery");
        config.Owner.Verify("Gagamba.Execution.Linux.M1.Owner");
        var control = -1; var childControl = -1; var credential = -1; var childCredential = -1;
        Process? child = null;
        Microsoft.Win32.SafeHandles.SafeFileHandle? ownerReference=null,recoveryReference=null;
        try
        {
            if (!FdPassing.TryCreateInheritableSocketPair(out control, out childControl, out _)
                || !FdPassing.TryCreateInheritableSocketPair(out credential, out childCredential, out _)
                || !RecoveryInterop.Protect(control) || !RecoveryInterop.Protect(credential)) throw new IOException("Private channels unavailable");
            var start = new ProcessStartInfo("/usr/bin/dotnet") { UseShellExecute = false,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(recovery);
            if(config.Integrated is not null) start=config.Integrated.Runtime.StartInfo(recovery);
            start.Environment["GAGAMBA_B2_CONTROL_FD"] = childControl.ToString(System.Globalization.CultureInfo.InvariantCulture);
            start.Environment["GAGAMBA_B2_CREDENTIAL_FD"] = childCredential.ToString(System.Globalization.CultureInfo.InvariantCulture);
            child = Process.Start(start) ?? throw new IOException("Recovery spawn failed");
            if(config.Integrated is not null)
            {
                if(!PidFd.TryOpen(child.Id,out var pidfd,out _)) throw new IOException("Recovery lifetime unavailable");
                recoveryReference=new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)pidfd,true);
            }
            PreparationInterop.CloseFd(childControl); childControl = -1;
            PreparationInterop.CloseFd(childCredential); childCredential = -1;
            var packet = JsonSerializer.SerializeToUtf8Bytes(config);
            try
            {
                if (packet.Length > RecoveryConfiguration.MaxBytes || !FdPassing.TrySendMessage(credential, packet, out _))
                    throw new IOException("Bootstrap transfer refused");
            }
            finally { CryptographicOperations.ZeroMemory(packet); }
            var wire = new RecoveryWire(config.Execution, config.Generation);
            var auth = new byte[48]; config.Session.TryWriteBytes(auth); config.Secret.CopyTo(auth, 16);
            var frame = wire.Create(RecoveryMessage.Associate, auth);
            try { if (!FdPassing.TrySendMessage(control, frame, out _)) throw new IOException("Association transfer failed"); }
            finally { CryptographicOperations.ZeroMemory(auth); CryptographicOperations.ZeroMemory(frame); }
            OwnerRendezvous? rendezvous=null;
            if(config.Integrated is not null)
            {
                if(!FdPassing.TryReceiveRecord(control,7000,out var record,out _)) throw new IOException("Recovery rendezvous missing");
                using(record!)
                {
                    if(!wire.Accept(record!.Payload,out var type,out var data) || type!=RecoveryMessage.Rendezvous
                        || record.DescriptorCount!=1) throw new IOException("Recovery lifetime role refused");
                    rendezvous=JsonSerializer.Deserialize<OwnerRendezvous>(data) ?? throw new IOException("Rendezvous identity missing");
                    if(rendezvous.Version!=1 || rendezvous.LifetimeRole!=DirectBootstrapRole.OwnerProcessReference)
                        throw new IOException("Owner lifetime role refused");
                    var association=rendezvous.Association; association.Validate();
                    if(association.Execution!=config.Execution || association.Generation!=config.Integrated.StoreGeneration
                        || association.OriginalSession!=config.Session || association.Controller!=config.Integrated.Controller
                        || association.Recovery!=(DirectProcessIdentity.Current() with {Pid=child.Id}))
                        throw new IOException("Immutable recovery/owner association refused");
                    var descriptors=record.DetachDescriptors();
                    ownerReference=new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)descriptors[0],true);
                    if(!DirectPeerIdentity.LiveReference(descriptors[0],association.Owner)
                        || !DirectPeerIdentity.LiveReference(recoveryReference!.DangerousGetHandle().ToInt32(),association.Recovery))
                        throw new IOException("Original helper lifetime lost");
                }
            }
            else if (!FdPassing.TryReceiveMessage(control, 7000, out var ready, out _)
                || !wire.Accept(ready, out var type, out _) || type != RecoveryMessage.Ready)
                throw new IOException("Recovery association refused");
            var connection = new RecoveryConnection(child, wire, control) {Rendezvous=rendezvous,
                OwnerReference=ownerReference,RecoveryReference=recoveryReference};
            ownerReference=null; recoveryReference=null;
            control = -1; child = null;
            return connection;
        }
        finally
        {
            ownerReference?.Dispose(); recoveryReference?.Dispose();
            CryptographicOperations.ZeroMemory(config.Secret);
            foreach (var fd in new[] { control, childControl, credential, childCredential }) if (fd >= 0) PreparationInterop.CloseFd(fd);
            // A failed bootstrap has no admitted operation. Only the exact test/installation child is terminated.
            if (child is not null) { if (!child.HasExited) child.Kill(); child.WaitForExit(5000); child.Dispose(); }
        }
    }
    internal bool Send(RecoveryMessage type) =>
        _control.TryUse(control => FdPassing.TrySendMessage(control, Wire.Create(type), out _));
    // The whole send/receive exchange runs under the lifecycle gate: disposal
    // waits for it and the descriptor cannot close or be reused mid-frame.
    // An unusable or disposed connection yields the existing loss observation
    // (null) without touching the descriptor.
    internal RecoverySnapshot? Inspect()
    {
        RecoverySnapshot? snapshot = null;
        _ = _control.TryUse(control =>
            FdPassing.TrySendMessage(control, Wire.Create(RecoveryMessage.Inspect), out _)
            && FdPassing.TryReceiveMessage(control, 1000, out var frame, out _)
            && Wire.Accept(frame, out var type, out var payload)
            && type == RecoveryMessage.Snapshot
            && (snapshot = JsonSerializer.Deserialize<RecoverySnapshot>(payload)) is not null);
        return snapshot;
    }
    // Cancelling a caller's wait never owns the process lifetime or stop obligation.
    internal async Task<RecoverySnapshot?> ObserveAsync(CancellationToken cancellation)
    {
        await Task.Yield(); cancellation.ThrowIfCancellationRequested(); return Inspect();
    }
    public void Dispose()
    {
        _control.Dispose();
        OwnerReference?.Dispose(); RecoveryReference?.Dispose();
        Process.Dispose();
    }
}
