using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Native;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// Normal qualified integrated initialization only. This is not manifest or
// RELEASE authority, and it cannot authorize prepublication validation. Slice E
// must supply its independent context atomically; no validation exception exists.
internal static class RecoveryInitializationAdmission
{
    internal static void Require(LoadedInstallation? retained, InstallationIdentity? expected)
    {
        RequireSelection(retained, expected);
        RequireEligible(retained!.Selection, expected!, retained.VerifyFinalAdmission());
    }

    private static void RequireSelection(LoadedInstallation? retained, InstallationIdentity? expected)
    {
        if (retained is null || expected is null)
            throw new InvalidDataException("recovery initialization requires retained installed authority");
        if (retained.Selection.State != InstalledState.Qualified || retained.Identity != expected ||
            retained.Selection.QualificationId is null || retained.Selection.QualificationRecordSha256 is null)
            throw new InvalidDataException("recovery initialization installation binding refused");
    }

    // The same exact-tuple decision is used by both real protected reads. A bare
    // binding, process authentication or a successful S2 echo is not a grant.
    internal static void RequireEligible(InstalledResult retained, InstallationIdentity expected,
        FinalAdmissionVerifier.Grant? grant)
    {
        if (retained.Identity != expected || grant is null ||
            !AdmissionAuthority.IsFinalAdmissionEligible(retained, grant))
            throw new InvalidDataException("recovery initialization final admission refused");
        ReleaseGenerationGate.RequireStillQualified(grant.Selection, retained);
    }

    internal static void RequireIntegrated(RecoveryConfiguration config, LoadedInstallation? retained,
        int control, int controllerFd)
    {
        config.Validate();
        var spec = config.Integrated ?? throw new InvalidDataException("integrated recovery specification required");
        if (config.Version == 1)
        {
            if (retained is not null)
                throw new InvalidDataException("legacy recovery cannot carry installed authority");
            return;
        }

        // Refuse missing/mismatched context before even attempting native reads.
        RequireSelection(retained, spec.InstallationIdentity);
        // Never extend the original setup/operation cutoff. The existing
        // five-second bootstrap scan cap also bounds a direct caller's read.
        long deadline = Math.Min(Math.Min(spec.ExpiresAtNs,
            checked(spec.AcceptedAtNs + 10_000_000_000L)),
            checked(InstallationDeadline.Now + 5_000_000_000L));
        void Service()
        {
            B2ProcessIdentity.RequireLive(controllerFd, config.ControllerPid);
            var poll = new DirectUnix.PollFd { Fd = control, Events = 1 | 0x2000 };
            if (DirectUnix.Poll(ref poll, (UIntPtr)1, 0) < 0 || poll.Revents != 0)
                throw new IOException("stop or controller loss during recovery initialization verification");
        }
        Service();
        _ = InstallationDeadline.Remaining(deadline);
        _ = InstallationDeadline.Verify(() =>
        {
            Require(retained, spec.InstallationIdentity);
            return true;
        }, deadline, Service);
    }
}

#if !GAGAMBA_PRODUCTION
// Observer only: it cannot supply authority or skip a check. Counts entry into
// initialization separately from later attempted/completed native effects.
internal static class RecoveryInitializationTestFixture
{
    internal static readonly AsyncLocal<Action?> BeforeInitialization = new();
}
#endif
