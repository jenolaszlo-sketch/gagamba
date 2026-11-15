# Q2 blocking findings

Both controls use a valid v3 wire witness, the original-capability digest from the independently committed Q1 witness generator, and a live caller-supplied `JsonElement` created from the altered projection. The test mutates exactly one value for each recorded observation. The adopted frozen record §3 requires exact semantic comparison using declared types. The owner entry §3 requires a bounded refusal with no validated payload for a failed check. Every case below instead escapes through `JsonElement.DeepEquals` at `ValidationPreCodec.cs:363` in R1. Each was repeated against the original revision and both R1 configurations. [Exact input hashes, outcomes and complete stack traces](raw/q2-exceptions.json)

## CODEC-Q2-F01 · Medium · Blocking

A syntactically indexed JSON string can carry an invalid surrogate sequence. When the equality implementation needs to unescape the expected member, it can throw `InvalidOperationException`. The local guard around decoding projection member names and the narrow disposed-document guard do not contain a string value failure.

- Both topologies throw for an independently supplied `EvidenceDirectory` of `"/q1/\uD800\u0058"`. These also threw at the old projection comparator.
- Integrated throws for `"\uD800"` supplied separately for each of `ToolSourceName`, `InputSourceName` and `FilterSourceName`. The original implementation accepted those test inputs because it skipped the comparison. R1 checks the member but permits an exception to escape.
- Nearby malformed values can already return refusal through the equality routine’s short-circuit. The qualification does not rely on those short-circuits for safety.

Expected bounded refusal with no validated data. R1 threw the exception in all five cases and published no result. The smallest correction is to reject unusable expected string representations locally when comparing projected scalar values. Preserve ordinal decoded-string equality, valid escape spellings and distinct wire validation. [Exact B2 projection](findings-inputs/B2_malformed_expected_value__q1__uD800_u0058.projection) · [Exact B2 wire](findings-inputs/B2_malformed_expected_value__q1__uD800_u0058.wire) · [Exact integrated policy projection](findings-inputs/Integrated_policy_malformed_ToolSourceName.projection)

## CODEC-Q2-F02 · Medium · Blocking

A JSON number can be indexed with an exponent too large for the semantic comparer to normalize. Checking `JsonValueKind.Number` leaves that input in the comparison. `DeepEquals` throws `ArgumentOutOfRangeException` with parameter `exponent`.

- In each topology, root `ControllerUid` values `72e2147483648`, `72e-2147483649` and `72e9999999999999999999999999` produced the exception. These six observations also threw against the original implementation.
- Integrated `TotalBudgetBytes` set to `16777216e2147483648` and `MaxObjects` set to `4e2147483648` produced the exception. The original implementation skipped these policy comparisons and returned success.
- The adjacent positive projected values `4.00e0` and `167772160e-1` matched normally. The strict wire grammar still refused alternate wire number tokens.

Expected bounded refusal with no validated data. R1 threw for each case and published no result. The smallest correction is to make the numeric leaf comparison return refusal for unrepresentable inputs while preserving exact agreement for valid values. A targeted comparison boundary is sufficient. Changes to scanning, dispatch, binary ownership and the protocol record are unnecessary. [Exact UID projection](findings-inputs/B2_uid_number_72e2147483648.projection) · [Exact maximum-object projection](findings-inputs/Integrated_policy_malformed_MaxObjects.projection)

Both findings affect internal error handling. The shipping graph contains no production codec caller. The probes establish neither protected effects nor authority. The tests use only synthetic data and local managed execution.
