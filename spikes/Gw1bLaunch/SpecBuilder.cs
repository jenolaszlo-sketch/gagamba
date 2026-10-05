// Minimal SBOX spec encoder + independent verifier, built against
// spikes/Gw1bLaunch/pinned/BaseContainerSpecification.fbs (mxc v0.8.0).
// Supports exactly the fields the staircase needs: version (slot 0),
// app_container (slot 1), fs_read_write (slot 7), fs_read_only (slot 8).
// Every other slot is never written. The verifier re-parses the buffer from
// scratch and the launch gate requires round-trip equality before any call.
using System.Text;

namespace Gagamba.Spikes.Gw1bLaunch;

public sealed record SandboxSpecRequest(
    string Version,
    bool AppContainer,
    IReadOnlyList<string> FsReadWrite,
    IReadOnlyList<string> FsReadOnly,
    string Capabilities = "");

public static class SpecBuilder
{
    public const string RequiredVersion = "0.1.0";
    public const string FileIdentifier = "SBOX";
    public const int MaxBufferBytes = 64 * 1024;
    public const int MaxPathChars = 32_000;

    private const int SlotVersion = 0;
    private const int SlotAppContainer = 1;
    private const int SlotCapabilities = 6;
    private const int SlotFsReadWrite = 7;
    private const int SlotFsReadOnly = 8;

    public static byte[] Build(SandboxSpecRequest request)
    {
        if (request.Version != RequiredVersion)
            throw new ArgumentException($"engine supports version {RequiredVersion}", nameof(request));
        if (request.Capabilities.Length > 4096
            || request.Capabilities.Any(c => !(char.IsLetter(c) || c == ',')))
            throw new ArgumentException("capabilities must be comma-delimited names", nameof(request));
        foreach (string p in request.FsReadWrite.Concat(request.FsReadOnly))
        {
            if (string.IsNullOrWhiteSpace(p) || p.Length > MaxPathChars)
                throw new ArgumentException("grant path missing or exceeds bound");
            if (!Path.IsPathRooted(p))
                throw new ArgumentException($"grant path must be fully qualified: {p}");
        }
        // Fail closed before dispatch: the engine rejects filesystem grants and
        // capabilities unless app_container is enabled (E_INVALIDARG). Catch it
        // locally so unsupported shapes never reach the API.
        if (!request.AppContainer
            && (request.FsReadWrite.Count > 0 || request.FsReadOnly.Count > 0
                || request.Capabilities.Length > 0))
            throw new ArgumentException("filesystem grants and capabilities require app_container=true");

        byte[] versionBytes = Encoding.UTF8.GetBytes(request.Version);
        byte[] capsBytes = Encoding.UTF8.GetBytes(request.Capabilities);
        List<byte[]> rw = request.FsReadWrite.Select(Encoding.UTF8.GetBytes).ToList();
        List<byte[]> ro = request.FsReadOnly.Select(Encoding.UTF8.GetBytes).ToList();

        // Table fields: version always; app_container only when true (flatc
        // omits default scalars); grants/caps only when non-empty.
        var fields = new List<(int Slot, int Size, int Align)> { (SlotVersion, 4, 4) };
        if (request.AppContainer) fields.Add((SlotAppContainer, 1, 1));
        if (capsBytes.Length > 0) fields.Add((SlotCapabilities, 4, 4));
        if (rw.Count > 0) fields.Add((SlotFsReadWrite, 4, 4));
        if (ro.Count > 0) fields.Add((SlotFsReadOnly, 4, 4));

        int maxSlot = fields.Max(f => f.Slot);

        // Canonical layout (mirrors flatc byte-for-byte on all reference
        // shapes): vtable first, then table, then data. soffset stores
        // (table - vtable). The [vtable,table] block is 4-aligned as a unit,
        // so a 0-3 byte gap precedes the vtable when (vsize + tlen) % 4 != 0.
        // Table fields pack from the table end in descending (size, slot).
        int tableLen = AlignUp(4 + fields.Sum(f => f.Size), 4);
        int vtableSize = 4 + 2 * (maxSlot + 1);
        int vtablePos = 8 + ((4 - ((vtableSize + tableLen) % 4)) % 4);
        int tableAt = vtablePos + vtableSize;
        int cursor = tableLen;
        var offsets = new Dictionary<int, int>();
        foreach (var (slot, size, align) in fields
                     .OrderByDescending(f => f.Size).ThenByDescending(f => f.Slot))
        {
            cursor -= size;
            cursor -= cursor % align;
            offsets[slot] = cursor;
        }
        int dataPos = tableAt + tableLen;

        // Data order mirrors flatc: per-slot units in descending slot order,
        // each vector immediately followed by its strings, so every uoffset
        // points forward. Within a vector, strings emit in REVERSE list order
        // (flatc back-to-front building); element uoffsets still index
        // positions in list order.
        // Empirical flatc quirk, replicated exactly and byte-tested: when a
        // capabilities string is present, 4 zero bytes separate it from the
        // version string. Mechanism unknown; deviation is engine-rejected, so
        // the canonical form is reproduced verbatim (see evidence doc).
        int rwVecPos = -1, roVecPos = -1;
        var vecStrPos = new Dictionary<int, List<int>>();
        var singlePos = new Dictionary<int, int>();
        int dpos = dataPos;
        bool hasCaps = capsBytes.Length > 0;
        foreach (int slot in fields.Select(f => f.Slot).OrderByDescending(s => s))
        {
            if (slot == SlotAppContainer)
                continue; // inline scalar, no data object
            if (slot == SlotFsReadWrite || slot == SlotFsReadOnly)
            {
                var list = slot == SlotFsReadWrite ? rw : ro;
                if (list.Count == 0) continue;
                if (slot == SlotFsReadWrite) rwVecPos = dpos; else roVecPos = dpos;
                dpos += 4 + 4 * list.Count;
                var positions = new List<int>();
                foreach (byte[] b in ((IEnumerable<byte[]>)list).Reverse())
                {
                    positions.Add(dpos);
                    dpos += StringSize(b.Length);
                }
                positions.Reverse(); // back to list order for element fields
                vecStrPos[slot] = positions;
            }
            else
            {
                byte[] b = slot == SlotVersion ? versionBytes : capsBytes;
                singlePos[slot] = dpos;
                dpos += StringSize(b.Length);
                if (slot == SlotCapabilities && hasCaps)
                    dpos += 4; // canonical gap before the version string
            }
        }
        int versionPos = singlePos[SlotVersion];
        int capsPos = singlePos.TryGetValue(SlotCapabilities, out int cp) ? cp : -1;
        var rwPos = vecStrPos.TryGetValue(SlotFsReadWrite, out var rwp) ? rwp : new List<int>();
        var roPos = vecStrPos.TryGetValue(SlotFsReadOnly, out var rop) ? rop : new List<int>();

        // Materialize data in the same per-slot order positions were computed.
        // Vector strings write in reverse list order (positions were assigned
        // that way); element fields still index positions in list order.
        var data = new List<byte>();
        foreach (int slot in fields.Select(f => f.Slot).OrderByDescending(s => s))
        {
            if (slot == SlotAppContainer)
                continue;
            if (slot == SlotFsReadWrite || slot == SlotFsReadOnly)
            {
                var list = slot == SlotFsReadWrite ? rw : ro;
                if (list.Count == 0) continue;
                int vecPos = slot == SlotFsReadWrite ? rwVecPos : roVecPos;
                var positions = vecStrPos[slot];
                data.AddRange(BitConverter.GetBytes(list.Count));
                for (int i = 0; i < list.Count; i++)
                    data.AddRange(BitConverter.GetBytes(positions[i] - (vecPos + 4 + i * 4)));
                foreach (byte[] b in ((IEnumerable<byte[]>)list).Reverse()) data.AddRange(StringObject(b));
            }
            else
            {
                byte[] b = slot == SlotVersion ? versionBytes : capsBytes;
                if (b.Length == 0 && slot == SlotCapabilities) continue;
                data.AddRange(StringObject(b));
                if (slot == SlotCapabilities && hasCaps)
                    data.AddRange(new byte[4]); // canonical gap (see above)
            }
        }

        int total = dataPos + data.Count;
        if (total > MaxBufferBytes)
            throw new InvalidOperationException("spec exceeds 64 KiB bound");
        var buf = new byte[total];

        // Header: root uoffset + file identifier.
        BitConverter.GetBytes(tableAt).CopyTo(buf, 0);
        Encoding.ASCII.GetBytes(FileIdentifier).CopyTo(buf, 4);

        // Table.
        BitConverter.GetBytes(tableAt - vtablePos).CopyTo(buf, tableAt); // soffset
        BitConverter.GetBytes(versionPos - (tableAt + offsets[SlotVersion])).CopyTo(buf, tableAt + offsets[SlotVersion]);
        if (offsets.TryGetValue(SlotAppContainer, out int appOff))
            buf[tableAt + appOff] = request.AppContainer ? (byte)1 : (byte)0;
        if (capsBytes.Length > 0)
            BitConverter.GetBytes(capsPos - (tableAt + offsets[SlotCapabilities])).CopyTo(buf, tableAt + offsets[SlotCapabilities]);
        if (rw.Count > 0)
            BitConverter.GetBytes(rwVecPos - (tableAt + offsets[SlotFsReadWrite])).CopyTo(buf, tableAt + offsets[SlotFsReadWrite]);
        if (ro.Count > 0)
            BitConverter.GetBytes(roVecPos - (tableAt + offsets[SlotFsReadOnly])).CopyTo(buf, tableAt + offsets[SlotFsReadOnly]);

        // VTable.
        BitConverter.GetBytes((ushort)(4 + 2 * (maxSlot + 1))).CopyTo(buf, vtablePos);
        BitConverter.GetBytes((ushort)tableLen).CopyTo(buf, vtablePos + 2);
        for (int s = 0; s <= maxSlot; s++)
        {
            ushort entry = (ushort)(offsets.TryGetValue(s, out int o) ? o : 0);
            BitConverter.GetBytes(entry).CopyTo(buf, vtablePos + 4 + 2 * s);
        }

        // Data.
        data.ToArray().CopyTo(buf, dataPos);
        return buf;
    }

    /// <summary>
    /// Builds, then independently re-parses and requires round-trip equality.
    /// Throws when the buffer does not decode to exactly the request.
    /// Normalizes grants first (preparation-time semantics, GW-1B-L8).
    /// </summary>
    public static byte[] BuildVerified(SandboxSpecRequest request)
    {
        SandboxSpecRequest norm = Normalize(request);
        byte[] buf = Build(norm);
        SandboxSpecRequest decoded = Verify(buf);
        if (decoded.Version != norm.Version
            || decoded.AppContainer != norm.AppContainer
            || decoded.Capabilities != norm.Capabilities
            || !decoded.FsReadWrite.SequenceEqual(norm.FsReadWrite)
            || !decoded.FsReadOnly.SequenceEqual(norm.FsReadOnly))
            throw new InvalidOperationException("spec round-trip mismatch: refusing to call");
        return buf;
    }

    /// <summary>
    /// Preparation-time grant semantics (GW-1B-L8): resolve/canonicalize
    /// every grant (full path, no trailing separator, drive roots kept
    /// intact), collapse exact duplicates within a kind (case-insensitive
    /// on Windows), and reject the same path in both kinds (a path cannot
    /// be read-write and read-only at once). Nested overlap is NOT rejected
    /// here: narrowing (ro inside rw) is legitimate least-privilege shaping
    /// and the engine decides nested shapes (see L1-GRANT-SHAPE G6); widening
    /// should be expressed as a single wider grant. Pure function of the
    /// request: already-canonical distinct grants pass through unchanged.
    /// </summary>
    public static SandboxSpecRequest Normalize(SandboxSpecRequest request)
    {
        List<string> rw = NormalizeGrantPaths(request.FsReadWrite);
        List<string> ro = NormalizeGrantPaths(request.FsReadOnly);
        var both = new HashSet<string>(rw, StringComparer.OrdinalIgnoreCase);
        both.IntersectWith(ro);
        if (both.Count > 0)
            throw new ArgumentException($"grant path in both rw and ro: {string.Join(",", both.Take(3))}");
        return request with { FsReadWrite = rw, FsReadOnly = ro };
    }

    /// <summary>
    /// Canonical form for one grant kind: fully-qualified, separator-
    /// normalized, no trailing separator (except roots), order-preserving,
    /// first-wins dedupe (case-insensitive). Throws on missing, relative,
    /// overlong, or unresolvable paths.
    /// </summary>
    public static List<string> NormalizeGrantPaths(IEnumerable<string> paths)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var clean = new List<string>();
        foreach (string p in paths)
        {
            if (string.IsNullOrWhiteSpace(p) || p.Length > MaxPathChars)
                throw new ArgumentException("grant path missing or exceeds bound");
            if (!Path.IsPathRooted(p))
                throw new ArgumentException($"grant path must be fully qualified: {p}");
            string full;
            try
            {
                full = Path.GetFullPath(p);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"grant path not resolvable: {p}", ex);
            }
            string root = Path.GetPathRoot(full) ?? string.Empty;
            string trimmed = full.Length > root.Length
                ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : full;
            if (seen.Add(trimmed))
                clean.Add(trimmed);
        }
        return clean;
    }

    /// <summary>Independent verifier: parses the buffer from scratch.</summary>
    public static SandboxSpecRequest Verify(byte[] buf)
    {
        if (buf.Length < 12)
            throw new InvalidOperationException("buffer too small");
        int tableAt = BitConverter.ToInt32(buf, 0);
        if (tableAt < 8 || tableAt >= buf.Length)
            throw new InvalidOperationException("root offset out of bounds");
        if (Encoding.ASCII.GetString(buf, 4, 4) != FileIdentifier)
            throw new InvalidOperationException("missing SBOX file identifier");

        int vtableRel = BitConverter.ToInt32(buf, tableAt);
        int vtableAt = tableAt - vtableRel;
        if (vtableAt < 0 || vtableAt + 4 > buf.Length)
            throw new InvalidOperationException("vtable out of bounds");
        int vlen = BitConverter.ToUInt16(buf, vtableAt);
        if (vtableAt + vlen > buf.Length || vlen < 4 || (vlen % 2) != 0)
            throw new InvalidOperationException("bad vtable length");
        int slots = (vlen - 4) / 2;

        int Field(int slot)
        {
            if (slot >= slots)
                return -1;
            int entry = BitConverter.ToUInt16(buf, vtableAt + 4 + 2 * slot);
            if (entry == 0)
                return -1;
            int abs = tableAt + entry;
            if (abs < 0 || abs + 4 > buf.Length)
                throw new InvalidOperationException($"slot {slot} out of bounds");
            return abs;
        }

        string ReadString(int uoffField)
        {
            int rel = BitConverter.ToInt32(buf, uoffField);
            int at = uoffField + rel;
            if (at < 0 || at + 4 > buf.Length)
                throw new InvalidOperationException("string ref out of bounds");
            int len = BitConverter.ToInt32(buf, at);
            if (len < 0 || at + 4 + len > buf.Length)
                throw new InvalidOperationException("string bounds exceed buffer");
            return Encoding.UTF8.GetString(buf, at + 4, len);
        }

        List<string> ReadStringVector(int uoffField)
        {
            int rel = BitConverter.ToInt32(buf, uoffField);
            int at = uoffField + rel;
            if (at < 0 || at + 4 > buf.Length)
                throw new InvalidOperationException("vector ref out of bounds");
            int len = BitConverter.ToInt32(buf, at);
            if (len < 0 || len > 4096 || at + 4 + len * 4 > buf.Length)
                throw new InvalidOperationException("vector bounds exceed buffer");
            var items = new List<string>();
            for (int i = 0; i < len; i++)
                items.Add(ReadString(at + 4 + i * 4));
            return items;
        }

        // Only slots this builder ever writes may be present.
        var allowed = new HashSet<int> { SlotVersion, SlotAppContainer, SlotCapabilities, SlotFsReadWrite, SlotFsReadOnly };
        for (int s = 0; s < slots; s++)
        {
            if (BitConverter.ToUInt16(buf, vtableAt + 4 + 2 * s) != 0 && !allowed.Contains(s))
                throw new InvalidOperationException($"unexpected present slot {s}");
        }

        int vField = Field(SlotVersion);
        if (vField < 0)
            throw new InvalidOperationException("required version absent");
        string version = ReadString(vField);
        if (version != RequiredVersion)
            throw new InvalidOperationException($"version {version} != {RequiredVersion}");

        int aField = Field(SlotAppContainer);
        bool app = aField >= 0 && buf[aField] != 0;

        int rwField = Field(SlotFsReadWrite);
        int roField = Field(SlotFsReadOnly);
        int capsField = Field(SlotCapabilities);
        return new(version, app,
            rwField >= 0 ? ReadStringVector(rwField) : [],
            roField >= 0 ? ReadStringVector(roField) : [],
            capsField >= 0 ? ReadString(capsField) : "");
    }

    private static int AlignUp(int value, int align) => (value + align - 1) / align * align;

    private static int StringSize(int contentLength) => 4 + contentLength + ((4 - contentLength % 4) % 4);

    private static byte[] StringObject(byte[] content)
    {
        var obj = new List<byte>(BitConverter.GetBytes(content.Length));
        obj.AddRange(content);
        while (obj.Count % 4 != 0)
            obj.Add(0);
        return obj.ToArray();
    }
}
