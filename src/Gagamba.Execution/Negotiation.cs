// GP-2 negotiation: requested guarantees versus available capabilities.
// Fail-closed: any unsatisfied requirement rejects with reasons. Preferred
// capabilities are reported, never decisive. Constructed guarantees are
// named as such, never silently equated with native ones.
namespace Gagamba.Execution;

/// <summary>
/// One requested guarantee. Minimum is a floor, not a target: Full
/// satisfies a Partial minimum. AllowConstructed decides whether a
/// composed (watchdog-style) grant counts.
/// </summary>
public sealed record ExecutionRequirement(
    ExecutionCapability Capability,
    CapabilityLevel Minimum,
    bool AllowConstructed,
    string Reason = "")
{
    public static ExecutionRequirement Require(
        ExecutionCapability capability,
        CapabilityLevel minimum = CapabilityLevel.Full,
        bool allowConstructed = false,
        string reason = "") => new(capability, minimum, allowConstructed, reason);
}

/// <summary>Outcome of negotiating requirements against one platform.</summary>
public sealed record NegotiationResult(
    bool Accepted,
    IReadOnlyList<string> Unmet,
    IReadOnlyList<string> Met,
    IReadOnlyList<string> PreferredNotes)
{
    public static NegotiationResult Reject(IEnumerable<string> unmet) =>
        new(false, unmet.ToList(), Array.Empty<string>(), Array.Empty<string>());
}

public static class ExecutionNegotiator
{
    /// <summary>
    /// Accept iff every requirement is satisfied. Preferred capabilities add
    /// notes only. Empty requirements accept (nothing demanded).
    /// A composed overlay (platform-specific, e.g. watchdog + kill) can
    /// satisfy requirements that allow construction; the resulting Met
    /// entries name the composition explicitly.
    /// </summary>
    public static NegotiationResult Negotiate(
        PlatformCapabilities platform,
        IEnumerable<ExecutionRequirement> required,
        IEnumerable<ExecutionRequirement>? preferred = null,
        IReadOnlyDictionary<ExecutionCapability, CapabilityGrant>? composed = null)
    {
        var unmet = new List<string>();
        var met = new List<string>();
        foreach (var req in required)
        {
            platform.TryGet(req.Capability, out var grant);
            CapabilityGrant? comp = null;
            if (req.AllowConstructed)
                composed?.TryGetValue(req.Capability, out comp);
            bool nativeOk = grant is not null && grant.Satisfies(req.Minimum, allowConstructed: false);
            bool composedOk = comp is not null
                && comp.Satisfies(req.Minimum, allowConstructed: true);
            if (nativeOk)
            {
                met.Add($"{platform.Platform}: {req.Capability}={grant!.Level}/{grant.Kind}");
            }
            else if (composedOk)
            {
                met.Add($"{platform.Platform}: {req.Capability}={comp!.Level}/" +
                    $"Constructed(via {comp.Evidence})");
            }
            else
            {
                string have = grant is not null ? $"{grant.Level}/{grant.Kind}" : "unlisted";
                string why = string.IsNullOrWhiteSpace(req.Reason) ? "" : $" ({req.Reason})";
                unmet.Add($"{platform.Platform}: requires {req.Capability}>={req.Minimum}" +
                    (req.AllowConstructed ? " (constructed ok)" : " (native only)") +
                    $", has {have}{why}");
            }
        }
        var notes = new List<string>();
        foreach (var pref in preferred ?? Enumerable.Empty<ExecutionRequirement>())
        {
            if (platform.TryGet(pref.Capability, out var grant)
                && grant.Satisfies(pref.Minimum, pref.AllowConstructed))
                notes.Add($"{platform.Platform}: preferred {pref.Capability} holds ({grant.Level}/{grant.Kind})");
            else
                notes.Add($"{platform.Platform}: preferred {pref.Capability} missing (advisory only)");
        }
        return new NegotiationResult(unmet.Count == 0, unmet, met, notes);
    }
}
