using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Gagamba.Execution.Linux.M1.Bootstrap;

using Fact = Gagamba.Execution.Linux.M1.Bootstrap.ValidationCustodyFact;
using Event = Gagamba.Execution.Linux.M1.Bootstrap.ValidationCustodyEvent;
using State = Gagamba.Execution.Linux.M1.Bootstrap.ValidationCustodyState;
using Refusal = Gagamba.Execution.Linux.M1.Bootstrap.ValidationCustodyRefusal;
using Model = Gagamba.Execution.Linux.M1.Bootstrap.ValidationCustodyModel;
using Binding = Gagamba.Execution.Linux.M1.Bootstrap.ValidationCustodyBinding;
using Evidence = Gagamba.Execution.Linux.M1.Bootstrap.ValidationCustodyEvidence;
using Expectations = Gagamba.Execution.Linux.M1.Bootstrap.ValidationCustodyExpectations;
using Snapshot = Gagamba.Execution.Linux.M1.Bootstrap.ValidationCustodyModelSnapshot;

internal static partial class Probe
{
    private static readonly Event[] Events =
        [Event.ObserveIssue, Event.ObserveOffer, Event.ObserveDrop, Event.ObserveConsume, Event.Complete];
    private static readonly State[] Destinations =
        [State.IssuedModel, State.OfferedModel, State.DroppedModel, State.ConsumedModel, State.Closed];
    private static readonly int[] Due = [0, 0, 0, 0, 0, 1, 1, 2, 3, 4, 5];
    private static readonly string[] DigestNames =
        ["launcher-image", "launcher-closure", "controller-image", "controller-closure",
         "driver-image", "driver-closure", "artifact-set", "catalog", "manifest", "policy", "script",
         "review", "owner-receipt", "approval", "capability"];
    private static readonly string[] Premises =
        ["protected-inputs", "provenance", "approval-current", "original-association", "live-originals",
         "fresh-authorization", "issuance", "offer", "drop", "consumption", "cleanup"];
    private static int passed, failed;

    private sealed record Vector
    {
        internal ValidationPreContext Pre { get; init; } = new(3, ValidationPreTopology.B2);
        internal Gava1Ids Ids { get; init; } = new(Label(701), Label(702), Label(703));
        internal Guid Host { get; init; } = Label(704);
        internal Guid Boot { get; init; } = Label(705);
        internal Guid Selection { get; init; } = Label(706);
        internal Guid L { get; init; } = Label(707);
        internal Guid C { get; init; } = Label(708);
        internal byte[]?[] Bytes { get; } = DigestNames.Select(n => (byte[]?)Digest(n)).ToArray();
        internal string Case { get; init; } = "Q1.original_0501";
        internal long Admit { get; init; } = 20;
        internal long Session { get; init; } = 80;
        internal string? Qid { get; init; }
        internal string? Qhash { get; init; }

        internal Binding Bind() => new(Pre, Ids, Host, Boot, Selection, L, C, Bytes[0], Bytes[1],
            Bytes[2], Bytes[3], Bytes[4], Bytes[5], Bytes[6], Bytes[7], Bytes[8], Bytes[9], Bytes[10],
            Bytes[11], Bytes[12], Bytes[13], Bytes[14], Case, Admit, Session, Qid, Qhash);
    }

    private static Guid Label(int n) => new(n, 0x31bc, 0x51d9, [0x90, 2, 93, 17, 8, 127, 199, 49]);
    private static byte[] Digest(string s) => SHA256.HashData(Encoding.UTF8.GetBytes("Q1 custody synthetic / " + s));
    private static Vector Witness(int topology) => new() { Pre = new(3, (ValidationPreTopology)topology) };
    private static Fact[] Facts(int step) =>
        Due.Select(first => step >= first ? Fact.Consistent : Fact.Unknown).ToArray();
    private static Evidence Observe(Binding binding, int step, (int Index, Fact Value)? change = null)
    {
        var f = Facts(step);
        if (change is { } c)
            f[c.Index] = c.Value;
        return new(binding, f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10]);
    }

    private static void Check(bool ok, string detail)
    {
        if (!ok)
            throw new InvalidOperationException(detail);
    }

    private static void Case(string id, Action test)
    {
        try
        {
            test();
            passed++;
            Console.WriteLine($"{id}\tPASS");
        }
        catch (Exception error)
        {
            failed++;
            string detail = error.GetBaseException().Message.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
            Console.WriteLine($"{id}\tFAIL\t{error.GetType().Name}: {detail}");
        }
    }

    private static Model Create(Vector expectation, Binding observation, long now = 0)
    {
        var code = Model.TryCreate(new(expectation.Bind()), Observe(observation, 0), now, false, out var model);
        Check(code == Refusal.None && model is not null, $"create returned {code}, model={(model is not null)}");
        return model!;
    }

    private static void RefuseCreate(Refusal expected, Expectations? expectations, Evidence? evidence,
        long now = 0, bool stop = false)
    {
        var code = Model.TryCreate(expectations, evidence, now, stop, out var model);
        using (model)
            Check(code == expected && model is null, $"create expected {expected}/null, got {code}, model={(model is not null)}");
    }

    private static Snapshot Succeed(Model model, int eventIndex, Evidence evidence, long now)
    {
        var result = model.Advance(Events[eventIndex], evidence, now, false, out var snapshot);
        Check(result == Refusal.None && snapshot?.State == Destinations[eventIndex],
            $"{Events[eventIndex]} expected {Destinations[eventIndex]}, got {result}/{snapshot?.State}");
        return snapshot!;
    }

    private static Model At(Vector expectation, Binding observation, int completed)
    {
        var model = Create(expectation, observation);
        for (int index = 0; index < completed; index++)
            Succeed(model, index, Observe(observation, index + 1), index + 1);
        return model;
    }

    private static void Refuse(Model model, Event evt, Evidence? evidence, long now, bool stop,
        Refusal expected, Binding original, int step)
    {
        var actual = model.Advance(evt, evidence, now, stop, out var snapshot);
        Check(actual == expected && snapshot is null,
            $"{evt} expected {expected}/null, got {actual}, snapshot={snapshot?.State}");
        Terminal(model, original, step);
    }

    private static void Terminal(Model model, Binding original, int step)
    {
        int retry = Math.Min(step, 4);
        var result = model.Advance(Events[retry], Observe(original, retry + 1), step + 6, false, out var output);
        Check(result == Refusal.Transition && output is null, $"terminal retry got {result}/{output?.State}");
    }

    private static void Structural(int topology)
    {
        var canonical = Witness(topology);
        var original = Witness(topology).Bind();
        Case($"R02/R07/{topology}/nominal", () =>
        {
            using var model = Create(canonical, original);
            for (int i = 0; i < Events.Length; i++)
                Succeed(model, i, Observe(original, i + 1), 0);
            Terminal(model, original, 5);
        });
        Case($"R02/{topology}/missing-expectations", () => RefuseCreate(Refusal.Context, null, Observe(original, 0)));
        Case($"R02/{topology}/missing-expected-binding", () => RefuseCreate(Refusal.Context, new(null!), Observe(original, 0)));
        Case($"R02/{topology}/missing-evidence", () => RefuseCreate(Refusal.Context, new(canonical.Bind()), null));
        Case($"R02/{topology}/missing-observed-binding", () => RefuseCreate(Refusal.Context, new(canonical.Bind()), Observe(null!, 0)));
        var invalid = new (string, Vector)[]
        {
            ("version", Witness(topology) with { Pre = new(2, canonical.Pre.Topology) }),
            ("topology", Witness(topology) with { Pre = new(3, (ValidationPreTopology)77) }),
            ("host", Witness(topology) with { Host = Guid.Empty }),
            ("boot", Witness(topology) with { Boot = Guid.Empty }),
            ("selection", Witness(topology) with { Selection = Guid.Empty }),
            ("launcher", Witness(topology) with { L = Guid.Empty }),
            ("controller", Witness(topology) with { C = Guid.Empty }),
            ("equal-originals", Witness(topology) with { C = canonical.L }),
            ("session", Witness(topology) with { Ids = canonical.Ids with { Session = Guid.Empty } }),
            ("execution", Witness(topology) with { Ids = canonical.Ids with { Execution = Guid.Empty } }),
            ("lease", Witness(topology) with { Ids = canonical.Ids with { Lease = Guid.Empty } }),
            ("null-case", Witness(topology) with { Case = null! }),
            ("empty-case", Witness(topology) with { Case = "" }),
            ("overlong-case", Witness(topology) with { Case = new string('Z', 129) }),
            ("leading-dot", Witness(topology) with { Case = ".q" }),
            ("trailing-dot", Witness(topology) with { Case = "q." }),
            ("unicode-case", Witness(topology) with { Case = "case-é" }),
            ("space", Witness(topology) with { Case = "case id" }),
            ("qualification-id", Witness(topology) with { Qid = "" }),
            ("qualification-hash", Witness(topology) with { Qhash = "" }),
            ("admit-zero", Witness(topology) with { Admit = 0 }),
            ("session-earlier", Witness(topology) with { Session = 19 }),
        };
        foreach (var (field, vector) in invalid)
        {
            Case($"R02/{topology}/{field}/expected", () =>
                RefuseCreate(Refusal.Context, new(vector.Bind()), Observe(original, 0)));
            Case($"R02/{topology}/{field}/observed", () =>
            {
                var bad = vector.Bind();
                RefuseCreate(Refusal.Context, new(canonical.Bind()), Observe(bad, 0));
                using var model = At(canonical, original, 0);
                Refuse(model, Events[0], Observe(bad, 1), 1, false, Refusal.Context, original, 0);
            });
        }
        foreach (string goodCase in new[] { "_", new string('z', 127) + "-", "x..Y" })
        {
            Case($"R02/{topology}/case-valid-{goodCase.Length}", () =>
            {
                var independent = Witness(topology) with { Case = goodCase };
                using var model = Create(independent, (Witness(topology) with { Case = goodCase }).Bind());
            });
        }

        for (int i = 0; i < DigestNames.Length; i++)
        {
            string field = DigestNames[i];
            foreach (int length in new[] { -1, 0, 31, 33, 4096 })
            {
                Case($"R02/{topology}/{field}/{length}", () =>
                {
                    var bad = Witness(topology);
                    bad.Bytes[i] = length < 0 ? null : new byte[length];
                    RefuseCreate(Refusal.Context, new(bad.Bind()), Observe(original, 0));
                    using var model = At(canonical, original, 0);
                    Refuse(model, Events[0], Observe(bad.Bind(), 1), 1, false, Refusal.Context, original, 0);
                });
            }
            Case($"R03-R04/{topology}/{field}", () =>
            {
                var other = Witness(topology);
                other.Bytes[i] = Digest("contradiction " + field);
                AgainstOriginal(canonical, original, other.Bind());
            });
        }

        var different = new (string, Vector)[]
        {
            ("topology", Witness(3 - topology)),
            ("host", Witness(topology) with { Host = Label(801) }),
            ("boot", Witness(topology) with { Boot = Label(802) }),
            ("selection", Witness(topology) with { Selection = Label(803) }),
            ("launcher", Witness(topology) with { L = Label(804) }),
            ("controller", Witness(topology) with { C = Label(805) }),
            ("originals-swapped", Witness(topology) with { L = canonical.C, C = canonical.L }),
            ("session", Witness(topology) with { Ids = canonical.Ids with { Session = Label(806) } }),
            ("execution", Witness(topology) with { Ids = canonical.Ids with { Execution = Label(807) } }),
            ("lease", Witness(topology) with { Ids = canonical.Ids with { Lease = Label(808) } }),
            ("ordinal-case", Witness(topology) with { Case = canonical.Case.ToLowerInvariant() }),
            ("admit", Witness(topology) with { Admit = 22 }),
            ("session-bound", Witness(topology) with { Session = 82 }),
        };
        foreach (var (field, vector) in different)
            Case($"R05/{topology}/{field}", () => AgainstOriginal(canonical, original, vector.Bind()));
    }

    private static void AgainstOriginal(Vector expected, Binding original, Binding contradictory)
    {
        RefuseCreate(Refusal.Comparison, new(expected.Bind()), Observe(contradictory, 0));
        using var model = At(expected, original, 0);
        Refuse(model, Events[0], Observe(contradictory, 1), 1, false, Refusal.Comparison, original, 0);
    }

    private static void PremisesAndTransitions()
    {
        var expected = Witness(2);
        var original = Witness(2).Bind();
        for (int stage = 0; stage <= 5; stage++)
        {
            for (int i = 0; i < Due.Length; i++)
            {
                foreach (Fact replacement in new[] { Fact.Unknown, Fact.Consistent, Fact.Contradictory, (Fact)3, (Fact)255 })
                {
                    if (replacement == Facts(stage)[i])
                        continue;
                    Case($"R06/{stage}/{Premises[i]}/{(int)replacement}", () =>
                    {
                        var invalid = Observe(original, stage, (i, replacement));
                        Refusal refusal = (byte)replacement > 2 ? Refusal.Context : Refusal.Evidence;
                        if (stage == 0)
                            RefuseCreate(refusal, new(expected.Bind()), invalid);
                        else
                        {
                            using var model = At(expected, original, stage - 1);
                            Refuse(model, Events[stage - 1], invalid, stage, false, refusal, original, stage - 1);
                        }
                    });
                }
            }
        }

        for (int stage = 0; stage <= 5; stage++)
        {
            foreach (Event evt in Events.Concat([(Event)0, (Event)6, (Event)255]))
            {
                if (stage < Events.Length && evt == Events[stage])
                    continue;
                Case($"R07-R08/{stage}/{(byte)evt}", () =>
                {
                    using var model = At(expected, original, stage);
                    Refuse(model, evt, Observe(original, Math.Min(stage + 1, 5)), stage + 1, false,
                        Refusal.Transition, original, stage);
                });
            }
        }
        Case("R07/repeated-issue-with-issue-facts", () =>
        {
            using var model = At(expected, original, 1);
            Refuse(model, Event.ObserveIssue, Observe(original, 1), 2, false, Refusal.Transition, original, 1);
        });
        Case("R08/consume-before-drop-with-all-premises", () =>
        {
            using var model = At(expected, original, 2);
            Refuse(model, Event.ObserveConsume, Observe(original, 4), 3, false, Refusal.Transition, original, 2);
        });
    }

    private static void TimeAndPrecedence()
    {
        var expected = Witness(1);
        var original = Witness(1).Bind();
        var other = (Witness(1) with { C = Label(930) }).Bind();

        var create = new (string, long, bool, Binding, (int, Fact)?, Refusal)[]
        {
            ("negative-beats-stop", long.MinValue, true, other, (2, Fact.Unknown), Refusal.Context),
            ("enum-beats-stop", 20, true, other, (2, (Fact)255), Refusal.Context),
            ("stop-beats-expiry", 20, true, other, (2, Fact.Unknown), Refusal.Stopped),
            ("expiry-beats-comparison", 20, false, other, (2, Fact.Unknown), Refusal.Expired),
            ("comparison-beats-evidence", 19, false, other, (2, Fact.Unknown), Refusal.Comparison),
            ("evidence-alone", 19, false, original, (2, Fact.Unknown), Refusal.Evidence),
        };
        foreach (var (name, time, stop, binding, change, refusal) in create)
        {
            Case($"R09-R11/create/{name}", () =>
                RefuseCreate(refusal, new(expected.Bind()), Observe(binding, 0, change), time, stop));
            Case($"R09-R11/advance/{name}", () =>
            {
                using var model = At(expected, original, 0);
                Refuse(model, Event.ObserveIssue, Observe(binding, 1, change), time, stop, refusal, original, 0);
            });
        }

        Case("R11/advance/transition-beats-all", () =>
        {
            using var model = At(expected, original, 0);
            Refuse(model, Event.Complete, null, long.MinValue, true, Refusal.Transition, original, 0);
        });
        Case("R09/advance/backward-nonnegative", () =>
        {
            using var model = Create(expected, original, 10);
            Refuse(model, Event.ObserveIssue, Observe(original, 1), 9, true, Refusal.Context, original, 0);
        });
        foreach (int stage in Enumerable.Range(0, 5))
        {
            long limit = stage == 4 ? expected.Session : expected.Admit;
            Case($"R09/advance/{stage}/exact-bound", () =>
            {
                using var model = At(expected, original, stage);
                Refuse(model, Events[stage], Observe(original, stage + 1), limit, false,
                    Refusal.Expired, original, stage);
            });
            Case($"R09/advance/{stage}/before-bound", () =>
            {
                using var model = At(expected, original, stage);
                Succeed(model, stage, Observe(original, stage + 1), limit - 1);
            });
        }
        Case("R09/max-int64", () =>
        {
            var held = expected with { Admit = long.MaxValue, Session = long.MaxValue };
            var seen = (Witness(1) with { Admit = long.MaxValue, Session = long.MaxValue }).Bind();
            using var model = Create(held, seen, long.MaxValue - 1);
            for (int i = 0; i < 4; i++)
                Succeed(model, i, Observe(seen, i + 1), long.MaxValue - 1);
            Refuse(model, Event.Complete, Observe(seen, 5), long.MaxValue, false, Refusal.Expired, seen, 4);
        });
    }

    private static byte[][] Extract(Binding b) =>
        [b.LauncherExecutable, b.LauncherClosure, b.ControllerExecutable, b.ControllerClosure,
         b.DriverExecutable, b.DriverClosure, b.ArtifactSet, b.Catalog, b.Manifest, b.Policy, b.Script,
         b.Review, b.OwnerReceipt, b.Approval, b.Capability];

    private static void OwnershipWithdrawalAndLifetime()
    {
        var expected = Witness(1);
        var original = Witness(1).Bind();
        Case("R13/all-owned-buffers", () =>
        {
            var independent = Witness(1);
            var suspect = Witness(1);
            var left = independent.Bind();
            var right = suspect.Bind();
            var expectation = new Expectations(left);
            var seen = Observe(right, 0);
            foreach (var bytes in independent.Bytes.Concat(suspect.Bytes))
                Array.Fill(bytes!, (byte)0xcc);
            foreach (var bytes in Extract(left).Concat(Extract(right)).Concat(Extract(expectation.Binding))
                         .Concat(Extract(seen.Binding)))
                Array.Fill(bytes, (byte)0xdd);
            var result = Model.TryCreate(expectation, seen, 0, false, out var model);
            using (model)
            {
                Check(result == Refusal.None && model is not null, $"ownership create {result}");
                var issued = Succeed(model!, 0, Observe(original, 1), 1);
                var before = Extract(issued.Binding);
                foreach (var bytes in before)
                    Array.Fill(bytes, (byte)0xee);
                var offered = Succeed(model!, 1, Observe(original, 2), 2);
                foreach (var snapshot in new[] { issued, offered })
                    Check(Extract(snapshot.Binding).Zip(DigestNames).All(p => p.First.SequenceEqual(Digest(p.Second))),
                        "changed snapshot digest");
                Check(Snapshot.EvidenceKind == "supplied-structural-model", "diagnostic marker");
            }
        });

        Case("R15/withdraw-before-issue", () =>
        {
            using var model = At(expected, original, 0);
            Refuse(model, Event.ObserveIssue, Observe(original, 1, (2, Fact.Contradictory)), 1, false,
                Refusal.Evidence, original, 0);
        });
        Case("R15/original-after-issue", () =>
        {
            using var model = At(expected, original, 1);
            Succeed(model, 1, Observe(original, 2), 2);
            var replacement = Witness(1);
            replacement.Bytes[13] = Digest("newly published approval");
            Refuse(model, Event.ObserveDrop, Observe(replacement.Bind(), 3), 3, false,
                Refusal.Comparison, original, 2);
        });
        Case("R12/R24/dispose-twice", () =>
        {
            using var model = At(expected, original, 2);
            model.Dispose();
            model.Dispose();
            Terminal(model, original, 2);
        });
        Case("R12/R24/null-observation-clears", () =>
        {
            using var model = At(expected, original, 2);
            Refuse(model, Event.ObserveDrop, null, 3, false, Refusal.Context, original, 2);
        });
        Case("R14/duplicate-issue-race", () =>
        {
            using var model = At(expected, original, 0);
            var evidence = Observe(original, 1);
            using var ready = new CountdownEvent(2);
            using var fire = new ManualResetEventSlim();
            Task<(Refusal Result, Snapshot? Out)> Start() => Task.Factory.StartNew(() =>
            {
                ready.Signal();
                Check(fire.Wait(TimeSpan.FromSeconds(10)), "start barrier timeout");
                var result = model.Advance(Event.ObserveIssue, evidence, 1, false, out var output);
                return (result, output);
            }, TaskCreationOptions.LongRunning);
            var left = Start();
            var right = Start();
            try { Check(ready.Wait(TimeSpan.FromSeconds(10)), "ready barrier timeout"); }
            finally { fire.Set(); }
            Check(Task.WaitAll([left, right], TimeSpan.FromSeconds(10)), "contender deadline");
            var outcomes = new[] { left.Result, right.Result };
            Check(outcomes.Count(x => x.Result == Refusal.None && x.Out?.State == State.IssuedModel) == 1 &&
                outcomes.Count(x => x.Result == Refusal.Transition && x.Out is null) == 1,
                "duplicate outcomes " + string.Join(", ", outcomes.Select(x => $"{x.Result}/{x.Out?.State}")));
            Terminal(model, original, 1);
        });
        Case("R14/dispose-happens-before", () =>
        {
            using var model = At(expected, original, 1);
            var disposed = Task.Run(model.Dispose);
            Check(disposed.Wait(TimeSpan.FromSeconds(10)), "dispose deadline");
            Terminal(model, original, 1);
        });
    }

    private static int Main(string[] args)
    {
        Console.WriteLine("id\tresult\tdetail");
        Console.WriteLine($"core\t{Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(Gava1Ids).Assembly.Location))).ToLowerInvariant()}");
        Structural(1);
        Structural(2);
        PremisesAndTransitions();
        TimeAndPrecedence();
        OwnershipWithdrawalAndLifetime();
#if !DERIVATIVE
        Composition(args);
#endif
        Console.WriteLine($"TOTAL\t{passed}\t{failed}");
        return failed == 0 ? 0 : 1;
    }
}
