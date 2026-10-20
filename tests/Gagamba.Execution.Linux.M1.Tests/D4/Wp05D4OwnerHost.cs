// D4 test-only fresh-process release-owner fixture. This file is in the test
// assembly, so the non-packable Wp05D4Owner console can call it without
// exposing production internals. It composes the unchanged R12 release path
// (real OwnershipJournal, PreparedTransaction, HeldExecution and ReleaseArbiter)
// and adds only test-controlled gates, storage faults and observer records.
using System.Diagnostics;
using System.Reflection;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Microsoft.Win32.SafeHandles;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests.D4;

public static class Wp05D4OwnerHost
{
    public static int Run(string[] args)
    {
        try
        {
            return RunCore(Parse(args));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"D4 owner failed: {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    private static Dictionary<string, string> Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index++)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"expected --key, got {args[index]}");
            var key = args[index][2..];
            var value = index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++index]
                : "true";
            values[key] = value;
        }

        return values;
    }

    private static string Required(IReadOnlyDictionary<string, string> args, string name) =>
        args.TryGetValue(name, out var value) && !string.IsNullOrEmpty(value)
            ? value
            : throw new ArgumentException($"missing --{name}");

    private static int RunCore(IReadOnlyDictionary<string, string> args)
    {
        var mode = Required(args, "mode");
        var caseId = Required(args, "case");
        var resultPath = Required(args, "result");
        var witnessPath = Required(args, "witness");
        var barrierPath = args.GetValueOrDefault("barrier", string.Empty);
        var goPath = args.GetValueOrDefault("go", string.Empty);
        D4Witness.Append(witnessPath, caseId, "OwnerStarted", "owner", Environment.ProcessId.ToString());

        if (mode is "recover" or "replace-release" or "contend")
        {
            return RunRecoveryMode(mode, args, caseId, resultPath, witnessPath, barrierPath, goPath);
        }

        var root = Required(args, "root");
        var barrierName = args.GetValueOrDefault("barrier-name", "none");
        var storageGate = args.GetValueOrDefault("storage-gate", string.Empty);
        var fault = args.GetValueOrDefault("fault", string.Empty);
        var hold = args.ContainsKey("hold");
        Directory.CreateDirectory(root);
        using var session = new OwnerSession(root, barrierPath, goPath, witnessPath, caseId);
        session.Ready();
        session.Storage.Arm(storageGate, fault);

        if (mode == "stop" || mode == "stop-fail")
        {
            if (mode == "stop-fail") session.Storage.ArmStopFailure();
            session.BarrierName = "none";
            var stop = session.Arbiter.RequestStop(StopCauseKind.CallerStop);
            D4Result.Write(resultPath, new Dictionary<string, string>
            {
                ["mode"] = mode,
                ["owner_pid"] = Environment.ProcessId.ToString(),
                ["stop_accepted"] = stop.Accepted.ToString(),
                ["stop_durable"] = stop.DurableStop.ToString(),
                ["stop_accepted_at"] = stop.StopAcceptedAtNanoseconds.ToString(),
                ["stop_deadline"] = stop.StopDeadlineNanoseconds.ToString(),
                ["journal_seq"] = session.LatestSequence().ToString(),
                ["capability_live"] = session.Capability.IsLive.ToString(),
            });
            if (hold || barrierName != "none") D4Barrier.Park(barrierPath, goPath, "after-stop");
            return 0;
        }

        if (mode == "ambiguous-intent")
        {
            session.BarrierName = barrierName;
            var decision = session.Arbiter.RequestRelease(session.Request);
            D4Result.Write(resultPath, new Dictionary<string, string>
            {
                ["mode"] = mode,
                ["owner_pid"] = Environment.ProcessId.ToString(),
                ["decision_kind"] = decision.Kind.ToString(),
                ["durable_release"] = decision.DurableRelease.ToString(),
                ["delivery"] = decision.Delivery.ToString(),
                ["durable_committed"] = decision.DurableCommitted.ToString(),
                ["journal_seq"] = session.LatestSequence().ToString(),
                ["transport_attempts"] = session.Attempts.ToString(),
            });
            if (hold || barrierName != "none") D4Barrier.Park(barrierPath, goPath, "after-ambiguous");
            return 0;
        }

        if (mode == "duplicate")
        {
            session.BarrierName = "after-intent";
            var atIntent = new ManualResetEventSlim();
            var resume = new ManualResetEventSlim();
            session.Arbiter.TestBarrier = name =>
            {
                if (name != "after-intent") return;
                D4Barrier.Announce(barrierPath, "after-intent", Environment.ProcessId, session.LatestSequence());
                atIntent.Set();
                if (!resume.Wait(TimeSpan.FromSeconds(30)))
                    throw new TimeoutException("duplicate barrier timed out");
            };
            var winnerTask = Task.Run(() => session.Arbiter.RequestRelease(session.Request));
            if (!atIntent.Wait(TimeSpan.FromSeconds(30)))
                throw new TimeoutException("winner did not reach after-intent");
            var concurrent = session.Arbiter.RequestRelease(session.Request);
            resume.Set();
            if (!winnerTask.Wait(TimeSpan.FromSeconds(30)))
                throw new TimeoutException("winner release did not complete");
            var winner = winnerTask.Result;
            var later = session.Arbiter.RequestRelease(session.Request);
            session.RecordCoordinatorExit();
            D4Result.Write(resultPath, new Dictionary<string, string>
            {
                ["mode"] = mode,
                ["owner_pid"] = Environment.ProcessId.ToString(),
                ["winner_kind"] = winner.Kind.ToString(),
                ["winner_durable"] = winner.DurableRelease.ToString(),
                ["concurrent_kind"] = concurrent.Kind.ToString(),
                ["concurrent_durable"] = concurrent.DurableRelease.ToString(),
                ["later_kind"] = later.Kind.ToString(),
                ["journal_seq"] = session.LatestSequence().ToString(),
                ["transport_attempts"] = session.Attempts.ToString(),
                ["coordinator_exit"] = session.CoordinatorExit?.ToString() ?? "unknown",
            });
            if (hold) D4Barrier.Park(barrierPath, goPath, "after-duplicate");
            return 0;
        }

        if (mode == "release")
        {
            if (args.ContainsKey("hold-before-release"))
                D4Barrier.Park(barrierPath, goPath, "release-ready");
            session.BarrierName = barrierName;
            var decision = session.Arbiter.RequestRelease(session.Request);
            session.RecordCoordinatorExit();
            D4Result.Write(resultPath, new Dictionary<string, string>
            {
                ["mode"] = mode,
                ["owner_pid"] = Environment.ProcessId.ToString(),
                ["decision_kind"] = decision.Kind.ToString(),
                ["durable_release"] = decision.DurableRelease.ToString(),
                ["delivery"] = decision.Delivery.ToString(),
                ["durable_committed"] = decision.DurableCommitted.ToString(),
                ["live_execution_excluded"] = decision.LiveExecutionExcluded.ToString(),
                ["journal_seq"] = session.LatestSequence().ToString(),
                ["transport_attempts"] = session.Attempts.ToString(),
                ["coordinator_exit"] = session.CoordinatorExit?.ToString() ?? "unknown",
            });
            if (hold) D4Barrier.Park(barrierPath, goPath, "after-release");
            return 0;
        }

        throw new ArgumentException($"unsupported mode {mode}");
    }

    private static int RunRecoveryMode(
        string mode,
        IReadOnlyDictionary<string, string> args,
        string caseId,
        string resultPath,
        string witnessPath,
        string barrierPath,
        string goPath)
    {
        var journalPath = Required(args, "journal");
        var journal = new OwnershipJournal(new JournalFileStorage(), journalPath);
        var initial = journal.ReadLatest();
        if (mode is "recover" or "replace-release"
            && !initial.Ok
            && initial.Error.Contains("seal", StringComparison.OrdinalIgnoreCase))
        {
            D4Witness.Append(witnessPath, caseId, "RecoverySealObserved", "replacement",
                Environment.ProcessId.ToString(), result: OneLine(initial.Error));
            if (journal.TryRecoverCommit(out _, out var recoverError))
            {
                D4Witness.Append(witnessPath, caseId, "RecoverySealConfirmed", "replacement",
                    Environment.ProcessId.ToString(), result: "recovered");
            }
            else
            {
                D4Witness.Append(witnessPath, caseId, "RecoverySealRefused", "replacement",
                    Environment.ProcessId.ToString(), result: OneLine(recoverError));
            }
        }

        if (!journal.TryAcquireWriter(out var writer, out var acquireError))
        {
            D4Result.Write(resultPath, new Dictionary<string, string>
            {
                ["mode"] = mode,
                ["owner_pid"] = Environment.ProcessId.ToString(),
                ["writer"] = "refused",
                ["error"] = OneLine(acquireError),
                ["initial_journal_ok"] = initial.Ok.ToString(),
                ["initial_journal_error"] = OneLine(initial.Error),
            });
            D4Witness.Append(witnessPath, caseId, "RecoveryWriterRefused", "replacement", Environment.ProcessId.ToString());
            return 3;
        }

        using (writer)
        {
            var baseline = writer!.BaselineSequence;
            D4Witness.Append(witnessPath, caseId, "RecoveryStarted", "replacement",
                Environment.ProcessId.ToString(), baseline: baseline);
            if (mode == "contend")
            {
                D4Result.Write(resultPath, new Dictionary<string, string>
                {
                    ["mode"] = mode,
                    ["owner_pid"] = Environment.ProcessId.ToString(),
                    ["writer"] = "acquired",
                    ["baseline"] = baseline.ToString(),
                });
                D4Barrier.Announce(barrierPath, "contend-held", Environment.ProcessId, baseline);
                Thread.Sleep(TimeSpan.FromMilliseconds(int.Parse(args.GetValueOrDefault("hold-ms", "2000"))));
                return 0;
            }

            var latest = journal.ReadLatest();
            if (!latest.Ok || latest.Snapshot is null)
            {
                D4Result.Write(resultPath, new Dictionary<string, string>
                {
                    ["mode"] = mode,
                    ["owner_pid"] = Environment.ProcessId.ToString(),
                    ["writer"] = "acquired",
                    ["baseline"] = baseline.ToString(),
                    ["recovered"] = "false",
                    ["error"] = OneLine(latest.Error),
                    ["release_attempted"] = "false",
                });
                D4Witness.Append(witnessPath, caseId, "RecoveryFinished", "replacement",
                    Environment.ProcessId.ToString(), result: OneLine(latest.Error), baseline: baseline);
                return 0;
            }

            var snapshot = latest.Snapshot;
            var classification = ReleaseRecoveryClassifier.Classify(snapshot);
            var result = new Dictionary<string, string>
            {
                ["mode"] = mode,
                ["owner_pid"] = Environment.ProcessId.ToString(),
                ["writer"] = "acquired",
                ["baseline"] = baseline.ToString(),
                ["recovered"] = "true",
                ["sequence"] = snapshot.Sequence.ToString(),
                ["release_tag"] = snapshot.Release?.Tag.ToString() ?? "none",
                ["release_delivery"] = snapshot.Release?.DeliveryResult.ToString() ?? "none",
                ["stop"] = snapshot.Stop is null ? "false" : "true",
                ["stop_deadline"] = snapshot.Stop?.StopDeadlineNanoseconds.ToString() ?? "0",
                ["classification_release"] = classification.Release.ToString(),
                ["classification_exec"] = classification.Exec.ToString(),
                ["classification_root"] = classification.Root.ToString(),
                ["release_attempted"] = "false",
            };

            if (mode == "replace-release")
            {
                result["release_attempted"] = "true";
                result["release_source"] = "new-test-only-capability";
                var preparation = snapshot.Preparation
                    ?? throw new InvalidOperationException("prepared snapshot has no preparation identity");
                var sourcePath = Required(args, "source");
                var replacementManifest = BuildManifestFromSource(sourcePath);
                var replacementDigest = ManifestCodec.ComputeDigestBytes(replacementManifest);
                if (!preparation.ManifestDigest.AsSpan().SequenceEqual(replacementDigest))
                    throw new InvalidOperationException("replacement manifest digest does not match the prepared identity");
                var identity = new PreparationIdentity(
                    preparation.ExecutionId, preparation.IssuerId, preparation.GenerationId,
                    preparation.PreparationId, replacementDigest);
                var store = new PreparedTransaction.PreparationStore(
                    preparation.IssuerId, preparation.GenerationId, new D4Clock());
                using var capability = store.Mint(
                    replacementManifest, identity, MonotonicNanoseconds() + 60_000_000_000L, TestClosure.Empty());
                var transport = new D4RecordingTransport(witnessPath, caseId, Environment.ProcessId);
                var arbiter = new ReleaseArbiter(
                    capability, journal, writer, new D4Clock(),
                    () => new ReleasePrerequisites(true, true, true, true), transport);
                var now = MonotonicNanoseconds();
                var decision = arbiter.RequestRelease(
                    new ReleaseRequest(identity, now + 10_000_000_000L, now + 30_000_000_000L));
                result["replacement_decision"] = decision.Kind.ToString();
                result["replacement_durable"] = decision.DurableRelease.ToString();
                result["replacement_delivery"] = decision.Delivery.ToString();
                result["replacement_transport_attempts"] = transport.Attempts.ToString();
                result["replacement_journal_seq"] = journal.ReadLatest().Snapshot?.Sequence.ToString() ?? "unknown";
            }

            D4Result.Write(resultPath, result);
            D4Witness.Append(witnessPath, caseId, "RecoveryFinished", "replacement",
                Environment.ProcessId.ToString(), result: result["classification_release"], baseline: baseline);
            if (mode == "replace-release" && args.ContainsKey("hold"))
                D4Barrier.Park(barrierPath, goPath, "after-replacement-release");
            return 0;
        }
    }

    private static string OneLine(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ');

    public static void CleanupCgroup(string delegatedParent, string childName)
    {
        var path = Path.Combine(delegatedParent, childName);
        if (!Directory.Exists(path)) return;
        try { File.WriteAllText(Path.Combine(path, "cgroup.kill"), "1"); } catch { }
        for (var attempt = 0; attempt < 100; attempt++)
        {
            try
            {
                var events = File.ReadAllText(Path.Combine(path, "cgroup.events"));
                if (events.Contains("populated 0", StringComparison.Ordinal)) break;
            }
            catch
            {
                break;
            }

            Thread.Sleep(20);
        }

        try { Directory.Delete(path); } catch { }
    }

    private static long MonotonicNanoseconds()
    {
        var ticks = Stopwatch.GetTimestamp();
        return ticks / Stopwatch.Frequency * 1_000_000_000L
            + ticks % Stopwatch.Frequency * 1_000_000_000L / Stopwatch.Frequency;
    }

    private static string RepositoryRoot()
    {
        var configured = Environment.GetEnvironmentVariable("GAGAMBA_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Gagamba.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("GAGAMBA_REPOSITORY_ROOT is not set and no repository root was found");
    }

    private static RestrictedManifest BuildManifestFromSource(string sourcePath)
    {
        var descriptions = new List<StagedObjectDescription>();
        foreach (var role in new[] { "tool", "input", "coordinator", "filter" })
        {
            var path = Path.Combine(sourcePath, role);
            var bytes = File.ReadAllBytes(path);
            descriptions.Add(new StagedObjectDescription(
                role,
                System.Security.Cryptography.SHA256.HashData(bytes),
                (ulong)bytes.Length,
                0,
                0,
                role));
        }

        return BuildManifestCore(descriptions);
    }

    private static RestrictedManifest BuildManifestCore(IReadOnlyList<StagedObjectDescription> descriptions)
    {
        var baseline = SampleManifest();
        var closureObjects = descriptions.Select(item => new ClosureObject(
            item.Role, item.Sha256, item.Length,
            item.Role == "filter" ? "fd:6" : "/" + item.Role,
            item.Role == "filter" ? "p07-seccomp-filter" : "static-test-object"));
        return new RestrictedManifest(
            RestrictedManifest.CurrentSchemaVersion,
            RestrictedManifest.ImplementationIdentifier,
            baseline.Authority,
            baseline.Resources,
            new ClosureIdentity(closureObjects, new[] { "bubblewrap-0.11.1:" + HelperInstallation.PinnedSha256 }),
            baseline.Invocation,
            baseline.Channels);
    }

    private sealed class D4Clock : ITransactionClock
    {
        public long NowNanoseconds => MonotonicNanoseconds();
    }

    private sealed class D4RecordingTransport : IReleaseWriteChannel
    {
        private readonly string _witnessPath;
        private readonly string _caseId;
        private readonly int _pid;
        public int Attempts { get; private set; }

        public D4RecordingTransport(string witnessPath, string caseId, int pid)
        {
            _witnessPath = witnessPath;
            _caseId = caseId;
            _pid = pid;
        }

        public ReleasePipeWriteResult TryWriteOnce(ReadOnlyMemory<byte> frame)
        {
            Attempts++;
            D4Witness.Append(_witnessPath, _caseId, "TestTransportAttempt", "test-only-interface",
                _pid.ToString(), result: $"attempt={Attempts};bytes={frame.Length}");
            return ReleasePipeWriteResult.Delivered;
        }
    }

    private sealed class OwnerSession : IDisposable
    {
        private readonly string _root;
        private readonly string _sourcePath;
        private readonly string _stagingPath;
        private readonly string _journalPath;
        private readonly string _barrierPath;
        private readonly string _goPath;
        private readonly string _witnessPath;
        private readonly string _caseId;
        private readonly string _delegatedParent;
        private readonly string _childName;
        private readonly SafeFileHandle _stagingParentAuthority;
        private readonly SafeFileHandle _parentAuthority;
        private readonly PreparedClosure _closure;
        private readonly HeldExecution _held;
        private readonly D4WitnessingTransport _transport;
        private readonly BubblewrapAdapter _launcher = new();
        private readonly D4Clock _clock = new();
        private bool _disposed;

        internal OwnerSession(
            string root, string barrierPath, string goPath, string witnessPath, string caseId)
        {
            _root = root;
            _sourcePath = Path.Combine(root, "source");
            _stagingPath = Path.Combine(root, "staging");
            _journalPath = Path.Combine(root, "journal");
            _barrierPath = barrierPath;
            _goPath = goPath;
            _witnessPath = witnessPath;
            _caseId = caseId;
            Directory.CreateDirectory(_sourcePath);
            Directory.CreateDirectory(_stagingPath);
            Directory.CreateDirectory(_journalPath);
            _delegatedParent = Environment.GetEnvironmentVariable("GAGAMBA_DELEGATED_CGROUP")
                ?? throw new InvalidOperationException("GAGAMBA_DELEGATED_CGROUP is required");
            _childName = "d4-" + Guid.NewGuid().ToString("N")[..12];

            var artifacts = RepositoryRoot();
            File.WriteAllBytes(Path.Combine(_sourcePath, "tool"), File.ReadAllBytes(
                Path.Combine(artifacts, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture")));
            File.WriteAllBytes(Path.Combine(_sourcePath, "input"), new byte[64]);
            File.WriteAllBytes(Path.Combine(_sourcePath, "coordinator"), File.ReadAllBytes(
                Path.Combine(artifacts, "artifacts/linux-m1/wp04/exec-coordinator")));
            File.WriteAllBytes(Path.Combine(_sourcePath, "filter"), File.ReadAllBytes(
                Path.Combine(artifacts, "artifacts/linux-m1/wp04/seccomp-filter.bin")));

            Storage = new D4JournalStorage(new JournalFileStorage(), barrierPath, goPath, witnessPath, caseId);
            Journal = new OwnershipJournal(Storage, _journalPath);
            Require(Journal.TryAcquireWriter(out var writer, out var error), error);
            Writer = writer!;
            D4Witness.Append(_witnessPath, _caseId, "JournalWriterAcquired", "owner",
                Environment.ProcessId.ToString(), baseline: Writer.BaselineSequence);

            var accepted = MonotonicNanoseconds();
            SetupDeadline = accepted + 10_000_000_000L;
            OverallDeadline = accepted + 30_000_000_000L;
            var expiresAt = accepted + 60_000_000_000L;
            var objects = new[]
            {
                new CopyObjectPlan("tool", "tool", 4UL * 1024 * 1024, null),
                new CopyObjectPlan("input", "input", 4UL * 1024 * 1024, null),
                new CopyObjectPlan("coordinator", "coordinator", 4UL * 1024 * 1024, null),
                new CopyObjectPlan("filter", "filter", 4UL * 1024 * 1024, null),
            };
            var environment = Array.Empty<KeyValuePair<string, string>>();

            var stagingErrno = PreparationInterop.OpenDirectory(_stagingPath, out var stagingFd);
            Require(stagingErrno == 0 && stagingFd >= 0, $"staging open errno {stagingErrno}");
            Require(PreparationInterop.StatFd(stagingFd, out var stagingStat) == 0, "staging stat");
            Require(PreparationInterop.IsDirectory(stagingStat.Mode), "staging is not a directory");
            _stagingParentAuthority = new SafeFileHandle((IntPtr)stagingFd, ownsHandle: true);

            var delegatedErrno = PreparationInterop.OpenDirectory(_delegatedParent, out var delegatedFd);
            Require(delegatedErrno == 0 && delegatedFd >= 0, $"delegated parent open errno {delegatedErrno}");
            Require(CgroupInterop.TryVerifyDelegatedParent(delegatedFd, out var verifyError), verifyError);
            _parentAuthority = new SafeFileHandle((IntPtr)delegatedFd, ownsHandle: true);

            var copyPlan = new CopyPlan(
                _sourcePath, _stagingPath, "prep-001", objects,
                16UL * 1024 * 1024, 128, environment, Acknowledge: null, Hooks: null,
                StagingParentAuthority: _stagingParentAuthority);
            var execId = Guid.NewGuid();
            var leaseGeneration = Guid.NewGuid();
            var issuerId = Guid.NewGuid();
            var storeGenId = Guid.NewGuid();
            var prepId = Guid.NewGuid();
            var specification = new OwnerPreparationSpecification(
                Writer.Directory, _childName, execId, leaseGeneration, Boot(), issuerId,
                storeGenId, _sourcePath, _stagingPath, stagingStat.Device, stagingStat.Inode,
                "prep-001", objects, 16UL * 1024 * 1024, 128, environment,
                expiresAt, null, true, accepted);
            var intent = OwnerPreparationSpecificationCodec.BuildIntent(specification);
            Require(Journal.WriteNext(Writer, intent).Ok, "allocation intent write failed");
            Require(ScopedAllocationContext.TryCreate(
                (int)_parentAuthority.DangerousGetHandle(), Journal, Writer, intent,
                execId, leaseGeneration, Boot(), out var context, out var contextError), contextError);
            Require(CgroupInterop.TryCreateDomain(context!, _childName,
                out var backend, out var domainIdentity, out var domainError), domainError);
            PrimaryLease = new DomainLease(backend!, domainIdentity!);
            RecoveryLease = new DomainLease((CgroupBackend)backend!.DuplicateForRecovery(), domainIdentity!);
            var registered = MirrorRegistered(specification, domainIdentity!);
            Require(Journal.WriteNext(Writer, registered).Ok, "registered snapshot write failed");
            Require(PrimaryLease.TryInstallWp04Limits(out var limitsError), limitsError);
            var copy = SafeCopyIn.Prepare(copyPlan, LibcCopyHost.Instance, CancellationToken.None);
            Require(copy.Succeeded, copy.Detail);
            _closure = copy.Closure!;
            var descriptions = _closure.Objects.Select(staged => new StagedObjectDescription(
                staged.Role, staged.Sha256, staged.Length, staged.Device, staged.Inode, staged.StagingName)).ToArray();
            Manifest = BuildManifest(descriptions);
            Identity = new PreparationIdentity(
                execId, issuerId, storeGenId, prepId, ManifestCodec.ComputeDigestBytes(Manifest));
            var prepared = registered with
            {
                Stage = PreparationStage.Prepared,
                Preparation = new RecordedPreparation(
                    Identity.ExecutionId, Identity.IssuerId, Identity.GenerationId,
                    Identity.PreparationId, Identity.ManifestDigest.ToArray()),
            };
            Require(Journal.WriteNext(Writer, prepared).Ok, "prepared snapshot write failed");
            var store = new PreparedTransaction.PreparationStore(issuerId, storeGenId, _clock);
            var publication = PreparationPublisher.Publish(
                store, _closure, Manifest, Identity, expiresAt, _ => true, CancellationToken.None);
            Require(publication.Published, publication.Detail);
            Capability = publication.Capability!;
            var held = _launcher
                .PrepareHeldAsync(PrimaryLease, _closure, Manifest, Identity,
                    SetupDeadline, OverallDeadline, CancellationToken.None)
                .GetAwaiter().GetResult();            Require(held.Held && held.Execution is not null, held.Error);
            _held = held.Execution!;
            _transport = new D4WitnessingTransport(_held, _witnessPath, _caseId, _barrierPath, _goPath);
            Arbiter = new ReleaseArbiter(
                Capability, Journal, Writer, _clock,
                () => new ReleasePrerequisites(
                    PrimaryLease.TryReadWp04Limits(out _, out _),
                    _held.CoordinatorStatusReader is { IsClosed: false, IsInvalid: false },
                    Environment.ProcessId > 0,
                    RecoveryLease.AuthorityAlive),
                _transport);
            Arbiter.TestBarrier = name =>
            {
                if (BarrierName == "none" || name != BarrierName) return;
                D4Barrier.Park(_barrierPath, _goPath, name);
            };
        }

        internal OwnershipJournal Journal { get; }
        internal JournalWriterLease Writer { get; }
        internal DomainLease PrimaryLease { get; }
        internal DomainLease RecoveryLease { get; }
        internal PreparedTransaction Capability { get; }
        internal RestrictedManifest Manifest { get; }
        internal PreparationIdentity Identity { get; }
        internal long SetupDeadline { get; }
        internal long OverallDeadline { get; }
        internal ReleaseArbiter Arbiter { get; }
        internal D4JournalStorage Storage { get; }
        internal string BarrierName { get; set; } = "none";
        internal int Attempts => _transport.Attempts;
        internal int? CoordinatorExit { get; private set; }
        internal ReleaseRequest Request => new(Identity, SetupDeadline, OverallDeadline);
        internal long LatestSequence() => Journal.ReadLatest().Snapshot?.Sequence ?? -1;
        internal string ChildName => _childName;
        internal string ControlFd
        {
            get
            {
                var controlField = typeof(HeldExecution).GetField("_controlWriter", BindingFlags.NonPublic | BindingFlags.Instance);
                var handle = controlField?.GetValue(_held) as SafeFileHandle;
                return handle is null ? "unknown" : handle.DangerousGetHandle().ToInt32().ToString();
            }
        }

        internal void Ready()
        {
            D4Result.Write(Path.Combine(_root, "owner.ready"), new Dictionary<string, string>
            {
                ["owner_pid"] = Environment.ProcessId.ToString(),
                ["owner_start"] = StartIdentity(Environment.ProcessId),
                ["journal"] = _journalPath,
                ["source"] = _sourcePath,
                ["staging"] = _stagingPath,
                ["child"] = _childName,
                ["identity_exec"] = Identity.ExecutionId.ToString(),
                ["identity_prep"] = Identity.PreparationId.ToString(),
                ["setup"] = SetupDeadline.ToString(),
                ["overall"] = OverallDeadline.ToString(),
                ["control_fd"] = ControlFd,
                ["journal_seq"] = LatestSequence().ToString(),
            });
            D4Witness.Append(_witnessPath, _caseId, "OwnerPrepared", "owner",
                Environment.ProcessId.ToString(), baseline: LatestSequence());
        }

        internal void RecordCoordinatorExit()
        {
            if (_held.WrapperReaper.TryReap(5000, out var exitCode, out _))
                CoordinatorExit = exitCode;
            D4Witness.Append(_witnessPath, _caseId, "CoordinatorExitObserved", "owner",
                Environment.ProcessId.ToString(), result: CoordinatorExit?.ToString() ?? "unknown");
        }

        private static string Boot() => CgroupInterop.TryReadBootId(out var id, out _) ? id! : "unknown";

        private static string StartIdentity(int pid)
        {
            try
            {
                var stat = File.ReadAllText($"/proc/{pid}/stat");
                var close = stat.LastIndexOf(')');
                var fields = stat[(close + 2)..].Split(' ');
                return $"{pid}:{fields[19]}";
            }
            catch
            {
                return $"{pid}:unknown";
            }
        }

        private static OwnershipSnapshot MirrorRegistered(
            OwnerPreparationSpecification specification, DomainIdentity domain)
        {
            var allocations = OwnerPreparationSpecificationCodec.PlannedAllocations(specification).ToList();
            for (var index = 0; index < allocations.Count; index++)
            {
                if (allocations[index].Kind == AllocationKind.Domain)
                {
                    allocations[index] = allocations[index] with
                    {
                        Device = domain.ChildDevice,
                        Inode = domain.ChildInode,
                        Confirmed = true,
                    };
                }
            }

            return new OwnershipSnapshot(
                OwnershipSnapshotCodec.LegacyVersion, 0, specification.ExecutionId,
                specification.BootId, domain.CgroupMountId, domain.CgroupMountFsType,
                domain.ParentDevice, domain.ParentInode, domain.ChildName,
                domain.ChildDevice, domain.ChildInode, specification.LeaseGeneration,
                OwnershipWriterKind.Owner, OwnershipCleanupState.NotStarted,
                PreparationStage.AllocationRegistered, specification.StagingParentDevice,
                specification.StagingParentInode, allocations, Preparation: null);
        }

        private static RestrictedManifest BuildManifest(IReadOnlyList<StagedObjectDescription> descriptions)
        {
            var baseline = SampleManifest();
            var closureObjects = descriptions.Select(item => new ClosureObject(
                item.Role, item.Sha256, item.Length,
                item.Role == "filter" ? "fd:6" : "/" + item.Role,
                item.Role == "filter" ? "p07-seccomp-filter" : "static-test-object"));
            return new RestrictedManifest(
                RestrictedManifest.CurrentSchemaVersion,
                RestrictedManifest.ImplementationIdentifier,
                baseline.Authority,
                baseline.Resources,
                new ClosureIdentity(closureObjects, new[] { "bubblewrap-0.11.1:" + HelperInstallation.PinnedSha256 }),
                baseline.Invocation,
                baseline.Channels);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                _launcher.AbortAsync(_held, PrimaryLease, CancellationToken.None)
                    .GetAwaiter().GetResult();
            }
            catch
            {
            }

            try { _held.Dispose(); } catch { }
            try { PrimaryLease.Dispose(); } catch { }
            try { RecoveryLease.Dispose(); } catch { }
            try { Capability.Dispose(); } catch { }
            try { Writer.Dispose(); } catch { }
            try { _stagingParentAuthority.Dispose(); } catch { }
            try { _parentAuthority.Dispose(); } catch { }
            try { _closure.Dispose(); } catch { }
            CleanupCgroup(_delegatedParent, _childName);
        }

        private static void Require(bool condition, string error)
        {
            if (!condition) throw new InvalidOperationException(error);
        }
    }

    private sealed class D4WitnessingTransport : IReleaseWriteChannel
    {
        private readonly HeldExecution _held;
        private readonly string _witnessPath;
        private readonly string _caseId;
        private readonly string _barrierPath;
        private readonly string _goPath;
        public int Attempts { get; private set; }

        public D4WitnessingTransport(
            HeldExecution held, string witnessPath, string caseId, string barrierPath, string goPath)
        {
            _held = held;
            _witnessPath = witnessPath;
            _caseId = caseId;
            _barrierPath = barrierPath;
            _goPath = goPath;
        }

        public ReleasePipeWriteResult TryWriteOnce(ReadOnlyMemory<byte> frame)
        {
            Attempts++;
            D4Witness.Append(_witnessPath, _caseId, "PreWriteWitnessAcknowledged", "owner",
                Environment.ProcessId.ToString(), result: $"attempt={Attempts};bytes={frame.Length}");
            if (!string.IsNullOrEmpty(_barrierPath) && File.Exists(_goPath) == false
                && Environment.GetEnvironmentVariable("D4_WITNESS_GATE") == "1")
            {
                D4Barrier.Announce(_barrierPath, "witness-pre-write", Environment.ProcessId, -1);
                D4Barrier.WaitForGo(_goPath, TimeSpan.FromSeconds(30));
            }

            ReleasePipeWriteResult result;
            try
            {
                result = _held.TryWriteRelease(frame);
            }
            catch (Exception ex)
            {
                result = ReleasePipeWriteResult.Unknown;
                D4Witness.Append(_witnessPath, _caseId, "WriteOutcomeUnknown", "owner",
                    Environment.ProcessId.ToString(), result: ex.GetType().Name);
                return result;
            }

            D4Witness.Append(_witnessPath, _caseId, "WriteReturned", "owner",
                Environment.ProcessId.ToString(), result: result.ToString());
            return result;
        }
    }

    internal sealed class D4JournalStorage : IJournalStorage
    {
        private readonly IJournalStorage _inner;
        private readonly string _barrierPath;
        private readonly string _goPath;
        private readonly string _witnessPath;
        private readonly string _caseId;
        private string _gateStep = string.Empty;
        private bool _armed;
        private bool _gateConsumed;
        private bool _sealFault;
        private bool _stopFault;
        private bool _stopFaultConsumed;

        public D4JournalStorage(
            IJournalStorage inner, string barrierPath, string goPath, string witnessPath, string caseId)
        {
            _inner = inner;
            _barrierPath = barrierPath;
            _goPath = goPath;
            _witnessPath = witnessPath;
            _caseId = caseId;
        }

        public void Arm(string gateStep, string fault)
        {
            _gateStep = gateStep;
            _sealFault = fault == "ambiguous-seal";
            _stopFault = false;
            _armed = true;
        }

        public void ArmStopFailure() => _stopFault = true;

        private JournalIoResult? Gate(string step, string name)
        {
            if (!_armed || _gateConsumed) return null;
            if (!string.Equals(_gateStep, step + ":" + name, StringComparison.Ordinal)) return null;
            _gateConsumed = true;
            D4Witness.Append(_witnessPath, _caseId, "StorageGateReached", "owner",
                Environment.ProcessId.ToString(), result: step + ":" + name);
            D4Barrier.Announce(_barrierPath, "storage:" + step + ":" + name, Environment.ProcessId, -1);
            D4Barrier.WaitForGo(_goPath, TimeSpan.FromSeconds(60));
            return null;
        }

        public JournalIoResult CreateExclusive(string directory, string name)
        {
            Gate("create", name);
            return _inner.CreateExclusive(directory, name);
        }

        public JournalIoResult WriteAll(string directory, string name, byte[] content)
        {
            Gate("write", name);
            if (_stopFault && !_stopFaultConsumed && name == OwnershipJournal.TempName)
            {
                _stopFaultConsumed = true;
                D4Witness.Append(_witnessPath, _caseId, "StopDurableWriteFailed", "test-only-storage",
                    Environment.ProcessId.ToString());
                return JournalIoResult.Failure("D4 stop durable write failure");
            }

            return _inner.WriteAll(directory, name, content);
        }

        public JournalIoResult FsyncFile(string directory, string name)
        {
            Gate("fsyncfile", name);
            return _inner.FsyncFile(directory, name);
        }

        public JournalIoResult Rename(string directory, string sourceName, string targetName)
        {
            Gate("rename", targetName);
            return _inner.Rename(directory, sourceName, targetName);
        }

        public JournalIoResult FsyncDirectory(string directory)
        {
            Gate("fsyncdir", directory);
            return _inner.FsyncDirectory(directory);
        }

        public JournalIoResult Unlink(string directory, string name)
        {
            Gate("unlink", name);
            if (_sealFault && name == OwnershipJournal.SealName)
            {
                _sealFault = false;
                D4Witness.Append(_witnessPath, _caseId, "AmbiguousSealFailureInjected", "test-only-storage",
                    Environment.ProcessId.ToString());
                return JournalIoResult.Failure("D4 ambiguous seal release failure");
            }

            return _inner.Unlink(directory, name);
        }

        public bool Exists(string directory, string name) => _inner.Exists(directory, name);
        public byte[]? ReadAll(string directory, string name) => _inner.ReadAll(directory, name);
        public JournalIoResult TryAcquireExclusiveLock(string directory, string name) =>
            _inner.TryAcquireExclusiveLock(directory, name);
        public JournalIoResult ReleaseExclusiveLock(string directory, string name) =>
            _inner.ReleaseExclusiveLock(directory, name);
        public bool IsLockHeld(string directory, string name) => _inner.IsLockHeld(directory, name);
    }
}

internal static class D4Barrier
{
    internal static void Announce(string path, string name, int pid, long sequence)
    {
        if (string.IsNullOrEmpty(path)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        D4Result.Write(path, new Dictionary<string, string>
        {
            ["name"] = name,
            ["pid"] = pid.ToString(),
            ["sequence"] = sequence.ToString(),
        });
    }

    internal static void Park(string barrierPath, string goPath, string name)
    {
        Announce(barrierPath, name, Environment.ProcessId, -1);
        WaitForGo(goPath, TimeSpan.FromSeconds(120));
    }

    internal static void WaitForGo(string goPath, TimeSpan timeout)
    {
        if (string.IsNullOrEmpty(goPath)) throw new InvalidOperationException("go path is required");
        var deadline = DateTime.UtcNow + timeout;
        while (!File.Exists(goPath))
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException($"timed out waiting for {goPath}");
            Thread.Sleep(20);
        }
    }
}

internal static class D4Result
{
    internal static void Write(string path, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrEmpty(path)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, values.Select(item => $"{item.Key}={item.Value}"));
    }

    internal static Dictionary<string, string> Read(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(path))
        {
            var index = line.IndexOf('=');
            if (index <= 0) continue;
            result[line[..index]] = line[(index + 1)..];
        }

        return result;
    }
}

internal static class D4Witness
{
    internal static void Append(
        string path, string caseId, string eventName, string source,
        string? pid = null, string? result = null, long baseline = -1)
    {
        if (string.IsNullOrEmpty(path)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var line = $"case={OneLine(caseId)} ts={DateTime.UtcNow:O} event={OneLine(eventName)} source={OneLine(source)} "
            + $"pid={pid ?? "unknown"} baseline={baseline} result={OneLine(result ?? string.Empty)}";
        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        using var writer = new StreamWriter(stream);
        writer.WriteLine(line);
        writer.Flush();
        stream.Flush(flushToDisk: true);
    }

    internal static string[] Read(string path) =>
        File.Exists(path) ? File.ReadAllLines(path) : Array.Empty<string>();

    private static string OneLine(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ');
}
