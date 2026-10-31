# B4Q1-F01-R2 Cleanup Classification Type Hardening

Date: 2026-10-31
Remediation slice: GAGAMBA-B4Q1-F01-R2
Baseline source SHA: `9bae9a724aea54710a80527a9404b62cdc224882` (R1 documentation tip; parent of the R2 implementation commit)
Final source SHA: `0e89cc710fb235569191baf44d9188eda8c9f9a8` (tree `4caf454100cbcc3999467d55b95bbc7c564c0766`); this document is its documentation-only child commit on `main`
Branch: `main`
Host OS/version: Windows 11 (NT 10.0.26200.0)
Architecture: x86_64
Runtime/SDK version: Python `3.14.8` (Windows deterministic tests); Ubuntu `python3` 3.14 for the WSL2 read-only reclassification check against the retained WSL2 evidence
Relevant native prerequisites: none added or provisioned; the reclassification check only reads the previously qualified WSL2 workspace evidence
Qualification scope: strict type handling in the shared cleanup classifier (`eng/int01_cleanup.py`) and its deterministic tests; reclassification of the accepted 32-case INT-01 raw evidence with the hardened module
Explicit exclusions: no production source, durable-journal format, lifecycle, security-contract, release/recovery authority or native-harness behavior change; no new native matrix execution; no B5 work; no historical producer, R1 evidence or independent-review artifact was modified

---

## 1. F02 finding and required correction

The focused independent review's F02 qualification identified that the R1
classifier still relied on Python equality for two type-sensitive values:

- `classify_dispose` used `dispose_cleanup.get('State') == 1`. Python treats
  `True == 1` and `1.0 == 1` as true, so a boolean or floating-point `State`
  was promoted to `LiveCleanupConfirmed` even though the on-wire disposition
  state is an integer enum.
- Both `classify_final_record` and `classify_dispose` compared the supplied
  durable verdict directly with `2` (`2.0 == 2`), so a float verdict could be
  reported as Confirmed if it bypassed the file/`journal-status` readers.
  Malformed values were also echoed raw in the `DurableVerdict` evidence field
  instead of being normalized by the shared strict validator.

F02 requires exact integer typing at the classification boundary, not merely
numeric equality.

## 2. Correction

All changes are confined to `eng/int01_cleanup.py` and
`eng/tests/test_int01_cleanup.py`.

- `_verdict()` remains the single shared durable-verdict validator and now
  documents that booleans are `int` subclasses in Python but are not valid wire
  verdicts; floats, strings, null and out-of-range integers are rejected.
- Both classification entry points now normalize their durable-verdict argument
  through `_verdict()` before comparison and evidence recording, so malformed
  input cannot confirm cleanup and `DurableVerdict` is `null`.
- A new `_dispose_state()` accepts only an exact-integer live disposal state;
  `classify_dispose` requires that validated state to equal `1`.
- Existing evidence fields are unchanged: `DurableVerdict`, `FinalFlags`,
  `PhysicalCleanupObserved`, `PhysicalCleanupImproved`, `DisposeState`,
  `LiveCleanupConfirmed`, `State`. The `Confirmed`/`Unknown` vocabulary is
  unchanged. `DisposeState` still records the observed raw value for evidence,
  while `LiveCleanupConfirmed` reflects strict validation.

No production source, journal record, journal reader, security assertion,
release/recovery authority or native runner changed.

## 3. Baseline-to-fix evidence

Tests were added first and run against the unchanged R1 module; the failing
run is preserved at `artifacts/rc2b-04-b4-q1f01-r2/tests-failing-before.log`
(SHA-256 `991228d8733c197bc9c075e8b28486039f1e69a7ef9dbf477a43cd36a60ddcae`).

| Probe / test | Baseline failure (R1 module) | Implementation change | Fixed observation |
|---|---|---|---|
| `classify_dispose({'State': True}, 2)` | `Confirmed`, `LiveCleanupConfirmed=true` | exact-int state validator | `Unknown`, `LiveCleanupConfirmed=false` |
| `classify_dispose({'State': 1.0}, 2)` | `Confirmed`, `LiveCleanupConfirmed=true` | exact-int state validator | `Unknown`, `LiveCleanupConfirmed=false` |
| `classify_dispose({'State': '1'}, 2)` | `Unknown` (already rejected by equality) | exact-int state validator | `Unknown` (regression case retained) |
| `classify_dispose({'State': 1}, 2)` | `Confirmed` | valid integer control | `Confirmed`, `DisposeState=1` |
| `classify_final_record(flags, True)` | `Unknown` but `DurableVerdict=true` raw | shared validator at entry | `Unknown`, `DurableVerdict=null` |
| `classify_final_record(flags, 2.0)` | `Confirmed` | shared validator at entry | `Unknown`, `DurableVerdict=null` |
| `classify_final_record(flags, '2')` | `Unknown` but `DurableVerdict="2"` raw | shared validator at entry | `Unknown`, `DurableVerdict=null` |
| `classify_final_record(flags, 2)` | `Confirmed` | valid integer control | `Confirmed` |
| null / missing / non-dict / out-of-range dispose states | mixed | strict state and payload validation | all `Unknown` |

The failing-before run recorded `Ran 16 tests ... FAILED (failures=5)`:
`True` and `1.0` dispose states, plus `True`, `2.0` and `"2"` durable verdicts.
The fixed run recorded `Ran 16 tests ... OK`
(`artifacts/rc2b-04-b4-q1f01-r2/tests-passing-after.log`, SHA-256
`41a1865e99c65e08e82ff16019436e1053068bd2fab61820e633d748dadb90c5`).

## 4. Deterministic test results

| Check | Result |
|---|---|
| `python -m unittest eng.tests.test_int01_cleanup -v` | **16 passed / 0 failed / 0 skipped** |
| `python -m unittest discover -s eng.tests -p "test_*.py"` | **35 passed / 0 failed / 0 skipped** |

The complete `eng/tests` log is retained at
`artifacts/rc2b-04-b4-q1f01-r2/eng-tests-all.log` (SHA-256
`a0cc5096ce13d0854e46a4b3b8762de55d0757e30cfd5ff84db96ae01e131932`).

## 5. Accepted INT-01 interpretation

The accepted 32-case INT-01 raw evidence from R1 was re-read and reclassified
from its recorded `RecoveryFinal`/`FinalObservation` plus the durable verdict in
each retained WSL2 workspace, using the hardened module (no new native matrix
execution, no evidence rewriting).

Command: `wsl.exe -d Ubuntu -u gaga -- python3 <repo>/artifacts/rc2b-04-b4-q1f01-r2/reclassify-int01.py`

Result:

```text
cases=32 confirmed=20 unknown=12 other=0
flips vs accepted: []
```

`missing-staging-ack` remains **Unknown** with durable verdict `4`;
`missing-domain-ack` remains **Unknown** with durable verdict `0`; the accepted
**20 Confirmed / 12 Unknown** interpretation is unchanged. Log:
`artifacts/rc2b-04-b4-q1f01-r2/reclassify-int01.log` (SHA-256
`388018d68608afbd0f908d02bd3088d94f214610b5d20127993cbf849ce76fc0`).

## 6. Independent verification of the exact patch

A fresh-context independent verification agent (OpenCode general-purpose
subagent) reviewed commit `0e89cc710fb235569191baf44d9188eda8c9f9a8` only,
read-only, with no file changes, worktrees, commits or pushes. Scope: diff
contents and scope, strict type behavior at both entry points, evidence-field
preservation, the two test commands, and the INT-01 reclassification.

Reported result: **SUPPORTED — no findings.**

- Files changed by the commit: `eng/int01_cleanup.py` (+16/−2) and
  `eng/tests/test_int01_cleanup.py` (+40); no other paths or mode changes.
- `_verdict()` uses `type(value) is int`; both entry points normalize through
  it; `_dispose_state()` accepts only exact `int` and requires `== 1`.
- `DurableVerdict` records the normalized value; malformed input yields null.
- Evidence field names and the `Confirmed`/`Unknown` vocabulary are preserved.
- Tests: classifier **16/16 OK**, complete `eng/tests` **35/35 OK**.
- Reclassification: `cases=32 confirmed=20 unknown=12 other=0`,
  `flips vs accepted: []`.

This verification is evidence about the review process, not a substitute for
owner acceptance.

## 7. Preservation and qualification boundary

The R1 evidence document, the original producer/reviewer records, the focused
independent review and all earlier checkpoints are unchanged. This slice adds
this separate R2 record and current-status entries only.

Not established: new native execution was not performed for R2 (none was
required by the type-only change), and nothing here extends the WSL2
qualification to native Linux/macOS, VM/HCS, B5 sealed-C3, B6 attestation, B7
lifecycle qualification, Hufu, packaging or production. No production source,
journal contract, security assertion or release/recovery authority changed, and
B5 was not started.
