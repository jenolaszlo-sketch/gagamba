using Gagamba.Execution;
using Gagamba.Execution.Windows;
using Xunit;

namespace Gagamba.Execution.Windows.Tests;

public sealed class Ar4WindowsTests
{
    [Fact]
    public void VectorSerializationPreservesEmptyQuotesBackslashesAndUnicode()
    {
        var vector = new[] { "", "plain", "two words", "a\"b", "C:\\path with space\\", "雪" };
        Assert.Equal("\"\" plain \"two words\" \"a\\\"b\" \"C:\\path with space\\\\\" 雪",
            WindowsCommandLine.Serialize(vector));
        Assert.Equal("raw --value", ProcessStartSpec.Simple("tool", "raw --value").Arguments);
    }

    [Fact]
    public async Task DiscardRequiresExactIssuerAndSpentTokenRemainsIdempotent()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Skip("Windows Job Object"); return; }
        await using var issuer = new WindowsExecutionProvider();
        await using var foreign = new WindowsExecutionProvider();
        var req = new ExecutionRequirements(Array.Empty<ExecutionRequirement>());
        var token = Assert.IsType<PrepareResult.Accepted>(issuer.Prepare(req)).Prepared;
        Assert.IsType<DiscardResult.Failed>(foreign.Discard(token));
        Assert.IsType<DiscardResult.Failed>(issuer.Discard(new PreparedExecution(token.Provider,
            token.PreparationId, token.Met)));
        Assert.IsType<DiscardResult.Discarded>(issuer.Discard(token));
        Assert.IsType<DiscardResult.Discarded>(issuer.Discard(token));
        await issuer.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => issuer.Describe());
    }
}
