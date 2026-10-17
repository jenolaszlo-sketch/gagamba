using Gagamba.Execution.Linux.M1.Launch;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Launch;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class CoordinatorMountVerifierTests
{
    private static readonly IReadOnlySet<string> NoHostDevices = new HashSet<string>(StringComparer.Ordinal);

    private const string ValidMountInfo =
        "450 278 0:88 /newroot / ro,nosuid,nodev,relatime - tmpfs tmpfs rw,uid=1000,gid=1000\n" +
        "451 450 0:72 /stage/tool /tool ro,nosuid,nodev - tmpfs tmpfs rw,size=3940128k\n" +
        "452 450 0:72 /stage/input /input ro,nosuid,nodev - tmpfs tmpfs rw,size=3940128k\n" +
        "453 450 0:72 /stage/coordinator /coordinator ro,nosuid,nodev - tmpfs tmpfs rw,size=3940128k\n" +
        "454 450 0:89 / /proc ro,nosuid,nodev,noexec,relatime - proc proc rw\n" +
        "474 450 0:90 / /dev/shm rw,nosuid,nodev,relatime - tmpfs tmpfs rw,size=1024k,mode=755,uid=1000,gid=1000\n" +
        "504 450 0:91 / /tmp rw,nosuid,nodev,relatime - tmpfs tmpfs rw,size=7168k,mode=755,uid=1000,gid=1000\n";

    [Fact]
    public void AcceptsTheSection11CeilingsInKernelNormalizedKibibytes()
    {
        SkipUnlessLinux();

        Assert.True(CoordinatorMountVerifier.Validate(ValidMountInfo, NoHostDevices, out var error), error);
    }

    [Fact]
    public void AcceptsEquivalentByteFormOfTheExactCeilings()
    {
        SkipUnlessLinux();
        var mountInfo = ValidMountInfo
            .Replace("size=7168k", "size=7340032", StringComparison.Ordinal)
            .Replace("size=1024k", "size=1048576", StringComparison.Ordinal);

        Assert.True(CoordinatorMountVerifier.Validate(mountInfo, NoHostDevices, out var error), error);
    }

    [Theory]
    [InlineData("size=7167k")]
    [InlineData("size=7169k")]
    [InlineData("size=bad")]
    [InlineData("size=")]
    public void RejectsATmpCeilingThatIsNotExactlySection11(string size)
    {
        SkipUnlessLinux();
        var mountInfo = ValidMountInfo.Replace("size=7168k", size, StringComparison.Ordinal);

        Assert.False(CoordinatorMountVerifier.Validate(mountInfo, NoHostDevices, out var error));
        Assert.Contains(size is "size=bad" or "size=" ? "size option is missing or unsupported" : "/tmp expected 7340032 bytes",
            error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("size=1023k")]
    [InlineData("size=1025k")]
    public void RejectsAShmCeilingThatIsNotExactlySection11(string size)
    {
        SkipUnlessLinux();
        var mountInfo = ValidMountInfo.Replace("size=1024k", size, StringComparison.Ordinal);

        Assert.False(CoordinatorMountVerifier.Validate(mountInfo, NoHostDevices, out var error));
        Assert.Contains("/dev/shm expected 1048576 bytes", error, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsScratchMountBackedByAHostFilesystemDevice()
    {
        SkipUnlessLinux();
        var hostDevices = new HashSet<string>(StringComparer.Ordinal) { "0:91" };

        Assert.False(CoordinatorMountVerifier.Validate(ValidMountInfo, hostDevices, out var error));
        Assert.Contains("scratch mount identity/topology mismatch: /tmp", error, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsBindSubstitutionWithANonRootBackingPath()
    {
        SkipUnlessLinux();
        var mountInfo = ValidMountInfo.Replace("0:91 / /tmp", "0:91 /host/tmp /tmp", StringComparison.Ordinal);

        Assert.False(CoordinatorMountVerifier.Validate(mountInfo, NoHostDevices, out var error));
        Assert.Contains("scratch mount identity/topology mismatch: /tmp", error, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAScratchMountWithTheWrongFilesystemType()
    {
        SkipUnlessLinux();
        var mountInfo = ValidMountInfo.Replace("0:91 / /tmp rw,nosuid,nodev,relatime - tmpfs tmpfs",
            "8:1 / /tmp rw,nosuid,nodev,relatime - ext4 /dev/sda", StringComparison.Ordinal);

        Assert.False(CoordinatorMountVerifier.Validate(mountInfo, NoHostDevices, out var error));
        Assert.Contains("filesystem type mismatch", error, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAMissingScratchMount()
    {
        SkipUnlessLinux();
        var mountInfo = string.Join('\n', ValidMountInfo.Split('\n').Where(line => !line.Contains(" /dev/shm ", StringComparison.Ordinal)));

        Assert.False(CoordinatorMountVerifier.Validate(mountInfo, NoHostDevices, out var error));
        Assert.Contains("exact WP04 tree", error, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDuplicateConflictingMountPoints()
    {
        SkipUnlessLinux();
        var mountInfo = ValidMountInfo + "505 450 0:92 / /tmp rw,nosuid,nodev - tmpfs tmpfs rw,size=7168k\n";

        Assert.False(CoordinatorMountVerifier.Validate(mountInfo, NoHostDevices, out var error));
        Assert.Contains("duplicate mount point", error, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDuplicateMountIds()
    {
        SkipUnlessLinux();
        var duplicateId = ValidMountInfo.Replace("504 450 0:91 / /tmp", "474 450 0:91 / /tmp", StringComparison.Ordinal);

        Assert.False(CoordinatorMountVerifier.Validate(duplicateId, NoHostDevices, out var error));
        Assert.Contains("duplicate mount ID", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("shared:1")]
    [InlineData("master:1")]
    public void RejectsProhibitedPropagationMetadata(string propagation)
    {
        SkipUnlessLinux();
        var mountInfo = ValidMountInfo.Replace("rw,nosuid,nodev,relatime - tmpfs tmpfs rw,size=7168k",
            $"rw,nosuid,nodev,relatime {propagation} - tmpfs tmpfs rw,size=7168k", StringComparison.Ordinal);

        Assert.False(CoordinatorMountVerifier.Validate(mountInfo, NoHostDevices, out var error));
        Assert.Contains("identity/topology mismatch: /tmp", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("nodev")]
    [InlineData("nosuid")]
    public void RejectsMissingScratchSecurityMountOption(string requiredOption)
    {
        SkipUnlessLinux();
        var mountInfo = ValidMountInfo.Replace($"rw,nosuid,nodev,relatime - tmpfs tmpfs rw,size=7168k",
            $"rw,{(requiredOption == "nodev" ? "nosuid,relatime" : "nodev,relatime")} - tmpfs tmpfs rw,size=7168k",
            StringComparison.Ordinal);

        Assert.False(CoordinatorMountVerifier.Validate(mountInfo, NoHostDevices, out var error));
        Assert.Contains("scratch mount policy mismatch: /tmp", error, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsScratchMountWithTheWrongParentOrAliasedDevice()
    {
        SkipUnlessLinux();
        var wrongParent = ValidMountInfo.Replace("504 450 0:91", "504 451 0:91", StringComparison.Ordinal);
        Assert.False(CoordinatorMountVerifier.Validate(wrongParent, NoHostDevices, out var parentError));
        Assert.Contains("identity/topology mismatch: /tmp", parentError, StringComparison.Ordinal);

        var aliasedDevice = ValidMountInfo.Replace("504 450 0:91", "504 450 0:90", StringComparison.Ordinal);
        Assert.False(CoordinatorMountVerifier.Validate(aliasedDevice, NoHostDevices, out var aliasError));
        Assert.Contains("identity/topology mismatch: /tmp", aliasError, StringComparison.Ordinal);
    }

    [Fact]
    public void RequiresCoordinatorNamespaceIdentityToDifferFromTheHost()
    {
        SkipUnlessLinux();
        var host = new[] { "user", "mnt", "pid", "net", "ipc", "uts" }
            .Select(ReadNamespaceInode).ToArray();
        var coordinator = host.Select(value => value + 1).ToArray();

        Assert.True(CoordinatorMountVerifier.HasPrivateNamespaces(coordinator, out var goodError), goodError);
        coordinator[1] = host[1];

        Assert.False(CoordinatorMountVerifier.HasPrivateNamespaces(coordinator, out var error));
        Assert.Contains("private mnt namespace", error, StringComparison.Ordinal);
    }

    private static ulong ReadNamespaceInode(string name)
    {
        var target = new FileInfo($"/proc/self/ns/{name}").LinkTarget;
        Assert.NotNull(target);
        Assert.EndsWith("]", target, StringComparison.Ordinal);
        var open = target!.LastIndexOf('[');
        Assert.True(open > 0);
        Assert.True(ulong.TryParse(target.AsSpan(open + 1, target.Length - open - 2), out var inode));
        return inode;
    }

    private static void SkipUnlessLinux()
    {
        if (!OperatingSystem.IsLinux()) Assert.Skip("requires Linux mount namespace semantics");
    }
}
