# Q2 resolution matrix

Frozen §3 and entry §3 require bounded refusal for these unusable supplied projections. Both R2 runs returned no decoded value and raised no exception. The raw ledgers record SHA-256 values for both original input files in each case.

| Case | R1 result in both configurations | Required R2 | Debug R2 | Release R2 |
| --- | --- | --- | --- | --- |
| B2_malformed_expected_value__q1__uD800_u0058 | throw InvalidOperationException | Expectation | Expectation | Expectation |
| B2_uid_number_72e_2147483649 | throw ArgumentOutOfRangeException | Expectation | Expectation | Expectation |
| B2_uid_number_72e2147483648 | throw ArgumentOutOfRangeException | Expectation | Expectation | Expectation |
| B2_uid_number_72e9999999999999999999999999 | throw ArgumentOutOfRangeException | Expectation | Expectation | Expectation |
| Integrated_malformed_expected_value__q1__uD800_u0058 | throw InvalidOperationException | Expectation | Expectation | Expectation |
| Integrated_policy_malformed_FilterSourceName | throw InvalidOperationException | Expectation | Expectation | Expectation |
| Integrated_policy_malformed_InputSourceName | throw InvalidOperationException | Expectation | Expectation | Expectation |
| Integrated_policy_malformed_MaxObjects | throw ArgumentOutOfRangeException | Expectation | Expectation | Expectation |
| Integrated_policy_malformed_ToolSourceName | throw InvalidOperationException | Expectation | Expectation | Expectation |
| Integrated_policy_malformed_TotalBudgetBytes | throw ArgumentOutOfRangeException | Expectation | Expectation | Expectation |
| Integrated_uid_number_72e_2147483649 | throw ArgumentOutOfRangeException | Expectation | Expectation | Expectation |
| Integrated_uid_number_72e2147483648 | throw ArgumentOutOfRangeException | Expectation | Expectation | Expectation |
| Integrated_uid_number_72e9999999999999999999999999 | throw ArgumentOutOfRangeException | Expectation | Expectation | Expectation |
