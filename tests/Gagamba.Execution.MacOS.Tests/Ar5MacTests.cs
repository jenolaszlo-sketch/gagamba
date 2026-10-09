using Gagamba.Execution;
using Gagamba.Execution.MacOS;
using Xunit;

namespace Gagamba.Execution.MacOS.Tests;

public sealed class Ar5MacTests
{
    private sealed class Fake : ILaunchdTransport
    {
        public int Kickstarts { get; private set; }
        public string? Plist { get; private set; }
        private bool _removed;
        public HelperResult Run(string verb, string target, string? extraArg = null, TimeSpan? timeout = null)
        {
            if (verb == "bootstrap") Plist = extraArg;
            if (verb == "kickstart") Kickstarts++;
            if (verb == "bootout") _removed = true;
            if (verb == "print" && target != "gui/501")
                return _removed
                    ? new HelperResult(113, "Could not find service \"test\" in domain", "")
                    : new HelperResult(0, "state = running\n", "");
            return new HelperResult(0, "", "");
        }
    }

    [Fact]
    public async Task CaptureRefusesBeforeDispatchAndLegacyOutputHasNoHiddenFile()
    {
        var fake = new Fake();
        var provider = new MacOsExecutionProvider("gui/501", fake);
        try
        {
            var token = Assert.IsType<PrepareResult.Accepted>(provider.Prepare(
                new ExecutionRequirements([]))).Prepared;
            var spec = ProcessStartSpec.Vector("/bin/echo", ["secret"], Path.GetTempPath());
            var refusal = provider.LaunchCaptured(token, spec, new OutputCaptureOptions(32, 32, 64));
            Assert.IsType<CaptureLaunchResult.Failed>(refusal);
            Assert.Equal(0, fake.Kickstarts);
            Assert.IsType<LaunchResult.Started>(provider.Launch(token, spec));
            string plist = File.ReadAllText(fake.Plist!);
            Assert.Contains("<key>StandardOutPath</key><string>/dev/null</string>", plist);
            Assert.Contains("<key>StandardErrorPath</key><string>/dev/null</string>", plist);
            Assert.DoesNotContain("stdout.log", plist);
            Assert.DoesNotContain("stderr.log", plist);
        }
        finally { await provider.DisposeAsync(); }
    }
}
