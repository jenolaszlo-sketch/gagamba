// Pure unit tests: argument splitter, environment validation, and print
// state parsing need no macOS and no launchd. Run on every CI OS.
using Gagamba.Execution.MacOS;
using Xunit;

namespace Gagamba.Execution.MacOS.Tests;

public sealed class PureTests
{
    [Theory]
    [InlineData("", new string[] { })]
    [InlineData("--no-restore --nologo", new[] { "--no-restore", "--nologo" })]
    [InlineData("-c \"echo hi > f.txt\"", new[] { "-c", "echo hi > f.txt" })]
    [InlineData("--dir 'a b' --x", new[] { "--dir", "a b", "--x" })]
    [InlineData("  spaced   out  ", new[] { "spaced", "out" })]
    public void SplitArgumentsBehaves(string commandLine, string[] expected)
    {
        Assert.Equal(expected, Launchd.SplitArguments(commandLine));
    }

    [Fact]
    public void EnvValidationRejectsBadShapes()
    {
        Assert.False(MacOsExecutionProvider.TryBuildEnvironment(
            new Dictionary<string, string> { ["A=B"] = "x" }, out _, out string e1));
        Assert.Contains("=", e1);
        Assert.False(MacOsExecutionProvider.TryBuildEnvironment(
            new Dictionary<string, string> { [""] = "x" }, out _, out string e2));
        Assert.Contains("empty", e2);
        Assert.False(MacOsExecutionProvider.TryBuildEnvironment(
            new Dictionary<string, string> { ["A"] = "x\0" }, out _, out string e3));
        Assert.Contains("NUL", e3);
    }

    [Fact]
    public void EnvNamesAreCaseSensitiveLikeUnix()
    {
        // PATH and Path coexist on macOS (unlike Windows): Ordinal compare.
        Assert.True(MacOsExecutionProvider.TryBuildEnvironment(
            new Dictionary<string, string> { ["Path"] = "a", ["PATH"] = "b" },
            out var env, out _));
        Assert.NotNull(env);
        Assert.Equal(2, env!.Count);
    }

    [Fact]
    public void IsRunningReadsStateOnly()
    {
        Assert.True(Launchd.IsRunning("pid = 1891\nstate = running\n"));
        Assert.True(Launchd.IsRunning("  state = running  \n  pid = 7\n"));
        Assert.False(Launchd.IsRunning("state = exited\n"));
        Assert.False(Launchd.IsRunning("pid = 1891\n"));
        Assert.False(Launchd.IsRunning(""));
    }

    [Fact]
    public void NotRunningIsNotRunning()
    {
        // Consumer-pressure regression (Hufu SandboxRunner, 2026-10-07):
        // launchd reports instantly-exited on-demand jobs as
        // `state = not running` with a recorded code. Substring matching
        // misread that as running: Launch "succeeded" on a never-observed
        // job and completion polled forever.
        Assert.False(Launchd.IsRunning("state = not running\n"));
        Assert.False(Launchd.IsRunning("pid = 0\nstate = not running\nlast exit code = 0\n"));
        var terminal = Launchd.ParseState("state = not running\nlast exit code = 0\n", 0);
        Assert.False(terminal.Running);
        Assert.Equal(0, terminal.ExitCode);
        var gone = Launchd.ParseState("Could not find service \"org.example\" in domain", 113);
        Assert.False(gone.Running);
        Assert.Null(gone.ExitCode);
    }
}
