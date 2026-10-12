// WP02 identity-checked staging cleanup tests (G2-07): a staged object is
// removed only when the current entry still is the exact object this
// preparation created; replacements survive; failures are reported rather
// than swallowed.
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using System.Runtime.InteropServices;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Preparation.CopyTestSupport;

namespace Gagamba.Execution.Linux.M1.Tests.Preparation;

public sealed class StagingCleanupTests
{
    private static (FakeCopyHost Host, StagedObject Staged, SafeHandle StagingDir) StageOne()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0x41, 1024));
        var result = SafeCopyIn.Prepare(Plan(host, Tool()), host, CancellationToken.None);
        Assert.True(result.Succeeded, result.Detail);
        var closure = result.Closure!;
        return (host, closure.Objects[0], closure.StagingDirectory!);
    }

    [Fact]
    public void OriginalEntryRemoved()
    {
        var (host, staged, dir) = StageOne();
        var result = StagingCleanup.CleanOne(host, dir, staged.StagingName, staged.Device, staged.Inode);
        Assert.Equal(CleanupItemOutcome.Removed, result.Outcome);
        Assert.Empty(host.StagingFileNames());
    }

    [Fact]
    public void AlreadyAbsentIsIdempotent()
    {
        var (host, staged, dir) = StageOne();
        Assert.Equal(CleanupItemOutcome.Removed, StagingCleanup.CleanOne(host, dir, staged.StagingName, staged.Device, staged.Inode).Outcome);
        var again = StagingCleanup.CleanOne(host, dir, staged.StagingName, staged.Device, staged.Inode);
        Assert.Equal(CleanupItemOutcome.AlreadyAbsent, again.Outcome);
    }

    [Fact]
    public void ReplacementRegularFileSurvives()
    {
        var (host, staged, dir) = StageOne();
        host.ReplaceStaged(staged.StagingName, Bytes(0x99, 512));
        var result = StagingCleanup.CleanOne(host, dir, staged.StagingName, staged.Device, staged.Inode);
        Assert.Equal(CleanupItemOutcome.IdentityMismatch, result.Outcome);
        Assert.True(host.StagingFileExists(staged.StagingName));
    }

    [Fact]
    public void ReplacementSymlinkSurvives()
    {
        var (host, staged, dir) = StageOne();
        host.ReplaceStagedWithSymlink(staged.StagingName);
        var result = StagingCleanup.CleanOne(host, dir, staged.StagingName, staged.Device, staged.Inode);
        Assert.Equal(CleanupItemOutcome.IdentityMismatch, result.Outcome);
        Assert.True(host.StagingFileExists(staged.StagingName));
    }

    [Fact]
    public void SameContentDifferentInodeSurvives()
    {
        var (host, staged, dir) = StageOne();
        // Same bytes, but a distinct filesystem object.
        host.ReplaceStaged(staged.StagingName, Bytes(0x41, 1024));
        var result = StagingCleanup.CleanOne(host, dir, staged.StagingName, staged.Device, staged.Inode);
        Assert.Equal(CleanupItemOutcome.IdentityMismatch, result.Outcome);
        Assert.True(host.StagingFileExists(staged.StagingName));
    }

    [Fact]
    public void InspectionFailureIsNotConfirmed()
    {
        var (host, staged, dir) = StageOne();
        host.OpenSourceErrno = _ => PreparationInterop.EACCES;
        var result = StagingCleanup.CleanOne(host, dir, staged.StagingName, staged.Device, staged.Inode);
        Assert.Equal(CleanupItemOutcome.InspectionFailed, result.Outcome);
        Assert.True(host.StagingFileExists(staged.StagingName));
    }

    [Fact]
    public void UnlinkFailureIsNotConfirmed()
    {
        var (host, staged, dir) = StageOne();
        host.UnlinkErrno = _ => PreparationInterop.EACCES;
        var result = StagingCleanup.CleanOne(host, dir, staged.StagingName, staged.Device, staged.Inode);
        Assert.Equal(CleanupItemOutcome.RemovalFailed, result.Outcome);
        Assert.True(host.StagingFileExists(staged.StagingName));
    }

    [Fact]
    public void RepeatedCleanupAfterMismatchNeverRemovesReplacement()
    {
        var (host, staged, dir) = StageOne();
        host.ReplaceStaged(staged.StagingName, Bytes(0x77, 128));
        for (var i = 0; i < 3; i++)
        {
            var result = StagingCleanup.CleanOne(host, dir, staged.StagingName, staged.Device, staged.Inode);
            Assert.Equal(CleanupItemOutcome.IdentityMismatch, result.Outcome);
            Assert.True(host.StagingFileExists(staged.StagingName));
        }
    }

    [Fact]
    public void ReportConfirmedOnlyWhenAllClean()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0x51, 256));
        host.AddSourceFile("input", Bytes(0x52, 256));
        var result = SafeCopyIn.Prepare(Plan(host, Tool(), Input()), host, CancellationToken.None);
        var closure = result.Closure!;
        var good = StagingCleanup.CleanAll(host, closure.StagingDirectory!,
            closure.Objects.Select(o => (o.StagingName, o.Device, o.Inode)).ToArray());
        Assert.True(good.Confirmed);

        var host2 = NewHost();
        host2.AddSourceFile("tool", Bytes(0x53, 256));
        var r2 = SafeCopyIn.Prepare(Plan(host2, Tool()), host2, CancellationToken.None);
        var c2 = r2.Closure!;
        host2.ReplaceStaged(c2.Objects[0].StagingName, Bytes(0x54, 256));
        var debt = StagingCleanup.CleanAll(host2, c2.StagingDirectory!,
            c2.Objects.Select(o => (o.StagingName, o.Device, o.Inode)).ToArray());
        Assert.False(debt.Confirmed);
        Assert.True(debt.DebtRetained);
    }
}
