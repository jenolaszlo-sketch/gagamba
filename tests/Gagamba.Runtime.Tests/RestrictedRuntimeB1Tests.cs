using Gagamba.Execution;
using Gagamba.Execution.Restricted.Control;
using Xunit;

namespace Gagamba.Runtime.Tests;

public sealed class RestrictedRuntimeB1Tests
{
    [Fact]
    public void RestrictedSelectionIsSeparateAndUnavailableWithoutInstalledQualification()
    {
        var provider = RestrictedExecutionRuntime.Create();
        Assert.False(provider is IExecutionProvider);
        Assert.Equal("gagamba-restricted-control-v1", provider.Describe().ContractId);
        foreach (var rid in new[] { "win-x64", "linux-x64", "osx-arm64" })
        {
            var result = provider.CreateOperation(new("restricted-offline-closure-v1", "1", rid,
                "caller-cannot-qualify", new byte[32]));
            Assert.Null(result.Operation);
            Assert.Equal(RestrictedRefusalReason.InstallationUnavailable, result.Reason);
        }
    }
}
