using Gagamba.Execution.Linux.M1.Installed;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// Phase policy only. Kernel authentication, S2 and loaded consumption are performed
// by the calling actors; this object cannot acquire or manufacture any of those proofs.
internal sealed class B2InstallationBarrier
{
    private readonly InstallationIdentity _expected;
    private readonly Guid _session;
    private readonly int _controller,_recovery;
    private readonly long _deadline;
    private B2Binding? _binding;
    private int _phase;
    internal string? FirstFailure {get;private set;}
    internal bool Qualified=>_phase==6 && FirstFailure is null;
    internal B2InstallationBarrier(InstallationIdentity expected,Guid session,int controller,int recovery,long deadline)
    {expected.Validate();_expected=expected;_session=session;_controller=controller;_recovery=recovery;_deadline=deadline;}
    internal void Stop(string cause) {FirstFailure??=cause;_phase=-1;}
    private void Require(bool condition,string cause)
    {if(!condition || FirstFailure is not null) {Stop(cause);throw new InvalidDataException("B2 installation barrier: "+FirstFailure);}}
    private void At(int phase,long now)
    {Require(now>0 && now<_deadline,"Deadline");Require(_phase==phase,"Phase");}
    internal void Recovery(byte[] echo,long now)
    {At(0,now);try{Require(InstallationWire.CompareEcho(echo,_expected,0)==InstallationMismatch.None,"RecoveryIdentity");_phase=1;}catch{Stop("RecoveryIdentity");throw;}}
    internal void Bound(B2Binding binding,long now)
    {
        At(1,now);try{binding.Validate();}catch{Stop("OriginalBinding");throw;}
        Require(binding.Session==_session && binding.ControllerPid==_controller && binding.RecoveryPid==_recovery && binding.DeadlineNs==_deadline,"OriginalBinding");
        _binding=binding;_phase=2;
    }
    internal void Owner(byte[] echo,long now)
    {At(2,now);try{Require(InstallationWire.CompareEcho(echo,_expected,1)==InstallationMismatch.None,"OwnerIdentity");_phase=3;}catch{Stop("OwnerIdentity");throw;}}
    // Caller invokes this only after its original S2 selection and both live pins revalidate.
    internal byte[] Confirmation(long now)
    {At(3,now);_phase=4;return InstallationWire.Confirmation(_session,_expected);}
    internal void Ready(byte[] snapshot,long now)
    {At(4,now);try{_=InstallationWire.Ready(snapshot,_binding!);_phase=5;}catch{Stop("Snapshot");throw;}}
    internal void Commit(long now) {At(5,now);_phase=6;}
}

internal sealed class B2RecoveryReadyBarrier
{
    private readonly B2Binding _binding;
    private readonly InstallationIdentity _expected;
    private int _phase;
    internal string? FirstFailure {get;private set;}
    internal B2RecoveryReadyBarrier(B2Binding binding,InstallationIdentity expected)
    {binding.Validate();expected.Validate();_binding=binding;_expected=expected;}
    internal void Stop(string cause) {FirstFailure??=cause;_phase=-1;}
    private void At(int phase,long now)
    {
        if(FirstFailure is not null || _phase!=phase || now<=0 || now>=_binding.DeadlineNs)
        {Stop("PhaseOrDeadline");throw new InvalidDataException("recovery Ready barrier: "+FirstFailure);}
    }
    internal void Lifeline(long now) {At(0,now);_phase=1;}
    internal void ReferencesSent(long now) {At(1,now);_phase=2;}
    internal void Confirm(byte[] confirmation,long now)
    {At(2,now);try{InstallationWire.AcceptConfirmation(confirmation,_binding.Session,_expected);_phase=3;}catch{Stop("Confirmation");throw;}}
    internal void Ready(byte[] snapshot,long now)
    {At(3,now);try{_=InstallationWire.Ready(snapshot,_binding);_phase=4;}catch{Stop("Snapshot");throw;}}
}
