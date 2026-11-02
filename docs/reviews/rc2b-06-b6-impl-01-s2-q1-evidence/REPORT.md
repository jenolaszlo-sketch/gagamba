# REPORT.md — GAGAMBA-RC2B-06-B6-IMPL-01-S2-Q1 independent qualification

**Role:** independent security and implementation reviewer (different agent from the S2 author).
**Subject:** installed-artifact trust foundation of B6-S2.
**Reviewed implementation:** `8b897e4ebb8e53385199083a38086a9713f0e07a` (tree `af1dc67…`).
**Preserved candidate/evidence:** `c9d867d40e91af2342e85917759af0a6b05538a4` (tree `8bee84d…`).
**Review workspace:** `C:/Users/Laszlos/source/repos/gagamba-b6-s2-q1-review` (outside canonical source).
**Canonical repository:** unmodified; no worktree, branch, reset, rebase, merge, push or privileged
operation was performed.

---

## Executive summary

The S2 implementation correctly separates four things the mission asked to distinguish:

1. **Artifact integrity** — every candidate byte is bound by the canonical manifest; the artifact-set
   digest is recomputed independently and matches `31d8ebc2…`.
2. **Installation authority** — only `VerifyActive()` over the fixed, root-owned `/opt/gagamba`
   closure can return a trusted state; it takes no caller input and the public surface returns no
   identity.
3. **Qualification acceptance** — the receipt selects exactly one root-owned record by validated
   identifier + recomputed hash; a user-owned, fully consistent metadata triple stays Unqualified.
4. **Runtime eligibility** — `Qualified` additionally requires host prerequisites; a fresh install is
   `Installed/Unqualified`. (End-to-end admission/execution is a later stage.)

A valid hash is demonstrably **not** accepted as a substitute for the other three requirements.

The producer's headline claim ("S2 IMPLEMENTED WITH RESTRICTIONS — READY FOR INDEPENDENT Q1") is
**confirmed at the bounded S2 scope**, with two MINOR observations and two explicit qualifications.

**Disposition: S2-Q1 SUPPORTED WITH QUALIFICATIONS.**

---

## Independent probe ledger (summary)

Level keys: **P** = producer result inspected; **R** = producer test independently reproduced;
**N** = new reviewer-authored probe executed; **I** = code/design inspection only; **B** = native
qualification blocked.

| Probe | Claim | Level | Result |
|---|---|---|---|
| P-BIND-01 | repo/commit/tree identity | I (git) | Pass |
| P-BIND-02 | preservation commit is docs/evidence only | I (git diff) | Pass |
| P-BIND-03 | 44 untracked paths preserved | I (git) | Pass |
| P-ART-01 | archive safety (no traversal/links) | N | Pass |
| P-ART-02 | 14/14 artifact hashes+lengths+roles | N | Pass |
| P-ART-03 | artifact-set digest recomputation | N | Pass |
| P-ART-04 | shared M1 bytes across components | N | Pass |
| P-ART-05 | deps closure / no System.IO.Hashing | N | Pass |
| P-ART-06 | archive modes vs manifest | N | Pass (F01 observation) |
| P-CANON-01..28 | codec/receipt/record red-team probes | N | 28/28 Pass |
| P-TRUST-01 | forged user metadata → Unqualified | N | Pass |
| P-TRUST-02 | check-staged refuses user root | N | Pass |
| P-TRUST-03 | check-association pure logic accepts / tamper refuses | N | Pass |
| P-FS-01 | native symlink/fifo/hardlink/sparse/extra/mode/mutation | R | 1/1 Pass (multi-mutation) |
| P-TX-01..11 | installer transaction cases | R | 11/11 Pass |
| P-RET-01 | removal always fails closed | R | Pass |
| P-S1-01..24 | S1 positive separation probes | R | 24/24 Pass |
| P-S1-25..32 | S1 negative gate probes (representative) | N | 8/8 Pass |
| P-S1-33..51 | publish/stale/ref-disabled S1 probes | P | inspected, not re-run |
| P-REG-01 | full M1 Debug suite | R | 1141/0/270 (= 1113 + 28) |
| P-REG-02 | portable subset reconciliation | R/I | 63 + 2 reconciled |
| P-PTRACE-01..02 | mutated/odd generation during hashing | P | inspected, not re-run |
| P-WSL-01 | `/` ownership/fstype/namespace | N | Pass (0777 real ext4) |
| P-WSL-02 | verifier refusal corresponds to `/` | N | Pass |

Independent probe count: **N (new) = 60+**; **R (reproduced) = native 1 + transaction 11 + portable
63 + full-suite equivalent 1113 + S1 positive 24**; **P (inspected) = S1 publish/stale/ref-disabled,
ptrace races, owner/recovery wiring**. Credentials: no producer result was accepted without either an
independent re-run or a source-level derivation.

---

## Mandatory conclusions

### A. Artifact identity

**Yes.** The candidate's actual fourteen-artifact closure agrees with the accepted canonical manifest
and the artifact-set digest. Independent recomputation of the manifest bytes yields
`31d8ebc21c227e8ed6cf9e6952b69365a71f6ec02786c0cf0fa8a0ab8906e133`; all 14 artifacts match
path/role/kind/length/mode/RID/build identity and SHA-256; no test-only artifact is present.
(CANONICAL-MANIFEST-REVIEW.md.)

### B. Trust anchor

**No.** Internally consistent but user-controlled metadata cannot produce a `Qualified` state.
`VerifyActive()` is the only authority path, takes no arguments, and uses the fixed root; the public
API returns `void` from caller-supplied roots; there is no test seam, conditional compilation or
environment influence in the production verifier. A fully consistent, digest-matching, user-owned
triple passed the pure association helper but `verify-active`/`check-staged` refused.
(TRUST-ANCHOR-REVIEW.md.)

### C. Qualification association

**Yes.** The receipt selects exactly one record by validated `qualificationId` (grammar checked
before any filesystem lookup), recomputes the record hash from actual bytes, and binds digest,
profile/version, RID, scope, source commit/tree and `buildConfiguration=Production`. There is no
enumeration, wildcard or latest-file-wins fallback; a bare `accepted` flag and a producer report
cannot self-authenticate. (QUALIFICATION-RECORD-REVIEW.md.)

### D. Filesystem protection

**Yes (nonprivileged).** `openat2(RESOLVE_BENEATH|RESOLVE_NO_SYMLINKS|RESOLVE_NO_MAGICLINKS)` with
`O_NOFOLLOW`/`O_PATH`, fd-based hashing, before/after `fstat` identity, `nlink==1`, no setuid/caps,
exact-set enumeration, sparse rejection, and re-acquisition of every path. The native test
independently refused symlink, symlink-ancestor, FIFO, hardlink, sparse, incorrect-mode, missing,
extra and mutated artifacts with a valid positive control. Residuals (root/kernel) are documented,
not hidden. (FILESYSTEM-VERIFICATION-REVIEW.md.)

### E. Installation atomicity

**No partial/concurrent generation can be selected as complete.** The installer refuses
missing/mutated/extra/symlink candidates, pins source fds, verifies the digest during copy, applies
the declared ownership/mode as root, `fsync`s files and directories, publishes the generation with
`RENAME_NOREPLACE`, and writes `active` last. 11/11 transaction tests reproduced.
(INSTALLATION-TRANSACTION-REVIEW.md.) Root-executed durability is a qualification.

### F. Generation retention

**HD-4 is upheld by unconditional refusal.** Active, HELD, live-owner/recovery, retained-stop-authority,
unknown and incomplete-cleanup states all refuse; the production removal path always refuses because
no holder proof exists. No automatic garbage collection. (GENERATION-RETENTION-REVIEW.md.)

### G. Production/test boundary

**Preserved.** S1 restrictions are intact; 24 positive separation probes and 8 representative negative
gate probes (`GAGAMBA001/002/003/004/006/008`) were independently reproduced with exact diagnostics
and no leaked production binary. No test-only assembly is promoted. (REGRESSION-REVIEW.md §1.)

### H. B1–B5 lifecycle

**No alteration.** The full M1 Debug suite reproduced 0 failures (1,141 passed including 28 reviewer
probes ⇒ 1,113 producer cases, 270 skipped). No `Bootstrap`/`Lifecycle`/`Native`/`Restricted`/`Runtime`
source changed; `RestrictedExecutionRuntime.Create()` still returns the unavailable provider; no new
release authority, deadline, capability or journal schema exists. (REGRESSION-REVIEW.md §2–3.)

### I. Host suitability

`/` is `root:root` mode `0777` on the real WSL2 ext4 root (`/dev/sdf`); `/opt` is normal `0755`.
The accepted ancestor predicate (root-owned, not group/other-writable, no sticky exception) correctly
refuses, and `verify-active` returned `Unqualified Directory is group/other writable.` Native
root-owned installation qualification remains **Pending** on this host; the smallest safe prerequisite
is a disposable host whose ancestors to the install root are root-owned and not group/other-writable.
(NATIVE-QUALIFICATION-LIMITS.md.)

### J. Qualification boundary

**Established now:** source binding, artifact identity, canonical codec strictness, descriptor-relative
filesystem verification (nonprivileged), trust-anchor separation, qualification-record association,
installer transaction logic (nonprivileged), retention fail-closed, S1 separation, B1–B5 preservation.
**Requires later B6 stages:** wire installation-identity echo, loaded-assembly checks, public
descriptor v2, pre-RELEASE enforcement, full native dependency closure, real generation removal.
**Requires a different/repaired disposable qualification host:** root-owned native installation and a
real `Qualified` state (HD-1).

---

## Final disposition

> **S2-Q1 SUPPORTED WITH QUALIFICATIONS**

The S2 installed-artifact trust implementation satisfies the accepted bounded source and deterministic
verification contracts. No blocking security defect was found. Remaining privileged/native and
later-stage qualifications are explicit.

**Recommendation:** Ready for S2 owner acceptance with qualifications.

The reviewer does **not** claim actual root-owned native qualification, end-to-end
installation-to-execution security, or production restricted-execution readiness.
