using Gagamba.Execution;
using Gagamba.Runtime;

string expected = OperatingSystem.IsWindows() ? WellKnownPlatforms.Windows.Platform
    : OperatingSystem.IsLinux() ? WellKnownPlatforms.Linux.Platform
    : OperatingSystem.IsMacOS() ? WellKnownPlatforms.MacOs.Platform
    : throw new PlatformNotSupportedException();

var runtimeOptions = new ExecutionRuntimeOptions
{
    LinuxCgroupParent = Environment.GetEnvironmentVariable("GAGAMBA_LINUX_CGROUP"),
    MacOsDomain = Environment.GetEnvironmentVariable("GAGAMBA_LAUNCHD_DOMAIN"),
};
await using var runtime = ExecutionRuntime.Create(runtimeOptions);
if (runtime.Describe().Platform != expected) throw new Exception("wrong installed provider selected");

ProcessStartSpec spec;
if (OperatingSystem.IsWindows())
{
    string system = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    spec = new ProcessStartSpec(Path.Combine(Environment.SystemDirectory, "cmd.exe"),
        "/d /c %SYSTEMROOT%\\System32\\ping.exe -n 30 127.0.0.1 >nul",
        Path.GetTempPath(), new Dictionary<string, string> { ["SYSTEMROOT"] = system });
}
else
    spec = ProcessStartSpec.Vector("/bin/sh", ["-c", "/bin/sleep 30"], "/tmp");

var receipt = await ExecutionOrchestrator.RunAsync(runtime,
    new ExecutionRequirements([]), spec,
    new RunOptions(TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(8),
        OperatingSystem.IsMacOS() ? null : new OutputCaptureOptions(1024, 1024, 2048)))
    .WaitAsync(TimeSpan.FromSeconds(20));
if (receipt.Cause != RunCause.Deadline || !receipt.StopRequestAccepted
    || receipt.Cleanup != CleanupStatus.Confirmed || receipt.Platform != expected)
    throw new Exception($"installed lifecycle failed: {receipt.Cause}, {receipt.Cleanup}");
if (!OperatingSystem.IsMacOS() && receipt.Output?.Status != OutputCaptureStatus.Complete)
    throw new Exception("installed bounded capture failed");
Console.WriteLine("installed lifecycle passed on " + expected);
