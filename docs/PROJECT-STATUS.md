# Gagamba project status

Canonical current summary; historical decisions and independent reviews remain immutable.
Status date: 2026-10-28 (Asia/Manila).

## Accepted component baseline

RC2A-03 is **ACCEPTED WITH QUALIFICATIONS** by GAGAMBA-RC2A-03-OWNER-01 at
`1ce5c37f25c3bf49f55fc6a31a930f8430358d24`, root tree
`6b7605ee89ec9545926143875cfd3e2410c8ac48`, parent / accepted R1
`271e34b45fa504f6bb7ef7359c4f2f1a9649e49d` (R1 tree
`f3f63129be98c5c9f9efba3302da331ae65139ee`).

Authority: [owner decision](decisions/restricted-consumer-contract-rc2a-03-owner-acceptance.md).
The owner decision's §§5–6 are the complete qualification/exclusion register.
Q2 report `a58f1b2a07d959c763bf6d08143797846948fe74`, final receipt
`e060c91225274f13d53421b613a5736225ef6de4` (tree
`23bd0006ba1178f3b84ee510d89c5c0179cddc91`) supports all 13 claims with qualifications
and closes Q1 F01–F04 **for R1**. Q1 `dd7e47d2c5f75af126ce4a081013bf21182d73ee`
remains authoritative historical evidence against original `c946df0`.
Review/assessment histories remain separate, linked by immutable commit identity.

Exact protected identities:

| Object | Git identity |
|---|---|
| src | `b8c167e3d7340a562221591bdc2b6df7adb372d1` |
| native (preservation only) | `85631d2f04512376621d3d09f51d67fd90f45335` |
| tests | `9c906b349b0bf0cffdee331fb5700f9b7828c199` |
| Frozen API document | `2d6b9d12cab02f092c6b6e0b690138cf6726f12b` |

## Security and evidence boundary

Accepted scope is the passive restricted consumer value/codec/reference component.
Successful decoding grants no execution authority and proves no establishment or isolation.
No digest or passive reference establishes producer/provider provenance, temporal/deadline/
freshness/liveness, release/retained-state authority, sandbox confinement, recovery or lifecycle.
R1-F04 and D4a enrolled/synthetic versus D4b actual provider-origin timing-source limits remain.

Windows is primary; the RC2A-02A/B managed assertion set has separately preserved WSL2 evidence.
RC2A-02C's focused reference/surface qualification is Windows-only. WSL2 is not native Linux;
native Linux/macOS **consumer qualification** remain NOT RUN. Earlier native-track evidence
retains its own separately scoped dispositions and is not imported as consumer qualification.

Carry forward: two digest/recipe-bound large vectors; no binary/package/distribution/deployment
reproducibility or cross-process hash-determinism guarantee; corrected RC2A-01 R2A-F03 origin
and disclosed historical citation error; 40-count erratum; authorization audit-trail and review
method disclosures; canonical-rigidity correction and precise/broad-filter accounting; preserved
failed attempts and B R1/Q2 corrections; bounded re-encode reasoning; differing raw CRLF/LF
versus equal normalized/JSON representations; qualified E2/E6 and no mandatory consolidated run.
Existing R2A-F01/F02/F04 and R1-F01/F05 dispositions remain scoped.

## Milestones and outstanding work

| Item | Current disposition |
|---|---|
| RC-1 / RC1-A | Accepted with recorded qualifications |
| RC2A-01 implementation | Accepted with follow-up; follow-ups retain their scoped later dispositions |
| RC2A-02 | Accepted with qualifications at `9519e7cb612af6ff69c93c0d128553138533d9fd` |
| RC2A-03 | Accepted with qualifications at the exact owner decision above |
| RC2A-04 local package/clean consumer/mixed graph | **QUALIFIED WITH RESTRICTIONS / REVIEW REQUIRED**; independent review and owner acceptance pending; [evidence](evidence/rc2a-04/README.md) |
| RC2B / RC-2B facade | Not established; R1-F02/F03 OPEN |
| HUFU-R / R1-F06, AE4-B01 and Hufu integration | OPEN/BLOCKED; no integration authorization |
| Native C1 review, C3/C4 and broader native track | Separate commissions/dispositions; not accepted by RC2A-03 |
| GAGAMBA-INTEGRATION-01 | Stage 2A preservation verified; accepted baseline and 23 preservation refs published under Stage 2B authorization; final verification and CI outcome recorded separately |

## Repository integration state and next owner decision

The owner explicitly authorized GAGAMBA-INTEGRATION-01 Stage 2B on 2026-10-28:
publication of the exact 23 preservation refs and their histories, disclosed low-sensitivity
path details, controlled workflow suspension/restoration, accepted-lineage fast-forward,
this three-file documentation child and one post-push CI run. The other 77 provenance
finding groups repeat previously public account/path/hostname information.

The 23 checkpoint/review/assessment refs and accepted baseline were published and
verified before this documentation commit was prepared for installation. Seven review
and two assessment branches retain their independent histories; none was merged into main.
The accepted implementation/decision baseline remains exactly
`1ce5c37f25c3bf49f55fc6a31a930f8430358d24`. The integration status commit `12578dd41ce23b2e890c6f6b4a027db074f31f50` is its
direct child. Subsequent link maintenance and RC2A-04 qualification tooling retain
the production, native, API and test identities above unchanged.
Moving historical native code into main grants it no broader acceptance.

Stage 2A recovery was verified from the Git bundle, cold administrative metadata and all
72 worktree snapshots. The owner accepted the same-physical-disk backup limitation for
this non-destructive integration. Backup data and local-only work remain private.
No branch deletion, worktree retirement, repository migration, tag or package publication
is authorized. A verified off-device copy remains required before destructive retirement.

Final remote commit/ref verification, restoration of the three workflow states, and the
single CI run's exact final-main SHA binding and outcome are recorded in the separate
Stage 2B completion receipts and GitHub Actions. This summary does not substitute for
those results. Ordinary CI build/test/pack/probe results do not establish new consumer,
native-runtime, installed-package or distribution qualification.

RC2A-04 was separately commissioned on main. Exact local package source is
`82cf8983a61f60a95d0aed0233ef0efac985148b` (tree
`d37ad20f2d7e5ee4ee705e4c78b766df32b61f0c`); candidate version is
`0.1.0-rc2a.82cf8983a61f60a95d0aed0233ef0efac985148b`.
The clean Windows package consumer passed 1,227 assertions using 21 exported vectors;
Windows and WSL2 each passed 48 public plus 457 precise portable regressions (0 failed,
0 skipped). Five coherent local packages and symbols were inspected. The mixed graph
with Hufu core preview.11 restored and loaded managed types without native activation;
the Hufu Sandbox package is unavailable, so its integration graph remains unqualified.
V19/V20 remain recipe/digest-bound, native Linux/macOS consumer qualification remains
NOT RUN, and no binary reproducibility or execution-authority claim is added.
No package was published. Automatic CI for the evidence commit is recorded separately.

Next owner decision: after independent review of the RC2A-04 source, actual local
packages and evidence, accept with restrictions or require bounded remediation.
No subsequent milestone starts automatically. RC2B, native and Hufu gates retain
their independent dispositions.

Maintain this file as the single current project summary; handoff/work-queue point here.
Keep accepted decisions, qualification packages and independent reviews immutable.
