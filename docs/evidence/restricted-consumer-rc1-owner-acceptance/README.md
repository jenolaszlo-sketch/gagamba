# RC1-A owner-acceptance evidence

- Date: 2026-10-25 (Asia/Manila).
- Slice: owner decision preservation and prospective progress/test-plan update.
- Baseline source/design: `d609c29d3048bc0fd91583068cb013fb906fdf90`.
- Final production source: unchanged from that baseline. Final decision commit
  and root tree are in the external completion receipt and user report.
- Branch: `decisions/gagamba-rc1-owner-acceptance`.
- Host OS/architecture: Windows authoring workstation; exact values recorded in
  `source-binding.json`. .NET SDK not queried; no build or execution required.
- Authoring runtime: Python, exact version in `source-binding.json`.
- Native prerequisites: not used; no native workload started, cleanup not applicable.
- Qualification scope: documentation, source identity and decision-provenance checks.
- Exclusions: implementation, production/test/project/package edits, package
  builds, native tests, Hufu edits, runtime qualification, push/merge/tag/publication.

## Decision evidence and scope

The [owner record](../../decisions/restricted-consumer-rc1-owner-acceptance.md)
records explicit approval, not a recommendation awaiting another owner response.
GLM review `feb9d7584c41712195d4b90754ed7a342ba74bb7` was inspected directly from
Git; its parent is design `d609c29d3048bc0fd91583068cb013fb906fdf90` and its only
change is the independent review document. This decision is a separate direct
child of the design, preserving the review commit/branch without copying it.

| Before RC1-A | Recorded change | Established | Still pending |
|---|---|---|---|
| Original plan has owner/review pending | Reference exact completed GLM review and explicit user approval | Accepted architectural direction with qualifications | Implementation and qualification |
| R1-F01 encode-side size rule needs explicit commission text | Mandatory 65,536-byte encode/decode rule in decision and RC2A-01 | Owner-selected size/refusal requirement | Named API failure behavior, implementation, tests |
| R1-F05 additions not named in test rows | Carry digest, Supports and oversized-Encode cases into T04/T05/T15 | Required future test coverage | Test code and execution |
| R1-F02/R1-F03 are later facade questions | Keep OPEN and assigned to RC-2B T18/T23 | Deferred ownership and gates preserved | RC-2B decisions, code and native evidence |

No baseline runtime failure, fixed runtime behavior, independent implementation
acceptance or native result is claimed. Prior design/evidence and GLM findings
are not rewritten. C1–C4 and C3 Policy A remain settled decisions; their separate
implementation/qualification status is not advanced by this record.

## Files and byte domains

- `owner-instruction.md`: readable copy of the supplied RC1-A assignment; UTF-8,
  LF, no BOM, trailing whitespace removed. It is not represented as byte-identical
  to the original attachment.
- `source-binding.json`: original attachment path/byte length/SHA-256, readable
  copy hash, exact design/review/consumer and branch/tree/blob identities, host,
  and hashes of inspected source documents. Git hashes use exact blob bytes.
- `validation.json`: actual static checks and results, changed-path inventory,
  explicit non-execution/qualification statements.
- `document-sha256.json`: final LF-normalized byte hashes of every changed
  document/evidence file except this hash manifest itself. The final commit/tree
  binds the manifest; no self-referential commit/hash claim.

Local scripts and the final completion receipt are outside the Gagamba checkout:
`C:\Users\Laszlos\source\repos\Solo\artifacts\gagamba-rc1-owner-acceptance-tools`.
The independent review's text is not included in this evidence directory.

## Executed inspection and validation

Read-only inspection used `Get-Content`, `rg --files`, `git show`,
`git rev-parse`, `git diff-tree`, `git status --short --branch`, and
`git worktree list --porcelain`. Large review output was followed by targeted
section reads. The applicable root `AGENTS.md`, current handoff/queue and
remediation evidence convention were read. No additional nested AGENTS.md was
found for these paths.

Worktree creation was:

```text
git worktree add -b decisions/gagamba-rc1-owner-acceptance C:/Users/Laszlos/source/repos/Solo/gagamba-rc1-owner-acceptance d609c29d3048bc0fd91583068cb013fb906fdf90
```

The static validator was:

```text
C:\Python314\python.exe C:\Users\Laszlos\source\repos\Solo\artifacts\gagamba-rc1-owner-acceptance-tools\record_and_validate.py
```

Checks cover all seven finding dispositions, passive boundary, Mode, both codec
size directions, R1-F05 tests, distinct stage gates, open AE4-B01, new/updated
document links, exact review/source/attachment hashes, unchanged design and
production/test/native trees, preservation of the separate review branch, and
the documentation-only allowlist. `git diff --check` and the final staged
`git diff --cached --check` are also run. Final committed blob hashes, direct
parent, clean worktree and unchanged review/Hufu worktrees are verified after
commit. Results are in `validation.json` and the external completion receipt.

These are document consistency checks, not an independent architecture review
or executed RC-2A tests. No `dotnet build`, `dotnet test`, `dotnet pack`, package
restore or native workload is part of this task. Stop after the clean decision
commit; RC2A-01/RC2A-02 require the next separate assignment.
