# P4 and protected-native boundary for E-2

Date: 2026-11-09 (Asia/Manila)

## P4 classification

The E-2 entry assessment is documentation-only: **P4-NEUTRAL**.

A future E-2 implementation is only provisionally neutral if it adds:

- no production caller;
- no protected sink;
- no E-1 consumer;
- no lifecycle wiring;
- no first-protected-effect change.

Any of those additions requires **P4 RE-AUDIT REQUIRED** before the first protected effect.

## Preserved accepted P4 disposition

From `docs/decisions/rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md`:

- 21 COVERED;
- 5 AUTHORIZED CONTINUATION;
- 3 LEGACY / TEST-ONLY;
- zero SLICE E DEPENDENCY;
- zero GAP;
- zero INCONCLUSIVE.

The 5 AUTHORIZED CONTINUATION and 3 LEGACY / TEST-ONLY rows are not reinterpreted as unconditional COVERED.

## Preserved qualifications

- **F-R1 / O-R1:** S10/S11 domination relies on the audited caller graph and upstream F01 domination. Any new production caller of the generic callees requires a fresh sink/authority audit.
- **O-R2:** S28 remains test-only. `internal` visibility is not authority.
- **O-R3:** refusal-reason typing under the constructor-recheck race remains open.
- **Observation-to-effect residual:** a root change after the last relevant stable observation may escape detection, even before the verifier returns. No atomic root-mutation/effect lock or retroactive cancellation is promised.
- **NATIVE-PENDING:** applicable covered rows remain pending.
- **331 unqualified native skips:** not passes, not coverage, not protected native positives.
- **Slice N:** separate authorized milestone.

## E-2-specific re-audit triggers

A fresh P4 audit is required before the first protected effect if E-2 adds any of:

- a K/M/F/S reader in a production path;
- a caller of `PrepublicationIntegrityProjection.Verify` or `VerifyActiveIntegrity`;
- a validation context construction;
- L/C/R/O validation wiring;
- a new protected sink or changes the first protected effect;
- any change to the S10/S11 caller graph.

A refusal-only package that remains entirely unwired may be provisionally neutral, but the classification must be verified at its exact revision, not assumed from this document.

## Non-claims

- No native protected positive is claimed.
- No privilege drop, SCM/pidfd custody, GAVA1, v3-pre, native RELEASE, stop/reap, or cleanup qualification is claimed.
- No deterministic test or synthetic fixture substitutes for protected native qualification.
- No positive validation, production activation, or deployment readiness is claimed.
