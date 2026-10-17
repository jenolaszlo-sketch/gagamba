// WP05 release/stop runtime vocabulary. The decision is a live-owner result;
// durable recovery classification remains separate and conservative.
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Execution;

internal enum ReleaseDecisionKind : byte
{
    Refused = 0,
    Released = 1,
    Duplicate = 2,
    Stopped = 3,
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
    long OverallDeadlineNanoseconds);

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
    string Reason);

internal sealed record StopDecision(
    bool Accepted,
    bool Duplicate,
    bool ReleaseAlreadyCommitted,
    bool DurableStop,
    StopCauseKind Cause,
    long StopAcceptedAtNanoseconds,
    long StopDeadlineNanoseconds,
    string Reason);
