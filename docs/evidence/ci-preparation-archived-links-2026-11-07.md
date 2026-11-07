# CI preparation: archived review document links

Date: 2026-11-07 (Asia/Manila).
Baseline: `9ca765c6af9bcc1665bc80e4c28f1a77d3c8d81d`, canonical main.
Scope: preparation link inventory only; no runtime or qualification change.

## Failure and correction

[CI job 128352234385](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/42355386593/job/128352234385)
failed on `../evidence/rc2b-06-b6-native-env-prep-01.md` in the archived
`01-rc2b-06-b6-native-env-prep-01.md`. The exact Windows PowerShell CI command
reproduced that failure locally before the fix.

Three documents in the native-environment Q1 review package retain relative links
from their original `docs/plans` or `docs/evidence` directories. All three copies
are bound by the review package's SHA-256 manifest; the original source identities
are recorded in its `document-binding.json` at commit
`af4300f5dff7e4e49fca98233911a44a15894a43`.

Extend `eng/preparation-legacy-links.json` with the three exact source identities
and their 29 broken reference occurrences. Each mapping records the original
source path/commit, original link text, occurrence count and line, canonical
target, and target Git blob verified at the baseline above. Existing inventory
entries are unchanged. The map's original top-level `verifiedAtCommit` continues
to describe its pre-existing entries; the new entries carry their own baseline.

The verifier still rejects changed source bytes, missing targets, unexpected
occurrence counts and unmapped broken links. No directory exclusion or permissive
fallback was added. The review package already has `** -text -whitespace` in its
own `.gitattributes`; neither attributes, archived bytes, manifest hashes nor the
verifier needed modification.

## Local verification

- `powershell -NoProfile -ExecutionPolicy Bypass -File eng/verify-preparation.ps1`:
  before, exit 1 with the reported broken link; after, exit 0 / Passed.
  The first passing canonical check covered 379 Markdown documents, 1,669 local
  links, eight historical compatibility documents and 71 mapped occurrences.
  Those counts precede this evidence note and its handoff/queue links.
- `python -m unittest discover -s eng/tests -p test_preparation_checkout.py -v`:
  2/2 passed; real Git checkout conversion preserves all eight mapped source
  SHA-256 and Git blob identities with `core.autocrlf=true` and `false`.
- `git diff --check`: passed for the inventory change.

Local fix only; no commit or push performed, and GitHub CI has not run this fix.
Preparation success is not backend or native qualification. Existing E-1 owner
qualifications and pending work remain in effect.
