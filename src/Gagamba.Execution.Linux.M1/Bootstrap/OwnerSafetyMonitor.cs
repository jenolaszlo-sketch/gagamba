using System.Diagnostics;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Restricted;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal sealed record OwnerDirectIdentity(Guid OriginalSession, DirectProcessIdentity Controller, DirectProcessIdentity Recovery,
    ExperimentalRuntimePin Runtime);
internal sealed record OwnerRendezvous(string Address, DirectAssociation Association,ushort Version=1,
    DirectBootstrapRole LifetimeRole=DirectBootstrapRole.OwnerProcessReference);

// Separate from the request/observation worker. Native stop happens BEFORE the arbiter's
// best-effort durable stop task, so journal I/O cannot hold up native safety stopping.
internal sealed class OwnerSafetyMonitor : IDisposable
{
    private readonly object _gate = new();
    private readonly SafeFileHandle _controller, _recovery, _lifeline;
    private readonly DirectProcessIdentity _controllerIdentity, _recoveryIdentity;
    private readonly long _setup, _overall;
    private readonly Thread _monitor;
    private readonly ManualResetEventSlim _wake = new(false);
    private DomainLease? _domain;
    private OwnerProtocolChannel? _controllerChannel;
    private PreparedTransaction? _capability;
    private ReleaseArbiter? _arbiter;
    private Thread? _stopWorker;
    private int _stopped, _done, _released, _retained, _resources;
    private bool _closing;
    private StopCauseKind? _firstCause;
    private Launch.ProtectedCapture? _capture;
    internal void BindCapture(Launch.ProtectedCapture capture)
    { lock (_gate) { _capture = capture; if (StopLatched) capture.BoundDrain(Now()+6_000_000_000L); } }
    internal Action<StopCauseKind,bool>? QualificationNativeStop { get; set; }
    internal int ControllerDescriptor => _controller.DangerousGetHandle().ToInt32();
    internal bool StopLatched => Volatile.Read(ref _stopped) != 0;
    internal bool RetentionConfirmed => Volatile.Read(ref _retained) != 0;
    internal bool ResourcesValid => Volatile.Read(ref _resources) != 0;
    internal OwnerSafetyMonitor(int controllerPidfd, int recoveryPidfd, int lifeline,
        OwnerDirectIdentity identity, long acceptedAt, long expiresAt)
    {
        _controller = new SafeFileHandle((IntPtr)controllerPidfd,true);
        _recovery = new SafeFileHandle((IntPtr)recoveryPidfd,true);
        _lifeline = new SafeFileHandle((IntPtr)lifeline,true);
        _controllerIdentity = identity.Controller; _recoveryIdentity = identity.Recovery;
        _setup = Math.Min(checked(acceptedAt + 10_000_000_000L),expiresAt);
        _overall = Math.Min(checked(acceptedAt + 30_000_000_000L),expiresAt);
        _monitor = new Thread(Monitor) { IsBackground=true, Name="gagamba-owner-safety" };
        _monitor.Start();
    }
    internal void PublishDomain(DomainLease domain)
    {
        lock (_gate)
        {
            if (_domain is not null) throw new InvalidOperationException("Repeated safety domain publication");
            _domain = domain.DuplicateForRecovery();
            if (StopLatched) _ = _domain.RequestStop();
        }
    }
    internal void BindController(OwnerProtocolChannel channel)
    {
        lock (_gate)
        {
            if (_controllerChannel is not null) throw new InvalidOperationException("Repeated original controller session");
            _controllerChannel=channel;
        }
    }
    internal void ConfirmRetention() => Interlocked.Exchange(ref _retained,1);
    internal bool PublishCapability(PreparedTransaction capability)
    {
        lock (_gate)
        {
            _capability=capability;
            if (!StopLatched) return true;
            _=capability.TryConsume(out _); return false;
        }
    }
    internal void BindArbiter(ReleaseArbiter arbiter)
    {
        lock (_gate) { if (_closing) throw new ObjectDisposedException(nameof(OwnerSafetyMonitor)); _arbiter=arbiter; if (StopLatched) DispatchStop(_firstCause ?? StopCauseKind.SetupFault); }
    }
    internal void Released() => Interlocked.Exchange(ref _released,1);
    private bool LifelineAlive()
    {
        var poll=new DirectUnix.PollFd {Fd=_lifeline.DangerousGetHandle().ToInt32(),Events=1|0x2000};
        return DirectUnix.Poll(ref poll,(UIntPtr)1,0)>=0 && poll.Revents==0;
    }
    internal bool RecoveryAlive => !StopLatched && RetentionConfirmed && LifelineAlive()
        && DirectPeerIdentity.LiveReference(_recovery.DangerousGetHandle().ToInt32(),_recoveryIdentity);
    internal bool ControllerAlive => !StopLatched
        && DirectPeerIdentity.LiveReference(_controller.DangerousGetHandle().ToInt32(),_controllerIdentity)
        && (_controllerChannel?.IsAlive ?? false);
    internal void AcceptStop(StopCauseKind cause)
    {
        bool first;
        var kernelAccepted=false;
        lock (_gate)
        {
            first = !StopLatched;
            _firstCause ??= cause;
            Interlocked.Exchange(ref _stopped, 1);
            _capture?.BoundDrain(Now()+6_000_000_000L);
            // Native stop does not depend on callbacks, request channel or durable stop settlement.
            if (_domain is not null) kernelAccepted=_domain.RequestStop().Ok;
            if (_arbiter is null && _capability is not null) _ = _capability.TryConsume(out _);
            DispatchStop(_firstCause.Value);
        }
        if(first) _=FdPassing.TrySendMessage(_lifeline.DangerousGetHandle().ToInt32(),[2],out _);
        if(first) QualificationNativeStop?.Invoke(cause,kernelAccepted);
    }
    private void DispatchStop(StopCauseKind cause)
    {
        if (_arbiter is not null && _stopWorker is null)
        {
            var arbiter=_arbiter;
            _stopWorker=new Thread(() => arbiter.RequestStop(cause)) {IsBackground=true,Name="gagamba-owner-stop"};
            _stopWorker.Start();
        }
    }
    private void Monitor()
    {
        try
        {
            while (Volatile.Read(ref _done)==0)
            {
                var controllerLive=DirectPeerIdentity.LiveReference(_controller.DangerousGetHandle().ToInt32(),_controllerIdentity);
                var recoveryLive=DirectPeerIdentity.LiveReference(_recovery.DangerousGetHandle().ToInt32(),_recoveryIdentity);
                var poll=new DirectUnix.PollFd {Fd=_lifeline.DangerousGetHandle().ToInt32(),Events=1|0x2000};
                var healthy=DirectUnix.Poll(ref poll,(UIntPtr)1,0)>=0 && poll.Revents==0;
                bool channelLive,releaseCommitted;
                lock(_gate)
                {
                    releaseCommitted=Volatile.Read(ref _released)!=0 || _arbiter?.RuntimeReleaseCommitted==true;
                    channelLive=_controllerChannel?.IsAlive ?? true; // original pidfd governs before association
                    if(_domain is not null)
                        Volatile.Write(ref _resources,_domain.AuthorityAlive && _domain.VerifyIdentity().Match
                            && _domain.TryReadWp04Limits(out var limits,out _)
                            && limits.GetValueOrDefault("memory.max")=="134217728"
                            && limits.GetValueOrDefault("memory.swap.max")=="0"
                            && limits.GetValueOrDefault("pids.max")=="48" ? 1 : 0);
                }
                var now=Now();
                if(!controllerLive || !channelLive) AcceptStop(StopCauseKind.CallerLoss);
                else if(!recoveryLive || !healthy) AcceptStop(StopCauseKind.SetupFault);
                else if(now>=_overall || (!releaseCommitted && now>=_setup)) AcceptStop(StopCauseKind.SetupExpired);
                if(StopLatched) { lock(_gate) { if(_domain is not null) { _ = _domain.RequestStop();
                    var population = _domain.ObservePopulation();
                    if (population.Known && population.Empty) _capture?.BoundDrain(Now()+1_000_000_000L); } } }
                _wake.Wait(10);
            }
        }
        catch { AcceptStop(StopCauseKind.SetupFault); }
    }
    internal static long Now()
    {
        var ticks=Stopwatch.GetTimestamp(); return ticks/Stopwatch.Frequency*1_000_000_000L
            +ticks%Stopwatch.Frequency*1_000_000_000L/Stopwatch.Frequency;
    }
    public void Dispose()
    {
        // Keep native monitoring active while the arbiter settles its durable stop. If disk I/O
        // wedges, independent recovery can still stop the domain and observe/refuse live-owner takeover.
        AcceptStop(StopCauseKind.CallerStop);
        Thread? worker;
        lock(_gate) { _closing=true; worker=_stopWorker; }
        worker?.Join();
        Interlocked.Exchange(ref _done,1); _wake.Set();
        _monitor.Join(); // monitor never waits on IPC, observation, journal or callbacks
        lock(_gate) { _domain?.Dispose(); }
        _controller.Dispose(); _recovery.Dispose(); _lifeline.Dispose(); _wake.Dispose();
    }
}
