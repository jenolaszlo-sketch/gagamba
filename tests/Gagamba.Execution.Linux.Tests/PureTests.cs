// Pure unit tests: argument splitter and environment validation need no
// OS and no cgroup delegation. Run on every CI OS.
using Gagamba.Execution.Linux;
using Xunit;

namespace Gagamba.Execution.Linux.Tests;

public sealed class PureTests
{
    [Theory]
    [InlineData("", new string[] { })]
    [InlineData("--no-restore --nologo", new[] { "--no-restore", "--nologo" })]
    [InlineData("-c \"echo hi > f.txt\"", new[] { "-c", "echo hi > f.txt" })]
    [InlineData("--dir 'a b' --x", new[] { "--dir", "a b", "--x" })]
    [InlineData("a\\ b c", new[] { "a b", "c" })]
    [InlineData("  spaced   out  ", new[] { "spaced", "out" })]
    public void SplitArgumentsBehaves(string commandLine, string[] expected)
    {
        Assert.Equal(expected, NativeMethods.SplitArguments(commandLine));
    }

    [Fact]
    public void EnvValidationRejectsBadShapes()
    {
        Assert.False(LinuxExecutionProvider.TryBuildEnvironment(
            new Dictionary<string, string> { ["A=B"] = "x" }, out _, out string e1));
        Assert.Contains("name", e1);
        Assert.False(LinuxExecutionProvider.TryBuildEnvironment(
            new Dictionary<string, string> { ["A"] = "x\0" }, out _, out string e2));
        Assert.Contains("NUL", e2);
        Assert.True(LinuxExecutionProvider.TryBuildEnvironment(
            new Dictionary<string, string> { ["Path"] = "a", ["PATH"] = "b" },
            out var envp, out string e3), e3);
        Assert.Contains("Path=a", envp);
        Assert.Contains("PATH=b", envp);
    }

    [Fact]
    public void EnvEncodingIsNullTerminatedArray()
    {
        Assert.True(LinuxExecutionProvider.TryBuildEnvironment(
            new Dictionary<string, string> { ["B"] = "2", ["A"] = "1" },
            out string?[] envp, out _));
        Assert.NotNull(envp);
        Assert.Null(envp[^1]);
        Assert.Equal("A=1", envp[0]);
        Assert.Equal("B=2", envp[1]);
    }
}
