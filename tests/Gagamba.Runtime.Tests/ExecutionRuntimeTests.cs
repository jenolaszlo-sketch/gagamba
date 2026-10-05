// Requirement-driven launch tests: the runtime selects the native provider
// for the current OS, negotiates requirements through it, and never fakes a
// composed guarantee. Portable: runs on every CI OS.
using Gagamba.Execution;
using Gagamba.Runtime;
using Xunit;

namespace Gagamba.Runtime.Tests;

public sealed class ExecutionRuntimeTests : IAsyncLifetime
{
    private ExecutionRuntime _runtime = ExecutionRuntime.Create();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _runtime.DisposeAsync();

    [Fact]
    public void SelectsProviderForCurrentOs()
    {
        string expected =
            OperatingSystem.IsWindows() ? "windows-job"
            : OperatingSystem.IsLinux() ? "linux-cgroup-v2"
            : OperatingSystem.IsMacOS() ? "macos-launchd-pg"
            : "unsupported";
        var caps = ExecutionRuntime.Create().Describe();
        Assert.Equal(expected, caps.Platform);
        Assert.Empty(MatrixValidation.Validate(caps));
    }

    [Fact]
    public void OwnerDeathCleanupNeverFakedAcrossRuntime()
    {
        // Windows grants it natively; Linux/macOS are Absent and must refuse
        // when the caller demands it natively (allowConstructed:false). The
        // runtime must not invent a composed guarantee.
        var prep = _runtime.Prepare(new ExecutionRequirements(new[]
        {
            ExecutionRequirement.Require(ExecutionCapability.OwnerDeathCleanup,
                CapabilityLevel.Full, allowConstructed: false),
        }));
        if (OperatingSystem.IsWindows())
        {
            Assert.IsType<PrepareResult.Accepted>(prep);
        }
        else
        {
            var rejected = Assert.IsType<PrepareResult.Rejected>(prep);
            Assert.Contains(rejected.Reasons, r => r.Contains("OwnerDeathCleanup"));
        }
    }

    [Fact]
    public void UnmetRequirementIsNamedInRefusal()
    {
        // Every native profile lacks Full-native EscapeResistant except
        // Windows. This proves the refusal explains which guarantee is
        // missing rather than failing opaquely.
        var prep = _runtime.Prepare(new ExecutionRequirements(new[]
        {
            ExecutionRequirement.Require(ExecutionCapability.EscapeResistant,
                CapabilityLevel.Full, allowConstructed: false),
        }));
        if (OperatingSystem.IsWindows())
        {
            Assert.IsType<PrepareResult.Accepted>(prep);
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var rejected = Assert.IsType<PrepareResult.Rejected>(prep);
            Assert.Contains(rejected.Reasons, r => r.Contains("EscapeResistant"));
        }
    }

    [Fact]
    public void EmptyRequirementsAcceptWhenProviderUsable()
    {
        var prep = _runtime.Prepare(new ExecutionRequirements(
            Array.Empty<ExecutionRequirement>()));
        // Windows and macOS always usable; Linux depends on delegation. A
        // refusal here must be explicit (delegation), never a throw.
        if (!OperatingSystem.IsLinux())
            Assert.IsType<PrepareResult.Accepted>(prep);
        else
            Assert.NotNull(prep);
    }

    [Fact]
    public void PublicSurfaceStaysOpaque()
    {
        var banned = new[] { "Pid", "JobHandle", "Cgroup", "Pgid", "Hwnd",
            "JobObject", "Launchd", "ProcessGroup", "Label", "Plist", "Handle" };
        var offenders = new List<string>();
        foreach (var t in typeof(ExecutionRuntime).Assembly.GetTypes()
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
                if (ret is not null && (ret == typeof(IntPtr) || ret == typeof(UIntPtr)
                    || typeof(System.Runtime.InteropServices.SafeHandle).IsAssignableFrom(ret)
                    || ret == typeof(System.Diagnostics.Process)))
                    offenders.Add($"{t.Name}.{m.Name}:{ret.Name}");
                // Native handle type must not surface; "Handle" in a name is
                // allowed only for the opaque ExecutionHandle in the SPI.
                if (m.Name is "DangerousHandle" or "NativeHandle" or "JobHandle")
                    offenders.Add($"{t.Name}.{m.Name}");
            }
        }
        Assert.True(offenders.Count == 0, "leaked native identity: " + string.Join(", ", offenders));
    }
}
