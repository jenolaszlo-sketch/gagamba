# Gagamba RC2A-02B-Q2 — Independent Remediation Verification

**Verdict: VERIFIED WITH QUALIFICATIONS.** Both findings from Q1 are resolved.
Independent committed-blob hashing and TRX parsing found no new material defect.
The remaining qualifications are the previously declared Windows + WSL2 scope,
native-platform availability, and deferred obligations. This report does not
record owner acceptance.

## 1. Git identities and ancestry

Q2 was created in isolated worktree
`C:\Users\Laszlos\source\repos\Solo\q2`, branch
`review/gagamba-rc2a-02b-q2`, at final R1 tip
`1cd22b9b109ddba4673b11487bceea62d2e92117` before any Q2 changes.

| Role | Commit | Tree | Parent |
|---|---|---|---|
| RC2A-02A owner acceptance / RC2A-02B start | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` | `8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e` | `c1a01761f8d58136a381ad87269635c6c14ad093` |
| RC2A-02B execution commit | `5b4292d0fefc576664df61692dfad5cef6362879` | `3f76b861e689d04a17c64f8a3e71e57851c21f58` | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` |
| Original qualification evidence | `dd16baec48663d40cd29504d98e57e30dd0855df` | `96e86aac2d284637805809b54da72e76bf46f1fc` | `5b4292d0fefc576664df61692dfad5cef6362879` |
| Q1 independent review | `5bc396c2e5619906ae202afe0a4244dfd6024d9e` | `8ebbccb7b3787ce8e57bc74bbc93a8c1edb9e650` | `dd16baec48663d40cd29504d98e57e30dd0855df` |
| R1 correction commit | `12d914d319b1df3eedafc5439bc547dacb578651` | `35cc347f8e436bd1fec9997cb50ede81e561767c` | `dd16baec48663d40cd29504d98e57e30dd0855df` |
| Final R1 tip | `1cd22b9b109ddba4673b11487bceea62d2e92117` | `2f2bae947dee368ae073dd3fe7877380ca8854ff` | `12d914d319b1df3eedafc5439bc547dacb578651` |

These identities and parents were read from Git objects. The local refs still
point to qualification `dd16baec…`, Q1 `5bc396c…`, and R1 `1cd22b9…`.
Q1 and R1 are sibling histories from the qualification commit; Q1 is not an
ancestor of R1 or Q2. Q2 is a direct child of final R1. All four worktrees
(qualification, Q1, R1 and Q2) were clean at final verification.

The accepted RC2A-02A evidence is `c1a01761f8d58136a381ad87269635c6c14ad093`
(tree `8c51c3c2e06e6544f3ad0e8aceabfed8457d8c19`); tested implementation is
`71a28afe2623516fb4176e6a4ce7988a3d70df06` (tree
`70b932148cbfa59b0e424229879eebd36df2d80c`). At owner acceptance, qualification,
and final R1, the `src`, `tests`, and `native` trees remain
`b8c167e3d7340a562221591bdc2b6df7adb372d1`,
`5c3268286575cb55891fae9288627657f2d30ddf`, and
`85631d2f04512376621d3d09f51d67fd90f45335` respectively. Both recorded
execution sources remain the accepted baseline commit `e5fff9e…`.

## 2. Q1 finding R1-01 — artifact hashes

I independently parsed `results.json`, obtained each artifact’s raw bytes with
`git show 1cd22b9:<path>`, and calculated SHA-256 with Python `hashlib`. All 29
final references match. Comparing against `results.json` at the original
qualification commit identified exactly 14 changed digests; each corresponding
artifact Git blob is identical between `dd16baec` and final R1. The old 14 values
also match the execution commit’s manifest, confirming they named earlier bytes.

| Artifact | Recorded at qualification | Independently computed final SHA-256 |
|---|---|---|
| `runs/windows/windows-public.trx` | `be1e235875ea5f245296982e9733a7b40f1271174557e62311987ab959f3567c` | `a675daa1534899997fcd8fb21d47929fe85ff3610f9401e61278158fb010e79d` |
| `runs/windows/windows-public.log` | `48065acf0d96bdcd4ef765e1bc5b94560b33007679e9d63fb9a519d65bccaf49` | `263624ce7494a641ddc034ca0920db69ffead3d05c9d19e3119de474ea7d6015` |
| `runs/windows/windows-public.exit.txt` | `11918c0c24a8c1cfc005438c46dd510eae27ed0e3f28c397b420d6b1e72ab140` | `45ad8c9cee33fd85ae12285a12748110ad68fab190ccead18bf71a7f7364dec0` |
| `runs/windows/windows-required-set.trx` | `bce3dfa77768a15425c8a4c44a9d937caab506fb75ddf56fbf64600031ce458f` | `5006d59ff15772450df41bb033619df8a2b9d7d2fdc69c1d77503b319fe3f3af` |
| `runs/windows/windows-required-set.log` | `e71e10ebabd3da18093d974413cd89f814606dd9fc5a936d5cd75c647d383eab` | `b5383bf88a279eaa4f6f54844ef49cd18bcde826eb30c8294996d1427802461f` |
| `runs/windows/windows-required-set.exit.txt` | `050a1e4eccdb4fac65db072744ce90df72d72741fb2d750028f8c113841d4421` | `30c923aa518d702f7d10ff6b119396ebb4ddf37f3fa19062bf2735324daa9894` |
| `runs/windows/windows-required-set-rerun.trx` | `270d2a189ae5b8da77556c581cbe50dd98b408d50159e75e5b116d2da0e84bb0` | `54bc21c6d1d7c70a8a3e66a7202c2dd9e8642698cb0d9f4a85451b6e6c20222d` |
| `runs/windows/windows-required-set-rerun.log` | `36de2d786916f8d28721727fc28ae17225ef37d08856bbe29f43fb0a5eef6310` | `0022cb4c0b5995f15d8b7ef4ea6c3c5da4a66870c4127942719f32e79276b9ee` |
| `runs/windows/windows-required-set-rerun.exit.txt` | `f79c91d32f54d73aa9fafdb1362f865de6554af3eba9cc92591c2ad21844faa2` | `9843b543f9497d8696944ccbc77d34a073281a2517906b5ff36902584f9077a9` |
| `runs/windows/windows-plan-filter.trx` | `ecc09ae28191c8a44382b106eea48099ffeed5c54db07d784ebbb13010836301` | `93cf05b960133d8c19b9a1507ded16ff9e9832c40ccf1eea7c5f3077005025cd` |
| `runs/windows/windows-plan-filter.log` | `6fcf03815c026dd4f47deb47b09c00e37dd75ed0e0df5a1d6a25d19677d9f17f` | `7faa28f4005c724d9d24c101d04c93acda2064b8e38ece46707940738e960f93` |
| `runs/windows/windows-plan-filter.exit.txt` | `15315da001708a5205cfff581aefa25b3c1dc1d205984cfe979ac1a9598ddce4` | `1a598cb3a472be4e7be1ffd905f315e8b30008ff8d154dd55917849ae9185b46` |
| `runs/windows/build-execution.log` | `73b421651771ca90bca6310ba8f5b31e6f3dce2539696e026703a79c75ea79b6` | `2eee7922d5da3c72597ab70004bdd8068f26214f37b51f0f01a0ec5550383356` |
| `runs/windows/windows-environment.txt` | `f021ed4ac513b8c926dae9fea95e6bba54dbecb73ea8729465ddf7b54e584fa6` | `0aee498bcb594d310a83d9db271e652bed10b64cd2138cc94fd5f675859c011d` |

The other 15 references remain unchanged and match their committed bytes. All
43 `SHA256SUMS` entries were recomputed from final R1 Git blobs and pass. All
nine document bindings pass against LF-normalized committed bytes and byte
counts; their path set is identical to the original qualification manifest.
All 29 artifact blob object IDs match between the qualification and final R1
commits, so the remediation changed metadata and documentation only.

## 3. Q1 finding R1-02 — broad-filter explanation

I parsed the raw Windows TRX files and mapped each `UnitTestResult.testId` to
`TestDefinitions/UnitTest/TestMethod.className`. The broad-filter run includes
the 457-test precise result and 57 additional test results across exactly five
classes:

| Additional class | Source-level classification | Raw TRX outcome |
|---|---|---:|
| `CoordinatorProtocolQualificationTests` | Managed protocol-frame tests; no Linux guard | 11 passed |
| `Wp05V3IndependentQualificationTests` | Managed codec tests; no Linux guard | 5 passed |
| `NonRootQualificationTests` | Linux-only guard; supported-platform attribute | 31 skipped (`NotExecuted`) |
| `OwnerDeathQualificationTests` | Linux cgroup prerequisite guard; supported-platform attribute | 7 skipped (`NotExecuted`) |
| `Wp05ReleasePathIndependentQualificationTests` | Linux-only guard; supported-platform attribute | 3 skipped (`NotExecuted`) |

The raw broad-filter TRX totals **514 results: 473 passed, 0 failed, 41
NotExecuted**. The precise result is 457/0/0, so the five classes above are its
57-test delta. `results.json` now records the same two passing-class and three
skipped-class counts. The exact precise filter remains
`FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Tests.Qualification.`
The broad run remains provenance evidence only.

The corrected [README](../../evidence/restricted-consumer-contract-rc2a-02b-portability/README.md),
[qualification matrix](../../evidence/restricted-consumer-contract-rc2a-02b-portability/qualification-matrix.md),
and `results.json` describe this accounting accurately. The README and matrix
continue to label Windows and WSL2 results separately and native Linux/macOS as
not run. The unchanged [reviewer handoff](../../evidence/restricted-consumer-contract-rc2a-02b-portability/reviewer-handoff.md)
does not claim those hosts passed.

## 4. Review of the R1 verifier

I inspected the committed [R1 verifier](../gagamba-rc2a-02b-r1/verify_evidence.py)
and did not use it as the source of the independent results above. It does
calculate hashes from Git blobs, checks all listed artifact/manifests, parses
the raw TRXs, compares test-name/outcome multisets, and guards the source/raw
diff. Its logic is not self-recursive and does not rely on the Q1 verifier's
result file to determine the corrected hashes.

It is a fixed-target checker: it hard-codes the expected commits, branch,
worktree shape, path list, and expected class delta. Its scope is appropriate to
the recorded R1 snapshot, but it is not a general reusable evidence validator.
I found three coverage limits and checked them independently here:

- It verifies that nine document bindings pass but does not compare their path
  set with the original manifest. Q2 compared the exact path sets and verified
  all nine bindings.
- It reconstructs the broad-filter class delta from test names but does not
  compare the added `passedClasses` / `skippedClasses` fields in `results.json`
  with raw TRX class metadata. Q2 used TRX test IDs and `TestMethod.className`
  and confirmed both JSON maps.
- It infers classes by splitting `testName` at its final period and uses a
  simple Markdown-link regex. Q2 independently used the TRX class metadata and
  checked relative links in the affected evidence documents and review reports
  against their Git trees.

These are verifier coverage limitations, not R1 remediation defects. The
independent checks close those gaps for this review. All examined evidence and
Q1/R1/Q2 report links resolve.

## 5. Scope and preservation

The diff from qualification `dd16baec…` to final R1 `1cd22b9…` contains exactly
five evidence files plus the R1 report and verifier. `src`, `tests`, `native`,
and every raw file under `runs/` are byte-identical to the qualification commit.
All seven recorded run counters and build outcomes are unchanged. The source
binding still names `e5fff9e…`, its tree, and the same `src`/`tests`/`native`
subtrees at the baseline and qualification commits.

The preserved raw TRX supports the original managed-code claims:

| Environment | Public | Precise required set | Precise rerun | Broad provenance run |
|---|---:|---:|---:|---:|
| Windows native | 67/0/0 | 457/0/0 | 457/0/0 | 473/0/41 |
| WSL2 Ubuntu, labeled `wsl2` | 67/0/0 | 457/0/0 | 457/0/0 | Not run |

Raw Windows/WSL2 public and precise name/outcome multisets match; each required
set matches its rerun. Q1 independently rebuilt and reran the Windows public
and precise sets; Q1 inspected the preserved WSL2 evidence but did not rerun
WSL2 because it was inaccessible. Q2 independently parsed those committed
artifacts and did not rerun qualification suites.

The controlling plan §3 D P2 says Linux/macOS qualification is run natively
when hosts are available and requires an unavailable platform to be recorded as
“not run,” neither pass nor failure. §9.1.4 requires real native hosts for
native Linux/macOS evidence; it does not require provisioning such hosts for
this bounded stage. This agrees with Q1's plan interpretation. No new host
requirement is introduced.

## 6. Qualifications, open obligations, and disposition

Previously declared limitations remain:

- Windows and WSL2 support only the bounded managed-code value/codec scope.
- Native Linux and macOS remain **NOT RUN**.
- Native confinement, package compatibility, clean-consumer compatibility,
  binary reproducibility, and Hufu qualification are not established.
- Q2A-S / RC-D04 remains **OPEN**. RC-2B R1-F02/F03, the R1-F06 HUFU-R
  recheck, AE4-B01, Hufu resumption, RC2A-03/04, native qualification, and
  packaging/publication remain open, not authorized, or not qualified as
  previously recorded.
- Full RC2A-02 remains **NOT YET ACCEPTED**. This Q2 report does not accept
  RC2A-02B.

Q1 also disclosed that the qualification matrix asserts the separate §9.1.1
authorization instruction but the instruction's source was not preserved in the
evidence package. That is a pre-existing audit-trail note, not a defect
introduced by R1; the owner can retain or add the authorization pointer in the
acceptance record.

R1-01 and R1-02 are resolved. No new material defect was found. The verdict is
**VERIFIED WITH QUALIFICATIONS** because the evidence remains bounded to the
declared platforms and scope, with the open obligations above.

**Recommendation:** proceed to owner acceptance of RC2A-02B for the stated
Windows + WSL2 managed-code value/codec scope, carrying forward the disclosed
authorization-provenance note and all open obligations. No additional broad
review or full-suite rerun is indicated by this verification. Do not treat that
decision as acceptance of full RC2A-02 or authorization for RC2A-03.

## 7. Reproduction

From the Q2 worktree, run the independent read-only verifier:

```powershell
python docs/reviews/gagamba-rc2a-02b-q2-review/verify_q2.py
git diff --check 1cd22b9b109ddba4673b11487bceea62d2e92117 HEAD
git status --short --branch
```

The verifier independently calculates SHA-256 values from Git blobs, validates
both manifests and the raw TRX sets, checks the R1 diff and source bindings,
and verifies review-branch identities and evidence links. It does not rerun
the qualification suites or call the R1 verifier.
