# Qualified HELD constructor remediation — P3-R1

Date: 2026-11-06 (Asia/Manila)
Remediation slice: GAGAMBA-RC2B-06-B6-ALT-B-P3-R1
Baseline source SHA: `982a14373e25ed9cde50533e5d6e4fcb30094185`
Baseline tree: `ef5e88ff7e5870aff6678bdff74854dd57975b40`
Final source SHA: `18a788da2f2f50625468f90feb144b9b9a193af2`
Final source tree: `06933471948165a1532880be42cc1961f9fa6519`
Branch: canonical `main`
Host OS/version: Windows 11, 10.0.26200
Architecture: x64 / win-x64
Runtime/SDK: .NET runtime 10.0.12; SDK 10.0.401; MSBuild 18.9.11
Native prerequisites: no protected Linux installation, native owner, domain or target provisioned or used
Qualification scope: implementation and deterministic constructor/wiring/lifecycle regression evidence on Windows
Explicit exclusions: independent Q2, owner acceptance, protected native qualification, P4, Slice E, Slice N and production activation

**P3-R1 REMEDIATED — READY FOR INDEPENDENT Q2**

Raw evidence: `C:\Users\Laszlos\source\repos\Solo\gagamba-alt-b-p3-r1-codex`.
This document records producer remediation evidence, not independent qualification.
The source/test commit above is followed by a documentation-only evidence commit;
`final-identity.json` in the external package records the exact final HEAD and tree.

## Finding, baseline and preserved work

Independent P3-Q1 F1 is **Fixed at the implementation level**. A matching qualified context
made the original condition `installed && authority is not Qualified` false, after which
`else if (authority is not Legacy)` was true. The disjoint qualified type was therefore
always rejected before constructing the arbiter. This is erroneous condition grouping/type
exclusion, not a failed native generation check or an inverted active-generation verdict.

The checkout started at the expected commit/tree with one tracked modification to
`HeldReleasePhase.cs`, no staged changes, 44 historical untracked files and one canonical
worktree. Before editing, the existing patch was saved as `initial.patch`, with tracked
and untracked SHA-256 inventories in `initial-snapshot.json`. The user confirmed no other
agent was modifying Gagamba. The interrupted patch correctly repairs the condition and
was retained **verbatim**; `pre-commit-preservation.json` proves its equality to the
committed production diff and preservation of every original tracked working-file byte
and all 44 historical untracked paths/bytes before source commit.

The Q1 package was read without alteration; all **2,255/2,255** entries of its recursive
manifest matched (`q1-manifest-verification.json`). Its original constructor reproducer,
`REPORT.md`, `findings.md` and `owner-handoff.md` were read. The interrupted Q2 `STOPPED.md`
was read as context only; none of that package's preliminary runs qualifies this repair.
Neither review package was overwritten.

| Probe | Baseline observation | Change | Fixed observation |
| --- | --- | --- | --- |
| Exact new positive constructor regression | 0 passed / 1 failed against the preserved Q1 Debug DLL; original InvalidDataException | Preserve corrected constructor grouping and allow Qualified or Legacy in the non-installed branch | 1 passed, including real arbiter creation and callback identity |
| Reflection constructor matrix, Debug and Release | Qualified installed and no-wire-identity cases both reject; legacy accepts; missing/validation/incompatible kind refuse | Same narrow guard correction | 8/8 expected outcomes per flavor; qualified cases construct and are gated; legacy remains ungated |

The baseline regression runs the newly built test DLL in an external **binary-only**
directory with the Q1 M1 DLL substituted. Its SHA-256 is checked against the independent
report (`2eb7be0d0cbe025050b658e1990a3e81a4b719127150252f7a593c4126562b59`).
No historical source was rebuilt or installed over canonical main. The standalone matrix
also loads the unchanged original Debug and Release DLLs directly. Its baseline rows
characterize the defect and are not positive qualification rows.

## Correction and resulting authority boundary

Only the constructor guard changes in production (8 insertions, 3 deletions). The installed
request branch still requires Qualified; the other branch rejects every context except
Qualified or Legacy. The guard remains in place. No gate, trust factory, RELEASE writer,
protocol, journal schema, deadline, capability rule or arbitration ordering was changed.

| Input | HELD outcome | Generation verifier |
| --- | --- | --- |
| Matching retained Qualified with installed identity | Constructs | Instance method forwards to `OwnerReleaseGenerationGate.Verify` |
| Qualified with installation identity omitted from request | Constructs at this type boundary | Still gated; cannot select legacy. The upstream transaction's existing request/context consistency rejection remains unchanged. |
| Legacy, uninstalled request | Constructs | Existing null callback / legacy behavior |
| Installed request with Legacy or missing authority | Refuses before binding arbiter | None constructed |
| Missing authority for uninstalled request | Refuses | None constructed |
| Validation, either request shape | Refuses | None constructed |

Real P1/P2 trusted provenance and tuple/echo consistency checks remain at their accepted
production boundaries. Reflection-manufactured golden `LoadedInstallation` objects in
these fixtures model the retained type and tuple; they do not attest a protected S2 store.
A Debug-only `Qualified.ForTest` shape without a loaded installation is used to prove the
**actual constructor-installed callback** refuses RELEASE, rather than silently selecting
legacy. It returns the existing post-intent `UnqualifiedInstallation` refusal with zero
attempts, consumes/closes the capability, retains Intent, avoids runtime/durable RELEASE
commit, and submits the existing SetupFault stop. It is not positive authorization.

The actual qualified callback is readonly on the arbiter and remains the existing phase
method group over the retained context. A constructor test asserts its target/method,
its single shared-gate IL call, the retained context identity, the original `HeldExecution`
transport, and the safety monitor's binding to this same arbiter. No caller Boolean,
wire echo or later observation can select a replacement context or a null qualified gate.

## Regression construction and limits

`HeldReleasePhaseConstructorTests` adds **13 deterministic rows**. They execute the real
HELD constructor with a real capability, journal and writer over fake storage. The native
monitor is an explicitly synthetic object with only its binding lock initialized; no
monitor thread, recovery reference, cgroup or target is created. Invalid, non-owning file
handles prevent native I/O. Constructor wiring assertions execute before any test mutation.

For arbiter behavior, reflection replaces only OS-dependent clock, prerequisites and
transport on the **arbiter created by HELD**. The real-generation-refusal test preserves
and executes the installed production callback. Stop/deadline tests separately replace
that readonly callback in the test fixture with a bounded successful verifier, allowing
a deterministic positive-scan decision without fabricating native trust. They exercise
the existing final winner checks, not actual protected-store scanning. The stop race uses
explicit entered/proceed barriers, finite 10-second waits and a finally-released barrier.
The two expiry rows advance the controlled clock during verification; neither deadline
nor stop can be overridden by a successful verifier. Legacy success followed by duplicate
produces one counting-transport attempt; qualified refusal followed by duplicate produces
zero attempts and no second journal update. Existing P3 races/decision tests are retained.

Existing P1/P2 tests also cover immutable retained tuples, mismatch refusal, absence of
validation factories and absence of Boolean selectors. Their production source is unchanged.
These fixtures establish managed constructor and arbitration behavior; they do not establish
native physical write, stop/reaping, protected-root read or real cleanup behavior.

## Exact verification on the committed source

Final builds and runs record source HEAD `18a788da2f2f50625468f90feb144b9b9a193af2`
and an empty tracked diff in `commands.json`. Earlier successful precommit logs are
retained separately under `precommit-validation`; the final run rebuilt the committed
source to bind the delivered binaries directly to that revision. The source snapshots
and `source-identities.json` compare working bytes with committed bytes, disclosing
LF/CRLF representation separately. Overlapping focused filters must not be summed.

| Check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| New constructor / production wiring | 13 | 0 | 0 |
| Existing P3 (`FullyQualifiedName~OwnerReleaseGenerationGate`) | 27 | 0 | 0 |
| Q10 + P3 (`FullyQualifiedName~ReleaseGenerationGateTests`) | 20 | 0 | 0 |
| Relevant P1/P2 and lifecycle regression filter | 196 | 0 | 20 |
| Full Windows M1 Debug runner | 1,335 | 0 | 331 |
| Fixed constructor matrix, Debug | 8 | 0 | 0 |
| Fixed constructor matrix, Release | 8 | 0 | 0 |

Full Windows M1 totals **1,666**, exactly the baseline 1,653 plus 13 new rows.
All **331 native skips remain unqualified**. The regression filter is recorded verbatim
in `commands.json` and `run_checks.py`; it includes authority retention, manifest admission,
final acceptance, generation gate, installation wire, recovery B2, direct authentication
and release-arbiter tests. Raw TRX and row counts are in `test-results` / `test-summary.json`.

| Build / artifact check | Result |
| --- | --- |
| Debug M1 test project and dependencies (including Owner) | 0 warnings / 0 errors; instrumented verifier PASS |
| Release M1 | 0 warnings / 0 errors |
| Release Owner | 0 warnings / 0 errors |
| Release Recovery | 0 warnings / 0 errors |
| Production verifier, Release M1 | PASS (exit 0) |
| Production verifier, Debug M1 negative control | FAIL as expected (exit 1); detects `Grant::ForTest`, `Qualified::ForTest`, `OwnerReleaseGenerationGate::VerifyObserved`, and existing instrumentation |
| External constructor probe build | 0 warnings / 0 errors |

Final binary identities:

| Assembly | SHA-256 | MVID |
| --- | --- | --- |
| Release M1 | `F6DEB97331F81E655043FDBFDFD7F5A74076D0DC1B7A2D4B26A6706DABE31222` | `fb18ed6a-5b73-48ea-9544-fc9edd7612cc` |
| Debug M1 | `8F6C368D6816659F08D08E7E5AD55C83C901BF848CE0B7295481B6AE7650E4B2` | `c4ef5172-bc41-4eed-8c7d-f4c6f4f3fbc2` |

## Cleanup, preservation and qualification boundary

The new fixtures create no OS execution domain, target, child process, protected files,
real descriptor ownership or monitor thread. Capabilities, writer leases, non-owning
handle wrappers and barrier objects are disposed. The external binary baseline directory,
probe binaries and raw logs are intentionally retained as evidence. Test/build processes
complete; final process inspection is recorded in the package. Native root/descendant
termination, domain emptiness, reaper cleanup and partial initialization cleanup are
**not measured here** and remain pending. Managed completion does not qualify them.

The existing original C3 deadlines, stop precedence, conservative post-intent refusal,
one-attempt semantics and sole physical RELEASE writer remain unchanged. The accepted
root observation-to-effect residual remains: a switch after the last relevant stable
observation, including before verifier return, can escape detection. This residual does
not waive stop/deadline obligations.

Final repository and package audits are recorded in `final-preservation.json` and
`final-identity.json`: clean tracked/index state, all 44 original untracked paths and hashes
preserved, one canonical main worktree, only the intended source/test and additive evidence
changes. The external `SHA256SUMS.txt` binds every retained package file except itself.
No reset, restore, clean, prune, rebase, branch, linked worktree, push, privileged provisioning,
production activation or owner acceptance occurred. No independent Q2 was executed.

Next: separately commissioned independent Q2 using the [P3-Q2 handoff](../reviews/rc2b-06-b6-alt-b-p3-q2-handoff.md).
