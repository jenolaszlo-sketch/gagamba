# B6-F02 Mechanism B bounded implementation 01 — independent Q1 handoff

**GAGAMBA-RC2B-06-B6-F02-MECHANISM-B-IMPL-01-Q1**

**Status: READY FOR INDEPENDENT Q1 — IMPLEMENTED WITH QUALIFICATIONS.**

Date: 2026-11-05 (Asia/Manila)

Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`, branch `main`

Producer evidence: [B6-F02 Mechanism B bounded implementation 01](../evidence/rc2b-06-b6-f02-mechanism-b-impl-01.md)

Owner authority: [Mechanism B owner acceptance](../decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md)

Authorized plan: [Bounded implementation plan](../plans/rc2b-06-b6-f02-mechanism-b-impl-01.md)

## 1. Review target

| Item | Commit | Tree |
|---|---|---|
| Canonical baseline | `504b6d06ac88591072b11f428ae88cade390c075` | `e3f028dc32c48935fb4abdc6a553a2641c960c53` |
| Owner acceptance and plan | `4aab87761078a92d05cd08aa1998cbe2c4f6aaf1` | `582e80115f01d7adec6c7d771a875beb315901c5` |
| Implementation and portable tests | `831c980e517cff48885aec24283d91a2b41e0860` | `66bda80f7aec5f92a4ddcac309f305bf62568846` |

The Q1 evidence commit is the commit containing this handoff. Verify the exact commit/tree from `git log` rather than trusting this file's own hash. The remote `origin/main` is `21f0aa7f3afd3a7dbfbc6ecae51d4025b13439b7`; the implementation commits were not pushed.

Review these committed files:

- `src/Gagamba.Execution.Linux.M1/Installed/FinalAcceptance.cs`
- `src/Gagamba.Execution.Linux.M1/Installed/FinalAdmissionVerifier.cs`
- `src/Gagamba.Execution.Linux.M1/Installed/AdmissionAuthority.cs`
- `src/Gagamba.Execution.Linux.M1/Installed/InstalledFileSystem.cs`
- `src/Gagamba.Execution.Linux.M1/Installed/InstalledMetadata.cs`
- `src/Gagamba.Execution.Linux.M1/Installed/LoadedInstallation.cs`
- `src/Gagamba.Execution.Linux.M1/Restricted/B3Qualification.cs`
- `src/Gagamba.Execution.Linux.M1/Bootstrap/IntegratedB3Client.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Installed/FinalAcceptanceTests.cs`

The producer's committed file SHA-256 values and test commands are in the [evidence](../evidence/rc2b-06-b6-f02-mechanism-b-impl-01.md). Recompute them independently.

## 2. What is claimed

- Strict CJSON-1 final-record, selector and host-identity codecs.
- Exact S2, host and owner-decision binding with no caller-supplied expected hash.
- A protected-store verifier restricted to the three fixed runtime paths.
- A private grant constructible only by the full verifier.
- Fail-closed separation of S2 `Qualified`, final admission and production activation.
- Retention and revalidation of the raw S2 qualification-record hash.
- Final-admission gates before B3 journal creation and recovery spawn.
- No change to owner-only RELEASE, recovery stop-only, original C3 or Unknown handling.
- 69 focused portable tests and a clean full Windows M1 runner.

## 3. What is not claimed

- No native protected-store positive, root-owned host identity, selector, record or approval exists.
- The 331 Windows skips are not passes.
- No final-record publisher, gate-results validator, approval publication, withdrawal, quarantine or lock protocol.
- No full production admission sink map; only two explicit gates are implemented.
- No full session-bound capability, installed-profile driver, S4/B7, privilege drop or native loader closure.
- No production activation; `RestrictedExecutionRuntime.Create()` remains unavailable.
- Q10's pre-RELEASE active-generation reread is **not implemented** and remains an open non-waiver.

## 4. Independent Q1 questions

### Source and schema

1. **Q1-01 — Canonical codec.** Does `FinalAcceptanceCodec.ParseCanonical` enforce exactly the accepted CJSON-1 rules: ASCII printable only, one root object, strictly ascending unique keys, no unknown/missing/duplicate keys, canonical reserialization equality, unsigned canonical integers, and the 65,536-byte limit? Are there parser edge cases (escapes, BOM, trailing bytes, depth, empty input, non-object roots) that could pass?
2. **Q1-02 — Record schema.** Does the record parser require exactly the accepted 24 fields and exact literal values for `acceptance` and `buildConfiguration`? Are `limitations`, timestamps, IDs, commits, trees and hashes bounded and validated as claimed?
3. **Q1-03 — Selector schema.** Does the selector parser require exactly the accepted 7 fields, `accepted` as the JSON literal `true` (not a string or truthy value), schema version 1, and valid hashes/IDs? Can a false or non-Boolean acceptance pass?
4. **Q1-04 — Host identity.** Is `/etc/gagamba/host-id` required to be exactly 64 lowercase hexadecimal ASCII bytes plus one LF, with no BOM, CR, extra byte or alternate encoding?
5. **Q1-05 — No caller authority.** Does any runtime API accept a caller-supplied path, expected final-record hash, selector, or self-asserted acceptance? Is the selector name derived only from the retained S2 artifact-set digest and the record name only from the protected selector hash? Is there any search, fallback or `latest` behavior?

### Protected store and grant

6. **Q1-06 — Fixed paths.** Does the verifier read only `/etc/gagamba/host-id`, `/opt/gagamba/final-admission-selectors/<artifactSetSha256>.json` and `/opt/gagamba/final-admissions/<finalRecordSha256>.json`? Confirm no runtime read of `/etc/gagamba/accepted-final-admissions` and no alternate approval store.
7. **Q1-07 — Filesystem custody.** Does `OpenProtectedDirectory`/`ReadFile` reuse the existing no-follow, descriptor-relative, `fstat`, reacquire, capability, sparse-file and stability primitives? Are `0444`, single-link regular-file, root ownership, exact leaf `0755`, and root-owned non-group/other-writable ancestor requirements correctly enforced? Does a missing native primitive or Linux feature fail closed?
8. **Q1-08 — Grant construction.** Is `FinalAdmissionVerifier.Grant` truly constructible only by `Grant.VerifyInstalled`? Check the private constructor, nested access, every factory and every production caller. Can a `FinalAdmissionBinding`, Boolean, selector, validation result or reflection path create an effective grant?
9. **Q1-09 — Stability protocol.** Are the host/selector/record rereads, byte comparisons, snapshot comparisons, directory reopen comparisons and fresh S2 revalidation sufficient? Identify any remaining TOCTOU window, wrong comparison type, or ordering defect.
10. **Q1-10 — Failure conversion.** Are native unavailability exceptions (`NotSupportedException`, `DllNotFoundException`, `EntryPointNotFoundException`, `BadImageFormatException`) converted to typed fail-closed refusal without accidentally granting admission or swallowing a mismatch? Also check `InvalidDataException`, `IOException` and `CryptographicException` propagation from `VerifyInstalled` into `B3Qualification.TryCreate` and `IntegratedB3Client.Prepare`: does every failure occur before a journal, allocation, staging directory, recovery spawn or session authority?

### S2 binding and plumbing

11. **Q1-11 — S2 expectation source.** Does `FinalAdmissionExpectation.FromSelection` derive every bound value from the retained trusted S2 selection, including the raw qualification-record hash? Is `platformInventorySha256` correctly mapped to `platform.runtimeInventorySha256`? Is that mapping adequately disclosed as an implementation qualification?
12. **Q1-12 — Raw-hash plumbing.** Is `QualificationRecordSha256` populated only from the trusted `VerifyActive` receipt-selected record, retained by `InstalledResult.Qualified`, and compared by `LoadedInstallation.Revalidate`? Can any legacy, fresh, unavailable or test path construct a qualified result with a null or caller-controlled raw hash?
13. **Q1-13 — Selection and owner-decision binding.** Does the verifier refuse if the S2 selection's identity, qualification ID, raw record hash, source commit or source tree changes after final bytes are read? Are there other S2 fields that should be revalidated? Is equality of `ownerDecisionSha256` between the protected selector and record sufficient, given that the selector is the protected administrative projection and runtime never reads the `/etc` approval?

### Admission separation and gates

14. **Q1-14 — Separation predicates.** Do `IsFinalAdmissionEligible` and `IsProductionAdmissionAuthorized` require the exact grant and binding match? Is `IsFinalAdmissionBindingEligible` used only as a pure test/helper predicate and never as production authority? Can S2 `Qualified` alone, a legacy selector, or a validation result satisfy admission?
15. **Q1-15 — Unreachable activation.** Does `ProductionActivationAuthorization` have only a private constructor and no factory in this slice? Confirm no production path can instantiate it and that final admission does not imply activation.
16. **Q1-16 — B3 gate placement.** In `B3Qualification.TryCreate`, does the final-admission gate run before `B3PreparationOwner` construction and its journal-directory effect? Does every refusal return the typed refusal before allocation, journal creation, staging or session authority?
17. **Q1-17 — Recovery gate placement.** In `IntegratedB3Client.Prepare`, does final-admission verification run before `RecoveryConnection.Start` can spawn recovery or owner processes? Are there earlier effects in `Prepare` that the gate does not cover?
18. **Q1-18 — Legacy-false path.** Does `QualifiedInstallation=true` with null `IntegratedRecovery` now refuse instead of taking the legacy non-integrated path? Is `QualifiedInstallation=false` behavior unchanged, and do existing tests support that?
19. **Q1-19 — Remaining sinks.** Within this bounded slice, are there any production admission, allocation, helper, preparation or release entry points that bypass the two gates? The producer does **not** claim a complete sink map; identify any concrete bypass that makes the bounded claim false.

### Preservation and authority invariants

20. **Q1-20 — RELEASE/stopping.** Confirm the commit does not add a RELEASE writer or alter owner-only one-attempt release, recovery stop-only behavior, original C3 stopping semantics, or conservative Unknown handling. Are the release arbiter and owner/recovery protocols untouched?
21. **Q1-21 — Unavailable factory.** Does `RestrictedExecutionRuntime.Create()` still return `UnavailableRestrictedExecutionProvider`? Is there any new public activation or restricted-execution route?
22. **Q1-22 — Q10.** Is Q10's accepted pre-RELEASE active-generation reread/refusal still explicitly open? Does any source comment, test or evidence statement imply it is implemented or silently waived?
23. **Q1-23 — Preservation inventory.** Verify independently that 44 historical untracked paths and 74 registered worktrees are preserved; no branch, linked worktree, reset, rebase, stash, clean, prune, force operation or push occurred. Check `git status --porcelain=v1 -uall`, `git worktree list --porcelain`, `git log origin/main..HEAD` and `git diff --check`.
24. **Q1-24 — Evidence truthfulness.** Compare every claim in the evidence with the actual source, commands and raw output. Are skipped native tests, portable tests or source inspection presented as native qualification? Are any `PASS`/`SUPPORTED` statements unsupported?
25. **Q1-25 — Disposition.** Given the pending native protected-store positives, the publisher non-goal, the false-`QualifiedInstallation` change, the `platformInventorySha256` mapping and open Q10, is **IMPLEMENTED WITH QUALIFICATIONS — READY FOR INDEPENDENT Q1** the correct disposition? If not, state the exact blocking defect and severity.

## 5. Review method

The reviewer should independently:

1. verify the commit/tree ancestry from the canonical baseline and confirm the exact changed file set;
2. recompute SHA-256 for all nine committed files and compare with the evidence;
3. inspect the committed source rather than only the diff narrative;
4. run the three commands in the evidence from a clean checkout of the implementation commit;
5. construct independent portable probes for the codec, binding, grant and separation claims where practical;
6. attempt to identify a concrete bypass, authority forgery, path escape, TOCTOU, legacy fallback or false native claim;
7. recheck preservation and confirm no push or out-of-scope change;
8. report findings by severity with exact file/line evidence and a disposition.

Native protected-store execution requires a separately authorized Linux root environment. It is not available in this commission and must not be simulated or inferred.

## 6. Expected Q1 response

Report:

- **disposition** (for example SUPPORTED WITH QUALIFICATIONS, INCOMPLETE, or BLOCKED);
- **findings** with severity, exact source locations, reproduction and whether each is a defect, qualification, or observation;
- **claim verification** for source binding, codec, grant, separation, gates, preservation and evidence truthfulness;
- **unexecuted procedures** that remain pending and must not be counted as passes;
- **recommendation** on whether the owner may accept this bounded slice, with any mandatory remediation.

Q1 must not itself provision protected stores, create accepted records, activate production, or push.
