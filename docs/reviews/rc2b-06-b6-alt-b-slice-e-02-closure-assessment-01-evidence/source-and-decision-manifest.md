# Source and decision identity manifest

All blobs are resolved at assessment HEAD
`a30d1807686ee15d39ef7e9103e8b992062bd971`. Prefixes are 12 hex characters.

## Normative and governing sources

| Source | Role | Blob |
|---|---|---|
| `docs/decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md` | Alternative B design acceptance; trusted-root bounds; atomic dependencies | `844cb149ffee` |
| `docs/plans/rc2b-06-b6-integrated-start-authority-impl-01.md` | Plan §3.5 atomic set (protected acquisition, exact case policy, fixture) | `485ad1ac76e8` |
| `docs/reviews/rc2b-06-b6-alt-b-slice-e-entry-01.md` | Slice E entry review; §8 package sequence; §13 atomic boundary | `0c311fc84bde` |
| `docs/decisions/rc2b-06-b6-alt-b-slice-e-01-entry-owner-decision-01.md` | Slice E planning sequence; Q-E1/Q-E2/Q-E3; E-6 ownership | `7002dcb3f04e` |
| `docs/decisions/rc2b-06-b6-alt-b-slice-e-01-owner-acceptance-01.md` | E-1 acceptance with qualifications | `6851869180a0` |
| `docs/decisions/rc2b-06-b6-alt-b-slice-e-02-entry-owner-01.md` | E-2 entry reconciliation authorization | `4c256e9dd0e7` |
| `docs/reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01.md` | E-2 entry assessment; §13 package boundary | `f3935c979431` |
| `docs/reviews/rc2b-06-b6-alt-b-slice-e-02-entry-reassessment-01.md` | E-2 entry reassessment; package sequence and atomic allocation | `09c70a5eb570` |
| `docs/decisions/rc2b-06-b6-alt-b-slice-e-02-entry-owner-02.md` | E-2 entry decision — P1 only | `f5ab96c6fedc` |

## Package identities and decisions

| Record | Identity | Acceptance blob |
|---|---|---|
| P0 report | `f08e485f011ca06104a8ac9e2a57cd76901f8dfb` | — |
| P0 frozen-schema record | `db25949d25babd474d00ae5b2f436e15507f22b3`; SHA-256 `11bca1f6add93abd759c5ec4afeca506e4e894e2ab79cc6ccf25da59e3c167d6` | — |
| P0 owner acceptance | commit `5b2ef14` | `8dd20d11202b` |
| D1 owner decision/adoption | commit `bdc7fa2` | `83fe8d264967` |
| P1 report | `8dfe6b8cb9917dbc7018ee2cea4e5584879f8dcd` | — |
| P1 implementation | commit `06006a7` | — |
| P1-Q1 report | `7ee0b781dbfd74aef873e5649c83d5db28795016` | — |
| P1 owner acceptance | commit `28275ba` | `f360932adc49` |
| P2 entry assessment | `745dbc8ca33a` | — |
| P2 entry owner | commit `dfa570f` | `893b48a2a7c8` |
| P2 report | `48f33ca30e8a9859421850beadf08279d45e82a3` | — |
| P2 implementation | commit `9b80969`, tree `fc77bc1d673f787adae2d332141f1dd5f854e883` | — |
| P2-Q1 report | `e785c42ab8e1` | — |
| P2 owner acceptance | commit `24c4cf4`, wording correction `a30d180` | `fb00cc37f1b8` |
| P4 coverage acceptance | commit `dfe8201` | `31ae1b08cb46` |
| P4-03 acceptance | — | `2c398b384f0b` |

## Accepted implementation blobs

P1 production:

- `src/Gagamba.Execution.Linux.M1/Validation/CanonicalJson.cs` — `276581c492e155d7b24090db8967eb4179730df0`
- `src/Gagamba.Execution.Linux.M1/Validation/FrozenSchemaRules.cs` — `868ddbeeaafdcb070a53c2c6f6c083a1fd4e50f6`
- `src/Gagamba.Execution.Linux.M1/Validation/FrozenContracts.cs` — `3344069cd3f7b766cb4a43e6e91b18caa7d03b4f`
- `src/Gagamba.Execution.Linux.M1/Validation/FrozenContractVerifier.cs` — `fdff96b93176058f025bb814603c0678d78b6fe6`

P2 production:

- `src/Gagamba.Execution.Linux.M1/Validation/DetachedEvidenceVerifier.cs` — `2b482a7068b3eae4603a93d79454e9c69ce7b6d3`
- `src/Gagamba.Execution.Linux.M1/Validation/DetachedEvidenceContracts.cs` — `652486d4ab26fec6a975e876e59a473b66bc331c`

P2 evidence packages (index only, not copied):

- P0 evidence README `facc89fc4d63`
- P1 evidence README `908c553955fb`
- P2 evidence README `f225ac91d1be`
- P1-Q1 evidence README `ea7fbd67b7e0`
- P2-Q1 evidence README `ec3777867ee6`

## Decision lineage (commit subjects)

| Commit | Subject |
|---|---|
| `77101ac` | `docs: reconcile Slice E-2 frozen-schema provenance (P0)` |
| `5b2ef14` | `docs: accept Slice E-2 P0 frozen-schema provenance with qualifications` |
| `bdc7fa2` | `docs: adopt Slice E-2 validation-catalog approval chain with qualifications` |
| `2dab45a` | `docs: authorize Slice E-2 entry with E-2-P1 only` |
| `ad52aae` | `docs: reassess Slice E-2 entry readiness with qualifications` |
| `06006a7` | `feat: implement E-2-P1 frozen-schema contract core` |
| `ab5f7a7` | `docs: independently qualify Slice E-2 P1 with qualifications` |
| `28275ba` | `docs: accept Slice E-2 P1 frozen-schema contract core with qualifications` |
| `051c682` | `docs: assess Slice E-2 P2 entry readiness with qualifications` |
| `dfa570f` | `docs: authorize Slice E-2 P2 entry with P2 only` |
| `9b80969` | `feat: implement E-2-P2 detached R/Wc/P verification` |
| `285c016` | `docs: independently qualify Slice E-2 P2 with qualifications` |
| `661800d` | `docs: correct P2-Q1 report internal cross-reference` |
| `a79dc60` | `docs: align P2-Q1 evidence README disposition reference` |
| `24c4cf4` | `docs: accept Slice E-2 P2 detached evidence verification with qualifications` |
| `a30d180` | `docs: correct P2 acceptance current-state remote-reference wording` |
| `8bd7142` | `docs: accept Slice E-1 with qualifications` |
| `dfe8201` | `docs: reconcile final refusal-only P4 coverage` |
