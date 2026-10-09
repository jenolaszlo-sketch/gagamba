using Gagamba.Execution;
using Gagamba.Execution.MacOS;
using Xunit;

namespace Gagamba.Execution.MacOS.Tests;

public sealed class Ar4MacTests
{
    private sealed class Fake : ILaunchdTransport
    {
        public string? Plist { get; private set; }
        private bool _removed;
        public HelperResult Run(string verb, string target, string? extraArg = null, TimeSpan? timeout = null)
        {
            if (verb == "bootstrap") Plist = extraArg;
            if (verb == "bootout") _removed = true;
            if (verb == "print" && target != "gui/501")
                return _removed
                    ? new HelperResult(113, "Could not find service \"test\" in domain", "")
                    : new HelperResult(0, "state = running\n", "");
            return new HelperResult(0, "", "");
        }
    }

    [Fact]
    public async Task ArgumentVectorReachesPlistWithoutRawReparsing()
    {
        var fake = new Fake();
        var provider = new MacOsExecutionProvider("gui/501", fake);
        try
        {
            var prep = Assert.IsType<PrepareResult.Accepted>(provider.Prepare(
                new ExecutionRequirements(Array.Empty<ExecutionRequirement>()))).Prepared;
            var spec = ProcessStartSpec.Vector("/bin/echo",
                new[] { "", "two words", "a\"b", "C:\\trail\\", "雪" },
                Path.GetTempPath(), new Dictionary<string, string>());
            Assert.IsType<LaunchResult.Started>(provider.Launch(prep, spec));
            string plist = File.ReadAllText(fake.Plist!);
            Assert.Contains("<string></string>", plist);
            Assert.Contains("<string>two words</string>", plist);
            Assert.Contains("<string>a\"b</string>", plist);
            Assert.Contains("<string>C:\\trail\\</string>", plist);
            Assert.Contains("<string>雪</string>", plist);
        }
        finally { await provider.DisposeAsync(); }
    }

    [Fact]
    public async Task ForeignAndForgedDiscardFailButIssuedTokenIsIdempotent()
    {
        var first = new MacOsExecutionProvider("gui/501", new Fake());
        var second = new MacOsExecutionProvider("gui/501", new Fake());
        try
        {
            var token = Assert.IsType<PrepareResult.Accepted>(first.Prepare(
                new ExecutionRequirements(Array.Empty<ExecutionRequirement>()))).Prepared;
            Assert.IsType<DiscardResult.Failed>(second.Discard(token));
            Assert.IsType<DiscardResult.Failed>(first.Discard(new PreparedExecution(
                token.Provider, token.PreparationId, token.Met)));
            Assert.IsType<DiscardResult.Discarded>(first.Discard(token));
            Assert.IsType<DiscardResult.Discarded>(first.Discard(token));
        }
        finally { await first.DisposeAsync(); await second.DisposeAsync(); }
    }

    [Fact]
    public async Task InvalidInvocationDoesNotConsumePreparation()
    {
        var fake = new Fake();
        var provider = new MacOsExecutionProvider("gui/501", fake);
        try
        {
            var token = Assert.IsType<PrepareResult.Accepted>(provider.Prepare(
                new ExecutionRequirements(Array.Empty<ExecutionRequirement>()))).Prepared;
            Assert.IsType<LaunchResult.Failed>(provider.Launch(token,
                new ProcessStartSpec("/bin/echo", "", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
                    new Dictionary<string, string>())));
            Assert.IsType<LaunchResult.Started>(provider.Launch(token,
                ProcessStartSpec.Vector("/bin/echo", new[] { "valid" }, Path.GetTempPath())));
        }
        finally { await provider.DisposeAsync(); }
    }

    [Fact]
    public async Task ConcurrentLaunchAndDiscardHaveExactlyOneWinner()
    {
        var provider = new MacOsExecutionProvider("gui/501", new Fake());
        try
        {
            var token = Assert.IsType<PrepareResult.Accepted>(provider.Prepare(
                new ExecutionRequirements(Array.Empty<ExecutionRequirement>()))).Prepared;
            var launch = Task.Run(() => provider.Launch(token,
                ProcessStartSpec.Vector("/bin/echo", new[] { "race" }, Path.GetTempPath())));
            var discard = Task.Run(() => provider.Discard(token));
            bool launched = await launch is LaunchResult.Started;
            bool discarded = await discard is DiscardResult.Discarded;
            Assert.NotEqual(launched, discarded);
        }
        finally { await provider.DisposeAsync(); }
    }
}
