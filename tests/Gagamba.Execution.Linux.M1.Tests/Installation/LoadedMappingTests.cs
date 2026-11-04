using Gagamba.Execution.Linux.M1.Installed;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installation;

public sealed class LoadedMappingTests
{
    [Theory]
    [InlineData("1000-2000 r-xp 0000 08:01 123 /usr/lib/dotnet/libcoreclr.so",true)]
    [InlineData("1000-2000 r-xp 0000 08:01 123 /unexpected/injected.so",true)]
    [InlineData("1000-2000 r--p 0000 08:01 123 /role/Owner.dll",false)]
    [InlineData("1000-2000 rw-s 0000 00:01 123 /dev/shm/lttng-ust-wait",false)]
    [InlineData("1000-2000 rwxp 0000 00:01 123 /memfd:doublemapper (deleted)",false)]
    [InlineData("1000-2000 r-xp 0000 00:00 0 [vdso]",false)]
    public void NativeFileInventoryDistinguishesGeneratedMemoryFromPersistedExecutableFiles(string line,bool file)
    {
        Assert.Equal(file,InstalledFileSystem.ExecutableFileMapping(line,out _));
    }
    [Fact] public void NativeRuntimeHasMappedManagedFilesAndSeparateGeneratedMappings()
    {
        if(!OperatingSystem.IsLinux()) Assert.Skip("actual Linux CLR mapping observations");
        var location=typeof(object).Assembly.Location;
        Assert.False(string.IsNullOrEmpty(location));
        Assert.Contains(File.ReadAllLines("/proc/self/maps"),line=>line.EndsWith(" "+location,StringComparison.Ordinal));
        var paths=InstalledFileSystem.MappedPaths().ToArray();
        Assert.NotEmpty(paths);Assert.DoesNotContain(paths,p=>p.StartsWith("/memfd:",StringComparison.Ordinal));
        Assert.DoesNotContain(paths,p=>p.StartsWith("/dev/shm/",StringComparison.Ordinal));
    }
}
