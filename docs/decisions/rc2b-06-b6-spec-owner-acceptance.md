# RC2B-06 B6 Specification Owner Acceptance

**Commission:** GAGAMBA-RC2B-06-B6-OWNER-01.

**Date recorded:** 2026-11-01 (Asia/Manila). Documentation and decision recording only.
Authorized owner disposition: **B6 specification accepted with qualifications.**

**Disposition:**

> **B6 SPECIFICATION ACCEPTED WITH QUALIFICATIONS**

This decision does **not** authorize B6 implementation, native installation, HD
implementation, package publication or production activation. **Specification acceptance
is not implementation acceptance or authorization.**

---

## 1. Basis of acceptance

The owner accepts the B6 installed-artifact trust and deployment-attestation
specification within its documented boundaries, on the basis of:

- the source-bound B6 specification and its R1 remediation; and
- the **preserved** independent requalification whose own disposition is
  **`B6-SPEC-R1-Q2 SUPPORTED WITH QUALIFICATIONS`**.

Preserved independent review evidence and provenance:
[../reviews/rc2b-06-b6-spec-r1-q2-independent-review.md](../reviews/rc2b-06-b6-spec-r1-q2-independent-review.md)
(index/receipt) and the byte-identical reviewer documents under
[../reviews/rc2b-06-b6-spec-r1-q2-review-evidence/](../reviews/rc2b-06-b6-spec-r1-q2-review-evidence/README.md).
Bounded corrections applied before acceptance are recorded in
[../evidence/rc2b-06-b6-spec-r1-q2-correction-receipt.md](../evidence/rc2b-06-b6-spec-r1-q2-correction-receipt.md).

Verified local source identities at acceptance (full SHAs, resolved locally):

| Role | Commit | Tree |
|---|---|---|
| Accepted B5 baseline / `origin/main` | `2cd8c1d716ed2e3e61156819730cd3d39e81844b` | `e9ed9989ee35c43557e0a93fdec4d313af4f740e` |
| Original B6 specification | `cc47f4dd2dc23895dc1d0cfacea81301ec75f08b` | `6e25ac365cc7a3d5f761dd0ff5306af2e5b60fe7` |
| Independent Q1 review / R1 parent | `08ebcb5479e5ffb78189c06f1bd5c02d49f2f7df` | `630b803d57e27a0044686595e108868072aa20ea` |
| R1 design remediation | `77ee4414df37b9f88979bde02742756538ba45af` | `62736d355e2650b5f40cdc3d904ed49466cbf025` |
| R1 provenance receipt / Q2 source baseline | `5b3466719a50df072848ecc5f832adbaeb06b3bf` | `87bb8f73520c37cbffe936c4f8c336398e9112d7` |

Ancestry is linear `2cd8c1d → cc47f4d → 08ebcb5 → 77ee441 → 5b34667`. No path under
`src/`, `tests/`, `native/`, `eng/` or `.github/` changed through the B6 specification
and remediation sequence.

---

## 2. Independent Q2 review — scope and non-claims

- **Verdict (reviewer's own):** `B6-SPEC-R1-Q2 SUPPORTED WITH QUALIFICATIONS`.
- **Scope:** read-only source examination, Git identity/ancestry/diff verification,
  byte-exact blob hashing, cross-document consistency analysis, and **12 paper/contract
  adversarial probes** (10 supported with unambiguous answers; 2 bounded
  underspecification in root-owned surface). Plus an independent rerun of the read-only
  preparation check (Passed).
- **Not performed:** no implementation, no installer, **no native installation or
  sandbox-execution qualification**, no host change, no package build, no fetch/reset/
  rebase/merge/push.

**The 12 contract probes and the entire Q2 review are design/inspection-based; they are
not native execution qualification.** No installed-qualified native B6 candidate has been
built or run.

### 2.1 Q2 qualification register (reviewer severities preserved)

| Finding | Subject | Owner disposition |
|---|---|---|
| Q2-F01 | Remaining incorrect FD-source wording | Bounded documentation correction — addressed |
| Q2-F02 | Duplicate T20 section | Bounded documentation correction — addressed |
| Q2-F03 | Design numbering artifacts | Bounded documentation correction — addressed |
| Q2-F04 | Globalization policy incorrectly grouped under HD-3 | Separate **HD-5** decision recorded |
| Q2-F05 | T25-A evidence claim exceeds recorded exact test identities | Evidence description corrected |
| Q2-F06 | Qualification ID grammar and trusted acceptance wording | Explicit validation/provenance defined |
| Q2-F07 | Stale docs-tree reference | Contextual attribution corrected |
| Q2-F08 | Stale origin/main reference | Snapshot attribution corrected |
| Q2-F09 | Identity-structure versus echo-field variance | Wire/identity projection reconciled |

No Blocking finding existed; none was created. The corrections are **owner-applied and
not independently requalified**. No correction changed substantive trust semantics,
message ordering or authority, so no focused independent delta review is triggered. This
owner determination is open to independent re-check.

### 2.2 Q1 closure (preserved)

All five originally-major Q1 findings (B6-Q1-F01, F02, F04, F07, F13) have source-backed
design resolutions; the remaining bounded Q1 findings are corrected or preserved. No
mandatory Q1 security finding remains unresolved. The full Q1 closure matrix is preserved
in `../reviews/rc2b-06-b6-spec-r1-q2-review-evidence/Q1-FINDING-CLOSURE.md`.

---

## 3. Accepted design

The owner accepts the following B6 design elements as specified:

- **Root-owned immutable local installation generations** with an atomic `active`
  pointer and no symlinks/hardlinks/setuid/caps.
- **Canonical exact-artifact-set identity:** per-file SHA-256 + length; artifact-set
  digest = SHA-256 over the canonical manifest bytes, recomputed from the installed
  bytes and required to equal the directory name, `active`, the receipt and the
  qualification record.
- **Root-controlled receipt and separate qualification association:** the receipt binds
  `qualificationId` + `qualificationRecordSha256`; selection is by validated identifier
  with no enumeration or latest-file-wins fallback.
- **Conservative verification and admission**, including exact-set enforcement and typed
  fail-closed refusal.
- **Verification-to-use protection** under the accepted threat model, with the
  documented root-only residual explicitly recorded rather than hidden.
- **Pinned executable/native helper resolution** with no PATH/shared-store fallback.
- **Explicit trusted runtime/dependency closure** (native `dlopen` inputs classified;
  ICU/OpenSSL/zlib/native-runtime loader).
- **Installation identity across controller, owner and recovery**, verified at wire
  precision, with the identity/echo projection now stated.
- **No recovery RELEASE capability** and **no disk-only authority reconstruction**.
- **Test-only versus production artifact separation** as a defined implementation
  obligation.
- **Explicit failure/refusal behavior.**
- **Bounded B6-T01–T25 qualification plan**, including T20a/T20b/T20c and T25-A/T25-B,
  with required cases that cannot run marked `Pending` (never `Passed`).

---

## 4. Accepted human decisions (HD-1–HD-5)

### HD-1 — Privileged local installation: **APPROVED WITH RESTRICTIONS**

B6 may use narrowly scoped privileged filesystem operations within a disposable WSL2
Linux qualification environment to create and verify the proposed root-owned local
installation. This is a **policy decision**; it does **not** authorize executing
privileged commands in this commission.

Restrictions:

- Privileged operations must be listed explicitly in the later implementation commission.
- Target only the intended Gagamba installation and qualification paths.
- No unrestricted root shell scripts without inspection; no blanket `sudo`.
- No unrelated global system changes; no opportunistic host hardening or system-package
  upgrades; no removal of unrelated directories.
- Preserve explicit installation ownership and mode evidence; require controlled cleanup
  and rollback instructions.
- Inspect intended operations before executing them in the later qualification task.

The qualified profile remains limited to the selected disposable environment. A
privileged installation passing inside WSL2 does **not** establish independent Linux-host
production qualification.

**Boundary:** HD-1 approves the use of narrowly scoped privileged installation as a
**future** qualification mechanism, not an immediate privilege escalation or installer
execution.

### HD-2 — Installed-provider descriptor: **APPROVED — MINIMAL PASSIVE V2**

Accept the minimal additive descriptor-v2 design; the public passive surface may expose
only the minimum enrollment/diagnostic facts (installation state, refusal reason,
artifact-set SHA-256, deployment profile, protocol/implementation version, RID,
qualification identifier). Detailed per-file verification, dependency-resolution
observations, platform diagnostics and internal trust evidence remain outside the public
contract for now.

The public descriptor must remain passive. It cannot create a preparation capability,
grant physical RELEASE authority, recover a release capability, convert an unsupported
provider into a usable restricted provider, or establish qualification solely from
decoded or caller-supplied data.

Actual public API changes require the separately commissioned B6 implementation and
compatibility qualification. If a smaller surface satisfies B6 without weakening the
accepted guarantees, that is preferred.

### HD-3 — Platform qualification scope: **APPROVED — WSL2 x86-64 EXPERIMENTAL**

B6's initial local installed-artifact qualification is limited to the existing controlled
WSL2 x86-64 Linux environment. The supported experimental profile must verify exact Linux
x86-64 native artifacts, the corresponding seccomp filter architecture, actual runtime and
native dependency closure, required kernel and cgroup features, the pinned Bubblewrap
environment, matching owner/recovery/controller protocols, and native installation
identity and authority.

**Arm64 remains unsupported for this profile.** Native Linux-host qualification remains
B5-L01/B7 follow-up work. No support is claimed for other distributions, native Linux
platforms, Windows restricted execution or macOS. No global production-readiness claim
follows.

### HD-4 — Immutable generation retention: **APPROVED**

Adopt immutable, versioned installed artifact generations. Each admitted operation is
bound to one accepted installation identity.

- No automatic garbage collection of installed generations; explicit removal is required.
- Active generations cannot be removed. HELD generations cannot be removed.
- Generations used by live owner/recovery processes cannot be removed.
- A generation with unresolved retained authority or uncertain ownership cannot be
  removed; unknown holder state results in refusal, not optimistic removal.
- Changing the active generation does not retroactively change an already admitted
  operation's artifact identity.
- Existing retained stopping authority remains governed by B1–B5 semantics; a change in
  the `active` pointer is not permission to abandon an older generation's recovery
  obligations.
- No controller-relaunch or cross-generation adoption mechanism is approved.

Scope: installation lifecycle policy only; no deployment manager, automatic updater or
migration system.

### HD-5 — Trusted-runtime globalization: **APPROVED AS A CONDITIONAL DESIGN POLICY**

Prefer invariant globalization for Gagamba's trusted managed controller, owner and
recovery components when compatibility can be demonstrated, to reduce conditional native
ICU dependency requirements within the qualified trusted-runtime closure.

Required compatibility proof (B6 implementation qualification): invariant globalization
must not change protocol encoding/decoding, canonical identity and digest calculations,
path handling or ordering, version/culture-sensitive parsing, security-relevant
comparisons, journal encoding, clock/deadline behavior, qualification metadata semantics,
or other accepted B1–B5 invariants. Test the actual production build configuration, not
only instrumented/test-variant assemblies.

Conditional outcome: if it passes the compatibility tests, use it for the qualified
trusted components; if it fails, **do not silently switch to an ICU-dependent deployment
and claim HD-5 is satisfied** — report the exact incompatibility and request a bounded
alternative platform-baseline decision.

Explicit limit: this policy does **not** impose invariant globalization on the untrusted
target; the sandbox must not silently change target application requirements. OpenSSL,
zlib and other native runtime dependencies remain subject to independent closure and
qualification; selecting invariant globalization does not remove those obligations.

---

## 5. Retained qualifications

This acceptance does **not** establish and explicitly retains:

- No B6 implementation exists yet.
- No installed-qualified native B6 candidate has passed qualification.
- No production/test build separation has been implemented.
- No B6 installed trust anchor has been executed.
- No native loader closure has been qualified.
- No B6 installed-identity handshake has been executed.
- No B6 source or package artifact has been published.
- B5 remains accepted with its original qualifications; all carried B1–B5 findings retain
  their dispositions (including B3-F04/F06, B5-L04, B4Q1-F02–F04, Q1R-08, R1-F02).
- The independent Q2 review is design/inspection-based, not native execution
  qualification.
- Q2 documentation corrections are not falsely attributed to the independent reviewer.
- Production restricted execution remains unavailable; `RestrictedExecutionRuntime.Create()`
  remains production-unavailable.
- WSL2 is not a native Linux host; arm64, macOS, Windows restricted execution and native
  Linux-host qualification remain out of scope.

---

## 6. Implementation readiness (conditions for the next commission)

Next commission: **`GAGAMBA-RC2B-06-B6-IMPL-01`** — separately authorized. Before
implementation begins, require:

1. Accepted source-bound B6 specification (this decision).
2. Preserved independent Q2 evidence (this commission).
3. Recorded HD-1–HD-5 decisions (this decision).
4. Corrected bounded documentation issues (Q2 correction receipt).
5. No unresolved security-relevant ambiguity introduced by the corrections.
6. Exact proposed production and test file scope.
7. Explicit production/test build-boundary design.
8. Exact installation-identity wire contract.
9. Strict qualification-record selection and trusted acceptance rules.
10. Native loader/dependency verification plan.
11. Precise privileged-operation and host-preservation plan (per HD-1 restrictions).
12. Tests-first execution and independent review requirements.

Suggested organization of the later implementation (not permission to implement now):
**installation identity → trusted closure → admission → owner/recovery binding → native
qualification.**

If bounded document corrections change a security-relevant contract or reveal a new
contradiction, require a focused delta review before implementation authorization. Purely
editorial or provenance corrections do not require another full B6 architecture review.

---

## 7. Preservation and non-authorizations

- The independent Q2 review evidence is preserved byte-for-byte; the reviewer's findings,
  rationale and verdict are unaltered.
- B1–B5 decisions, previous B6 reviews and historical evidence are preserved; the 74
  pre-existing worktrees are unchanged; unrelated untracked and ignored material is
  preserved.
- This commission modified only documentation (`docs/`): it did not change `src/`,
  `tests/`, `native/`, `eng/` or `.github/`.

This decision does **not** authorize: B6 implementation; modifications to `src/`,
`native/`, `tests/`, `eng/` or `.github/`; a privileged installation; Windows/WSL2
global host changes; NuGet/package build or publication; production restricted-provider
activation; B7; Hufu integration; microVM/HCS work; native Linux-host qualification;
reopening accepted B1–B5 decisions; removal of historical worktrees; rewriting review
evidence; or pushing without separate authorization.

---

## 8. Final disposition

> **B6 SPEC ACCEPTED WITH QUALIFICATIONS — READY FOR IMPLEMENTATION COMMISSION**

The review evidence is preserved, the bounded corrections are consistent, HD-1–HD-5 are
recorded, and the design is ready for a **separately authorized** B6 implementation. No
implementation, native qualification, publication or push is performed or authorized
here.
