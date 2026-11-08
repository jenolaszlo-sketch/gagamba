using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Installed;

/// <summary>Descriptor-relative verification of candidate and installed payloads.</summary>
internal static partial class InstalledFileSystem
{
    private const string InstallRoot = "/opt/gagamba";
    private const int O_PATH = 0x200000;
    private const int Getdents64X64 = 217;
    private const int MaxDepth = 8;
    private const int MaxFiles = 32;
    private const int MaxDirectories = 8;
    private const int MaxMetadataBytes = 256 * 1024;
    private const int MaxArtifactBytes = 32 * 1024 * 1024;
    private const long MaxTotalBytes = 128L * 1024 * 1024;
    private const uint S_IFMT = 0xF000, S_IFREG = 0x8000, S_IFDIR = 0x4000;
    private const uint S_ISUID = 0x800, S_ISGID = 0x400;

    [StructLayout(LayoutKind.Sequential)]
    private struct Stat64
    {
        public ulong Dev, Ino, Nlink;
        public uint Mode, Uid, Gid, Pad;
        public ulong Rdev;
        public long Size, BlockSize, Blocks;
        public long AtimeSec, AtimeNsec, MtimeSec, MtimeNsec, CtimeSec, CtimeNsec;
        public long Unused0, Unused1, Unused2;
    }

    [DllImport("libc", EntryPoint = "fstat", SetLastError = true)]
    private static extern int FStat(int fd, out Stat64 stat);

    [DllImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static extern long Syscall(long number, int fd, IntPtr buffer, nuint count);

    internal readonly record struct Snapshot(ulong Dev, ulong Ino, uint Mode, uint Uid, uint Gid,
        ulong Nlink, long Size, long Blocks, long MtimeSec, long MtimeNsec, long CtimeSec, long CtimeNsec)
    {
        public bool IsFile => (Mode & S_IFMT) == S_IFREG;
        public bool IsDir => (Mode & S_IFMT) == S_IFDIR;
        public uint Permissions => Mode & 0xFFF;
    }

    internal sealed class Fd : IDisposable
    {
        public int Value { get; private set; }
        public Snapshot Snapshot { get; }
        public Fd(int fd) { Value = fd; Snapshot = Stat(fd); }
        public int Detach() { int fd = Value; Value = -1; return fd; }
        public void Dispose() { if (Value >= 0) PreparationInterop.CloseFd(Value); Value = -1; }
    }

    private sealed class Scan
    {
        public required InstalledManifest Manifest { get; init; }
        public required Dictionary<string, InstalledArtifact> Files { get; init; }
        public required HashSet<string> Directories { get; init; }
        public required bool Trusted { get; init; }
        public HashSet<string> SeenFiles { get; } = new(StringComparer.Ordinal);
        public HashSet<string> SeenDirs { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, byte[]> Configs { get; } = new(StringComparer.Ordinal);
        public long TotalBytes;
    }

    /// <summary>
    /// Verifies only bytes under an explicitly supplied candidate root. It reads no active
    /// pointer, receipt association or qualification record and returns no trusted identity.
    /// Throws InvalidDataException for malformed or mismatched candidate data.
    /// </summary>
    internal static void VerifyPayload(string absoluteGenerationRoot, bool receiptPresent)
        => VerifyPayloadCore(absoluteGenerationRoot, receiptPresent, trusted: false);

    /// <summary>Root-side staging check. Requires the supplied base chain and closure metadata
    /// to match the installed owner/mode contract, but returns no active installation identity.</summary>
    internal static void VerifyStaged(string absoluteGenerationRoot)
        => VerifyPayloadCore(absoluteGenerationRoot, receiptPresent: true, trusted: true);

    private static void VerifyPayloadCore(string absoluteGenerationRoot, bool receiptPresent, bool trusted)
    {
        EnsureLinuxX64();
        using var root = OpenAbsoluteDirectory(absoluteGenerationRoot, trustedAncestors: trusted);
        RequireDirectory(root.Snapshot, trusted, trusted ? 0u : null, requireExact0755: true);
        var manifestBytes = ReadTrustedFile(root.Value, "manifest.json", trusted ? 0u : null,
            trusted ? 0u : null, 0x124u, MaxMetadataBytes, trusted, out _);
        var manifest = InstalledManifest.Parse(manifestBytes);
        var receipt = receiptPresent ? ReadTrustedFile(root.Value, "install-receipt.json", trusted ? 0u : null,
            trusted ? 0u : null, 0x124u, MaxMetadataBytes, trusted, out _) : null;
        if (receipt is not null) _ = InstalledMetadata.Receipt(receipt, manifest);

        var scan = CreateScan(manifest, trusted);
        scan.Files.Add("manifest.json", MetadataArtifact("metadata-manifest", "manifest.json", manifestBytes));
        if (receiptPresent)
            scan.Files.Add("install-receipt.json", MetadataArtifact("metadata-receipt", "install-receipt.json", receipt!));
        Walk(root.Value, "", 0, scan, includeRootMetadata: true);
        EnsureExact(scan, includeReceipt: receiptPresent);
        InstalledPayload.CheckDependencies(manifest, scan.Configs);
        using var again = OpenAbsoluteDirectory(absoluteGenerationRoot, trusted);
        Same(root.Snapshot,again.Snapshot,"Candidate or staging root replaced during verification.");
    }

    /// <summary>Verifies the fixed production installation. Only this method may return a
    /// trusted state, and Qualified requires a receipt-bound accepted record and host checks.</summary>
    // F-02: the fixed-root integrity prefix below is mirrored by
    // VerifyActiveIntegrity (InstalledFileSystem.Prepublication.cs). The shared
    // parity contract is enforced by InstalledVerificationParityTests; any change
    // to the shared prefix must be applied to both paths and re-reviewed. The
    // qualified-only association/result behavior must remain local to this path.
    internal static InstalledResult VerifyActive()
    {
        try
        {
            EnsureLinuxX64();
            using var root = OpenAbsoluteDirectory(InstallRoot, trustedAncestors: true, missingUnavailable: true);
            RequireDirectory(root.Snapshot,true,0,true);
            using var installations = OpenDirectory(root.Value, "installations", trusted: true, uid: 0, requireExact0755: true);
            using var qualifications = OpenDirectory(root.Value, "qualifications", trusted: true, uid: 0, requireExact0755: true);

            var rootBefore = root.Snapshot;
            var installationsBefore = installations.Snapshot;
            var qualificationsBefore = qualifications.Snapshot;
            byte[] activeBytes = ReadTrustedFile(root.Value, "active", 0, 0, 0x124, 128, true, out var activeBefore);
            string digest = InstalledMetadata.Active(activeBytes);

            using var generation = OpenDirectory(installations.Value, digest, trusted: true, uid: 0, requireExact0755: true);
            var generationBefore = generation.Snapshot;
            byte[] manifestBytes = ReadTrustedFile(generation.Value, "manifest.json", 0, 0, 0x124, MaxMetadataBytes, true, out var manifestBefore);
            var manifest = InstalledManifest.Parse(manifestBytes);
            SameDigest(manifest.Digest, digest, "active generation and manifest digest disagree");
            byte[] receiptBytes = ReadTrustedFile(generation.Value, "install-receipt.json", 0, 0, 0x124, MaxMetadataBytes, true, out var receiptBefore);
            var receipt = InstalledMetadata.Receipt(receiptBytes, manifest);

            var scan = CreateScan(manifest, trusted: true);
            scan.Files.Add("manifest.json", MetadataArtifact("metadata-manifest", "manifest.json", manifestBytes));
            scan.Files.Add("install-receipt.json", MetadataArtifact("metadata-receipt", "install-receipt.json", receiptBytes));
            Walk(generation.Value, "", 0, scan, includeRootMetadata: true);
            EnsureExact(scan, includeReceipt: true);
            InstalledPayload.CheckDependencies(manifest, scan.Configs);

            // A fresh installation has no qualification association and is never Qualified.
            var qualificationId = receipt.QualificationId;
            var qualificationRecordSha256 = receipt.QualificationRecordSha256;
            if (qualificationId is null || qualificationRecordSha256 is null)
            {
                RecheckSelectors(root.Value, installations.Value, qualifications.Value,
                    digest, rootBefore, installationsBefore, qualificationsBefore, activeBefore,
                    generationBefore, manifestBefore, receiptBefore, null, default);
                return InstalledResult.Fresh(manifest);
            }

            string recordName = InstalledMetadata.SelectedRecordName(receipt);
            byte[] recordBytes = ReadTrustedFile(qualifications.Value, recordName, 0, 0, 0x124,
                MaxMetadataBytes, true, out var recordBefore);
            InstalledMetadata.AcceptRecord(recordBytes, receipt, manifest);
            // InstalledPayload.CheckHost throws on every unavailable or mismatched production
            // prerequisite. It is intentionally reached only for the trusted active path.
            InstalledPayload.CheckHost(manifest);

            RecheckSelectors(root.Value, installations.Value, qualifications.Value,
                digest, rootBefore, installationsBefore, qualificationsBefore, activeBefore,
                generationBefore, manifestBefore, receiptBefore, recordName, recordBefore);
            return InstalledResult.Qualified(manifest, qualificationId, qualificationRecordSha256);
        }
        catch (NotSupportedException ex) { return InstalledResult.Unavailable(ex.Message); }
        catch (DllNotFoundException ex) { return InstalledResult.Unavailable("Native verification primitive unavailable: " + ex.Message); }
        catch (EntryPointNotFoundException ex) { return InstalledResult.Unavailable("Native verification primitive unavailable: " + ex.Message); }
        catch (InvalidOperationException ex) { return InstalledResult.Unqualified(ex.Message); }
        catch (System.Text.Json.JsonException ex) { return InstalledResult.Unqualified(ex.Message); }
        catch (System.Collections.Generic.KeyNotFoundException ex) { return InstalledResult.Unqualified(ex.Message); }
        catch (BadImageFormatException ex) { return InstalledResult.Unqualified(ex.Message); }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or
                                   ArgumentException or OverflowException or CryptographicException)
        { return InstalledResult.Unqualified(ex.Message); }
    }

    private static void EnsureLinuxX64()
    {
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new NotSupportedException("Installed artifact verification is supported only on Linux x86-64.");
        if (PreparationInterop.ProbeOpenat2Support() == Openat2SupportKind.Unsupported)
            throw new NotSupportedException("openat2 is unavailable.");
    }

    private static Scan CreateScan(InstalledManifest manifest, bool trusted)
    {
        if (manifest.Artifacts.Count is < 1 or > MaxFiles - 2)
            throw Invalid("Manifest artifact count exceeds the bounded verifier set.");
        var files = new Dictionary<string, InstalledArtifact>(StringComparer.Ordinal);
        var dirs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var artifact in manifest.Artifacts)
        {
            ValidatePath(artifact.Path);
            if (artifact.Length is < 1 or > MaxArtifactBytes || artifact.Mode is not ("0444" or "0555"))
                throw Invalid("Artifact length or mode is outside the verifier contract.");
            if (!files.TryAdd(artifact.Path, artifact)) throw Invalid("Duplicate artifact path in manifest.");
            int slash = artifact.Path.IndexOf('/');
            if (slash > 0) dirs.Add(artifact.Path[..slash]);
        }
        if (dirs.Count > MaxDirectories) throw Invalid("Manifest directory count exceeds verifier bound.");
        return new Scan { Manifest = manifest, Files = files, Directories = dirs, Trusted = trusted };
    }

    private static InstalledArtifact MetadataArtifact(string role, string path, byte[] bytes) =>
        new(role, "metadata", path, Convert.ToHexStringLower(SHA256.HashData(bytes)), bytes.LongLength, "0444");

    private static void ValidatePath(string path)
    {
        if (path.Length is < 3 or > 512 || path[0] == '/' || path.Contains('\\') || path.Contains('\0'))
            throw Invalid("Artifact path is not a bounded relative POSIX path.");
        var parts = path.Split('/');
        if (parts.Length != 2 || parts.Any(p => p.Length is < 1 or > 255 || p is "." or ".." ||
                p.Any(c => c < 0x21 || c > 0x7e || c == ':')))
            throw Invalid("Artifact path grammar is invalid.");
        if (path is "manifest.json" or "install-receipt.json") throw Invalid("Artifact path collides with trusted metadata.");
    }

    private static void Walk(int dirFd, string prefix, int depth, Scan scan, bool includeRootMetadata)
    {
        if (depth > MaxDepth) throw Invalid("Generation directory depth exceeds bound.");
        var before = Stat(dirFd);
        RequireDirectory(before, scan.Trusted, scan.Trusted ? 0u : null, requireExact0755: true);
        foreach (string name in Names(dirFd))
        {
            string path = prefix.Length == 0 ? name : prefix + "/" + name;
            if (includeRootMetadata && prefix.Length == 0 && (name is "manifest.json" or "install-receipt.json"))
            {
                if (!scan.Files.ContainsKey(path)) throw Invalid("Unexpected metadata file.");
                VerifyMetadata(dirFd, name, scan);
                continue;
            }
            if (!scan.Files.ContainsKey(path) && !scan.Directories.Contains(path))
                throw Invalid("Unlisted generation path: " + path);
            using var pinned = OpenPath(dirFd, name);
            var snap = pinned.Snapshot;
            if (snap.IsDir)
            {
                if (!scan.Directories.Contains(path) || !scan.SeenDirs.Add(path)) throw Invalid("Unexpected generation directory: " + path);
                RequireDirectory(snap, scan.Trusted, scan.Trusted ? 0u : null, requireExact0755: true);
                using var child = ReopenPinnedDirectory(pinned.Value);
                Same(snap, child.Snapshot, "Directory changed while pinning.");
                Walk(child.Value, path, depth + 1, scan, includeRootMetadata: false);
                Stable(child.Value, child.Snapshot, "nested generation directory");
                Reacquire(dirFd, name, snap, wantDirectory: true);
            }
            else if (snap.IsFile)
            {
                if (!scan.Files.TryGetValue(path, out var artifact) || !scan.SeenFiles.Add(path))
                    throw Invalid("Unexpected or duplicate generation file: " + path);
                VerifyArtifact(dirFd, name, path, pinned.Value, snap, artifact, scan);
            }
            else
            {
                // O_PATH pins special files without activating devices or blocking on FIFOs.
                throw Invalid("Symlink or special filesystem object is present: " + path);
            }
        }
        Stable(dirFd, before, "directory enumeration");
    }

    private static void VerifyMetadata(int dirFd, string name, Scan scan)
    {
        var expected = scan.Files[name];
        using var fd = OpenRegular(dirFd, name, scan.Trusted, MaxMetadataBytes,
            scan.Trusted ? 0u : null, scan.Trusted ? 0u : null, 0x124u);
        byte[] bytes = ReadPinned(fd.Value, MaxMetadataBytes);
        CheckBytes(expected, bytes);
        Stable(fd.Value, fd.Snapshot, name);
        Reacquire(dirFd, name, fd.Snapshot, wantDirectory: false);
        scan.SeenFiles.Add(name);
    }

    private static void VerifyArtifact(int dirFd, string name, string path, int pathFd,
        Snapshot pinnedStat, InstalledArtifact artifact, Scan scan)
    {
        uint mode = Convert.ToUInt32(artifact.Mode, 8);
        RequireRegular(pinnedStat, scan.Trusted, scan.Trusted ? 0u : null, scan.Trusted ? 0u : null,
            mode, MaxArtifactBytes);
        if (pinnedStat.Size != artifact.Length) throw Invalid("Artifact size mismatch: " + path);
        EnsureNotSparse(pinnedStat, path);
        using var readable = ReopenPinned(pathFd);
        Same(pinnedStat, readable.Snapshot, "Artifact changed before hashing: " + path);
        NoCapabilities(readable.Value, required: true);
        byte[] bytes = ReadPinned(readable.Value, MaxArtifactBytes);
        if (bytes.LongLength != artifact.Length) throw Invalid("Artifact size changed while reading: " + path);
        InstalledPayload.Check(artifact, bytes);
        Stable(readable.Value, pinnedStat, path);
        Reacquire(dirFd, name, pinnedStat, wantDirectory: false);
        scan.TotalBytes = checked(scan.TotalBytes + bytes.LongLength);
        if (scan.TotalBytes > MaxTotalBytes) throw Invalid("Generation exceeds total byte bound.");
        if (artifact.Kind == "config") scan.Configs.Add(path, bytes);
    }

    private static byte[] ReadTrustedFile(int dirFd, string name, uint? uid, uint? gid,
        uint? mode, int maxBytes, bool trusted, out Snapshot snapshot)
    {
        using var fd = OpenRegular(dirFd, name, trusted, maxBytes, uid, gid, mode);
        snapshot = fd.Snapshot;
        byte[] bytes = ReadPinned(fd.Value, maxBytes);
        Stable(fd.Value, snapshot, name);
        Reacquire(dirFd, name, snapshot, wantDirectory: false);
        return bytes;
    }

    private static Fd OpenRegular(int dirFd, string name, bool trusted, int maxBytes,
        uint? uid = null, uint? gid = null, uint? mode = null)
    {
        using var path = OpenPath(dirFd, name);
        var s = path.Snapshot;
        RequireRegular(s, trusted, uid, gid, mode, maxBytes);
        EnsureNotSparse(s, name);
        using var read = ReopenPinned(path.Value);
        Same(s, read.Snapshot, "File changed while being opened: " + name);
        NoCapabilities(read.Value, required: true);
        Reacquire(dirFd, name, s, wantDirectory: false);
        return new Fd(read.Detach());
    }

    private static Fd OpenAbsoluteDirectory(string path, bool trustedAncestors, bool missingUnavailable = false)
    {
        if (string.IsNullOrEmpty(path) || path[0] != '/' || path.Contains('\0')) throw Invalid("Root path must be absolute.");
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 64 || parts.Any(p => p is "." or ".." || p.Length > 255))
            throw Invalid("Root path grammar is invalid.");
        if (PreparationInterop.OpenDirectory("/", out int fd) != 0) throw Invalid("Cannot pin filesystem root.");
        var current = new Fd(fd);
        try
        {
            if (trustedAncestors) RequireDirectory(current.Snapshot, trusted: true, uid: 0, requireExact0755: false);
            foreach (var part in parts)
            {
                using var next = OpenDirectory(current.Value, part, trustedAncestors, trustedAncestors ? 0u : null,
                    requireExact0755: false, missingUnavailable: missingUnavailable);
                current.Dispose(); current = new Fd(next.Detach());
            }
            return current;
        }
        catch { current.Dispose(); throw; }
    }

    internal sealed class ProtectedDirectory : IDisposable
    {
        private readonly Fd _fd;

        internal ProtectedDirectory(Fd fd) => _fd = fd;

        internal Snapshot Snapshot => _fd.Snapshot;

        internal byte[] ReadFile(string name, int maxBytes, out Snapshot snapshot)
        {
            if (name.Length is < 1 or > 255 || name is "." or ".." || name.Contains('/') || name.Contains('\\') || name.Contains('\0'))
                throw Invalid("Protected file name is not a bounded single component.");
            return ReadTrustedFile(_fd.Value, name, 0, 0, 0x124, maxBytes, true, out snapshot);
        }

        public void Dispose() => _fd.Dispose();
    }

    // Final-acceptance stores are separate from the S2 generation inventory. Their
    // leaf directories are managed 0755; every ancestor is checked root-owned and
    // not group/other writable by OpenAbsoluteDirectory.
    internal static ProtectedDirectory OpenProtectedDirectory(string absolutePath)
    {
        EnsureLinuxX64();
        var fd = OpenAbsoluteDirectory(absolutePath, trustedAncestors: true);
        try
        {
            RequireDirectory(fd.Snapshot, trusted: true, uid: 0, requireExact0755: true);
            return new ProtectedDirectory(fd);
        }
        catch
        {
            fd.Dispose();
            throw;
        }
    }

    private static Fd OpenDirectory(int parentFd, string name, bool trusted, uint? uid = null,
        bool requireExact0755 = false, bool missingUnavailable = false)
    {
        int e = PreparationInterop.OpenRelative(parentFd, name,
            PreparationInterop.O_RDONLY | PreparationInterop.O_DIRECTORY | PreparationInterop.O_NOFOLLOW | PreparationInterop.O_CLOEXEC,
            PreparationInterop.RequiredResolve, out int fd);
        if (e == PreparationInterop.ENOENT && missingUnavailable)
            throw new NotSupportedException("Installed trust root is absent at " + name + ".");
        if (e != 0) throw Invalid("Cannot safely open directory " + name + " (errno " + e + ").");
        try
        {
            var opened = new Fd(fd); RequireDirectory(opened.Snapshot, trusted, uid ?? (trusted ? 0u : null), requireExact0755);
            return opened;
        }
        catch { PreparationInterop.CloseFd(fd); throw; }
    }

    private static Fd OpenPath(int dirFd, string name)
    {
        int e = PreparationInterop.OpenRelative(dirFd, name, O_PATH | PreparationInterop.O_NOFOLLOW | PreparationInterop.O_CLOEXEC,
            PreparationInterop.RequiredResolve, out int fd);
        if (e != 0) throw Invalid("Cannot pin path " + name + " (errno " + e + ").");
        try { return new Fd(fd); } catch { PreparationInterop.CloseFd(fd); throw; }
    }

    private static Fd ReopenPinned(int fd)
    {
        var before = Stat(fd);
        if (!before.IsFile) throw Invalid("Read-open attempted on a non-regular pinned object.");
        int read = NativeLaunchInterop.Open("/proc/self/fd/" + fd, PreparationInterop.O_RDONLY | PreparationInterop.O_CLOEXEC);
        if (read < 0) throw Invalid("Cannot reopen pinned regular-file descriptor.");
        try { var result = new Fd(read); Same(before, result.Snapshot, "Pinned file changed at read-open."); return result; }
        catch { PreparationInterop.CloseFd(read); throw; }
    }

    private static Fd ReopenPinnedDirectory(int fd)
    {
        var before = Stat(fd);
        if (!before.IsDir) throw Invalid("Directory descriptor expected.");
        int read = NativeLaunchInterop.Open("/proc/self/fd/" + fd,
            PreparationInterop.O_RDONLY | PreparationInterop.O_DIRECTORY | PreparationInterop.O_CLOEXEC);
        if (read < 0) throw Invalid("Cannot reopen pinned directory descriptor.");
        try { var result = new Fd(read); Same(before, result.Snapshot, "Pinned directory changed at read-open."); return result; }
        catch { PreparationInterop.CloseFd(read); throw; }
    }

    private static void Reacquire(int parentFd, string name, Snapshot expected, bool wantDirectory)
    {
        using var again = OpenPath(parentFd, name);
        if (wantDirectory ? !again.Snapshot.IsDir : !again.Snapshot.IsFile) throw Invalid("Reacquired path type changed: " + name);
        Same(expected, again.Snapshot, "Path replaced or changed during verification: " + name);
    }

    private static Snapshot Stat(int fd)
    {
        if (fd < 0 || FStat(fd, out var s) != 0) throw new NotSupportedException("fstat failed (errno " + Marshal.GetLastPInvokeError() + ").");
        return new(s.Dev, s.Ino, s.Mode, s.Uid, s.Gid, s.Nlink, s.Size, s.Blocks,
            s.MtimeSec, s.MtimeNsec, s.CtimeSec, s.CtimeNsec);
    }

    internal static void RequireDirectory(Snapshot s, bool trusted, uint? uid, bool requireExact0755)
    {
        if (!s.IsDir) throw Invalid("Directory expected.");
        // Candidate ancestors (including /tmp) are not a trust anchor. Each component was opened without following links.
        if (!trusted && !requireExact0755) return;
        if (uid.HasValue && s.Uid != uid.Value) throw Invalid("Directory uid mismatch.");
        if (s.Nlink < 2 || (s.Permissions & (S_ISUID | S_ISGID)) != 0)
            throw Invalid("Directory link metadata or set-id bits are invalid.");
        if (trusted && s.Gid != 0) throw Invalid("Trusted directory gid mismatch.");
        if ((s.Permissions & 0x12 /* group/other write */) != 0)
            throw Invalid("Directory is group/other writable.");
        if (requireExact0755 && s.Permissions != 0x1ED)
            throw Invalid("Generation directory mode must be exactly 0755.");
    }

    internal static void RequireRegular(Snapshot s, bool trusted, uint? uid, uint? gid, uint? mode, long max)
    {
        if (!s.IsFile || s.Nlink != 1 || s.Size < 0 || s.Size > max) throw Invalid("Regular file size/type is invalid.");
        if (uid.HasValue && s.Uid != uid.Value) throw Invalid("File uid mismatch.");
        if (gid.HasValue && s.Gid != gid.Value) throw Invalid("File gid mismatch.");
        if (mode.HasValue && s.Permissions != mode.Value) throw Invalid("File mode mismatch.");
        if ((s.Permissions & (S_ISUID | S_ISGID)) != 0) throw Invalid("File has set-id bits.");
    }

    private static void NoCapabilities(int fd, bool required)
    {
        if (required && NativeLaunchInterop.HasFileCapabilityXattr(fd, out var error))
            throw Invalid("File capability inspection failed or capability is present: " + error);
    }

    private static void EnsureNotSparse(Snapshot s, string path)
    {
        if (s.Blocks < 0 || checked(s.Blocks * 512) < s.Size) throw Invalid("Sparse/abnormal file refused: " + path);
    }

    private static byte[] ReadPinned(int fd, int maximum)
    {
        using var stream = new FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)fd, ownsHandle: false),
            FileAccess.Read, 64 * 1024, isAsync: false);
        using var output = new MemoryStream();
        var buffer = new byte[64 * 1024];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
        {
            if (output.Length + read > maximum) throw Invalid("File exceeds bounded read size.");
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }

    private static IEnumerable<string> Names(int fd)
    {
        IntPtr buffer = Marshal.AllocHGlobal(16 * 1024);
        try
        {
            while (true)
            {
                long result;
                do { result = Syscall(Getdents64X64, fd, buffer, 16 * 1024); }
                while (result < 0 && Marshal.GetLastPInvokeError() == PreparationInterop.EINTR);
                if (result < 0) throw new NotSupportedException("getdents64 failed (errno " + Marshal.GetLastPInvokeError() + ").");
                if (result == 0) yield break;
                for (int offset = 0; offset < result;)
                {
                    if (result - offset < 20) throw Invalid("Malformed getdents64 record.");
                    var rec = IntPtr.Add(buffer, offset);
                    int length = (ushort)Marshal.ReadInt16(rec, 16);
                    if (length < 20 || length > result - offset) throw Invalid("Malformed getdents64 record size.");
                    byte[] bytes = new byte[length - 19]; Marshal.Copy(IntPtr.Add(rec, 19), bytes, 0, bytes.Length);
                    int nul = Array.IndexOf(bytes, (byte)0); if (nul < 0) throw Invalid("Malformed directory entry name.");
                    string name;
                    try { name = new UTF8Encoding(false, true).GetString(bytes, 0, nul); }
                    catch (DecoderFallbackException ex) { throw new InvalidDataException("Non-UTF8 directory entry.", ex); }
                    offset += length;
                    if (name is "." or "..") continue;
                    if (name.Length is < 1 or > 255 || name.Contains('/') || name.Contains('\\') || name.Contains('\0'))
                        throw Invalid("Invalid directory entry name.");
                    yield return name;
                }
            }
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static void CheckBytes(InstalledArtifact artifact, byte[] bytes)
    {
        if (bytes.LongLength != artifact.Length || Convert.ToHexStringLower(SHA256.HashData(bytes)) != artifact.Sha256)
            throw Invalid("Metadata byte identity mismatch: " + artifact.Path);
    }

    private static void Stable(int fd, Snapshot before, string what) => Same(before, Stat(fd), "Metadata changed while reading: " + what);
    private static void Same(Snapshot expected, Snapshot actual, string reason)
    { if (expected != actual) throw Invalid(reason); }

    private static void SameDigest(string actual, string expected, string reason)
    { if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(expected))) throw Invalid(reason); }

    private static void EnsureExact(Scan scan, bool includeReceipt)
    {
        foreach (var artifact in scan.Manifest.Artifacts)
            if (!scan.SeenFiles.Contains(artifact.Path)) throw Invalid("Manifest artifact missing: " + artifact.Path);
        foreach (var dir in scan.Directories)
            if (!scan.SeenDirs.Contains(dir)) throw Invalid("Manifest directory missing: " + dir);
        if (!scan.SeenFiles.Contains("manifest.json")) throw Invalid("manifest.json missing from exact set.");
        if (includeReceipt != scan.SeenFiles.Contains("install-receipt.json")) throw Invalid("Receipt exact-set mismatch.");
    }

    private static void RecheckSelectors(int rootFd, int installationsFd, int qualsFd, string digest,
        Snapshot root, Snapshot installations, Snapshot qualifications, Snapshot active,
        Snapshot generation, Snapshot manifest, Snapshot receipt, string? recordName, Snapshot record)
    {
        using var pointer = OpenRegular(rootFd, "active", true, 128, 0, 0, 0x124);
        Same(active, pointer.Snapshot, "active pointer replaced during verification.");
        using var gens = OpenDirectory(rootFd, "installations", trusted: true, uid: 0, requireExact0755: true);
        Same(installations, gens.Snapshot, "installations directory changed during verification.");
        using var qs = OpenDirectory(rootFd, "qualifications", trusted: true, uid: 0, requireExact0755: true);
        Same(qualifications, qs.Snapshot, "qualifications directory changed during verification.");
        using var rootAgain = OpenAbsoluteDirectory(InstallRoot, trustedAncestors: true);
        Same(root, rootAgain.Snapshot, "installation root changed during verification.");
        using var gen = OpenDirectory(installationsFd, digest, trusted: true, uid: 0, requireExact0755: true);
        Same(generation, gen.Snapshot, "active generation directory replaced during verification.");
        using var m = OpenRegular(gen.Value, "manifest.json", true, MaxMetadataBytes, 0, 0, 0x124); Same(manifest, m.Snapshot, "manifest replaced during verification.");
        using var r = OpenRegular(gen.Value, "install-receipt.json", true, MaxMetadataBytes, 0, 0, 0x124); Same(receipt, r.Snapshot, "receipt replaced during verification.");
        if (recordName is not null)
        { using var q = OpenRegular(qualsFd, recordName, true, MaxMetadataBytes, 0, 0, 0x124); Same(record, q.Snapshot, "qualification record replaced during verification."); }
    }

    private static InvalidDataException Invalid(string message) => new(message);
}
