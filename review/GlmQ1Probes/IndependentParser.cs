// REVIEW-ONLY (GLM RC2A-02A Q1). Independent schema-1 structural parser,
// written from the committed RC2A-01 API contract text and the RC-D02/RC-D03
// grammar description. It shares no code with the production codec, the
// qualification harness or the ReferenceEncoder. It records byte offsets of
// named fields so probe expectations are derived independently of both the
// production decoder and the qualification wire template.
namespace GlmQ1Probes;

internal static class IndependentParser
{
    internal static readonly string[] BudgetNames =
    {
        "Memory", "Swap", "Tasks", "FileDescriptors", "TmpBytes", "ShmBytes",
        "StdoutBytes", "StderrBytes", "CaptureAggregateBytes", "SetupTimeoutNs",
        "OverallTimeoutNs", "StopAllowanceNs", "FinalDrainNs",
    };

    internal sealed record ParseResult(
        bool Ok,
        string? Failure,
        Dictionary<string, long> Offsets,
        Dictionary<string, object?> Fields);

    internal static ParseResult Parse(ReadOnlySpan<byte> span)
    {
        var b = span.ToArray();
        var offsets = new Dictionary<string, long>(StringComparer.Ordinal);
        var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
        var p = 0;

        long U16()
        {
            if (p + 2 > b.Length) throw new InvalidOperationException($"out of input at {p}");
            var v = b[p] | (b[p + 1] << 8);
            p += 2;
            return v;
        }

        long U32()
        {
            if (p + 4 > b.Length) throw new InvalidOperationException($"out of input at {p}");
            var v = b[p] | (b[p + 1] << 8) | (b[p + 2] << 16) | (b[p + 3] << 24);
            p += 4;
            return (uint)v;
        }

        ulong U64()
        {
            if (p + 8 > b.Length) throw new InvalidOperationException($"out of input at {p}");
            ulong v = 0;
            for (var i = 0; i < 8; i++) v |= (ulong)b[p + i] << (8 * i);
            p += 8;
            return v;
        }

        string Text(string name)
        {
            offsets[name] = p;
            var len = U32();
            if (p + len > b.Length) throw new InvalidOperationException($"string '{name}' length {len} exceeds input at {p}");
            var s = System.Text.Encoding.UTF8.GetString(b.AsSpan().Slice(p, (int)len).ToArray());
            p += (int)len;
            return s;
        }

        void Bool(string name)
        {
            offsets[name] = p;
            if (p + 1 > b.Length) throw new InvalidOperationException($"out of input at {p}");
            fields[name] = b[p] == 1;
            p += 1;
        }

        try
        {
            offsets["schema"] = p;
            var schema = U16();
            fields["schema"] = schema;
            fields["implementation"] = Text("implementation");

            var nsCount = U32();
            offsets["namespaces.count"] = p - 4;
            var ns = new List<string>();
            for (var i = 0; i < nsCount; i++) ns.Add(Text($"namespaces.item[{i}]"));
            fields["namespaces"] = ns.ToArray();

            var capCount = U32();
            offsets["capabilities.count"] = p - 4;
            var caps = new List<string>();
            for (var i = 0; i < capCount; i++) caps.Add(Text($"capabilities.item[{i}]"));
            fields["capabilities"] = caps.ToArray();

            Bool("authority.noNewPrivs");
            offsets["authority.filterSha"] = p;
            fields["authority.filterSha"] = b.AsSpan().Slice(p, 32).ToArray();
            p += 32;
            offsets["authority.filterLength"] = p;
            fields["authority.filterLength"] = U64();

            var roCount = U32();
            offsets["readOnly.count"] = p - 4;
            var ro = new List<string>();
            for (var i = 0; i < roCount; i++) ro.Add(Text($"readOnly.item[{i}]"));
            fields["readOnly"] = ro.ToArray();

            var storeCount = U32();
            offsets["stores.count"] = p - 4;
            var stores = new List<string>();
            for (var i = 0; i < storeCount; i++)
            {
                var path = Text($"stores.item[{i}].path");
                offsets[$"stores.item[{i}].bytes"] = p;
                var bytes = U64();
                Bool($"stores.item[{i}].hasInode");
                offsets[$"stores.item[{i}].inode"] = p;
                var inodes = U64();
                stores.Add($"{path} bytes={bytes} hasInode={fields[$"stores.item[{i}].hasInode"]} inodes={inodes}");
            }
            fields["stores"] = stores.ToArray();

            Bool("authority.noAmbientEnvironment");
            Bool("authority.noIpcEndpoints");
            Bool("authority.noExtraInheritedFds");
            Bool("mount.toolExecutable");
            Bool("mount.scratchNoexec");
            offsets["authority.networkQualification"] = p;
            fields["authority.networkQualification"] = b[p];
            p += 1;

            foreach (var name in BudgetNames)
            {
                offsets[$"budget.{name}.kind"] = p;
                var kind = b[p];
                p += 1;
                offsets[$"budget.{name}.value"] = p;
                var value = U64();
                var unit = Text($"budget.{name}.unit");
                offsets[$"budget.{name}.qualification"] = p;
                var qual = b[p];
                p += 1;
                fields[$"budget.{name}"] = $"kind={kind} value={value} unit={unit} qual={qual}";
            }

            offsets["resources.inodeQualification"] = p;
            fields["resources.inodeQualification"] = b[p];
            p += 1;
            Bool("core.disabled");
            Bool("core.runtimeDumpsDisabled");
            Bool("core.patternPresent");
            if (fields["core.patternPresent"] is true) fields["core.pattern"] = Text("core.pattern");
            else fields["core.pattern"] = null;

            offsets["owner.stagingBytes"] = p; fields["owner.stagingBytes"] = U64();
            offsets["owner.stagingObjects"] = p; fields["owner.stagingObjects"] = U32();
            offsets["owner.configBytes"] = p; fields["owner.configBytes"] = U64();
            offsets["owner.coordinatorStatusBytes"] = p; fields["owner.coordinatorStatusBytes"] = U64();
            offsets["owner.helperStatusBytes"] = p; fields["owner.helperStatusBytes"] = U64();
            offsets["owner.journalBytes"] = p; fields["owner.journalBytes"] = U64();
            offsets["owner.receiptBytes"] = p; fields["owner.receiptBytes"] = U64();

            var objCount = U32();
            offsets["objects.count"] = p - 4;
            var objects = new List<string>();
            for (var i = 0; i < objCount; i++)
            {
                var role = Text($"objects.item[{i}].role");
                offsets[$"objects.item[{i}].sha"] = p;
                var sha = b.AsSpan().Slice(p, 32).ToArray();
                p += 32;
                offsets[$"objects.item[{i}].length"] = p;
                var len = U64();
                var dest = Text($"objects.item[{i}].destination");
                var build = Text($"objects.item[{i}].buildIdentity");
                objects.Add($"{role} len={len} dest={dest} build={build} sha[0..2]={sha[0]:x2}{sha[1]:x2}");
            }
            fields["objects"] = objects.ToArray();

            var helperCount = U32();
            offsets["helpers.count"] = p - 4;
            var helpers = new List<string>();
            for (var i = 0; i < helperCount; i++) helpers.Add(Text($"helpers.item[{i}]"));
            fields["helpers"] = helpers.ToArray();

            fields["invocation.toolPath"] = Text("invocation.toolPath");
            fields["invocation.mode"] = Text("invocation.mode");
            fields["invocation.inputPath"] = Text("invocation.inputPath");

            var argvCount = U32();
            offsets["argv.count"] = p - 4;
            var argv = new List<string>();
            for (var i = 0; i < argvCount; i++) argv.Add(Text($"argv.item[{i}]"));
            fields["argv"] = argv.ToArray();

            var envCount = U32();
            offsets["env.count"] = p - 4;
            var env = new List<string>();
            for (var i = 0; i < envCount; i++)
            {
                var k = Text($"env.key[{i}]");
                var v = Text($"env.value[{i}]");
                env.Add($"{k}={v}");
            }
            fields["environment"] = env.ToArray();

            fields["invocation.workDirectory"] = Text("invocation.workDirectory");

            Bool("channels.stdinEof");
            offsets["channels.stdoutLimit"] = p; fields["channels.stdoutLimit"] = U64();
            offsets["channels.stderrLimit"] = p; fields["channels.stderrLimit"] = U64();
            offsets["channels.aggregateLimit"] = p; fields["channels.aggregateLimit"] = U64();

            if (p != b.Length) return new ParseResult(false, $"trailing data at {p}", offsets, fields);
            return new ParseResult(true, null, offsets, fields);
        }
        catch (Exception ex)
        {
            return new ParseResult(false, $"{ex.Message} (stopped at {p})", offsets, fields);
        }
    }
}

