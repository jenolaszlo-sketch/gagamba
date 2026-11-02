using System.Runtime.InteropServices;
using Xunit;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Tests.Installed;

/// <summary>
/// Non-privileged native tests run against a locally built production candidate. Set
/// GAGAMBA_S2_TEST_CANDIDATE to a VerifyPayload-ready candidate directory; the tests copy it
/// into a private temporary directory, so no installed state is touched.
/// </summary>
public sealed class InstalledFileSystemNativeTests
{
    private const string CandidateVariable = "GAGAMBA_S2_TEST_CANDIDATE";

    [DllImport("libc", EntryPoint = "mkfifo", SetLastError = true)]
    private static extern int MkFifo(string path, uint mode);

    [DllImport("libc", EntryPoint = "link", SetLastError = true)]
    private static extern int HardLink(string source,string destination);

    [DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint Uid();

    [Fact]
    public void VerifyPayload_accepts_candidate_and_refuses_unlisted_symlink_fifo_hardlink_and_sparse_files()
    {
        if (!OperatingSystem.IsLinux())
            Assert.Skip("Linux fd verification test.");
        if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture != Architecture.X64)
            Assert.Skip("S2 native qualification is x86-64 only.");
        if (Uid() == 0) Assert.Skip("Non-root host required.");
        if (PreparationInterop.ProbeOpenat2Support() == Openat2SupportKind.Unsupported)
            Assert.Skip("openat2 is unavailable on this kernel.");

        string? source = Environment.GetEnvironmentVariable(CandidateVariable);
        if (string.IsNullOrWhiteSpace(source) || !Directory.Exists(source))
            Assert.Skip($"Set {CandidateVariable} to a production candidate directory.");

        var manifest = InstalledManifest.Parse(File.ReadAllBytes(Path.Combine(source, "manifest.json")));
        bool receiptPresent = File.Exists(Path.Combine(source, "install-receipt.json"));
        using var temp = new TemporaryDirectory();
        string root = Path.Combine(temp.Path, "candidate");
        CopyCandidate(source, root, manifest);

        // The API returns void and touches no authority selector; passing this test cannot mint
        // an InstalledResult, digest selection, or qualification decision.
        InstalledFileSystem.VerifyPayload(root, receiptPresent);

        File.WriteAllText(Path.Combine(root, "unlisted.bin"), "extra");
        Assert.Throws<InvalidDataException>(() => InstalledFileSystem.VerifyPayload(root, receiptPresent));
        File.Delete(Path.Combine(root, "unlisted.bin"));

        var artifact = manifest.Artifacts.First(a => a.Kind == "managed");
        string artifactPath = Path.Combine(root, artifact.Path.Replace('/', Path.DirectorySeparatorChar));
        string sourceArtifact = Path.Combine(source, artifact.Path.Replace('/', Path.DirectorySeparatorChar));
        byte[] original = File.ReadAllBytes(sourceArtifact);

        File.Delete(artifactPath);
        File.CreateSymbolicLink(artifactPath, sourceArtifact);
        Assert.Throws<InvalidDataException>(() => InstalledFileSystem.VerifyPayload(root, receiptPresent));
        File.Delete(artifactPath);
        RestoreArtifact(artifactPath, original, artifact.Mode);

        File.Delete(artifactPath);
        Assert.Equal(0, MkFifo(artifactPath, 0x1A4 /* 0644 */));
        Assert.Throws<InvalidDataException>(() => InstalledFileSystem.VerifyPayload(root, receiptPresent));
        File.Delete(artifactPath);
        RestoreArtifact(artifactPath, original, artifact.Mode);

        // An external second name changes the inode link count without adding an entry to the generation.
        Assert.Equal(0,HardLink(artifactPath,Path.Combine(temp.Path, "outside-link")));
        Assert.Throws<InvalidDataException>(() => InstalledFileSystem.VerifyPayload(root, receiptPresent));
        File.Delete(Path.Combine(temp.Path, "outside-link"));

        SetMode(artifactPath,"0644");
        Assert.Throws<InvalidDataException>(() => InstalledFileSystem.VerifyPayload(root,receiptPresent));
        SetMode(artifactPath,artifact.Mode);
        File.Delete(artifactPath);
        Assert.Throws<InvalidDataException>(() => InstalledFileSystem.VerifyPayload(root,receiptPresent));
        RestoreArtifact(artifactPath,original,artifact.Mode);
        var wrong = (byte[])original.Clone();
        wrong[0] ^= 1;
        File.Delete(artifactPath);
        RestoreArtifact(artifactPath,wrong,artifact.Mode);
        Assert.Throws<InvalidDataException>(() => InstalledFileSystem.VerifyPayload(root,receiptPresent));
        File.Delete(artifactPath);
        RestoreArtifact(artifactPath,original,artifact.Mode);
        Directory.CreateSymbolicLink(Path.Combine(temp.Path,"alias"),root);
        Assert.Throws<InvalidDataException>(() => InstalledFileSystem.VerifyPayload(Path.Combine(temp.Path,"alias"),receiptPresent));
        Directory.Delete(Path.Combine(temp.Path,"alias"));
        InstalledFileSystem.VerifyPayload(root,receiptPresent);
        var largest = manifest.Artifacts.OrderByDescending(a => a.Length).First();
        string largestPath = Path.Combine(root, largest.Path.Replace('/', Path.DirectorySeparatorChar));
        File.Delete(largestPath);
        using (var sparse = new FileStream(largestPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            sparse.SetLength(largest.Length);
        SetMode(largestPath, largest.Mode);
        var sparseFailure = Assert.Throws<InvalidDataException>(() => InstalledFileSystem.VerifyPayload(root, receiptPresent));
        Assert.Contains("Sparse/abnormal", sparseFailure.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyCandidate(string source, string destination, InstalledManifest manifest)
    {
        Directory.CreateDirectory(destination);
        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(destination, relative));
        }
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, file);
            string target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
            string mode = relative is "manifest.json" or "install-receipt.json"
                ? "0444"
                : manifest.Artifacts.Single(a => a.Path == relative.Replace(Path.DirectorySeparatorChar, '/')).Mode;
            SetMode(target, mode);
        }
        foreach (string directory in Directory.EnumerateDirectories(destination, "*", SearchOption.AllDirectories))
            SetMode(directory, "0755");
        SetMode(destination, "0755");
    }

    private static void RestoreArtifact(string path, byte[] bytes, string mode)
    {
        File.WriteAllBytes(path, bytes);
        SetMode(path, mode);
    }

    private static void SetMode(string path, string mode)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();
        File.SetUnixFileMode(path,(UnixFileMode)Convert.ToInt32(mode,8));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gagamba-s2-" + Guid.NewGuid().ToString("N"));
        public TemporaryDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
