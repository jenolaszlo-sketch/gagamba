# E-2-P2 negative-control matrix

Date: 2026-11-09 (Asia/Manila)

Test sources: `tests/Gagamba.Execution.Linux.M1.Tests/Validation/DetachedEvidenceNegativeTests.cs`
(blob `600c40f2…`) and `DetachedEvidenceTests.cs` (blob `ce058ede…`). Every
control asserts the exact first-failing diagnostic; structural inputs are raw
byte mutations, never routed through a canonicalizing encoder.

## 1. CJSON-1 and structural controls (owner §18; baseline 9)

| Control | Representative input | First refusal | Test |
|---|---|---|---|
| Missing field | R without `conclusion` | `R field set` | `ReviewRefusesFieldSetViolations` |
| Extension field | R + `"extra":1` (ascending position) | `R field set` | `ReviewRefusesFieldSetViolations` |
| Missing/extension (Wc, P) | Wc without `decision`; P + `"approval":true` | `Wc field set` / `P field set` | `ReceiptRefusesFieldSetAndSchemaViolations`, `ContentRefusesFieldSetAndSchemaViolations` |
| Wrong literal | `conclusion`/`decision` altered | `R conclusion` / `Wc decision` | same |
| Unsupported version | `schemaVersion` 0, 2, `"1"` | `schemaVersion integer` | `ReviewRefusesSchemaAndIdentifierViolations`, others |
| Invalid identifier | `case/2` | `R caseId` / `Wc caseId` / `P caseId` | same |
| Wrong type | `caseId:null`; `hostId:null`; `accepted:"true"` | `caseId string`; `hostId hex`; `accepted boolean` | same |
| Invalid enum | `phase:"pending"`; `topology:"b4"` | `R/Wc/P phase` / `R/Wc/P topology` | same |
| Hash format | 63-char / upper-case hex | `<field> hex` | same |
| Empty string | `caseId:""` | `CJSON-1 string` (codec rule) | `ReviewRefusesByteLevelViolations` |
| Noncanonical whitespace | trailing space | `noncanonical CJSON-1 representation` | same |
| Multiple objects / truncation | `R + "{}"`; last byte cut | `invalid CJSON-1` | same |
| Invalid escape | `case\q2` | `invalid CJSON-1` | same |
| Noncanonical escape | `case\u002d2` | `noncanonical CJSON-1 representation` | same |
| Noncanonical integer | `schemaVersion:1.0` | `CJSON-1 unsigned canonical integer` | same |
| Non-ASCII byte | `0xE9` injected | `CJSON-1 ASCII printable bytes` | same |
| Duplicate key / wrong order | repeated `caseId`; swapped keys | `CJSON-1 keys are not strictly ascending` | same |
| Oversized / empty input | 65,536+ bytes; `[]` | `CJSON-1 size` | same |

## 2. Identity/binding and substitution controls (owner §18)

| Control | Expected first refusal |
|---|---|
| Incorrect `Hk` in R (`d×64`) | `R catalogSha256 does not bind K` |
| Substituted K (R2/Wc2/P2 with K3) | `R catalogSha256 does not bind K` |
| Incorrect `Hk` in Wc; substituted Wc on K3 | `Wc catalogSha256 does not bind K` |
| Incorrect `Hr` in Wc (`d×64`) | `Wc catalogReviewSha256 does not bind R` |
| Substituted R into Wc verification | `R catalogSha256 does not bind K` (vanguard R re-validation) |
| Incorrect `Hk`/`Hr`/`Hw` in P | `P catalogSha256 does not bind K` / `P catalogReviewSha256 does not bind R` / `P ownerCatalogAcceptanceSha256 does not bind Wc` |
| Substituted P into a valid b2 chain | `P catalogSha256 does not bind K`; external `Hp` differs (`Hp2 ≠ SHA256(P3)`) |
| Qualification pair disagreement (pre/post K) | `R/Wc/P qualificationId disagreement`, `…qualificationRecordSha256 disagreement` |
| Missing identity reference (dead value) | the corresponding `does not bind` diagnostic |

## 3. Hash-consistent semantic-invalidity controls (owner §18, §12)

Every affected hash is recomputed (edited R ⇒ propagate new `Hr` into Wc and P;
edited Wc ⇒ propagate new `Hw` into P; edited P ⇒ no downstream), so the first
refusal names the intended relationship, never a stale hash:

| Record | Edits (validly rehashed) | First refusal |
|---|---|---|
| R | `caseId`, `phase`, `topology`, `hostId`, `artifactSetSha256`, `fixtureManifestSha256`, `fixturePolicySha256`, `actionScriptSha256`, `qualificationId`, `qualificationRecordSha256` | `R <field> disagreement` |
| Wc | `caseId`, `hostId`, `phase`, `qualificationRecordSha256` | `Wc <field> disagreement` |
| P | `caseId`, `phase`, `artifactSetSha256`, `qualificationId` | `P <field> disagreement` |

Tests: `ChainRefusesHashConsistentReviewDisagreements`,
`ChainRefusesHashConsistentReceiptDisagreements`,
`ChainRefusesHashConsistentContentDisagreements`. Together these exercise every
duplicated K-value category at least once (`caseId`, `phase`, `topology`,
`hostId`, `artifactSetSha256`, `qualificationId`, `qualificationRecordSha256`,
`fixtureManifestSha256`, `fixturePolicySha256`, `actionScriptSha256`).

## 4. Authority-boundary controls (owner §18; mandatory distinction)

| Control | Result | Test |
|---|---|---|
| R/Wc/P with injected reviewer/owner/administrator identity, signature, or timestamp metadata | Structural rejection `R/Wc/P field set`; never interpreted as authority | `AuthorityMetadataInjectionIsRefusedAsAFieldSetViolation` |
| Valid but untrusted P-shaped record | Structural success required; no authority claim | `ValidUntrustedContentRecordIsAcceptedStructurallyWithoutAuthority` |
| Full hash-consistent chain of unknown provenance | Structural success; result surface projects no authority type and no `Accepted` flag | `UntrustedChainIsStructurallyValidButCarriesNoAuthorityProjection` |
| `accepted` treated as an authority signal | `false` ⇒ `P accepted is not true`; non-boolean ⇒ `accepted boolean`; literal not projected as a result property | `ContentRefusesFieldSetAndSchemaViolations`, untrusted-chain test |
| Structural success read as catalog approval / invocation permit / promotion | No promotion method; P2 members reference only Validation-namespace and BCL types; no production signature consumes P2 results; no direct production caller | `DetachedEvidenceReachabilityTests` (see `reachability-and-release.md`) |

## 5. Determinism and fail-closed behavior (owner §§18, 20)

- `RefusalMessagesAreDeterministicAndInputsAreNotMutated` — three identical
  refusals with an exact diagnostic; admin and catalog byte arrays unchanged.
- `RepeatedVerificationIsDeterministicAndDoesNotMutateInputs` — three identical
  successful verifications with stable digests; all four inputs byte-unchanged.
- No partial validation success: `TryVerify*` returns `false` with a null result
  contract and a non-empty diagnostic on every refusal path; no swallowed
  exceptions; no input mutation; no I/O, environment, or static mutable state.
- The two required outcomes remain distinct: structural rejection for accepted-
  invariant violations, and structural acceptance without authority claims for
  valid-but-untrusted content.
