// GP-2 matrix integrity: the contract is only as honest as its cells.
// Every platform lists every capability; levels and kinds stay coherent;
// every grant names its evidence.
namespace Gagamba.Execution;

public static class MatrixValidation
{
    private static readonly ExecutionCapability[] AllCapabilities =
        (ExecutionCapability[])Enum.GetValues(typeof(ExecutionCapability));

    public static IReadOnlyList<string> Validate(PlatformCapabilities platform)
    {
        var errors = new List<string>();
        foreach (var cap in AllCapabilities)
        {
            if (!platform.TryGet(cap, out var grant))
            {
                errors.Add($"{platform.Platform}: missing {cap}");
                continue;
            }
            if (string.IsNullOrWhiteSpace(grant.Evidence))
                errors.Add($"{platform.Platform}: {cap} cites no evidence");
            bool kindOk = grant.Level switch
            {
                CapabilityLevel.Absent => grant.Kind == GuaranteeKind.None,
                _ => grant.Kind is GuaranteeKind.Native or GuaranteeKind.Constructed,
            };
            if (!kindOk)
                errors.Add($"{platform.Platform}: {cap} has incoherent {grant.Level}/{grant.Kind}");
        }
        return errors;
    }

    public static IReadOnlyList<string> ValidateAll(
        IEnumerable<PlatformCapabilities> platforms)
    {
        var errors = new List<string>();
        foreach (var p in platforms)
            errors.AddRange(Validate(p));
        return errors;
    }
}
