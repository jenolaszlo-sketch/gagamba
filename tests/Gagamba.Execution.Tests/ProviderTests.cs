// GP-3 SPI tests: fake providers prove the boundary behaves — negotiation
// through the interface, issuance validation, opaqueness, disposal.
// Portable pure logic (fakes, no OS calls); runs on every CI OS.
using Gagamba.Execution;
using Xunit;

namespace Gagamba.Execution.Tests;

internal sealed class FakeProvider : IExecutionProvider
{
    private readonly PlatformCapabilities _profile;
    private readonly IReadOnlyDictionary<ExecutionCapability, CapabilityGrant>? _composed;
    private readonly HashSet<Guid> _preparations = new();
    private readonly HashSet<Guid> _executions = new();
    private bool _disposed;

    public FakeProvider(
        PlatformCapabilities profile,
        IReadOnlyDictionary<ExecutionCapability, CapabilityGrant>? composed = null)
    {
        _profile = profile;
        _composed = composed;
    }

    public PlatformCapabilities Describe()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _profile;
    }

    public PrepareResult Prepare(ExecutionRequirements requirements)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var r = ExecutionNegotiator.Negotiate(
            _profile, requirements.Required, requirements.Preferred, _composed);
        if (!r.Accepted)
            return new PrepareResult.Rejected(r.Unmet);
        var prep = new PreparedExecution(_profile.Platform, Guid.NewGuid(), r.Met);
        _preparations.Add(prep.PreparationId);
        return new PrepareResult.Accepted(prep);
    }

    public DiscardResult Discard(PreparedExecution preparation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (preparation.Provider != _profile.Platform)
            return new DiscardResult.Failed(new[] { "unknown preparation: not issued by this provider" });
        _preparations.Remove(preparation.PreparationId);
        return new DiscardResult.Discarded(preparation);
    }

    public LaunchResult Launch(PreparedExecution prepared, ProcessStartSpec process)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (prepared.Provider != _profile.Platform || !_preparations.Contains(prepared.PreparationId))
            return new LaunchResult.Failed(new[] { "unknown preparation: not issued by this provider" });
        if (string.IsNullOrWhiteSpace(process.Executable))
            return new LaunchResult.Failed(new[] { "no executable" });
        var handle = new ExecutionHandle(_profile.Platform, Guid.NewGuid());
        _executions.Add(handle.ExecutionId);
        return new LaunchResult.Started(handle);
    }

    public TerminateResult Terminate(ExecutionHandle execution)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (execution.Provider != _profile.Platform || !_executions.Contains(execution.ExecutionId))
            return new TerminateResult.Failed(new[] { "unknown execution: not issued by this provider" });
        _executions.Remove(execution.ExecutionId);
        return new TerminateResult.Terminated(execution);
    }

    public ValueTask<CompletionResult> WaitForCompletionAsync(ExecutionHandle execution,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (execution.Provider != _profile.Platform || !_executions.Contains(execution.ExecutionId))
            return ValueTask.FromResult<CompletionResult>(
                new CompletionResult.Failed(new[] { "unknown execution: not issued by this provider" }));
        _executions.Remove(execution.ExecutionId);
        return ValueTask.FromResult<CompletionResult>(new CompletionResult.NaturalExit(0));
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        _preparations.Clear();
        _executions.Clear();
        return ValueTask.CompletedTask;
    }
}

public sealed class ProviderContractTests
{
    private static ExecutionRequirements StrictAgent() => new(
        new[]
        {
            ExecutionRequirement.Require(ExecutionCapability.UnitTermination),
            ExecutionRequirement.Require(ExecutionCapability.SurvivesRootExit),
            ExecutionRequirement.Require(ExecutionCapability.EscapeResistant),
        });

    [Fact]
    public async Task FullLifecycleThroughTheInterface()
    {
        await using var provider = new FakeProvider(WellKnownPlatforms.Windows);
        Assert.Equal("windows-job", provider.Describe().Platform);
        var prep = provider.Prepare(StrictAgent());
        var accepted = Assert.IsType<PrepareResult.Accepted>(prep);
        var launch = provider.Launch(accepted.Prepared,
            ProcessStartSpec.Simple("tool.exe", "--work", "/tmp/ws"));
        var started = Assert.IsType<LaunchResult.Started>(launch);
        var term = provider.Terminate(started.Handle);
        Assert.IsType<TerminateResult.Terminated>(term);
    }

    [Fact]
    public async Task ForeignPreparationAndUnknownHandleFailClosed()
    {
        await using var win = new FakeProvider(WellKnownPlatforms.Windows);
        await using var mac = new FakeProvider(WellKnownPlatforms.MacOs);
        var prep = Assert.IsType<PrepareResult.Accepted>(
            win.Prepare(new ExecutionRequirements(Array.Empty<ExecutionRequirement>())));
        // A preparation is only meaningful to its own provider.
        var cross = mac.Launch(prep.Prepared, ProcessStartSpec.Simple("tool.exe"));
        Assert.IsType<LaunchResult.Failed>(cross);
        var unknown = mac.Terminate(new ExecutionHandle("macos-launchd-pg", Guid.NewGuid()));
        Assert.IsType<TerminateResult.Failed>(unknown);
    }

    [Fact]
    public async Task MacStrictAgentRejectsWithReasons()
    {
        await using var mac = new FakeProvider(WellKnownPlatforms.MacOs);
        var prep = mac.Prepare(StrictAgent());
        var rejected = Assert.IsType<PrepareResult.Rejected>(prep);
        Assert.Contains(rejected.Reasons, r => r.Contains("EscapeResistant"));
    }

    [Fact]
    public async Task DiscardReclaimsPreparationAndIsIdempotent()
    {
        await using var win = new FakeProvider(WellKnownPlatforms.Windows);
        await using var mac = new FakeProvider(WellKnownPlatforms.MacOs);
        var prep = Assert.IsType<PrepareResult.Accepted>(
            win.Prepare(new ExecutionRequirements(Array.Empty<ExecutionRequirement>()))).Prepared;

        Assert.IsType<DiscardResult.Discarded>(win.Discard(prep));
        // After discard the preparation can never launch.
        Assert.IsType<LaunchResult.Failed>(win.Launch(prep, ProcessStartSpec.Simple("tool.exe")));
        // Idempotent: a second discard is still a no-op success.
        Assert.IsType<DiscardResult.Discarded>(win.Discard(prep));
        // A preparation not issued by this provider fails closed.
        Assert.IsType<DiscardResult.Failed>(mac.Discard(prep));
    }

    [Fact]
    public async Task ComposedOwnerDeathFlowsThroughProvider()
    {
        await using var lin = new FakeProvider(WellKnownPlatforms.Linux,
            WellKnownPlatforms.ComposedOwnerDeath["linux-cgroup-v2"]);
        var req = new ExecutionRequirements(new[]
        {
            ExecutionRequirement.Require(
                ExecutionCapability.OwnerDeathCleanup, CapabilityLevel.Partial, true),
        });
        var prep = Assert.IsType<PrepareResult.Accepted>(lin.Prepare(req));
        Assert.Contains(prep.Prepared.Met, m => m.Contains("Constructed"));
    }

    [Fact]
    public async Task DisposedProviderThrowsOnEveryMethod()
    {
        var provider = new FakeProvider(WellKnownPlatforms.Windows);
        await provider.DisposeAsync();
        var req = new ExecutionRequirements(Array.Empty<ExecutionRequirement>());
        Assert.Throws<ObjectDisposedException>(() => provider.Describe());
        Assert.Throws<ObjectDisposedException>(() => provider.Prepare(req));
        Assert.Throws<ObjectDisposedException>(() =>
            provider.Launch(new PreparedExecution("x", Guid.NewGuid(), Array.Empty<string>()),
                ProcessStartSpec.Simple("x")));
        Assert.Throws<ObjectDisposedException>(() =>
            provider.Terminate(new ExecutionHandle("x", Guid.NewGuid())));
        Assert.Throws<ObjectDisposedException>(() =>
            provider.Discard(new PreparedExecution("x", Guid.NewGuid(), Array.Empty<string>())));
    }

    [Fact]
    public void HandlesStayOpaque()
    {
        // No PIDs, native handles, cgroup paths, PGIDs, or job objects may
        // cross the public contract (names or types).
        var bannedTypes = new HashSet<Type>
        {
            typeof(IntPtr), typeof(UIntPtr),
            typeof(System.Runtime.InteropServices.SafeHandle),
            typeof(System.Diagnostics.Process),
        };
        string[] bannedWords = new[]
        {
            "Pid", "JobHandle", "Cgroup", "Pgid", "Hwnd",
            "JobObject", "Launchd", "ProcessGroup",
        };
        // PreparationId/ExecutionId contain no banned substring; the opaque
        // handle concept itself (ExecutionHandle) is allowed.
        bool bannedName(string n) =>
            bannedWords.Any(w => n.Contains(w, StringComparison.OrdinalIgnoreCase));
        // Public contract surface only: internal helpers necessarily use
        // native handles; the rule is that none cross into public view.
        var offenders = new List<string>();
        foreach (var t in typeof(PlatformCapabilities).Assembly.GetTypes()
                     .Where(t => t.IsPublic || t.IsNestedPublic))
        {
            foreach (var m in t.GetMembers(
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.DeclaredOnly))
            {
                Type? ret = (m as System.Reflection.PropertyInfo)?.PropertyType
                    ?? (m as System.Reflection.FieldInfo)?.FieldType
                    ?? (m as System.Reflection.MethodInfo)?.ReturnType;
                if (ret is not null && (bannedTypes.Contains(ret)
                    || typeof(System.Runtime.InteropServices.SafeHandle).IsAssignableFrom(ret)))
                    offenders.Add($"{t.Name}.{m.Name}:{ret.Name}");
                if (bannedName(m.Name))
                    offenders.Add($"{t.Name}.{m.Name}");
            }
        }
        Assert.True(offenders.Count == 0, "leaked native identity: " + string.Join(", ", offenders));
    }
}
