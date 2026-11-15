# Mandatory owner acceptance matrix

The dispositions below adjudicate the technical rows separately from the reviewer-independence gate. Evidence review does not amount to a new execution of the test suites.

| Area and governing requirement | Verified result | Determination |
|---|---|---|
| Original implementation and R2 scope, E-3 entry §§3–5 and adopted frozen §§2–5 | Both closed bootstrap schemas and binary contexts are covered by the Q3 source and conformance review. R2 changes one codec source and one focused test file, with the refusal-only E-3 boundary intact. Q3 is documentation-only. | Technically supported |
| Q1 findings, frozen §§3–4 and entry §3 | 17 exact cases compared against original evidence and R2 results in both configurations. Expected policy, duplicate, lifetime and digest ownership refusals matched. | All 17 technically resolved on R2 |
| Q2 findings, frozen §3 and entry §3 | All 13 exact inputs hash-match Q2. Original R1 exceptions reproduce in the Q3 record. Each corrected result is Expectation without data. | Both findings technically resolved on R2 |
| Q3 supplemental, frozen §3 | Verified each of 56 rows per configuration including semantic equality, decimal non-equivalence, large and small exponents and string representations. | Passed |
| Focused managed, entry §§4–5 | XML verifies 146 original, 24 R1 and 15 R2 for 185 combined passes per configuration, without failures or skips. | Passed |
| Broader M1 managed, entry exit evidence | XML verifies 1,678 passes and 331 skips per configuration, zero failures. Aborted VSTest excluded. | Passed for managed scope |
| Production build, entry §5 | Verified all 51 individual Passed records and preserved both build configurations. | Passed |
| Preparation validation, repository gate | Verified nine preparation regressions and preparation validator success. This owner commit also runs the validator with its new links present. | Passed |
| RELEASE, entry §5 | Hash-bound Release M1, result PASS, 14/14 required types, exit 0. | Passed |
| Debug negative, entry §5 | Hash-bound Debug M1, refusal diagnosed configuration and hooks, exit 1. | Passed negative control |
| Compiled isolation, entry §5 | 36 input paths, two known managed test callers sets, zero production references. Actual Owner, Recovery and InstallTool M1 copy hashes match their core. | Passed within static scan scope |
| P4, entry §5 and coverage acceptance | Same production and test reverse-call inventory as original, no new protected-effect path, preserved S10/S11 first-effect dominance and S28 sole test construction. | Neutral for exact R2 |
| Inventory, ancestry and evidence integrity, commission §§2–4 | R2/Q3 source identity exact. Git/working file hash verification for 156 Q1, 100 Q2, 64 R2 and 77 Q3 inputs, with adopted and inherited source identities. | Verified |
| Q3 reviewer independence, entry §5 and owner commission §4 | A fresh Q3 execution and separate probes were performed by the same task that changed the R2 source and tests. No distinct reviewer performed or attested that qualification. Signatures or author metadata would not establish review responsibility on their own. | **Unsatisfied and blocking** |

[Q3 conformance](../../reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/conformance.md) · [Q3 exact Q1 outcomes](../../reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/q1-matrix.md) · [Q3 exact Q2 outcomes](../../reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/q2-matrix.md) · [Q3 counts and run bindings](../../reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/summary.json) · [Q3 controls, verifier and scan paths](../../reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/reproduction.md)

## E3-OWNER-B01

The implementation and later Q3 review were performed in the same Codex task. The task wrote the R2 production change and tests, then authored the Q3 probes and report. The governing entry's distinct implementation, independent qualification and owner decision stages are substantive safeguards. The handoffs and the new oracles establish separate steps and useful verification evidence. They do not supply a conflict-free reviewer. This owner assessment does not downgrade the mandatory independence requirement to a non-blocking qualification. The smallest next step is a distinct review of the exact R2 revision and its evidence, with the reviewer's role recorded. The evidence does not justify redesign or further code remediation.
