using System.Text;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Restricted;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Launch;

public sealed class ProtectedCaptureTests
{
    private sealed class Pipes : IDisposable
    {
        internal readonly SafeFileHandle OutRead, OutWrite, ErrRead, ErrWrite;
        internal Pipes()
        {
            if (!OperatingSystem.IsLinux()) Assert.Skip("Linux capture pipes required");
            Assert.True(NativeLaunchInterop.TryCreatePipe(out var r, out var w, out _)); OutRead=r!; OutWrite=w!;
            Assert.True(NativeLaunchInterop.TryCreatePipe(out r, out w, out _)); ErrRead=r!; ErrWrite=w!;
        }
        internal void Write(bool stdout, string text)
        {
            var bytes=Encoding.UTF8.GetBytes(text);
            Assert.Equal(bytes.Length,NativeLaunchInterop.WriteBytesOnce((stdout?OutWrite:ErrWrite).DangerousGetHandle().ToInt32(),bytes));
        }
        public void Dispose() { OutWrite.Dispose();ErrWrite.Dispose();OutRead.Dispose();ErrRead.Dispose(); }
    }
    [Fact]
    public void ExactBudgetsRetainBothStreamsBeforeAnyObserverAndReturnCopies()
    {
        using var pipes=new Pipes(); var stops=0;
        using var capture=new ProtectedCapture(pipes.OutRead,pipes.ErrRead,new(true,8,8,16),OwnerSafetyMonitor.Now()+5_000_000_000L,_=>Interlocked.Increment(ref stops));
        pipes.Write(true,"12345678");pipes.Write(false,"abcdefgh");
        Assert.True(SpinWait.SpinUntil(()=>capture.Snapshot().Stdout.Seen==8 && capture.Snapshot().Stderr.Seen==8,1000));
        pipes.OutWrite.Dispose();pipes.ErrWrite.Dispose();var result=capture.Finish();
        Assert.True(result.Complete);Assert.Equal(0,stops);
        Assert.Equal("12345678",Encoding.UTF8.GetString(capture.Read(true,0,8)));
        var copy=capture.Read(false,0,8);copy[0]=0;
        Assert.Equal("abcdefgh",Encoding.UTF8.GetString(capture.Read(false,0,8)));
        Assert.Empty(capture.Read(true,8,1));Assert.Empty(capture.Read(true,ulong.MinValue,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>capture.Read(true,ulong.MaxValue,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>capture.Read(true,0,16385));
    }
    [Theory][InlineData(true)][InlineData(false)]
    public void FirstExcessByteStopsWithWritersStillOpenAndNoReader(bool stdout)
    {
        using var pipes=new Pipes(); using var stopped=new ManualResetEventSlim(); StopCauseKind? cause=null;var stops=0;
        using var capture=new ProtectedCapture(pipes.OutRead,pipes.ErrRead,new(true,8,8,16),OwnerSafetyMonitor.Now()+5_000_000_000L,c=>{cause=c;Interlocked.Increment(ref stops);stopped.Set();});
        pipes.Write(stdout,"123456789");Assert.True(stopped.Wait(1000,TestContext.Current.CancellationToken));
        Assert.Equal(StopCauseKind.OutputOverflow,cause);Assert.False(capture.Healthy);
        var result=capture.Snapshot();var stream=stdout?result.Stdout:result.Stderr;
        Assert.Equal(8,stream.Bytes.Length);Assert.Equal(9UL,stream.Seen);Assert.True(stream.Overflow);Assert.False(stream.Eof);
        pipes.Write(stdout,"more");pipes.OutWrite.Dispose();pipes.ErrWrite.Dispose();capture.Finish();Assert.Equal(1,stops);
    }
    [Fact]
    public void SharedAggregateReservationCannotOverallocate()
    {
        using var pipes=new Pipes();using var stopped=new ManualResetEventSlim();
        using var capture=new ProtectedCapture(pipes.OutRead,pipes.ErrRead,new(true,8,8,10),OwnerSafetyMonitor.Now()+5_000_000_000L,_=>stopped.Set());
        Parallel.Invoke(new ParallelOptions { CancellationToken=TestContext.Current.CancellationToken },()=>pipes.Write(true,"123456"),()=>pipes.Write(false,"abcdef"));
        Assert.True(stopped.Wait(1000,TestContext.Current.CancellationToken));var result=capture.Snapshot();
        Assert.Equal(10,result.Stdout.Bytes.Length+result.Stderr.Bytes.Length);
        Assert.Equal(12UL,result.Stdout.Seen+result.Stderr.Seen);
        Assert.True(result.Stdout.Overflow||result.Stderr.Overflow);
    }
    [Fact]
    public void CaptureStoreFailureStopsWithoutWaitingForOtherPipe()
    {
        using var pipes=new Pipes();using var stopped=new ManualResetEventSlim();StopCauseKind? cause=null;
        using var capture=new ProtectedCapture(pipes.OutRead,pipes.ErrRead,new(true,8,8,16),OwnerSafetyMonitor.Now()+5_000_000_000L,
            c=>{cause=c;stopped.Set();},_=>throw new IOException("injected fixed-custody write failure"));
        pipes.Write(true,"one");Assert.True(stopped.Wait(1000,TestContext.Current.CancellationToken));
        Assert.Equal(StopCauseKind.OutputReadFailure,cause);Assert.True(capture.Snapshot().Stdout.Failed);Assert.False(capture.Healthy);
    }
    [Fact]
    public void NativeReadFailureStopsWithoutAStatusObserver()
    {
        using var pipes=new Pipes();using var stopped=new ManualResetEventSlim();StopCauseKind? cause=null;
        using var capture=new ProtectedCapture(pipes.OutWrite,pipes.ErrRead,new(true,8,8,16),OwnerSafetyMonitor.Now()+5_000_000_000L,c=>{cause=c;stopped.Set();});
        pipes.OutRead.Dispose();Assert.True(stopped.Wait(1000,TestContext.Current.CancellationToken));
        Assert.Equal(StopCauseKind.OutputReadFailure,cause);Assert.True(capture.Snapshot().Stdout.Failed);
    }
    [Fact]
    public void UnclosedDescendantWriterExpiresWithoutInventingEofOrExtendingDeadline()
    {
        using var pipes=new Pipes();var stops=0;
        using var capture=new ProtectedCapture(pipes.OutRead,pipes.ErrRead,new(true,8,8,16),OwnerSafetyMonitor.Now()+5_000_000_000L,_=>stops++);
        pipes.Write(true,"root");Assert.True(SpinWait.SpinUntil(()=>capture.Snapshot().Stdout.Seen==4,1000));
        capture.BoundDrain(OwnerSafetyMonitor.Now()+50_000_000L);capture.BoundDrain(OwnerSafetyMonitor.Now()+10_000_000_000L);
        var start=OwnerSafetyMonitor.Now();var result=capture.Finish();
        Assert.InRange(OwnerSafetyMonitor.Now()-start,0,500_000_000L);Assert.True(result.Expired);
        Assert.True(pipes.OutRead.IsClosed);Assert.True(pipes.ErrRead.IsClosed);
        Assert.False(result.Stdout.Eof);Assert.False(result.Complete);Assert.Equal(4UL,result.Stdout.Seen);Assert.Equal(0,stops);
    }
    [Fact]
    public void CaptureCannotAcquireRecoveryOrReleaseAuthority()
    {
        using var channel=new OwnerProtocolChannel(123456,Guid.NewGuid(),Guid.NewGuid(),false){RecoveryOnly=true};
        Assert.True(OwnerProtocolChannel.DirectControl(OwnerProtocolMessageType.CaptureRead));
        Assert.False(channel.TrySend(OwnerProtocolMessageType.CaptureRead,new byte[13],out var error));Assert.Contains("authority role",error);
        Assert.False(channel.TryReceive(OwnerProtocolMessageType.CaptureBytes,out _,out _));
        Assert.False(OwnerProtocol.HasValidControlPayloadShape(new(OwnerProtocolMessageType.CaptureRead,Guid.NewGuid(),Guid.NewGuid(),1,new byte[14])));
        Assert.False(OwnerProtocol.HasValidControlPayloadShape(new(OwnerProtocolMessageType.CaptureBytes,Guid.NewGuid(),Guid.NewGuid(),1,new byte[16386])));
    }
}
