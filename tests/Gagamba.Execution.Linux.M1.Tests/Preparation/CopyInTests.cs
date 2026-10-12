// WP02 portable copy tests: budgets, cancellation, injected I/O faults,
// drift detection and handle hygiene, all through the fake host.
using Gagamba.Execution.Linux.M1.Preparation;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Preparation.CopyTestSupport;

namespace Gagamba.Execution.Linux.M1.Tests.Preparation;

public sealed class CopyInTests
{
    [Fact]
    public void ExactBudgetLimitSucceeds()
    {
        var host = NewHost();
        var content = Bytes(0x10, 4096);
        host.AddSourceFile("tool", content);
        var plan = Plan(host, Tool("tool", (ulong)content.Length, Digest(content)));
        var result = SafeCopyIn.Prepare(plan, host, CancellationToken.None);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Closure);
        Assert.Equal(content, host.ReadStaged(result.Closure!.Objects[0].StagingName));
        Assert.Equal(Digest(content), result.Closure.Objects[0].Sha256);
        Assert.Equal((ulong)content.Length, result.Closure.Objects[0].Length);
        result.Closure.Dispose();
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void LimitPlusOneRefusesAndCleansPartial()
    {
        var host = NewHost();
        var content = Bytes(0x20, 4096);
        host.AddSourceFile("tool", content);
        var plan = Plan(host, Tool("tool", (ulong)content.Length - 1));
        var result = SafeCopyIn.Prepare(plan, host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.BoundsExceeded, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.StagingFileNames());
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void TotalStagedContentBudgetEnforced()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0x30, 200000));
        host.AddSourceFile("input", Bytes(0x40, 200000));
        var plan = Plan(host,
            Tool("tool", ToolBudget),
            Input("input", InputBudget)) with { TotalBudgetBytes = 300000 };
        var result = SafeCopyIn.Prepare(plan, host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.BoundsExceeded, result.Failure);
        // The first object completed before the total budget ran out, so it
        // is accounted and retained for explicit cleanup — never silently
        // deleted, and no capability minted.
        Assert.NotNull(result.Closure);
        Assert.Single(result.Closure!.Objects);
        Assert.Equal((ulong)200000, result.Closure.Objects[0].Length);
        result.Closure.DiscardAccounted(host);
        Assert.Empty(host.StagingFileNames());
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void ObjectCountBudgetEnforced()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0x50, 64));
        var plan = Plan(host, Tool(), Tool("tool", ToolBudget)) with { MaxObjects = 1 };
        var result = SafeCopyIn.Prepare(plan, host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.BoundsExceeded, result.Failure);
        Assert.Null(result.Closure);

        var empty = Plan(host) with { MaxObjects = 128 };
        var emptyResult = SafeCopyIn.Prepare(empty, host, CancellationToken.None);
        Assert.False(emptyResult.Succeeded);
        Assert.Equal(CopyFailureKind.BoundsExceeded, emptyResult.Failure);
    }

    [Fact]
    public void EnvironmentBoundEnforcedAndSnapshotted()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0x60, 64));
        var big = new string('E', 16384);
        var over = Plan(host, Tool()) with
        {
            Environment = new[] { new KeyValuePair<string, string>("K", big) },
        };
        var overResult = SafeCopyIn.Prepare(over, host, CancellationToken.None);
        Assert.False(overResult.Succeeded);
        Assert.Equal(CopyFailureKind.BoundsExceeded, overResult.Failure);

        var nul = Plan(host, Tool()) with
        {
            Environment = new[] { new KeyValuePair<string, string>("K\0", "V") },
        };
        Assert.Equal(CopyFailureKind.InvalidSource, SafeCopyIn.Prepare(nul, host, CancellationToken.None).Failure);

        var env = new List<KeyValuePair<string, string>> { new("HOME", "/tmp") };
        var ok = Plan(host, Tool()) with { Environment = env };
        host.AddSourceFile("tool", Bytes(0x60, 64));
        var okResult = SafeCopyIn.Prepare(ok, host, CancellationToken.None);
        Assert.True(okResult.Succeeded);
        env.Add(new KeyValuePair<string, string>("MUTATED", "late"));
        Assert.Single(okResult.Closure!.Environment);
        okResult.Closure.Dispose();
    }

    [Fact]
    public void CancellationBeforePublicationAllocatesNothing()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0x70, 64));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = SafeCopyIn.Prepare(Plan(host, Tool()), host, cts.Token);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.Cancelled, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.StagingFileNames());
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void CancellationMidCopyCleansPartial()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0x80, 200000));
        using var cts = new CancellationTokenSource();
        var plan = Plan(host, Tool()) with
        {
            Hooks = new CopyTestHooks(
                _ =>
                {
                    cts.Cancel();
                },
                null),
        };
        var result = SafeCopyIn.Prepare(plan, host, cts.Token);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.Cancelled, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.StagingFileNames());
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void ReadFailureIsInvalidSource()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0x90, 128));
        host.FailReadsAfterChunks = 0;
        var result = SafeCopyIn.Prepare(Plan(host, Tool()), host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.InvalidSource, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.StagingFileNames());
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void WriteFailureIsStagingFailure()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0xA0, 128));
        host.FailWritesAfterChunks = 0;
        var result = SafeCopyIn.Prepare(Plan(host, Tool()), host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.StagingFailure, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.StagingFileNames());
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void ClaimedDigestMismatchRefused()
    {
        var host = NewHost();
        var content = Bytes(0xB0, 256);
        host.AddSourceFile("tool", content);
        var wrong = Digest(Bytes(0xC0, 256));
        var result = SafeCopyIn.Prepare(Plan(host, Tool("tool", ToolBudget, wrong)), host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.DigestMismatch, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.StagingFileNames());
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void BoundMetadataImmuneToSourceMutation()
    {
        var host = NewHost();
        var content = Bytes(0xD0, 256);
        var claimed = Digest(content);
        var claimedCopy = (byte[])claimed.Clone();
        host.AddSourceFile("tool", content);
        var result = SafeCopyIn.Prepare(Plan(host, Tool("tool", ToolBudget, claimedCopy)), host, CancellationToken.None);
        Assert.True(result.Succeeded);
        for (var i = 0; i < claimedCopy.Length; i++)
        {
            claimedCopy[i] = 0xFF;
        }

        host.AppendSource("tool", new byte[] { 0x00 });
        var staged = result.Closure!.Objects[0];
        Assert.Equal(claimed, staged.Sha256);
        Assert.Equal((ulong)256, staged.Length);
        result.Closure.Dispose();
    }

    [Fact]
    public void UnknownRoleAndBadNamesRefused()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0xE0, 32));
        var role = SafeCopyIn.Prepare(Plan(host, new CopyObjectPlan("weird", "tool", ToolBudget, null)), host, CancellationToken.None);
        Assert.Equal(CopyFailureKind.InvalidSource, role.Failure);
        foreach (var bad in new[] { "..", ".", "", "a/b", "/abs", "nul\0byte" })
        {
            var attempt = SafeCopyIn.Prepare(Plan(host, new CopyObjectPlan("tool", bad, ToolBudget, null)), host, CancellationToken.None);
            Assert.Equal(CopyFailureKind.InvalidSource, attempt.Failure);
        }

        var tooLong = new string('n', 256);
        Assert.Equal(
            CopyFailureKind.InvalidSource,
            SafeCopyIn.Prepare(Plan(host, new CopyObjectPlan("tool", tooLong, ToolBudget, null)), host, CancellationToken.None).Failure);
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void SourceGrowthDuringCopyRefused()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0xF0, 100000));
        var grown = false;
        var plan = Plan(host, Tool()) with
        {
            Hooks = new CopyTestHooks(
                _ =>
                {
                    if (!grown)
                    {
                        grown = true;
                        host.AppendSource("tool", Bytes(0x01, 1000));
                    }
                },
                null),
        };
        var result = SafeCopyIn.Prepare(plan, host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.ObjectReplaced, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.StagingFileNames());
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void SourceTruncationDuringCopyRefused()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0x0F, 100000));
        var plan = Plan(host, Tool()) with
        {
            Hooks = new CopyTestHooks(_ => host.TruncateSource("tool", 1000), null),
        };
        var result = SafeCopyIn.Prepare(plan, host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.ObjectReplaced, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void NonRegularAndMissingSourcesRefused()
    {
        var host = NewHost();
        host.AddSourceDirectory("subdir");
        var dir = SafeCopyIn.Prepare(
            Plan(host, new CopyObjectPlan("tool", "subdir", ToolBudget, null)), host, CancellationToken.None);
        Assert.Equal(CopyFailureKind.InvalidSource, dir.Failure);

        host.AddSourceFile("tool", Bytes(0x11, 16));
        var missingPlan = Plan(host, Input("absent")) with { StagingDirectoryName = "prep-002" };
        var missing = SafeCopyIn.Prepare(missingPlan, host, CancellationToken.None);
        Assert.Equal(CopyFailureKind.InvalidSource, missing.Failure);
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void SymlinkSourceRefused()
    {
        var host = NewHost();
        host.AddSourceSymlink("tool");
        var result = SafeCopyIn.Prepare(Plan(host, Tool()), host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.SymlinkRefused, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void SparseSourceRefused()
    {
        var host = NewHost();
        host.AddSparseSourceFile("tool", Bytes(0x23, 4096));
        var result = SafeCopyIn.Prepare(Plan(host, Tool()), host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.SparseRefused, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.StagingFileNames());
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void SparseBelowBudgetStillRefused()
    {
        var host = NewHost();
        host.AddSparseSourceFile("input", Bytes(0x24, 1024));
        var result = SafeCopyIn.Prepare(Plan(host, Input()), host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.SparseRefused, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.StagingFileNames());
    }

    [Fact]
    public void UnreliableBlockAccountingRefused()
    {
        var host = NewHost();
        host.AddUnreliableSourceFile("tool", Bytes(0x25, 512));
        var result = SafeCopyIn.Prepare(Plan(host, Tool()), host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.SparseRefused, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.StagingFileNames());
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void UnsupportedPrimitivePropagates()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0x22, 16));
        host.OpenSourceErrno = _ => 38;
        var result = SafeCopyIn.Prepare(Plan(host, Tool()), host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.Unsupported, result.Failure);
        Assert.Null(result.Closure);
    }
}
