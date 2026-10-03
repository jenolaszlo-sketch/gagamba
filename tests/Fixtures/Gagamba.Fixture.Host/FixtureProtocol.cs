// GP-1A host-side protocol types and strict validator.
// Mirrors docs/fixture-protocol.md: JSONL, protocolVersion 1, 16 KiB max,
// enumerated kinds, identity + sequence enforcement.
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Gagamba.Fixture.Host;

public static class FixtureProtocol
{
    public const int ProtocolVersion = 1;
    public const int MaxLineBytes = 16 * 1024;

    public static readonly HashSet<string> WorkerKinds = new(StringComparer.Ordinal)
    {
        "Ready", "Result", "Error",
    };

    public static readonly HashSet<string> HostKinds = new(StringComparer.Ordinal)
    {
        "Continue",
    };

    public sealed record ValidationResult(bool Ok, string? Reason, JsonObject? Message);

    /// <summary>
    /// Strictly validates one worker output line.
    /// </summary>
    public static ValidationResult ValidateWorkerLine(
        string line,
        string expectedRunId,
        string expectedWorkerId,
        int expectedSequence)
    {
        if (Encoding.UTF8.GetByteCount(line) > MaxLineBytes)
            return new(false, "oversize-record", null);

        JsonNode? node;
        try { node = JsonNode.Parse(line); }
        catch { return new(false, "invalid-json", null); }

        if (node is not JsonObject obj)
            return new(false, "invalid-envelope", null);
        if (obj["protocolVersion"]?.GetValue<int>() != ProtocolVersion)
            return new(false, "unknown-version", null);
        if (!string.Equals(obj["runId"]?.GetValue<string>(), expectedRunId, StringComparison.Ordinal))
            return new(false, "mismatched-identity", null);
        if (!string.Equals(obj["workerId"]?.GetValue<string>(), expectedWorkerId, StringComparison.Ordinal))
            return new(false, "mismatched-identity", null);
        if (obj["sequence"]?.GetValue<int>() != expectedSequence)
        {
            // Distinguish duplicate vs out-of-order when caller provides context is
            // done by tests via reason prefix; validator reports bad-sequence.
            return new(false, "bad-sequence", null);
        }

        string? kind = obj["kind"]?.GetValue<string>();
        if (string.IsNullOrEmpty(kind))
            return new(false, "missing-kind", null);
        if (!WorkerKinds.Contains(kind))
            return new(false, $"unknown-kind:{kind}", null);
        if (obj["payload"] is not JsonObject)
            return new(false, "missing-payload", null);

        return new(true, null, obj);
    }

    public static string BuildContinue(string runId, string workerId, int sequence, string op, JsonObject? opParams)
    {
        var payload = new JsonObject { ["op"] = op };
        if (opParams is not null)
            payload["params"] = opParams;
        var msg = new JsonObject
        {
            ["protocolVersion"] = ProtocolVersion,
            ["runId"] = runId,
            ["workerId"] = workerId,
            ["sequence"] = sequence,
            ["kind"] = "Continue",
            ["payload"] = payload,
        };
        string line = msg.ToJsonString();
        if (Encoding.UTF8.GetByteCount(line) > MaxLineBytes)
            throw new InvalidOperationException("Continue record exceeds 16 KiB (caller must bound params).");
        return line;
    }

    public static string BuildRaw(string runId, string workerId, int sequence, string kind, JsonObject payload)
    {
        var msg = new JsonObject
        {
            ["protocolVersion"] = ProtocolVersion,
            ["runId"] = runId,
            ["workerId"] = workerId,
            ["sequence"] = sequence,
            ["kind"] = kind,
            ["payload"] = payload,
        };
        return msg.ToJsonString();
    }
}
