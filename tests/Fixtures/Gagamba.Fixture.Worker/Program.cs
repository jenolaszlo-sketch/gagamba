// GP-1A fixture worker. Private test tooling only, not a public Gagamba API.
// Enumerated operations only. No arbitrary shell-command field.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class Program
{
    private const int ProtocolVersion = 1;
    private const int MaxProtocolLineBytes = 16 * 1024;
    private const int MaxWriteBytes = 8 * 1024;

    private static readonly HashSet<string> AllowedOps = new(StringComparer.Ordinal)
    {
        "read-sentinel", "write-output", "exit",
    };

    internal static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("usage: Worker <protocol|raw-stdout|raw-stderr|echo-stdin|hang|exit-code> ...");
            return 2;
        }

        try
        {
            return args[0] switch
            {
                "protocol" => RunProtocol(args[1..]),
                "raw-stdout" => RunRawStdout(args[1..]),
                "raw-stderr" => RunRawStderr(args[1..]),
                "echo-stdin" => RunEchoStdin(args[1..]),
                "hang" => RunHang(),
                "exit-code" => RunExitCode(args[1..]),
                _ => Fail($"unknown mode '{args[0]}'"),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"worker-fatal: {ex.GetType().Name}: {ex.Message}");
            return 2;
        }
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"worker-usage: {message}");
        return 2;
    }

    // ---------- protocol mode ----------

    private static int RunProtocol(string[] args)
    {
        string? runId = null;
        string? workerId = null;
        string? workspace = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--run-id" when i + 1 < args.Length: runId = args[++i]; break;
                case "--worker-id" when i + 1 < args.Length: workerId = args[++i]; break;
                case "--workspace" when i + 1 < args.Length: workspace = args[++i]; break;
                default: return Fail($"unknown protocol arg '{args[i]}'");
            }
        }

        if (string.IsNullOrEmpty(runId) || string.IsNullOrEmpty(workerId) || string.IsNullOrEmpty(workspace))
            return Fail("protocol requires --run-id, --worker-id, --workspace");
        if (!Directory.Exists(workspace))
            return Fail("workspace must exist");

        string workspaceFull = Path.GetFullPath(workspace);

        // Send Ready with sequence 0.
        int nextOutSequence = 0;
        WriteLine(new JsonObject
        {
            ["protocolVersion"] = ProtocolVersion,
            ["runId"] = runId,
            ["workerId"] = workerId,
            ["sequence"] = nextOutSequence++,
            ["kind"] = "Ready",
            ["payload"] = new JsonObject { ["workspace"] = workspaceFull },
        });

        int expectedInSequence = 1;
        string? line;
        while ((line = Console.ReadLine()) is not null)
        {
            byte[] lineBytes = Encoding.UTF8.GetByteCount(line) > 0 ? Encoding.UTF8.GetBytes(line) : [];
            if (lineBytes.Length > MaxProtocolLineBytes)
            {
                WriteError(runId, workerId, ref nextOutSequence, "oversize-record");
                return 2;
            }

            JsonNode? node;
            try { node = JsonNode.Parse(line); }
            catch { WriteError(runId, workerId, ref nextOutSequence, "invalid-json"); return 2; }

            string? error = ValidateHostMessage(node, runId, workerId, expectedInSequence);
            if (error is not null)
            {
                WriteError(runId, workerId, ref nextOutSequence, error);
                return 2;
            }

            expectedInSequence++;
            var obj = node!.AsObject();
            string kind = obj["kind"]!.GetValue<string>();
            if (!string.Equals(kind, "Continue", StringComparison.Ordinal))
            {
                WriteError(runId, workerId, ref nextOutSequence, $"unsupported-kind:{kind}");
                return 2;
            }

            var payload = obj["payload"]?.AsObject();
            string? op = payload?["op"]?.GetValue<string>();
            if (op is null || !AllowedOps.Contains(op))
            {
                WriteError(runId, workerId, ref nextOutSequence, $"unsupported-op:{op ?? "<missing>"}");
                return 2;
            }

            var opParams = payload!["params"]?.AsObject();
            switch (op)
            {
                case "read-sentinel":
                {
                    string? rel = opParams?["relativePath"]?.GetValue<string>();
                    if (!TryResolveWithin(workspaceFull, rel, out string? full, out string? pathError))
                    {
                        WriteError(runId, workerId, ref nextOutSequence, pathError!);
                        return 2;
                    }
                    byte[] bytes;
                    try { bytes = File.ReadAllBytes(full!); }
                    catch (Exception ex)
                    {
                        WriteResult(runId, workerId, ref nextOutSequence,
                            new JsonObject { ["ok"] = false, ["error"] = $"read-failed:{ex.GetType().Name}" });
                        continue;
                    }
                    string sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
                    WriteResult(runId, workerId, ref nextOutSequence,
                        new JsonObject { ["ok"] = true, ["op"] = op, ["sha256"] = sha, ["length"] = bytes.Length });
                    break;
                }
                case "write-output":
                {
                    string? rel = opParams?["relativePath"]?.GetValue<string>();
                    string? b64 = opParams?["contentBase64"]?.GetValue<string>();
                    int maxBytes = opParams?["maxBytes"]?.GetValue<int>() ?? -1;
                    if (!TryResolveWithin(workspaceFull, rel, out string? full, out string? pathError))
                    {
                        WriteError(runId, workerId, ref nextOutSequence, pathError!);
                        return 2;
                    }
                    if (string.IsNullOrEmpty(b64) || maxBytes <= 0 || maxBytes > MaxWriteBytes)
                    {
                        WriteError(runId, workerId, ref nextOutSequence, "invalid-write-params");
                        return 2;
                    }
                    byte[] content;
                    try { content = Convert.FromBase64String(b64); }
                    catch { WriteError(runId, workerId, ref nextOutSequence, "invalid-base64"); return 2; }
                    if (content.Length > maxBytes)
                    {
                        WriteError(runId, workerId, ref nextOutSequence, "content-exceeds-maxBytes");
                        return 2;
                    }
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(full!)!);
                        File.WriteAllBytes(full!, content);
                    }
                    catch (Exception ex)
                    {
                        WriteResult(runId, workerId, ref nextOutSequence,
                            new JsonObject { ["ok"] = false, ["error"] = $"write-failed:{ex.GetType().Name}" });
                        continue;
                    }
                    string sha = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
                    WriteResult(runId, workerId, ref nextOutSequence,
                        new JsonObject { ["ok"] = true, ["op"] = op, ["sha256"] = sha, ["length"] = content.Length });
                    break;
                }
                case "exit":
                {
                    int code = opParams?["code"]?.GetValue<int>() ?? 0;
                    if (code is < 0 or > 255)
                    {
                        WriteError(runId, workerId, ref nextOutSequence, "invalid-exit-code");
                        return 2;
                    }
                    WriteResult(runId, workerId, ref nextOutSequence,
                        new JsonObject { ["ok"] = true, ["op"] = op, ["code"] = code, ["exiting"] = true });
                    return code;
                }
            }
        }

        // stdin EOF without exit op: orderly shutdown, exit 0 only if host closed after results.
        return 0;
    }

    private static string? ValidateHostMessage(JsonNode? node, string runId, string workerId, int expectedSequence)
    {
        if (node is not JsonObject obj) return "invalid-envelope";
        if (obj["protocolVersion"]?.GetValue<int>() != ProtocolVersion) return "unknown-version";
        if (!string.Equals(obj["runId"]?.GetValue<string>(), runId, StringComparison.Ordinal)) return "mismatched-runId";
        if (!string.Equals(obj["workerId"]?.GetValue<string>(), workerId, StringComparison.Ordinal)) return "mismatched-workerId";
        if (obj["sequence"]?.GetValue<int>() != expectedSequence) return "bad-sequence";
        string? kind = obj["kind"]?.GetValue<string>();
        if (kind is null) return "missing-kind";
        if (!string.Equals(kind, "Continue", StringComparison.Ordinal)) return $"unsupported-kind:{kind}";
        return null;
    }

    private static void WriteLine(JsonObject obj)
    {
        string line = obj.ToJsonString();
        byte[] bytes = Encoding.UTF8.GetBytes(line);
        if (bytes.Length > MaxProtocolLineBytes)
            throw new InvalidOperationException("outgoing record exceeds 16 KiB");
        Console.WriteLine(line);
        Console.Out.Flush();
    }

    private static void WriteResult(string runId, string workerId, ref int seq, JsonObject resultPayload)
    {
        WriteLine(new JsonObject
        {
            ["protocolVersion"] = ProtocolVersion,
            ["runId"] = runId,
            ["workerId"] = workerId,
            ["sequence"] = seq++,
            ["kind"] = "Result",
            ["payload"] = resultPayload,
        });
    }

    private static void WriteError(string runId, string workerId, ref int seq, string reason)
    {
        try
        {
            WriteLine(new JsonObject
            {
                ["protocolVersion"] = ProtocolVersion,
                ["runId"] = runId,
                ["workerId"] = workerId,
                ["sequence"] = seq++,
                ["kind"] = "Error",
                ["payload"] = new JsonObject { ["reason"] = reason },
            });
        }
        catch { /* best effort */ }
        Console.Error.WriteLine($"worker-protocol-error: {reason}");
    }

    internal static bool TryResolveWithin(string workspaceFull, string? relativePath, out string? full, out string? error)
    {
        full = null;
        error = null;
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            error = "path-missing";
            return false;
        }
        // Reject absolute, UNC, device, ADS, traversal. Keep narrow for v1.
        if (Path.IsPathRooted(relativePath)
            || relativePath.Contains(':')
            || relativePath.StartsWith(@"\\", StringComparison.Ordinal)
            || relativePath.StartsWith("//", StringComparison.Ordinal))
        {
            error = "path-outside-workspace";
            return false;
        }
        string combined = Path.GetFullPath(Path.Combine(workspaceFull, relativePath));
        string prefix = workspaceFull.EndsWith(Path.DirectorySeparatorChar)
            ? workspaceFull
            : workspaceFull + Path.DirectorySeparatorChar;
        if (!combined.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !string.Equals(combined, workspaceFull, StringComparison.OrdinalIgnoreCase))
        {
            error = "path-outside-workspace";
            return false;
        }
        // Reject any remaining ".." that resolves outside (covered) plus literal ".." segments for clarity.
        if (relativePath.Split(['/', '\\']).Contains(".."))
        {
            error = "path-outside-workspace";
            return false;
        }
        full = combined;
        return true;
    }

    // ---------- raw modes (separate invocations, no protocol framing) ----------

    private static int RunRawStdout(string[] args)
    {
        long bytes = GetLongArg(args, "--bytes", 0);
        if (bytes is < 0 or > 2_097_152) return Fail("--bytes must be 0..2097152");
        Stream stdout = Console.OpenStandardOutput();
        for (long i = 0; i < bytes; i++)
            stdout.WriteByte((byte)('A' + (i % 26)));
        stdout.Flush();
        return 0;
    }

    private static int RunRawStderr(string[] args)
    {
        long bytes = GetLongArg(args, "--bytes", 0);
        if (bytes is < 0 or > 2_097_152) return Fail("--bytes must be 0..2097152");
        Stream stderr = Console.OpenStandardError();
        for (long i = 0; i < bytes; i++)
            stderr.WriteByte((byte)('a' + (i % 26)));
        stderr.Flush();
        return 0;
    }

    private static int RunEchoStdin(string[] args)
    {
        long maxBytes = GetLongArg(args, "--max-bytes", 65536);
        if (maxBytes is <= 0 or > 65536) return Fail("--max-bytes must be 1..65536");
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        byte[] buffer = new byte[8192];
        long total = 0;
        int read;
        while ((read = stdin.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                Console.Error.WriteLine("worker: stdin-exceeds-maxBytes");
                return 3;
            }
            stdout.Write(buffer, 0, read);
        }
        stdout.Flush();
        return 0;
    }

    private static int RunHang()
    {
        // Sleep long enough for the host watchdog to kill us. No output after startup.
        Console.WriteLine("hanging");
        Console.Out.Flush();
        Thread.Sleep(TimeSpan.FromMinutes(5));
        return 0;
    }

    private static int RunExitCode(string[] args)
    {
        int code = (int)GetLongArg(args, "--code", -1);
        string? workspace = GetStringArg(args, "--workspace");
        string? marker = GetStringArg(args, "--marker");
        string? content = GetStringArg(args, "--content") ?? "target-entry";
        if (code is < 0 or > 255) return Fail("--code must be 0..255");
        if (marker is not null)
        {
            if (workspace is null) return Fail("--workspace required with --marker");
            string workspaceFull = Path.GetFullPath(workspace);
            if (!TryResolveWithin(workspaceFull, marker, out string? full, out _))
                return Fail("marker outside workspace");
            Directory.CreateDirectory(Path.GetDirectoryName(full!)!);
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            if (bytes.Length > MaxWriteBytes) return Fail("marker content too large");
            File.WriteAllBytes(full!, bytes);
        }
        return code;
    }

    private static long GetLongArg(string[] args, string name, long def)
    {
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == name && long.TryParse(args[i + 1], out long v))
                return v;
        return def;
    }

    private static string? GetStringArg(string[] args, string name)
    {
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == name)
                return args[i + 1];
        return null;
    }
}
