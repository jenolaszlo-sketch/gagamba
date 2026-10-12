// WP02 deterministic fake copy host: in-memory directory/file model with
// fault injection for portable tests. Same engine code path as production;
// faults that need real kernel behavior live in the Linux-native tests.
using System.Runtime.InteropServices;
using System.Text;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Tests.Preparation;

internal sealed class FakeFdHandle : SafeHandle
{
    private readonly FakeCopyHost _host;
    private readonly int _id;

    public FakeFdHandle(FakeCopyHost host, int id)
        : base(new IntPtr(id), ownsHandle: true)
    {
        _host = host;
        _id = id;
    }

    public int Id => _id;

    public override bool IsInvalid => handle == IntPtr.Zero || handle == new IntPtr(-1);

    protected override bool ReleaseHandle()
    {
        _host.NotifyClosed(_id);
        SetHandle(IntPtr.Zero);
        return true;
    }
}

internal sealed class FakeCopyHost : ICopyHost
{
    private sealed class Node
    {
        public byte[] Content = Array.Empty<byte>();
        public uint Mode;
        public ulong Ino;
        public int Nlink = 1;
        public bool IsSymlink;
        public bool Sparse;
        public bool UnreliableBlocks;
    }

    private sealed class Directory
    {
        public readonly Dictionary<string, Node> Files = new(StringComparer.Ordinal);
        public readonly Dictionary<string, Directory> Subdirectories = new(StringComparer.Ordinal);
        public ulong Ino;
    }

    private sealed class OpenDescription
    {
        public required object Target;
        public required string Access;
    }

    private readonly Dictionary<string, Directory> _roots = new(StringComparer.Ordinal);
    private readonly Dictionary<int, OpenDescription> _open = new();
    private readonly HashSet<int> _live = new();
    private int _nextFd = 100;
    private ulong _nextIno = 1000;
    private const ulong FakeDevice = 0xA11CE;

    public string? RootPath { get; set; }

    public string? ParentPath { get; set; }

    public Func<string, int>? OpenSourceErrno { get; set; }

    public Func<string, int>? UnlinkErrno { get; set; }

    public Func<string, int>? CreateStagingErrno { get; set; }

    public int FailReadsAfterChunks { get; set; } = -1;

    public int FailWritesAfterChunks { get; set; } = -1;

    public int ReadChunks;
    public int WriteChunks;
    public int SyncFileCalls;
    public int SyncDirectoryCalls;

    public IReadOnlyCollection<int> LiveHandles => _live;

    public IReadOnlyCollection<string> LiveWriteAccesses() =>
        _live.Where(id => _open[id].Access == "write").Select(id => $"fd{id}").ToArray();

    public void AddSourceFile(string name, byte[] content, int nlink = 1)
    {
        RequireRoot();
        _roots[RootPath!].Files[name] = new Node { Content = (byte[])content.Clone(), Mode = 0x100, Ino = _nextIno++, Nlink = nlink };
    }

    public void AddSourceSymlink(string name)
    {
        RequireRoot();
        _roots[RootPath!].Files[name] = new Node { IsSymlink = true, Ino = _nextIno++ };
    }

    public void AddSparseSourceFile(string name, byte[] content)
    {
        RequireRoot();
        _roots[RootPath!].Files[name] = new Node { Content = (byte[])content.Clone(), Mode = 0x100, Ino = _nextIno++, Sparse = true };
    }

    public void AddUnreliableSourceFile(string name, byte[] content)
    {
        RequireRoot();
        _roots[RootPath!].Files[name] = new Node { Content = (byte[])content.Clone(), Mode = 0x100, Ino = _nextIno++, UnreliableBlocks = true };
    }

    public void AddSourceDirectory(string name)
    {
        RequireRoot();
        _roots[RootPath!].Subdirectories[name] = new Directory { Ino = _nextIno++ };
    }

    public void AppendSource(string name, byte[] extra)
    {
        var node = _roots[RootPath!].Files[name];
        var grown = new byte[node.Content.Length + extra.Length];
        Buffer.BlockCopy(node.Content, 0, grown, 0, node.Content.Length);
        Buffer.BlockCopy(extra, 0, grown, node.Content.Length, extra.Length);
        node.Content = grown;
    }

    public void TruncateSource(string name, int length)
    {
        var node = _roots[RootPath!].Files[name];
        var cut = new byte[length];
        Buffer.BlockCopy(node.Content, 0, cut, 0, Math.Min(length, node.Content.Length));
        node.Content = cut;
    }

    public byte[] ReadSource(string name) => (byte[])_roots[RootPath!].Files[name].Content.Clone();

    public void PlantStagingCollision(string name, byte[] content)
    {
        PendingCollision = (name, (byte[])content.Clone());
    }

    public (string Name, byte[] Content)? PendingCollision { get; private set; }

    public bool StagingFileExists(string name)
    {
        var subdir = FindSubdir();
        return subdir is not null && subdir.Files.ContainsKey(name);
    }

    public IReadOnlyCollection<string> StagingFileNames()
    {
        var subdir = FindSubdir();
        return subdir is null
            ? Array.Empty<string>()
            : subdir.Files.Keys.ToArray();
    }

    public byte[] ReadStaged(string name)
    {
        var subdir = FindSubdir();
        ArgumentNullException.ThrowIfNull(subdir);
        return (byte[])subdir.Files[name].Content.Clone();
    }

    public void ReplaceStaged(string name, byte[] content)
    {
        var subdir = FindSubdir();
        ArgumentNullException.ThrowIfNull(subdir);
        subdir.Files[name] = new Node { Content = (byte[])content.Clone(), Mode = subdir.Files[name].Mode, Ino = _nextIno++ };
    }

    public void MutateStaged(string name, byte[] content)
    {
        var subdir = FindSubdir();
        ArgumentNullException.ThrowIfNull(subdir);
        subdir.Files[name].Content = (byte[])content.Clone();
    }

    public void ReplaceStagedWithSymlink(string name)
    {
        var subdir = FindSubdir();
        ArgumentNullException.ThrowIfNull(subdir);
        subdir.Files[name] = new Node { IsSymlink = true, Ino = _nextIno++ };
    }

    public void ChmodStaged(string name, uint mode)
    {
        var subdir = FindSubdir();
        ArgumentNullException.ThrowIfNull(subdir);
        subdir.Files[name].Mode = mode;
    }

    public void SetStagedNlink(string name, int nlink)
    {
        var subdir = FindSubdir();
        ArgumentNullException.ThrowIfNull(subdir);
        subdir.Files[name].Nlink = nlink;
    }

    public void NotifyClosed(int id) => _live.Remove(id);

    private void RequireRoot()
    {
        if (RootPath is null)
        {
            throw new InvalidOperationException("FakeCopyHost.RootPath must be set.");
        }

        _roots.TryAdd(RootPath, new Directory { Ino = _nextIno++ });
    }

    private Directory? FindSubdir()
    {
        if (ParentPath is null || !_roots.TryGetValue(ParentPath, out var parent))
        {
            return null;
        }

        return parent.Subdirectories.Values.FirstOrDefault();
    }

    private int Allocate(object target, string access)
    {
        var id = _nextFd++;
        _open[id] = new OpenDescription { Target = target, Access = access };
        _live.Add(id);
        return id;
    }

    public int OpenDirectory(string path, out SafeHandle? handle)
    {
        handle = null;
        if (path.Contains('\0'))
        {
            return PreparationInterop.EINVAL;
        }

        if (Encoding.UTF8.GetByteCount(path) > 4096)
        {
            return PreparationInterop.ENAMETOOLONG;
        }

        if (!_roots.TryGetValue(path, out var dir))
        {
            dir = new Directory { Ino = _nextIno++ };
            _roots[path] = dir;
        }

        handle = new FakeFdHandle(this, Allocate(dir, "dir"));
        return 0;
    }

    public int OpenSubdirectory(SafeHandle parent, string name, out SafeHandle? handle)
    {
        handle = null;
        var desc = _open[((FakeFdHandle)parent).Id];
        var dir = (Directory)desc.Target;
        if (!dir.Subdirectories.TryGetValue(name, out var subdir))
        {
            return PreparationInterop.ENOENT;
        }

        handle = new FakeFdHandle(this, Allocate(subdir, "dir"));
        return 0;
    }

    public int StatHandle(SafeHandle handle, out FileIdentity identity)
    {
        identity = default;
        var desc = _open[((FakeFdHandle)handle).Id];
        if (desc.Target is Directory dir)
        {
            identity = new FileIdentity(FakeDevice, dir.Ino, PreparationInterop.S_IFDIR | 0x1C0, 2, 0, 0);
            return 0;
        }

        var node = (Node)desc.Target;
        long blocks = node.UnreliableBlocks
            ? 0
            : node.Sparse
                ? node.Content.Length / 1024
                : (node.Content.Length + 511) / 512;
        identity = new FileIdentity(
            FakeDevice, node.Ino, node.Mode | PreparationInterop.S_IFREG,
            (ulong)node.Nlink, node.Content.Length, blocks);
        return 0;
    }

    public int OpenSource(SafeHandle root, string name, out SafeHandle? handle)
    {
        handle = null;
        if (OpenSourceErrno is not null)
        {
            var injected = OpenSourceErrno(name);
            if (injected != 0)
            {
                return injected;
            }
        }

        var desc = _open[((FakeFdHandle)root).Id];
        var dir = (Directory)desc.Target;
        if (dir.Subdirectories.ContainsKey(name))
        {
            var subdir = dir.Subdirectories[name];
            handle = new FakeFdHandle(this, Allocate(subdir, "read"));
            return 0;
        }

        if (!dir.Files.TryGetValue(name, out var node))
        {
            return PreparationInterop.ENOENT;
        }

        if (node.IsSymlink)
        {
            return PreparationInterop.ELOOP;
        }

        handle = new FakeFdHandle(this, Allocate(node, "read"));
        return 0;
    }

    public int CreateStaging(SafeHandle parent, string name, out SafeHandle? handle)
    {
        handle = null;
        if (CreateStagingErrno is not null)
        {
            var injected = CreateStagingErrno(name);
            if (injected != 0)
            {
                return injected;
            }
        }

        var desc = _open[((FakeFdHandle)parent).Id];
        var dir = (Directory)desc.Target;
        if (dir.Files.ContainsKey(name))
        {
            return PreparationInterop.EEXIST;
        }

        var node = new Node { Mode = PreparationInterop.Mode0600, Ino = _nextIno++ };
        dir.Files[name] = node;
        handle = new FakeFdHandle(this, Allocate(node, "write"));
        return 0;
    }

    public int MakeDirectory(SafeHandle parent, string name, uint mode)
    {
        var desc = _open[((FakeFdHandle)parent).Id];
        var dir = (Directory)desc.Target;
        if (dir.Subdirectories.ContainsKey(name) || dir.Files.ContainsKey(name))
        {
            return PreparationInterop.EEXIST;
        }

        var subdir = new Directory { Ino = _nextIno++ };
        dir.Subdirectories[name] = subdir;
        if (PendingCollision is not null)
        {
            subdir.Files[PendingCollision.Value.Name] =
                new Node { Content = (byte[])PendingCollision.Value.Content.Clone(), Mode = PreparationInterop.Mode0600, Ino = _nextIno++ };
            PendingCollision = null;
        }

        return 0;
    }

    public Stream OpenRead(SafeHandle handle) =>
        new FakeReadStream(this, (Node)_open[((FakeFdHandle)handle).Id].Target);

    public Stream OpenWrite(SafeHandle handle) =>
        new FakeWriteStream(this, (Node)_open[((FakeFdHandle)handle).Id].Target);

    public int SetMode(SafeHandle handle, uint mode)
    {
        ((Node)_open[((FakeFdHandle)handle).Id].Target).Mode = mode;
        return 0;
    }

    public int SyncFile(SafeHandle handle)
    {
        SyncFileCalls++;
        return 0;
    }

    public int SyncDirectory(SafeHandle handle)
    {
        SyncDirectoryCalls++;
        return 0;
    }

    public int Unlink(SafeHandle parent, string name)
    {
        if (UnlinkErrno is not null)
        {
            var injected = UnlinkErrno(name);
            if (injected != 0)
            {
                return injected;
            }
        }

        var desc = _open[((FakeFdHandle)parent).Id];
        var dir = (Directory)desc.Target;
        if (dir.Files.Remove(name))
        {
            return 0;
        }

        if (dir.Subdirectories.Remove(name))
        {
            return 0;
        }

        return PreparationInterop.ENOENT;
    }

    private sealed class FakeReadStream : MemoryStream
    {
        private readonly FakeCopyHost _host;
        private readonly Node _node;
        private int _chunks;

        public FakeReadStream(FakeCopyHost host, Node node)
        {
            _host = host;
            _node = node;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_host.FailReadsAfterChunks >= 0 && _chunks >= _host.FailReadsAfterChunks)
            {
                throw new IOException("Injected read failure.");
            }

            var available = Math.Max(0, _node.Content.Length - Position);
            var take = (int)Math.Min(available, count);
            if (take == 0)
            {
                return 0;
            }

            Buffer.BlockCopy(_node.Content, (int)Position, buffer, offset, take);
            Position += take;
            _chunks++;
            _host.ReadChunks++;
            return take;
        }
    }

    private sealed class FakeWriteStream : MemoryStream
    {
        private readonly FakeCopyHost _host;
        private readonly Node _node;
        private int _chunks;

        public FakeWriteStream(FakeCopyHost host, Node node)
        {
            _host = host;
            _node = node;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_host.FailWritesAfterChunks >= 0 && _chunks >= _host.FailWritesAfterChunks)
            {
                throw new IOException("Injected write failure.");
            }

            var grown = new byte[_node.Content.Length + count];
            Buffer.BlockCopy(_node.Content, 0, grown, 0, _node.Content.Length);
            Buffer.BlockCopy(buffer, offset, grown, _node.Content.Length, count);
            _node.Content = grown;
            _chunks++;
            _host.WriteChunks++;
        }
    }
}
