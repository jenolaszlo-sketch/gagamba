// Minimal owner-process bootstrap. Transaction semantics live in the single
// shared OwnerPreparationTransaction path; this executable validates its
// inherited private endpoint and then runs that transaction as the issuer.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;

static void Emit(string line)
{
    Console.Out.WriteLine(line);
    Console.Out.Flush();
}

if (!OperatingSystem.IsLinux())
{
    Emit("ERR not-linux");
    return 2;
}

var arguments = new Dictionary<string, string>(StringComparer.Ordinal);
for (var index = 0; index + 1 < args.Length; index += 2)
{
    if (!args[index].StartsWith("--", StringComparison.Ordinal)
        || !arguments.TryAdd(args[index], args[index + 1]))
    {
        Emit("ERR invalid-arguments");
        return 3;
    }
}

if (!arguments.TryGetValue("--exec", out var executionText)
    || !Guid.TryParseExact(executionText, "D", out var executionId)
    || executionId == Guid.Empty
    || !arguments.TryGetValue("--lease", out var leaseText)
    || !Guid.TryParseExact(leaseText, "D", out var leaseGeneration)
    || leaseGeneration == Guid.Empty
    || arguments.Count != 2)
{
    Emit("ERR invalid-identity");
    return 3;
}

var bootstrapFdText = Environment.GetEnvironmentVariable("GAGAMBA_BOOTSTRAP_FD");
if (bootstrapFdText is null || !int.TryParse(bootstrapFdText, out var bootstrapFd) || bootstrapFd < 0)
{
    Emit("ERR missing-private-channel");
    return 3;
}

var map = new BootstrapMap(
    OwnerBootstrap.CurrentVersion,
    new[]
    {
        new BootstrapDescriptor(0, BootstrapRole.ControlRead),
        new BootstrapDescriptor(1, BootstrapRole.ControlWrite),
        new BootstrapDescriptor(2, BootstrapRole.RecoveryEvidence),
        new BootstrapDescriptor(bootstrapFd, BootstrapRole.HandleChannel),
    });
if (!OwnerBootstrap.TryValidate(map, out var bootstrapError))
{
    PreparationInterop.CloseFd(bootstrapFd);
    Emit($"ERR bootstrap {bootstrapError}");
    return 3;
}

return OwnerPreparationTransaction.Run(bootstrapFd, executionId, leaseGeneration);
