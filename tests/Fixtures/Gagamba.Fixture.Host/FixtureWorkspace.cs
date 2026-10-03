// Disposable synthetic workspace. Host-created sentinel set only.
// Cleanup targets only known fixture-owned roots carrying the marker file.
using System.Security.Cryptography;
using System.Text;

namespace Gagamba.Fixture.Host;

public sealed class FixtureWorkspace : IDisposable
{
    public const string MarkerFileName = ".gagamba-fixture-root";
    public string Root { get; }
    public string InputDir => Path.Combine(Root, "input");
    public string OutputDir => Path.Combine(Root, "output");
    public string ScopeDir => Path.Combine(Root, "scope");

    private bool _disposed;

    private FixtureWorkspace(string root) => Root = root;

    public static FixtureWorkspace Create(string runId)
    {
        string safe = new string(runId.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        if (string.IsNullOrWhiteSpace(safe))
            safe = Guid.NewGuid().ToString("N");
        string root = Path.Combine(Path.GetTempPath(), $"gagamba-fixture-{safe}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "input"));
        Directory.CreateDirectory(Path.Combine(root, "output"));
        Directory.CreateDirectory(Path.Combine(root, "scope"));
        // Ownership marker: cleanup refuses to delete without it.
        File.WriteAllText(Path.Combine(root, MarkerFileName), $"runId={runId} pid={Environment.ProcessId}\n");
        return new FixtureWorkspace(root);
    }

    public string WriteSentinel(string relativePath, byte[] content)
    {
        string full = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, content);
        return full;
    }

    public static byte[] SyntheticBytes(string seed, int length)
    {
        // Deterministic synthetic data: SHA256(seed:counter) keystream. No secrets.
        var outBytes = new byte[length];
        int pos = 0;
        int counter = 0;
        while (pos < length)
        {
            byte[] block = SHA256.HashData(Encoding.UTF8.GetBytes($"{seed}:{counter}"));
            int take = Math.Min(block.Length, length - pos);
            Buffer.BlockCopy(block, 0, outBytes, pos, take);
            pos += take;
            counter++;
        }
        return outBytes;
    }

    public static string Sha256Hex(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public string Resolve(string relativePath)
    {
        if (Path.IsPathRooted(relativePath) || relativePath.Contains(':'))
            throw new InvalidOperationException("workspace path must be relative without drive/ADS syntax");
        string full = Path.GetFullPath(Path.Combine(Root, relativePath));
        string prefix = Root.EndsWith(Path.DirectorySeparatorChar) ? Root : Root + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("workspace path escapes root");
        return full;
    }

    /// <summary>Independent cleanup: only deletes roots we created (marker present).</summary>
    public string DisposeAndReport()
    {
        if (_disposed) return "Confirmed";
        _disposed = true;
        try
        {
            string marker = Path.Combine(Root, MarkerFileName);
            if (!File.Exists(marker))
                return "Unknown";
            Directory.Delete(Root, recursive: true);
            return Directory.Exists(Root) ? "Failed" : "Confirmed";
        }
        catch
        {
            return Directory.Exists(Root) ? "Failed" : "Confirmed";
        }
    }

    public void Dispose() => DisposeAndReport();
}
