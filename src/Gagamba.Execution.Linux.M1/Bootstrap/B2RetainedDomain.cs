using System.Diagnostics;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
namespace Gagamba.Execution.Linux.M1.Bootstrap;

// Bounded B2 qualification only: the existing owner transaction stops BEFORE
// AllocationRegistered/copy/Prepared/Mint. No B3 composition or target exists.
internal static class B2RetainedDomain
{
    internal static Process Start(M1RecoveryScope scope, RecoveryConfiguration config, string ownerPath)
    {
        foreach (var name in new[] { "source", "staging", "journal" }) Directory.CreateDirectory(Path.Combine(config.EvidenceDirectory, name));
        var journal = new OwnershipJournal(new JournalFileStorage(), Path.Combine(config.EvidenceDirectory, "journal"));
        if (!journal.TryAcquireWriter(out var writer, out _)) throw new IOException("B2 intent fence unavailable");
        using (writer!)
        {
            if (!CgroupInterop.TryReadBootId(out var boot, out _)) throw new IOException("Boot identity unavailable");
            var plan = new CopyPlan(Path.Combine(config.EvidenceDirectory, "source"), Path.Combine(config.EvidenceDirectory, "staging"),
                "b2-unused", [new("tool", "tool", 1, null)], 1, 1, [], null, null);
            var request = new OwnerPreparationRequest(ownerPath, "b2-" + config.Execution.ToString("N"), config.Execution, config.Generation,
                boot, plan, journal, writer!, Guid.NewGuid(), Guid.NewGuid(),
                _ => throw new InvalidOperationException("B2 cannot reach manifest authorization"),
                1 + (long)(Stopwatch.GetTimestamp() * (1_000_000_000d / Stopwatch.Frequency)) + 30_000_000_000L,
                TestBarrier: "after-domain-retained");
            var outcome = scope.StartOwnerPreparation(request, CancellationToken.None);
            if (outcome.Barrier != "after-domain-retained" || outcome.Owner is null || outcome.Description is not null
                || scope.RetainedRecoveryLeases.Count != 1 || !scope.RetainedRecoveryLeases[0].AuthorityAlive)
                throw new IOException("B2 retained-domain barrier refused");
            return outcome.Owner;
        }
    }
    internal static string Stop(M1RecoveryScope scope)
    {
        if (scope.RetainedRecoveryLeases.Count != 1) return "Unknown";
        var lease = scope.RetainedRecoveryLeases[0];
        var check = lease.VerifyIdentity();
        if (!check.Match) return "RefusedIdentity";
        var kill = lease.RequestStop(); var population = lease.ObservePopulation();
        return kill.Ok && population.Known && population.Empty ? "KillWrittenEmptyObserved" : "Unknown";
    }
    internal static bool RemoveAfterExit(M1RecoveryScope scope, Process owner)
    {
        if (!scope.TryOwnerPidFdExited(owner, 0, out var exited, out _) || !exited) return false;
        return scope.RetainedRecoveryLeases.Count == 1 && scope.RetainedRecoveryLeases[0].RemoveIfIdentityMatches().Removed;
    }
}
