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
    IReadOnlyList<string> FsReadOnly);

public static class SpecBuilder
{
    public const string RequiredVersion = "0.1.0";
    public const string FileIdentifier = "SBOX";
    public const int MaxBufferBytes = 64 * 1024;
    public const int MaxPathChars = 32_000;

    private const int SlotVersion = 0;
    private const int SlotAppContainer = 1;
    private const int SlotFsReadWrite = 7;
    private const int SlotFsReadOnly = 8;

    public static byte[] Build(SandboxSpecRequest request)
    {
        if (request.Version != RequiredVersion)
            throw new ArgumentException($"engine supports version {RequiredVersion}", nameof(request));
        foreach (string p in request.FsReadWrite.Concat(request.FsReadOnly))
        {
            if (string.IsNullOrWhiteSpace(p) || p.Length > MaxPathChars)
                throw new ArgumentException("grant path missing or exceeds bound");
            if (!Path.IsPathRooted(p))
                throw new ArgumentException($"grant path must be fully qualified: {p}");
        }

        byte[] versionBytes = Encoding.UTF8.GetBytes(request.Version);
        List<byte[]> rw = request.FsReadWrite.Select(Encoding.UTF8.GetBytes).ToList();
        List<byte[]> ro = request.FsReadOnly.Select(Encoding.UTF8.GetBytes).ToList();

        // Table fields, ascending slots: (slot, size, align).
        var fields = new List<(int Slot, int Size, int Align)> { (SlotVersion, 4, 4), (SlotAppContainer, 1, 1) };
        if (rw.Count > 0) fields.Add((SlotFsReadWrite, 4, 4));
        if (ro.Count > 0) fields.Add((SlotFsReadOnly, 4, 4));

        int maxSlot = fields.Max(f => f.Slot);

        // Canonical layout (mirrors flatc): vtable first, then table, then data.
        // soffset stores (table - vtable), i.e. vtableAt = tableAt - soffset.
        int vtablePos = 8;
        int vtableSize = 4 + 2 * (maxSlot + 1);
        int tableAt = AlignUp(vtablePos + vtableSize, 4);

        int cursor = 4; // past the table soffset
        var offsets = new Dictionary<int, int>();
        foreach (var (slot, size, align) in fields)
        {
            cursor = AlignUp(cursor, align);
            offsets[slot] = cursor;
            cursor += size;
        }
        int tableLen = AlignUp(cursor, 4);
        int dataPos = tableAt + tableLen;

        // Data layout: vectors first, then strings. Every uoffset therefore
        // points forward (target at a higher address than the referring field),
        // matching canonical flatc output. Backward references are legal
        // FlatBuffers but were empirically rejected (ERROR_INVALID_DATA) by the
        // engine, so this encoder never emits them.
        // Order: rw vector, ro vector, rw strings, ro strings, version string.
        int vecCursor = dataPos;
        int rwVecPos = -1, roVecPos = -1;
        if (rw.Count > 0) { rwVecPos = vecCursor; vecCursor += 4 + 4 * rw.Count; }
        if (ro.Count > 0) { roVecPos = vecCursor; vecCursor += 4 + 4 * ro.Count; }
        int strCursor = vecCursor;
        var rwPos = new List<int>();
        foreach (byte[] b in rw) { rwPos.Add(strCursor); strCursor += StringSize(b.Length); }
        var roPos = new List<int>();
        foreach (byte[] b in ro) { roPos.Add(strCursor); strCursor += StringSize(b.Length); }
        int versionPos = strCursor;

        // Materialize data with element uoffsets relative to each element field.
        var data = new List<byte>();
        if (rw.Count > 0)
        {
            data.AddRange(BitConverter.GetBytes(rw.Count));
            for (int i = 0; i < rw.Count; i++)
                data.AddRange(BitConverter.GetBytes(rwPos[i] - (rwVecPos + 4 + i * 4)));
        }
        if (ro.Count > 0)
        {
            data.AddRange(BitConverter.GetBytes(ro.Count));
            for (int i = 0; i < ro.Count; i++)
                data.AddRange(BitConverter.GetBytes(roPos[i] - (roVecPos + 4 + i * 4)));
        }
        foreach (byte[] b in rw) data.AddRange(StringObject(b));
        foreach (byte[] b in ro) data.AddRange(StringObject(b));
        data.AddRange(StringObject(versionBytes));

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
        buf[tableAt + offsets[SlotAppContainer]] = request.AppContainer ? (byte)1 : (byte)0;
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
    /// </summary>
    public static byte[] BuildVerified(SandboxSpecRequest request)
    {
        byte[] buf = Build(request);
        SandboxSpecRequest decoded = Verify(buf);
        if (decoded.Version != request.Version
            || decoded.AppContainer != request.AppContainer
            || !decoded.FsReadWrite.SequenceEqual(request.FsReadWrite)
            || !decoded.FsReadOnly.SequenceEqual(request.FsReadOnly))
            throw new InvalidOperationException("spec round-trip mismatch: refusing to call");
        return buf;
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
        var allowed = new HashSet<int> { SlotVersion, SlotAppContainer, SlotFsReadWrite, SlotFsReadOnly };
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
        return new(version, app,
            rwField >= 0 ? ReadStringVector(rwField) : [],
            roField >= 0 ? ReadStringVector(roField) : []);
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
