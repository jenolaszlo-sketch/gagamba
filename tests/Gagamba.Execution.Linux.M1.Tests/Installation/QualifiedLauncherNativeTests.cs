using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installation;

[Trait("Category","ProcessGlobalHostState")]
[Collection("S3NativeProcessState")]
public sealed class QualifiedLauncherNativeTests
{
    [Fact] public async Task ConcurrentSpawnsInheritOnlyTheirOwnMappedEndpoint()
    {
        if(!OperatingSystem.IsLinux()) Assert.Skip("non-root Linux concurrent spawn qualification");
        if(Environment.GetEnvironmentVariable("GAGAMBA_S3_EVIDENCE_FIXTURE") is null) Assert.Skip("explicit isolated fixture required");
        await Task.WhenAll(Task.Run(SpawnMappingsOriginalPinOneShotExportAndReapingAreActualKernelOperations,TestContext.Current.CancellationToken),
            Task.Run(SpawnMappingsOriginalPinOneShotExportAndReapingAreActualKernelOperations,TestContext.Current.CancellationToken)).WaitAsync(TimeSpan.FromSeconds(30),TestContext.Current.CancellationToken);
    }
    [Fact] public void SpawnMappingsOriginalPinOneShotExportAndReapingAreActualKernelOperations()
    {
        if(!OperatingSystem.IsLinux()) Assert.Skip("non-root Linux spawn/descriptor qualification");
        string? fixture=Environment.GetEnvironmentVariable("GAGAMBA_S3_EVIDENCE_FIXTURE");
        if(fixture is null) Assert.Skip("explicit isolated fixture required");
        Assert.True(RecoveryInterop.IsUnprivileged);
        string host="/usr/lib/dotnet/dotnet",version=Environment.Version.ToString(),shared=RuntimeEnvironment.GetRuntimeDirectory().TrimEnd('/'),fxr="/usr/lib/dotnet/host/fxr/"+version;
        using var file=File.OpenRead(host);
        var runtime=new ExperimentalRuntimePin(host,Convert.ToHexString(SHA256.HashData(file)),shared,ExperimentalRuntimePin.Inventory(shared),fxr,ExperimentalRuntimePin.Inventory(fxr),version);
        using var scope=new M1RecoveryScope();Assert.True(scope.TryInitialize().Ok);
        var pair=QualifiedLauncher.Pair();using var local=new SafeFileHandle((IntPtr)pair.Parent,true);
        var transfer=QualifiedLauncher.Pair();using var tx=new SafeFileHandle((IntPtr)transfer.Parent,true);using var rx=new SafeFileHandle((IntPtr)transfer.Child,true);
        var spawn=scope.TrySpawnOwner(fixture!,["--b6-launch"],pair.Child,runtime,qualified:true);
        Assert.True(spawn.Ok,spawn.Error);var process=spawn.Process!;
        try
        {
            Assert.True(FdPassing.TrySendMessage(pair.Parent,[42],out _));
            Assert.True(FdPassing.TryReceiveMessage(pair.Parent,5000,out var ready,out var error),error);
            Assert.Equal("MAPPED",Encoding.ASCII.GetString(ready));
            Assert.True(DirectPeerIdentity.LiveReference(spawn.PidFd,B2ProcessIdentity.Identity(process.Id)));
            using var unrelated=Process.GetCurrentProcess();
            Assert.False(scope.SendOriginalB2OwnerReference(unrelated,transfer.Parent,[1]));
            Assert.True(scope.SendOriginalB2OwnerReference(process,transfer.Parent,[1]));
            Assert.False(scope.SendOriginalB2OwnerReference(process,transfer.Parent,[1]));
            Assert.True(FdPassing.TryReceiveRecord(transfer.Child,1000,out var record,out error),error);
            using(record!) {using var copied=B2ProcessIdentity.ReceivePin(record!,process.Id);Assert.True(PreparationInterop.HasCloexec(copied.DangerousGetHandle().ToInt32()));}
            Assert.True(DirectPeerIdentity.LiveReference(spawn.PidFd,B2ProcessIdentity.Identity(process.Id)));
            Assert.True(FdPassing.TrySendMessage(pair.Parent,[43],out _));
            var exit=scope.WaitForOwner(process,TimeSpan.FromSeconds(5));Assert.True(exit.Exited,exit.Error);Assert.Equal(0,exit.ExitCode);
        }
        finally {_=PidFd.TrySignal(spawn.PidFd,9,out _);}
    }
}
