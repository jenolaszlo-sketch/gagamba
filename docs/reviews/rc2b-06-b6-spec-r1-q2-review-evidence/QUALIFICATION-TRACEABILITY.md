# B6-SPEC-R1-Q2 — Specification-to-Qualification Traceability

Mapping from mandatory B6 security claims to the amended qualification plan
(`docs/plans/rc2b-06-b6-qualification.md` @ `77ee441`). Every row records:
specification section, required test/probe, positive control, adversarial condition,
expected result, failure result, environment, evidence artifact/observation, and the
acceptance boundary. Test classes: `V` = deterministic verifier, `I` = integration
deterministic, `N` = native, `N-root` = native requiring root mutation.

## 1. Claim → test matrix

| Claim | Spec § | Test | Positive control | Adversarial condition | Expected | Failure result | Env | Evidence observation | Acceptance boundary |
|---|---|---|---|---|---|---|---|---|---|
| C1 Artifact identity verifiable | §4.1, §4.5 | T01; V01–V03 | `candidate-A` verifies `Qualified` | none (control) | digest == manifest == receipt == qualification == selection | typed refusal | WSL2 x86-64 (N) | manifest, receipt, per-file hashes | exact equality; no optimistic state |
| C2 Installed bytes == accepted identity | §5.2 | T01; T02; V04 | exact closure passes T01 | one-byte mutation, same length | `ArtifactIntegrityMismatch` before target | admits tampered bytes | N/N-root | recomputed digest, process inventory | zero process/cgroup/marker |
| C3 Unauthorized substitution refused | §2.5, §7.6, §8 | T03,T04,T05,T06,T07,T08,T09,T10,T12,T19,T21,T24; V05,V06,V11,V12 | exact production artefacts | native/helper/RID/protocol/rebuild/test-coordinator/filter/deps/symlink/host/test-variant | typed refusal before execution | fallback or admission | V+N+N-root | refusal log, marker absence | no PATH/shared-store fallback; no target |
| C4 Trust invalidated on change | §5.2 s8, §6.3, §6.5 | T13,T15,T16,T17,T18 | verify-then-spawn unchanged | post-verify mutation / active flip / holder loss | refuse release; preserve stop; `Unknown` if uncertain | release of changed set | N/N-root/I | fd identity, echo, release count | physical release count 0 or the one already committed |
| C5 Unsupported fails conservative | §8 | T19,T20,T20c,T21; V07,V08,V09,V11 | readable anchor | missing/malformed/contradictory/unsupported | `Unavailable`/`Unqualified` | `Qualified` on bad input | V+N | verifier error, descriptor | no `Qualified` without full match |
| C6 Qualification association deterministic (F13) | §4.5 | T20c; V08 | accepted record + matching receipt | missing/hash-mismatch/multiple/stale/other-digest/other-RID/unaccepted/conflicting | `Unqualified` | selection by search or wrong record | V+N | receipt/record comparison, descriptor | exactly one, fail-closed |
| C7 Native dependency closure (F04) | §2.7 | T20a; V06 | invariant runtime with `ldd` inventory | substituted/incompatible OpenSSL/ICU/zlib; missing library; roll-forward | `RuntimeDependencyMissing`/`UnqualifiedInstallation` | admits mismatched native deps | V+N+N-root | `ldd`, `/proc/<pid>/maps`, refusal | every `NEEDED` classified; no unexpected load |
| C8 Installation identity handshake (F07) | §6.2 | T20b; T15 | full integrated bootstrap, both echoes, reaches HELD | echo mismatch/protocol/generation/missing-echo/replay | controller `Success=0`; abort; no domain | reaches HELD/RELEASE on mismatch | N | frame captures, echo payloads, journal | no HELD/RELEASE/domain on any mismatch |
| C9 Production/test separation (F02) | §2.5 | T24; V12 | `Production`-config assembly | test-config build with hooks; reflection scan | hooks absent; test variant refused | hooks present or variant admitted | V+N | reflection result, refusal, `buildConfiguration` | zero prohibited types/members |
| C10 No new release authority | §3.2, §4.6, §6 | T16,T18,T20b s7 | normal HELD/abort | controller death/all-holders-loss/echo | recovery never gets RELEASE | recovery receives release | N | `ReleaseAuthority=false`, writer fd absence | no disk-only adoption |
| F01/F05 T25 reconciliation | plan T25-A/T25-B | T25-A; T25-B | installed integrated v2 closure | missing closure member | T25-B passes; T25-A accounts skips | legacy test counted as pass | V(accounting)+N | no-skip log, echo frames | zero installation-closure skips |
| F06 Ancestor predicate | §5.2 s1 | T01,T04; V04 | root-owned non-writable ancestors | sticky world-writable ancestor | refusal | sticky ancestor accepted | V+N | mode/owner evidence, refusal | strict root-only, sticky dropped |
| F03 SafeCopyIn wording | §5.2 s6 | T10,T13 | claimed-digest path copies | source-root path replaced (root) | root-only residual; content digest-bound | overclaim of fd anchoring | N/N-root | wording + residual | no fd-anchoring claim |

## 2. Independent expected results

Every negative case starts from a passing positive control in the same environment
(plan §4). Observations are independent of the code under test: process inventory,
cgroup inventory, journal absence, coordinator frame absence, target-marker absence,
`lsof`//`proc` maps — not a boolean returned by the implementation.

## 3. Circular-verification inspection

Checked for each pattern:

| Circular pattern | Present? | Note |
|---|---|---|
| Verification reuses the same untrusted metadata as the producer | No | receipt/record are root-owned; receiver compares against its own manifest-derived expected value (§6.2.6) |
| Fixtures generated from the expected-value implementation | No | `candidate-A` is an independent build; `candidate-A-rebuild` is an independent rebuild (T07) |
| Test checks only output shape, not actual trust | No | T02/T08/T09/T10 assert refusal with independent process/cgroup/marker absence, plus exact-set/digest recomputation |
| Test accepts absence of failure as proof of protection | No | plan §4 requires independent negative evidence; missing marker is only meaningful because the fixture has a positive control |
| Test checks a mock/simulated boundary while claiming a native boundary | No | `N` cases use real root install, processes, cgroup, Bubblewrap, coordinator, journal; WSL2 is explicitly not native Linux |

## 4. D1 — Security-critical negative cases distinguishable

The plan distinguishes all applicable cases: valid/valid (T01); modified artifact with valid-looking evidence (T02/T10); wrong artifact under expected identity/location (T07/T08); missing/corrupted evidence (T20/T21); unsupported identity/format (T05/T06/T19); stale authorization after replacement (T17/T20c); interrupted/inconsistent installation (T14); untrusted caller-supplied trust material (T21; §6.2.6). Each has an expected refusal and an independent observation. No passing result is invented.

## 5. D2 — Exact claim boundaries

| Claim | Proven by plan? | Boundary |
|---|---|---|
| A controlled file-substitution probe refuses rejection **in that environment** | Yes (T02/T08/T09) | Does **not** prove resistance to every hostile filesystem race; root-only residual documented |
| Specification establishes a design requirement | Yes | Does **not** establish production enforcement — no installer/verifier runs |
| WSL2 x86-64 qualification | Yes (declared) | Does **not** imply native Linux-host or arm64 |
| Ordinary unelevated tests | Yes | Do **not** imply elevated/privileged host guarantees |
| Binary reproducibility | No | Explicitly disclaimed; source commit ≠ binary identity |

Overextended claims are none: the plan explicitly separates WSL2 from native Linux,
marks unavailable loader/feature cases as `Unsupported`/`Pending`, and states a
source-only rebuild is not a qualified binary (plan §7).

## 6. D3 — Environment-specific qualification

- Windows, WSL2 and native Linux are separated; "WSL2 is not native Linux" is stated
  in scope (§1.1), exit criteria (§7) and non-qualifications (§8).
- `N-root` cases record `Pending` when root is unavailable; a skip is not a pass.
- Elevated/privileged (root install) requirements are confined to `N-root`; runtime
  remains unprivileged.
- No expansion to new platforms: arm64 is unsupported until a matching seccomp
  policy/build exists (§1.1, D4).

## 7. Traceability defects found

- **Q2-F02 (Minor).** The plan body contains `B6-T20 — Installation verification
  unavailable` twice (lines 399 and 533 in the committed file). The §9 traceability
  table lists it once. This is a duplicated-section artifact; it does not change any
  obligation but reduces the plan's precision.
- No missing mandatory claim→test mapping was found: F01–F14 remedies each map to at
  least one plan obligation (T24, T25-A/B, T20a, T20b, T20c, V04, T10, T01/T04).

## 8. Adequacy verdict

For its **claimed specification scope**, the plan contains sufficient tests to
distinguish valid from invalid trust decisions: valid/valid, modified-with-evidence,
wrong-artifact, missing/corrupt/contradictory evidence, unsupported format, stale
authorization, interrupted install and untrusted caller material. It provides
positive controls, independent adversarial cases, independent expected results,
reproducible observations, failure classifications and bounded acceptance criteria.
The only traceability defect is the duplicated T20 section (minor, non-blocking).
