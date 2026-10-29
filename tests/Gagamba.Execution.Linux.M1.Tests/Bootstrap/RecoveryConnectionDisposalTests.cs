// M-01 focused regression: Inspect/ObserveAsync/Send must not use a disposed
// connection, and concurrent disposal must never let a closed or reused
// descriptor number reach IPC. The gate tests are pure managed and run on
// every platform; the real-connection tests require a Linux socketpair.
using System.Diagnostics;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Native;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

public sealed class RecoveryConnectionDisposalTests
{
    [Fact]
    public void DisposedGateNeverInvokesAUseCallbackAgain()
    {
        var uses = 0;
        var closes = 0;
        var gate = new GuardedDescriptor(41, _ => closes++);
        Assert.Equal(41, gate.Descriptor);
        Assert.True(gate.TryUse(_ => { uses++; return true; }));
        gate.Dispose();
        Assert.True(gate.IsClosed);
        Assert.Equal(-1, gate.Descriptor);
        Assert.False(gate.TryUse(_ => { uses++; return true; }));
        Assert.False(gate.TryUse(_ => { uses++; return true; }));
        Assert.Equal(1, uses);
        Assert.Equal(1, closes);
    }

    [Fact]
    public async Task ConcurrentDisposalClosesExactlyOnceAndTerminalStateIsStable()
    {
        var closes = 0;
        var gate = new GuardedDescriptor(7, _ => Interlocked.Increment(ref closes));
        var start = new Barrier(9);
        var tasks = new List<Task>();
        for (var worker = 0; worker < 8; worker++)
        {
            tasks.Add(Task.Run(() =>
            {
                start.SignalAndWait();
                for (var iteration = 0; iteration < 1000; iteration++)
                {
                    _ = gate.TryUse(_ => true);
                }
            }, TestContext.Current.CancellationToken));
        }

        tasks.Add(Task.Run(() =>
        {
            start.SignalAndWait();
            gate.Dispose();
        }, TestContext.Current.CancellationToken));
        await Task.WhenAll(tasks);
        gate.Dispose();
        Assert.Equal(1, Volatile.Read(ref closes));
        Assert.True(gate.IsClosed);
        Assert.False(gate.TryUse(_ => throw new InvalidOperationException("disposed gate executed a use callback")));
    }

    [Fact]
    public async Task DisposalWaitsForAnInFlightUseAndNoUseRunsAfterTheClose()
    {
        var useEntered = new ManualResetEventSlim(false);
        var releaseUse = new ManualResetEventSlim(false);
        var closeObservedAfterUse = false;
        var closes = 0;
        var gate = new GuardedDescriptor(3, _ =>
        {
            closeObservedAfterUse = releaseUse.IsSet;
            Interlocked.Increment(ref closes);
        });
        var using_ = Task.Run(() => gate.TryUse(_ =>
        {
            useEntered.Set();
            Assert.True(releaseUse.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            return true;
        }), TestContext.Current.CancellationToken);
        Assert.True(useEntered.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        var disposing = Task.Run(gate.Dispose, TestContext.Current.CancellationToken);
        Assert.NotSame(disposing, await Task.WhenAny(disposing, Task.Delay(200, TestContext.Current.CancellationToken)));
        releaseUse.Set();
        Assert.True(await using_);
        await disposing;
        Assert.Equal(1, closes);
        Assert.True(closeObservedAfterUse, "the close must run only after the in-flight use finished");
    }

    [Fact]
    public void ReentrantGateUseFromTheSameThreadIsSerializedNotDeadlocked()
    {
        var gate = new GuardedDescriptor(11, _ => { });
        var inner = false;
        Assert.True(gate.TryUse(_ => { inner = gate.TryUse(_ => true); return true; }));
        Assert.True(inner);
        gate.Dispose();
        Assert.False(gate.TryUse(_ => true));
    }

    [Fact]
    public async Task DisposedConnectionRefusesSendInspectAndObserveWithoutTouchingTheDescriptor()
    {
        SkipUnlessLinux();
        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var parent, out var child, out var error), error);
        using var peer = new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)child, true);
        using var process = Process.GetCurrentProcess();
        var connection = new RecoveryConnection(process, new RecoveryWire(Guid.NewGuid(), Guid.NewGuid()), parent);
        var descriptor = connection.Control;
        Assert.True(descriptor >= 0);
        connection.Dispose();
        connection.Dispose();
        Assert.Equal(-1, connection.Control);
        Assert.False(connection.Send(RecoveryMessage.Stop));
        Assert.Null(connection.Inspect());
        Assert.Null(await connection.ObserveAsync(TestContext.Current.CancellationToken));
        // Cancellation ordering is preserved even after disposal.
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connection.ObserveAsync(cancelled.Token));
    }

    [Fact]
    public async Task DisposalDuringAnInFlightInspectIsSafeAndInspectReportsLoss()
    {
        SkipUnlessLinux();
        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var parent, out var child, out var error), error);
        using var peer = new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)child, true);
        using var process = Process.GetCurrentProcess();
        var connection = new RecoveryConnection(process, new RecoveryWire(Guid.NewGuid(), Guid.NewGuid()), parent);
        var inspecting = Task.Run(connection.Inspect, TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        var disposing = Task.Run(connection.Dispose, TestContext.Current.CancellationToken);
        // The silent peer makes Inspect wait out its bounded 1000 ms receive;
        // disposal must not close the descriptor underneath it.
        Assert.Null(await inspecting);
        await disposing;
        Assert.False(connection.Send(RecoveryMessage.Inspect));
        Assert.Null(connection.Inspect());
    }

    [Fact]
    public async Task RepeatedConcurrentInspectSendAndDisposeNeverThrowsOrDeadlocks()
    {
        SkipUnlessLinux();
        for (var iteration = 0; iteration < 5; iteration++)
        {
            Assert.True(FdPassing.TryCreateInheritableSocketPair(out var parent, out var child, out var error), error);
            using var peer = new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)child, true);
            using var process = Process.GetCurrentProcess();
            var connection = new RecoveryConnection(process, new RecoveryWire(Guid.NewGuid(), Guid.NewGuid()), parent);
            var start = new Barrier(3);
            var inspect = Task.Run(() =>
            {
                start.SignalAndWait();
                _ = connection.Inspect();
            }, TestContext.Current.CancellationToken);
            var send = Task.Run(() =>
            {
                start.SignalAndWait();
                _ = connection.Send(RecoveryMessage.Stop);
            }, TestContext.Current.CancellationToken);
            var dispose = Task.Run(() =>
            {
                start.SignalAndWait();
                connection.Dispose();
            }, TestContext.Current.CancellationToken);
            await Task.WhenAll(inspect, send, dispose);
        }
    }

    private static void SkipUnlessLinux()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("M-01 real-connection tests require an unprivileged Linux socketpair");
        }
    }
}
