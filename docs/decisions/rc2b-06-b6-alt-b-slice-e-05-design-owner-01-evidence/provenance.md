# Evidence provenance, preflight and limits

Commission date: 17 November 2026 (Asia/Manila). This is a documentation-only architectural decision, not an implemented remediation or an independent qualification of future source. No production/test implementation, protected acquisition, privileged launch, native qualification or push was performed.

## Verified repository facts

| Item | Observation |
|---|---|
| Canonical path / branch | `C:/Users/Laszlos/source/repos/Gagamba`, `main` |
| Baseline commit | `b309a9ec1a9a0f160ea0c2dd513001c3561a36d9` |
| Baseline tree | `ac1e2aeae8aea96f19972e3fccb06fa94dcf39c9` |
| Origin identity | `https://github.com/jenolaszlo-sketch/gagamba.git` |
| Observed published main | `1116c66d7858e6000f0001a0abe39319e4e694be` |
| Published commit's tree | `86768fbd5da028fc271fda295a7da5c389aab8bc` |
| Ancestry | Published E-4 is the direct parent of local E-5 assessment; `merge-base --is-ancestor` succeeded |
| Tracked checkout / index | Clean at preflight; historical untracked files preserved, not staged |
| Worktrees / stashes | Exactly one canonical worktree; no stash |
| Historical manifests | All 785 historical entries plus seven E-5 assessment entries verified against pinned commits and current HEAD; zero changed accepted member blobs |
| Historical untracked | All 44 exact paths, lengths and raw SHA-256 values match the native-input-freeze inventory; no extra baseline untracked file |
| External F02 copies | Same accepted 15-member manifest verified against both delivered root and `recheck-input` files; manifest SHA-256 `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82` |
| Alternative B amendment | Local `validation-bootstrap-compatibility.md` raw SHA-256 `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb` matched |

[Preflight receipt](preflight.json) records every manifest path, pinned revision, count and digest and all 44 historical untracked hashes. [Source identities](source-identities.json) binds 82 repository input paths and both external 15-member copies. The source inventory includes governing records used through the accepted E-4 receipt as well as directly inspected implementation/build inputs; it does not assert each historical report was independently requalified.

Repository instructions were read from the canonical AGENTS.md and required handoff/work queue, baseline/security/fixture/implementation/testing/evidence guidance. No nested AGENTS.md was found in the repository. The accepted status files are manifest members, so this new decision supplies additive current handoff/queue status while preserving their accepted bytes. The first sandbox Git checkout checks returned permission errors outside the Solo workspace; approved repository access reran them successfully. No Git configuration or worktree was changed to bypass the restriction. Other exploratory reads of guessed nonexistent filenames were corrected using repository inventory; they yielded no authority or evidence claim.

Published CI facts are historical accepted evidence, not new CI execution: [E-5 publication receipt](../rc2b-06-b6-alt-b-slice-e-05-entry-owner-01-evidence/publication-and-ci.md) binds [run 43879340208](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/43879340208), ten applicable successful jobs and the intentionally skipped Windows launch job. This commission independently checks the remote ref; it does not claim to rerun CI or qualify skipped native scope.

## Authoritative trace for the decisions

| Decision | Source of invariant | New ruling versus existing fact |
|---|---|---|
| D1 | F02 `mechanism-b-session-capability.md` fixed ingress; `minimum-implementation-contract.md` component and native-drop clauses; `actor-and-descriptor-binding.md` original graph; E-5 E5-01 | Existing fixed installed path and role separation are retained. Dedicated native project/artifact choice, initial managed comparison boundary and future C/D carrier reservations are new design decisions. No executable has been measured or approved. |
| D2 | [E-2 D1](../rc2b-06-b6-alt-b-slice-e-02-d1.md) §§5–13, [closure](../rc2b-06-b6-alt-b-slice-e-02-closure-owner-01.md) §§5–7, [plan](../../plans/rc2b-06-b6-integrated-start-authority-impl-01.md) §3.5 and [E-1 sequence](../rc2b-06-b6-alt-b-slice-e-01-entry-owner-decision-01.md) §9 | Existing V/W/A authority, no self-approval, atomic sequence and E-6 accountability retained. Detailed evidence-production/acquisition/consumption and future adapter handoffs are allocated here; no operational approval act performed. |
| D3 | Accepted E-3 R2 data/codec, E-4 `Gava1Originals`/expectations and test-only handoffs, F02 B1/B2/custody/refusal rules, E-5 E5-03 | Existing structural/provenance separation retained. New exact local model classes, methods, fields, pre-only scope, transitions, diagnostic refusal enum and file allowlist frozen here. It is not a replacement native protocol or authority type. |
| Gates | [E-4 acceptance](../rc2b-06-b6-alt-b-slice-e-04-owner-01.md), [E-5 verification matrix](../rc2b-06-b6-alt-b-slice-e-05-entry-owner-01-evidence/verification-and-p4.md), [P4 acceptance](../rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md) | Inherited regression counts, refusal-only limits, exact-revision audits and independent expectations preserved; E5-R01–R24 add model-specific controls. All new test outcomes remain unexecuted. |

External bytes are located under `C:/Users/Laszlos/source/repos/Solo/gagamba-f02-authority-mechanism-decision-01/`, both delivered root and `recheck-input/`. Exact names/hashes are in source-identities.json. The later F02 owner acceptance governs retained historical PENDING captions. No external history or durable independent custody has been proved by verifying local bytes.

## Reproducible integrity method and final record

For each manifest named in preflight.json, read its bytes with `git show <pinned-revision>:<manifest-path>`, verify its recorded raw SHA-256, then resolve each member relative to that manifest directory and hash the exact `git show <pinned-revision>:<member-path>` bytes. Compare each accepted member with current HEAD and checkout; checkout-only CRLF/LF conversion is permitted only where normal Git normalization explains it. Every pinned member matched current HEAD in this run. For historical untracked files, use `git ls-files --others --exclude-standard -z` and compare the exact path set, lengths and raw file hashes with `docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json`; do not normalize those bytes.

The new SHA256SUMS.txt uses UTF-8/LF committed bytes and excludes itself. It includes the primary decision as `../rc2b-06-b6-alt-b-slice-e-05-design-owner-01.md` and every other annex member. Validate all entries again from committed blobs after the one documentation-only commit. Re-run the historical checks, `git diff HEAD^ HEAD --name-status`, tracked/index checks, `git worktree list --porcelain`, `git stash list` and read-only `git ls-remote origin refs/heads/main`. Record `git rev-parse HEAD` and `HEAD^{tree}` outside the commit, since a commit cannot contain its own final hash without circularity. The final handoff supplies that exact external receipt and identities.

Do not rerun managed implementation or native qualification in this design commission. Preparation validation, documentation link/inventory checks and evidence hashing are appropriate document checks. Their actual results are recorded in [document verification](document-verification.md); implementation tests and compiled isolation remain future requirements.
