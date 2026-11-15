# Thirteen original Q2 cases

| Q2 case | Expected | R1 Debug | R1 Release | R2 Debug | R2 Release | Result |
|---|---|---|---|---|---|---|
| CODEC-Q2-F01 `B2/malformed-expected-value//q1/\uD800\u0058` | Expectation, no data | throw InvalidOperationException | throw InvalidOperationException | Expectation | Expectation | Closed |
| CODEC-Q2-F02 `B2/uid-number/72e2147483648` | Expectation, no data | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation | Closed |
| CODEC-Q2-F02 `B2/uid-number/72e9999999999999999999999999` | Expectation, no data | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation | Closed |
| CODEC-Q2-F02 `B2/uid-number/72e-2147483649` | Expectation, no data | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation | Closed |
| CODEC-Q2-F01 `Integrated/malformed-expected-value//q1/\uD800\u0058` | Expectation, no data | throw InvalidOperationException | throw InvalidOperationException | Expectation | Expectation | Closed |
| CODEC-Q2-F01 `Integrated/policy/malformed/FilterSourceName` | Expectation, no data | throw InvalidOperationException | throw InvalidOperationException | Expectation | Expectation | Closed |
| CODEC-Q2-F01 `Integrated/policy/malformed/InputSourceName` | Expectation, no data | throw InvalidOperationException | throw InvalidOperationException | Expectation | Expectation | Closed |
| CODEC-Q2-F02 `Integrated/policy/malformed/MaxObjects` | Expectation, no data | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation | Closed |
| CODEC-Q2-F01 `Integrated/policy/malformed/ToolSourceName` | Expectation, no data | throw InvalidOperationException | throw InvalidOperationException | Expectation | Expectation | Closed |
| CODEC-Q2-F02 `Integrated/policy/malformed/TotalBudgetBytes` | Expectation, no data | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation | Closed |
| CODEC-Q2-F02 `Integrated/uid-number/72e2147483648` | Expectation, no data | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation | Closed |
| CODEC-Q2-F02 `Integrated/uid-number/72e9999999999999999999999999` | Expectation, no data | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation | Closed |
| CODEC-Q2-F02 `Integrated/uid-number/72e-2147483649` | Expectation, no data | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation | Closed |

Frozen §3 exact semantic comparison and entry §3 bounded refusal govern each row. Both builds refused without a result object. Hashes match the original Q2 fixture bytes. [Exact rows and traces](raw/q2-cases.json) · [Original Q2 findings](../rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/findings.md)
