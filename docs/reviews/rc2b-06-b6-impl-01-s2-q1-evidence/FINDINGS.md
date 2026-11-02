# FINDINGS.md — S2-Q1 independent findings

Severity classes: **CRITICAL / MAJOR / MODERATE / MINOR / QUALIFICATION**.
**No CRITICAL, MAJOR or MODERATE finding was identified.** S2 owner acceptance is **not blocked** by
any finding below.

---

## F01 — MINOR (observation): candidate archive ownership vs installed ownership

- **Location:** `docs/evidence/rc2b-06-b6-impl-01-s2/candidate-payload.tar.gz`;
  `eng/linux-m1/install_common.py::_copy_artifact`.
- **Contract clause:** design §4.3 (installer may set ownership/mode, must not alter content);
  §7.2 (installed artifacts `root:root`).
- **Reproduction:** `probes/tar_mode_check.py` — archive members are `uid:gid = 1000:1000`; the
  manifest declares `uid:gid = 0:0`. Modes match (`0444`, coordinator `0555`).
- **Evidence:** CANONICAL-MANIFEST-REVIEW.md §4; `evidence/tar-mode-check` output.
- **Impact:** none on security. The candidate is untrusted installer *input*; the root installer
  re-creates every file (`os.open(..., dir_fd=ddir)` as root) with the declared mode, so the installed
  generation is `root:root`, which the verifier requires.
- **Scope:** documentation/evidence clarity only.
- **Remediation:** state explicitly in the producer evidence that archive ownership is the
  unprivileged build user's and that installed ownership is applied and verified separately.
- **Blocks S2 acceptance?** No.

---

## F02 — MINOR: second acceptance store not described in the accepted design

- **Location:** `eng/linux-m1/install_common.py::_acceptance`, `DECISIONS =
  "/etc/gagamba/accepted-qualifications"`; evidence `rc2b-06-b6-impl-01-s2.md` §"Operator boundary".
- **Contract clause:** design §4.5/§7.3 describe a single accepted record at
  `/opt/gagamba/qualifications/<id>.json`; the implementation additionally requires a root-owned
  `/etc/gagamba/accepted-qualifications/<id>.json` operator decision to run `qualify`.
- **Reproduction:** code inspection; the verifier (`InstalledFileSystem.VerifyActive`) never reads
  `/etc/gagamba`, so verification authority is unchanged.
- **Evidence:** QUALIFICATION-RECORD-REVIEW.md §3; TRUST-ANCHOR-REVIEW.md §4.
- **Impact:** none on the verifier's trust model — it **strengthens** the operator path (a separate
  root-owned decision must pre-commit to the exact record hash). The design is silent on this extra
  gate.
- **Scope:** documentation/spec alignment.
- **Remediation:** document the two-part acceptance (`/etc` decision authorizes `qualify`; the
  `/opt/gagamba` record is the verifier's acceptance basis) in the design or evidence, or fold the
  decision into the record per §4.5.
- **Blocks S2 acceptance?** No.

---

## F03 — MINOR: removal accepts a holder argument but hard-codes `Unknown`

- **Location:** `eng/linux-m1/install_common.py::remove_generation` / `retention_refusal`.
- **Contract clause:** HD-4 (refuse uncertain ownership/holder state).
- **Reproduction:** `remove_generation` always calls `retention_refusal(digest, active, "Unknown")`;
  the `"None"` branch is reachable only through the pure helper.
- **Evidence:** GENERATION-RETENTION-REVIEW.md §2/§4.
- **Impact:** the tool is unconditionally fail-closed, so no unauthorized removal is possible. The
  `holders` parameter is currently vestigial.
- **Scope:** code clarity / future holder integration.
- **Remediation:** none required for S2; wire the real holder check in the later removal stage.
- **Blocks S2 acceptance?** No.

---

## F04 — QUALIFICATION: native root-owned installation not performed

- **Location:** `InstalledFileSystem.VerifyActive` (fixed `/opt/gagamba`), HD-1 privileged execution.
- **Contract clause:** design §7.4, HD-1; B6 qualification requires a root-owned installation.
- **Reproduction:** host `/` is `root:root 0777`; `verify-active` → `Unqualified Directory is
  group/other writable.`; `/opt/gagamba` absent.
- **Evidence:** NATIVE-QUALIFICATION-LIMITS.md; `evidence/trust-anchor-probe.log`.
- **Impact:** no real `Qualified` host state exists; privileged installer durability and the platform
  baseline were not executed under root.
- **Scope:** native qualification, not implementation source.
- **Remediation:** a disposable host where all ancestors to the install root are root-owned and not
  group/other-writable, with HD-1 execution authorized.
- **Blocks S2 owner acceptance?** No (explicitly a qualification).

---

## F05 — QUALIFICATION: later-stage / native-closure guarantees deferred

- **Location:** design §5, §6, §7.3; S2 commission §3/§7.
- **Items:** full native dependency closure (ICU/OpenSSL/zlib/loader); pinned launch and
  cleared-environment exec; loaded-assembly `Location` checks; installation-identity echo
  (`RecoveryConfigV2`, RecoveryMessage 9/10, OwnerProtocolMessageType 36); public provider descriptor
  v2; pre-RELEASE `active` re-read; real generation removal.
- **Reproduction:** these symbols/messages do not exist in the reviewed range
  (`git diff df3d6f0..c9d867d` touches no wire/protocol/lifecycle file).
- **Evidence:** NATIVE-QUALIFICATION-LIMITS.md §4; SOURCE-BINDING.md §6.
- **Impact:** end-to-end installation-to-execution trust is not established; S2 provides only the
  installed-artifact verifier foundation.
- **Scope:** later B6/S3 stages.
- **Remediation:** proceed to the later stages; do not claim end-to-end security before they exist.
- **Blocks S2 owner acceptance?** No.

---

## Observations requiring no action

- **O1:** `RequireDirectory` does not implement a sticky-bit exception (correct per design §5.2); a
  sticky but non-writable directory would pass, which is consistent with "not group/other-writable".
- **O2:** `VerifyActive` does not enumerate unexpected entries in `/opt/gagamba` itself; the root,
  `installations` and `qualifications` directories are root-owned `0755` and root is trusted, so this
  is outside the accepted model.
- **O3:** The S1 negative-probe PowerShell suite is encoding-sensitive under Windows PowerShell 5.1;
  the producer used PowerShell 7. The reviewer reproduced the probes with raw byte capture. No
  implementation issue.
- **O4:** `CheckPlatform` requires the kernel string `microsoft-standard-WSL2`; this is the accepted
  HD-3 experimental profile, not a native-Linux claim.

---

## Summary

| Severity | Count |
|---|---|
| CRITICAL | 0 |
| MAJOR | 0 |
| MODERATE | 0 |
| MINOR | 3 (F01, F02, F03) |
| QUALIFICATION | 2 (F04, F05) |

**No blocking security defect.** Recommended disposition: **S2-Q1 SUPPORTED WITH QUALIFICATIONS**,
ready for S2 owner acceptance with the qualifications above.
