# Mandatory verification and P4 acceptance

The owner parsed the pinned Q1 evidence, including original XML counters and case multisets, VSTest TRX, fallback exits, supplemental results, mutated-build witnesses, build controls and compiled reference records. Every mandatory count and expected exit matched. All test identities, duplicate-name multiplicities and native skip reasons match the accepted baselines. [Owner audit receipt](acceptance-audit.json) · [Evidence-checking procedure](verify-acceptance.py)

| Gate | Debug accepted evidence | Release accepted evidence |
|---|---:|---:|
| E-5 focused | 42 passes | 42 passes |
| E-4 focused | 11 passes | 11 passes |
| E-3 focused | 185 passes | 185 passes |
| Broad managed M1 | 1,731 passes | 1,731 passes |
| Native | 331 unqualified skips | 331 unqualified skips |
| E-4 exact supplemental | 1,944 passes | 1,944 passes |
| Frozen E-5 sensitivity and fault controls | 17 passes | 17 passes |
| Independent Q1 | 696 passes | 696 passes |
| Independent omission controls | 20 detected plus 20 nominal passes | 20 detected plus 20 nominal passes |
| Direct production verifier | Expected refusal, exit 1 | PASS, exit 0 |

The focused suites are subsets of broad M1. The 1,731 broad passes include 1,689 inherited and 42 E-5 cases. Each E-4 supplemental ledger also records a separate disposable comparison-omission witness. All suites have zero failures and zero errors. The exact XML and multiset receipts support the counts. [Q1 execution summary](../../reviews/rc2b-06-b6-alt-b-slice-e-05-custody-q1-evidence/raw/execution-summary.json) · [Exact membership](../../reviews/rc2b-06-b6-alt-b-slice-e-05-custody-q1-evidence/raw/managed-membership.tsv)

The owner accepts all 51 passing build-policy controls, all nine passing preparation regressions, passing preparation validation, exact Release production verification and expected Debug-as-production refusal. Policy scope remains Windows PowerShell 5.1.26100.9818 and cached restores with `NuGetAudit=false`. Release-focused tests use the actual Release core and the required Release discovery copy. Broad Release preserves the authorized Debug fixture dependency. [Policy results](../../reviews/rc2b-06-b6-alt-b-slice-e-05-custody-q1-evidence/raw/policy-exact/results.json) · [Preparation regressions](../../reviews/rc2b-06-b6-alt-b-slice-e-05-custody-q1-evidence/raw/preparation-tests.log) · [Artifact and runner procedure](../../reviews/rc2b-06-b6-alt-b-slice-e-05-custody-q1-evidence/reproduction.md)

Eight VSTest 18.7.0 attempts ended at the 15-second testhost connection deadline, each at exit 1 with zero executed. Each subsequent authorized xUnit v3 3.2.0+9e422989c1 in-process run completed with exit 0 and the full expected membership. Q1-L01 through L04 retain their nonblocking runner, fixture/restore/policy, native/static-analysis and bounded exception qualifications. [Q1 findings register](../../reviews/rc2b-06-b6-alt-b-slice-e-05-custody-q1-evidence/findings.md)

## Compiled isolation and P4 ruling

**Neutral within the existing refusal-only scope**, at implementation commit `28895d55de98eea4bc402373b653f7216e092843` and tree `e314f2e8a1259e2263c8494bb0c8ca9f7eb27016`.

| Compiled paths | Debug | Release |
|---|---:|---:|
| Production and dependency/publish copies | 28 | 36 |
| Authorized test copies | 3 | 3 |
| Total | 31 | 39 |
| Unauthorized production model callers | 0 | 0 |
| New production E-3/E-4 consumers | 0 | 0 |
| Injected incoming reference | Detected, exit 1 | Detected, exit 1 |

The owner reconciled all 70 independent scanner rows with the separate reproduction inventory and hashes. Exact rows have no unexpected reference errors. Both independently injected incoming-reference builds have the expected changed identity and explicit call/signature detection. Four authorized test types provide positive model-reference controls. The source and compiled audit retain the permitted inert value-type/BCL dependencies and passive snapshot boundary. [Independent scanner summary](../../reviews/rc2b-06-b6-alt-b-slice-e-05-custody-q1-evidence/raw/independent-scan/summary.json) · [Q1 isolation method and limits](../../reviews/rc2b-06-b6-alt-b-slice-e-05-custody-q1-evidence/p4.md)

All 29 P4 dispositions persist as 21 Covered, five Authorized Continuation and three Legacy / Test-Only. Owner verification binds the original first-effect files to their unchanged published blobs. F01 continues to dominate installed S10/S11, S28 remains test-only, validation authority stays non-constructible and runtime activation remains unavailable. Per configuration, Q1 also records 15 targeted P4 witnesses and 32 additional arbiter witnesses. F-R1/O-R1, O-R2, O-R3, Q04/S09 and the last-observation-to-effect residual retain their original limits. [Source and 29-row reconciliation](../../reviews/rc2b-06-b6-alt-b-slice-e-05-custody-q1-evidence/raw/independent-p4-source.json) · [Executed P4 witnesses](../../reviews/rc2b-06-b6-alt-b-slice-e-05-custody-q1-evidence/raw/p4-executed.tsv)
