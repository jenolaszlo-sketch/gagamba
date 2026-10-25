# RC2A-02C — Q2A-S / RC-D04 focused qualification

Status: **RC2A-02C QUALIFIED LOCALLY / READY FOR INDEPENDENT REVIEW.**

Q2A-S / RC-D04 remains **OPEN** until focused independent review and a
separate owner disposition. Full RC2A-02 is **NOT ACCEPTED**. This record does
not authorize RC2A-03.

## Baseline and scope

The isolated branch `qualification/gagamba-rc2a-02c-q2as-rcd04` starts at owner
authorization `91612b9a42827ffdd76891819670623fa43daeed`, whose parent is the
RC2A-02B accepted decision `dc02951902f11031f8b0d225d2afa2cd537296a3`. The
pre-execution [requirement-to-assertion matrix](qualification-matrix.md) was
committed first at `66a66bf56508b23b5730827a012ba730b702b72b`. Tests-only
follow-ups were committed at `90895ee0865caeeacce2570dce836f25e1e5279f`,
`e1be8579bcaca806071e88dcdd389fd2a054384b`, and
`c4837b0b434fc75ebc3e08d278bdc09c3965a181`. The final tested commit is
`c4837b0b434fc75ebc3e08d278bdc09c3965a181`, tree
`d47eb6173ecf7b4de442f0f9c165f3ef9e24c88d`; `src`, `tests` and `native`
trees at that commit are respectively
`b8c167e3d7340a562221591bdc2b6df7adb372d1`,
`9c906b349b0bf0cffdee331fb5700f9b7828c199`, and
`85631d2f04512376621d3d09f51d67fd90f45335`. Among `src`, `tests` and `native`,
only the test tree differs from the accepted RC2A-02B implementation tree due
to the focused assertions. Exact machine-readable bindings are in
[source-binding.json](source-binding.json).

This stage covers the five-field public managed value, its validating
constructor, defensive/immutable digest handling, content equality and hash
behavior, declared public surface, and R1-F04 documentation boundary. It does
not qualify a live provider or artifact descriptor, deadlines, retained-state
authorization, release, native confinement, package compatibility, Hufu or
downstream runtime enforcement.

## Result

All **13/13** matrix rows have supporting local evidence. The focused test run
passed **3 / 3, 0 failed, 0 skipped**:

- `PassiveReferenceCopiesAndComparesContents` — copied input and returned
  digest isolation, repeated reads, separately allocated content equality,
  equal hash codes, and inequality for each GUID and a changed digest byte.
- `PassiveReferenceRejectsInvalidInputsByParameterName` — each empty GUID;
  digest lengths 0, 31 and 33; null byte-array conversion to an empty span; and
  accepted length 32, with the required parameter names.
- `PublicSurfaceIsSealedReadOnlyAndPassive` — exported contract type inventory,
  exact five-property reference surface, allowed constructor and methods,
  immutable digest type, interface, no public setters/events/operators/fields or
  nested reference types, and a separated record of inherited public methods.

The [public-surface inventory](runs/windows/final/public-surface-inventory.json)
records all 18 exported types in the contract namespace and each type's public
declared constructors, properties, methods, operators, events, interfaces,
nested types and fields. For `RestrictedPreparationReference`, it records one
constructor, exactly five get-only properties, `IEquatable<T>`, typed and object
`Equals`, `GetHashCode`, and no operators, events, public fields or nested
types. Inherited `Object.Equals(Object,Object)`, `ReferenceEquals(Object,Object)`,
`ToString` and `GetType` are listed separately. No added
authority-bearing reference member was observed.

The [timing/documentation review](documentation-review.md) confirms the R1-F04
statement: D4a timing uses explicit enrolled or synthetic data; later D4b
timing comes only from an actual provider-origin prepared execution. A passive
reference proves neither timing nor current liveness. This documents the
boundary; it does not qualify either timing source or its consumer.

## Execution record

Environment: Windows x64 host, NT version 10.0.26200.0, build 26200.9711; the
Windows registry reports ProductName `Windows 10 Home Single Language` and
DisplayVersion `25H2`; PowerShell 7.6.5; .NET SDK 10.0.401; .NET runtime
10.0.12. The detailed
environment and command outputs are in `runs/windows/`. `dotnet --version`
returned 10.0.401. `dotnet --info` printed the SDK, then exited 1 because this
unprivileged shell could not open the Windows Service Control Manager; its
complete output is preserved.

Commands and captured outcomes:

| Command | Result | Artifact |
|---|---|---|
| Initial `dotnet build` at `90895ee` | Exit 1 during restore (`NU1900`); vulnerability service index unavailable in the restricted network; no compile or contract assertion failure | `runs/windows/iteration-90895ee/build.log`, `build.exit` |
| Offline-compatible build at final tested commit | Exit 0; build succeeded, 0 warnings, 0 errors, using the pinned project package versions already available locally | `runs/windows/final/build.log`, `build.exit` |
| Initial sandboxed focused `dotnet test` | Aborted before test execution: testhost connection timed out after 90 seconds, 0 tests executed | `runs/windows/iteration-90895ee/rc2a-02c-passive-reference.trx`, `test.log`, `test.exit` |
| Final focused `dotnet test` at `c4837b0b434fc75ebc3e08d278bdc09c3965a181` | Exit 0; 3 passed, 0 failed, 0 skipped | `runs/windows/final/rc2a-02c-passive-reference.trx`, `test.log`, `test.exit`, `vstest-diag.log` |

The precise filter used was:

```text
FullyQualifiedName~RestrictedContractTests.PassiveReferenceCopiesAndComparesContents|FullyQualifiedName~RestrictedContractTests.PassiveReferenceRejectsInvalidInputsByParameterName|FullyQualifiedName~RestrictedContractTests.PublicSurfaceIsSealedReadOnlyAndPassive
```

The first restore and sandboxed testhost failures are retained as environmental
evidence; neither counts as a pass. The final focused test run used normal
testhost process communication after the restricted run could not connect. Two earlier tests-only iterations also
passed 3/3, with inventories that were then expanded to include inherited
public instance and static framework methods. Those files are preserved under
`runs/windows/iteration-90895ee/` and `runs/windows/iteration-e1be857/`. The
final claim and complete inventory are bound to commit
`c4837b0b434fc75ebc3e08d278bdc09c3965a181`. Prior RC2A-02A/02B qualification
artifacts remain unchanged and are supporting history, not relabeled as
RC2A-02C execution.

## Integrity, limitations and handoff

No production, native, or raw prior qualification artifact changed. The only
tracked code change is in
`tests/Gagamba.Execution.Tests/Restricted/RestrictedContractTests.cs`; all
other changes are qualification documents and run evidence. The RC2A-02A and
RC2A-02B Windows/WSL2 counts remain those accepted in their existing records.
`document-sha256.json` binds the applicable plans, source/API, test and evidence
documents; `SHA256SUMS` covers every file in this evidence package except itself.

Local evidence supports the bounded passive-reference contract, with no
confirmed implementation counterexample. The testhost timeout and offline
NuGet audit failure are recorded setup limitations. This is a Windows-only
managed-code result. It makes no claim about WSL2/native Linux/macOS for this
stage or about provider-origin preparation, deadlines, artifact release,
retained-state authorization, sandboxing, packaging, reproducibility or Hufu.

The [validation record](validation.md) lists the ancestry, scope, inventory,
link and integrity checks. Applicable document hashes are in
[`document-sha256.json`](document-sha256.json), and all package file hashes are
in [`SHA256SUMS`](SHA256SUMS). The [reviewer handoff](reviewer-handoff.md)
requests focused independent review of the pre-execution matrix, tests-only
diff, exact API inventory, raw run artifacts, timing documentation and
exclusions. After that review, the owner must separately disposition Q2A-S /
RC-D04. Until then, the obligation remains open and RC2A-02C is not owner-accepted.
