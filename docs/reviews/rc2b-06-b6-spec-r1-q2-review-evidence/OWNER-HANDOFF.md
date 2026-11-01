# B6-SPEC-R1-Q2 — Owner Handoff

**Disposition:** `B6-SPEC-R1-Q2 SUPPORTED WITH QUALIFICATIONS` (see `FINAL-HANDOFF.md`).

## 1. What the owner may safely do next

1. **Accept the B6-SPEC-R1 specification remediation** as a coherent, implementable,
   conservatively bounded installed-artifact trust specification, on the strength of
   this Q2 review, subject to the recorded qualifications.
2. **Record the HD-1–HD-4 owner decisions** (see §3) — these are prerequisites to any
   implementation commission, not approvals that this review grants.
3. **If further precision is desired**, commission a bounded, documentation-only
   correction of the minor findings Q2-F01, Q2-F02, Q2-F03 and Q2-F05 (a small "R2"
   could fold them in). None of these is required for the specification to be
   coherent or testable.
4. **Then, and only then**, commission a separate `GAGAMBA-RC2B-06-B6-IMPL-01` for
   implementation planning, if the owner chooses to proceed.

## 2. What remains unapproved (Q2 does not grant)

- **No implementation authorization.** A supported specification review is **not**
  authorization to execute the B6 implementation plan. Q2 does not implement B6.
- **No HD-1–HD-4 approval.** Q2 did not choose their alternatives.
- **No production-security claim.** B6 specifies how trust, verification, invalidation
  and refusal *should* work; it does not demonstrate them. Documentation validation is
  not native security enforcement.
- **No packaging, installer, native, host or publication work.**

## 3. Required owner decisions before IMPL-01 (from the documents, not inferred)

| ID | Decision | Blocks IMPL-01? |
|---|---|---|
| **HD-1** | Narrowly scoped `sudo` in the disposable WSL2 instance for the root-owned install | **Yes** for native `Qualified` cases (else `Pending`) |
| **HD-2** | Minimal public descriptor v2 surface (or defer to internal-only until B7) | No; can implement internal first |
| **HD-3** | B6 initial qualification remains WSL2 x86-64 only; native Linux deferred; arm64 unsupported | No; WSL2 is the declared scope |
| **HD-4** | Immutable generations retained until explicit removal; no auto-GC; removal refuses active/held/in-use | No; runtime policy |

Additionally (Q2-F04): the **globalization mode** (invariant vs non-invariant ICU) is a
decision currently mislabelled as "HD-3 scope." The owner should either record it as its
own decision or confirm the recommended default (invariant globalization). This does
not change the safety of the design (the native dependency is classified and verified
in both modes) but should be resolved explicitly.

## 4. Conditions that must hold for the qualified path

- Root-owned, non-group/other-writable, symlink-free install tree with root-owned
  ancestors to `/`, sticky exception removed.
- Qualification record selected by explicit receipt-bound `qualificationId` +
  `qualificationRecordSha256`; fresh install is `Installed/Unqualified`.
- Runtime non-root unprivileged with empty `CapEff`; legacy `/usr/bin/dotnet` +
  ambient-environment path refused for the qualified profile.
- Native dependency closure classified (ICU/OpenSSL/zlib/native loader) with refusal
  on unsupported/substituted versions.
- Production/test separation (no dormant fault hooks in the qualified closure).

## 5. Restrictions that remain in force

- No B6/B7 implementation, installer, native qualification, production provider,
  package publication, Hufu, or native Linux/macOS work.
- `RestrictedExecutionRuntime.Create()` remains production-unavailable.
- A future B6 implementation commission must include the independent review and owner
  acceptance specified in the plan's exit criteria; native cases that cannot run are
  `Pending`, never `Passed`.

## 6. Corrections the owner may wish to request (all optional, none blocking)

| Finding | Severity | Smallest correction |
|---|---|---|
| Q2-F04 | Significant qualification | Rename/label the globalization-mode decision; drop the "HD-3 scope" attribution |
| Q2-F01 | Minor | Replace residual "staged from verified fds" wording (design §6.3 prop 8; plan T13) |
| Q2-F02 | Minor | Delete the duplicate `B6-T20` section in the plan |
| Q2-F03 | Minor | Renumber design §2.5–§2.7 and §6.3–§6.6 in document order |
| Q2-F05 | Minor | Narrow the T25-A "exact test names recorded" sentence or add the names to evidence |
| Q2-F06 | Minor | Add a bounded `qualificationId` grammar; restate acceptance as root-owned file |
| Q2-F07 | Minor | Label the `docs`-tree row as a pre-remediation baseline value |
| Q2-F08 | Minor | Date-qualify the `origin/main` row |
| Q2-F09 | Minor | State the `InstallationIdentity` → echo payload projection |

## 7. Distinction preserved: specification acceptance vs implementation authorization

Accepting B6-SPEC-R1 means accepting that the **design and plan** correctly specify the
installed-artifact trust contract and could be implemented and qualified. It does **not**
mean the protection exists, is enforced, or has been demonstrated. The owner handoff
keeps that distinction explicit, as required.
