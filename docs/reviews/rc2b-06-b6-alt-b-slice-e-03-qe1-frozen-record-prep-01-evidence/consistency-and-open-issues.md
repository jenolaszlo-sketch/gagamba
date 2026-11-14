# Consistency review and FRP-01

## FRP-01 — Inherited bootstrap schema needs a controlling binding

The seven owner rulings settle the v3-specific JSON changes and integrated
tail. They instruct the reader to preserve the corresponding accepted v2
bootstrap and identity members outside those changes. F1 line 46 makes
the same delegation (SHA-256
`ea8c50d92a7bf7fe4bd2dca0f91155fc4533bef314fc4980036535c891822b93`).
The [owner decision §2](../../decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-normative-owner-01.md)
(blob `f3639dc619dbfee39859afb7da553e2167d9b83d`) includes a partial,
intentionally incomplete JSON example.

| Evidence | Exact observation | Consequence |
|---|---|---|
| SPEC §§6.2.2, 6.2.6, blob `7f9f615f75527221e81b7aaa498f4b564acb62b9` | Version and integrated identity location and complete identity fields are specified. The surrounding root/runtime example uses ellipses in both pinned executable objects and runtime values and omits base transport members. | It supplies no exhaustive root/nested member table or complete normative bootstrap bytes. |
| B2 amendment §2, blob `95dc3221ae2a5579f966526516073cdd0f392aa9` | Retains base JSON fields, adds the B2 identity and deadline, and states that integrated v2 forbids both B2 fields. It gives B2 null placement and strict numeric/shape constraints. | The exclusion is authoritative. It supplies no exhaustive base schema by itself. |
| WIRE §§C, D, H, blob `501487227208d935c3ab1337c9f87055c9e14b8d`; wire Q1 identity review §9, blob `f6e98a06a0bca05fc681a1eeeb97e5d75b038c2c` | WIRE adopts the wire delta and identifies implementation obligations. The Q1 review quotes historical CLR record declarations for the bootstrap and integrated wrapper. | A CLR declaration or illustrative example alone leaves wire required/omitted/null behavior and some nested fields unspecified. |
| Original B2 owner acceptance §§2–4, blob `3056254758f56d8a1cb99dd9b03f08e6e05fa689` | Accepts bounded legacy bootstrap implementation and evidence. Legacy review records size, depth and unknown-member rejection in that implementation. | This supplies historical compatibility evidence. It does not select which serializers, build-specific profile members, and permissive omission behavior are the exact E-3 wire oracle. |
| `QualifiedRecoveryConnection.cs` blob `2f6f779d6b5414c22790a7fb8a892b253e044963` and `RecoveryProtocol.cs` blob `f19017dccce5b83d42bb9276c6e8839d13564061` | Integrated path sets the B2 deadline null, starts from an integrated record with B2 identity null, then serializes the record with default options. Both public record fields are emitted as JSON null by that serializer. The integrated decoder checks whether the deserialized values are non-null. Omission and explicit null can reach the same result. This was established by source inspection. | Existing serialization cannot serve as an exact normative fallback. It has a different presence rule from a literal reading of the accepted integrated prohibition. |
| `IntegratedRecoveryHost.cs` blob `9a8ec67d3424bf1971bea04b056d78cee7c3501b` and source declarations of nested profile and runtime | The current implementation carries additional fields for the runtime pin, process identity and profile. The profile's Debug build can contain a qualification-specific member. | Importing an implementation schema wholesale risks exposing a build-specific or unrelated field and supplying unstated presence and token rules. No build was run. |

**Impact.** The positive complete B2 and integrated bootstraps and mandatory
negative member/type tests cannot be derived unambiguously. Strict unknown
member and pre-integration refusal depend on the exact allowed sets. Accepting
serializer-generated nulls or freezing every current implementation member
would change the contract under review. The distinction from BJ-02 is
important. The owner already chose mandatory nulls for the qualification
association inside the selected identity only.

**Smallest owner decision.** Bind an exact versioned legacy schema source or
set out the exact inherited root, closure and integrated nested object
tables, including member types, required/omitted/null behavior and value
constraints. Confirm the integrated B2-field exclusion and identify any
separate legacy compatibility action for current serialization. Keep any
chosen compatibility accommodation explicit, version-scoped, and separate
from the existing v3 rulings. Do not reopen BJ/IE or change a legacy serializer
during this work. If all required accepted bytes can be located, record their
exact identity and how they settle the discrepancy.

## Internal consistency results

| Items from commission §27 | Result |
|---|---|
| 1, 15, 16, 19, 20 | **Blocked for complete bootstrap.** Exact inherited mandatory member/type and refusal rules and corresponding full-record positive/negative vectors await FRP-01. Settled-field vectors support the seven rulings but cannot stand in for the missing base schema. |
| 2, 3 | Binary supported payload widths/offsets, context and version axes reconcile. Numeric v3 selection is explicit and separate from inherited binary framing. Legacy v1/v2 retain accepted treatment. |
| 4–7 | Boolean false, binary zero, mandatory nulls, literal lowercase SHA-256 text, and independent raw capability comparison agree. Caller authentication and protected custody remain external. |
| 8–13 | B2 76, integrated 76/105, 37 at 246 for the bound pair, both u8 tail prefixes and inherited 36/48-byte confirmations verified by mechanical checks. Reconstructed payloads preserve all inherited bytes outside status. |
| 14, 17, 18 | Existing 32-byte ReleaseRequest field and later owner recomputation remain separate. No operational authority or E-4/E-6 implementation requirement is projected. |

No conflict was found within the seven rules. Independent Q1 is deferred
until the inherited schema is bound and a complete candidate and positive
and negative bootstrap vectors can be submitted.
