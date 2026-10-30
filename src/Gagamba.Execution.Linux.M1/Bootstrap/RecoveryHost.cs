using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal static class RecoveryHost
{
    internal static int Run(string[] args)
    {
        if (args.Length != 0 || !RecoveryInterop.Supported) return 2;
        try { return RunCore(); }
        catch { return 3; } // Never echo configuration or session credentials.
    }

    private static int RunCore()
    {
        if (!RecoveryInterop.IsUnprivileged) return 3;
        var (control, credential) = RecoveryConfiguration.DescriptorRoles(
            Environment.GetEnvironmentVariable("GAGAMBA_B2_CONTROL_FD"), Environment.GetEnvironmentVariable("GAGAMBA_B2_CREDENTIAL_FD"));
        Environment.SetEnvironmentVariable("GAGAMBA_B2_CONTROL_FD", null);
        Environment.SetEnvironmentVariable("GAGAMBA_B2_CREDENTIAL_FD", null);
        var expectedParent = RecoveryInterop.GetParentPid();
        var uid = RecoveryInterop.GetUid();
        if (!RecoveryInterop.VerifyPeer(control, expectedParent, uid) || !RecoveryInterop.VerifyPeer(credential, expectedParent, uid)
            || !RecoveryInterop.DistinctSockets(control, credential)
            || !RecoveryInterop.Protect(control) || !RecoveryInterop.Protect(credential)) return 3;
        using var controlHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)control, true);
        RecoveryConfiguration config;
        byte[] packet;
        try
        {
            if (!FdPassing.TryReceiveMessage(credential, 5000, out packet, out _) || packet.Length > RecoveryConfiguration.MaxBytes) return 3;
            try { config = RecoveryConfiguration.Decode(packet); }
            finally { CryptographicOperations.ZeroMemory(packet); }
        }
        finally { PreparationInterop.CloseFd(credential); }
        config.Validate();
        using var association = new RecoveryAssociation(config.Session, config.Secret);
        CryptographicOperations.ZeroMemory(config.Secret);
        if (config.ControllerPid != expectedParent || config.ControllerUid != uid
            || config.Recovery.Verify("Gagamba.Execution.Linux.M1.Recovery") != System.Reflection.Assembly.GetEntryAssembly()!.Location)
            return 3;
        var ownerPath = config.Owner.Verify("Gagamba.Execution.Linux.M1.Owner");
        if (!PidFd.TryOpen(expectedParent, out var controllerFd, out _) || !PreparationInterop.HasCloexec(controllerFd)) return 3;
        using var controllerHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)controllerFd, true);
        if (!PidFd.TryExited(controllerFd, 0, out var controllerExited, out _) || controllerExited || !RecoveryInterop.DetachLifetime()) return 3;
        using var scope = new M1RecoveryScope();
        if (!scope.TryInitialize().Ok) return 3;
        // Domain-bearing requests are not part of the B2 wire. A supplied parent
        // must nevertheless pass existing M1 verification; never provision it.
        if (config.DelegatedParent is not null && !scope.TryOpenDelegatedParent(config.DelegatedParent).Ok) return 3;
        var wire = new RecoveryWire(config.Execution, config.Generation);
        if (!FdPassing.TryReceiveMessage(control, 5000, out var frame, out _)
            || !wire.Accept(frame, out var type, out var payload) || type != RecoveryMessage.Associate
            || !association.Authenticate(payload)) return 3;
        CryptographicOperations.ZeroMemory(payload); CryptographicOperations.ZeroMemory(frame);

        if(config.Integrated is not null)
            return IntegratedRecoveryHost.Run(config,scope,wire,control,controllerFd,ownerPath);

        Process owner;
        OwnerProtocolChannel? ownerChannel = null;
        if (config.DelegatedParent is not null)
        {
            owner = B2RetainedDomain.Start(scope, config, ownerPath);
        }
        else
        {
            if (!FdPassing.TryCreateInheritableSocketPair(out var recoveryEnd, out var ownerEnd, out _)) return 3;
            ownerChannel = new OwnerProtocolChannel(recoveryEnd, config.Execution, config.Generation, true);
            if (!RecoveryInterop.Protect(recoveryEnd)) { PreparationInterop.CloseFd(ownerEnd); ownerChannel.Dispose(); return 3; }
            var spawned = scope.TrySpawnOwner(ownerPath,
                ["--exec", config.Execution.ToString("D"), "--lease", config.Generation.ToString("D"), "--b2-lifeline", "1"], ownerEnd);
            if (!spawned.Ok) { ownerChannel.Dispose(); return 3; }
            owner = spawned.Process!;
            if (!ownerChannel.TrySend(OwnerProtocolMessageType.B2StartLifeline,
                    JsonSerializer.SerializeToUtf8Bytes(new B2OwnerStart(Environment.ProcessId, config.EvidenceDirectory)), out _)
                || !ownerChannel.TryReceiveAllowedWithin(3000, out _, out _, OwnerProtocolMessageType.B2LifelineReady))
            { ownerChannel.Dispose(); return 3; }
        }
        using var ownedOwnerChannel = ownerChannel;
        var obligation = new RecoveryObligation();
        var controllerLost = false;
        var ownerExited = false;
        var stopSent = false;
        var controlUsable = true;
        var ownerUsable = ownerChannel is not null;
        var nativeStop = "Unproven";
        var domainRemoved = false;
        var lifetimeDeadline = Stopwatch.GetTimestamp() + 30 * Stopwatch.Frequency;
        RecoverySnapshot Snapshot() => new(Environment.ProcessId, owner.Id, expectedParent, RecoveryInterop.GetSid(0),
            scope.SubreaperEnabled, controllerLost, ownerExited, obligation.Count, obligation.Cause, obligation.Deadline, obligation.OwnerAcknowledged, NativeStop: nativeStop,
            RetainedDomains: scope.RetainedRecoveryLeases.Count, DomainRemoved: domainRemoved);
        void Stop(string cause)
        {
            if (obligation.Request(cause, Stopwatch.GetTimestamp()))
            {
                // Nonblocking, dedicated owner socket. No observation/capture reader participates.
                if (ownerChannel is null)
                {
                    nativeStop = B2RetainedDomain.Stop(scope);
                    scope.CloseOwnerLifeline(owner);
                    return;
                }
                stopSent = ownerChannel.TrySend(OwnerProtocolMessageType.B2SafetyStop, [], out _);
                if (!stopSent) { ownerChannel.Dispose(); ownerUsable = false; } // EOF is independent owner stop signal.
            }
        }
        RecoveryEvidence.Write(config.EvidenceDirectory, "ready.json", Snapshot());
        if (!FdPassing.TrySendMessage(control, wire.Create(RecoveryMessage.Ready, JsonSerializer.SerializeToUtf8Bytes(Snapshot())), out _))
        { controllerLost = true; controlUsable = false; Stop("ControllerTransportLost"); }
        RecoverySnapshot? last = null;
        while (true)
        {
            if (!PidFd.TryExited(controllerFd, 0, out controllerExited, out var controllerError))
            {
                if (!RecoveryInterop.IsTransientPidfdError(controllerError))
                { controllerLost = true; Stop("ControllerExitEvidenceUnavailable"); }
            }
            else if (controllerExited) { controllerLost = true; Stop("ControllerExited"); }
            if (!scope.TryOwnerPidFdExited(owner, 0, out var exited, out var ownerError))
            {
                if (!RecoveryInterop.IsTransientPidfdError(ownerError)) Stop("OwnerExitEvidenceUnavailable");
            }
            else if (exited && !ownerExited)
            {
                ownerExited = scope.WaitForOwner(owner, TimeSpan.FromSeconds(1)).Exited;
                Stop("OwnerExited");
                if (config.DelegatedParent is not null) domainRemoved = B2RetainedDomain.RemoveAfterExit(scope, owner);
                RecoveryEvidence.Write(config.EvidenceDirectory, "owner-exit.json", Snapshot());
            }
            if (controlUsable)
            {
                if (FdPassing.TryReceiveMessage(control, 10, out frame, out var error))
                {
                    if (!wire.Accept(frame, out type, out payload))
                    { controlUsable = false; Stop("InvalidControllerFrame"); }
                    else if (type == RecoveryMessage.Stop) Stop("ControllerStop");
                    else if (type == RecoveryMessage.Inspect)
                    {
                        if (!FdPassing.TrySendMessage(control, wire.Create(RecoveryMessage.Snapshot, JsonSerializer.SerializeToUtf8Bytes(Snapshot())), out _))
                        { controlUsable = false; controllerLost = true; Stop("ControllerTransportLost"); }
                    }
                    else // Reconnect and duplicate association cannot restore any generation.
                    {
                        FdPassing.TrySendMessage(control, wire.Create(RecoveryMessage.Refused), out _);
                        Stop("ControllerAssociationRefused");
                    }
                }
                else if (!RecoveryInterop.IsTransientReceiveError(error))
                { controlUsable = false; controllerLost = true; Stop("ControllerTransportLost"); }
            }
            if (ownerUsable && ownerChannel is not null)
            {
                if (ownerChannel.TryReceiveAllowedWithin(0, out _, out var error, OwnerProtocolMessageType.B2StopObserved))
                {
                    if (stopSent) obligation.OwnerAcknowledged = true;
                }
                else if (!RecoveryInterop.IsTransientReceiveError(error)) { ownerUsable = false; Stop("OwnerTransportLost"); }
            }
            var current = Snapshot();
            if (obligation.Count != 0 && current != last)
            {
                RecoveryEvidence.Write(config.EvidenceDirectory, "stop.json", current);
                last = current;
            }
            if (Stopwatch.GetTimestamp() >= lifetimeDeadline)
            {
                Stop("BootstrapLifetimeExpired");
                if (ownerExited) return 0;
            }
            // No deadline is renewed. Unknown stop acknowledgement remains Unknown.
            Thread.Sleep(10);
        }
    }
}

internal sealed record B2OwnerStart(int RecoveryPid, string EvidenceDirectory);
internal static class B2OwnerLifeline
{
    internal static int Run(int fd, Guid execution, Guid generation)
    {
        using var channel = new OwnerProtocolChannel(fd, execution, generation, true);
        if (!RecoveryInterop.IsUnprivileged || !RecoveryInterop.Protect(fd)
            || !channel.TryReceive(OwnerProtocolMessageType.B2StartLifeline, out var message, out _)) return 3;
        var start = JsonSerializer.Deserialize<B2OwnerStart>(message!.Payload);
        if (start is null || start.RecoveryPid != RecoveryInterop.GetParentPid()
            || !RecoveryInterop.VerifyPeer(fd, start.RecoveryPid, RecoveryInterop.GetUid())
            || !Path.IsPathFullyQualified(start.EvidenceDirectory)) return 3;
        if (!PidFd.TryOpen(start.RecoveryPid, out var pidfd, out _) || !PreparationInterop.HasCloexec(pidfd)) return 3;
        using var recoveryPidfd = new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)pidfd, true);
        if (!channel.TrySend(OwnerProtocolMessageType.B2LifelineReady, [], out _)) return 3;
        var deadline = Stopwatch.GetTimestamp() + 35 * Stopwatch.Frequency;
        var cause = "RecoveryLifelineLost";
        var ack = false;
        while (Stopwatch.GetTimestamp() < deadline)
        {
            if (!PidFd.TryExited(pidfd, 0, out var exited, out var pidfdError))
            {
                if (RecoveryInterop.IsTransientPidfdError(pidfdError)) continue;
                cause = "RecoveryExitEvidenceUnavailable";
                break;
            }
            if (exited) break;
            if (channel.TryReceiveAllowedWithin(20, out _, out var error, OwnerProtocolMessageType.B2SafetyStop))
            {
                cause = "RecoveryStopRequested";
                // A test-owned marker withholds the fixture acknowledgement only.
                if (!File.Exists(Path.Combine(start.EvidenceDirectory, "withhold-ack")))
                    ack = channel.TrySend(OwnerProtocolMessageType.B2StopObserved, [], out _);
                break;
            }
            if (!RecoveryInterop.IsTransientReceiveError(error)) break;
        }
        RecoveryEvidence.Write(start.EvidenceDirectory, "owner-stop.json", new {
            OwnerPid = Environment.ProcessId, RecoveryPid = start.RecoveryPid, Cause = cause,
            StopCount = 1, AcknowledgementSent = ack, ReleaseAuthority = false, NativeStop = "Unproven" });
        return 0;
    }
}
