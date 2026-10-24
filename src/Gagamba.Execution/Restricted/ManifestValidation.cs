namespace Gagamba.Execution.Restricted;

/// <summary>Additional schema checks shared by parsed and internally projected public views.
/// Existing immutable model constructors enforce the structural invariants.</summary>
internal static class ManifestValidation
{
    internal static void Validate(RestrictedManifest manifest)
    {
        var a = manifest.Authority;
        Strings(a.Namespaces); Strings(a.Capabilities); Strings(a.ReadOnlyDestinations);
        foreach (var store in a.WritableStores) Text(store.Path);
        var r = manifest.Resources;
        foreach (var budget in new[] { r.Memory, r.Swap, r.Tasks, r.FileDescriptors, r.TmpBytes,
            r.ShmBytes, r.StdoutBytes, r.StderrBytes, r.CaptureAggregateBytes, r.SetupTimeoutNs,
            r.OverallTimeoutNs, r.StopAllowanceNs, r.FinalDrainNs })
        {
            if (budget.Kind is not (BudgetValueKind.Absent or BudgetValueKind.Exact or BudgetValueKind.Unlimited) ||
                budget.Qualification is not (QualificationDisposition.Mandatory or QualificationDisposition.PendingQualification))
                throw new ArgumentException("Undefined budget tag.", nameof(manifest));
            Text(budget.Unit);
        }
        if (r.CoreDumps.CorePatternObserved is { } pattern) Text(pattern);
        foreach (var item in manifest.Closure.Objects)
        {
            Text(item.Role); Text(item.Destination); Text(item.BuildIdentity);
        }
        Strings(manifest.Closure.HelperDependencies);
        var i = manifest.Invocation;
        Text(i.ToolPath); Text(i.Mode); Text(i.InputPath); Text(i.WorkDirectory);
        Strings(i.Argv);
        foreach (var pair in i.Environment) { Text(pair.Key); Text(pair.Value); }
    }

    private static void Strings(IEnumerable<string> values)
    {
        foreach (var value in values) Text(value);
    }

    private static void Text(string value) => _ = ManifestCodec.Utf8ByteCount(value);
}
