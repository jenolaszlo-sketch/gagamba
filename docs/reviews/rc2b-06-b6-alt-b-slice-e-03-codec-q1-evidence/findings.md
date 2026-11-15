# Counterexample register

The labels below match the [Debug ledger](vectors-debug/results.jsonl) and [Release ledger](vectors-release/results.jsonl). Each named failing bootstrap vector has the actual wire bytes in a same-stem .input file and its separately supplied projection in .expected.json, or a lifecycle instruction when the element intentionally has no live document. Underscores replace punctuation in vector filenames. The ordinary positive and each mismatch case use the same independent original-capability digest, calculated as SHA-256 of ASCII "Q1 independent original-capability". The test source supplies selected context version 3 and the indicated topology.

## CODEC-Q1-F01 · High · Blocking

Frozen §3 relationship 6 binds the separately supplied verified-policy projection, explicitly including source basenames, budget and object count. The paragraph after relationship 7 prescribes exact semantic comparisons and refusal for missing required comparison data. The wire bytes are [Integrated.json](vectors-debug/Integrated.json), with the unmodified [supplied projection](vectors-debug/Integrated-projection.json) as positive control. Mutate only the supplied profile, one member per invocation.

| Expected profile member | On wire and in the valid expectation | Mutated expectation | Required result | Actual result |
|---|---|---|---|---|
| ToolSourceName | "tool" | Omitted or "different-basename" | Bounded refusal, no data | None and structural data, both |
| InputSourceName | "input" | Omitted or "different-basename" | Bounded refusal, no data | None and structural data, both |
| CoordinatorSourceName | "coordinator" | Omitted or "different-basename" | Bounded refusal, no data | None and structural data, both |
| FilterSourceName | "filter" | Omitted or "different-basename" | Bounded refusal, no data | None and structural data, both |
| TotalBudgetBytes | 16777216 | Omitted or 16777215 | Bounded refusal, no data | None and structural data, both |
| MaxObjects | 4 | Omitted or 5 | Bounded refusal, no data | None and structural data, both |

In ValidationPreCodec.cs, lines 313–316 skip the employed rules before retrieving an expected member. ValidScalar and the schema independently enforce the constants on the wire. Comparing only the wire constants permits inconsistent or absent policy data. Require the six expected fields and exact semantic agreement. Keep their wire domains and the other comparisons.

## CODEC-Q1-F02 · Medium · Blocking

Frozen §3 relationship 2 calls for exact agreement with the originally supplied session, as an unambiguous typed claim. Keep the complete on-wire positive. Prepend the conflicting member "\u0053ession":"7abb4591-d3c0-4012-a398-114477880063" to its supplied projection, before the ordinary "Session":"7abb4591-d3c0-4012-a398-114477880003". Expect bounded refusal. Actual is None with data for both forms. The decoded names are the same, and ValidationPreCodec.cs:318 uses JsonElement.TryGetProperty for the projection. That API chooses the last matching member. The wire correctly detects duplicates even with escaped spelling. Refuse ambiguity along the consumed projection paths or accept an equivalently unambiguous typed representation. [B2 counterexample projection](vectors-debug/B2_ambiguous_original_session.expected.json) · [Integrated counterexample projection](vectors-debug/Integrated_ambiguous_original_session.expected.json)

## CODEC-Q1-F03 · Medium · Blocking

Use either full positive and parse its corresponding valid projection into a JsonDocument. Retain RootElement and dispose the document before passing that element to DecodeBootstrap. Expect bounded refusal without data under entry §3 and the frozen requirement to refuse missing comparison data. Actual is an uncaught ObjectDisposedException at ValidationPreCodec.cs:27 for both forms, after the out parameter is set to null. A malformed escaped string within an otherwise live expectation refused. This case is an expectation-lifetime failure and does not require any malformed wire bytes. Use an explicitly owned projection or handle unavailable projection state with a bounded refusal. [B2 input](vectors-debug/B2_disposed_original.input) · [Integrated input](vectors-debug/Integrated_disposed_original.input)

## CODEC-Q1-F04 · Medium · Blocking

Pack a successful 36-byte message 10 with header 01 00 00 00 and the digest SHA-256 of ASCII "Q1 independent install". Supply the resulting digest array to ValidationPreBinaryExpectations and confirm the baseline returns None in B2 version 3 with MatchingPredecessor true. Overwrite the same array with the digest of ASCII "Q1 independent replacement-install", keep the same expectations object, and supply a message 10 with the replacement digest. The probe records exact hex for both payloads as binary-expectation-alias/baseline and binary-expectation-alias/replaced. Expected Comparison, actual None. ValidationPreCodecData.cs:48–55 retains a ReadOnlyMemory view of caller-owned storage, and ValidationPreCodecBinary.cs:86 uses its current contents. This demonstrates sequential mutation without a race or use of protected inputs. Freeze digests at the expectation boundary or enforce equivalent immutable ownership. The future caller still owns provenance.

## CODEC-Q1-E01 · Low · Non-blocking

Entry §3 names the producer report and evidence under docs/evidence. The implementation commit adds them under docs/reviews and updates three permitted status documents. This documentation placement variance has no source or gate effect. Preserve the actual producer provenance and let the owner disposition the path variance. The prior frozen-record Q1-E01's three parser controls each passed.

## Adversarial review limits

A strict UTF-8 precheck, bounded JSON copy, default JSON depth ceiling and schema-limited object descent bound work from the wire. Scalar grammars reject coerced or out-of-range wire forms. Binary code matches fixed lengths before indexing. Integrated time arithmetic subtracts two positive signed 64-bit values, so the difference stays within range. Wire parser rejection never published a structural result in the independent negative controls. The successful bootstrap result is detached from the caller's wire buffer. The projection and memory issues are the observed failures at the supplied-data boundary. Internal memory ownership remains material even though this revision has no production consumer. No live time, cryptographic provenance, protected I/O, scheduling, transport or authority was exercised.
