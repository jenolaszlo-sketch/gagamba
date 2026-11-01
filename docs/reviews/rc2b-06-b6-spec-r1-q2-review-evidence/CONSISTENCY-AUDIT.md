# B6-SPEC-R1-Q2 — Cross-Document Consistency Audit

Reviewed set: B6 design (`77ee441`, unchanged `5b34667`), B6 qualification plan
(`77ee441`), R1 remediation evidence (`5b34667`), BIND-01 receipt (`5b34667`),
`PROJECT-STATUS.md`, `handoff.md`, `work-queue.md`, original B6 evidence
(`cc47f4d` `rc2b-06-b6-spec-01.md`), Q1 review (`08ebcb5`).

## 1. Source revision and provenance

| Claim | Documents | Verified |
|---|---|---|
| `cc47f4d` = original B6 specification | design, plan, both evidence files, pointers | consistent |
| `08ebcb5` = Q1 review / R1 parent baseline (**not** the remediation) | R1 evidence §1, BIND-01 §1, pointers | consistent |
| `77ee441` = R1 substantive remediation | R1 evidence §1, BIND-01 §1/§2, pointers (post-receipt) | consistent |
| `5b34667` = provenance receipt (child of remediation) | BIND-01 §1/§4 | consistent |
| No document embeds a commit's own final SHA | R1 pointers at `77ee441` defer the R1 SHA to the follow-up receipt; the receipt then embeds `77ee441` | consistent — self-reference correctly avoided |

No F1 violation found. `08ebcb5` is never mislabelled as the remediation.

## 2. Remediation status and implementation authorization

| Statement | Where | Consistent? |
|---|---|---|
| R1 is documentation-only; no implementation/test/native/install claimed | R1 evidence status, BIND-01 status, design status banner | yes |
| B6 implementation not authorized | pointers, design §, work-queue | yes |
| Next activity is focused independent Q2 review, not implementation | work-queue §6, handoff §8 | yes |
| B6/B7, Hufu, packaging, native Linux/macOS, production remain gated | pointers, design §11, plan §8 | yes |
| `RestrictedExecutionRuntime.Create()` remains `Unavailable` | design §2.3/§9/§12.2, plan §7 | yes |

Note: design §9 and §12.2 still name `B6-F10` (the original B6 evidence's finding
identifier) for the `System.IO.Hashing` deps issue, while the remediation evidence
calls the same issue `B6-Q1-F09`. The two identifiers refer to the same observation at
different review stages; this is a non-security cross-reference variance, not a
contradiction. (Recorded as a minor note under Q2-F06 cluster; not a finding on its
own.)

## 3. Approved vs proposed design; pending HD decisions

| Item | Status across docs | Consistent? |
|---|---|---|
| Root-owned immutable install + receipt + qualification record | proposed/recommended (design §4.4, §10.1); not implemented | yes |
| Descriptor v2 minimal surface | recommended (design §4.6; remediation §7 HD-2) | yes |
| Test-only separation via `GAMBA_PRODUCTION`-style compile-time exclusion | recommended (design §2.5 Alt A); IMPL-01 obligation | yes |
| Invariant globalization | recommended (design §2.7); alternative requires ICU version recorded | yes (but cross-reference defect, Q2-F04) |
| HD-1–HD-4 | open owner decisions | yes — none approved |

## 4. Known stale baseline-table observations (Q1 handoff §F2)

Two entries in `docs/evidence/rc2b-06-b6-spec-r1-remediation.md` §1 are residual.

### 4.1 `docs` tree `f958119d425a0cc4fd6613dae8383710b9bffab8`

- **Recorded row.** `| docs tree | f958119d425a0cc4fd6613dae8383710b9bffab8 (modified by this remediation) |`
  It sits in a table of baseline identities labelled "unchanged" for `src/tests/native/eng/.github`.
- **Independent finding.** `f958119…` is exactly the `docs` tree of the pre-B6 baseline
  `2cd8c1d`. The final committed `docs` trees are `5e05f37e…` (`77ee441`) and `17f0b304…`
  (`5b34667`). So the row reports a **pre-remediation baseline** value, not the final
  committed tree.
- **Context available.** The row is annotated "(modified by this remediation)", which
  signals it is the pre-modification value. The authoritative commit/tree identities
  (`77ee441`/`62736d35…`, `5b34667`/`87bb8f73…`) are exact and independently verified.
- **Classification: harmless historical/baseline observation with sufficient context.**
  It could be clearer that the value is the baseline, but it is not a source-binding
  blocker and does not change any reviewed identity. (Q2-F07.)

### 4.2 `origin/main` "equal; no ahead/behind"

- **Recorded row.** `| origin/main | 2cd8c1d716ed2e3e61156819730cd3d39e81844b (equal; no ahead/behind) |`
- **Independent finding.** At the authoring baseline `origin/main` did equal local
  `main` (`08ebcb5`). After the R1 remediation and receipt commits, local `main` is
  `5b34667` and is **ahead four, behind zero**. The row is a pre-remediation snapshot.
- **Context available.** The table is a "baseline source binding" table; the value is
  the remote tip at authoring time. The authoritative local/remote identities are
  unambiguous.
- **Classification: harmless historical observation with sufficient context.**
  Non-blocking. (Q2-F08.)

**Do not confuse either observation with the corrected R1 commit identity.** Both are
in the R1 *evidence* file, not the BIND-01 receipt; the receipt itself records the
correct `77ee441`/`62736d35…` identity. Producer evidence was not edited to match Q2
conclusions.

## 5. Qualification readiness and test/evidence requirements

| Statement | Where | Consistent? |
|---|---|---|
| T01–T25 + T25-A/T25-B + T20a/T20b/T20c + V01–V12 | plan §2/§3/§9; pointers | yes in the traceability table |
| A required native case that cannot run is `Pending` and blocks completion | plan §4/§7 | yes |
| A green deterministic suite alone is not completion | plan §7 | yes |
| WSL2 ≠ native Linux | plan §1.1/§7/§8; design §11 | yes |
| Exit criteria require T25-B zero skips, T25-A accounting | plan §7 | yes |

**Plan-internal defect:** the plan body duplicates the
`B6-T20 — Installation verification unavailable` section (lines 399 and 533). The §9
traceability table lists it once. This is a duplicated-section artifact (Q2-F02),
minor, non-blocking.

## 6. Platform support statements

- Initial scope is WSL2 x86-64 only; arm64 unsupported; native Linux-host/macOS out of
  scope (design §, plan §1.1/§8). Consistent across docs.
- No doc infers native-Linux or elevated-host guarantees from WSL2/unelevated runs.

## 7. Security-guarantee statements

- Root-owned receipt + qualification record; no self-asserted hashes; no new release
  authority; recovery never releases — stated consistently in design §3–§6, §11 and
  remediation §5.
- R1 adds the F02/F04/F07/F13 remedies without contradicting any preserved guarantee
  (C1–C4, Option A, B1–B5, one physical RELEASE, protected capture, conservative
  cleanup).

## 8. Contradiction ledger

| Candidate contradiction | Verdict |
|---|---|
| Globalization mode attributed to HD-3 (design §2.7) while HD-3 is platform scope | **Confirmed** — cross-reference defect; unsafe default not created (Q2-F04) |
| Residual "staged from verified fds" wording (design §6.3 prop 8; plan T13) vs corrected §5.2 step 6 | **Confirmed** — incomplete F03 wording correction (Q2-F01) |
| Duplicate T20 section in plan | **Confirmed** — formatting artifact (Q2-F02) |
| Design §2/§6 subsection numbering out of order / duplicated §6.5 | **Confirmed** — numbering artifact (Q2-F03) |
| T25-A claims exact test names "recorded in the remediation evidence artifact" but evidence lists only configurations + line range | **Confirmed** — evidence-claim mismatch (Q2-F05) |
| `B6-F10` vs `B6-Q1-F09` identifier variance for the same deps issue | Non-security cross-reference variance; not a contradiction |
| Any doc claiming HD approved or implementation authorized | **None found** |

No **selection-blocking** internal contradiction was found.
