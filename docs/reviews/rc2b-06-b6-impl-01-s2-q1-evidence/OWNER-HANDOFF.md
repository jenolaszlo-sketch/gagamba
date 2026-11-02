# OWNER-HANDOFF.md — decision material for the S2 owner

## Disposition

> **S2-Q1 SUPPORTED WITH QUALIFICATIONS** — no blocking defect; ready for S2 owner acceptance with
> qualifications.

## What is established (evidence-backed)

| Property | Evidence | Level |
|---|---|---|
| Exact source/preservation binding; evidence-only preservation commit | git diff/rev-parse | I |
| 14/14 artifacts match; artifact-set digest `31d8ebc2…` recomputed | independent hash/parse | N |
| Canonical codec strict/deterministic/ordered/bounded; no self-digest | 28 red-team probes | N |
| Trust anchor: user metadata cannot yield Qualified | forged-triple probe on WSL | N |
| Receipt selects exactly one root-owned record; no fallback/self-assertion | T20c + probes | N/R |
| Filesystem: symlink/ancestor/hardlink/FIFO/sparse/extra/mode/mutation refused | native test non-root | R |
| Installer: partial/mutated/extra/symlink refused; active published last | 11 transaction tests | R |
| Retention: removal fails closed (HD-4) | code + test | R/I |
| S1 production separation preserved | 24 positive + 8 negative probes | N/R |
| B1–B5 preserved; runtime factory unavailable | full M1 suite + diff | R/I |

## What is NOT established (explicit qualifications)

- No real root-owned installation and no real `Qualified` host state (WSL2 `/` is `0777`).
- No root-executed installer durability proof.
- No full native dependency closure (ICU/OpenSSL/zlib/loader).
- No wire identity echo, loaded-assembly checks, descriptor v2 or pre-RELEASE enforcement.
- No real generation removal (fails closed).
- No native Linux-host qualification (B5-L01), arm64, macOS or Windows restricted execution.

Do not read this review as authorizing production activation, privileged installation execution,
package publication or a push. None occurred.

## Recommended owner actions

1. Accept S2 with the qualifications above.
2. Record F01–F03 (MINOR, documentation/clarity) as bounded follow-ups — no code change required for
   acceptance.
3. Schedule the native/privileged qualification on a disposable host satisfying the root-owned
   ancestor predicate (a normal `0755` `/`), under a separately authorized HD-1 execution.
4. Proceed to the later B6 stages (wire identity echo consuming the installed identity, descriptor v2,
   pre-RELEASE enforcement, native closure) as separate commissions.

## Non-claims preserved

- The deterministic review is not native execution qualification.
- No self-asserted hash, receipt, report or caller input is accepted as authority.
- No claim of perfect TOCTOU elimination; the root/kernel residual is documented.
