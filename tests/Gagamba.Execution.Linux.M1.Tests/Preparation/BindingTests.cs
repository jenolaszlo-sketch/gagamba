// WP02 portable binding tests: read-only reopen identity, replacement
// detection, mode/link/size/digest verification and handle ownership.
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Preparation.CopyTestSupport;

namespace Gagamba.Execution.Linux.M1.Tests.Preparation;

public sealed class BindingTests
{
    [Fact]
    public void ReopenIdentityMatchesCreatedObject()
    {
        var host = NewHost();
        var content = Bytes(0x33, 5000);
        host.AddSourceFile("tool", content);
        var result = SafeCopyIn.Prepare(Plan(host, Tool("tool", ToolBudget, Digest(content))), host, CancellationToken.None);
        Assert.True(result.Succeeded);
        var staged = result.Closure!.Objects[0];
        Assert.Equal((uint)(PreparationInterop.S_IFREG | PreparationInterop.Mode0500), staged.Mode);
        Assert.Equal((ulong)content.Length, staged.Length);
        Assert.Equal("tool-000.staged", staged.StagingName);
        Assert.Equal(0, host.StatHandle(staged.Handle, out var identity));
        Assert.Equal(1UL, identity.LinkCount);
        Assert.Equal(staged.Device, identity.Device);
        Assert.Equal(staged.Inode, identity.Inode);
        result.Closure.Dispose();
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void ReplacementAtReopenRefused()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0x44, 512));
        var plan = Plan(host, Tool()) with
        {
            Hooks = new CopyTestHooks(null, name => host.ReplaceStaged(name, Bytes(0x55, 512))),
        };
        var result = SafeCopyIn.Prepare(plan, host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.ObjectReplaced, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.StagingFileNames());
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void SameObjectContentMutationRefused()
    {
        var host = NewHost();
        host.AddSourceFile("input", Bytes(0x66, 300));
        var plan = Plan(host, Input()) with
        {
            Hooks = new CopyTestHooks(null, name => host.MutateStaged(name, Bytes(0x77, 300))),
        };
        var result = SafeCopyIn.Prepare(plan, host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.ObjectReplaced, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void TruncatedReopenRefusedOnSize()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0x88, 1024));
        var plan = Plan(host, Tool()) with
        {
            Hooks = new CopyTestHooks(null, name => host.MutateStaged(name, Bytes(0x88, 100))),
        };
        var result = SafeCopyIn.Prepare(plan, host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.ObjectReplaced, result.Failure);
        Assert.Null(result.Closure);
    }

    [Fact]
    public void LinkCountViolationRefused()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0x99, 128));
        var plan = Plan(host, Tool()) with
        {
            Hooks = new CopyTestHooks(null, name => host.SetStagedNlink(name, 2)),
        };
        var result = SafeCopyIn.Prepare(plan, host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.ObjectReplaced, result.Failure);
        Assert.Null(result.Closure);
    }

    [Fact]
    public void ModeChangeAtReopenRefused()
    {
        var host = NewHost();
        host.AddSourceFile("input", Bytes(0xAA, 128));
        var plan = Plan(host, Input()) with
        {
            Hooks = new CopyTestHooks(null, name => host.ChmodStaged(name, PreparationInterop.Mode0600)),
        };
        var result = SafeCopyIn.Prepare(plan, host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.ObjectReplaced, result.Failure);
        Assert.Null(result.Closure);
    }

    [Fact]
    public void DestinationExclusiveCreateEnforced()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0xBB, 64));
        host.PlantStagingCollision("tool-000.staged", Bytes(0xCC, 64));
        var result = SafeCopyIn.Prepare(Plan(host, Tool()), host, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Equal(CopyFailureKind.StagingFailure, result.Failure);
        Assert.Null(result.Closure);
        Assert.Empty(host.LiveHandles);
        Assert.Equal(new[] { "tool-000.staged" }, host.StagingFileNames());
    }

    [Fact]
    public void NoWritableHandleRemainsAtPublication()
    {
        var host = NewHost();
        host.AddSourceFile("tool", Bytes(0xDD, 256));
        host.AddSourceFile("input", Bytes(0xEE, 128));
        var result = SafeCopyIn.Prepare(Plan(host, Tool(), Input()), host, CancellationToken.None);
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Closure!.Objects.Count);
        Assert.Equal(0x140u, result.Closure.Objects[0].Mode & 0x1FFu);
        Assert.Equal(0x100u, result.Closure.Objects[1].Mode & 0x1FFu);
        Assert.Empty(host.LiveWriteAccesses());
        result.Closure.Dispose();
        Assert.Empty(host.LiveHandles);
    }

    [Fact]
    public void InputRoleFinalModeIsReadOnly()
    {
        var host = NewHost();
        host.AddSourceFile("input", Bytes(0x12, 64));
        var result = SafeCopyIn.Prepare(Plan(host, Input()), host, CancellationToken.None);
        Assert.True(result.Succeeded);
        Assert.Equal(PreparationInterop.Mode0400, result.Closure!.Objects[0].Mode & 0x1FFu);
        result.Closure.Dispose();
    }

    [Fact]
    public void RetainedDescriptorSurvivesPathnameRemoval()
    {
        // Binding-time reopen is the only pathname resolution after the
        // exclusive create, and it happens before binding. Afterwards all
        // authority is the retained read-only FD plus bound identity: even
        // removing the staging pathname leaves the bound object usable and
        // verifiable, and no reopen mechanism remains to reacquire by name.
        var host = NewHost();
        var content = Bytes(0x13, 1024);
        host.AddSourceFile("tool", content);
        var result = SafeCopyIn.Prepare(Plan(host, Tool("tool", ToolBudget, Digest(content))), host, CancellationToken.None);
        Assert.True(result.Succeeded);
        var staged = result.Closure!.Objects[0];
        Assert.Equal(0, host.Unlink(result.Closure.StagingDirectory!, staged.StagingName));
        Assert.False(host.StagingFileExists(staged.StagingName));
        Assert.Equal(0, host.StatHandle(staged.Handle, out var identity));
        Assert.Equal(staged.Device, identity.Device);
        Assert.Equal(staged.Inode, identity.Inode);
        using var reader = host.OpenRead(staged.Handle);
        var reread = new byte[content.Length];
        var total = 0;
        int read;
        while ((read = reader.Read(reread, total, reread.Length - total)) > 0)
        {
            total += read;
        }

        Assert.Equal(content.Length, total);
        Assert.Equal(content, reread);
        result.Closure.Dispose();
        Assert.Empty(host.LiveHandles);
    }
}
