# Gagamba Linux M1 G1 re-review (preserved, second independent review)

- review type: second independent G1 review
- reviewed revision: 7599ecb0eb537670b2b5dbcff102198a1cbe091c
- native qualification: NotApplicable
- disposition: G1 NOT ACCEPTED — WP01 CORRECTION REQUIRED
- remaining findings: G1-R1, G1-R2

The independent review below is preserved verbatim and unsoftened. It does
not modify source, tests, ADR 0003, the implementation plan, AR-7 evidence,
or the preserved first G1 review.

---

# WP01 G1 independent re-review


Date: 2026-10-12 (Asia/Manila)  
Slice: WP01 contracts, capability identity and pure state model  
Original rejected source: `e2c2f119d260eccf8f2b10b2c3d983f2c6da0eeb`  
Preserved original review: `2d8e23473063ebd65978f5bb6a8e8507fc8a59e4`  
Reviewed source: **`7599ecb0eb537670b2b5dbcff102198a1cbe091c`**  
Branch: `qualification/ar7-feasibility`  
Host: Windows 11, OS version 10.0.26200.0, x64  
SDK/runtime: .NET SDK 10.0.401; .NET 10.0.12; xUnit v3 3.2.0  
Native WP01 qualification: **NotApplicable**  
Scope: static review, exact-revision managed build/tests, separate reviewer probes  
Exclusions: Linux native qualification, WP02 implementation, production admission, package publication


## Disposition


**G1 NOT ACCEPTED — WP01 CORRECTION REQUIRED. WP02 remains blocked.**


The original unsafe overwrites, false success after accepted stop, mutable terminal snapshots, lost metadata, missing manifest fields and enum-pin omission have been corrected. Two state-contract defects remain: a new domain-observation progression defect and incomplete correction of original G1-08. Neither reproduction requires native mechanisms or WP02 work.


The repository was clean at the reviewed SHA and remains unchanged. Tests used an archive of that exact revision. Reviewer probes were compiled in a separate copied test project against unchanged production sources; they are not included in the author's 56 tests.


## Findings


### G1-R1 — P2 / Major: allow a populated domain to become empty


Location: `src/Gagamba.Execution.Linux.M1/Execution/TransactionStateModel.cs:459–470`.


The new `_domainObserved` guard freezes the first boolean observation. After a confirmed exec and known root exit, `ObserveDomainEmpty(false)` succeeds, but a later `ObserveDomainEmpty(true)` returns `Conflict`. Even with complete capture, terminal publication retains `DomainEmpty=false` and produces `Unknown`.


This is a normal lifecycle progression: the root can exit while descendants remain, and the domain can subsequently empty. Plan Sections 6, 10 and 12 explicitly require continued observation until domain emptiness. A populated snapshot is not an immutable terminal result. The new guard confuses a change in domain state with contradictory root history.


Independent reproduction `ReviewerDomainCanBecomeEmptyAfterRootExit` fails with:


```text
Conflict; retained DomainEmpty=False; outcome=Unknown
```


Required correction: define domain observations separately from single-assignment root results. Permit populated → empty before terminal publication, retain the established empty fact appropriately, preserve terminal immutability, and add this lifecycle regression. No native polling implementation is requested here.


### G1-R2 — P2 / Minor: retain the release decision after subsequent state changes


Location: `src/Gagamba.Execution.Linux.M1/Execution/TransactionStateModel.cs:209–229`.


The duplicate branch only runs while `State == ReleaseCommitted` and a current capability remains. Repeating the identical authorization after trusted exec confirmation returns `InvalidStateOrder`. After a post-commit stop it returns `StoppedFirst` with the inaccurate detail “A stop already won before release.” After completion it again returns `InvalidStateOrder`.


Plan Section 8.2 requires duplicate calls to return the original decision without another write. Original G1-08 is therefore only partially corrected. All three reviewer cases retained exactly one transport attempt, so this is not independently a second-release safety defect; it is a decision/history contract defect.


Required correction: retain the release decision and its authorization identity independently of the current state and live capability, and resolve duplicates against that retained decision after Running, post-commit Stopping and terminal publication. Conflicting identities must still refuse; stopping before the first commit must still forbid release. Add regressions beyond the immediate duplicate currently tested.


## Original finding reconciliation


The numbering below follows the preserved independent review. The author's correction supplement assigns different meanings to several G1 identifiers and should not be used as a one-to-one closure map.


| Original finding | Re-review result |
|---|---|
| G1-01 canonical ordering | Corrected: UTF-8 byte comparator used for set names, paths, roles, environment and helper dependencies; permutations and duplicate rejection covered; supplementary Unicode order verified. |
| G1-02 manifest/establishment | Corrected for the identified WP01 omissions: mount policy, network/inode qualification, core-dump and owner budgets are represented/encoded; terminal establishment is separate and Absent. See the explicit future gate below. |
| G1-03 copied authorization identity | Corrected: UUIDs and digest contents compared explicitly. Independent copied-identity probe passes; actual capability consumption retains reference identity. |
| G1-04 observations/outcomes | The four original reproductions are corrected and covered: root 17 cannot become 0, known root cannot become Unknown, accepted overflow stop cannot yield success, and pre-release helper/capture observations are admitted. The correction introduces G1-R1. |
| G1-05 terminal immutability/metadata | Corrected: immutable copied collections, metadata retained separately from capability validity, errno and later stop causes included. Independent defensive-copy/errno/later-cause probe passes. |
| G1-06 cleanup confirmation | Corrected: Confirmed rejects missing/false components; model cleanup remains Unknown. |
| G1-07 executable coverage | Original enumerated omissions corrected: 14 enums/67 members pinned; mutation test now uses supplied arrays; matching-identifier foreign capability and all four missing prerequisites tested; original contradictory sequences covered. New G1-R1/R2 sequences remain uncovered by the submitted suite. Observation cancellation is still a fake polling exercise, not evidence for a future shared observation API. |
| G1-08 duplicate release | Partially corrected; G1-R2 remains. |
| G1-09 evidence precision | Improved: correction source bound to d03e669, current test counts accurate, unused import removed, reference encoder retained. Remaining editorial/provenance limitations are listed below. |


Baseline behavior is taken from the preserved original independent review; this re-review did not re-execute e2c2f119. Corrected behavior was rechecked through source inspection, the submitted tests and the additional probes.


## Verified claims


### Correction scope


Direct diffs confirm:


- `d03e669 → 5fe52fc`: one evidence heading changed to name d03e669 explicitly.
- `5fe52fc → 7599ecb`: three numeric `EstablishmentStatus` assertions, removal of one unused using, and the evidence re-audit note.
- Combined `d03e669 → 7599ecb`: only `docs/evidence/linux-m1/wp01.md` and `Contracts/ManifestTests.cs`, 12 insertions and 2 deletions. No production changes.


The complete WP01 diff against `9b9264d` stays within contracts, the pure model/store, tests, additive solution wiring and documentation. The M1 project is non-packable. No copy-in, native acquisition, real owner, launch, real transport, new public API/provider registration or WP02 implementation was found.


### Enums and canonical vector


An independent source audit matched each explicit enum member/value to its numeric assertion: **14 enums, 67 members, none missing**. It also decoded and independently hashed the literal golden vector:


```text
Length: 873 bytes
SHA-256: 82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3
```


The submitted production/golden and reference-encoding tests pass. The committed reference encoder independently implements byte framing and layout, but its `BytewiseOrder` comparer is unused: it iterates collections already sorted by production constructors. Therefore it does not independently prove sorting. Explicit expected-order tests and inspection of the shared production comparator provide the ordering evidence. Its comment referring to “Appendix D” has no corresponding appendix in the implementation plan.


The evidence's historical header still instructs readers to resolve unrestricted HEAD, and historical counts remain in earlier sections. Section 8 partly corrects this by binding the principal correction. Use the exact reviewed SHA above for this re-review; do not interpret the older header as current source binding. These editorial issues are separate from the two executable findings.


### Observed establishment boundary


Repository-wide source search found `EstablishedManifest.Observed(...)` calls only in contract tests. Production contains the representation factory declaration; the sole model terminal construction explicitly passes `EstablishedManifest.Absent` at `TransactionStateModel.cs:558`. No production WP01 execution path supplies Observed establishment evidence.


**The internal Observed representation is not a WP01 defect and need not be removed.** The first work package that produces Observed evidence must independently prove that it originates from the trusted observation mechanism required by the plan, including required external readbacks and the source-bound coordinator where applicable. Merely supplying a requested manifest is not that proof.


## Executed checks


| Unmodified submitted suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Execution contracts | 20 | 0 | 0 |
| Fixture self-tests | 15 | 0 | 0 |
| Linux M1 | 56 | 0 | 0 |
| macOS | 47 | 0 | 4 |
| Gw1bLaunch | 2 | 0 | 0 |
| Conformance | 6 | 0 | 0 |
| Runtime | 14 | 0 | 0 |
| Linux | 26 | 0 | 16 |
| Windows | 30 | 0 | 0 |
| **Total** | **216** | **0** | **20** |


M1 distribution is 19 manifest / 10 preparation / 27 state tests. Full solution build passed with zero warnings/errors. Native M1 remains **NotApplicable**; platform skips do not qualify Linux or macOS.


Environment qualification: the first sandboxed full-suite attempt had four failures in existing Windows-dependent tests (Gw1bLaunch 1, conformance 1, Windows 2). Gw1bLaunch encountered an invalid-handle SEH exception; fixture file operations were also denied in the sandbox. The three affected assemblies were rerun outside the sandbox against the same exact-source binaries and passed 2/2, 6/6 and 30/30. The table reports those host reruns; initial failure logs are retained rather than erased. No source repair was needed for these environment-specific failures.


The separate six-case reviewer run produced **2 passes and 4 failures**: copied authorization and terminal snapshot probes passed; three delayed-duplicate variants and one domain-progression probe failed. These are additional tests, not failures hidden inside the author's 56/56 result.


## Commands and reproducibility


An exact `git archive` of the reviewed SHA was extracted into a separate workspace. Builds used:


```powershell
dotnet build Gagamba.sln -m:1 -p:NuGetAudit=false -p:EnableSourceControlManagerQueries=false -p:EnableSourceLink=false -p:MSBuildEnableWorkloadResolver=false -v:minimal
dotnet tests/Gagamba.Execution.Linux.M1.Tests/bin/Debug/net10.0/Gagamba.Execution.Linux.M1.Tests.dll -noLogo -xml ../m1-results.xml
```


Each solution test assembly was run through its direct xUnit v3 executable using the same `dotnet <assembly.dll> -noLogo -xml <result.xml>` form. The adjacent evidence bundle retains the exact suite list, logs, XML results, enum/golden audit, reviewer source and a reproduction script. NuGet auditing and SourceLink/Git queries were disabled for the isolated archive; warnings-as-errors and deterministic compilation remained enabled. An initial parallel solution build returned failure without diagnostic errors; serial `-m:1` build succeeded.


The reviewer project copies the submitted tests, makes their existing harness class partial and adds `ReviewerProbes.cs`. It retains the friend assembly name solely to access the internal contracts. Production source is unchanged. Run its tests with:


```powershell
dotnet tests/Gagamba.Execution.Linux.M1.Review/bin/Debug/net10.0/Gagamba.Execution.Linux.M1.Tests.dll -noLogo -method '*Reviewer*' -xml ../reviewer-results.xml
```


## Cleanup and next boundary


WP01 tests/probes allocate no native domain or target. Existing Windows regression tests did create disposable fixtures. The sandbox invalid-handle exception left one suspended cmd fixture; it was matched by PID 42840, parent 48576 and its exact finite ping command, terminated, and its absence verified. Final process inspection found no matching review runners, conformance fixture commands or that ping fixture. This is local test cleanup, not Linux domain qualification.


Correct G1-R1 and G1-R2 inside WP01, add the focused portable regressions and bind the evidence to the resulting revision before another G1 review. Preserve the original review and this re-review. WP02 and production establishment generation remain outside this correction scope.
