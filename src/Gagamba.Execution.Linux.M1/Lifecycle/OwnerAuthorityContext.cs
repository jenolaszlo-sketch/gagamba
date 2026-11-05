using Gagamba.Execution.Linux.M1.Installed;

namespace Gagamba.Execution.Linux.M1.Lifecycle;

/// <summary>
/// Owner-local retained authority context for one owner process. The first
/// successful owner preflight supplies the normal qualified case; later echoes
/// compare against that exact retained selection and never replace it.
///
/// This is retention, not a grant. P1 does not authorize a manifest, staging,
/// HELD or RELEASE action from this context. The validation case intentionally
/// has no factory in this slice and remains unreachable until the separately
/// approved atomic validation-bootstrap integration. No caller Boolean selects
/// or upgrades an authority case.
/// </summary>
internal abstract class OwnerAuthorityContext
{
    private OwnerAuthorityContext()
    {
    }

    internal static OwnerAuthorityContext LegacyContext => Legacy.Instance;

    /// <summary>The normal qualified case, retaining the actual first owner-local
    /// verification (full identity, manifest, qualification id and raw record
    /// hash).</summary>
    internal sealed class Qualified : OwnerAuthorityContext
    {
        private readonly LoadedInstallation? _installation;

        private Qualified(LoadedInstallation? installation, InstalledResult selection)
        {
            _installation = installation;
            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
        }

        internal InstalledResult Selection { get; }
        internal InstallationIdentity Identity => Selection.Identity ??
            throw new InvalidDataException("retained qualified identity is absent");
        internal InstalledManifest Manifest => Selection.Manifest ??
            throw new InvalidDataException("retained qualified manifest is absent");
        internal string QualificationId => Selection.QualificationId ??
            throw new InvalidDataException("retained qualification id is absent");
        internal string QualificationRecordSha256 => Selection.QualificationRecordSha256 ??
            throw new InvalidDataException("retained qualification record hash is absent");

        internal LoadedInstallation Installation => _installation ??
            throw new InvalidOperationException("retained selection has no loaded installation");

        internal static Qualified FromVerified(LoadedInstallation installation) =>
            new(installation, installation.Selection);

        /// <summary>Require a later echo observation to match the first retained
        /// selection exactly. The observation is never adopted or cached in place
        /// of the retained tuple.</summary>
        internal void RequireEcho(InstalledResult observed) =>
            ReleaseGenerationGate.RequireStillQualified(observed, Selection);

#if !GAGAMBA_PRODUCTION
        // Deterministic tests need a retained-qualified context without a real
        // protected host installation. This factory is excluded from Release.
        internal static Qualified ForTest(InstalledResult selection) => new(null, selection);
#endif
    }

    /// <summary>The existing unqualified experimental case. It carries no
    /// installed identity and cannot be promoted to the qualified case.</summary>
    internal sealed class Legacy : OwnerAuthorityContext
    {
        internal static readonly Legacy Instance = new();

        private Legacy()
        {
        }
    }

    /// <summary>Reserved for the independently authorized validation context.
    /// P1 deliberately provides no factory, so ordinary callers cannot construct
    /// or obtain this case. The atomic validation-bootstrap slice must add its
    /// only factory together with the complete issuer/context/policy checks.</summary>
    internal sealed class Validation : OwnerAuthorityContext
    {
        private Validation()
        {
        }
    }
}
