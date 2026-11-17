# E-4 publication and hosted CI

The normal fast-forward push advanced `refs/heads/main` at `https://github.com/jenolaszlo-sketch/gagamba.git` from `6892ab0e235b2eaab943f218545cbe35ca95fd1b` to `1116c66d7858e6000f0001a0abe39319e4e694be` on 16 November 2026 (Asia/Manila). `git ls-remote` confirmed the destination ref after the push and GitHub's Git commit endpoint returned tree `86768fbd5da028fc271fda295a7da5c389aab8bc`. These are the publication boundary for this commission.

## Preflight and outgoing review

Canonical checkout `C:/Users/Laszlos/source/repos/Gagamba`, local `main`, sole worktree, empty stash list and clean tracked checkout and index. Starting HEAD and tree equal the published E-4 identities. Canonical contribution guidance and applicable decision paths were checked. All 44 historical untracked paths and exact bytes matched the [pinned inventory](../../evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json). The sorted JSON path/SHA-256 fingerprint was `9c8a0fa7f6260d56566362e59fdd3f9c884d5aa016490941279251050ea5af98`. The [E-4 owner integrity receipt](../rc2b-06-b6-alt-b-slice-e-04-owner-01-evidence/integrity.json) names the prior revisions and manifests. Verified all 775 prior and ten owner entries against their pinned commits, accepted HEAD and checkout with ordinary CRLF conversions accounted for. E-4 owner manifest SHA-256 `a913052189e00cb697592530bb1b87a2b222badb9d8f2f91425396ba25da238c`.

| Commit in ancestry order | Inspected delta |
|---|---|
| `dc43b4360dbe17c644118239d0fd691c639aa0cc` | E-4 entry assessment and annex, three status updates, 13 paths |
| `db44b7e50c6ce449fdb6b596ad52744cf10443c0` | E-4 authorization and annex, three status updates, 10 paths |
| `3c16b53b288b39de3fb36964eecc8d5cccbc62b6` | E-4 producer report and annex, two source and two test files, three status updates, 137 paths. Tree `2f30e0dc72d1565ac312c0c052ef820a03a3dea0`. |
| `34eae383d6f19b0a2f5f6ac957218c7f27eb6fa3` | Independent E-4 Q1 report and annex, 88 paths. Tree `6bbbda828488765132573230b443230168c251ed`. |
| `1116c66d7858e6000f0001a0abe39319e4e694be` | E-4 acceptance and annex, three status updates, 11 paths |

The range is linear and fast-forward from the observed E-3 origin. The per-commit inventory matched the intended report/annex, status and four-file code boundaries. Checked outgoing content for binary artifacts and credential patterns. The three matches were references to the E-4 authorization commit. Retained environment, source and review metadata are part of accepted evidence. The accepted E-4 owner record has a broken relative link to the GAVA1 reconciliation. The verified target lives in the [E-4 entry assessment annex](../rc2b-06-b6-alt-b-slice-e-04-entry-owner-01-evidence/gava1-contract.md). Historical evidence was preserved exactly.

## GitHub Actions comparison

Push workflow `ci`, `main`, attempt 1, completed `success` on both SHAs. Workflow blob `588b4ba60cde79430b61f07549bc2f83858153dc` is identical. Job results and IDs are from the GitHub Actions run API and test summaries and the probe verdict are from the run logs. [E-4 run 43879340208](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/43879340208) · [E-3 run 43802107558](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/43802107558)

| Job | E-4 job ID | E-3 job ID | Result in both |
|---|---:|---:|---|
| preparation | 133340721082 | 133084535359 | success |
| build (windows-latest) | 133340720923 | 133084535399 | success |
| build (ubuntu-latest) | 133340720928 | 133084535260 | success |
| pack (packaging metadata) | 133340721164 | 133084535059 | success |
| unit tests (windows-latest) | 133341622487 | 133084994209 | success |
| unit tests (ubuntu-latest) | 133341622299 | 133084994138 | success |
| fixture self-test (windows) | 133343360061 | 133085894905 | success |
| availability probe (windows) | 133344309956 | 133086430865 | success, `MODULE-ABSENT`, `qualified=False` |
| launch spike (windows) | 133344920694 | 133086863696 | intentional skip by probe condition |
| GM-2 macOS provider tests | 133340720563 | 133084535341 | success |
| macOS lifecycle probe (GM-1A) | 133340721020 | 133084535356 | success |

The nine xUnit summaries in each unit job match E-3. Windows had 245 passes and 16 Linux-provider native skips. Ubuntu had 237 passes and 24 skips (six Windows-provider, 16 Linux-provider, two conformance). Both reported zero failures. The unprivileged B2 process-lifeline step ran on Ubuntu and was skipped on Windows by its OS condition. It is separate from the xUnit counts. Windows fixture self-test reported 15/0/0 and macOS provider and conformance reported 51/0/0 and 6/0/0 respectively (pass/fail/skip). The Windows probe smoke step ran and the launch module was unavailable. Hosted native skips, the missing module and OS-conditional steps confer zero qualification for the skipped scope. The unchanged CI workflow builds the codecs and runs its enumerated filters. Exact E-3 and E-4 focused and independent managed qualification remains the evidence in their separate acceptance records.
