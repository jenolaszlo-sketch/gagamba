# E-3 requirements and entry checks

Source shorthand resolves through [the manifest](source-and-baseline.md).
Rows R01–R10 trace the known design and the gaps that require the adopted
layout. The evidence strategy is a proposal under Slice E review §§8–10.

| ID | Requirement and governing source | Assessment and next evidence |
|---|---|---|
| R01 | E-2 predecessor closure and inherited bounds. E-2 owner §§5–10. | Bound at baseline. Preserve register citations and package dispositions. |
| R02 | Frozen v3 layouts in Git before schema-dependent work. Slice E owner §§7, 9 and entry review §§8, 10. | **Blocked.** Recover and verify accepted source bytes and commit a field-level owner-adopted record before codec authorization. |
| R03 | Isolated v3 validation-pre codec/parser. Entry review §§4.4, 8; plan §3.5. | Establish exact serial/binary or textual representation, layouts, version, statuses, grammar, length and output shape from R02. Use exact fixtures and parse/re-encode evidence where normative. |
| R04 | Bounded bootstrap, phase echoes, authority mode/hash and original identity bindings. Entry review §4.4; design §§3.3–3.4; plan §3.5. | 8,192-byte bootstrap bound and required concepts are recorded. Exact fields, values and encoding determine refusal vectors. Codec compares supplied values within its contract. Future caller establishes authenticated INIT and trusted phase/context. |
| R05 | Older parsers refuse v3 and unknown modes, without downgrade. Entry review §§4.4, 8; design §3.5. | `RecoveryConfiguration.Validate` presently admits versions 1/2. Preserve entry points and test the applicable legacy surfaces with adopted v3 bytes. Do not extend v1/v2 or route decode errors into fallback. |
| R06 | Release correlation reconciliation. E-2 closure §5/R27, P0 §12, D1 §18 and plan §3.5. | Determine encoder fields, representation, any structural comparison and downstream cross-format bindings at R02. Test verified mismatch/substitution cases. E-6 implements owner recomputation and policy at the atomic gate. |
| R07 | Separate prepublication data, runtime integrity, and authority. Design §§3.1–3.4 and plan §3.5. | Data-only success for supplied content, no synthetic qualified object, protected reader, live context or first production consumer. No in-session phase upgrade. Verify types/dependencies/reachability. |
| R08 | Production refusal-only, RELEASE and P4 evidence. Slice E owner §7 Q-E3/Q-E5 and entry review §§8–10. | Candidate diff, no new callers/sinks, unchanged RELEASE verifier and Debug negative control, provisional P4-neutral design confirmed by independent Q1. Preserve later P4 audits. |
| R09 | Independent qualification and separate owner acceptance. Slice E owner §9, entry review §9 and design §7. | Adopt the source before implementation. Producer tests and exact source record, reviewer-owned vectors and reachability/RELEASE evidence, then owner ruling on the exact qualified revision. |
| R10 | E-4 handoff and later qualifications. Slice E owner §9, plan §§3.5–3.6 and E-2 closure §§5–7. | Carry forward exact pre data interface, frozen bytes and remaining GAVA1 and correlation decisions. Protected/native work requires separate authority. |

## Entry matrix

The matrix assesses readiness for a separate owner authorization. A satisfied
planning check does not claim successful implementation.

| # | Criterion | Result | Basis |
|---|---|---|---|
| 1 | E-2 formally closed | SATISFIED | E-2 owner §9, exact baseline |
| 2 | Closure qualifications preserved | SATISFIED | E-2 owner §§5–7, trigger ledger |
| 3 | Normative planning scope identifiable | SATISFIED | R03, refusal-only codec |
| 4 | v3-pre contract sufficiently defined in repo | BLOCKED | R02/R04, no adopted exact layout |
| 5 | Composite Q-E1 allocation understood | SATISFIED WITH QUALIFICATIONS | R06/R10, milestone responsibilities located, codec-facing correlation assignment still requires source evidence |
| 6 | No missing prerequisite hidden by deferral | BLOCKED | R02/R06, E-2 deliberately left the E-3 contract open |
| 7 | Proposed implementation surface bounded | SATISFIED WITH QUALIFICATIONS | New internal files and managed tests proposed. Exact files need owner approval after R02. |
| 8 | Required interfaces understood | SATISFIED WITH QUALIFICATIONS | Authority and caller separation established. Byte-level interface awaits R02 and R06. |
| 9 | Authority boundary defined | SATISFIED | R07 and plan §3.5 |
| 10 | RELEASE implications understood | SATISFIED | R08, unchanged policy and exact-candidate checks |
| 11 | P4 implications understood | SATISFIED WITH QUALIFICATIONS | R08, design assessment only, exact-candidate evidence pending |
| 12 | Relevant inherited triggers identified | SATISFIED | Trigger matrix |
| 13 | Mandatory future qualifications preserved | SATISFIED | R10, Q-E5 and Slice N |
| 14 | Proposed implementation test strategy defined | SATISFIED | R03–R08, concrete field cases await normative input |
| 15 | Independent qualification strategy defined | SATISFIED | R08/R09, independent vectors and exact revision verification |
| 16 | Owner authorization requirements identified | SATISFIED | Separate record adoption, entry authorization and post-Q1 acceptance |

## Entry-blocker sweep

- **E3-ENTRY-F01** covers missing committed v3-pre byte definitions, contract
  bounds beyond the recorded high-level caps, and the codec-facing part of
  release-correlation reconciliation. The in-repo adoption requirement is
  explicit. The missing normative input affects entry and later independent
  qualification. A separately commissioned, source-bound documentation record
  and owner adoption is the smallest resolution.
- No competing accepted version/layout was demonstrated. The current legacy
  version gate is evidence of refusal on existing paths. Neither it nor an
  unrelated v3 format is evidence of the new layout.
- There is no demonstrated E-3 prerequisite defect in P1/P2, E-1, F02,
  RELEASE or P4. A pure codec design leaves authority, production consumers,
  protected readers, runtime correlation enforcement and native integration
  outside the package. Keep candidate-specific P4 and RELEASE checks at exit.
- Independent qualification has a defined gate after implementation. A
  missing run for an unimplemented codec is not a separate entry blocker.
- The two unassigned predecessor obligations remain open without a trigger
  that blocks the isolated codec. Later governance allocations remain gated.
