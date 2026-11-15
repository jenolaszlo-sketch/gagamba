# Exact Q2 case results

| Q2 synthetic fixture | Baseline Debug | Baseline Release | R2 Debug | R2 Release |
|---|---|---|---|---|
| `B2_malformed_expected_value__q1__uD800_u0058` | throw InvalidOperationException | throw InvalidOperationException | Expectation | Expectation |
| `B2_uid_number_72e2147483648` | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation |
| `B2_uid_number_72e9999999999999999999999999` | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation |
| `B2_uid_number_72e_2147483649` | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation |
| `Integrated_malformed_expected_value__q1__uD800_u0058` | throw InvalidOperationException | throw InvalidOperationException | Expectation | Expectation |
| `Integrated_policy_malformed_FilterSourceName` | throw InvalidOperationException | throw InvalidOperationException | Expectation | Expectation |
| `Integrated_policy_malformed_InputSourceName` | throw InvalidOperationException | throw InvalidOperationException | Expectation | Expectation |
| `Integrated_policy_malformed_MaxObjects` | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation |
| `Integrated_policy_malformed_ToolSourceName` | throw InvalidOperationException | throw InvalidOperationException | Expectation | Expectation |
| `Integrated_policy_malformed_TotalBudgetBytes` | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation |
| `Integrated_uid_number_72e2147483648` | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation |
| `Integrated_uid_number_72e9999999999999999999999999` | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation |
| `Integrated_uid_number_72e_2147483649` | throw ArgumentOutOfRangeException | throw ArgumentOutOfRangeException | Expectation | Expectation |

The replay verified identical input hashes before and after. No corrected case published a decoded object. [Exact hashes and stacks](raw/cases.json)
