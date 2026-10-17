using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Launch;

internal enum LaunchDescriptorRole : byte
{
    EofStdin = 0,
    StdoutWriter = 1,
    StderrWriter = 2,
    CoordinatorConfig = 3,
    ControlReader = 4,
    CoordinatorStatusWriter = 5,
    SeccompProgram = 6,
    ToolObject = 7,
    InputObject = 8,
    CoordinatorObject = 9,
    HelperStatusWriter = 10,
    PinnedBubblewrap = 11,
}

internal sealed record LaunchDescriptor(int Slot, LaunchDescriptorRole Role, int FileDescriptor);

internal sealed class LaunchDescriptorMap
{
    private static readonly LaunchDescriptorRole[] RequiredRoles =
    [
        LaunchDescriptorRole.EofStdin,
        LaunchDescriptorRole.StdoutWriter,
        LaunchDescriptorRole.StderrWriter,
        LaunchDescriptorRole.CoordinatorConfig,
        LaunchDescriptorRole.ControlReader,
        LaunchDescriptorRole.CoordinatorStatusWriter,
        LaunchDescriptorRole.SeccompProgram,
        LaunchDescriptorRole.ToolObject,
        LaunchDescriptorRole.InputObject,
        LaunchDescriptorRole.CoordinatorObject,
        LaunchDescriptorRole.HelperStatusWriter,
        LaunchDescriptorRole.PinnedBubblewrap,
    ];

    private readonly LaunchDescriptor[] _entries;

    private LaunchDescriptorMap(LaunchDescriptor[] entries) => _entries = entries;

    internal IReadOnlyList<LaunchDescriptor> Entries => _entries;

    internal int this[LaunchDescriptorRole role] =>
        _entries.Single(entry => entry.Role == role).FileDescriptor;

    internal static bool TryCreate(
        IEnumerable<LaunchDescriptor>? descriptors,
        out LaunchDescriptorMap? map,
        out string error)
    {
        map = null;
        error = string.Empty;
        if (descriptors is null)
        {
            error = "descriptor map is missing";
            return false;
        }

        var entries = descriptors.OrderBy(entry => entry.Slot).ToArray();
        if (entries.Length != RequiredRoles.Length)
        {
            error = $"descriptor map has {entries.Length} entries; expected {RequiredRoles.Length}";
            return false;
        }

        var seenRoles = new HashSet<LaunchDescriptorRole>();
        var seenFds = new HashSet<int>();
        for (var slot = 0; slot < entries.Length; slot++)
        {
            var item = entries[slot];
            if (item.Slot != slot || item.Role != RequiredRoles[slot]
                || item.FileDescriptor < 0 || !seenRoles.Add(item.Role)
                || !seenFds.Add(item.FileDescriptor)
                || !PreparationInterop.IsValidFd(item.FileDescriptor)
                || !PreparationInterop.HasCloexec(item.FileDescriptor))
            {
                error = $"descriptor map slot {slot} has a missing, duplicate, unexpected, or non-CLOEXEC source";
                return false;
            }

            if (!NativeLaunchInterop.HasExpectedTypeAndAccess(slot, item.FileDescriptor, out error))
            {
                return false;
            }
        }

        map = new LaunchDescriptorMap(entries);
        return true;
    }

    internal static IReadOnlyList<(int Slot, string Role, string Fate)> Contract { get; } =
    [
        (0, "EOF-only stdin read pipe", "Coordinator and target; writer closed before HELD"),
        (1, "stdout pipe writer", "Coordinator/scaffolding; owner reader remains outside"),
        (2, "stderr pipe writer", "Coordinator/scaffolding; owner reader remains outside"),
        (3, "sealed immutable coordinator configuration", "Read before HELD, then closed"),
        (4, "control pipe reader", "Coordinator only; WP04 refuses every byte"),
        (5, "trusted coordinator status writer", "Coordinator parent only"),
        (6, "sealed seccomp program", "Bubblewrap consumes"),
        (7, "copied static tool read-only FD", "Read-only /tool bind"),
        (8, "copied input read-only FD", "Read-only /input bind"),
        (9, "copied coordinator read-only FD", "Read-only /coordinator bind"),
        (10, "Bubblewrap JSON status writer", "Independent bounded helper-status channel"),
        (11, "verified Bubblewrap executable FD", "Executed as /proc/self/fd/11"),
    ];
}
