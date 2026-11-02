# CLAIMS-ARGUMENTS-EVIDENCE.md — claim → probe → evidence → verdict

Every claim is stated, an independent probe is described, raw evidence is named, and the verdict is
one of **Supported / Qualified / Refuted**. "Qualified" means evidence holds only within a stated
limitation.

| # | Claim | Independent probe | Evidence | Verdict |
|---|---|---|---|---|
| C1 | The reviewed implementation is `8b897e4`, tree `af1dc67` | `git rev-parse` | SOURCE-BINDING.md | Supported |
| C2 | The preservation commit `c9d867d` changed only docs/evidence | `git diff --numstat 8b897e4 c9d867d` | SOURCE-BINDING.md §4 | Supported |
| C3 | The candidate contains exactly 14 production artifacts and no test artifact | `probes/inspect_tar.py`, `probes/verify_candidate.py` | CANONICAL-MANIFEST-REVIEW.md | Supported |
| C4 | Artifact hashes/lengths/paths/roles/modes match the manifest | `probes/verify_candidate.py` | 14/14 OK | Supported |
| C5 | Artifact-set digest = SHA-256 of canonical manifest bytes = `31d8ebc2…` | recomputation | CANONICAL-MANIFEST-REVIEW.md §1 | Supported |
| C6 | Archive digest ≠ artifact-set digest, both recorded | `Get-FileHash` | `0f4533b7…` vs `31d8ebc2…` | Supported |
| C7 | No unsafe archive entries | `probes/inspect_tar.py` | 0 unsafe | Supported |
| C8 | Owner/provider/recovery share byte-identical M1 | hashes | `3bc7e5df…` ×3 | Supported |
| C9 | Published deps exclude System.IO.Hashing/SourceLink; 3 managed assets | `probes/deps_analysis.py` | CANONICAL-MANIFEST-REVIEW.md §6 | Supported |
| C10 | Canonical codec is deterministic, strict, ordered, bounded | P01–P17, P26 red-team probes | 28/28 pass | Supported |
| C11 | Manifest has no self-digest and rejects one | producer "self-digest" case + inspection | OK | Supported |
| C12 | A user-controlled, fully consistent metadata triple cannot be Qualified | `probes/trust_anchor_probe.sh` | `evidence/trust-anchor-probe.log` | Supported |
| C13 | No test seam/conditional/`GetEnvironmentVariable` in production verifier | grep of `src/…/Installed`, `eng/InstallTool` | TRUST-ANCHOR-REVIEW.md §2 | Supported |
| C14 | Receipt selects exactly one record; no enumeration/latest-wins | T20c + P18–P25 | QUALIFICATION-RECORD-REVIEW.md | Supported |
| C15 | Identifier validated before filesystem lookup | `SelectedRecordName` + P27 | QUALIFICATION-RECORD-REVIEW.md §1–2 | Supported |
| C16 | Record hash checked from actual bytes; fields bound to manifest | `AcceptRecord` + tamper probe | QUALIFICATION-RECORD-REVIEW.md | Supported |
| C17 | Missing/malformed/inconsistent metadata fails closed | `VerifyActive` catch matrix | TRUST-ANCHOR-REVIEW.md | Supported |
| C18 | Qualified requires host prerequisites | `CheckHost` reached only pre-return | TRUST-ANCHOR-REVIEW.md §1 | Supported |
| C19 | Symlink/hardlink/FIFO/sparse/extra/missing/mode/mutation refused | native test re-run | FILESYSTEM-VERIFICATION-REVIEW.md §2 | Supported |
| C20 | Symlink ancestor substitution refused | native test re-run | same | Supported |
| C21 | Directory/pointer replacement during verification detected | code + producer ptrace evidence | FILESYSTEM-VERIFICATION-REVIEW.md §3/§4 | Qualified (ptrace not re-run) |
| C22 | Installer refuses partial/mutated/extra/symlink candidate | 11 transaction tests re-run | 11/11 | Supported |
| C23 | Interrupted active-pointer update retains both generations | `test_interruption_after_rename_retains_both_generations` | 11/11 | Supported |
| C24 | Concurrent verification sees a complete generation | `test_reader_sees_complete_generation_…` | 11/11 | Supported |
| C25 | Publication is crash-ordered (file+dir fsync before rename) | code inspection | INSTALLATION-TRANSACTION-REVIEW.md §3 | Qualified (root durability not executed) |
| C26 | Removal always fails closed | code + retention test | GENERATION-RETENTION-REVIEW.md | Supported |
| C27 | S1 production/test separation preserved | 24 positive + 8 negative probes re-run | REGRESSION-REVIEW.md §1 | Supported |
| C28 | No B1–B5 regression; 1113/0/270 reproduced | full M1 suite (1141 incl. 28 probes) | REGRESSION-REVIEW.md §2 | Supported |
| C29 | `RestrictedExecutionRuntime.Create()` still unavailable | `git diff` + source | REGRESSION-REVIEW.md §3 | Supported |
| C30 | WSL2 `/` = `root:root 0777`, real ext4 root | `stat`/`findmnt`/`uname` | NATIVE-QUALIFICATION-LIMITS.md | Supported |
| C31 | Verifier refusal corresponds to the checked `/` inode | `verify-active` run | "Directory is group/other writable." | Supported |
| C32 | Root-owned native `Qualified` state achieved | — | not attempted (blocked) | Refuted as achieved / Pending |

**No claim in the accepted S2 scope was refuted.** C21 and C25 are Qualified on environment
(producer-only or nonprivileged). C32 is explicitly *not* claimed: no native root-owned installation
was created.

## Notes on argument discipline

- The reviewer did not accept "valid manifest ⇒ trusted installation": C12/C13 demonstrate the
  separation.
- The reviewer did not accept "root-owned receipt ⇒ accepted qualification": C14–C16 show the record
  must be separately root-owned and hash-bound, and that no real record exists here.
- The reviewer did not accept "mutation detected ⇒ all races eliminated": §4 of
  FILESYSTEM-VERIFICATION-REVIEW.md states the residual.
- Passing producer tests were re-derived from evidence (TRX reconciliation), not assumed.
