# P0 acceptance mapping

Date: 2026-11-09 (Asia/Manila)

This file maps each frozen artifact or record to the committed acceptance that gives it provenance for the P0 reconciliation. It does not create acceptance, approval, or authority.

## 1. F02 frozen submission

| Item | Exact identity | Committed binding |
|---|---|---|
| 15-file frozen submission | `recheck-input-SHA256SUMS.txt` = `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82` | F02 Mechanism B owner acceptance, §2, at commit `4aab87761078a92d05cd08aa1998cbe2c4f6aaf1` |
| Focused independent recheck | `independent-recheck.md` = `efa70562503e75e9e77aa2cdbce54a47d76c2d74ebb8374b1dd7b27d7dca18e6` | Same decision, §2 |
| F02 owner acceptance document | `docs/decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md` = `5f72da6f7ef7ede6379f587521226038137e7de602e53b2b94669f366cd75b3e` | Committed at `4aab87761078a92d05cd08aa1998cbe2c4f6aaf1` |
| `case-policy-and-action-contract.md` | `667e6118e02b081475ce1a2c851d3bfe9b0a96fdb474a2fb0598f308e9c694f7` | F02 15-file manifest member |
| `final-acceptance-hash-contract.md` | `49d566e1bcae585d4d9566319cdbe5d6f06696dfbbd9bff6c3fda9ce7514b652` | F02 15-file manifest member; F02 acceptance §2 |
| `mechanism-b-session-capability.md` | `f5d28dbe2473dd8a47337e4ae80a40c815bce051818e326ace1b647bf0d0b010` | F02 15-file manifest member |
| `actor-and-descriptor-binding.md` | `ea8c50d92a7bf7fe4bd2dca0f91155fc4533bef314fc4980036535c891822b93` | F02 15-file manifest member |
| `trusted-actors-and-boundaries.md` | `dfc2188b811de50d96f8e1db117c10c15c9efc1603f486607bb2fd38538b201c` | F02 15-file manifest member |
| Other ten F02 files | See `digest-verification.txt` | F02 15-file manifest members |

The F02 acceptance is design-level. It does not activate production, create an approval record, publish a final record, provision a protected store, implement full session capability, implement the installed-profile driver, or authorize native qualification.

## 2. Alternative B validation context

| Item | Exact identity | Committed binding |
|---|---|---|
| `validation-bootstrap-compatibility.md` | `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb` | Alternative B owner decision §4, at commit `d804d9062bab5d9d540df5c2d64f1b2c31bdd667` |
| `validation-bootstrap-assessment.md` | `3c4307f6e51610c1c2cd067b2eaeaeaf87eadb99d471dc65283468907aebeced` | Same decision §4 |
| `implementation-and-test-handoff.md` | `9a1a195b69645423e1945b97e2ff26424a014ae785bf7076c5df521631be3054` | Same decision §4 |
| Alternative B owner decision document | `docs/decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md` = `3465b3503ad75daa40fead701fad36d165d642568bc60c98f67c3870462b6361` | Committed at `d804d9062bab5d9d540df5c2d64f1b2c31bdd667` |

These inputs supply accepted validation context and ordering semantics. They do not adopt the validation-catalog approval chain or the V/W/A roles.

## 3. E-1 / F-01 / F-02 / F-03 / P4 preservation

| Record | SHA-256 | Preserved status |
|---|---|---|
| E-1 entry owner decision | `573f248a41b6d0215d294b74152ab17c64478fc6ac484884dff0f001b46b84fb` | Q-E1 through Q-E5; E-2 sequencing |
| E-1 owner acceptance | `27de68690cca7027d46b0692e52e5da71641e38e13c4037fd31937d08f823b93` | Accepted with qualifications |
| F-01 owner acceptance | `30205d32409c097624da25cfd9ed6608f1588d79f3af96231125da06e6ad194b` | Closed with qualifications |
| F-02 parity owner acceptance | `00f39ae2a1fc1675fa195c3af0ba040fdd05eed956e497f5e0c6fa5285a56107` | Closed with qualifications |
| F-03 owner acceptance | `44c85e0062c42ddec6e98a74268d71f358a9435f506089aa416e6045d47fb944` | Closed with qualifications |
| P4 coverage owner acceptance | `3bdccfdf3e24d4a00fd9f2e600cee13e3eab94014def03a349023eb2bf3e4b6e` | Refusal-only coverage preserved |

The P0 record does not reopen, widen, or promote these closures.

## 4. E-2 entry chain

| Record | SHA-256 | Status |
|---|---|---|
| E-2 entry assessment | `f4212a8a1302695dae53a763682c0dc79de3a47af02e29b14abffbec96e1b664` | `RECONCILIATION REQUIRED`, accepted |
| E-2 entry owner decision | `ee0b2b81682e266488f59e0abcb870664ba25eaa0048764f885b44ae72793c79` | `ENTRY RECONCILIATION AUTHORIZED WITH QUALIFICATIONS` |

The entry decision authorizes P0 and D1 as separate documentation-only activities. It does not resolve Q-E1 or Q-E2, does not issue P0 owner acceptance, and does not authorize E-2 entry or implementation.

## 5. Ancestry checks

Every commit below is an ancestor of the P0 baseline `4263c8bd85bfed9c05b066d8cbc7524a1ba1a4fe`:

| Commit | Subject |
|---|---|
| `504b6d06ac88591072b11f428ae88cade390c075` | Freeze B6 native platform inputs and preserve plan Q1 |
| `4aab87761078a92d05cd08aa1998cbe2c4f6aaf1` | Accept B6-F02 Mechanism B and authorize bounded implementation |
| `d804d9062bab5d9d540df5c2d64f1b2c31bdd667` | Record Alternative B integrated Start authority decision |
| `142cc2308dfbe7fbab5231e1db0a6fcecc9922e6` | Accept the bounded F02 publication policy and B6-S2 with qualifications |
| `c7b76210928e1c6e14ea9bd64b2b5016d1b21f2e` | Accept Slice E entry and authorize bounded E-1 |
| `8bd71425eb31b69290affa2b86c78afce1bd0882` | Accept Slice E-1 with qualifications |
| `1ed1fc706ea76954e4f1d7d43673048ef82072ec` | Close Slice E-1 F-01 with qualifications |
| `6d08ae569c03e96872fcc5d330435991952207d6` | Close Slice E-1 F-03 with qualifications |
| `e86254e60fb3ded770156519e1d3e15e73bf36fc` | Close Slice E-1 F-02 with qualifications |
| `2d39380d0c0b61753097c197ce7c1df03cdddcb7` | Accept refusal-only P4 coverage with qualifications |
| `a35ce108d3f65c1a560a4b01ef91fa3d9ade900d` | Assess Slice E-2 entry as reconciliation required |
| `4263c8bd85bfed9c05b066d8cbc7524a1ba1a4fe` | Authorize Slice E-2 entry reconciliation with qualifications |

## 6. What is not adopted

- The validation-catalog approval chain.
- The V/W/A roles and their mapping to Slice E roles.
- The `/etc/gagamba/accepted-validation-catalogs` and `/etc/gagamba/validation-catalog-evidence` roots as repository policy.
- Any catalog instance or approval record.
- Any F02 expansion to validation catalogs or the final-admission chain.
- Any schema reader, catalog store, trust anchor, capability, runtime authority, or production code.
- Any claim that the external `Solo/` package is durable or independently published.
