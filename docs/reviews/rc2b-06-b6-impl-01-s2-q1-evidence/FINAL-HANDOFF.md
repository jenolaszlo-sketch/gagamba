# FINAL-HANDOFF.md — S2-Q1 independent qualification

**Commission:** GAGAMBA-RC2B-06-B6-IMPL-01-S2-Q1
**Disposition:** **S2-Q1 SUPPORTED WITH QUALIFICATIONS**
**Reviewer:** independent (different agent from the S2 implementation author)
**Date:** 2026-11-02

## 1. Exact reviewed implementation commit/tree

`8b897e4ebb8e53385199083a38086a9713f0e07a` / tree `af1dc67cf6c74979eb24e9395724b86f264f9a3f`

## 2. Exact preserved-candidate (evidence) commit/tree

`c9d867d40e91af2342e85917759af0a6b05538a4` / tree `8bee84d0d8750d59cea21053ebeb0fea1fbf596b`
(first S2 commit `35e74fd…` / tree `640d8ce…`)

## 3. Independent source/evidence diff inventory

- `git diff --numstat 8b897e4 c9d867d` = only `docs/PROJECT-STATUS.md`,
  `docs/evidence/rc2b-06-b6-impl-01-s2.md`,
  `docs/evidence/rc2b-06-b6-impl-01-s2/{candidate-manifest.json,candidate-payload.tar.gz,verification.zip}`,
  `docs/handoff.md`, `docs/work-queue.md`. **Evidence-only.**
- Reviewed range `df3d6f0..c9d867d` = 3 commits, linear, no unreviewed commits.
- 44 pre-existing untracked paths preserved (28 top-level entries, identical set).
- `main` is 6 commits ahead of `origin/main` (`401e1ea`); no push.

## 4. Verified archive SHA-256

`candidate-payload.tar.gz` = `0f4533b77daf2ae3c9d3a4487bc59d6be5be422393049f1fed5a3c0e54f1486a`

## 5. Verified fourteen-artifact inventory

See CANONICAL-MANIFEST-REVIEW.md §3. All 14 production artifacts present; path/role/kind/length/mode
match; no test-only artifact; SHA-256 of each matched independently.

## 6. Independently recomputed artifact-set SHA-256

`31d8ebc21c227e8ed6cf9e6952b69365a71f6ec02786c0cf0fa8a0ab8906e133` = SHA-256 of the canonical
manifest bytes (the `candidate-manifest.json` file). Matches the expected digest.

## 7. Production M1 and dependency-closure assessment

M1 = `3bc7e5df…` byte-identical in owner/provider/recovery; coordinator = `5e55e92b…`; seccomp =
`2aa87901…` (672 B). Published deps: `linux-x64` target, exactly 3 managed runtime assets, no
`System.IO.Hashing`/`SourceLink`, no native/runtimeTargets group; runtimeconfig `10.0.14`,
`rollForward=Disable`. Full native loader/system closure: **outstanding**.

## 8. Canonical manifest probe results

Positive control + 28 reviewer red-team probes pass (uppercase/all-zero/short/long hash, nested
duplicate key, field reorder, bad mode, decimal length, string uid, extra field, required/survives/
consumer/profile/protocol/policy/features mutations, receipt+record mutations, active boundary, id
grammar). Producer's 63 portable cases re-run and pass.

## 9. Trust-anchor and forged-authority probe results

Fully self-consistent user-owned triple: association logic accepts (exit 0), `check-staged` refuses,
`verify-active` = `Unqualified Directory is group/other writable.`, no `/opt/gagamba` created. No test
seam/conditional/env influence. **No Qualified from user metadata.**

## 10. B6-T20c nine-case matrix

All nine cases Supported (fresh→Installed/Unqualified; missing/wrong-hash/other-digest/rid/profile/
producer-only/conflict→Unqualified; multiple-records→only selected evaluated; stale→no fallback).
Malformed id refused before lookup.

## 11. Linux fd/filesystem verification results

Native test re-run non-root on WSL2: 1/1 passed (positive control + symlink, symlink-ancestor, FIFO,
hardlink, sparse, incorrect mode, missing, extra, mutated refusals). Producer `ptrace` mutation/
directory-substitution races inspected (valid code paths; not re-run).

## 12. WSL2 root-permission investigation

`/` = `drwxrwxrwx root:root 0777` on the real ext4 root `/dev/sdf`; `/opt` = `0755 root:root`;
kernel `6.6.114.1-microsoft-standard-WSL2`; Ubuntu 26.04.1. Predicate correctly refuses; refusal
message matches the checked `/` inode.

## 13. Installation transaction results

11/11 transaction tests reproduced (bounded copy, changed source during copy, no-replace publication,
interruption before/after generation rename, concurrent reader across pointer swap, symlink/traversal/
extra refusal, duplicate key refusal, retention refusal). Real root execution not performed.

## 14. Generation retention/removal results

Production removal **always refuses** (no holder proof); active/HELD/live/unknown/incomplete all
refuse; no auto-GC.

## 15. S1 production separation findings

24/24 positive probes reproduced; 8/8 representative negative gate probes reproduced with exact
`GAGAMBA001/002/003/004/006/008` diagnostics and no leaked production binary; remaining ~19 producer
probes inspected only.

## 16. B1–B5 regression findings

Full M1 Debug: 1,141 passed / 0 failed / 270 skipped (= producer 1,113 + 28 reviewer probes). No
lifecycle/protocol/runtime source changed; `RestrictedExecutionRuntime.Create()` still unavailable.

## 17. Independent probe count and pass/fail

New reviewer probes executed: 60+ (28 codec/receipt/record, 3 trust-anchor, 8 S1 negative, 2 WSL host,
artifact/deps scripts). Reproduced producer suites: native 1/1, transaction 11/11, portable 63/63,
full M1 1,113/0/270, S1 positive 24/24. **Zero probe failures attributable to the implementation.**

## 18. Producer evidence inspected vs tests independently executed

- **Inspected only:** S1 publish/stale/ref-disabled probes; `ptrace` race probes; wire/descriptor/
  lifecycle code (absent by design).
- **Independently executed:** artifact hashing/safety, codec probes, trust-anchor probes, native fd
  test, transaction tests, portable subset, full M1 suite, S1 positive+representative negative probes,
  WSL host probe.

## 19. Blocking and nonblocking findings

Blocking: **none**. Nonblocking MINOR: F01 (archive ownership wording), F02 (`/etc` acceptance store
undocumented), F03 (vestigial holder argument). Qualifications: F04 (native root install), F05
(later-stage/native closure).

## 20. Native/privileged qualifications

Blocked/pending: privileged root-owned installation, root-executed durability, real `Qualified`
state, full native dependency closure, pinned launch, assembly-`Location` checks, identity echo,
descriptor v2, pre-RELEASE enforcement, real generation removal, native Linux-host qualification.

## 21. Evidence manifest identity

`SHA256SUMS.txt` in this directory lists every review artifact (excluding itself, documented rule).
Raw producer evidence preserved unchanged under `evidence/verification/`; reviewer logs under
`evidence/*.log`; isolated source copy under `src-8b897e4/`; reviewer probes under `probes/`.

## 22. Recommended owner disposition

**Accept S2 with qualifications.** Direction: S2-Q1 SUPPORTED WITH QUALIFICATIONS → ready for S2 owner
acceptance, with the native/privileged and later-stage qualifications recorded and no production
activation.
