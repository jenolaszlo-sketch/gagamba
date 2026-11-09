# P0 schema extraction map

Date: 2026-11-09 (Asia/Manila)

This map identifies the exact external source used for each normative section of the [proposed provenance record](../../evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md). It does not add or alter schema semantics.

| Proposed-record section | External source | Exact source location | In-repository use |
|---|---|---|---|
| §6 CJSON-1 | `final-acceptance-hash-contract.md` | “Canonical encoding CJSON-1” | Canonical encoding and hash rules for K/M/F/S/R/Wc/P |
| §7 K | `case-policy-and-action-contract.md` | §1, first paragraph and construction steps 1–7 | Exact K keys, bounds, no review/owner/receipt/self-hash, `Hk` |
| §8 M | `case-policy-and-action-contract.md` | §3, “M exact keys” paragraph | Exact M keys, B2/B3 variants, object bounds, `Hm` |
| §9 F | `case-policy-and-action-contract.md` | §3, “F exact top-level keys” through `resources` table | Exact F keys, B2 nulls, B3 host paths, fixed copy/invocation/sandbox/resources, `Hf` |
| §10 S | `case-policy-and-action-contract.md` | §4, “S exact keys”, templates, and hash definitions | Exact S keys, three templates, `Ai`, `Hs`, deadlines |
| §11.1 R | `case-policy-and-action-contract.md` | §1, construction step 3 | Exact R keys and `Hr` |
| §11.2 Wc | `case-policy-and-action-contract.md` | §1, construction step 4 | Exact Wc keys and `Hw` |
| §11.3 P | `case-policy-and-action-contract.md` | §1, construction step 5 | Exact P keys, `Hp`, publication order, independent administrative acceptance |
| §11.4 live challenge/response | `case-policy-and-action-contract.md` | §1, construction step 7 | `Hr`/`Hw`/`Hp` fields and one-response semantics |
| §11.5 graph | `case-policy-and-action-contract.md` | §1, “The dependency graph is …” | Acyclic `M -> F -> S -> K -> R -> Wc -> P` graph |
| §12.1 roots | `case-policy-and-action-contract.md` §2; `validation-bootstrap-compatibility.md` §“Exact validation contract” | Protected acquisition and exact metadata roots | Root table |
| §12.2 custody | `case-policy-and-action-contract.md` | §2, “Publish immutable …” and descriptor-relative checks | Metadata/custody rules |
| §12.3 acquisition/timing | `case-policy-and-action-contract.md` | §2, “Each L/C/R/O independently acquires K …” and timing | Per-actor independent acquisition |
| §12.4 fixture source | `case-policy-and-action-contract.md` | §2, “Fixture source is …” | Fixture source, O copy/verify rules |
| §13 permitted uses | Owner decision; entry assessment; proposed record | Scope and limits | Uses permitted only after separate decisions |
| §14 prohibited uses | Owner decision; F02 acceptance; trust-store decision | Binding limits and separations | Prohibitions and F02 non-extension |
| §15 supersession | `case-policy-and-action-contract.md` | §1, withdrawal/replacement and no mid-attempt reapproval | Content-addressed/no-silent-substitution rule |
| §16 gaps | E-2 entry owner decision; `final-design-binding.md`; qualification review | Q-E1/Q-E2 scope, external identity, ValidId grammar | Qualifications and deferrals |

## Cross-reference notes

- The **S2 ValidId grammar** is not redefined by the external contract. The exact grammar used in K/F/qualification fields is taken from the in-repository [QUALIFICATION-RECORD-REVIEW.md](../rc2b-06-b6-impl-01-s2-q1-evidence/QUALIFICATION-RECORD-REVIEW.md) §1: 1–128 `[A-Za-z0-9._-]`, no leading/trailing dot, reject `.`/`..`, separators, whitespace, non-ASCII; strict ASCII, case-sensitive.
- The **final-admission chain** (`/etc/gagamba/accepted-final-admissions`, `/opt/gagamba/final-admissions`, `/opt/gagamba/final-admission-selectors`) is referenced only to preserve the separation from the validation-catalog chain. It is not part of the E-2 subset and is not adopted or implemented by P0.
- The **F02 qualification store** (`/etc/gagamba/accepted-qualifications`) is referenced only to preserve the store-specific boundary. P0 does not extend it.
- The **GAVA1 header**, **v3 validation-pre layouts**, and **release correlation** are expressly outside the E-2 subset and remain for E-3/E-4 reconciliation.

## Verification

Each source file's exact identity is recorded in [digest-verification.txt](digest-verification.txt). The extraction does not rely on an unrecorded external file.
