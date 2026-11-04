using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Native;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal static class QualifiedRecoveryEcho
{
    internal static LoadedInstallation Run(RecoveryConfiguration config,RecoveryWire wire,int control,int controllerFd)
    {
        long deadline=config.B2DeadlineNs??Math.Min(InstallationDeadline.Create(),config.Integrated!.ExpiresAtNs);
        InstallationDeadline.Validate(deadline);deadline=Math.Min(deadline,checked(InstallationDeadline.Now+5_000_000_000L));
        var expected=config.B2InstallationIdentity??config.Integrated!.InstallationIdentity!;
        void Service()
        {
            B2ProcessIdentity.RequireLive(controllerFd,config.ControllerPid);
            var poll=new DirectUnix.PollFd{Fd=control,Events=1|0x2000};
            if(DirectUnix.Poll(ref poll,(UIntPtr)1,0)<0 || (poll.Revents&(0x2000|8|16|32))!=0)
                throw new IOException("controller lost during recovery verification");
            if((poll.Revents&1)!=0) throw new IOException("stop or premature control during recovery verification");
        }
        var selected=InstallationDeadline.Verify(()=>
        {
            var selection=LoadedInstallation.Verify("recovery",expected);
            if(config.Integrated is null) selection.OwnerLaunch=QualifiedLauncher.InstalledPlan.Prepare(selection,"owner");
            return selection;
        },deadline,Service);
        if(!FdPassing.TrySendMessage(control,wire.Create(RecoveryMessage.InstallationEchoRequest,InstallationWire.Echo(selected.Identity,0)),out _))
            throw new IOException("recovery echo send failed");
        while(true)
        {
            B2ProcessIdentity.RequireLive(controllerFd,config.ControllerPid);
            int wait=InstallationDeadline.Remaining(deadline,10);
            if(FdPassing.TryReceiveMessage(control,wait,out var frame,out var error))
            {
                if(!wire.Accept(frame,out var type,out var payload) || type!=RecoveryMessage.InstallationEchoResponse)
                    throw new IOException("recovery response transition refused");
                InstallationWire.AcceptResponse(payload,expected);B2ProcessIdentity.RequireLive(controllerFd,config.ControllerPid);return selected;
            }
            if(!RecoveryInterop.IsTransientReceiveError(error)) throw new IOException("recovery response lost");
        }
    }
}
