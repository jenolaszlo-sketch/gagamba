using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Linux.M1.Restricted;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class RetainedDirectoryCleanupTests
{
    [Theory][InlineData("original")][InlineData("directory")][InlineData("symlink")]
    public void DirectoryRemovalRequiresTheNameToStillIdentifyTheRetainedObject(string replacement)
    {
        if (!OperatingSystem.IsLinux()) Assert.Skip("Linux retained directory handles required");
        var root=Path.Combine(Path.GetTempPath(),"gagamba-b4-clean-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);var stage=Path.Combine(root,"stage");Directory.CreateDirectory(stage);
        try
        {
            Assert.Equal(0,PreparationInterop.OpenDirectory(root,out var parentFd));using var parent=new SafeFileHandle((IntPtr)parentFd,true);
            Assert.Equal(0,PreparationInterop.OpenDirectory(stage,out var stageFd));using var held=new SafeFileHandle((IntPtr)stageFd,true);
            Assert.Equal(0,PreparationInterop.StatFd(stageFd,out var identity));
            if (replacement!="original")
            {
                Directory.Move(stage,Path.Combine(root,"original"));
                if (replacement=="directory") Directory.CreateDirectory(stage);
                else Directory.CreateSymbolicLink(stage,"original");
            }
            var cleaner=new B3ScopeCleaner(parentFd,stageFd);
            var result=cleaner.CleanOne(new(AllocationKind.StagingDirectory,"stage",0,[],identity.Device,identity.Inode,true));
            if (replacement=="original") { Assert.Equal(AllocationCleanOutcome.Cleaned,result.Outcome);Assert.False(Directory.Exists(stage)); }
            else { Assert.Equal(AllocationCleanOutcome.Mismatch,result.Outcome);Assert.True(Directory.Exists(stage));Assert.True(Directory.Exists(Path.Combine(root,"original"))); }
        }
        finally { Directory.Delete(root,true); }
    }
}
