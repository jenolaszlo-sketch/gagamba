# Mandatory remediation verification

This matrix applies to `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-06-RELEASE-PREP-F02-REMEDIATION-01`. The blocked producer results remain attributed to `53ffe7a21408a7e9674cf50b7098a26887c070b5`. Measurements in this matrix are required on the resulting exact remediation commit. [Original V01–V10 matrix and isolation method](../rc2b-06-b6-alt-b-slice-e-06-release-prep-entry-auth-01-evidence/verification.md) · [Blocked execution results](../../evidence/rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/execution-summary.json)

| Required gate | Debug | Release |
|---|---|---|
| E-6 focused | 192 passed | 192 passed |
| E-5 focused | 42 passed | 42 passed |
| E-4 focused | 11 passed | 11 passed |
| E-3 focused | 185 passed | 185 passed |
| Broad M1 | 1,923 passed, zero failures | 1,923 passed, zero failures |
| Inherited E-2 naming invariant | Pass | Pass |
| Native skips within broad M1 | Same 331, unqualified | Same 331, unqualified |
| R01 and mixed-GUID vectors | Exact full 258-byte preimages and 32-byte raw digests | Same |
| R02 altered expected digest | `DigestMismatch`, null material | Same |
| 34 E-6 mutation pairs | Nominal controls pass and all 34 deviations are detected | Same |
| E-4 supplement | 1,944 cases and catalog-omission detection | Same |
| Frozen E-5 sensitivity and faults | 17 controls | Same |
| E-5 Q1 regression replay | 696 cases, 20 nominal and 20 detected omissions | Same |
| E-5 Q1 and arbiter P4 witnesses | Preserve 15 + 32 witnesses and reconcile overlap | Same |
| Compiled incoming references | Zero production callers, zero new E-3/E-4/E-5 consumers, detect injected caller | Same |
| Direct production verifier | Expected Debug refusal and exit 1 | PASS and exit 0 |
| Exact-revision P4 | Retain all 29 refusal-only dispositions and record effect-edge audit | Same |

Run all 51 existing build-policy probes in the established Windows PowerShell 5.1 scope, all nine preparation regressions and preparation validation. Preserve raw outcomes, commands, exits, selected test and dependency hashes, module identities, manifests, cleanup, and case membership including skip reasons. Rebind actual Debug and Release core, test, fixture, discovery, production and publish copies and repeat the compiled dependency and dynamic activation analysis. Exercise the unchanged E-2 naming check against each actual focused core, including Release. The broad Release suite retains the authorized Debug fixture reference. Record any VSTest zero-execution timeout before using the authorized xUnit v3 in-process fallback. Cached restore with advisory checks disabled supplies no advisory coverage. [Producer replay procedures](../../evidence/rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/reproduction.md)

The restored broad total is 1,731 inherited passes plus 192 E-6 passes in each configuration. The two `MissingMember` theory display names carrying the old member spelling are expected to change solely by the frozen identifier substitution. Compare the inherited naming case to the exact historical case ID and the resumed passing outcome. Preserve other case IDs and statuses with multiplicity and all 331 historical skip identities and reasons. The existing mutation harness binds the old field name in the equality-omission witness for slot zero. Update that reference in a scratch copy, retain `ExactHashAssociation(0, false)` and the `ProjectionMismatch` oracle, and prove the same deviation is detected. Preserve the original producer procedures and mutation outputs at their pinned revisions.

Verify the minimal two-path diff from the blocked candidate, all historical identities, all manifests and the 44 untracked files, with the inherited E-2 test byte-identical. Reconcile any unexpected result before proposing readiness. Independent Q1 remains mandatory after successful producer remediation, in a fresh task uninvolved in implementation and producer verification. It repeats the exact-candidate V01–V10, independent input/vector, sensitivity and isolation requirements before separate owner acceptance.

## Owner verification replay

Copy the `verify-f02-owner.py.txt` procedure to writable external scratch without its evidence suffix. Run it at the starting producer commit for entry verification. For a post-commit replay, place a byte copy of this annex's `preflight.json` beside the script and pass `--final`. The script pins the historical scratch authorization audit and verifies Git-backed evidence, both inventories, live and tracking origin, raw candidate source bindings, full independent vectors and canonical preservation. The current documentation commit and tree are issued in an external receipt to avoid self-reference. [Preflight procedure](verify-f02-owner.py.txt) · [Preflight receipt](preflight.json)

The `inspect-member-names.ps1.txt` evidence procedure reads the blocked producer DLLs where available and validates their hash bindings before reflecting types and members. Run it in a distinct PowerShell process for each configuration with the canonical repository, producer scratch, output directory and `debug` or `release` as parameters. The `check-conflict.py.txt` procedure consumes the preflight and both metadata receipts and checks the manifest-bound historical logs and mutation results. These procedures record only the conflict and name projection. [Metadata inspection](inspect-member-names.ps1.txt) · [Conflict replay](check-conflict.py.txt)
