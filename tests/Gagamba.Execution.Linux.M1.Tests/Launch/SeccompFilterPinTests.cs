using System.Reflection;
using System.Security.Cryptography;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Preparation;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Launch;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class SeccompFilterPinTests
{
    private static readonly MethodInfo VerifyMethod = typeof(BubblewrapAdapter).GetMethod(
        "ReadAndVerifyFilter", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(typeof(BubblewrapAdapter).FullName, "ReadAndVerifyFilter");

    [Fact]
    public void AcceptsTheGeneratedPinnedFilterArtifact()
    {
        SkipUnlessLinux();
        var bytes = ReadGeneratedFilter();
        Assert.Equal(672UL, BubblewrapAdapter.FilterLength);
        Assert.Equal(BubblewrapAdapter.FilterSha256,
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());

        var result = Verify(bytes, SHA256.HashData(bytes));
        Assert.True(result.Accepted, result.Error);
        Assert.Equal(bytes, result.ReadBack);
    }

    [Fact]
    public void RejectsCorruptedFilterEvenWhenItsStagedDigestMatchesTheCorruptBytes()
    {
        SkipUnlessLinux();
        var corrupt = ReadGeneratedFilter();
        corrupt[0] ^= 0x01;

        var result = Verify(corrupt, SHA256.HashData(corrupt));
        Assert.False(result.Accepted);
        Assert.Contains("digest", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsAStagedHashThatDoesNotMatchTheFilterBytes()
    {
        SkipUnlessLinux();
        var bytes = ReadGeneratedFilter();
        var wrongHash = SHA256.HashData(bytes);
        wrongHash[0] ^= 0x80;

        var result = Verify(bytes, wrongHash);
        Assert.False(result.Accepted);
        Assert.Contains("digest", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsAnArtifactWithTheWrongPinnedLength()
    {
        SkipUnlessLinux();
        var bytes = ReadGeneratedFilter()[..^1];

        var result = Verify(bytes, SHA256.HashData(bytes));
        Assert.False(result.Accepted);
        Assert.Contains("length", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static void SkipUnlessLinux()
    {
        if (!OperatingSystem.IsLinux()) Assert.Skip("requires Linux pread for the pinned native filter verifier");
    }

    private static byte[] ReadGeneratedFilter()
    {
        var configuredRoot = Environment.GetEnvironmentVariable("GAGAMBA_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            var fullRoot = Path.GetFullPath(configuredRoot);
            if (!File.Exists(Path.Combine(fullRoot, "Gagamba.sln")))
                throw new DirectoryNotFoundException("GAGAMBA_REPOSITORY_ROOT does not identify the Gagamba repository");
            return File.ReadAllBytes(Path.Combine(fullRoot, "artifacts/linux-m1/wp04/seccomp-filter.bin"));
        }
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "Gagamba.sln")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Gagamba repository root not found");
        return File.ReadAllBytes(Path.Combine(root, "artifacts/linux-m1/wp04/seccomp-filter.bin"));
    }

    private static (bool Accepted, byte[] ReadBack, string Error) Verify(byte[] bytes, byte[] stagedHash)
    {
        var path = Path.Combine(Path.GetTempPath(), "gagamba-seccomp-pin-" + Guid.NewGuid().ToString("N"));
        File.WriteAllBytes(path, bytes);
        try
        {
            using SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var staged = new StagedObject("filter", stagedHash, (ulong)bytes.Length, 0, 0, 0, "filter", handle);
            object?[] arguments = { staged, Array.Empty<byte>(), string.Empty };
            var accepted = (bool)(VerifyMethod.Invoke(null, arguments)
                ?? throw new InvalidOperationException("filter verifier returned no result"));
            return (accepted, (byte[])arguments[1]!, (string)arguments[2]!);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
