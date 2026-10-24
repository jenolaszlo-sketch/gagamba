using System.Buffers.Binary;
using System.Text;
using Gagamba.Execution.Restricted.Contracts;

namespace Gagamba.Execution.Restricted;

/// <summary>Fixed-depth, bounded schema-1 reader. Wire sets are checked before model constructors sort.</summary>
internal ref struct ManifestReader
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private readonly ReadOnlySpan<byte> bytes;
    private int position;

    private ManifestReader(ReadOnlySpan<byte> bytes) { this.bytes = bytes; position = 0; }

    internal static ManifestDecodeResult Decode(ReadOnlySpan<byte> bytes)
    {
        var reader = new ManifestReader(bytes);
        try
        {
            var manifest = reader.ReadManifest();
            if (reader.position != bytes.Length) reader.Fail(ManifestDecodeError.TrailingData, reader.position);
            var view = new RestrictedManifestView(manifest);
            if (!ManifestCodec.Encode(manifest, RestrictedManifestCodec.MaxCanonicalBytes).AsSpan().SequenceEqual(bytes))
                reader.Fail(ManifestDecodeError.InvalidValue, 0);
            return new(view);
        }
        catch (Refusal error) { return new(error.Code, error.Offset); }
        catch (ArgumentException) { return new(ManifestDecodeError.InvalidValue, reader.position); }
    }

    private RestrictedManifest ReadManifest()
    {
        var schema = U16();
        if (schema != RestrictedManifest.CurrentSchemaVersion) Fail(ManifestDecodeError.UnsupportedSchema, 0);
        var implementation = Text();
        if (!string.Equals(implementation, RestrictedManifest.ImplementationIdentifier, StringComparison.Ordinal))
            Fail(ManifestDecodeError.UnsupportedImplementation, 2);
        return new(schema, implementation, Authority(), Resources(), Closure(), Invocation(), Channels());
    }

    private AuthorityManifest Authority()
    {
        var namespaces = Strings(ordered: true);
        var capabilityOffset = position;
        var capabilities = Strings(ordered: true);
        if (capabilities.Length != 0) Fail(ManifestDecodeError.InvalidValue, capabilityOffset);
        var noNewPrivs = Bool();
        var filter = Take(32).ToArray();
        var length = U64();
        var readOnly = Strings(ordered: true);
        var stores = Stores();
        var noEnvironment = RequiredTrue();
        var noIpc = RequiredTrue();
        var noFds = RequiredTrue();
        var executable = RequiredTrue();
        var scratch = Bool();
        var qualification = Qualification(pendingOnly: true);
        return new(namespaces, capabilities, noNewPrivs, filter, length, readOnly, stores,
            noEnvironment, noIpc, noFds, new(executable, scratch), qualification);
    }

    private WritableStore[] Stores()
    {
        var result = new WritableStore[Count(22)];
        string? previous = null;
        for (var index = 0; index < result.Length; index++)
        {
            var start = position;
            var path = Text(); Ordered(previous, path, start); previous = path;
            var size = U64(); var hasInode = Bool();
            var inodeOffset = position; var inodes = U64();
            if (!hasInode && inodes != 0) Fail(ManifestDecodeError.InvalidValue, inodeOffset);
            result[index] = new(path, size, hasInode, inodes);
        }
        return result;
    }

    private ResourceBudget Budget()
    {
        var start = position;
        var kind = (BudgetValueKind)Tag(2);
        var value = U64();
        if (kind != BudgetValueKind.Exact && value != 0) Fail(ManifestDecodeError.InvalidValue, start);
        return new(kind, value, Text(), Qualification());
    }

    private ResourceManifest Resources()
    {
        var memory = Budget(); var swap = Budget(); var tasks = Budget(); var fds = Budget();
        var tmp = Budget(); var shm = Budget(); var stdout = Budget(); var stderr = Budget();
        var aggregate = Budget(); var setup = Budget(); var overall = Budget();
        var stop = Budget(); var drain = Budget();
        var inode = Qualification(pendingOnly: true);
        var coreDisabled = RequiredTrue(); var runtimeDisabled = Bool();
        var pattern = Bool() ? Text() : null;
        var core = new CoreDumpPolicy(coreDisabled, runtimeDisabled, pattern);
        var allocation = new OwnerAllocationLimits(U64(), U32(), U64(), U64(), U64(), U64(), U64());
        return new(memory, swap, tasks, fds, tmp, shm, stdout, stderr, aggregate,
            setup, overall, stop, drain, inode, core, allocation);
    }

    private ClosureIdentity Closure()
    {
        var objects = new ClosureObject[Count(55)];
        string? previous = null;
        for (var index = 0; index < objects.Length; index++)
        {
            var start = position;
            var role = Text(); Ordered(previous, role, start); previous = role;
            objects[index] = new(role, Take(32).ToArray(), U64(), Text(), Text());
        }
        return new(objects, Strings(ordered: true));
    }

    private InvocationManifest Invocation()
    {
        var tool = FixedText("/tool"); var mode = Text(); var input = FixedText("/input");
        var argv = Strings(ordered: false);
        var environment = new KeyValuePair<string, string>[Count(9)];
        string? previous = null;
        for (var index = 0; index < environment.Length; index++)
        {
            var start = position;
            var key = Text(); Ordered(previous, key, start); previous = key;
            environment[index] = new(key, Text(allowEmpty: true));
        }
        return new(tool, mode, input, argv, environment, FixedText("/tmp"));
    }

    private ChannelManifest Channels() => new(RequiredTrue(), U64(), U64(), U64());

    private string[] Strings(bool ordered)
    {
        var result = new string[Count(5)];
        string? previous = null;
        for (var index = 0; index < result.Length; index++)
        {
            var start = position;
            var text = Text();
            if (ordered) Ordered(previous, text, start);
            result[index] = text; previous = text;
        }
        return result;
    }

    private void Ordered(string? previous, string current, int start)
    {
        if (previous is null) return;
        var order = Utf8BytewiseComparer.Instance.Compare(previous, current);
        if (order == 0) Fail(ManifestDecodeError.Duplicate, start);
        if (order > 0) Fail(ManifestDecodeError.NonCanonicalOrder, start);
    }

    private string FixedText(string expected)
    {
        var start = position; var text = Text();
        if (!string.Equals(text, expected, StringComparison.Ordinal)) Fail(ManifestDecodeError.InvalidValue, start);
        return text;
    }

    private string Text(bool allowEmpty = false)
    {
        var start = position;
        var length = U32();
        if (length > (uint)(bytes.Length - position)) Fail(ManifestDecodeError.InvalidLengthOrCount, start);
        if (!allowEmpty && length == 0) Fail(ManifestDecodeError.InvalidValue, start);
        try { return StrictUtf8.GetString(Take((int)length)); }
        catch (DecoderFallbackException) { Fail(ManifestDecodeError.InvalidUtf8, start); return null!; }
    }

    private int Count(int minimumItemBytes)
    {
        var start = position;
        var count = U32();
        if (count > (uint)((bytes.Length - position) / minimumItemBytes))
            Fail(ManifestDecodeError.InvalidLengthOrCount, start);
        return (int)count;
    }

    private QualificationDisposition Qualification(bool pendingOnly = false)
    {
        var start = position; var value = (QualificationDisposition)Tag(1);
        if (pendingOnly && value != QualificationDisposition.PendingQualification)
            Fail(ManifestDecodeError.InvalidValue, start);
        return value;
    }

    private bool RequiredTrue()
    {
        var start = position; var value = Bool();
        if (!value) Fail(ManifestDecodeError.InvalidValue, start);
        return value;
    }

    private bool Bool() => Tag(1) == 1;
    private byte Tag(byte maximum)
    {
        var start = position; var value = Take(1)[0];
        if (value > maximum) Fail(ManifestDecodeError.InvalidTag, start);
        return value;
    }
    private ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Take(2));
    private uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Take(4));
    private ulong U64() => BinaryPrimitives.ReadUInt64LittleEndian(Take(8));

    private ReadOnlySpan<byte> Take(int count)
    {
        if (count > bytes.Length - position) Fail(ManifestDecodeError.Truncated, bytes.Length);
        var result = bytes.Slice(position, count);
        position = checked(position + count);
        return result;
    }

    private void Fail(ManifestDecodeError code, int offset) => throw new Refusal(code, offset);
    private sealed class Refusal(ManifestDecodeError code, int offset) : Exception
    {
        internal ManifestDecodeError Code { get; } = code;
        internal int Offset { get; } = offset;
    }
}
