// Cross-platform conformance: one runner, one expected matrix, run against
// each real provider so drift between platforms is caught the same way.
// The matrix encodes what we EXPECT from the frozen GF-2 capability grants
// and the GF-2.5/GM-2 evidence; the runner measures what the provider DOES.
namespace Gagamba.Conformance;

/// <summary>Outcome of one conformance leg for one provider.</summary>
public enum ConformanceOutcome
{
    /// <summary>The measured behavior matched the expected guarantee.</summary>
    Passed,
    /// <summary>The measured behavior contradicted the expected guarantee.</summary>
    Failed,
    /// <summary>Not expressible/available here (e.g. no setsid on Windows);
    /// the capability is still attested structurally. Never used to hide a
    /// failure.</summary>
    Skipped,
}

/// <summary>One conformance result, with the reason it holds.</summary>
public sealed record ConformanceLeg(string Name, ConformanceOutcome Outcome, string Detail);

/// <summary>The full conformance report for one provider on one host.</summary>
public sealed record ConformanceReport(string Platform, IReadOnlyList<ConformanceLeg> Legs)
{
    public ConformanceLeg? Find(string name) =>
        Legs.FirstOrDefault(l => l.Name == name);

    public ConformanceOutcome Outcome(string name) =>
        Find(name)?.Outcome ?? ConformanceOutcome.Skipped;
}
