# Gagamba RC2A-02B-R1 — Evidence Remediation Report

**Status: RC2A-02B-R1 REMEDIATED / READY FOR INDEPENDENT VERIFICATION.** This
report records evidence corrections only. It is not an owner acceptance or a
replacement for independent verification.

## Baseline and Git identities

| Role | Branch / identity | Commit | Tree |
|---|---|---|---|
| RC2A-02A owner acceptance and RC2A-02B start baseline | `qualification/gagamba-rc2a-02b` ancestor | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` | `8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e` |
| RC2A-02B qualification evidence | `qualification/gagamba-rc2a-02b` | `dd16baec48663d40cd29504d98e57e30dd0855df` | `96e86aac2d284637805809b54da72e76bf46f1fc` |
| Execution commit between baseline and qualification evidence | qualification history | `5b4292d0fefc576664df61692dfad5cef6362879` | `3f76b861e689d04a17c64f8a3e71e57851c21f58` |
| Q1 independent review, preserved unchanged | `review/gagamba-rc2a-02b-q1` | `5bc396c2e5619906ae202afe0a4244dfd6024d9e` | `8ebbccb7b3787ce8e57bc74bbc93a8c1edb9e650` |
| R1 evidence remediation | `remediation/gagamba-rc2a-02b-r1` | `12d914d319b1df3eedafc5439bc547dacb578651` | `35cc347f8e436bd1fec9997cb50ede81e561767c` |

The qualification evidence commit is the direct parent of the Q1 review commit.
The remediation branch starts at the qualification evidence commit. The Q1
review branch remains separate and unmerged. The report and verifier are added
as a child documentation commit on the remediation branch; the final branch tip
is returned with this report.

The preserved Q1 review report at
`docs/reviews/gagamba-rc2a-02b-q1-review.md` and its machine-readable results
at `docs/reviews/gagamba-rc2a-02b-q1-review/verification-results.json` remain
on the review branch at the identity shown above; they are not copied here.

The recorded `src`, `tests`, and `native` subtree identities remain respectively
`b8c167e3d7340a562221591bdc2b6df7adb372d1`,
`5c3268286575cb55891fae9288627657f2d30ddf`, and
`85631d2f04512376621d3d09f51d67fd90f45335` from the accepted baseline through
the qualification commit.

## Findings and corrections

**R1-01 — incorrect `results.json` hashes.** Fourteen Windows artifact digests
were replaced with SHA-256 values computed from the artifact blobs committed at
`dd16baec48663d40cd29504d98e57e30dd0855df`. The raw TRX files, logs, exit
captures, environment capture, build outcomes, test counts, and qualification
outcomes were not changed. Before → after values are:

| Artifact | `results.json` before | SHA-256 of committed bytes after |
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

The other 15 artifact references in `results.json` were already correct and
remain unchanged. All 29 references now match the final committed artifact
bytes.

**R1-02 — incorrect broad-filter explanation.** The explanation in the
[evidence README](../../evidence/restricted-consumer-contract-rc2a-02b-portability/README.md),
[qualification matrix](../../evidence/restricted-consumer-contract-rc2a-02b-portability/qualification-matrix.md),
and `results.json` now reflects the Windows TRX:

| Additional class beyond the precise set | Kind | Raw Windows outcome |
|---|---|---:|
| `CoordinatorProtocolQualificationTests` | Managed code | 11 passed |
| `Wp05V3IndependentQualificationTests` | Managed code | 5 passed |
| `NonRootQualificationTests` | Linux-native | 31 skipped (`NotExecuted`) |
| `OwnerDeathQualificationTests` | Linux-native | 7 skipped (`NotExecuted`) |
| `Wp05ReleasePathIndependentQualificationTests` | Linux-native | 3 skipped (`NotExecuted`) |

The broad-filter total is **473 passed / 0 failed / 41 skipped** (514 results).
The precise `Tests.Qualification.` filter remains the qualification selection
mechanism. The broad-filter run is provenance evidence only and does not replace
the precise set.

## Changed-file inventory

The remediation commit `12d914d319b1df3eedafc5439bc547dacb578651` changes:

- `docs/evidence/restricted-consumer-contract-rc2a-02b-portability/README.md`
- `docs/evidence/restricted-consumer-contract-rc2a-02b-portability/qualification-matrix.md`
- `docs/evidence/restricted-consumer-contract-rc2a-02b-portability/results.json`
- `docs/evidence/restricted-consumer-contract-rc2a-02b-portability/document-sha256.json`
- `docs/evidence/restricted-consumer-contract-rc2a-02b-portability/SHA256SUMS`

The report commit adds this report and
[`verify_evidence.py`](verify_evidence.py). No production, test, native, or raw
qualification artifact changed.

## Integrity and qualification validation

The verifier reads committed blobs from Git and checks the final branch contents.
Its recorded results are:

- `results.json`: **29/29** artifact hashes match; 14 corrected and 15 retained.
- `SHA256SUMS`: **43/43** entries match.
- `document-sha256.json`: **9/9** LF-normalized document bindings match.
- Raw TRX counters match the recorded Windows and WSL2 results. Windows public:
  67/0/0; precise set and rerun: 457/0/0 each; broad filter: 473/0/41.
  WSL2 public: 67/0/0; precise set and rerun: 457/0/0 each.
- Windows and WSL2 public and precise qualification test-name/outcome
  multisets match; each platform's precise rerun matches its first run.
- Broad-filter delta from the precise Windows run: the five classes and outcomes
  listed above, 57 additional tests total.
- Source baseline, commit/tree chain, `src`/`tests`/`native` subtree identities,
  and the Q1 branch identity match their recorded values.
- All relative links in the evidence Markdown and this report resolve.
- The diff from the qualification commit contains only the five evidence files
  listed above, this report, and its verifier. The worktree is clean.

Commands recorded for verification (from repository root):

```powershell
python docs/reviews/gagamba-rc2a-02b-r1/verify_evidence.py
git diff --check dd16baec48663d40cd29504d98e57e30dd0855df HEAD
git diff --quiet dd16baec48663d40cd29504d98e57e30dd0855df HEAD -- src tests native docs/evidence/restricted-consumer-contract-rc2a-02b-portability/runs
git status --short --branch
```

The verifier’s successful output is summarized above and can be reproduced
with the command listed here. The full Windows and WSL2 suites were not rerun
because code and raw qualification artifacts remain unchanged.

## Scope and remaining obligations

The supported claim remains:

**RC2A-02B QUALIFIED LOCALLY: WINDOWS + WSL2, MANAGED-CODE VALUE/CODEC SCOPE.**

Native Linux and macOS remain **NOT RUN**. This evidence does not qualify native
sandbox or confinement behavior, package compatibility, clean-consumer
compatibility, binary reproducibility, or Hufu. Q2A-S / RC-D04 remains **OPEN**;
RC-2B R1-F02/F03 remain **OPEN**; the R1-F06 HUFU-R recheck, AE4-B01, Hufu
resumption, RC2A-03/04, native qualification, and packaging/publication remain
not authorized or not qualified as previously recorded. RC2A-02B owner
acceptance has not been recorded, full RC2A-02 is not accepted, and no later
milestone is authorized. The Q1 finding is not an acceptance decision.

## Recommended independent verification

1. Check out `remediation/gagamba-rc2a-02b-r1` and run the verifier command above.
2. Recompute the 14 listed Windows artifact hashes from the `HEAD` Git blobs and
   inspect the matching `results.json` entries.
3. Independently parse `runs/windows/windows-plan-filter.trx` and
   `runs/windows/windows-required-set.trx` to reproduce the five-class delta and
   473/0/41 total.
4. Recompute all 43 `SHA256SUMS` entries and all nine document bindings, then
   confirm the source/raw-evidence diff is empty and the Q1 ref remains at
   `5bc396c2e5619906ae202afe0a4244dfd6024d9e`.

No self-approval is recorded. The next decision belongs to an independent
verifier.
