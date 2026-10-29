using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// Internal commissioning bootstrap only. No public provider registration or reconnect listener.
internal sealed class RecoveryConnection : IDisposable
{
    internal Process Process { get; }
    internal RecoveryWire Wire { get; }
    internal int Control { get; }
    private RecoveryConnection(Process process, RecoveryWire wire, int control) { Process = process; Wire = wire; Control = control; }
    internal static RecoveryConnection Start(RecoveryConfiguration requested)
    {
        if (!RecoveryInterop.Supported || !RecoveryInterop.IsUnprivileged) throw new InvalidOperationException("B2 requires unprivileged Linux");
        var config = requested with { ControllerPid = Environment.ProcessId, ControllerUid = RecoveryInterop.GetUid(), Secret = RandomNumberGenerator.GetBytes(32) };
        config.Validate();
        var recovery = config.Recovery.Verify("Gagamba.Execution.Linux.M1.Recovery");
        config.Owner.Verify("Gagamba.Execution.Linux.M1.Owner");
        var control = -1; var childControl = -1; var credential = -1; var childCredential = -1;
        Process? child = null;
        try
        {
            if (!FdPassing.TryCreateInheritableSocketPair(out control, out childControl, out _)
                || !FdPassing.TryCreateInheritableSocketPair(out credential, out childCredential, out _)
                || !RecoveryInterop.Protect(control) || !RecoveryInterop.Protect(credential)) throw new IOException("Private channels unavailable");
            var start = new ProcessStartInfo("/usr/bin/dotnet") { UseShellExecute = false,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(recovery);
            start.Environment["GAGAMBA_B2_CONTROL_FD"] = childControl.ToString(System.Globalization.CultureInfo.InvariantCulture);
            start.Environment["GAGAMBA_B2_CREDENTIAL_FD"] = childCredential.ToString(System.Globalization.CultureInfo.InvariantCulture);
            child = Process.Start(start) ?? throw new IOException("Recovery spawn failed");
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
            if (!FdPassing.TryReceiveMessage(control, 7000, out var ready, out _)
                || !wire.Accept(ready, out var type, out _) || type != RecoveryMessage.Ready)
                throw new IOException("Recovery association refused");
            var connection = new RecoveryConnection(child, wire, control);
            control = -1; child = null;
            return connection;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(config.Secret);
            foreach (var fd in new[] { control, childControl, credential, childCredential }) if (fd >= 0) PreparationInterop.CloseFd(fd);
            // A failed bootstrap has no admitted operation. Only the exact test/installation child is terminated.
            if (child is not null) { if (!child.HasExited) child.Kill(); child.WaitForExit(5000); child.Dispose(); }
        }
    }
    internal bool Send(RecoveryMessage type) => FdPassing.TrySendMessage(Control, Wire.Create(type), out _);
    internal RecoverySnapshot? Inspect()
    {
        if (!Send(RecoveryMessage.Inspect) || !FdPassing.TryReceiveMessage(Control, 1000, out var frame, out _)
            || !Wire.Accept(frame, out var type, out var payload) || type != RecoveryMessage.Snapshot) return null;
        return JsonSerializer.Deserialize<RecoverySnapshot>(payload);
    }
    // Cancelling a caller's wait never owns the process lifetime or stop obligation.
    internal async Task<RecoverySnapshot?> ObserveAsync(CancellationToken cancellation)
    {
        await Task.Yield(); cancellation.ThrowIfCancellationRequested(); return Inspect();
    }
    public void Dispose() { PreparationInterop.CloseFd(Control); Process.Dispose(); }
}
