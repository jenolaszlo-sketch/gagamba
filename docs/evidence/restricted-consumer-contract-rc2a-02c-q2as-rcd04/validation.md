# RC2A-02C evidence validation record

Validation was performed against the focused qualification and its exact test
commit. These checks do not qualify downstream runtime authorization.

## Git identity, ancestry and scope

The evidence binding records the chain from owner authorization
`91612b9a42827ffdd76891819670623fa43daeed` through matrix commit
`66a66bf56508b23b5730827a012ba730b702b72b`, tests-first commit
`90895ee0865caeeacce2570dce836f25e1e5279f`, inventory corrections
`e1be8579bcaca806071e88dcdd389fd2a054384b`, and final tested commit
`c4837b0b434fc75ebc3e08d278bdc09c3965a181`. `git merge-base --is-ancestor`
returned success for the owner baseline and pre-execution matrix as ancestors
of the final tested commit. Q1 and Q2 review commits are not ancestors of this
branch.

The accepted/review refs were read without modification:

| Ref | Observed commit |
|---|---|
| `decisions/gagamba-rc2a-02a-owner-acceptance` | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` |
| `decisions/gagamba-rc2a-02b-owner-acceptance` | `dc02951902f11031f8b0d225d2afa2cd537296a3` |
| `review/gagamba-rc2a-02b-q1` | `5bc396c2e5619906ae202afe0a4244dfd6024d9e` |
| `review/gagamba-rc2a-02b-q2` | `60de1f2d34a7df337a426501cb3954fdd3216a6b` |
| `decisions/gagamba-q2as-rcd04-disposition` | `91612b9a42827ffdd76891819670623fa43daeed` |
| `qualification/gagamba-rc2a-02b` | `dd16baec48663d40cd29504d98e57e30dd0855df` |

Commands used for the scope comparison:

```powershell
git diff --name-status 91612b9a42827ffdd76891819670623fa43daeed c4837b0b434fc75ebc3e08d278bdc09c3965a181 -- src tests native
git diff --quiet 91612b9a42827ffdd76891819670623fa43daeed c4837b0b434fc75ebc3e08d278bdc09c3965a181 -- src native docs/evidence/restricted-consumer-contract-rc2a-02a-windows docs/evidence/restricted-consumer-contract-rc2a-02b-portability
```

The first command reports only
`M tests/Gagamba.Execution.Tests/Restricted/RestrictedContractTests.cs`.
The second exits 0. At the tested commit, `src` is
`b8c167e3d7340a562221591bdc2b6df7adb372d1`, `tests` is
`9c906b349b0bf0cffdee331fb5700f9b7828c199`, and `native` is
`85631d2f04512376621d3d09f51d67fd90f45335`. The raw RC2A-02A and RC2A-02B
qualification packages are byte-preserved by Git history.

## Run and inventory checks

The final TRX reports `total=3`, `executed=3`, `passed=3`, `failed=0`,
`skipped=0`; the build log reports 0 warnings and 0 errors. The initial
NU1900 restore failure and the sandboxed testhost timeout (0 tests executed)
remain preserved as environment observations, not qualification passes or
contract failures.

The inventory contains 18 exported contract namespace types. For the passive
reference it records one constructor with signature
`(Guid, Guid, Guid, Guid, ReadOnlySpan<byte>)`, exactly five get-only
properties, `IEquatable<RestrictedPreparationReference>`, the typed/object
`Equals` overloads and `GetHashCode`. It records no declared public operator,
event, field or nested type. Inherited public `Object` methods are separate,
including static `Equals(Object,Object)` and `ReferenceEquals(Object,Object)`.

## Link checks

The evidence package markdown has **14 relative links checked / 0 broken**.
An extended check across the package and four updated plan/handoff documents
checked 371 relative links; 368 resolve. Three unrelated WP05 historical review
links in `docs/handoff.md` were already unresolved at the owner-authorization
baseline and are unchanged by RC2A-02C. No newly added RC2A-02C evidence link is
broken.

## Integrity manifests

`document-sha256.json` binds applicable source, API, plan, test and evidence
documents using the repository's LF-normalized byte rule. `SHA256SUMS` covers
all package files except itself. Both were independently recalculated from the
final evidence files: **16/16 document bindings** and **39/39 package checksums
passed**. Source binding, ancestry, the 18-type inventory assertions, final TRX
counters and `git diff --check` also passed. The same checks were repeated after
the evidence commit and worktree cleanup; see the final commit and clean status
reported with the handoff.

## Qualification boundary

No production or native source changed, no previous raw qualification evidence
changed, and the full qualification suites were not rerun. The result covers
only the passive managed-code value and R1-F04 documentation boundary. Q2A-S /
RC-D04 remains open pending focused independent review and separate owner
disposition.
