// WP05 release/stop runtime vocabulary. The decision is a live-owner result;
// durable recovery classification remains separate and conservative.
using System.Collections.Immutable;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Control;

namespace Gagamba.Execution.Linux.M1.Execution;

internal enum ReleaseDecisionKind : byte
{
    Refused = 0,
    Released = 1,
    Duplicate = 2,
    Stopped = 3,
    AmbiguousPersistence = 4,
}

/// <summary>R1-F03 Option A position of one caller-supplied approval
/// correlation against the operation's admitted binding. Unknown means no
/// binding has been admitted and this call did not establish one.</summary>
internal enum ReleaseCorrelationBinding : byte
{
    Unknown = 0,
    Accepted = 1,
    Mismatch = 2,
    InvalidArgument = 3,
}

/// <summary>WP05 fake prerequisites. Resource and capture work is deliberately
/// not implemented in this tranche.</summary>
internal sealed record ReleasePrerequisites(
    bool ResourcesReady,
    bool CaptureReady,
    bool CallerAlive,
    bool RecoveryAlive)
{
    internal bool Satisfied => ResourcesReady && CaptureReady && CallerAlive && RecoveryAlive;
}

internal sealed record ReleaseRequest(
    PreparationIdentity Identity,
    long SetupDeadlineNanoseconds,
    long OverallDeadlineNanoseconds,
    ImmutableArray<byte> ApprovalCorrelationSha256 = default);

internal interface IReleaseWriteChannel
{
    ReleasePipeWriteResult TryWriteOnce(ReadOnlyMemory<byte> frame);
}

internal sealed record ReleaseDecision(
    ReleaseDecisionKind Kind,
    ReleaseDisposition DurableRelease,
    DeliveryResult Delivery,
    bool DurableCommitted,
    bool LiveExecutionExcluded,
    string Reason,
    ReleaseCorrelationBinding Binding = ReleaseCorrelationBinding.Unknown,
    RestrictedRefusalReason RefusalReason = RestrictedRefusalReason.None);

internal sealed record StopDecision(
    bool Accepted,
    bool Duplicate,
    bool ReleaseAlreadyCommitted,
    bool DurableStop,
    StopCauseKind Cause,
    long StopAcceptedAtNanoseconds,
    long StopDeadlineNanoseconds,
    string Reason);
