// Requirement-driven launch: the caller states guarantees, the runtime picks
// the native provider for the current OS, negotiates, and returns either a
// prepared execution or an explicit refusal. This is the integration point
// for callers (e.g. Hufu/Fuwen): they never name a provider class, a Job
// Object, a cgroup, or launchd. GP-3 (IExecutionProvider) is unchanged and
// frozen; the runtime is a thin selection facade over it.
//
// Constructed (watchdog) owner-death cleanup stays OUT of the native
// providers and out of this runtime: Windows has it natively, and the Linux
// and macOS compositions have materially different bounds (a macOS watchdog
// does not repair the setsid escape). Composition is a later, explicit layer.
using System.Runtime.InteropServices;
using Gagamba.Execution;
using Gagamba.Execution.Linux;
using Gagamba.Execution.MacOS;
using Gagamba.Execution.Windows;

namespace Gagamba.Runtime;

/// <summary>Platform-specific knobs a runtime may need. Everything is
/// optional; a missing/undelegated resource fails closed at Prepare.</summary>
public sealed record ExecutionRuntimeOptions
{
    /// <summary>Delegated cgroup v2 subtree root for the Linux provider.
    /// Defaults to /sys/fs/cgroup, which usually is NOT delegated; the
    /// provider rejects at Prepare when it cannot create sub-cgroups.</summary>
    public string? LinuxCgroupParent { get; init; }

    /// <summary>launchd domain for the macOS provider. Defaults to
    /// gui/&lt;uid&gt;.</summary>
    public string? MacOsDomain { get; init; }
}

/// <summary>
/// A selected execution provider for the current OS, presented through the
/// frozen SPI. On an unsupported OS it still exists but every Prepare is an
/// explicit refusal (fail-closed), never a silent no-op.
/// </summary>
public sealed class ExecutionRuntime : IExecutionProvider
{
    private readonly IExecutionProvider? _provider;
    private readonly string _refusal;
    private readonly object _gate = new();
    private bool _disposed;

    private ExecutionRuntime(IExecutionProvider? provider, string refusal)
    {
        _provider = provider;
        _refusal = refusal;
    }

    public bool HasProvider => _provider is not null;

    /// <summary>Why no provider was selected (empty when one was).</summary>
    public string RefusalReason => _refusal;

    /// <summary>Select the native provider for the current OS. Non-throwing;
    /// an unsupported OS yields a runtime whose Prepare refuses.</summary>
    public static ExecutionRuntime Create(ExecutionRuntimeOptions? options = null)
    {
        options ??= new ExecutionRuntimeOptions();
        if (OperatingSystem.IsWindows())
            return new ExecutionRuntime(new WindowsExecutionProvider(), "");
        if (OperatingSystem.IsLinux())
            return new ExecutionRuntime(
                new LinuxExecutionProvider(options.LinuxCgroupParent ?? "/sys/fs/cgroup"), "");
        if (OperatingSystem.IsMacOS())
            return new ExecutionRuntime(
                new MacOsExecutionProvider(options.MacOsDomain), "");
        return new ExecutionRuntime(null,
            $"no execution provider for '{RuntimeInformation.OSDescription}'");
    }

    public PlatformCapabilities Describe() =>
        _provider?.Describe() ?? UnsupportedPlatform;

    public PrepareResult Prepare(ExecutionRequirements requirements)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_provider is null)
            return new PrepareResult.Rejected(new[] { _refusal });
        return _provider.Prepare(requirements);
    }

    public LaunchResult Launch(PreparedExecution prepared, ProcessStartSpec process)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_provider is null)
            return new LaunchResult.Failed(new[] { _refusal });
        return _provider.Launch(prepared, process);
    }

    public TerminateResult Terminate(ExecutionHandle execution)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_provider is null)
            return new TerminateResult.Failed(new[] { _refusal });
        return _provider.Terminate(execution);
    }

    public DiscardResult Discard(PreparedExecution preparation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_provider is null)
            return new DiscardResult.Failed(new[] { _refusal });
        return _provider.Discard(preparation);
    }

    public ValueTask<CompletionResult> WaitForCompletionAsync(ExecutionHandle execution,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_provider is null)
            return ValueTask.FromResult<CompletionResult>(new CompletionResult.Failed(new[] { _refusal }));
        return _provider.WaitForCompletionAsync(execution, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;
        }
        return _provider?.DisposeAsync() ?? ValueTask.CompletedTask;
    }

    private static readonly PlatformCapabilities UnsupportedPlatform = new(
        "unsupported",
        Enum.GetValues<ExecutionCapability>().ToDictionary(
            c => c,
            _ => new CapabilityGrant(CapabilityLevel.Absent, GuaranteeKind.None,
                "no provider selected for this OS")));
}
