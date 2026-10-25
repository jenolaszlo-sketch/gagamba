# Gagamba RC2A-02B Independent Portability Qualification Review (Q1)

Date: 2026-10-25 (Asia/Manila)

Verdict: **REMEDIATION REQUIRED**

Review scope: RC2A-02B evidence and portability claims only. This review does not accept an owner decision or close RC2A-02.

## Verdict summary

The committed raw results support a bounded managed-code portability result on native Windows and WSL2 Ubuntu userspace. I independently rebuilt the Windows Execution project and reran the public suite and precise required set on the exact reviewed source: 67/0/0, 457/0/0, and 457/0/0. The producer's raw Windows and WSL2 TRX files also show matching 67- and 457-test name/outcome multisets.

The evidence package has two confirmed defects. Fourteen Windows artifact SHA-256 fields in `results.json` still name the pre-correction bytes, not the committed artifact blobs. The README also misdescribes the broad-filter extras as five Linux-native classes that all skip on Windows; raw results show two unrelated managed-code classes pass. The final `SHA256SUMS` and document bindings verify, and the stale hashes do not change the parsed test outcomes. The digest metadata is still part of the required provenance record, so RC2A-02B evidence should be corrected before owner acceptance.

Native Linux and macOS are explicitly recorded as **NOT RUN**. The plan allows unrun hosts to be recorded with reasons and says they are neither a pass nor a failure. No native Linux/macOS, confinement, sandbox, package, deployment, clean-consumer, or Hufu claim is supported or made here. Their absence is not a finding against the bounded Windows + WSL2 claim.

## 1. Git binding and review worktree

Repository metadata is shared from `C:\Users\Laszlos\source\repos\Gagamba`; the reviewed checkout is under `C:\Users\Laszlos\source\repos\Solo`.

| Role | Commit | Tree | Parent |
|---|---|---|---|
| Accepted RC2A-02A owner decision / RC2A-02B start | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` | `8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e` | `c1a01761f8d58136a381ad87269635c6c14ad093` |
| RC2A-02B execution evidence | `5b4292d0fefc576664df61692dfad5cef6362879` | `3f76b861e689d04a17c64f8a3e71e57851c21f58` | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` |
| RC2A-02B final evidence binding | `dd16baec48663d40cd29504d98e57e30dd0855df` | `96e86aac2d284637805809b54da72e76bf46f1fc` | `5b4292d0fefc576664df61692dfad5cef6362879` |

The dedicated review worktree was created at `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-02b-q1-review`, branch `review/gagamba-rc2a-02b-q1`, starting at the final evidence commit `dd16bae…`. The original qualification checkout remained on `qualification/gagamba-rc2a-02b`; no merge, rebase, reset, push, or accepted-branch change occurred.

The accepted-baseline subtree IDs match the recorded identities and remain unchanged at the final evidence commit:

- `src`: `b8c167e3d7340a562221591bdc2b6df7adb372d1`
- `tests`: `5c3268286575cb55891fae9288627657f2d30ddf`
- `native`: `85631d2f04512376621d3d09f51d67fd90f45335`

The execution commit changes 48 paths from the accepted baseline, all under documentation/evidence and plan tracking. It changes no `src`, `tests`, or `native` path. The final binding commit changes only `docs/evidence/restricted-consumer-contract-rc2a-02b-portability/SHA256SUMS`. `git fsck --full --connectivity-only --no-reflogs` over the three named commits exited 0; no missing or corrupt object was reported for them.

## 2. Qualification contract interpretation

The controlling contract is `docs/plans/restricted-consumer-contract-rc2a-02-qualification.md`:

- §3 D (Q2A-P) requires a Release build of `Gagamba.Execution`, Windows as the primary run, separate per-platform evidence, and pure managed-code test selection. WSL2 must always be labeled `wsl2`; it does not count as native Linux. Native Linux and macOS runs are made when real hosts are available. Unrun platforms must say “not run,” with a reason, and cannot be represented as passing.
- §3 D P4 bounds the stage to pure managed-code behavior in the existing `net10.0` assemblies. It does not qualify a native provider, sandbox, cgroups, confinement, package bundle, or deployment.
- §9.1 allows a later accepted start tip and requires a separate owner authorization, an explicit Q2A-S / RC-D04 disposition, platform availability, separate evidence, and re-execution of the RC2A-02A-qualified assertions on each declared platform.
- §10 records RC2A-02B execution on Windows and WSL2 and leaves native Linux/macOS NOT RUN. The prior owner decision explicitly keeps Q2A-S / RC-D04 open and outside RC2A-02B; that obligation remains before full RC2A-02 acceptance, not as an unrecorded portability test.

The evidence uses `e5fff9e` as the accepted owner-decision tip, a direct child of the accepted RC2A-02A evidence and the start permitted by §9.1. The tested source tree is the same at `e5fff9e`, `5b4292d`, and `dd16bae` for `src`, `tests`, and `native`.

The qualification matrix says the separate authorization condition was satisfied by the assignment “Proceed with RC2A-02B.” That instruction is not included or linked from the committed evidence. I therefore treat authorization as asserted by the matrix, not independently authenticated from Git. The owner should preserve its source or decision pointer if the repository is expected to provide a complete audit trail for §9.1.1.

## 3. Independently verified results

I parsed the committed TRX `UnitTestResult` entries and compared test-name/outcome multisets. The generator script and complete machine output are in `docs/reviews/gagamba-rc2a-02b-q1-review/verify_rc2a_02b.py` and `verification-results.json`.

| Environment | Public suite | Precise required set | Required rerun | Broad `~Qualification` run |
|---|---:|---:|---:|---:|
| Windows 11 native, AMD64 | 67/0/0 | 457/0/0 | 457/0/0 | 473 passed / 0 failed / 41 skipped |
| WSL2 Ubuntu 26.04.1 | 67/0/0 | 457/0/0 | 457/0/0 | Not run |

Build logs report zero warnings and zero errors on both producer environments. I independently ran `dotnet build src/Gagamba.Execution/Gagamba.Execution.csproj -c Release --no-restore -warnaserror` on Windows: 0 warnings, 0 errors. The producer environment records identify Windows as `Microsoft Windows NT 10.0.26200.0`, SDK `10.0.401`, runtime `10.0.12`; WSL2 is explicitly identified by its `6.6.114.1-microsoft-standard-WSL2` kernel, Ubuntu `26.04.1`, SDK `10.0.113`, and runtime `10.0.13`.

On my clean review worktree, independent Windows runs reproduced 67 public passes and the 457 required passes twice. Both required reruns had identical name/outcome multisets and matched the producer's Windows set. The independently run public set also matched the producer's Windows set. Raw logs, exit files, and TRX are committed beside the review in `docs/reviews/gagamba-rc2a-02b-q1-artifacts/windows/`.

The review commands were:

```powershell
dotnet restore tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -p:NuGetAudit=false --ignore-failed-sources
dotnet restore tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -p:NuGetAudit=false --ignore-failed-sources
dotnet build src/Gagamba.Execution/Gagamba.Execution.csproj -c Release --no-restore -warnaserror
dotnet test tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -c Release --no-restore --no-build --logger 'trx;LogFileName=q1-review-windows-public-elevated.trx' --results-directory docs/reviews/gagamba-rc2a-02b-q1-artifacts/windows --blame-hang-timeout 60s
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-restore --no-build --filter 'FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Tests.Qualification.' --logger 'trx;LogFileName=q1-review-windows-required-captured.trx' --results-directory docs/reviews/gagamba-rc2a-02b-q1-artifacts/windows --blame-hang-timeout 60s
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-restore --no-build --filter 'FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Tests.Qualification.' --logger 'trx;LogFileName=q1-review-windows-required-captured-rerun.trx' --results-directory docs/reviews/gagamba-rc2a-02b-q1-artifacts/windows --blame-hang-timeout 60s
```

The sandbox could not connect VSTest to its test host or blame data collector. Those two aborted attempts are retained with their partial TRX and are not counted as results. After restoring locally available packages with NuGet auditing disabled because the vulnerability endpoint was unavailable, the same tests passed outside that process restriction. `wsl.exe --list --quiet` returned `Wsl/E_ACCESSDENIED` in this review environment; I did not independently rerun WSL2. WSL2 evidence below is independently inspected and parsed, not freshly executed by this reviewer. Native Linux/macOS were also unavailable to this reviewer, and no passing result is inferred for either.

The two WSL2 SourceLink setup failures are plainly recorded as setup attempts: one used an archive without Git metadata, and one used a local-path `origin`. Neither is counted as a qualification run. The subsequent WSL2 runs show the canonical repository URL used only as SourceLink metadata and report no fetch.

## 4. Test selection and exclusions

The precise filter is:

```text
FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Tests.Qualification.
```

The raw 457-test set consists of 419 tests in the Q2A `Tests.Qualification.` namespace plus 19 `Contracts.ManifestTests`, 9 `Contracts.RestrictedPublicProjectionTests`, and 10 `PreparationIdentityTests`. These sum to 457, and all are passed on Windows and WSL2. This isolates the required consumer-contract qualification cases and RC2A-01 regressions.

The original plan filter `FullyQualifiedName~Qualification` was preserved on Windows. It selects the full 457-test set plus 57 additional cases: 16 passed tests in `CoordinatorProtocolQualificationTests` (11) and `Wp05V3IndependentQualificationTests` (5), and 41 skipped tests in three Linux/native-oriented classes: `NonRootQualificationTests` (31), `OwnerDeathQualificationTests` (7), and `Wp05ReleasePathIndependentQualificationTests` (3). The broader result is therefore 514 total, 473 passed, 0 failed, 41 skipped. The 16 passing cases are managed tests outside the RC2A-02A consumer value/codec assertion set; their absence from the 457-test WSL2 portability claim does not invalidate that bounded claim. The 41 skipped cases are not counted as passes.

The evidence README says the broad filter selects five Linux-native classes and that they skip on Windows. That explanation is inaccurate: only the three classes producing the 41 skips do so; the other two classes execute and pass 16 tests. This is a documentation defect, not a hidden failing test or a missing RC2A-02B assertion. Correct the description before treating the evidence narrative as final.

## 5. Evidence integrity and findings

The final committed-byte integrity checks pass:

- All **43** `SHA256SUMS` entries match the bytes returned by `git show dd16bae:<path>`.
- All **9** document bindings match the raw LF-normalized committed blobs and recorded byte counts.
- The CRLF/LF correction in `dd16bae` correctly changes the top-level `SHA256SUMS` entries to bind the intended final Git blob bytes. The final commit itself changes no TRX, log, or source file.
- All producer test counts, rerun comparisons, cross-platform comparisons, and source-tree IDs agree with the raw committed evidence.

**F-01 — P2, stale Windows artifact digest metadata.** `results.json` contains 29 artifact SHA-256 references; 14 Windows references do not match the corresponding final committed blobs. These are the public, precise required, required-rerun, and broad-filter TRX/log/exit files, plus the Execution build log and Windows environment file. Each stale value matches its corresponding entry in the earlier `5b4292d` manifest; `dd16bae` corrected the 43-entry manifest but left the nested `results.json` values unchanged. The WSL2 references match their committed blobs. This does not alter the test contents or counts, and the correct final blob hashes are present in `SHA256SUMS`; it does make the per-run digest fields self-inconsistent and fails the plan's artifact-hash provenance requirement. Owner action: update those fields to the final committed bytes, or explicitly preserve and bind the original byte-domain artifacts, then recompute the manifest. The full mismatch list is in `verification-results.json`.

**F-02 — P3, incorrect broad-filter rationale.** `README.md` §4 says all five filter-matched classes are Linux-native and skip on Windows. The raw TRX contradicts that: two classes pass 16 managed tests. This wording overstates the excluded native scope but does not change the exact 457-test set or its results. Owner action: describe the 41 skips and the 16 additional passing tests by class and keep the claim limited to the executed 457-test consumer-contract set.

No production, test, or native-code mismatch was found. The SHA manifest and raw TRX outcomes support the reported counts; the findings concern provenance metadata and evidence wording.

## 6. Platform limits and open obligations

- **Windows-native managed code:** independently reproduced by this reviewer.
- **WSL2 Linux-userspace managed code:** producer evidence verified from committed environment files, logs, and TRX; not independently rerun because WSL was inaccessible to this review process.
- **Native Linux:** explicitly NOT RUN; no native host was available to the producer or this reviewer. No native Linux pass is inferred.
- **Native macOS:** explicitly NOT RUN; no macOS host was available. No macOS pass is inferred.
- **Native confinement/sandbox behavior:** not qualified. The test runs did not start native fixtures, owners, helpers, or workload.
- **Package and clean-consumer compatibility, mixed graph, Hufu / AE4-B01:** not qualified; no such claim is made.
- **Q2A-S / RC-D04:** remains OPEN under the accepted F04 disposition. Full RC2A-02 remains NOT YET ACCEPTED.
- **Separate authorization provenance:** the matrix asserts the §9.1.1 instruction existed but does not preserve its source. Owner confirmation or an auditable pointer remains useful before acceptance.

The plan does not make unavailable native Linux/macOS hosts a passing gate for this bounded stage. It requires honest “not run” disclosure and forbids extending the result to those hosts; the evidence observes those boundaries.

## 7. Recommended owner action

Correct F-01 and F-02 in a separate evidence-only correction, preserve this review unchanged, and request owner acceptance of the bounded Windows + WSL2 managed-code result only after the artifact hashes and filter explanation agree with committed bytes and raw outcomes. Keep native Linux/macOS marked NOT RUN. Do not treat RC2A-02B acceptance as full RC2A-02 acceptance; Q2A-S / RC-D04 and the other deferred gates remain open.

## 8. Review artifacts

- `docs/reviews/gagamba-rc2a-02b-q1-review/verify_rc2a_02b.py` — reproducible committed-evidence checks.
- `docs/reviews/gagamba-rc2a-02b-q1-review/verification-results.json` — machine-readable check results, including all 14 stale digest references and filter deltas.
- `docs/reviews/gagamba-rc2a-02b-q1-review/verification-output.log` — verifier output.
- `docs/reviews/gagamba-rc2a-02b-q1-artifacts/review-environment.txt` — reviewer environment and execution limits.
- `docs/reviews/gagamba-rc2a-02b-q1-artifacts/windows/` — independent Windows build, logs, exit codes, and TRX, including aborted sandbox attempts identified as such.
