// WP03 complete domain-identity corroboration tests (G2-09): changing any
// single security-relevant field must make Corroborates false.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class DomainIdentityTests
{
    private static DomainIdentity Base() => RecoveryTestSupport.Identity();

    [Fact]
    public void IdenticalIdentitiesCorroborate()
    {
        Assert.True(Base().Corroborates(Base()));
    }

    [Fact]
    public void EveryFieldDifferenceRefuses()
    {
        var variants = new[]
        {
            Base() with { ExecutionId = Guid.NewGuid() },
            Base() with { BootId = "other" },
            Base() with { CgroupMountId = 999 },
            Base() with { CgroupMountFsType = "tmpfs" },
            Base() with { ParentDevice = 0xDEAD },
            Base() with { ParentInode = 999 },
            Base() with { ChildName = "other" },
            Base() with { ChildDevice = 0xDEAD },
            Base() with { ChildInode = 999 },
            Base() with { LeaseGeneration = Guid.NewGuid() },
        };

        foreach (var variant in variants)
        {
            Assert.False(Base().Corroborates(variant), $"variant unexpectedly corroborated: {variant}");
        }
    }

    [Fact]
    public void ChildDeviceOrInodeChangeIsDetected()
    {
        // Astra's reproduced G2-09 case.
        Assert.False(Base().Corroborates(Base() with { ChildDevice = 0xBEEF }));
        Assert.False(Base().Corroborates(Base() with { ChildInode = 0xBEEF }));
    }

    [Fact]
    public void SameNameButReplacedDetectsInodeSwap()
    {
        Assert.True(Base().SameNameButReplaced(Base() with { ChildInode = 999 }));
        Assert.False(Base().SameNameButReplaced(Base()));
    }
}
