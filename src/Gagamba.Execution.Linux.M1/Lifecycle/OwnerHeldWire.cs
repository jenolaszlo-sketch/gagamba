// B3 held-phase wire records for the controller-owned owner channel. These
// carry descriptions and decisions only: no capability, descriptor, bearer or
// release authority has a wire representation. Every payload is bounded,
// strictly decoded (unknown fields refuse) and identity-bound by the
// surrounding sequenced SOCK_SEQPACKET frame.
using System.Text.Json;
using System.Text.Json.Serialization;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Control;

namespace Gagamba.Execution.Linux.M1.Lifecycle;

internal sealed record OwnerReleaseRequestWire(
    Guid ExecutionId,
    Guid PreparationId,
    Guid GenerationId,
    byte[] ManifestDigest,
    byte[]? ApprovalCorrelationSha256);

internal sealed record OwnerReleaseDecisionWire(
    string ReleaseKind,
    string ReleaseDisposition,
    string Delivery,
    bool DurableCommit,
    string LiveExecutionExcluded,
    string CorrelationBinding,
    string RefusalReason,
    string Reason);

internal sealed record OwnerStopDecisionWire(
    bool Accepted,
    bool Duplicate,
    bool ReleaseAlreadyCommitted,
    bool DurableStop,
    string Cause,
    long AcceptedAtNanoseconds,
    long DeadlineNanoseconds,
    string Reason);

internal sealed record OwnerExecutionOutcomeWire(
    string Outcome,
    string ExecKind,
    int ExecErrno,
    string RootKind,
    int ExitCode,
    int Signal,
    bool CoreDumped,
    bool RawWaitPresent,
    int RawWaitStatus,
    bool WrapperExited,
    int WrapperExitCode,
    string DomainPopulation,
    ulong StdoutStoredBytes,
    ulong StdoutObservedBytes,
    bool StdoutEof,
    bool StdoutOverflow,
    ulong StderrStoredBytes,
    ulong StderrObservedBytes,
    bool StderrEof,
    bool StderrOverflow,
    bool CaptureReadFailure,
    byte[]? StdoutSha256,
    byte[]? StderrSha256,
    long ObservedAtNanoseconds,
    string Reason,
    int? PhysicalReleaseAttempts = null);

/// <summary>Strict bounded codec for the B3 held-phase payloads. The options
/// mirror the owner preparation description codec: unknown fields, excessive
/// depth and oversized input refuse before any decision is trusted.</summary>
internal static class OwnerHeldWireCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        MaxDepth = 24,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive = false,
    };

    internal const int MaxDecisionBytes = 1024;
    internal const int MaxOutcomeBytes = 4096;

    internal static byte[] EncodeReleaseRequest(OwnerReleaseRequestWire request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ExecutionId == Guid.Empty || request.PreparationId == Guid.Empty
            || request.GenerationId == Guid.Empty || request.ManifestDigest is not { Length: 32 }
            || request.ApprovalCorrelationSha256 is { Length: not 32 })
        {
            throw new ArgumentException("Release request identity is invalid.", nameof(request));
        }

        return EncodeJson(request, MaxDecisionBytes);
    }

    internal static bool TryDecodeReleaseRequest(
        ReadOnlySpan<byte> payload,
        out OwnerReleaseRequestWire? request,
        out string error)
    {
        request = null;
        if (!TryDecodeJson<OwnerReleaseRequestWire>(payload, MaxDecisionBytes, out var decoded, out error)
            || decoded is null)
        {
            error = error.Length == 0 ? "release request is invalid" : error;
            return false;
        }

        if (decoded.ExecutionId == Guid.Empty || decoded.PreparationId == Guid.Empty
            || decoded.GenerationId == Guid.Empty || decoded.ManifestDigest is not { Length: 32 }
            || decoded.ApprovalCorrelationSha256 is { Length: not 32 })
        {
            error = "release request identity is invalid";
            return false;
        }

        request = decoded;
        return true;
    }

    internal static byte[] EncodeReleaseDecision(ReleaseDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return EncodeJson(ToWire(decision), MaxDecisionBytes);
    }

    internal static bool TryDecodeReleaseDecision(
        ReadOnlySpan<byte> payload,
        out ReleaseDecision? decision,
        out string error)
    {
        decision = null;
        if (!TryDecodeJson<OwnerReleaseDecisionWire>(payload, MaxDecisionBytes, out var decoded, out error)
            || decoded is null)
        {
            error = error.Length == 0 ? "release decision is invalid" : error;
            return false;
        }

        if (!Enum.TryParse<ReleaseDecisionKind>(decoded.ReleaseKind, out var kind)
            || !Enum.IsDefined(kind)
            || !Enum.TryParse<ReleaseDisposition>(decoded.ReleaseDisposition, out var disposition)
            || !Enum.IsDefined(disposition)
            || !Enum.TryParse<DeliveryResult>(decoded.Delivery, out var delivery)
            || !Enum.IsDefined(delivery)
            || !Enum.TryParse<ReleaseCorrelationBinding>(decoded.CorrelationBinding, out var binding)
            || !Enum.IsDefined(binding)
            || !Enum.TryParse<RestrictedRefusalReason>(decoded.RefusalReason, out var reason)
            || !Enum.IsDefined(reason)
            || !TryDecodeLiveExcluded(decoded.LiveExecutionExcluded, out var liveExcluded)
            || decoded.Reason is null || decoded.Reason.Length > 512)
        {
            error = "release decision fields are not a valid bounded tag set";
            return false;
        }

        decision = new ReleaseDecision(kind, disposition, delivery, decoded.DurableCommit,
            liveExcluded, decoded.Reason, binding, reason);
        return true;
    }

    internal static byte[] EncodeStopDecision(StopDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return EncodeJson(ToWire(decision), MaxDecisionBytes);
    }

    internal static bool TryDecodeStopDecision(
        ReadOnlySpan<byte> payload,
        out StopDecision? decision,
        out string error)
    {
        decision = null;
        if (!TryDecodeJson<OwnerStopDecisionWire>(payload, MaxDecisionBytes, out var decoded, out error)
            || decoded is null)
        {
            error = error.Length == 0 ? "stop decision is invalid" : error;
            return false;
        }

        if (!Enum.TryParse<StopCauseKind>(decoded.Cause, out var cause)
            || !Enum.IsDefined(cause)
            || decoded.Reason is null || decoded.Reason.Length > 512
            || decoded.AcceptedAtNanoseconds < 0 || decoded.DeadlineNanoseconds < 0
            || (decoded.AcceptedAtNanoseconds > 0 && decoded.DeadlineNanoseconds > 0
                && decoded.DeadlineNanoseconds < decoded.AcceptedAtNanoseconds))
        {
            error = "stop decision fields are not a valid bounded tag set";
            return false;
        }

        decision = new StopDecision(decoded.Accepted, decoded.Duplicate,
            decoded.ReleaseAlreadyCommitted, decoded.DurableStop, cause,
            decoded.AcceptedAtNanoseconds, decoded.DeadlineNanoseconds, decoded.Reason);
        return true;
    }

    internal static byte[] EncodeExecutionOutcome(OwnerExecutionFacts facts, string reason)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(reason);
        return EncodeJson(new OwnerExecutionOutcomeWire(
            facts.Outcome.ToString(), facts.Exec.ToString(), facts.ExecErrno, facts.Root.ToString(),
            facts.ExitCode, facts.Signal, facts.CoreDumped, facts.RawWaitPresent, facts.RawWaitStatus,
            facts.WrapperExited, facts.WrapperExitCode,
            facts.DomainKnown ? (facts.DomainPopulated ? "Populated" : "Empty") : "Unknown",
            facts.StdoutStoredBytes, facts.StdoutObservedBytes, facts.StdoutEof, facts.StdoutOverflow,
            facts.StderrStoredBytes, facts.StderrObservedBytes, facts.StderrEof, facts.StderrOverflow,
            facts.CaptureReadFailure, facts.StdoutSha256, facts.StderrSha256, facts.ObservedAtNanoseconds,
            reason.Length > 512 ? reason[..512] : reason, facts.PhysicalReleaseAttempts), MaxOutcomeBytes);
    }

    internal static bool TryDecodeExecutionOutcome(
        ReadOnlySpan<byte> payload,
        out OwnerExecutionFacts? facts,
        out string reason,
        out string error)
    {
        facts = null;
        reason = string.Empty;
        if (!TryDecodeJson<OwnerExecutionOutcomeWire>(payload, MaxOutcomeBytes, out var decoded, out error)
            || decoded is null)
        {
            error = error.Length == 0 ? "execution outcome is invalid" : error;
            return false;
        }

        if (!Enum.TryParse<RestrictedOutcome>(decoded.Outcome, out var outcome) || !Enum.IsDefined(outcome)
            || !Enum.TryParse<ExecTag>(decoded.ExecKind, out var exec) || !Enum.IsDefined(exec)
            || !Enum.TryParse<RootKind>(decoded.RootKind, out var root) || !Enum.IsDefined(root)
            || decoded.DomainPopulation is not ("Empty" or "Populated" or "Unknown")
            || decoded.StdoutSha256 is { Length: not 32 } || decoded.StderrSha256 is { Length: not 32 }
            || decoded.PhysicalReleaseAttempts is < 0 or > 1
            || decoded.ObservedAtNanoseconds <= 0
            || decoded.Reason is null || decoded.Reason.Length > 512)
        {
            error = "execution outcome fields are not a valid bounded tag set";
            return false;
        }

        facts = new OwnerExecutionFacts(
            outcome, exec, decoded.ExecErrno, root, decoded.ExitCode, decoded.Signal, decoded.CoreDumped,
            decoded.RawWaitPresent, decoded.RawWaitStatus, decoded.WrapperExited, decoded.WrapperExitCode,
            decoded.DomainPopulation != "Unknown", decoded.DomainPopulation == "Populated",
            decoded.StdoutStoredBytes, decoded.StdoutObservedBytes, decoded.StdoutEof, decoded.StdoutOverflow,
            decoded.StderrStoredBytes, decoded.StderrObservedBytes, decoded.StderrEof, decoded.StderrOverflow,
            decoded.CaptureReadFailure, decoded.StdoutSha256, decoded.StderrSha256, decoded.ObservedAtNanoseconds,decoded.PhysicalReleaseAttempts);
        reason = decoded.Reason;
        return true;
    }

    internal static OwnerReleaseRequestWire RequestFromIdentity(
        Guid executionId,
        Guid preparationId,
        Guid generationId,
        ReadOnlySpan<byte> manifestDigest,
        ReadOnlySpan<byte> correlation,
        bool correlationPresent)
    {
        return new OwnerReleaseRequestWire(executionId, preparationId, generationId,
            manifestDigest.ToArray(), correlationPresent ? correlation.ToArray() : null);
    }

    private static OwnerReleaseDecisionWire ToWire(ReleaseDecision decision) => new(
        decision.Kind.ToString(),
        decision.DurableRelease.ToString(),
        decision.Delivery.ToString(),
        decision.DurableCommitted,
        decision.LiveExecutionExcluded ? "Excluded" : "NotExcluded",
        decision.Binding.ToString(),
        decision.RefusalReason.ToString(),
        decision.Reason);

    private static OwnerStopDecisionWire ToWire(StopDecision decision) => new(
        decision.Accepted,
        decision.Duplicate,
        decision.ReleaseAlreadyCommitted,
        decision.DurableStop,
        decision.Cause.ToString(),
        decision.StopAcceptedAtNanoseconds,
        decision.StopDeadlineNanoseconds,
        decision.Reason);

    private static bool TryDecodeLiveExcluded(string value, out bool excluded)
    {
        excluded = value switch
        {
            "Excluded" => true,
            "NotExcluded" => false,
            _ => false,
        };
        return value is "Excluded" or "NotExcluded";
    }

    private static byte[] EncodeJson<T>(T value, int maximum)
    {
        var encoded = JsonSerializer.SerializeToUtf8Bytes(value, Options);
        if (encoded.Length > maximum)
        {
            throw new ArgumentException("B3 held-phase payload exceeds its bound.");
        }

        return encoded;
    }

    private static bool TryDecodeJson<T>(ReadOnlySpan<byte> payload, int maximum, out T? value, out string error)
    {
        value = default;
        error = string.Empty;
        if (payload.Length == 0 || payload.Length > maximum)
        {
            error = "B3 held-phase payload length is outside the bound";
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(payload, Options);
            return value is not null;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException)
        {
            error = $"invalid B3 held-phase payload: {ex.GetType().Name}";
            return false;
        }
    }
}

/// <summary>Owner-observed post-release execution facts derived only from the
/// trusted coordinator status sequence, the retained wrapper reaper, the
/// bounded capture drains and the retained domain handles. Missing axes stay
/// unknown; fixture output is a separate fact and never classifies exec.</summary>
internal sealed record OwnerExecutionFacts(
    RestrictedOutcome Outcome,
    ExecTag Exec,
    int ExecErrno,
    RootKind Root,
    int ExitCode,
    int Signal,
    bool CoreDumped,
    bool RawWaitPresent,
    int RawWaitStatus,
    bool WrapperExited,
    int WrapperExitCode,
    bool DomainKnown,
    bool DomainPopulated,
    ulong StdoutStoredBytes,
    ulong StdoutObservedBytes,
    bool StdoutEof,
    bool StdoutOverflow,
    ulong StderrStoredBytes,
    ulong StderrObservedBytes,
    bool StderrEof,
    bool StderrOverflow,
    bool CaptureReadFailure,
    byte[]? StdoutSha256 = null,
    byte[]? StderrSha256 = null,
    long ObservedAtNanoseconds = 0,
    int? PhysicalReleaseAttempts = null);
