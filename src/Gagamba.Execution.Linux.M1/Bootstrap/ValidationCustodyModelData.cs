namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal enum ValidationCustodyFact : byte
{
    Unknown = 0,
    Consistent = 1,
    Contradictory = 2
}

internal enum ValidationCustodyEvent : byte
{
    ObserveIssue = 1,
    ObserveOffer = 2,
    ObserveDrop = 3,
    ObserveConsume = 4,
    Complete = 5
}

internal enum ValidationCustodyState : byte
{
    PendingModel = 1,
    IssuedModel = 2,
    OfferedModel = 3,
    DroppedModel = 4,
    ConsumedModel = 5,
    Closed = 6
}

internal enum ValidationCustodyRefusal : byte
{
    None = 0,
    Context = 1,
    Comparison = 2,
    Evidence = 3,
    Transition = 4,
    Stopped = 5,
    Expired = 6
}

internal sealed class ValidationCustodyBinding
{
    private readonly byte[] launcherExecutable;
    private readonly byte[] launcherClosure;
    private readonly byte[] controllerExecutable;
    private readonly byte[] controllerClosure;
    private readonly byte[] driverExecutable;
    private readonly byte[] driverClosure;
    private readonly byte[] artifactSet;
    private readonly byte[] catalog;
    private readonly byte[] manifest;
    private readonly byte[] policy;
    private readonly byte[] script;
    private readonly byte[] review;
    private readonly byte[] ownerReceipt;
    private readonly byte[] approval;
    private readonly byte[] capability;

    internal ValidationCustodyBinding(
        ValidationPreContext pre,
        Gava1Ids ids,
        Guid host,
        Guid boot,
        Guid selection,
        Guid launcherOriginal,
        Guid controllerOriginal,
        byte[]? launcherExecutable,
        byte[]? launcherClosure,
        byte[]? controllerExecutable,
        byte[]? controllerClosure,
        byte[]? driverExecutable,
        byte[]? driverClosure,
        byte[]? artifactSet,
        byte[]? catalog,
        byte[]? manifest,
        byte[]? policy,
        byte[]? script,
        byte[]? review,
        byte[]? ownerReceipt,
        byte[]? approval,
        byte[]? capability,
        string caseId,
        long admitBeforeNs,
        long sessionEndNs,
        string? qualificationId,
        string? qualificationRecordSha256)
    {
        Pre = pre;
        Ids = ids;
        Host = host;
        Boot = boot;
        Selection = selection;
        LauncherOriginal = launcherOriginal;
        ControllerOriginal = controllerOriginal;
        this.launcherExecutable = CopyDigest(launcherExecutable);
        this.launcherClosure = CopyDigest(launcherClosure);
        this.controllerExecutable = CopyDigest(controllerExecutable);
        this.controllerClosure = CopyDigest(controllerClosure);
        this.driverExecutable = CopyDigest(driverExecutable);
        this.driverClosure = CopyDigest(driverClosure);
        this.artifactSet = CopyDigest(artifactSet);
        this.catalog = CopyDigest(catalog);
        this.manifest = CopyDigest(manifest);
        this.policy = CopyDigest(policy);
        this.script = CopyDigest(script);
        this.review = CopyDigest(review);
        this.ownerReceipt = CopyDigest(ownerReceipt);
        this.approval = CopyDigest(approval);
        this.capability = CopyDigest(capability);
        CaseId = caseId is { Length: <= 128 } ? caseId : string.Empty;
        AdmitBeforeNs = admitBeforeNs;
        SessionEndNs = sessionEndNs;
        // The pre-only shape requires null. Retain only the presence of invalid values.
        QualificationId = qualificationId is null ? null : string.Empty;
        QualificationRecordSha256 = qualificationRecordSha256 is null ? null : string.Empty;
    }

    internal ValidationPreContext Pre { get; }
    internal Gava1Ids Ids { get; }
    internal Guid Host { get; }
    internal Guid Boot { get; }
    internal Guid Selection { get; }
    internal Guid LauncherOriginal { get; }
    internal Guid ControllerOriginal { get; }
    internal byte[] LauncherExecutable => launcherExecutable.ToArray();
    internal byte[] LauncherClosure => launcherClosure.ToArray();
    internal byte[] ControllerExecutable => controllerExecutable.ToArray();
    internal byte[] ControllerClosure => controllerClosure.ToArray();
    internal byte[] DriverExecutable => driverExecutable.ToArray();
    internal byte[] DriverClosure => driverClosure.ToArray();
    internal byte[] ArtifactSet => artifactSet.ToArray();
    internal byte[] Catalog => catalog.ToArray();
    internal byte[] Manifest => manifest.ToArray();
    internal byte[] Policy => policy.ToArray();
    internal byte[] Script => script.ToArray();
    internal byte[] Review => review.ToArray();
    internal byte[] OwnerReceipt => ownerReceipt.ToArray();
    internal byte[] Approval => approval.ToArray();
    internal byte[] Capability => capability.ToArray();
    internal string CaseId { get; }
    internal long AdmitBeforeNs { get; }
    internal long SessionEndNs { get; }
    internal string? QualificationId { get; }
    internal string? QualificationRecordSha256 { get; }

    internal ValidationCustodyBinding Copy() => new(
        Pre, Ids, Host, Boot, Selection, LauncherOriginal, ControllerOriginal,
        launcherExecutable, launcherClosure, controllerExecutable, controllerClosure,
        driverExecutable, driverClosure, artifactSet, catalog, manifest, policy, script,
        review, ownerReceipt, approval, capability, CaseId, AdmitBeforeNs, SessionEndNs,
        QualificationId, QualificationRecordSha256);

    internal bool IsWellFormed =>
        Pre.IsPre && Ids.Complete &&
        Host != Guid.Empty && Boot != Guid.Empty && Selection != Guid.Empty &&
        LauncherOriginal != Guid.Empty && ControllerOriginal != Guid.Empty &&
        LauncherOriginal != ControllerOriginal &&
        launcherExecutable.Length == 32 &&
        launcherClosure.Length == 32 &&
        controllerExecutable.Length == 32 &&
        controllerClosure.Length == 32 &&
        driverExecutable.Length == 32 &&
        driverClosure.Length == 32 &&
        artifactSet.Length == 32 &&
        catalog.Length == 32 &&
        manifest.Length == 32 &&
        policy.Length == 32 &&
        script.Length == 32 &&
        review.Length == 32 &&
        ownerReceipt.Length == 32 &&
        approval.Length == 32 &&
        capability.Length == 32 &&
        IsCaseId(CaseId) && AdmitBeforeNs > 0 && SessionEndNs >= AdmitBeforeNs &&
        QualificationId is null && QualificationRecordSha256 is null;

    internal bool Matches(ValidationCustodyBinding observed) =>
        Pre == observed.Pre &&
        Ids == observed.Ids &&
        Host == observed.Host &&
        Boot == observed.Boot &&
        Selection == observed.Selection &&
        LauncherOriginal == observed.LauncherOriginal &&
        ControllerOriginal == observed.ControllerOriginal &&
        launcherExecutable.AsSpan().SequenceEqual(observed.launcherExecutable) &&
        launcherClosure.AsSpan().SequenceEqual(observed.launcherClosure) &&
        controllerExecutable.AsSpan().SequenceEqual(observed.controllerExecutable) &&
        controllerClosure.AsSpan().SequenceEqual(observed.controllerClosure) &&
        driverExecutable.AsSpan().SequenceEqual(observed.driverExecutable) &&
        driverClosure.AsSpan().SequenceEqual(observed.driverClosure) &&
        artifactSet.AsSpan().SequenceEqual(observed.artifactSet) &&
        catalog.AsSpan().SequenceEqual(observed.catalog) &&
        manifest.AsSpan().SequenceEqual(observed.manifest) &&
        policy.AsSpan().SequenceEqual(observed.policy) &&
        script.AsSpan().SequenceEqual(observed.script) &&
        review.AsSpan().SequenceEqual(observed.review) &&
        ownerReceipt.AsSpan().SequenceEqual(observed.ownerReceipt) &&
        approval.AsSpan().SequenceEqual(observed.approval) &&
        capability.AsSpan().SequenceEqual(observed.capability) &&
        string.Equals(CaseId, observed.CaseId, StringComparison.Ordinal) &&
        AdmitBeforeNs == observed.AdmitBeforeNs &&
        SessionEndNs == observed.SessionEndNs &&
        string.Equals(QualificationId, observed.QualificationId, StringComparison.Ordinal) &&
        string.Equals(QualificationRecordSha256, observed.QualificationRecordSha256, StringComparison.Ordinal);

    private static byte[] CopyDigest(byte[]? value) => value is { Length: 32 } ? value.ToArray() : [];

    private static bool IsCaseId(string value)
    {
        if (value.Length == 0 || value[0] == '.' || value[^1] == '.')
        {
            return false;
        }

        foreach (char c in value)
        {
            if (c is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or '-'))
            {
                return false;
            }
        }

        return true;
    }
}

internal sealed class ValidationCustodyExpectations
{
    // Missing operands are represented for the model's Context refusal.
    internal ValidationCustodyExpectations(ValidationCustodyBinding binding) => Binding = binding?.Copy()!;

    internal ValidationCustodyBinding Binding { get; }
}

internal sealed class ValidationCustodyEvidence
{
    internal ValidationCustodyEvidence(
        ValidationCustodyBinding binding,
        ValidationCustodyFact protectedInputs,
        ValidationCustodyFact provenance,
        ValidationCustodyFact approvalCurrent,
        ValidationCustodyFact originalAssociation,
        ValidationCustodyFact liveOriginals,
        ValidationCustodyFact freshAuthorization,
        ValidationCustodyFact issuanceCommitted,
        ValidationCustodyFact offerValidated,
        ValidationCustodyFact permanentDrop,
        ValidationCustodyFact consumptionCommitted,
        ValidationCustodyFact cleanupConfirmed)
    {
        Binding = binding?.Copy()!;
        ProtectedInputs = protectedInputs;
        Provenance = provenance;
        ApprovalCurrent = approvalCurrent;
        OriginalAssociation = originalAssociation;
        LiveOriginals = liveOriginals;
        FreshAuthorization = freshAuthorization;
        IssuanceCommitted = issuanceCommitted;
        OfferValidated = offerValidated;
        PermanentDrop = permanentDrop;
        ConsumptionCommitted = consumptionCommitted;
        CleanupConfirmed = cleanupConfirmed;
    }

    internal ValidationCustodyBinding Binding { get; }
    internal ValidationCustodyFact ProtectedInputs { get; }
    internal ValidationCustodyFact Provenance { get; }
    internal ValidationCustodyFact ApprovalCurrent { get; }
    internal ValidationCustodyFact OriginalAssociation { get; }
    internal ValidationCustodyFact LiveOriginals { get; }
    internal ValidationCustodyFact FreshAuthorization { get; }
    internal ValidationCustodyFact IssuanceCommitted { get; }
    internal ValidationCustodyFact OfferValidated { get; }
    internal ValidationCustodyFact PermanentDrop { get; }
    internal ValidationCustodyFact ConsumptionCommitted { get; }
    internal ValidationCustodyFact CleanupConfirmed { get; }

    internal bool HasDefinedFacts =>
        ProtectedInputs <= ValidationCustodyFact.Contradictory &&
        Provenance <= ValidationCustodyFact.Contradictory &&
        ApprovalCurrent <= ValidationCustodyFact.Contradictory &&
        OriginalAssociation <= ValidationCustodyFact.Contradictory &&
        LiveOriginals <= ValidationCustodyFact.Contradictory &&
        FreshAuthorization <= ValidationCustodyFact.Contradictory &&
        IssuanceCommitted <= ValidationCustodyFact.Contradictory &&
        OfferValidated <= ValidationCustodyFact.Contradictory &&
        PermanentDrop <= ValidationCustodyFact.Contradictory &&
        ConsumptionCommitted <= ValidationCustodyFact.Contradictory &&
        CleanupConfirmed <= ValidationCustodyFact.Contradictory;

    internal bool MatchesStage(ValidationCustodyState stage) =>
        ProtectedInputs == ValidationCustodyFact.Consistent &&
        Provenance == ValidationCustodyFact.Consistent &&
        ApprovalCurrent == ValidationCustodyFact.Consistent &&
        OriginalAssociation == ValidationCustodyFact.Consistent &&
        LiveOriginals == ValidationCustodyFact.Consistent &&
        FreshAuthorization == FactDue(stage, ValidationCustodyState.IssuedModel) &&
        IssuanceCommitted == FactDue(stage, ValidationCustodyState.IssuedModel) &&
        OfferValidated == FactDue(stage, ValidationCustodyState.OfferedModel) &&
        PermanentDrop == FactDue(stage, ValidationCustodyState.DroppedModel) &&
        ConsumptionCommitted == FactDue(stage, ValidationCustodyState.ConsumedModel) &&
        CleanupConfirmed == FactDue(stage, ValidationCustodyState.Closed);

    private static ValidationCustodyFact FactDue(ValidationCustodyState stage, ValidationCustodyState requiredFrom) =>
        stage >= requiredFrom ? ValidationCustodyFact.Consistent : ValidationCustodyFact.Unknown;
}

internal sealed class ValidationCustodyModelSnapshot
{
    internal const string EvidenceKind = "supplied-structural-model";

    internal ValidationCustodyModelSnapshot(ValidationCustodyState state, ValidationCustodyBinding binding)
    {
        State = state;
        Binding = binding.Copy();
    }

    internal ValidationCustodyState State { get; }
    internal ValidationCustodyBinding Binding { get; }
}
